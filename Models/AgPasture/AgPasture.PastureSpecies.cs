//-----------------------------------------------------------------------
// <copyright file="AgPasture.PastureSpecies.cs" project="AgPasture" solution="APSIMx" company="APSIM Initiative">
//     Copyright (c) ASPIM initiative. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Models.Core;
using Models.Soils;
using Models.PMF;
using Models.Soils.Arbitrator;
using Models.Interfaces;
using APSIM.Shared.Utilities;

namespace Models.AgPasture
{
    /// <summary>Describes a pasture species</summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class PastureSpecies : Model, ICrop, ICrop2, ICanopy, IUptake
    {
        #region Links, events and delegates  -------------------------------------------------------------------------------

        //- Links  ----------------------------------------------------------------------------------------------------

        /// <summary>Link to APSIM's Clock (time information)</summary>
        [Link]
        private Clock myClock = null;

        /// <summary>Link to APSIM's WeatherFile (meteorological information)</summary>
        [Link]
        private IWeather myMetData = null;

        /// <summary>Link to APSIM summary</summary>
        [Link]
        private ISummary mySummary = null;

        /// <summary>Link to the Soil (soil layers and other information)</summary>
        [Link]
        private Soils.Soil mySoil = null;

        /// <summary>Link to apsim's Resource Arbitrator module</summary>
        [Link(IsOptional = true)]
        private Arbitrator.Arbitrator apsimArbitrator = null;

        /// <summary>Link to apsim's Resource Arbitrator module</summary>
        [Link(IsOptional = true)]
        private SoilArbitrator soilArbitrator = null;

        //- Events  ---------------------------------------------------------------------------------------------------

        /// <summary>Reference to a FOM incorporation event</summary>
        /// <param name="Data">The data with soil FOM to be added.</param>
        public delegate void FOMLayerDelegate(Soils.FOMLayerType Data);

        /// <summary>Occurs when plant is depositing senesced roots.</summary>
        public event FOMLayerDelegate IncorpFOM;

        /// <summary>Reference to a BiomassRemoved event</summary>
        /// <param name="Data">The data about biomass deposited by this plant to the soil surface.</param>
        public delegate void BiomassRemovedDelegate(PMF.BiomassRemovedType Data);

        /// <summary>Occurs when plant is depositing litter.</summary>
        public event BiomassRemovedDelegate BiomassRemoved;

        /// <summary>Reference to a WaterChanged event</summary>
        /// <param name="Data">The changes in the amount of water for each soil layer.</param>
        public delegate void WaterChangedDelegate(PMF.WaterChangedType Data);

        /// <summary>Occurs when plant takes up water.</summary>
        public event WaterChangedDelegate WaterChanged;

        /// <summary>Reference to a NitrogenChanged event</summary>
        /// <param name="Data">The changes in the soil N for each soil layer.</param>
        public delegate void NitrogenChangedDelegate(Soils.NitrogenChangedType Data);

        /// <summary>Occurs when the plant takes up soil N.</summary>
        public event NitrogenChangedDelegate NitrogenChanged;

        #endregion

        #region Canopy interface  ------------------------------------------------------------------------------------------

        /// <summary>Canopy type</summary>
        public string CanopyType
        {
            get { return Name; }
        }

        /// <summary>The albedo value for this species</summary>
        private double myAlbedo = 0.26;

        /// <summary>Gets or sets the species albedo.</summary>
        /// <value>The albedo value.</value>
        [Description("Albedo for canopy:")]
        public double Albedo
        {
            get { return myAlbedo; }
            set { myAlbedo = value; }
        }

        /// <summary>The maximum stomatal conductance (m/s)</summary>
        private double myGsmax = 0.011;

        /// <summary>Gets or sets the gsmax</summary>
        public double Gsmax
        {
            get { return myGsmax; }
            set { myGsmax = value; }
        }

        /// <summary>The solar radiation at which stomatal conductance decreases to 50% (W/m2)</summary>
        private double myR50 = 200;

        /// <summary>Gets or sets the R50</summary>
        public double R50
        {
            get { return myR50; }
            set { myR50 = value; }
        }

        /// <summary>Gets the LAI of live tissue (m^2/m^2)</summary>
        public double LAI
        {
            get { return LAIGreen; }
        }

        /// <summary>Gets the total LAI, live + dead (m^2/m^2)</summary>
        public double LAITotal
        {
            get { return LAIGreen + LAIDead; }
        }

        /// <summary>Gets the cover green (0-1)</summary>
        public double CoverGreen
        {
            get { return CalcPlantCover(greenLAI); }
        }

        /// <summary>Gets the cover total (0-1)</summary>
        public double CoverTotal
        {
            get { return CalcPlantCover(greenLAI + deadLAI); }
        }

        /// <summary>Gets the canopy height (mm)</summary>
        [Description("Plants average height")]
        [Units("mm")]
        public double Height
        {
            get { return Math.Max(20.0, HeightFromMass.Value(StandingWt)); } // TODO: update this function
        }

        /// <summary>Gets the canopy depth (mm)</summary>
        [Description("The depth of the canopy (mm)")]
        [Units("mm")]
        public double Depth
        {
            get { return Height; }
        }

        // TODO: have to verify how this works (what exactly is needed by MicroClimate
        /// <summary>Plant growth limiting factor, supplied to another module calculating potential transpiration</summary>
        public double FRGR
        {
            get { return 1.0; }
        }

        /// <summary>Potential evapotranspiration, as calculated by MicroClimate</summary>
        [XmlIgnore]
        public double PotentialEP
        {
            get { return myWaterDemand; }
            set
            {
                myWaterDemand = value;
                demandWater = myWaterDemand;
            }
        }

        /// <summary>Light profile (energy available for each canopy layer)</summary>
        private CanopyEnergyBalanceInterceptionlayerType[] myLightProfile;

        /// <summary>Gets or sets the light profile for this plant, as calculated by MicroClimate</summary>
        [XmlIgnore]
        public CanopyEnergyBalanceInterceptionlayerType[] LightProfile
        {
            get { return myLightProfile; }
            set
            {
                InterceptedRadn = 0.0;
                myLightProfile = value;
                foreach (CanopyEnergyBalanceInterceptionlayerType canopyLayer in myLightProfile)
                    InterceptedRadn += canopyLayer.amount;
            }
        }

        #endregion

        #region ICrop implementation  --------------------------------------------------------------------------------------

        /// <summary>Gets a list of cultivar names (not used by AgPasture)</summary>
        public string[] CultivarNames
        {
            get { return null; }
        }

        /// <summary>Sows the plant</summary>
        /// <param name="cultivar">Cultivar type (not used)</param>
        /// <param name="population">Plants per area (not used)</param>
        /// <param name="depth">Sowing depth (not used)</param>
        /// <param name="rowSpacing">Space between rows (not used)</param>
        /// <param name="maxCover">Maximum ground cover (not used)</param>
        /// <param name="budNumber">Number of buds (not used)</param>
        /// <remarks>
        /// For AgPasture species the sow parameters are not used, the command to sow simply enables the plant to grow,
        /// This is done by setting the plant 'alive'. From this point germination processes takes place and eventually emergence.
        /// At emergence, plant DM is set to its default minimum value, allocated according to EmergenceFractions and with
        /// optimum N concentration. Plant height and root depth are set to their minimum values.
        /// </remarks>
        public void Sow(string cultivar, double population, double depth, double rowSpacing, double maxCover = 1, double budNumber = 1)
        {
            if (isAlive)
                mySummary.WriteWarning(this, " Cannot sow the pasture species \"" + Name + "\", as it is already growing");
            else
            {
                ResetZero();
                isAlive = true;
                phenologicStage = 0;
                mySummary.WriteMessage(this, " The pasture species \"" + Name + "\" has been sown today");
            }
        }

        /// <summary>Returns true if the crop is ready for harvesting</summary>
        public bool IsReadyForHarvesting
        {
            get { return false; }
        }

        /// <summary>Harvest the crop</summary>
        public void Harvest()
        {
        }

        /// <summary>End the crop</summary>
        public void EndCrop()
        {
        }

        #endregion

        #region ICrop2 implementation  -------------------------------------------------------------------------------------

        /// <summary>
        /// Generic descriptor used by MicroClimate to look up for canopy properties for this plant
        /// </summary>
        [Description("Generic type of crop")]
        [Units("")]
        public string CropType
        {
            get { return Name; }
        }

        /// <summary>Flag whether the plant in the ground</summary>
        [XmlIgnore]
        public bool PlantInGround
        {
            get { return true; }
        }

        /// <summary>Flag whether the plant has emerged</summary>
        [XmlIgnore]
        public bool PlantEmerged
        {
            get { return true; }
        }

        /// <summary>
        /// The set of crop canopy properties used by Arbitrator for light and energy calculations
        /// </summary>
        public CanopyProperties CanopyProperties
        {
            get { return myCanopyProperties; }
        }

        /// <summary>The canopy data for this plant</summary>
        CanopyProperties myCanopyProperties = new CanopyProperties();

        /// <summary>
        /// The set of crop root properties used by Arbitrator for water and nutrient calculations
        /// </summary>
        public RootProperties RootProperties
        {
            get { return myRootProperties; }
        }

        /// <summary>The root data for this plant</summary>
        RootProperties myRootProperties = new RootProperties();

        /// <summary> Water demand for this plant (mm/day)</summary>
        [XmlIgnore]
        public double demandWater { get; set; }

        /// <summary> The actual supply of water to the plant (mm), values given for each soil layer</summary>
        [XmlIgnore]
        public double[] uptakeWater { get; set; }

        /// <summary>Nitrogen demand for this plant (kgN/ha/day)</summary>
        [XmlIgnore]
        public double demandNitrogen { get; set; }

        /// <summary>
        /// The actual supply of nitrogen (ammonium and nitrate) to the plant (kgN/ha), values given for each soil layer
        /// </summary>
        [XmlIgnore]
        public double[] uptakeNitrogen { get; set; }

        /// <summary>
        /// The actual supply of nitrogen (nitrate) to the plant (kgN/ha), values given for each soil layer
        /// </summary>
        [XmlIgnore]
        public double[] uptakeNO3 { get; set; }

        /// <summary>
        /// The actual supply of nitrogen (ammonium) to the plant (kgN/ha), values given for each soil layer
        /// </summary>
        [XmlIgnore]
        public double[] uptakeNH4 { get; set; }

        /// <summary>The proportion of nitrogen uptake from each layer in the form of nitrate (0-1)</summary>
        [XmlIgnore]
        public double[] uptakeNitrogenPropNO3 { get; set; }

        #endregion

        #region Model parameters  ------------------------------------------------------------------------------------------

        // NOTE: default parameters describe a generic perennial ryegrass species

        /// <summary>Family type for this plant species (grass/legume/brassica)</summary>
        private PlantFamilyType mySpeciesFamily = PlantFamilyType.Grass;

        /// <summary>Gets or sets the species family type.</summary>
        [Description("Family type for this plant species [grass/legume/brassica]:")]
        public PlantFamilyType SpeciesPlantFamily
        {
            get { return mySpeciesFamily; }
            set
            {
                mySpeciesFamily = value;
                isLegume = mySpeciesFamily == PlantFamilyType.Legume;
            }
        }

        /// <summary>Gets or sets the species photosynthetic pathway.</summary>
        /// <value>The species photo pathway.</value>
        [Description("Metabolic pathway for C fixation during photosynthesis [C3/C4/CAM]:")]
        public PhotosynthesisPathwayType PhotosynthesisPathway { get; set; } = PhotosynthesisPathwayType.C3;

        /// <summary>Gets or sets the initial shoot DM.</summary>
        [Description("Initial above ground DM (leaf, stem, stolon, etc) [kg DM/ha]:")]
        [Units("kg/ha")]
        public double InitialShootDM { get; set; } = 2000.0;

        /// <summary>Gets or sets the initial dm root.</summary>
        [Description("Initial below ground DM (roots) [kg DM/ha]:")]
        [Units("kg/ha")]
        public double InitialRootDM { get; set; } = 500.0;

        /// <summary>Gets or sets the initial root depth.</summary>
        [Description("Initial depth for roots [mm]:")]
        [Units("mm")]
        public double InitialRootDepth { get; set; } = 750.0;

        // TODO: temporary?? initial DM fractions for grass or legume species
        /// <summary>The initial fractions of DM for each plant part in grass</summary>
        private double[] initialDMFractions_grass = new double[]
        {0.15, 0.25, 0.25, 0.05, 0.05, 0.10, 0.10, 0.05, 0.00, 0.00, 0.00};

        /// <summary>The initial fractions of DM for each plant part in legume</summary>
        private double[] initialDMFractions_legume = new double[]
        {0.20, 0.25, 0.25, 0.00, 0.02, 0.04, 0.04, 0.00, 0.06, 0.12, 0.12};

        /// <summary>The initial fractions of DM for each plant part in forbs</summary>
        private double[] initialDMFractions_forbs = new double[]
        {0.20, 0.20, 0.15, 0.05, 0.15, 0.15, 0.10, 0.00, 0.00, 0.00, 0.00};

        // - Photosysnthesis and growth  ------------------------------------------------------------------------------

        /// <summary>Reference CO2 assimilation rate during photosynthesis [mg CO2/m2 leaf/s].</summary>
        [Description("Reference CO2 assimilation rate during photosynthesis [mg CO2/m2/s]:")]
        [Units("mg/m^2/s")]
        public double ReferencePhotosynthesisRate { get; set; } = 1.0;

        /// <summary>Leaf photosynthetic efficiency [mg CO2/J]</summary>
        [Description("Leaf photosynthetic efficiency [mg CO2/J]:")]
        [Units("mg CO2/J")]
        public double PhotosyntheticEfficiency { get; set; } = 0.01;

        /// <summary>Photosynthesis curvature parameter [J/kg/s]</summary>
        [Description("Photosynthesis curvature parameter [J/kg/s]:")]
        [Units("J/kg/s")]
        public double PhotosynthesisCurveFactor { get; set; } = 0.8;

        /// <summary>Fraction of total radiation that is photosynthetically active [0-1]</summary>
        [XmlIgnore]
        private double fractionPAR { get; set; } = 0.5;

        /// <summary> Maintenance respiration coefficient - Fraction of DM consumed by respiration [0-1]</summary>
        [Description("Maintenance respiration coefficient [0-1]:")]
        [Units("0-1")]
        public double MaintenanceRespirationCoefficient { get; set; } = 0.03;

        /// <summary>Growth respiration coefficient - fraction of photosynthesis CO2 not assimilated (0-1)</summary>
        [Description("Growth respiration coefficient [0-1]:")]
        [Units("0-1")]
        public double GrowthRespirationCoefficient { get; set; } = 0.25;

        /// <summary>Light extinction coefficient (0-1)</summary>
        [Description("Light extinction coefficient [0-1]:")]
        [Units("0-1")]
        public double LightExtentionCoefficient { get; set; } = 0.5;

        /// <summary>Minimum temperature for growth [oC]</summary>
        /// <value>The growth tmin.</value>
        [Description("Minimum temperature for growth [oC]:")]
        [Units("oC")]
        public double GrowthTmin { get; set; } = 2.0;

        /// <summary>Optimum temperature for growth [oC]</summary>
        [Description("Optimum temperature for growth [oC]:")]
        [Units("oC")]
        public double GrowthTopt { get; set; } = 20.0;

        /// <summary>Curve parameter for growth response to temperature</summary>
        [Description("Curve parameter for growth response to temperature:")]
        [Units("-")]
        public double GrowthTq { get; set; } = 1.75;

        /// <summary>Onset temperature for heat effects on growth [oC]</summary>
        [Description("Onset temperature for heat effects on growth [oC]:")]
        [Units("oC")]
        public double HeatOnsetT { get; set; } = 28.0;

        /// <summary>Temperature for full heat effect on growth (no growth) [oC]</summary>
        [Description("Temperature for full heat effect on growth [oC]:")]
        [Units("oC")]
        public double HeatFullT { get; set; } = 35.0;

        /// <summary>Cumulative degrees-day for recovery from heat stress [oCd]</summary>
        [Description("Cumulative degrees-day for recovery from heat stress [oCd]:")]
        [Units("oCd")]
        public double HeatSumT { get; set; } = 30.0;

        /// <summary>Reference temperature for recovery from heat stress [oC]</summary>
        [Description("Reference temperature for recovery from heat stress [oC]:")]
        [Units("oC")]
        public double HeatRecoverT { get; set; } = 25.0;

        /// <summary>Onset temperature for cold effects on growth [oC]</summary>
        [Description("Onset temperature for cold effects on growth [oC]:")]
        [Units("oC")]
        public double ColdOnsetT { get; set; } = 0.0;

        /// <summary>Temperature for full cold effect on growth (no growth) [oC]</summary>
        [Description("Temperature for full cold effect on growth [oC]:")]
        [Units("oC")]
        public double ColdFullT { get; set; } = -3.0;

        /// <summary>Cumulative degrees for recovery from cold stress [oCd]</summary>
        [Description("Cumulative degrees for recovery from cold stress [oCd]:")]
        [Units("oCd")]
        public double ColdSumT { get; set; } = 20.0;

        /// <summary>Reference temperature for recovery from cold stress [oC]</summary>
        [Description("Reference temperature for recovery from cold stress [oC]:")]
        [Units("oC")]
        public double ColdRecoverT { get; set; } = 00.0;

        /// <summary>Reference CO2 concentration for photosynthesis [ppm]</summary>
        [Description("Reference CO2 concentration for photosynthesis [ppm]:")]
        [Units("ppm")]
        public double ReferenceCO2 { get; set; } = 380.0;

        /// <summary> Coefficient for the function describing the CO2 effect on photosynthesis [ppm CO2]</summary>
        [Description("Coefficient for the function describing the CO2 effect on photosynthesis [ppm CO2]:")]
        [Units("ppm")]
        public double CoefficientCO2EffectOnPhotosynthesis { get; set; } = 700.0;

        /// <summary>Scalling paramenter for the CO2 effects on N uptake [ppm Co2]</summary>
        [Description("Scalling paramenter for the CO2 effects on N requirement [ppm Co2]:")]
        [Units("ppm")]
        public double OffsetCO2EffectOnNuptake { get; set; } = 600.0;

        /// <summary>Minimum value for the effect of CO2 on N requirement [0-1]</summary>
        [Description("Minimum value for the effect of CO2 on N requirement [0-1]:")]
        [Units("0-1")]
        public double MinimumCO2EffectOnNuptake { get; set; } = 0.7;

        /// <summary>Exponent of the function describing the effect of CO2 on N requirement</summary>
        [Description("Exponent of the function describing the effect of CO2 on N requirement:")]
        [Units("-")]
        public double ExponentCO2EffectOnNuptake { get; set; } = 2.0;

        // - Germination and emergence  -------------------------------------------------------------------------------

        /// <summary>Cumulative degrees-day needed for seed germination [oCd]</summary>
        [Description("Cumulative degrees-day needed for seed germination [oCd]:")]
        [Units("oCd")]
        public double DegreesDayForGermination { get; set; } = 100.0;

        /// <summary>The fractions of DM for each plant part at emergence (all plants)</summary>
        private double[] EmergenceDMFractions = new double[]
        {0.60, 0.25, 0.00, 0.00, 0.15, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00};

        // - DM allocation  -------------------------------------------------------------------------------------------

        /// <summary>Maximum fraction of DM allocated to roots (from daily growth) [0-1]</summary>
        [Description("Maximum fraction of DM allocated to roots (from daily growth) [0-1]:")]
        [Units("0-1")]
        public double MaxRootAllocation { get; set; } = 0.25;

        /// <summary>Factor by which DM allocation to shoot is increased due to reproductive growth [0-1]</summary>
        /// <remarks>
        /// Allocation to shoot is typically given by 1-maxRootFraction, but during a given period (spring) it can be increased
        /// to simulate reproductive growth. Shoot allocation is adjsuted by multiplying its base value by 1 + SeasonShootAllocationIncrease
        /// </remarks>
        [Description("Factor by which DM allocation to shoot is increased during 'spring' [0-1]:")]
        [Units("0-1")]
        public double ShootSeasonalAllocationIncrease { get; set; } = 0.8;

        /// <summary>Day for the beginning of the period with higher shoot allocation ('spring')</summary>
        private int doyIniHighShoot = 232;

        /// <summary>Day for the beginning of the period with higher shoot allocation ('spring')</summary>
        /// <remarks>Care must be taken as this varies with north or south hemisphere</remarks>
        [Description("Day for the beginning of the period with higher shoot allocation ('spring'):")]
        [Units("-")]
        public int DayInitHigherShootAllocation
        {
            get { return doyIniHighShoot; }
            set { doyIniHighShoot = value; }
        }

        /// <summary>
        /// Number of days defining the duration of the three phases with higher DM allocation to shoot (onset, sill, return)
        /// </summary>
        private int[] higherShootAllocationPeriods = {30, 60, 30};

        /// <summary>
        /// Number of days defining the duration of the three phases with higher DM allocation to shoot (onset, sill, return)
        /// </summary>
        /// <remarks>
        /// Three numbers are needed, they define the duration of the phases for increase, plateau, and the deacrease in allocation
        /// The allocation to shoot is maximum at the plateau phase, it is 1 + SeasonShootAllocationIncrease times the value of maxSRratio
        /// </remarks>
        [Description("Duration of the three phases of higher DM allocation to shoot [days]:")]
        [Units("days")]
        public int[] HigherShootAllocationPeriods
        {
            get { return higherShootAllocationPeriods; }
            set
            {
                for (int i = 0; i < 3; i++)
                    higherShootAllocationPeriods[i] = value[i];
                // so, if 1 or 2 values are supplied the remainder are not changed, if more values are given, they are ignored
            }
        }

        /// <summary>Fraction of new shoot growth allocated to leaves [0-1]</summary>
        [Description("Fraction of new shoot growth allocated to leaves [0-1]:")]
        [Units("0-1")]
        public double FracToLeaf { get; set; } = 0.7;

        /// <summary>Fraction of new shoot growth allocated to stolons [0-1]</summary>
        [Description("Fraction of new shoot growth allocated to stolons [0-1]:")]
        [Units("0-1")]
        public double FracToStolon { get; set; } = 0.0;

        /// <summary>Specific leaf area [m^2/kg DM]</summary>
        [Description("Specific leaf area [m^2/kg DM]:")]
        [Units("m^2/kg")]
        public double SpecificLeafArea { get; set; } = 20.0;

        /// <summary>Specific root length [m/g DM]</summary>
        [Description("Specific root length [m/g DM]:")]
        [Units("m/g")]
        public double SpecificRootLength { get; set; } = 75.0;


        // Turnover and senescence  -----------------------------------------------------------------------------------

        /// <summary>Daily DM turnover rate for shoot tissue [0-1]</summary>
        [Description("Daily DM turnover rate for shoot tissue  [0-1]:")]
        [Units("0-1")]
        public double TissueTurnoverRateShoot { get; set; } = 0.025;

        /// <summary>Daily detachment rate for DM dead [0-1]</summary>
        [Description("Daily detachment rate for DM dead [0-1]:")]
        [Units("0-1")]
        public double DetachmentRate { get; set; } = 0.11;

        /// <summary>Daily DM turnover rate for root tissue [0-1]</summary>
        [Description("Daily DM turnover rate for root tissue [0-1]")]
        [Units("0-1")]
        public double TissueTurnoverRateRoot { get; set; } = 0.02;

        /// <summary>Minimum temperature for tissue turnover [oC]</summary>
        [Description("Minimum temperature for tissue turnover [oC]:")]
        [Units("oC")]
        public double TissueTurnoverTmin { get; set; } = 2.0;

        /// <summary>Optimum temperature for tissue turnover [oC]</summary>
        [Description("Optimum temperature for tissue turnover [oC]:")]
        [Units("oC")]
        public double TissueTurnoverTopt { get; set; } = 20.0;

        /// <summary>Maximum increase in tissue turnover due to water stress</summary>
        [Description("Maximum increase in tissue turnover due to water stress:")]
        [Units("-")]
        public double TissueTurnoverWFactorMax { get; set; } = 2.0;

        /// <summary>Optimum value GLFwater for tissue turnover [0-1], below this value tissue turnover increases</summary>
        [Description("Optimum value GLFwater for tissue turnover [0-1]")]
        [Units("0-1")]
        public double TissueTurnoverGLFWopt { get; set; } = 0.5;

        /// <summary>Stock factor for increasing tissue turnover rate</summary>
        [XmlIgnore]
        [Units("-")]
        public double StockParameter { get; set; } = 0.05;

        /// <summary>Fraction of luxury N in tissue 2 available for remobilisation [0-1]</summary>
        [Description("Fraction of luxury N in tissue 2 available for remobilisation [0-1]:")]
        [Units("0-1")]
        public double KappaNRemob2 { get; set; } = 0.0;

        /// <summary>Fraction of luxury N in tissue 3 available for remobilisation [0-1]</summary>
        [Description("Fraction of luxury N in tissue 3 available for remobilisation [0-1]:")]
        [Units("0-1")]
        public double KappaNRemob3 { get; set; } = 0.0;

        /// <summary>Fraction of non-utilised remobilised N that is returned to dead material [0-1]</summary>
        [Description("Fraction of non-utilised remobilised N that is returned to dead material [0-1]:")]
        [Units("0-1")]
        public double KappaNRemob4 { get; set; } = 0.0;

        /// <summary>Fraction of senescent DM that is remobilised (as carbohydrate) [0-1]</summary>
        [XmlIgnore]
        [Units("0-1")]
        public double KappaCRemob { get; set; } = 0.0;

        /// <summary>Fraction of senescent DM (protein) that is remobilised to new growth [0-1]</summary>
        [XmlIgnore]
        [Units("0-1")]
        public double FacCNRemob { get; set; } = 0.0;

        // - N concentration  -----------------------------------------------------------------------------------------

        /// <summary>N concentration thresholds for leaves (optimum, minimum and maximum) [g/g]</summary>
        [Description("N concentration thresholds for leaves (optimum, minimum and maximum) [g/g]:")]
        [Units("g/g")]
        public double[] NThresholdsForLeaves { get; set; } = { 0.040, 0.012, 0.050 };

        /// <summary>N concentration thresholds for stems (optimum, minimum and maximum) [g/g]</summary>
        [Description("N concentration thresholds for stems (optimum, minimum and maximum) [g/g:]")]
        [Units("g/g")]
        public double[] NThresholdsForStems { get; set; } = { 0.020, 0.006, 0.025 };

        /// <summary>N concentration thresholds for stolons (optimum, minimum and maximum) [g/g]</summary>
        [Description("N concentration thresholds for stolons (optimum, minimum and maximum) [g/g:]")]
        [Units("g/g")]
        public double[] NThresholdsForStolons { get; set; } = { 0.0, 0.0, 0.0 };

        /// <summary>N concentration thresholds for roots (optimum, minimum and maximum) [g/g]</summary>
        [Description("N concentration thresholds for roots (optimum, minimum and maximum) [g/g:]")]
        [Units("g/g")]
        public double[] NThresholdsForRoots { get; set; } = { 0.020, 0.006, 0.025 };

        /// <summary>Concentration of N in tissues at stage 2 relative to stage 1 [0-1]</summary>
        /// <value>The relative n stage2.</value>
        [Description("Concentration of N in tissues at stage 2 relative to stage 1 [0-1]:")]
        [Units("0-1")]
        public double RelativeNStage2 { get; set; } = 1.0;

        /// <summary>Concentration of N in tissues at stage 3 relative to stage 1 [0-1]</summary>
        /// <value>The relative n stage3.</value>
        [Description("Concentration of N in tissues at stage 3 relative to stage 1 [0-1]:")]
        [Units("0-1")]
        public double RelativeNStage3 { get; set; } = 1.0;

        // - N fixation  ----------------------------------------------------------------------------------------------

        /// <summary>Minimum fraction of N demand supplied by biologic N fixation [0-1]</summary>
        [Description("Minimum fraction of N demand supplied by biologic N fixation [0-1]:")]
        [Units("0-1")]
        public double MinimumNFixation { get; set; } = 0.0;

        /// <summary>Maximum fraction of N demand supplied by biologic N fixation [0-1]</summary>
        [Description("Maximum fraction of N demand supplied by biologic N fixation [0-1]:")]
        [Units("0-1")]
        public double MaximumNFixation { get; set; } = 0.0;

        // - Growth limiting factors  ---------------------------------------------------------------------------------

        /// <summary>Curve parameter for the effect of N deficiency on plant growth.</summary>
        [Description("Curve parameter for the effect of N deficiency on plant growth:")]
        [Units("-")]
        public double DillutionCoefN { get; set; } = 0.5;

        /// <summary>Exponent factor for the water stress function</summary>
        [Description("Exponent factor for the water stress function:")]
        [Units("-")]
        public double WaterStressExponent { get; set; } = 1.0;

        /// <summary>Maximum reduction in plant growth due to water logging (saturated soil) [0-1]</summary>
        [Description("Maximum reduction in plant growth due to water logging (saturated soil) [0-1]:")]
        [Units("0-1")]
        public double WaterLoggingCoefficient { get; set; } = 0.1;

        /// <summary>Minimum water-free pore space for growth with no limitations [%]</summary>
        [Description("Minimum water-free pore space for growth with no limitations [%]:")]
        [Units("%")]
        public double MinimumWaterFreePorosity { get; set; } = 10.0;

        /// <summary>Gets or sets a generic growth factor, represents any arbitrary limitation to potential growth.</summary>
        /// <remarks> This factor can be used to describe the effects of conditions such as disease, etc.</remarks>
        [Description("Generic factor affecting potential plant growth [0-1]:")]
        [Units("0-1")]
        public double GlfGeneric { get; set; } = 1.0;

        /// <summary>Gets or sets a generic growth limiting factor, represents an arbitrary soil limitation.</summary>
        /// <remarks> This factor can be used to describe the effect of limitation in nutrients other than N.</remarks>
        [Description("Generic limiting factor due to soil fertilisty [0-1]:")]
        [Units("0-1")]
        public double GlfSFertility { get; set; } = 1.0;

        // - Digestibility and feed quality  --------------------------------------------------------------------------

        /// <summary>Digestibility of cell walls in plant tissues, by age (growing, developed, mature and dead) [0-1]</summary>
        [Description("Digestibility of cell wall in plant tissues, by age (growing, developed, mature and dead) [0-1]:")]
        [Units("0-1")]
        public double[] DigestibilitiesCellWall { get; set; } = { 0.6, 0.6, 0.6, 0.2 };

        /// <summary>Digestibility of protein in plant tissues, by age (growing, developed, mature and dead) [0-1]</summary>
        [Description("Digestibility of protein in plant tissues, by age (growing, developed, mature and dead) [0-1]:")]
        [Units("0-1")]
        public double[] DigestibilitiesProtein { get; set; } = { 1.0, 1.0, 1.0, 1.0 };

        /// <summary>Soluble fraction of carbohydrates in plant tissues, by age (growing, developed, mature and dead) [0-1]</summary>
        [Description("Fraction of new growth that is soluble carbohydrates in each plant organ (leaf, stem, stolon, root) [0-1]:")]
        [Units("0-1")]
        public double[] SugarFraction { get; set; } = { 0.5, 0.5, 0.5, 0.0 };

        // - Minimum DM and preferences when harvesting  --------------------------------------------------------------

        /// <summary>Minimum above ground green DM [kg DM/ha]</summary>
        [Description("Minimum above ground green DM [kg DM/ha]:")]
        [Units("kg/ha")]
        public double MinimumGreenWt { get; set; } = 300.0;

        /// <summary>Relative preference for live over dead material during graze</summary>
        [Description("Relative preference for live over dead material during graze:")]
        [Units("-")]
        public double RelativePreferenceForGreen { get; set; } = 1.0;

        /// <summary>Relative preference for leaf over stem material during graze</summary>
        [Description("Relative preference for leaf over stem material during graze:")]
        [Units("-")]
        public double RelativePreferenceForLeaf { get; set; } = 1.0;

        // - Plant height  --------------------------------------------------------------------------------------------

        /// <summary>Minimum shoot height [mm]</summary>
        [Description("Minimum shoot height [mm]:")]
        [Units("mm")]
        public double MinimumPlantHeight { get; set; } = 25.0;

        /// <summary>Maximum shoot height [mm]</summary>
        [Description("Maximum shoot height [mm]:")]
        [Units("mm")]
        public double MaximumPlantHeight { get; set; } = 600.0;

        /// <summary>Exponent of shoot height funtion [>1.0]</summary>
        [Description("Exponent of shoot height funtion [>1.0]:")]
        [Units(">1.0")]
        public double ExponentHeightFromMass { get; set; } = 2.8;

        /// <summary>DM weight for maximum shoot height[g/m2]</summary>
        [Description("DM weight for maximum shoot height[g/m2]:")]
        [Units("g/m2")]
        public double MassForMaximumHeight { get; set; } = 1000;

        // - Root distribution and height  ----------------------------------------------------------------------------

        /// <summary>Minimum rooting depth, at emergence [mm]</summary>
        [Description("Minimum rooting depth, at emergence [mm]:")]
        [Units("mm")]
        public double MinimumRootDepth { get; set; }

        /// <summary>Maximum rooting depth [mm]</summary>
        [Description("Maximum rooting depth [mm]:")]
        [Units("mm")]
        public double MaximumRootDepth { get; set; } = 750.0;

        /// <summary>Daily root elongation rate at optimum temperature [mm/day]</summary>
        [Description("Daily root elongation rate at optimum temperature [mm/day]:")]
        [Units("mm/day")]
        public double RootElongationRate { get; set; }

        /// <summary>Depth for constant distribution of roots, proportion decreases below this value [mm]</summary>
        [Description("Depth for constant distribution of roots [mm]:")]
        [Units("mm")]
        public double DepthForConstantRootProportion { get; set; } = 90.0;

        /// <summary>Coefficient for the root distribution, exponent to reduce proportion as function of depth [-]</summary>
        [Description("Coefficient for the root distribution [-]:")]
        [Units("-")]
        public double ExponentRootDistribution { get; set; } = 3.2;

        /// <summary>Factor to compute root distribution (where below maxRootDepth the function is zero)</summary>
        private double rootBottomDistributionFactor = 1.05;

        // - Annual species  ------------------------------------------------------------------------------------------

        /// <summary>The day of year when seeds are allowed to germinate</summary>
        private int doyGermination = 200;

        /// <summary>The number of days from emergence to anthesis</summary>
        private int daysEmergenceToAnthesis = 0;

        /// <summary>The number of days from anthesis to maturity</summary>
        private int daysAnthesisToMaturity = 0;

        /// <summary>The cumulative degrees-day from emergence to anthesis</summary>
        private double degreesDayForAnthesis = 0.0;

        /// <summary>The cumulative degrees-day from anthesis to maturity</summary>
        private double degreesDayForMaturity = 0.0;

        // - Other parameters  ----------------------------------------------------------------------------------------

        /// <summary>Broken stick type function describing how plant height varies with DM</summary>
        [XmlIgnore]
        public BrokenStick HeightFromMass = new BrokenStick
        {
            X = new double[5] {0, 1000, 2000, 3000, 4000},
            Y = new double[5] {0, 25, 75, 150, 250}
        };

        /// <summary>The FVPD function</summary>
        [XmlIgnore]
        public BrokenStick FVPDFunction = new BrokenStick
        {
            X = new double[3] {0.0, 10.0, 50.0},
            Y = new double[3] {1.0, 1.0, 1.0}
        };

        /// <summary>Flag which module will perform the water uptake process</summary>
        internal string myWaterUptakeSource = "species";

        /// <summary>Flag whether the alternative water uptake process will be used</summary>
        internal string useAltWUptake = "no";

        /// <summary>Reference value of Ksat for water availability function</summary>
        internal double ReferenceKSuptake = 1000.0;

        /// <summary>Flag which module will perform the nitrogen uptake process</summary>
        internal string myNitrogenUptakeSource = "species";

        /// <summary>Flag whether the alternative nitrogen uptake process will be used</summary>
        internal string useAltNUptake = "no";

        /// <summary>Availability factor for NH4</summary>
        internal double kuNH4 = 0.50;

        /// <summary>Availability factor for NO3</summary>
        internal double kuNO3 = 0.95;

        /// <summary>Reference value for root length density fot the Water and N availability</summary>
        internal double ReferenceRLD = 2.0;

        /// <summary>the local value for KNO3</summary>
        private double myKNO3 = 1.0;

        /// <summary>The value for the nitrate uptake coefficient</summary>
        [Description("Nitrate uptake coefficient")]
        public double KNO3
        {
            get { return myKNO3; }
            set { myKNO3 = value; }
        }

        /// <summary>the local value for KNH4</summary>
        private double myKNH4 = 1.0;

        /// <summary>The value for the ammonium uptake coefficient</summary>
        [Description("Ammonium uptake coefficient")]
        public double KNH4
        {
            get { return myKNH4; }
            set { myKNH4 = value; }
        }

        #endregion

        #region Private variables  -----------------------------------------------------------------------------------------

        /// <summary>Flag for whether several routines are ran by species or are controlled by the Sward</summary>
        internal bool isSwardControlled = false;

        /// <summary>Flag for whether this species is alive (activelly growing)</summary>
        private bool isAlive = true;

        /// <summary>State of leaves (DM and N)</summary>
        internal GenericAboveGroundOrgan leaves;

        /// <summary>State of sheath/stems (DM and N)</summary>
        internal GenericAboveGroundOrgan stems;

        /// <summary>State of stolons (DM and N)</summary>
        internal GenericAboveGroundOrgan stolons;

        /// <summary>State of roots (DM and N)</summary>
        internal GenericBelowGroundOrgan roots;

        /// <summary>Basic state variables for this plant (to be used for reset)</summary>
        private SpeciesBasicStateSettings InitialState;

        /// <summary>State for this plant on the previous day</summary>
        private SpeciesStateParameters myPreviousState;

        // Defining the plant type  -----------------------------------------------------------------------------------

        /// <summary>Flag this species type, annual or perennial</summary>
        private bool isAnnual = false;

        /// <summary>Flag whether this species is a legume</summary>
        private bool isLegume = false;

        // Annual species adn phenology  ------------------------------------------------------------------------------

        /// <summary>The phenologic stage</summary>
        /// <remarks>0 = germinating, 1 = vegetative, 2 = reproductive, negative for dormant/not sown</remarks>
        private int phenologicStage = -1;

        /// <summary>The number of days since emergence</summary>
        private int daysSinceEmergence = 0;

        /// <summary>The number of days from emergence</summary>
        private int daysfromEmergence = 0;

        /// <summary>The number of days from anthesis</summary>
        private int daysfromAnthesis = 0;

        /// <summary>The cumulatve degrees day during vegetative phase</summary>
        private double growingGDD = 0.0;

        /// <summary>The factor for biomass senescence according to phase</summary>
        private double phenoFactor = 0.0;

        // Photosynthesis, growth, and turnover  ----------------------------------------------------------------------

        /// <summary>The intercepted solar radiation</summary>
        public double InterceptedRadn;

        /// <summary>The irradiance on top of canopy</summary>
        private double irradianceTopOfCanopy;

        /// <summary>The gross photosynthesis rate (C assimilation)</summary>
        private double Pgross = 0.0;

        /// <summary>The growth respiration rate (C loss)</summary>
        private double Resp_g = 0.0;

        /// <summary>The maintenance respiration rate (C loss)</summary>
        private double Resp_m = 0.0;

        /// <summary>The amount of C remobilised from senesced tissue</summary>
        private double CRemobilisable = 0.0;

        /// <summary>Daily net growth potential (kgDM/ha)</summary>
        private double dGrowthPot;

        /// <summary>Daily potential growth after water stress</summary>
        private double dGrowthWstress;

        /// <summary>Daily growth after nutrient stress (actual growth)</summary>
        private double dGrowthActual;

        /// <summary>Effective growth of roots</summary>
        private double dGrowthRoot;

        /// <summary>Effective growth of shoot (herbage growth)</summary>
        private double dGrowthShoot;

        /// <summary>Effective plant growth (actual growth minus senescence)</summary>
        private double dGrowthEff;

        /// <summary>Daily litter production (dead to surface OM)</summary>
        private double dLitter;

        /// <summary>N amount in litter procuded</summary>
        private double dNlitter;

        /// <summary>Daily root sennesce (added to soil FOM)</summary>
        private double dRootSen;

        /// <summary>N amount in senesced roots</summary>
        private double dNrootSen;

        /// <summary>Fraction of growth allocated to shoot (0-1)</summary>
        private double fShoot;

        /// <summary>The daily DM turnover rate (from tissue 1 to 2, then to 3, then to 4)</summary>
        private double gama = 0.0;

        /// <summary>The daily DM turnover rate for stolons</summary>
        private double gamaS = 0.0; // for stolons

        /// <summary>The daily DM turnover rate for dead tissue (from tissue 4 to litter)</summary>
        private double gamaD = 0.0;

        /// <summary>The daily DM turnover rate for roots</summary>
        private double gamaR = 0.0;

        /// <summary>The cumulative degrees-day during germination phase</summary>
        private double germinationGDD = 0.0;

        // Plant height, LAI and cover  -------------------------------------------------------------------------------

        /// <summary>The plant's green LAI</summary>
        private double greenLAI;

        /// <summary>The plant's dead LAI</summary>
        private double deadLAI;

        // Root depth and distribution --------------------------------------------------------------------------------

        /// <summary>The daily variation in root depth</summary>
        private double dRootDepth = 50;

        /// <summary>The maximum shoot-root ratio</summary>
        private double maxSRratio;

        // Amounts and fluxes of N in the plant  ----------------------------------------------------------------------

        /// <summary>The N demand for new growth, with luxury uptake</summary>
        private double NdemandLux;

        /// <summary>The N demand for new growth, at optimum N content</summary>
        private double NdemandOpt;

        /// <summary>The amount of N fixation from atmosphere (for legumes)</summary>
        internal double Nfixation = 0.0;

        /// <summary>The amount of N remobilised from senesced tissue</summary>
        private double NRemobilised = 0.0;

        /// <summary>The amount of N remobilisable from senesced tissue</summary>
        private double NRemobilisable = 0.0;

        /// <summary>The amount of N being remobilised from senesced tissue</summary>
        private double NRemobilising = 0.0;

        /// <summary>The amount of N actually remobilised to new growth</summary>
        private double Nremob2NewGrowth = 0.0;

        /// <summary>The amount of N used in new growth</summary>
        internal double newGrowthN = 0.0;

        /// <summary>The aount of luxury N (above Nopt) in tissue 2 potentially remobilisable</summary>
        private double NLuxury2;

        /// <summary>The amount of luxury N (above Nopt) in tissue 3 potentially remobilisable</summary>
        private double NLuxury3;

        /// <summary>The amount of luxury N actually remobilised from tissue 2</summary>
        private double NFastRemob2 = 0.0;

        /// <summary>The amount of luxury N actually remobilised from tissue 3</summary>
        private double NFastRemob3 = 0.0;

        // N uptake process  ------------------------------------------------------------------------------------------

        /// <summary>The amount of N demanded from the soil</summary>
        private double mySoilNDemand;

        /// <summary>The amount of N in the soil available to the plant</summary>
        internal double[] mySoilAvailableN;

        /// <summary>The amount of NH4 in the soil available to the plant</summary>
        internal double[] mySoilNH4available;

        /// <summary>The amount of NO3 in the soil available to the plant</summary>
        internal double[] mySoilNO3available;

        /// <summary>The amount of N actually taken up</summary>
        internal double mySoilNuptake;

        /// <summary>The amount of potential NH4_N uptake from each soil layer</summary>
        internal double[] myPotentialNH4NUptake;

        /// <summary>The amount of potential NO3_N uptake from each soil layer</summary>
        internal double[] myPotentialNO3NUptake;

        /// <summary>The amount of N uptake from each soil layer</summary>
        internal double[] mySoilNitrogenTakenUp;

        // water uptake process  --------------------------------------------------------------------------------------

        /// <summary>The amount of water demanded for new growth</summary>
        internal double myWaterDemand = 0.0;

        /// <summary>The amount of soil available water</summary>
        private double[] mySoilAvailableWater;

        /// <summary>The amount of soil water taken up</summary>
        internal double[] mySoilWaterTakenUp;

        // growth limiting factors ------------------------------------------------------------------------------------

        /// <summary>The growth factor due to variations in intercepted radiation</summary>
        private double glfRadn = 1.0;

        /// <summary>The growth factor due to N variations in atmospheric CO2</summary>
        private double glfCO2 = 1.0;

        /// <summary>The growth factor due to variations in plant N concentration</summary>
        private double glfNc = 1.0;

        /// <summary>The growth factor due to variations in air temperature</summary>
        private double glfTemp = 1.0;

        /// <summary>The growth factor due to heat stress</summary>
        private double glfHeat = 1.0;
        
        /// <summary>The growth factor due to cold stress</summary>
        private double glfCold = 1.0;

        /// <summary>The growth limiting factor due to water stress</summary>
        internal double glfWater = 1.0;

        /// <summary>The growth limiting factor due to water logging</summary>
        internal double glfAeration = 1.0;

        /// <summary>The growth limiting factor due to N stress</summary>
        internal double glfN = 0.0;

        // Auxiliary variables for temperature stress  ----------------------------------------------------------------

        /// <summary>Growth rate reduction factor due to high temperatures</summary>
        private double highTempEffect = 1.0;

        /// <summary>Growth rate reduction factor due to low temperatures</summary>
        private double lowTempEffect = 1.0;

        /// <summary>Cumulative degress of temperature for recovery from heat damage</summary>
        private double accumT4Heat = 0.0;

        /// <summary>Cumulative degress of temperature for recovry from cold damage</summary>
        private double accumT4Cold = 0.0;

        // Harvest and digestibility  ---------------------------------------------------------------------------------

        /// <summary>The DM amount harvested (defoliated)</summary>
        private double dmDefoliated = 0.0;

        /// <summary>The N amount harvested (defoliated)</summary>
        private double Ndefoliated = 0.0;

        /// <summary>The digestibility of herbage</summary>
        private double digestHerbage = 0.0;

        /// <summary>The digestibility of defoliated material</summary>
        private double digestDefoliated = 0.0;

        /// <summary>The fraction of standing DM harvested</summary>
        internal double fractionHarvested = 0.0;

        // general auxiliary variables  -------------------------------------------------------------------------------

        /// <summary>Number of layers in the soil</summary>
        private int nLayers = 0;

        #endregion

        #region Constants and auxiliary  -----------------------------------------------------------------------------------

        /// <summary>Average carbon content in plant dry matter</summary>
        const double CarbonFractionInDM = 0.4;

        /// <summary>Factor for converting nitrogen to protein</summary>
        const double NitrogenToProteinFactor = 6.25;

        /// <summary>The C:N ratio of protein</summary>
        const double CNratioProtein = 3.5;

        /// <summary>The C:N ratio of cell wall</summary>
        const double CNratioCellWall = 100.0;

        /// <summary>Maximum difference between two values of double precision in this model</summary>
        const double myEpsilon = 0.000000001;

        /// <summary>A yes or no answer</summary>
        private enum YesNoAnswer
        {
            /// <summary>a positive answer</summary>
            yes,

            /// <summary>a negative answer</summary>
            no
        }

        /// <summary>List of valid species family names</summary>
        public enum PlantFamilyType
        {
            /// <summary>A grass species, Poaceae</summary>
            Grass,

            /// <summary>A legume species, Fabaceae</summary>
            Legume,

            /// <summary>A non grass or legume species</summary>
            Forb
        }

        /// <summary>List of valid photosynthesis pathways</summary>
        public enum PhotosynthesisPathwayType
        {
            /// <summary>A C3 plant</summary>
            C3,

            /// <summary>A C4 plant</summary>
            C4
        }

        #endregion

        #region Model outputs  ---------------------------------------------------------------------------------------------

        /// <summary>
        /// Is the plant alive?
        /// </summary>
        public bool IsAlive
        {
            get { return PlantStatus == "alive"; }
        }

        /// <summary>Gets the plant status.</summary>
        /// <value>The plant status (dead, alive, etc).</value>
        [Description("Plant status (dead, alive, etc)")]
        [Units("")]
        public string PlantStatus
        {
            get
            {
                if (isAlive)
                    return "alive";
                else
                    return "out";
            }
        }

        /// <summary>Gets the index for the plant development stage.</summary>
        /// <value>The stage index.</value>
        [Description("Plant development stage number")]
        [Units("")]
        public int Stage
        {
            get
            {
                if (isAlive)
                {
                    if(phenologicStage < myEpsilon)
                        return 1; //"germination";
                    else
                        return 3; //"vegetative" & "reproductive";
                }
                else
                    return 0; //"out"
            }
        }

        /// <summary>Gets the name of the plant development stage.</summary>
        /// <value>The name of the stage.</value>
        [Description("Plant development stage name")]
        [Units("")]
        public string StageName
        {
            get
            {
                if (isAlive)
                {
                    if (phenologicStage == 0)
                        return "germination";
                    else
                        return "vegetative";
                }
                else
                    return "out";
            }
        }

        #region - DM and C amounts  ----------------------------------------------------------------------------------------

        /// <summary>Gets the total plant C content.</summary>
        /// <value>The plant C content.</value>
        [Description("Total amount of C in plants")]
        [Units("kgDM/ha")]
        public double TotalC
        {
            get { return TotalWt * CarbonFractionInDM; }
        }

        /// <summary>Gets the plant total dry matter weight.</summary>
        /// <value>The total DM weight.</value>
        [Description("Total plant dry matter weight")]
        [Units("kgDM/ha")]
        public double TotalWt
        {
            get { return AboveGroundWt + BelowGroundWt; }
        }

        /// <summary>Gets the plant DM weight above ground.</summary>
        /// <value>The above ground DM weight.</value>
        [Description("Dry matter weight above ground")]
        [Units("kgDM/ha")]
        public double AboveGroundWt
        {
            get { return AboveGroundLivedWt + AboveGroundDeadWt; }
        }

        /// <summary>Gets the DM weight of live plant parts above ground.</summary>
        /// <value>The above ground DM weight of live plant parts.</value>
        [Description("Dry matter weight of alive plants above ground")]
        [Units("kgDM/ha")]
        public double AboveGroundLivedWt
        {
            get { return leaves.DMGreen + stems.DMGreen + stolons.DMGreen; }
        }

        /// <summary>Gets the DM weight of dead plant parts above ground.</summary>
        /// <value>The above ground dead DM weight.</value>
        [Description("Dry matter weight of dead plants above ground")]
        [Units("kgDM/ha")]
        public double AboveGroundDeadWt
        {
            get { return leaves.DMDead + stems.DMDead + stolons.DMDead; }
        }

        /// <summary>Gets the DM weight of the plant below ground.</summary>
        /// <value>The below ground DM weight of plant.</value>
        [Description("Dry matter weight below ground")]
        [Units("kgDM/ha")]
        public double BelowGroundWt
        {
            get { return roots.DMTotal; }
        }

        /// <summary>Gets the total standing DM weight.</summary>
        /// <value>The DM weight of leaves and stems.</value>
        [Description("Dry matter weight of standing herbage")]
        [Units("kgDM/ha")]
        public double StandingWt
        {
            get { return leaves.DMTotal + stems.DMTotal; }
        }

        /// <summary>Gets the DM weight of standing live plant material.</summary>
        /// <value>The DM weight of live leaves and stems.</value>
        [Description("Dry matter weight of live standing plants parts")]
        [Units("kgDM/ha")]
        public double StandingLiveWt
        {
            get { return leaves.DMGreen + stems.DMGreen; }
        }

        /// <summary>Gets the DM weight of standing dead plant material.</summary>
        /// <value>The DM weight of dead leaves and stems.</value>
        [Description("Dry matter weight of dead standing plants parts")]
        [Units("kgDM/ha")]
        public double StandingDeadWt
        {
            get { return leaves.DMDead + stems.DMDead; }
        }

        /// <summary>Gets the total DM weight of leaves.</summary>
        /// <value>The leaf DM weight.</value>
        [Description("Dry matter weight of leaves")]
        [Units("kgDM/ha")]
        public double LeafWt
        {
            get { return leaves.DMTotal; }
        }

        /// <summary>Gets the DM weight of green leaves.</summary>
        /// <value>The green leaf DM weight.</value>
        [Description("Dry matter weight of live leaves")]
        [Units("kgDM/ha")]
        public double LeafGreenWt
        {
            get { return leaves.DMGreen; }
        }

        /// <summary>Gets the DM weight of dead leaves.</summary>
        /// <value>The dead leaf DM weight.</value>
        [Description("Dry matter weight of dead leaves")]
        [Units("kgDM/ha")]
        public double LeafDeadWt
        {
            get { return leaves.DMDead; }
        }

        /// <summary>Gets the toal DM weight of stems and sheath.</summary>
        /// <value>The stem DM weight.</value>
        [Description("Dry matter weight of stems and sheath")]
        [Units("kgDM/ha")]
        public double StemWt
        {
            get { return stems.DMTotal; }
        }

        /// <summary>Gets the DM weight of live stems and sheath.</summary>
        /// <value>The live stems DM weight.</value>
        [Description("Dry matter weight of alive stems and sheath")]
        [Units("kgDM/ha")]
        public double StemGreenWt
        {
            get { return stems.DMGreen; }
        }

        /// <summary>Gets the DM weight of dead stems and sheath.</summary>
        /// <value>The dead stems DM weight.</value>
        [Description("Dry matter weight of dead stems and sheath")]
        [Units("kgDM/ha")]
        public double StemDeadWt
        {
            get { return stems.DMDead; }
        }

        /// <summary>Gets the total DM weight od stolons.</summary>
        /// <value>The stolon DM weight.</value>
        [Description("Dry matter weight of stolons")]
        [Units("kgDM/ha")]
        public double StolonWt
        {
            get { return stolons.DMTotal; }
        }

        /// <summary>Gets the total DM weight of roots.</summary>
        /// <value>The root DM weight.</value>
        [Description("Dry matter weight of roots")]
        [Units("kgDM/ha")]
        public double RootWt
        {
            get { return roots.DMTotal; }
        }

        /// <summary>Gets the DM weight of roots for each layer.</summary>
        /// <value>The root DM weight.</value>
        [Description("Dry matter weight of roots")]
        [Units("kgDM/ha")]
        public double[] RootLayerWt
        {
            get { return roots.Tissue[0].DMLayer; }
        }

        /// <summary>Gets the DM weight of leaves at stage1 (developing).</summary>
        /// <value>The stage1 leaf DM weight.</value>
        [Description("Dry matter weight of leaves at stage 1 (developing)")]
        [Units("kgDM/ha")]
        public double LeafStage1Wt
        {
            get { return leaves.Tissue[0].DM; }
        }

        /// <summary>Gets the DM weight of leaves stage2 (mature).</summary>
        /// <value>The stage2 leaf DM weight.</value>
        [Description("Dry matter weight of leaves at stage 2 (mature)")]
        [Units("kgDM/ha")]
        public double LeafStage2Wt
        {
            get { return leaves.Tissue[1].DM; }
        }

        /// <summary>Gets the DM weight of leaves at stage3 (senescing).</summary>
        /// <value>The stage3 leaf DM weight.</value>
        [Description("Dry matter weight of leaves at stage 3 (senescing)")]
        [Units("kgDM/ha")]
        public double LeafStage3Wt
        {
            get { return leaves.Tissue[2].DM; }
        }

        /// <summary>Gets the DM weight of leaves at stage4 (dead).</summary>
        /// <value>The stage4 leaf DM weight.</value>
        [Description("Dry matter weight of leaves at stage 4 (dead)")]
        [Units("kgDM/ha")]
        public double LeafStage4Wt
        {
            get { return leaves.Tissue[3].DM; }
        }

        /// <summary>Gets the DM weight stems and sheath at stage1 (developing).</summary>
        /// <value>The stage1 stems DM weight.</value>
        [Description("Dry matter weight of stems at stage 1 (developing)")]
        [Units("kgDM/ha")]
        public double StemStage1Wt
        {
            get { return stems.Tissue[0].DM; }
        }

        /// <summary>Gets the DM weight of stems and sheath at stage2 (mature).</summary>
        /// <value>The stage2 stems DM weight.</value>
        [Description("Dry matter weight of stems at stage 2 (mature)")]
        [Units("kgDM/ha")]
        public double StemStage2Wt
        {
            get { return stems.Tissue[1].DM; }
        }

        /// <summary>Gets the DM weight of stems and sheath at stage3 (senescing)).</summary>
        /// <value>The stage3 stems DM weight.</value>
        [Description("Dry matter weight of stems at stage 3 (senescing)")]
        [Units("kgDM/ha")]
        public double StemStage3Wt
        {
            get { return stems.Tissue[2].DM; }
        }

        /// <summary>Gets the DM weight of stems and sheath at stage4 (dead).</summary>
        /// <value>The stage4 stems DM weight.</value>
        [Description("Dry matter weight of stems at stage 4 (dead)")]
        [Units("kgDM/ha")]
        public double StemStage4Wt
        {
            get { return stems.Tissue[3].DM; }
        }

        /// <summary>Gets the DM weight of stolons at stage1 (developing).</summary>
        /// <value>The stage1 stolon DM weight.</value>
        [Description("Dry matter weight of stolons at stage 1 (developing)")]
        [Units("kgDM/ha")]
        public double StolonStage1Wt
        {
            get { return stolons.Tissue[0].DM; }
        }

        /// <summary>Gets the DM weight of stolons at stage2 (mature).</summary>
        /// <value>The stage2 stolon DM weight.</value>
        [Description("Dry matter weight of stolons at stage 2 (mature)")]
        [Units("kgDM/ha")]
        public double StolonStage2Wt
        {
            get { return stolons.Tissue[1].DM; }
        }

        /// <summary>Gets the DM weight of stolons at stage3 (senescing).</summary>
        /// <value>The stage3 stolon DM weight.</value>
        [Description("Dry matter weight of stolons at stage 3 (senescing)")]
        [Units("kgDM/ha")]
        public double StolonStage3Wt
        {
            get { return stolons.Tissue[2].DM; }
        }

        #endregion

        #region - C and DM flows  ------------------------------------------------------------------------------------------

        /// <summary>Gets the potential carbon assimilation.</summary>
        /// <value>The potential carbon assimilation.</value>
        [Description("Potential C assimilation, corrected for extreme temperatures")]
        [Units("kgC/ha")]
        public double PotCarbonAssimilation
        {
            get { return Pgross; }
        }

        /// <summary>Gets the carbon loss via respiration.</summary>
        /// <value>The carbon loss via respiration.</value>
        [Description("Loss of C via respiration")]
        [Units("kgC/ha")]
        public double CarbonLossRespiration
        {
            get { return Resp_m; }
        }

        /// <summary>Gets the carbon remobilised from senescent tissue.</summary>
        /// <value>The carbon remobilised.</value>
        [Description("C remobilised from senescent tissue")]
        [Units("kgC/ha")]
        public double CarbonRemobilisable
        {
            get { return CRemobilisable; }
        }

        /// <summary>Gets the gross potential growth rate.</summary>
        /// <value>The potential C assimilation, in DM equivalent.</value>
        [Description("Gross potential growth rate (potential C assimilation)")]
        [Units("kgDM/ha")]
        public double GrossPotentialGrowthWt
        {
            get { return Pgross / CarbonFractionInDM; }
        }

        /// <summary>Gets the respiration rate.</summary>
        /// <value>The loss of C due to respiration, in DM equivalent.</value>
        [Description("Respiration rate (DM lost via respiration)")]
        [Units("kgDM/ha")]
        public double RespirationWt
        {
            get { return Resp_m / CarbonFractionInDM; }
        }

        /// <summary>Gets the remobilisation rate.</summary>
        /// <value>The C remobilised, in DM equivalent.</value>
        [Description("C remobilisation (DM remobilised from old tissue to new growth)")]
        [Units("kgDM/ha")]
        public double RemobilisationWt
        {
            get { return CRemobilisable / CarbonFractionInDM; }
        }

        /// <summary>Gets the net potential growth rate.</summary>
        /// <value>The net potential growth rate.</value>
        [Description("Net potential growth rate")]
        [Units("kgDM/ha")]
        public double NetPotentialGrowthWt
        {
            get { return dGrowthPot; }
        }

        /// <summary>Gets the potential growth rate after water stress.</summary>
        /// <value>The potential growth after water stress.</value>
        [Description("Potential growth rate after water stress")]
        [Units("kgDM/ha")]
        public double PotGrowthWt_Wstress
        {
            get { return dGrowthWstress; }
        }

        /// <summary>Gets the actual growth rate.</summary>
        /// <value>The actual growth rate.</value>
        [Description("Actual growth rate, after nutrient stress")]
        [Units("kgDM/ha")]
        public double ActualGrowthWt
        {
            get { return dGrowthActual; }
        }

        /// <summary>Gets the effective growth rate.</summary>
        /// <value>The effective growth rate.</value>
        [Description("Effective growth rate, after turnover")]
        [Units("kgDM/ha")]
        public double EffectiveGrowthWt
        {
            get { return dGrowthEff; }
        }

        /// <summary>Gets the effective herbage growth rate.</summary>
        /// <value>The herbage growth rate.</value>
        [Description("Effective herbage growth rate, above ground")]
        [Units("kgDM/ha")]
        public double HerbageGrowthWt
        {
            get { return dGrowthShoot; }
        }

        /// <summary>Gets the effective root growth rate.</summary>
        /// <value>The root growth DM weight.</value>
        [Description("Effective root growth rate")]
        [Units("kgDM/ha")]
        public double RootGrowthWt
        {
            get { return dGrowthRoot - dRootSen; }
        }

        /// <summary>Gets the litter DM weight deposited onto soil surface.</summary>
        /// <value>The litter DM weight deposited.</value>
        [Description("Litter amount deposited onto soil surface")]
        [Units("kgDM/ha")]
        public double LitterWt
        {
            get { return dLitter; }
        }

        /// <summary>Gets the senesced root DM weight.</summary>
        /// <value>The senesced root DM weight.</value>
        [Description("Amount of senesced roots added to soil FOM")]
        [Units("kgDM/ha")]
        public double RootSenescedWt
        {
            get { return dRootSen; }
        }

        /// <summary>Gets the gross primary productivity.</summary>
        /// <value>The gross primary productivity.</value>
        [Description("Gross primary productivity")]
        [Units("kgDM/ha")]
        public double GPP
        {
            get { return Pgross / CarbonFractionInDM; }
        }

        /// <summary>Gets the net primary productivity.</summary>
        /// <value>The net primary productivity.</value>
        [Description("Net primary productivity")]
        [Units("kgDM/ha")]
        public double NPP
        {
            get { return (Pgross * (1 - GrowthRespirationCoefficient) - Resp_m) / CarbonFractionInDM; }
        }

        /// <summary>Gets the net above-ground primary productivity.</summary>
        /// <value>The net above-ground primary productivity.</value>
        [Description("Net above-ground primary productivity")]
        [Units("kgDM/ha")]
        public double NAPP
        {
            get { return (Pgross * (1 - GrowthRespirationCoefficient) - Resp_m) * fShoot / CarbonFractionInDM; }
        }

        /// <summary>Gets the net below-ground primary productivity.</summary>
        /// <value>The net below-ground primary productivity.</value>
        [Description("Net below-ground primary productivity")]
        [Units("kgDM/ha")]
        public double NBPP
        {
            get { return (Pgross * (1 - GrowthRespirationCoefficient) - Resp_m) * (1 - fShoot) / CarbonFractionInDM; }
        }

        #endregion

        #region - N amounts  -----------------------------------------------------------------------------------------------

        /// <summary>Gets the plant total N content.</summary>
        /// <value>The total N content.</value>
        [Description("Total plant N amount")]
        [Units("kgN/ha")]
        public double TotalN
        {
            get { return AboveGroundN + BelowGroundN; }
        }

        /// <summary>Gets the N content in the plant above ground.</summary>
        /// <value>The above ground N content.</value>
        [Description("N amount of plant parts above ground")]
        [Units("kgN/ha")]
        public double AboveGroundN
        {
            get { return AboveGroundLiveN + AboveGroundDeadN; }
        }

        /// <summary>Gets the N content in live plant material above ground.</summary>
        /// <value>The N content above ground of live plants.</value>
        [Description("N amount of alive plant parts above ground")]
        [Units("kgN/ha")]
        public double AboveGroundLiveN
        {
            get { return leaves.NGreen + stems.NGreen + stolons.NGreen; }
        }

        /// <summary>Gets the N content of dead plant material above ground.</summary>
        /// <value>The N content above ground of dead plants.</value>
        [Description("N amount of dead plant parts above ground")]
        [Units("kgN/ha")]
        public double AboveGroundDeadN
        {
            get { return leaves.NDead + stems.NDead + stolons.NDead; }
        }

        /// <summary>Gets the N content of plants below ground.</summary>
        /// <value>The below ground N content.</value>
        [Description("N amount of plant parts below ground")]
        [Units("kgN/ha")]
        public double BelowGroundN
        {
            get { return roots.NTotal; }
        }

        /// <summary>Gets the N content of standing plants.</summary>
        /// <value>The N content of leaves and stems.</value>
        [Description("N amount of standing herbage")]
        [Units("kgN/ha")]
        public double StandingN
        {
            get { return leaves.NTotal + stems.NTotal; }
        }

        /// <summary>Gets the N content of standing live plant material.</summary>
        /// <value>The N content of live leaves and stems.</value>
        [Description("N amount of alive standing herbage")]
        [Units("kgN/ha")]
        public double StandingLiveN
        {
            get { return leaves.NGreen + stems.NGreen; }
        }

        /// <summary>Gets the N content  of standing dead plant material.</summary>
        /// <value>The N content of dead leaves and stems.</value>
        [Description("N amount of dead standing herbage")]
        [Units("kgN/ha")]
        public double StandingDeadN
        {
            get { return leaves.NDead + stems.NDead; }
        }

        /// <summary>Gets the total N content of leaves.</summary>
        /// <value>The leaf N content.</value>
        [Description("N amount in the plant's leaves")]
        [Units("kgN/ha")]
        public double LeafN
        {
            get { return leaves.NTotal; }
        }

        /// <summary>Gets the total N content of stems and sheath.</summary>
        /// <value>The stem N content.</value>
        [Description("N amount in the plant's stems")]
        [Units("kgN/ha")]
        public double StemN
        {
            get { return stems.NTotal; }
        }

        /// <summary>Gets the total N content of stolons.</summary>
        /// <value>The stolon N content.</value>
        [Description("N amount in the plant's stolons")]
        [Units("kgN/ha")]
        public double StolonN
        {
            get { return stolons.NTotal; }
        }

        /// <summary>Gets the total N content of roots.</summary>
        /// <value>The root N content.</value>
        [Description("N amount in the plant's roots")]
        [Units("kgN/ha")]
        public double RootN
        {
            get { return roots.NTotal; }
        }

        /// <summary>Gets the N content of green leaves.</summary>
        /// <value>The green leaf N content.</value>
        [Description("N amount in alive leaves")]
        [Units("kgN/ha")]
        public double LeafGreenN
        {
            get { return leaves.NGreen; }
        }

        /// <summary>Gets the N content of dead leaves.</summary>
        /// <value>The dead leaf N content.</value>
        [Description("N amount in dead leaves")]
        [Units("kgN/ha")]
        public double LeafDeadN
        {
            get { return leaves.NDead; }
        }

        /// <summary>Gets the N content of green stems and sheath.</summary>
        /// <value>The green stem N content.</value>
        [Description("N amount in alive stems")]
        [Units("kgN/ha")]
        public double StemGreenN
        {
            get { return stems.NGreen; }
        }

        /// <summary>Gets the N content  of dead stems and sheath.</summary>
        /// <value>The dead stem N content.</value>
        [Description("N amount in dead sytems")]
        [Units("kgN/ha")]
        public double StemDeadN
        {
            get { return stems.NDead; }
        }

        /// <summary>Gets the N content of leaves at stage1 (developing).</summary>
        /// <value>The stage1 leaf N.</value>
        [Description("N amount in leaves at stage 1 (developing)")]
        [Units("kgN/ha")]
        public double LeafStage1N
        {
            get { return leaves.Tissue[0].Namount; }
        }

        /// <summary>Gets the N content of leaves at stage2 (mature).</summary>
        /// <value>The stage2 leaf N.</value>
        [Description("N amount in leaves at stage 2 (mature)")]
        [Units("kgN/ha")]
        public double LeafStage2N
        {
            get { return leaves.Tissue[1].Namount; }
        }

        /// <summary>Gets the N content of leaves at stage3 (senescing).</summary>
        /// <value>The stage3 leaf N.</value>
        [Description("N amount in leaves at stage 3 (senescing)")]
        [Units("kgN/ha")]
        public double LeafStage3N
        {
            get { return leaves.Tissue[2].Namount; }
        }

        /// <summary>Gets the N content of leaves at stage4 (dead).</summary>
        /// <value>The stage4 leaf N.</value>
        [Description("N amount in leaves at stage 4 (dead)")]
        [Units("kgN/ha")]
        public double LeafStage4N
        {
            get { return leaves.Tissue[3].Namount; }
        }

        /// <summary>Gets the N content of stems and sheath at stage1 (developing).</summary>
        /// <value>The stage1 stem N.</value>
        [Description("N amount in stems at stage 1 (developing)")]
        [Units("kgN/ha")]
        public double StemStage1N
        {
            get { return stems.Tissue[0].Namount; }
        }

        /// <summary>Gets the N content of stems and sheath at stage2 (mature).</summary>
        /// <value>The stage2 stem N.</value>
        [Description("N amount in stems at stage 2 (mature)")]
        [Units("kgN/ha")]
        public double StemStage2N
        {
            get { return stems.Tissue[1].Namount; }
        }

        /// <summary>Gets the N content of stems and sheath at stage3 (senescing).</summary>
        /// <value>The stage3 stem N.</value>
        [Description("N amount in stems at stage 3 (senescing)")]
        [Units("kgN/ha")]
        public double StemStage3N
        {
            get { return stems.Tissue[2].Namount; }
        }

        /// <summary>Gets the N content of stems and sheath at stage4 (dead).</summary>
        /// <value>The stage4 stem N.</value>
        [Description("N amount in stems at stage 4 (dead)")]
        [Units("kgN/ha")]
        public double StemStage4N
        {
            get { return stems.Tissue[3].Namount; }
        }

        /// <summary>Gets the N content of stolons at stage1 (developing).</summary>
        /// <value>The stage1 stolon N.</value>
        [Description("N amount in stolons at stage 1 (developing)")]
        [Units("kgN/ha")]
        public double StolonStage1N
        {
            get { return stolons.Tissue[0].Namount; }
        }

        /// <summary>Gets the N content of stolons at stage2 (mature).</summary>
        /// <value>The stage2 stolon N.</value>
        [Description("N amount in stolons at stage 2 (mature)")]
        [Units("kgN/ha")]
        public double StolonStage2N
        {
            get { return stolons.Tissue[1].Namount; }
        }

        /// <summary>Gets the N content of stolons as stage3 (senescing).</summary>
        /// <value>The stolon stage3 n.</value>
        [Description("N amount in stolons at stage 3 (senescing)")]
        [Units("kgN/ha")]
        public double StolonStage3N
        {
            get { return stolons.Tissue[2].Namount; }
        }

        #endregion

        #region - N concentrations  ----------------------------------------------------------------------------------------

        /// <summary>Gets the average N concentration of standing plant material.</summary>
        /// <value>The average N concentration of leaves and stems.</value>
        [Description("Average N concentration in standing plant parts")]
        [Units("kgN/kgDM")]
        public double StandingNConc
        {
            get { return MathUtilities.Divide(StandingN, StandingWt, 0.0); }
        }

        /// <summary>Gets the average N concentration of leaves.</summary>
        /// <value>The leaf N concentration.</value>
        [Description("Average N concentration in leaves")]
        [Units("kgN/kgDM")]
        public double LeafNConc
        {
            get { return MathUtilities.Divide(LeafN, LeafWt, 0.0); }
        }

        /// <summary>Gets the average N concentration of stems and sheath.</summary>
        /// <value>The stem N concentration.</value>
        [Description("Average N concentration in stems")]
        [Units("kgN/kgDM")]
        public double StemNConc
        {
            get { return MathUtilities.Divide(StemN, StemWt, 0.0); }
        }

        /// <summary>Gets the average N concentration of stolons.</summary>
        /// <value>The stolon N concentration.</value>
        [Description("Average N concentration in stolons")]
        [Units("kgN/kgDM")]
        public double StolonNConc
        {
            get { return MathUtilities.Divide(StolonN, StolonWt, 0.0); }
        }

        /// <summary>Gets the average N concentration of roots.</summary>
        /// <value>The root N concentration.</value>
        [Description("Average N concentration in roots")]
        [Units("kgN/kgDM")]
        public double RootNConc
        {
            get { return MathUtilities.Divide(RootN, RootWt, 0.0); }
        }

        /// <summary>Gets the N concentration of leaves at stage1 (developing).</summary>
        /// <value>The stage1 leaf N concentration.</value>
        [Description("N concentration of leaves at stage 1 (developing)")]
        [Units("kgN/kgDM")]
        public double LeafStage1NConc
        {
            get { return leaves.Tissue[0].Nconc; }
        }

        /// <summary>Gets the N concentration of leaves at stage2 (mature).</summary>
        /// <value>The stage2 leaf N concentration.</value>
        [Description("N concentration of leaves at stage 2 (mature)")]
        [Units("kgN/kgDM")]
        public double LeafStage2NConc
        {
            get { return leaves.Tissue[1].Nconc; }
        }

        /// <summary>Gets the N concentration of leaves at stage3 (senescing).</summary>
        /// <value>The stage3 leaf N concentration.</value>
        [Description("N concentration of leaves at stage 3 (senescing)")]
        [Units("kgN/kgDM")]
        public double LeafStage3NConc
        {
            get { return leaves.Tissue[2].Nconc; }
        }

        /// <summary>Gets the N concentration of leaves at stage4 (dead).</summary>
        /// <value>The stage4 leaf N concentration.</value>
        [Description("N concentration of leaves at stage 4 (dead)")]
        [Units("kgN/kgDM")]
        public double LeafStage4NConc
        {
            get { return leaves.Tissue[3].Nconc; }
        }

        /// <summary>Gets the N concentration of stems at stage1 (developing).</summary>
        /// <value>The stage1 stem N concentration.</value>
        [Description("N concentration of stems at stage 1 (developing)")]
        [Units("kgN/kgDM")]
        public double StemStage1NConc
        {
            get { return stems.Tissue[0].Nconc; }
        }

        /// <summary>Gets the N concentration of stems at stage2 (mature).</summary>
        /// <value>The stage2 stem N concentration.</value>
        [Description("N concentration of stems at stage 2 (mature)")]
        [Units("kgN/kgDM")]
        public double StemStage2NConc
        {
            get { return stems.Tissue[1].Nconc; }
        }

        /// <summary>Gets the N concentration of stems at stage3 (senescing).</summary>
        /// <value>The stage3 stem N concentration.</value>
        [Description("N concentration of stems at stage 3 (senescing)")]
        [Units("kgN/kgDM")]
        public double StemStage3NConc
        {
            get { return stems.Tissue[2].Nconc; }
        }

        /// <summary>Gets the N concentration of stems at stage4 (dead).</summary>
        /// <value>The stage4 stem N concentration.</value>
        [Description("N concentration of stems at stage 4 (dead)")]
        [Units("kgN/kgDM")]
        public double StemStage4NConc
        {
            get { return stems.Tissue[3].Nconc; }
        }

        /// <summary>Gets the N concentration of stolons at stage1 (developing).</summary>
        /// <value>The stage1 stolon N concentration.</value>
        [Description("N concentration of stolons at stage 1 (developing)")]
        [Units("kgN/kgDM")]
        public double StolonStage1NConc
        {
            get { return stolons.Tissue[0].Nconc; }
        }

        /// <summary>Gets the N concentration of stolons at stage2 (mature).</summary>
        /// <value>The stage2 stolon N concentration.</value>
        [Description("N concentration of stolons at stage 2 (mature)")]
        [Units("kgN/kgDM")]
        public double StolonStage2NConc
        {
            get { return stolons.Tissue[1].Nconc; }
        }

        /// <summary>Gets the N concentration of stolons at stage3 (senescing).</summary>
        /// <value>The stage3 stolon N concentration.</value>
        [Description("N concentration of stolons at stage 3 (senescing)")]
        [Units("kgN/kgDM")]
        public double StolonStage3NConc
        {
            get { return stolons.Tissue[2].Nconc; }
        }

        /// <summary>Gets the N concentration in new grown tissue.</summary>
        /// <value>The actual growth N concentration.</value>
        [Description("Concentration of N in new growth")]
        [Units("kgN/kgDM")]
        public double ActualGrowthNConc
        {
            get { return MathUtilities.Divide(newGrowthN, dGrowthActual, 0.0); }
        }

        #endregion

        #region - N flows  -------------------------------------------------------------------------------------------------

        /// <summary>Gets amount of N remobilisable from senesced tissue.</summary>
        /// <value>The remobilisable N amount.</value>
        [Description("Amount of N remobilisable from senesced material")]
        [Units("kgN/ha")]
        public double RemobilisableN
        {
            get { return NRemobilisable; }
        }

        /// <summary>Gets the amount of N remobilised from senesced tissue.</summary>
        /// <value>The remobilised N amount.</value>
        [Description("Amount of N remobilised from senesced material")]
        [Units("kgN/ha")]
        public double RemobilisedN
        {
            get { return Nremob2NewGrowth; }
        }

        /// <summary>Gets the amount of luxury N potentially remobilisable.</summary>
        /// <value>The remobilisable luxury N amount.</value>
        [Description("Amount of luxury N potentially remobilisable")]
        [Units("kgN/ha")]
        public double RemobilisableLuxuryN
        {
            get { return NLuxury2 + NLuxury3; }
        }

        /// <summary>Gets the amount of luxury N remobilised.</summary>
        /// <value>The remobilised luxury N amount.</value>
        [Description("Amount of luxury N remobilised")]
        [Units("kgN/ha")]
        public double RemobilisedLuxuryN
        {
            get { return NFastRemob2 + NFastRemob3; }
        }

        /// <summary>Gets the amount of luxury N potentially remobilisable from tissue 2.</summary>
        /// <value>The remobilisable luxury N amoount.</value>
        [Description("Amount of luxury N potentially remobilisable from tissue 2")]
        [Units("kgN/ha")]
        public double RemobT2LuxuryN
        {
            get { return NLuxury2; }
        }

        /// <summary>Gets the amount of luxury N potentially remobilisable from tissue 3.</summary>
        /// <value>The remobilisable luxury N amount.</value>
        [Description("Amount of luxury N potentially remobilisable from tissue 3")]
        [Units("kgN/ha")]
        public double RemobT3LuxuryN
        {
            get { return NLuxury3; }
        }

        /// <summary>Gets the amount of atmospheric N fixed.</summary>
        /// <value>The fixed N amount.</value>
        [Description("Amount of atmospheric N fixed")]
        [Units("kgN/ha")]
        public double FixedN
        {
            get { return Nfixation; }
        }

        /// <summary>Gets the amount of N required with luxury uptake.</summary>
        /// <value>The required N with luxury.</value>
        [Description("Amount of N required with luxury uptake")]
        [Units("kgN/ha")]
        public double RequiredLuxuryN
        {
            get { return NdemandLux; }
        }

        /// <summary>Gets the amount of N required for optimum N content.</summary>
        /// <value>The required optimum N amount.</value>
        [Description("Amount of N required for optimum growth")]
        [Units("kgN/ha")]
        public double RequiredOptimumN
        {
            get { return NdemandOpt; }
        }

        /// <summary>Gets the amount of N demanded from soil.</summary>
        /// <value>The N demand from soil.</value>
        [Description("Amount of N demanded from soil")]
        [Units("kgN/ha")]
        public double DemandSoilN
        {
            get { return mySoilNDemand; }
        }

        /// <summary>Gets the amount of plant available N in the soil.</summary>
        /// <value>The soil available N.</value>
        [Description("Amount of N available in the soil")]
        [Units("kgN/ha")]
        public double[] SoilAvailableN
        {
            get { return mySoilAvailableN; }
        }

        /// <summary>Gets the amount of N taken up from soil.</summary>
        /// <value>The N uptake.</value>
        [Description("Amount of N uptake")]
        [Units("kgN/ha")]
        public double[] UptakeN
        {
            get { return mySoilNitrogenTakenUp; }
        }

        /// <summary>Gets the amount of N deposited as litter onto soil surface.</summary>
        /// <value>The litter N amount.</value>
        [Description("Amount of N deposited as litter onto soil surface")]
        [Units("kgN/ha")]
        public double LitterN
        {
            get { return dNlitter; }
        }

        /// <summary>Gets the amount of N from senesced roots added to soil FOM.</summary>
        /// <value>The senesced root N amount.</value>
        [Description("Amount of N from senesced roots added to soil FOM")]
        [Units("kgN/ha")]
        public double SenescedRootN
        {
            get { return dNrootSen; }
        }

        /// <summary>Gets the amount of N in new grown tissue.</summary>
        /// <value>The actual growth N amount.</value>
        [Description("Amount of N in new growth")]
        [Units("kgN/ha")]
        public double ActualGrowthN
        {
            get { return newGrowthN; }
        }

        #endregion

        #region - Turnover rates and DM allocation  ------------------------------------------------------------------------

        /// <summary>Gets the turnover rate for live DM (leaves, stems and sheath).</summary>
        /// <value>The turnover rate for live DM.</value>
        [Description("Turnover rate for live DM (leaves and stem)")]
        [Units("0-1")]
        public double LiveDMTurnoverRate
        {
            get { return gama; }
        }

        /// <summary>Gets the turnover rate for dead DM (leaves, stems and sheath).</summary>
        /// <value>The turnover rate for dead DM.</value>
        [Description("Turnover rate for dead DM (leaves and stem)")]
        [Units("0-1")]
        public double DeadDMTurnoverRate
        {
            get { return gamaD; }
        }

        /// <summary>Gets the turnover rate for live DM in stolons.</summary>
        /// <value>The turnover rate for stolon DM.</value>
        [Description("DM turnover rate for stolons")]
        [Units("0-1")]
        public double StolonDMTurnoverRate
        {
            get { return gamaS; }
        }

        /// <summary>Gets the turnover rate for live DM in roots.</summary>
        /// <value>The turnover rate for root DM.</value>
        [Description("DM turnover rate for roots")]
        [Units("0-1")]
        public double RootDMTurnoverRate
        {
            get { return gamaR; }
        }

        /// <summary>Gets the DM allocation to shoot.</summary>
        /// <value>The shoot DM allocation.</value>
        [Description("Fraction of DM allocated to Shoot")]
        [Units("0-1")]
        public double ShootDMAllocation
        {
            get { return fShoot; }
        }

        /// <summary>Gets the DM allocation to roots.</summary>
        /// <value>The root dm allocation.</value>
        [Description("Fraction of DM allocated to roots")]
        [Units("0-1")]
        public double RootDMAllocation
        {
            get { return 1 - fShoot; }
        }

        #endregion

        #region - LAI and cover  -------------------------------------------------------------------------------------------

        /// <summary>Gets the plant's green LAI (leaf area index).</summary>
        /// <value>The green LAI.</value>
        [Description("Leaf area index of green leaves")]
        [Units("m^2/m^2")]
        public double LAIGreen
        {
            get { return greenLAI; }
        }

        /// <summary>Gets the plant's dead LAI (leaf area index).</summary>
        /// <value>The dead LAI.</value>
        [Description("Leaf area index of dead leaves")]
        [Units("m^2/m^2")]
        public double LAIDead
        {
            get { return deadLAI; }
        }

        /// <summary>Gets the irradiance on top of canopy.</summary>
        /// <value>The irradiance on top of canopy.</value>
        [Description("Irridance on the top of canopy")]
        [Units("W.m^2/m^2")]
        public double IrradianceTopCanopy
        {
            get { return irradianceTopOfCanopy; }
        }

        /// <summary>Gets the plant's dead cover.</summary>
        /// <value>The dead cover.</value>
        [Description("Fraction of soil covered by dead leaves")]
        [Units("%")]
        public double CoverDead
        {
            get { return CalcPlantCover(deadLAI); }
        }

        #endregion

        #region - Root depth and distribution  -----------------------------------------------------------------------------

        /// <summary>Gets the root depth.</summary>
        /// <value>The root depth.</value>
        [Description("Depth of roots")]
        [Units("mm")]
        public double RootDepth
        {
            get { return roots.Depth; }
        }

        /// <summary>Gets the root frontier.</summary>
        /// <value>The layer at bottom of root zone.</value>
        [Description("Layer at bottom of root zone")]
        [Units("mm")]
        public double RootFrontier
        {
            get { return roots.BottomLayer; }
        }

        /// <summary>Gets the fraction of root dry matter for each soil layer.</summary>
        /// <value>The root fraction.</value>
        [Description("Fraction of root dry matter for each soil layer")]
        [Units("0-1")]
        public double[] RootWtFraction
        {
            get { return roots.Tissue[0].FractionWt; }
        }

        /// <summary>Gets the plant's root length density for each soil layer.</summary>
        /// <value>The root length density.</value>
        [Description("Root length density")]
        [Units("mm/mm^3")]
        public double[] RLD
        {
            get
            {
                double[] result = new double[nLayers];
                double totalRootLength = roots.Tissue[0].DM * SpecificRootLength; // m root/m2 
                totalRootLength *= 0.0000001; // convert into mm root/mm2 soil) - TODO: fix this when using DM in g/m2
                for (int layer = 0; layer < result.Length; layer++)
                {
                    result[layer] = roots.Tissue[0].FractionWt[layer] * totalRootLength / mySoil.Thickness[layer];
                }
                return result;
            }
        }

        #endregion

        #region - Water amounts  -------------------------------------------------------------------------------------------

        /// <summary>Gets the lower limit of soil water content for plant uptake.</summary>
        /// <value>The water uptake lower limit.</value>
        [Description("Lower limit of soil water content for plant uptake")]
        [Units("mm^3/mm^3")]
        public double[] LL
        {
            get
            {
                SoilCrop soilInfo = (SoilCrop)mySoil.Crop(Name);
                return soilInfo.LL;
            }
        }

        /// <summary>Gets the amount of water demanded by the plant.</summary>
        /// <value>The water demand.</value>
        [Description("Plant water demand")]
        [Units("mm")]
        public double WaterDemand
        {
            get { return myWaterDemand; }
        }

        /// <summary>Gets the amount of soil water available for uptake.</summary>
        /// <value>The soil available water.</value>
        [Description("Plant availabe water")]
        [Units("mm")]
        public double[] SoilAvailableWater
        {
            get { return mySoilAvailableWater; }
        }

        /// <summary>Gets the amount of water taken up by the plant.</summary>
        /// <value>The water uptake.</value>
        [Description("Plant water uptake")]
        [Units("mm")]
        public double[] WaterUptake
        {
            get { return mySoilWaterTakenUp; }
        }

        #endregion

        #region - Growth limiting factors  ---------------------------------------------------------------------------------

        /// <summary>Gets the growth factor due to variations in intercepted radiation.</summary>
        /// <value>A factor influencing potential plant growth.</value>
        [Description("Growth factor due to variations in intercepted radiation")]
        [Units("0-1")]
        public double GlfRadnIntercept
        {
            get { return glfRadn; }
        }

        /// <summary>Gets the growth limiting factor due to variations in atmospheric CO2.</summary>
        /// <value>A factor influencing potential plant growth.</value>
        [Description("Growth limiting factor due to variations in atmospheric CO2")]
        [Units("0-1")]
        public double GlfCO2
        {
            get { return glfCO2; }
        }

        /// <summary>Gets the growth limiting factor due to variations in plant N concentration.</summary>
        /// <value>A factor influencing potential plant growth.</value>
        [Description("Growth limiting factor due to variations in plant N concentration")]
        [Units("0-1")]
        public double GlfNContent
        {
            get { return glfNc; }
        }

        /// <summary>Gets the growth limiting factor due to variations in air temperature.</summary>
        /// <value>A factor influencing potential plant growth.</value>
        [Description("Growth limiting factor due to variations in air temperature")]
        [Units("0-1")]
        public double GlfTemperature
        {
            get { return glfTemp; }
        }

        /// <summary>Gets the growth limiting factor due to heat stress.</summary>
        /// <value>A factor influencing potential plant growth.</value>
        [Description("Growth limiting factor due to heat stress")]
        [Units("0-1")]
        public double GlfHeatDamage
        {
            get { return glfHeat; }
        }

        /// <summary>Gets the growth limiting factor due to cold stress.</summary>
        /// <value>A factor influencing potential plant growth.</value>
        [Description("Growth limiting factor due to cold stress")]
        [Units("0-1")]
        public double GlfColdDamage
        {
            get { return glfCold; }
        }

        /// <summary>Gets the growth limiting factor due to water deficit.</summary>
        /// <value>A factor limiting plant growth.</value>
        [Description("Growth limiting factor due to water deficit")]
        [Units("0-1")]
        public double GlfWaterSupply
        {
            get { return glfWater; }
        }

        /// <summary>Gets the growth limiting factor due to lack of soil aeration.</summary>
        /// <value>A factor limiting plant growth.</value>
        [Description("Growth limiting factor due to lack of soil aeration")]
        [Units("0-1")]
        public double GlfWaterLogging
        {
            get { return glfAeration; }
        }

        /// <summary>Gets the growth limiting factor due to soil N availability.</summary>
        /// <value>A factor limiting plant growth.</value>
        [Description("Growth limiting factor due to soil N availability")]
        [Units("0-1")]
        public double GlfNSupply
        {
            get { return glfN; }
        }

        // TODO: verify that this is really needed
        /// <summary>Gets the vapour pressure deficit factor.</summary>
        /// <value>The vapour pressure deficit factor.</value>
        [Description("Effect of vapour pressure on growth (used by micromet)")]
        [Units("0-1")]
        public double FVPD
        {
            get { return FVPDFunction.Value(VPD()); }
        }

        #endregion

        #region - Harvest variables  ---------------------------------------------------------------------------------------

        /// <summary>Gets the amount of dry matter harvestable (leaf + stem).</summary>
        /// <value>The harvestable DM weight.</value>
        [Description("Amount of dry matter harvestable (leaf+stem)")]
        [Units("kgDM/ha")]
        public double HarvestableWt
        {
            get { return Math.Max(0.0, StandingLiveWt - MinimumGreenWt) + StandingDeadWt; }
        }

        /// <summary>Gets the amount of dry matter harvested.</summary>
        /// <value>The harvested DM weight.</value>
        [Description("Amount of plant dry matter removed by harvest")]
        [Units("kgDM/ha")]
        public double HarvestedWt
        {
            get { return dmDefoliated; }
        }

        /// <summary>Gets the fraction of the plant that was harvested.</summary>
        /// <value>The fraction harvested.</value>
        [Description("Fraction harvested")]
        [Units("0-1")]
        public double HarvestedFraction
        {
            get { return fractionHarvested; }
        }

        /// <summary>Gets the amount of plant N removed by harvest.</summary>
        /// <value>The harvested N amount.</value>
        [Description("Amount of plant nitrogen removed by harvest")]
        [Units("kgN/ha")]
        public double HarvestedN
        {
            get { return Ndefoliated; }
        }

        /// <summary>Gets the N concentration in harvested DM.</summary>
        /// <value>The N concentration in harvested DM.</value>
        [Description("average N concentration of harvested material")]
        [Units("kgN/kgDM")]
        public double HarvestedNconc
        {
            get { return MathUtilities.Divide(HarvestedN, HarvestedWt, 0.0); }
        }

        /// <summary>Gets the average herbage digestibility.</summary>
        /// <value>The herbage digestibility.</value>
        [Description("Average digestibility of herbage")]
        [Units("0-1")]
        public double HerbageDigestibility
        {
            get { return digestHerbage; }
        }

        /// <summary>Gets the average digestibility of harvested DM.</summary>
        /// <value>The harvested digestibility.</value>
        [Description("Average digestibility of harvested meterial")]
        [Units("0-1")]
        public double HarvestedDigestibility
        {
            get { return digestDefoliated; }
        }

        /// <summary>Gets the average herbage ME (metabolisable energy).</summary>
        /// <value>The herbage ME.</value>
        [Description("Average ME of herbage")]
        [Units("(MJ/ha)")]
        public double HerbageME
        {
            get { return 16 * digestHerbage * AboveGroundWt; }
        }

        /// <summary>Gets the average ME (metabolisable energy) of harvested DM.</summary>
        /// <value>The harvested ME.</value>
        [Description("Average ME of harvested material")]
        [Units("(MJ/ha)")]
        public double HarvestedME
        {
            get { return 16 * digestDefoliated * HarvestedWt; }
        }

        #endregion

        #endregion

        #region Initialisation methods  ------------------------------------------------------------------------------------

        /// <summary>Performs the initialisation procedures for this species (set DM, N, LAI, etc)</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            // get the number of layers in the soil profile
            nLayers = mySoil.Thickness.Length;

            // set up the organs (use 4 or 2 tissues, the last is dead)
            leaves = new GenericAboveGroundOrgan(4);
            stems = new GenericAboveGroundOrgan(4);
            stolons = new GenericAboveGroundOrgan(4);
            roots = new GenericBelowGroundOrgan(2, nLayers);

            // initialise soil water and N variables
            InitiliaseSoilArrays();

            // Check and save initial state
            CheckInitialState();

            // set initial plant state
            SetInitialState();

            // initialise the class which will hold yesterday's plant state
            myPreviousState = new SpeciesStateParameters(nLayers);

            // check whether uptake is done here or by another module
            if (apsimArbitrator != null)
            {
                myWaterUptakeSource = "Arbitrator";
                myNitrogenUptakeSource = "Arbitrator";
            }

            // check whether uptake is done here or by another module
            if (soilArbitrator != null)
            {
                myWaterUptakeSource = "SoilArbitrator";
                myNitrogenUptakeSource = "SoilArbitrator";
            }
        }

        /// <summary>
        /// Initialise arrays to same length as soil layers
        /// </summary>
        private void InitiliaseSoilArrays()
        {
            mySoilAvailableWater = new double[nLayers];
            mySoilWaterTakenUp = new double[nLayers];
            mySoilNH4available = new double[nLayers];
            mySoilNO3available = new double[nLayers];
            mySoilAvailableN = new double[nLayers];
            mySoilNitrogenTakenUp = new double[nLayers];
        }

        /// <summary>
        /// Initialise, check, and save the varibles representing the initial plant state
        /// </summary>
        private void CheckInitialState()
        {
            // 1. Choose the appropriate DM partition, based on species family
            double[] initialDMFractions;
            if (mySpeciesFamily == PlantFamilyType.Grass)
                initialDMFractions = initialDMFractions_grass;
            else if (mySpeciesFamily == PlantFamilyType.Legume)
                initialDMFractions = initialDMFractions_legume;
            else
                initialDMFractions = initialDMFractions_forbs;

            // 2. Initialise N concentration thresholds (optimum, minimum, and maximum)
            leaves.NConcOptimum = NThresholdsForLeaves[0];
            leaves.NConcMinimum = NThresholdsForLeaves[1];
            leaves.NConcMaximum = NThresholdsForLeaves[2];

            stems.NConcOptimum = NThresholdsForStems[0];
            stems.NConcMinimum = NThresholdsForStems[1];
            stems.NConcMaximum = NThresholdsForStems[2];

            stolons.NConcOptimum = NThresholdsForStolons[0];
            stolons.NConcMinimum = NThresholdsForStolons[1];
            stolons.NConcMaximum = NThresholdsForStolons[2];

            roots.NConcOptimum = NThresholdsForRoots[0];
            roots.NConcMinimum = NThresholdsForRoots[1];
            roots.NConcMaximum = NThresholdsForRoots[2];

            // 3. Save initial state (may be used later for reset)
            InitialState = new SpeciesBasicStateSettings();
            if (InitialShootDM > myEpsilon)
            {
                // DM is positive, plant is on the ground and able to grow straightaway
                InitialState.PhenoStage = 1;
                for (int pool = 0; pool < 11; pool++)
                    InitialState.DMWeight[pool] = initialDMFractions[pool] * InitialShootDM;
                InitialState.DMWeight[11] = InitialRootDM;
                InitialState.RootDepth = InitialRootDepth;
                // assume N concentration is at optimum for green pools and minimum for dead pools
                InitialState.NAmount[0] = InitialState.DMWeight[0] * leaves.NConcOptimum;
                InitialState.NAmount[1] = InitialState.DMWeight[1] * leaves.NConcOptimum;
                InitialState.NAmount[2] = InitialState.DMWeight[2] * leaves.NConcOptimum;
                InitialState.NAmount[3] = InitialState.DMWeight[3] * leaves.NConcMinimum;
                InitialState.NAmount[4] = InitialState.DMWeight[4] * stems.NConcOptimum;
                InitialState.NAmount[5] = InitialState.DMWeight[5] * stems.NConcOptimum;
                InitialState.NAmount[6] = InitialState.DMWeight[6] * stems.NConcOptimum;
                InitialState.NAmount[7] = InitialState.DMWeight[7] * stems.NConcMinimum;
                InitialState.NAmount[8] = InitialState.DMWeight[8] * stolons.NConcOptimum;
                InitialState.NAmount[9] = InitialState.DMWeight[9] * stolons.NConcOptimum;
                InitialState.NAmount[10] = InitialState.DMWeight[10] * stolons.NConcOptimum;
                InitialState.NAmount[11] = InitialState.DMWeight[11] * roots.NConcOptimum;
            }
            else if (InitialShootDM > -myEpsilon)
            {
                // DM is zero, plant has just sown and is able to germinate
                InitialState.PhenoStage = 0;
            }
            else
            {
                //DM is negative, plant is not yet in the ground 
                InitialState.PhenoStage = -1;
            }

            // 3. Set the digestibility parameters for each tissue
            for (int tissue = 0; tissue < 4; tissue++)
            {
                leaves.Tissue[tissue].DigestibilityCellWall = DigestibilitiesCellWall[tissue];
                leaves.Tissue[tissue].DigestibilityProtein = DigestibilitiesProtein[tissue];

                stems.Tissue[tissue].DigestibilityCellWall = DigestibilitiesCellWall[tissue];
                stems.Tissue[tissue].DigestibilityProtein = DigestibilitiesProtein[tissue];

                if (tissue < 3)
                {
                    stolons.Tissue[tissue].DigestibilityCellWall = DigestibilitiesCellWall[tissue];
                    stolons.Tissue[tissue].DigestibilityProtein = DigestibilitiesProtein[tissue];
                }
            }

            leaves.SugarFraction = SugarFraction[0];
            stems.SugarFraction = SugarFraction[1];
            stolons.SugarFraction = SugarFraction[2];
            //roots are not considered for digestibility
        }

        /// <summary>
        /// Set the initial parameters for this plant, including DM and N content of various pools plus plant height and root depth
        /// </summary>
        private void SetInitialState()
        {
            // 1. Initialise DM of each tissue pool above-ground (initial values supplied by user)
            leaves.Tissue[0].DM = InitialState.DMWeight[0];
            leaves.Tissue[1].DM = InitialState.DMWeight[1];
            leaves.Tissue[2].DM = InitialState.DMWeight[2];
            leaves.Tissue[3].DM = InitialState.DMWeight[3];
            stems.Tissue[0].DM = InitialState.DMWeight[4];
            stems.Tissue[1].DM = InitialState.DMWeight[5];
            stems.Tissue[2].DM = InitialState.DMWeight[6];
            stems.Tissue[3].DM = InitialState.DMWeight[7];
            stolons.Tissue[0].DM = InitialState.DMWeight[8];
            stolons.Tissue[1].DM = InitialState.DMWeight[9];
            stolons.Tissue[2].DM = InitialState.DMWeight[10];

            // 2. Initialise root DM, N, depth, and distribution
            roots.Depth = InitialState.RootDepth;
            roots.BottomLayer = RootZoneBottomLayer();
            roots.TargetDistribution = RootDistributionTarget();
            double[] iniRootFraction = CurrentRootDistributionTarget();
            for (int layer = 0; layer < nLayers; layer++)
                roots.Tissue[0].DMLayer[layer] = InitialState.DMWeight[11] * iniRootFraction[layer];

            InitialiseRootsProperties();

            // 3. Initialise the N amounts in each pool above-ground (assume to be at optimum concentration)
            leaves.Tissue[0].Namount = InitialState.NAmount[0];
            leaves.Tissue[1].Namount = InitialState.NAmount[1];
            leaves.Tissue[2].Namount = InitialState.NAmount[2];
            leaves.Tissue[3].Namount = InitialState.NAmount[3];
            stems.Tissue[0].Namount = InitialState.NAmount[4];
            stems.Tissue[1].Namount = InitialState.NAmount[5];
            stems.Tissue[2].Namount = InitialState.NAmount[6];
            stems.Tissue[3].Namount = InitialState.NAmount[7];
            stolons.Tissue[0].Namount = InitialState.NAmount[8];
            stolons.Tissue[1].Namount = InitialState.NAmount[9];
            stolons.Tissue[2].Namount = InitialState.NAmount[10];
            roots.Tissue[0].Namount = InitialState.NAmount[11];

            // maximum shoot:root ratio
            maxSRratio = (1 - MaxRootAllocation) / MaxRootAllocation;

            // 4. Canopy height and related variables
            InitialiseCanopy();

            // 5. Set initial phenological stage
            phenologicStage = InitialState.PhenoStage;

            // 6. Calculate the values for LAI
            EvaluateLAI();
        }

        /// <summary>Set the plant state at germination</summary>
        internal void SetEmergenceState()
        {
            // 1. Set the above ground DM, equals MinimumGreenWt
            leaves.Tissue[0].DM = MinimumGreenWt * EmergenceDMFractions[0];
            leaves.Tissue[1].DM = MinimumGreenWt * EmergenceDMFractions[1];
            leaves.Tissue[2].DM = MinimumGreenWt * EmergenceDMFractions[2];
            leaves.Tissue[3].DM = MinimumGreenWt * EmergenceDMFractions[3];
            stems.Tissue[0].DM = MinimumGreenWt * EmergenceDMFractions[4];
            stems.Tissue[1].DM = MinimumGreenWt * EmergenceDMFractions[5];
            stems.Tissue[2].DM = MinimumGreenWt * EmergenceDMFractions[6];
            stems.Tissue[3].DM = MinimumGreenWt * EmergenceDMFractions[7];
            stolons.Tissue[0].DM = MinimumGreenWt * EmergenceDMFractions[8];
            stolons.Tissue[1].DM = MinimumGreenWt * EmergenceDMFractions[9];
            stolons.Tissue[2].DM = MinimumGreenWt * EmergenceDMFractions[10];

            // 2. Set root depth and DM (root DM equals shoot)
            roots.Depth = MinimumRootDepth;
            roots.BottomLayer = RootZoneBottomLayer();
            double[] rootFractions = CurrentRootDistributionTarget();            
            for (int layer = 0; layer < nLayers; layer++)
                roots.Tissue[0].DMLayer[layer] = MinimumGreenWt * rootFractions[layer];

            // 3. Set the N amounts in each plant part (assume to be at optimum)
            leaves.Tissue[0].Nconc = leaves.NConcOptimum;
            leaves.Tissue[1].Nconc = leaves.NConcOptimum;
            leaves.Tissue[2].Nconc = leaves.NConcOptimum;
            leaves.Tissue[3].Nconc = leaves.NConcOptimum;
            stems.Tissue[0].Nconc = stems.NConcOptimum;
            stems.Tissue[1].Nconc = stems.NConcOptimum;
            stems.Tissue[2].Nconc = stems.NConcOptimum;
            stems.Tissue[3].Nconc = stems.NConcOptimum;
            stolons.Tissue[0].Nconc = stolons.NConcOptimum;
            stolons.Tissue[1].Nconc = stolons.NConcOptimum;
            stolons.Tissue[2].Nconc = stolons.NConcOptimum;
            roots.Tissue[0].Nconc = roots.NConcOptimum;

            // 4. Set phenological stage to vegetative
            phenologicStage = 1;

            // 5. Calculate the values for LAI
            EvaluateLAI();
        }

        /// <summary>Initialise the variables in canopy properties</summary>
        private void InitialiseCanopy()
        {
            // Used in Val's Arbitrator (via ICrop2)
            myCanopyProperties.Name = Name;
            myCanopyProperties.CoverGreen = CoverGreen;
            myCanopyProperties.CoverTot = CoverTotal;
            myCanopyProperties.CanopyDepth = Height;
            myCanopyProperties.CanopyHeight = Height;
            myCanopyProperties.LAIGreen = LAIGreen;
            myCanopyProperties.LAItot = LAITotal;
            myCanopyProperties.MaximumStomatalConductance = myGsmax;
            myCanopyProperties.HalfSatStomatalConductance = myR50;
            myCanopyProperties.CanopyEmissivity = 0.96; // TODO: this should be on the UI (maybe)
            myCanopyProperties.Frgr = FRGR;
        }

        /// <summary>Initialise the variables in root properties</summary>
        private void InitialiseRootsProperties()
        {
            // Used in Val's Arbitrator (via ICrop2)
            SoilCrop soilCrop = this.mySoil.Crop(Name) as SoilCrop;

            myRootProperties.RootDepth = roots.Depth;
            myRootProperties.KL = soilCrop.KL;
            myRootProperties.MinNO3ConcForUptake = new double[mySoil.Thickness.Length];
            myRootProperties.MinNH4ConcForUptake = new double[mySoil.Thickness.Length];
            myRootProperties.KNO3 = myKNO3;
            myRootProperties.KNH4 = myKNH4;

            myRootProperties.LowerLimitDep = new double[mySoil.Thickness.Length];
            myRootProperties.UptakePreferenceByLayer = new double[mySoil.Thickness.Length];
            myRootProperties.RootExplorationByLayer = new double[mySoil.Thickness.Length];
            for (int layer = 0; layer < mySoil.Thickness.Length; layer++)
            {
                myRootProperties.LowerLimitDep[layer] = soilCrop.LL[layer] * mySoil.Thickness[layer];
                myRootProperties.MinNO3ConcForUptake[layer] = 0.0;
                myRootProperties.MinNH4ConcForUptake[layer] = 0.0;
                myRootProperties.UptakePreferenceByLayer[layer] = 1.0;
                myRootProperties.RootExplorationByLayer[layer] = FractionLayerWithRoots(layer);
            }
            myRootProperties.RootLengthDensityByVolume = RLD;
        }

        #endregion

        #region Daily processes  -------------------------------------------------------------------------------------------

        /// <summary>EventHandler - preparation befor the main process</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            // 1. Zero out several variables
            RefreshVariables();

            // N remobilisable today is what was computed yesterday
            NRemobilisable = NRemobilised;
        }

        /// <summary>Performs the plant growth calculations</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoPlantGrowth")]
        private void OnDoPlantGrowth(object sender, EventArgs e)
        {
            if (isAlive && !isSwardControlled)
            {
                // stores the current state for this species
                SaveState();

                // check phenology of annuals
                if (isAnnual)
                    phenologicStage = annualsPhenology();

                if (phenologicStage == 0)
                {
                    // plant has not emerged yet, check germination progress
                    if (DailyGerminationProgress() >= 1.0)
                    {
                        // germination completed
                        SetEmergenceState();
                    }
                }
                else
                {
                    //Plant has emerged, compute growth and uptake

                    // step 01 - Potential growth
                    CalcDailyPotentialGrowth();

                    // Water demand, supply, and uptake
                    DoWaterCalculations();

                    // step 02 - Potential growth after water limitations
                    CalcGrowthAfterWaterLimitations();

                    // Nitrogen demand, supply, and uptake
                    DoNitrogenCalculations();

                    // step 03 - Growth after nutrient limitations, but before senescence
                    CalcGrowthAfterNutrientLimitations();

                    // Partition new growth into various tissues
                    DoNewGrowthAllocations();

                    // Compute tissue turnover and remobilisation (C and N)
                    DoTissueTurnoverAndRemobilisation();

                    // step 04 - Effective growth, after all limitations and senescence
                    CalcEffectiveGrowth();

                    // Send amounts of litter and senesced roots to other modules
                    DoSurfaceOMReturn(dLitter, dNlitter);
                    DoIncorpFomEvent(dRootSen, dNrootSen);
                }
            }
            //else
            //    Growth is controlled by Sward (all species)
        }

        #region - Plant growth processes  ----------------------------------------------------------------------------------


        /// <summary>Evaluates the phenologic stage of annual plants</summary>
        /// <returns>An integer representing the plant's phenologic stage</returns>
        private int annualsPhenology()
        {
            int result = -1;
            double phenoFactor1;
            double phenoFactor2;

            // check whether germination started
            if (myClock.Today.DayOfYear == doyGermination)
            {
                // just allowed to germinate
                result = 0;
            }

            if (phenologicStage > 0)
            {
                // accumulate days count and degrees-day
                daysSinceEmergence += 1;
                growingGDD += Math.Max(0.0, Tmean(0.5) - GrowthTmin);

                // Note, germination is considered together with perennials in DailyGerminationProgress

                // check development over vegetative growth
                if ((daysSinceEmergence == daysEmergenceToAnthesis) || (growingGDD >= degreesDayForAnthesis))
                {
                    phenologicStage = 2;
                    growingGDD = Math.Max(growingGDD, degreesDayForAnthesis);
                }

                phenoFactor1 = MathUtilities.Divide(daysSinceEmergence, daysEmergenceToAnthesis, 1.0);
                phenoFactor2 = MathUtilities.Divide(growingGDD, degreesDayForAnthesis, 1.0);

                // check development over reproductive growth
                if (phenologicStage > 1)
                {
                    if ((daysSinceEmergence == daysEmergenceToAnthesis + daysAnthesisToMaturity) || (growingGDD >= degreesDayForMaturity))
                    {
                        growingGDD = Math.Max(growingGDD, degreesDayForMaturity);
                        EndCrop();
                    }

                    phenoFactor1 = MathUtilities.Divide(daysSinceEmergence- daysEmergenceToAnthesis, daysAnthesisToMaturity, 1.0);
                    phenoFactor2 = MathUtilities.Divide(growingGDD - degreesDayForAnthesis, degreesDayForMaturity, 1.0);
                }

                // get the phenologic factor (fraction of phase)
                phenoFactor = Math.Max(phenoFactor1, phenoFactor2);
            }

            return result;
        }

        /// <summary>Computation of daily progress through germination</summary>
        /// <returns>Fraction of germination phase completed</returns>
        internal double DailyGerminationProgress()
        {
            germinationGDD += Math.Max(0.0, Tmean(0.5) - GrowthTmin);
            return MathUtilities.Divide(germinationGDD, DegreesDayForGermination, 1.0);
        }

        /// <summary>Calculates the potential growth.</summary>
        internal void CalcDailyPotentialGrowth()
        {
            // Gross potential growth (kgC/ha/day)
            Pgross = DailyGrossPotentialGrowth();

            // Respiration (kgC/ha/day)
            Resp_m = DailyMaintenanceRespiration();
            Resp_g = DailyGrowthRespiration();

            // Remobilisation (kgC/ha/day) (got from previous day turnover)
            CRemobilisable = 0.0;

            // Net potential growth (kgDM/ha/day)
            dGrowthPot = DailyNetPotentialGrowth();
        }

        /// <summary>Calculates the growth with water limitations.</summary>
        internal void CalcGrowthAfterWaterLimitations()
        {
            // Potential growth after water limitations
            dGrowthWstress = dGrowthPot * Math.Pow(glfWater, WaterStressExponent);

            // allocation of todays growth
            fShoot = ToShootFraction();
            //   FL = UpdatefLeaf();
        }

        /// <summary>Calculates the actual plant growth (after limitations, before senescence).</summary>
        internal void CalcGrowthAfterNutrientLimitations()
        {
            // Adjust GLF due to N deficiency: Many plants, especially grasses, can keep growth even when N supply is
            //  insuficient, this is done by reducing the N concentration in the plant tissues. This is represented here
            //  by adjusting the effect of N deficiency using a power function. When the exponent is 1.0, the reduction
            //  in growth is linearly proportional to N deficiency, a greater value results in less reduction in growth.
            //  For many plants the value should be smaller than 1.0. For grasses, the exponent is typically around 0.5.
            double glfNit = Math.Pow(glfN, DillutionCoefN);

            // The generic limitation factor is assumed to be equivalent to a nutrient deficiency, so it is considered here
            dGrowthActual = dGrowthWstress * Math.Min(glfNit, GlfSFertility);
        }

        /// <summary>Calculates the plant effective growth (include changes in LAI and root depth).</summary>
        internal void CalcEffectiveGrowth()
        {
            // Effective, or net, growth
            dGrowthEff = dGrowthShoot + (dGrowthRoot - dRootSen);

            // update root depth
            EvaluateRootGrowth();

            // Update LAI
            EvaluateLAI();

            // Update digestibility
            EvaluateDigestibility();
        }

        /// <summary>Computes the plant's gross potential growth rate</summary>
        /// <returns>The potential amount of C assimilated via photosynthesis (kgC/ha)</returns>
        private double DailyGrossPotentialGrowth()
        {
            // CO2 effects on Pmax
            glfCO2 = PCO2Effects();

            // N concentration effects on Pmax
            glfNc = PmxNeffect();

            // Temperature effects to Pmax
            double tempGlf1 = TemperatureLimitingFactor(Tmean(0.5));
            double tempGlf2 = TemperatureLimitingFactor(Tmean(0.75));

            //Temperature growth factor (for reporting purposes only)
            glfTemp = (0.25 * tempGlf1) + (0.75 * tempGlf2);

            // Potential photosynthetic rate (mg CO2/m^2 leaf/s)
            //   at dawn and dusk (first and last quarter of the day)
            double Pmax1 = ReferencePhotosynthesisRate * tempGlf1 * glfCO2 * glfNc;
            //   at bright light (half of sunlight length, middle of day)
            double Pmax2 = ReferencePhotosynthesisRate * tempGlf2 * glfCO2 * glfNc;

            // Day light length, converted to seconds
            double myDayLength = 3600 * myMetData.CalculateDayLength(-6);

            // Photosynthetically active radiation, converted from MJ/m2.day to J/m2.s
            double interceptedPAR = fractionPAR * InterceptedRadn * 1000000.0 / myDayLength;

            // Irradiance at top of canopy in the middle of the day (J/m2 leaf/s)
            //irradianceTopOfCanopy = interceptedPAR * LightExtentionCoefficient * (4.0 / 3.0); TODO: enable this
            irradianceTopOfCanopy = interceptedPAR * LightExtentionCoefficient * 1.33333;

            //Photosynthesis per leaf area under full irradiance at the top of the canopy (mg CO2/m^2 leaf/s)
            double Pl1 = SingleLeafPhotosynthesis(0.5 * irradianceTopOfCanopy, Pmax1);
            double Pl2 = SingleLeafPhotosynthesis(irradianceTopOfCanopy, Pmax2);

            // Photosynthesis per leaf area for the day (mg CO2/m^2 leaf/day)
            double Pl_Daily = myDayLength * (Pl1 + Pl2) * 0.5;

            // Radiation effects (for reporting purposes only)
            glfRadn = MathUtilities.Divide((0.25 * Pl1) + (0.75 * Pl2), (0.25 * Pmax1) + (0.75 * Pmax2), 1.0);

            // Photosynthesis for whole canopy, per ground area (mg CO2/m^2/day)
            double Pc_Daily = Pl_Daily * CoverGreen / LightExtentionCoefficient;

            //  Carbon assimilation per leaf area (g C/m^2/day)
            double CarbonAssim = Pc_Daily * 0.001 * (12.0 / 44.0); // Convert from mgCO2 to gC           

            // Base gross photosynthesis, converted to kg C/ha/day
            double BaseGrossPhotosynthesis = CarbonAssim * 10; // convert from g/m2 to kg/ha (= 10000/1000)

            // Consider the extreme temperature effects (in practice only one temp stress factor is < 1)
            glfHeat = HeatStress();
            glfCold = ColdStress();

            // Actual gross photosynthesis (gross potential growth - kg C/ha/day)
            return BaseGrossPhotosynthesis * Math.Min(glfHeat, glfCold) * GlfGeneric;
        }

        /// <summary>
        /// Compute the photosynthetic rate for a single leaf
        /// </summary>
        /// <param name="IL">Instantaneous intercepted radiation (depends on time of day)</param>
        /// <param name="Pmax">Max photosynthetic rate, given T, CO2 and N concentration</param>
        /// <returns>the photosynthetic rate [mgCO2/m2 leaf/s]</returns>
        private double SingleLeafPhotosynthesis(double IL, double Pmax)
        {
            double photoAux1 = PhotosyntheticEfficiency * IL + Pmax;
            double photoAux2 = 4 * PhotosynthesisCurveFactor * PhotosyntheticEfficiency * IL * Pmax;
            double Pl = (0.5 / PhotosynthesisCurveFactor) * (photoAux1 - Math.Sqrt(Math.Pow(photoAux1, 2) - photoAux2));
            return Pl;
        }

        /// <summary>Computes the plant's loss of C due to maintenance respiration</summary>
        /// <returns>The amount of C lost to atmosphere (kgC/ha)</returns>
        private double DailyMaintenanceRespiration()
        {
            // Temperature effects on respiration
            double Teffect = 0;
            if (Tmean(0.5) > GrowthTmin)
            {
                if (Tmean(0.5) < GrowthTopt)
                {
                    Teffect = TemperatureLimitingFactor(Tmean(0.5));
                }
                else
                {
                    Teffect = Math.Min(1.25, Tmean(0.5) / GrowthTopt);
                    // Using growthTopt as reference temperature, and maximum of 1.25
                    Teffect *= TemperatureLimitingFactor(GrowthTopt);
                }
            }

            // Total DM converted to C (kg/ha)
            double liveBiomassC = (AboveGroundLivedWt + BelowGroundWt) * CarbonFractionInDM;
            double result = liveBiomassC * MaintenanceRespirationCoefficient * Teffect * glfNc;
            return Math.Max(0.0, result);
        }

        /// <summary>Computes the plant's loss of C due to growth respiration</summary>
        /// <returns>The amount of C lost to atmosphere (kgC/ha)</returns>
        private double DailyGrowthRespiration()
        {
            return Pgross * GrowthRespirationCoefficient;
        }

        /// <summary>Compute the plant's net potential growth</summary>
        /// <returns>The net potential growth (kg DM/ha)</returns>
        private double DailyNetPotentialGrowth()
        {
            // Net potential growth (C assimilation) for the day (excluding respiration)
            double NetPotGrowth = 0.0;
            NetPotGrowth = (1 - GrowthRespirationCoefficient) * (Pgross + CRemobilisable - Resp_m);
            // TODO: the respCoeff should only multiply Pgross
            //NetPotGrowth = Pgross + CRemobilised - Resp_g - Resp_m;
            NetPotGrowth = Math.Max(0.0, NetPotGrowth);

            // Net daily potential growth (kg DM/ha)
            NetPotGrowth /= CarbonFractionInDM;

            // phenologically related reduction in growth of annual species (from IJ)
            if (isAnnual)
                NetPotGrowth *= annualSpeciesReduction();

            return NetPotGrowth;
        }

        /// <summary>Update DM and N amounts allocating new growth to each organ</summary>
        /// <exception cref="System.Exception">
        /// Mass balance lost on partition of new growth DM
        /// or
        /// Mass balance lost on partition of new growth N
        /// </exception>
        internal void DoNewGrowthAllocations()
        {
            // TODO: implement fLeaf
            // Leaf appearance rate, as modified by temp & water stress  -  Not really used, should it??
            //double effTemp = TemperatureLimitingFactor(Tmean);
            //double effWater = Math.Pow(glfWater, 0.33333);
            //double rateLeafGrowth = leafRate * effTemp * effWater;
            //rateLeafGrowth = Math.Max(0.0, Math.Min(1.0, rateLeafGrowth));

            if (dGrowthActual > 0.0)
            {
                // Fractions of new growth for each plant part (fShoot was calculated in DoPlantGrowth)
                double toLeaf = fShoot * FracToLeaf;
                double toStem = fShoot * (1.0 - FracToStolon - FracToLeaf);
                double toStolon = fShoot * FracToStolon;
                double toRoot = 1.0 - fShoot;

                // Checking mass balance
                double ToAll = toLeaf + toStolon + toStem + toRoot;
                if (Math.Abs(ToAll - 1.0) > 0.0001)
                    throw new Exception("Mass balance lost on partition of new growth DM");

                // New growth is allocated to the first tissue pools
                leaves.Tissue[0].DM += toLeaf * dGrowthActual;
                stems.Tissue[0].DM += toStem * dGrowthActual;
                stolons.Tissue[0].DM += toStolon * dGrowthActual;
                //roots.Tissue[0].DM += Done later in EvaluateRootGrowth
                dGrowthShoot = (toLeaf + toStem + toStolon) * dGrowthActual;
                dGrowthRoot = toRoot * dGrowthActual;

                // Set the amount of sugar in each organ (all in tissue1)
                leaves.Tissue[0].DMSugar = leaves.SugarFraction * toLeaf * dGrowthActual;
                stems.Tissue[0].DMSugar = stems.SugarFraction * toStem * dGrowthActual;
                stolons.Tissue[0].DMSugar = stolons.SugarFraction * toStolon * dGrowthActual;

                // Partitioning N based on DM fractions and on max [N] in plant parts
                double Nsum = (toLeaf * leaves.NConcMaximum) + (toStem * stems.NConcMaximum)
                            + (toStolon * stolons.NConcMaximum) + (toRoot * roots.NConcMaximum);
                double toLeafN = toLeaf * MathUtilities.Divide(leaves.NConcMaximum, Nsum, 0.0);
                double toStemN = toStem * MathUtilities.Divide(stems.NConcMaximum, Nsum, 0.0);
                double toStolonN = toStolon * MathUtilities.Divide(stolons.NConcMaximum, Nsum, 0.0);
                double toRootN = toRoot * MathUtilities.Divide(roots.NConcMaximum, Nsum, 0.0);

                // Checking mass balance
                ToAll = toRootN + toLeafN + toStolonN + toStemN;
                if (Math.Abs(ToAll - 1.0) > 0.0001)
                    throw new Exception("Mass balance lost on partition of new growth N");

                // Allocate N from new growth to the first tissue pools
                leaves.Tissue[0].Namount += toLeafN * newGrowthN;
                stems.Tissue[0].Namount += toStemN * newGrowthN;
                stolons.Tissue[0].Namount += toStolonN * newGrowthN;
                roots.Tissue[0].Namount += toRootN * newGrowthN;

                // Fraction of Nremob not used in new growth that is returned (or kept) to dead tissue
                double leftoverNremob = NRemobilising * KappaNRemob4;
                Nsum = myPreviousState.leaves.Tissue[3].Namount + myPreviousState.stems.Tissue[3].Namount;
                if ((leftoverNremob > 0.0) && (Nsum > 0.0))
                {
                    leaves.Tissue[3].Namount += leftoverNremob * MathUtilities.Divide(myPreviousState.leaves.Tissue[3].Namount, Nsum, 0.0);
                    stems.Tissue[3].Namount += leftoverNremob * MathUtilities.Divide(myPreviousState.stems.Tissue[3].Namount, Nsum, 0.0);
                    NRemobilising -= leftoverNremob;
                    // Note: this is only valid for leaf and stems, the remaining (1-kappaNRemob4) and the amounts in roots
                    //  and stolon is disposed off (added to soil FOM or Surface OM via litter)
                }

                // Check whether luxury N was remobilised during N balance
                if (NFastRemob2 + NFastRemob3 > 0.0)
                {
                    // If N was remobilised, update the N content in tissues accordingly
                    //  partition between parts is assumed proportional to N content
                    if (NFastRemob2 > 0.0)
                    {
                        Nsum = myPreviousState.leaves.Tissue[1].Namount + myPreviousState.stems.Tissue[1].Namount + myPreviousState.stolons.Tissue[1].Namount;
                        leaves.Tissue[1].Namount += NFastRemob2 * MathUtilities.Divide(myPreviousState.leaves.Tissue[1].Namount, Nsum, 0.0);
                        stems.Tissue[1].Namount += NFastRemob2 * MathUtilities.Divide(myPreviousState.stems.Tissue[1].Namount, Nsum, 0.0);
                        stolons.Tissue[1].Namount += NFastRemob2 * MathUtilities.Divide(myPreviousState.stolons.Tissue[1].Namount, Nsum, 0.0);
                    }
                    if (NFastRemob3 > 0.0)
                    {
                        Nsum = myPreviousState.leaves.Tissue[2].Namount + myPreviousState.stems.Tissue[2].Namount + myPreviousState.stolons.Tissue[2].Namount;
                        leaves.Tissue[2].Namount += NFastRemob3 * MathUtilities.Divide(myPreviousState.leaves.Tissue[2].Namount, Nsum, 0.0);
                        stems.Tissue[2].Namount += NFastRemob3 * MathUtilities.Divide(myPreviousState.stems.Tissue[2].Namount, Nsum, 0.0);
                        stolons.Tissue[2].Namount += NFastRemob3 * MathUtilities.Divide(myPreviousState.stolons.Tissue[2].Namount, Nsum, 0.0);
                    }
                }
            }
            else
            {
                // no actuall growth, just zero out some variables
                dGrowthShoot = 0.0;
                dGrowthRoot = 0.0;
            }
        }

        /// <summary>Computes the fraction of today's growth allocated to shoot</summary>
        /// <returns>The fraction of DM growth allocated to shoot (0-1)</returns>
        /// <remarks>
        /// Takes into consideration any seasonal variations and defoliation, this is done by
        /// targeting a given shoot:root ratio (that is the maxSRratio)
        /// </remarks>
        private double ToShootFraction()
        {
            double result = 1.0;
            double previousAbvGrdLivedWt = myPreviousState.leaves.DMGreen + myPreviousState.stems.DMGreen +
                                           myPreviousState.stolons.DMGreen;
            if (myPreviousState.roots.DMTotal > 0.00001 || previousAbvGrdLivedWt < myPreviousState.roots.DMTotal)
            {
                double fac = 1.0;
                int doyIncrease = doyIniHighShoot + higherShootAllocationPeriods[0]; //35;   //75
                int doyPlateau = doyIncrease + higherShootAllocationPeriods[1]; // 95;   // 110;
                int doyDecrease = doyPlateau + higherShootAllocationPeriods[2]; // 125;  // 140;
                int doy = myClock.Today.DayOfYear;

                if (doy > doyIniHighShoot)
                {
                    if (doy < doyIncrease)
                        fac = 1 + ShootSeasonalAllocationIncrease * MathUtilities.Divide(doy - doyIniHighShoot, higherShootAllocationPeriods[0], 0.0);
                    else if (doy <= doyPlateau)
                        fac = 1.0 + ShootSeasonalAllocationIncrease;
                    else if (doy <= doyDecrease)
                        fac = 1 + ShootSeasonalAllocationIncrease * (1 - MathUtilities.Divide(doy - doyPlateau, higherShootAllocationPeriods[2], 0.0));
                    else
                        fac = 1;
                }
                else
                {
                    if (doyDecrease > 365 && doy <= doyDecrease - 365)
                        fac = 1 + ShootSeasonalAllocationIncrease * (1 - MathUtilities.Divide(365 + doy - doyPlateau, higherShootAllocationPeriods[2], 0.0));
                }

                double presentSRratio = previousAbvGrdLivedWt / myPreviousState.roots.DMTotal;
                double targetedSRratio = fac * maxSRratio;
                double newSRratio;

                if (presentSRratio > targetedSRratio)
                    newSRratio = targetedSRratio;
                else
                    newSRratio = targetedSRratio * (targetedSRratio / presentSRratio);

                newSRratio *= Math.Min(glfWater, glfN);

                result = newSRratio / (1.0 + newSRratio);

                if (result / (1 - result) < targetedSRratio)
                    result = targetedSRratio / (1 + targetedSRratio);
            }

            return result;
        }

        /// <summary>Calculates the target (ideal) ratio of leaf to stems/stolons</summary>
        /// <returns>The target leaf to stem ratio</returns>
        private double CalcLeafRatio()
        {
            double leafRatio;
            double fLeaf = FracToLeaf;
            double dmMaxFLeaf = 500;
            double dmReferenceFLeaf = 2000;
            double minFLeaf = 0.0;
            double exponentFLeaf = 3.0;

            if (AboveGroundLivedWt > dmMaxFLeaf)
            {
                double dmAux = Math.Pow((AboveGroundLivedWt - dmMaxFLeaf) / (dmReferenceFLeaf - dmMaxFLeaf), exponentFLeaf);
                fLeaf = minFLeaf + (FracToLeaf - minFLeaf) / (1 + dmAux);
            }

            if (fLeaf < 1.0)
                leafRatio = fLeaf / (1 - fLeaf);
            else
                leafRatio = fLeaf / myEpsilon;

            return leafRatio;
        }

        /// <summary>Computes the turnover rate and update each tissue pool of all plant parts</summary>
        /// <exception cref="System.Exception">
        /// Loss of mass balance on C remobilisation - leaf
        /// or
        /// Loss of mass balance on C remobilisation - stem
        /// or
        /// Loss of mass balance on C remobilisation - stolon
        /// or
        /// Loss of mass balance on C remobilisation - root
        /// </exception>
        /// <remarks>The C and N amounts for remobilisation are also computed in here</remarks>
        internal void DoTissueTurnoverAndRemobilisation()
        {
            // The turnover rates are affected by temperature and soil moisture
            double TempFac = TempFactorForTissueTurnover(Tmean(0.5));
            double WaterFac = WaterFactorForTissueTurnover();
            double WaterFac2Litter = Math.Pow(glfWater, 3);
            double WaterFac2Root = 2 - glfWater;

            //stocking rate affecting transfer of dead to litter (default as 0 for now - should be read in)
            double SR = 0;
            double StockFac2Litter = StockParameter * SR;

            // Turnover rate for leaf and stem
            gama = TissueTurnoverRateShoot * TempFac * WaterFac;

            // Turnover rate for stolon
            gamaS = gama;

            // Turnover rate for dead to litter
            double digestDead = (leaves.DigestibilityDead * leaves.DMDead) + (stems.DigestibilityDead * stems.DMDead);
            digestDead = MathUtilities.Divide(digestDead, leaves.DMDead + stems.DMDead, 0.0);
            gamaD = DetachmentRate * WaterFac2Litter * digestDead / 0.4;

            gamaD += StockFac2Litter;

            // Turnover rate for roots
            gamaR = TissueTurnoverRateRoot * TempFac * WaterFac2Root;

            // Check whether any adjust on turnover rates are needed
            if (gama > 0.0)
            {
                // Check phenology effect for annuals
                if (isAnnual && phenologicStage > 0)
                {
                    if (phenologicStage == 1)
                    {
                        //vegetative, turnover is zero at emergence and increases with age
                        gama *= phenoFactor;
                        gamaS *= phenoFactor;
                        gamaR *= Math.Pow(phenoFactor, 2.0);
                        gamaD *= phenoFactor;
                    }
                    else if (phenologicStage == 2)
                    {
                        //reproductive, turnover increases with age and reach one at maturity
                        gama += (1.0 - gama) * Math.Pow(phenoFactor, 2.0);
                        gamaS = gama;
                        gamaR = (1.0 - gamaR) * Math.Pow(phenoFactor, 3.0);
                        gamaD = (1.0 - gamaD) * Math.Pow(phenoFactor, 3.0);
                    }
                }

                // Fraction of DM defoliated today
                double FracDefoliated = MathUtilities.Divide(dmDefoliated,
                    dmDefoliated + myPreviousState.leaves.DMTotal + myPreviousState.stems.DMTotal + myPreviousState.stolons.DMTotal, 0.0);

                // Adjust stolon turnover due to defoliation (increase stolon senescence)
                gamaS += FracDefoliated * (1.0 - gamaS);

                // Adjust turnover if senescence will result in dmGreen < dmGreenmin
                if (!isAnnual)
                {
                    double potSenescence = gama * (myPreviousState.leaves.Tissue[2].DM + myPreviousState.stems.Tissue[2].DM
                                                   + myPreviousState.stolons.Tissue[2].DM);
                    double dmGreenToBe = myPreviousState.AboveGroundLiveWt + dGrowthActual - potSenescence;
                    // TODO: here it should be dGrowthShoot, not dGrowthActual
                    //double dmGreenToBe = myPreviousState.AboveGroundLiveWt + dGrowthShoot - potSenescence;
                    if (dmGreenToBe < MinimumGreenWt)
                    {
                        if (myPreviousState.AboveGroundLiveWt + dGrowthShoot <= MinimumGreenWt)
                        {
                            // this should not happen anyway
                            gama = 0.0;
                            gamaS = 0.0;
                            gamaR = 0.0;
                        }
                        else
                        {
                            double gama_adj = MathUtilities.Divide(myPreviousState.AboveGroundLiveWt + dGrowthShoot - MinimumGreenWt,
                                myPreviousState.leaves.Tissue[2].DM + myPreviousState.stems.Tissue[2].DM + myPreviousState.stolons.Tissue[2].DM, gama);
                            gamaR *= gama_adj / gama;
                            gamaD *= gama_adj / gama;
                            gama = gama_adj;
                        }
                    }

                    // set a minimum root too
                    if (roots.DMGreen < 0.5 * MinimumGreenWt)
                        gamaR = 0.0;
                }

                // Do the actual DM turnover for all tissues
                double dDM_in = 0.0; // growth has been accounted for in PartitionNewGrowth
                double dDM_out = 2 * gama * myPreviousState.leaves.Tissue[0].DM;
                leaves.Tissue[0].DM += dDM_in - dDM_out;
                leaves.Tissue[0].Namount += -dDM_out * myPreviousState.leaves.Tissue[0].Nconc;
                dDM_in = dDM_out;
                dDM_out = gama * myPreviousState.leaves.Tissue[1].DM;
                leaves.Tissue[1].DM += dDM_in - dDM_out;
                leaves.Tissue[1].Namount += dDM_in * myPreviousState.leaves.Tissue[0].Nconc - dDM_out * myPreviousState.leaves.Tissue[1].Nconc;
                dDM_in = dDM_out;
                dDM_out = gama * myPreviousState.leaves.Tissue[2].DM;
                leaves.Tissue[2].DM += dDM_in - dDM_out;
                leaves.Tissue[2].Namount += dDM_in * myPreviousState.leaves.Tissue[1].Nconc - dDM_out * myPreviousState.leaves.Tissue[2].Nconc;
                dDM_in = dDM_out;
                dDM_out = gamaD * myPreviousState.leaves.Tissue[3].DM;
                double ChRemobSugar = dDM_in * KappaCRemob;
                double ChRemobProtein = dDM_in * (myPreviousState.leaves.Tissue[2].Nconc - leaves.NConcMinimum) * CNratioProtein * FacCNRemob;
                dDM_in -= ChRemobSugar + ChRemobProtein;
                if (dDM_in < 0.0)
                    throw new Exception("Loss of mass balance on C remobilisation - leaf");
                leaves.Tissue[3].DM += dDM_in - dDM_out;
                leaves.Tissue[3].Namount += dDM_in * leaves.NConcMinimum - dDM_out * myPreviousState.leaves.Tissue[3].Nconc;
                dLitter = dDM_out;
                dNlitter = dDM_out * myPreviousState.leaves.Tissue[3].Nconc;
                dGrowthShoot -= dDM_out;
                double NRemobilised = dDM_in * (myPreviousState.leaves.Tissue[2].Nconc - leaves.NConcMinimum);
                double ChRemobl = ChRemobSugar + ChRemobProtein;

                dDM_in = 0.0; // growth has been accounted for in PartitionNewGrowth
                dDM_out = 2 * gama * myPreviousState.stems.Tissue[0].DM;
                stems.Tissue[0].DM += dDM_in - dDM_out;
                stems.Tissue[0].Namount += -dDM_out * myPreviousState.stems.Tissue[0].Nconc;
                dDM_in = dDM_out;
                dDM_out = gama * myPreviousState.stems.Tissue[1].DM;
                stems.Tissue[1].DM += dDM_in - dDM_out;
                stems.Tissue[1].Namount += dDM_in * myPreviousState.stems.Tissue[0].Nconc - dDM_out * myPreviousState.stems.Tissue[1].Nconc;
                dDM_in = dDM_out;
                dDM_out = gama * myPreviousState.stems.Tissue[2].DM;
                stems.Tissue[2].DM += dDM_in - dDM_out;
                stems.Tissue[2].Namount += dDM_in * myPreviousState.stems.Tissue[1].Nconc - dDM_out * myPreviousState.stems.Tissue[2].Nconc;
                dDM_in = dDM_out;
                dDM_out = gamaD * myPreviousState.stems.Tissue[3].DM;
                ChRemobSugar = dDM_in * KappaCRemob;
                ChRemobProtein = dDM_in * (myPreviousState.stems.Tissue[2].Nconc - stems.NConcMinimum) * CNratioProtein * FacCNRemob;
                dDM_in -= ChRemobSugar + ChRemobProtein;
                if (dDM_in < 0.0)
                    throw new Exception("Loss of mass balance on C remobilisation - stem");
                stems.Tissue[3].DM += dDM_in - dDM_out;
                stems.Tissue[3].Namount += dDM_in * stems.NConcMinimum - dDM_out * myPreviousState.stems.Tissue[3].Nconc;
                dLitter += dDM_out;
                dNlitter += dDM_out * myPreviousState.stems.Tissue[3].Nconc;
                dGrowthShoot -= dDM_out;
                NRemobilised += dDM_in * (myPreviousState.stems.Tissue[2].Nconc - stems.NConcMinimum);
                ChRemobl += ChRemobSugar + ChRemobProtein;

                dDM_in = 0.0; // growth has been accounted for in PartitionNewGrowth
                dDM_out = 2 * gamaS * myPreviousState.stolons.Tissue[0].DM;
                stolons.Tissue[0].DM += dDM_in - dDM_out;
                stolons.Tissue[0].Namount += -dDM_out * myPreviousState.stolons.Tissue[0].Nconc;
                dDM_in = dDM_out;
                dDM_out = gamaS * myPreviousState.stolons.Tissue[1].DM;
                stolons.Tissue[1].DM += dDM_in - dDM_out;
                stolons.Tissue[1].Namount += dDM_in * myPreviousState.stolons.Tissue[0].Nconc - dDM_out * myPreviousState.stolons.Tissue[1].Nconc;
                dDM_in = dDM_out;
                dDM_out = gamaS * myPreviousState.stolons.Tissue[2].DM;
                stolons.Tissue[2].DM += dDM_in - dDM_out;
                stolons.Tissue[2].Namount += dDM_in * myPreviousState.stolons.Tissue[1].Nconc - dDM_out * myPreviousState.stolons.Tissue[2].Nconc;
                dDM_in = dDM_out;
                ChRemobSugar = dDM_in * KappaCRemob;
                ChRemobProtein = dDM_in * (myPreviousState.stolons.Tissue[2].Nconc - stolons.NConcMinimum) * CNratioProtein * FacCNRemob;
                dDM_in -= ChRemobSugar + ChRemobProtein;
                if (dDM_in < 0.0)
                    throw new Exception("Loss of mass balance on C remobilisation - stolon");
                dLitter += dDM_in;
                dNlitter += dDM_in * stolons.NConcMinimum + 0.5 * dDM_in * (myPreviousState.stolons.Tissue[2].Nconc - stolons.NConcMinimum);
                dGrowthShoot -= dDM_in;
                NRemobilised += 0.5 * dDM_in * (myPreviousState.stolons.Tissue[2].Nconc - stolons.NConcMinimum);
                ChRemobl += ChRemobSugar + ChRemobProtein;

                dRootSen = gamaR * myPreviousState.roots.Tissue[0].DM;
                //roots.Tissue[0].DM -= Done later in EvaluateRootGrowth
                ChRemobSugar = dRootSen * KappaCRemob;
                ChRemobProtein = dRootSen * (myPreviousState.roots.Tissue[0].Nconc - roots.NConcMinimum) * CNratioProtein * FacCNRemob;
                dRootSen -= ChRemobSugar + ChRemobProtein;
                if (dRootSen < 0.0)
                    throw new Exception("Loss of mass balance on C remobilisation - root");
                dNrootSen = gamaR * myPreviousState.roots.Tissue[0].Namount - 0.5 * dRootSen * (myPreviousState.roots.Tissue[0].Nconc - roots.NConcMinimum);
                roots.Tissue[0].Namount -= gamaR * myPreviousState.roots.Tissue[0].Namount;
                NRemobilised += 0.5 * dRootSen * (myPreviousState.roots.Tissue[0].Nconc - roots.NConcMinimum);
                ChRemobl += ChRemobSugar + ChRemobProtein;

                // Remobilised C to be used in tomorrow's growth (converted from carbohydrate to C)
                CRemobilisable = ChRemobl * CarbonFractionInDM;

                // Fraction of N remobilised yesterday that was not used in new growth is added to today's litter
                dNlitter += NRemobilising;
            }
            else
            {
                // No turnover, just zero out some variables
                dLitter = 0.0;
                dNlitter = 0.0;
                dRootSen = 0.0;
                dNrootSen = 0.0;
                CRemobilisable = 0.0;
                NRemobilised = 0.0;
            }
            // N remobilisable from luxury N to be potentially used for growth tomorrow
            NLuxury2 = Math.Max(0.0, leaves.Tissue[1].Namount - (leaves.Tissue[1].DM * leaves.NConcOptimum * RelativeNStage2))
                     + Math.Max(0.0, stems.Tissue[1].Namount - (stems.Tissue[1].DM * stems.NConcOptimum * RelativeNStage2))
                     + Math.Max(0.0, stolons.Tissue[1].Namount - (stolons.Tissue[1].DM * stolons.NConcOptimum * RelativeNStage2));
            NLuxury3 = Math.Max(0.0, leaves.Tissue[2].Namount - (leaves.Tissue[2].DM * leaves.NConcOptimum * RelativeNStage3))
                     + Math.Max(0.0, stems.Tissue[2].Namount - (stems.Tissue[2].DM * stems.NConcOptimum * RelativeNStage3))
                     + Math.Max(0.0, stolons.Tissue[2].Namount - (stolons.Tissue[2].DM * stolons.NConcOptimum * RelativeNStage3));
            // only a fraction of luxury N is actually available for remobilisation:
            NLuxury2 *= KappaNRemob2;
            NLuxury3 *= KappaNRemob3;
        }

        #endregion

        #region - Water uptake processes  ----------------------------------------------------------------------------------

        /// <summary>Gets the water uptake for each layer as calculated by an external module (SWIM)</summary>
        /// <param name="SoilWater">The soil water.</param>
        /// <remarks>
        /// This method is only used when an external method is used to compute water uptake (this includes AgPasture)
        /// </remarks>
        [EventSubscribe("WaterUptakesCalculated")]
        private void OnWaterUptakesCalculated(PMF.WaterUptakesCalculatedType SoilWater)
        {
            for (int iCrop = 0; iCrop < SoilWater.Uptakes.Length; iCrop++)
            {
                if (SoilWater.Uptakes[iCrop].Name == Name)
                {
                    for (int layer = 0; layer < SoilWater.Uptakes[iCrop].Amount.Length; layer++)
                        mySoilWaterTakenUp[layer] = SoilWater.Uptakes[iCrop].Amount[layer];
                }
            }
        }

        /// <summary>
        /// Gets the amount of water uptake for this species as computed by the resource Arbitrator
        /// </summary>
        private void GetWaterUptake()
        {
            Array.Clear(mySoilWaterTakenUp, 0, mySoilWaterTakenUp.Length);
            for (int layer = 0; layer < roots.BottomLayer; layer++)
                mySoilWaterTakenUp[layer] = uptakeWater[layer];
        }

        /// <summary>
        /// Consider water uptake calculations (plus GLFWater)
        /// </summary>
        internal void DoWaterCalculations()
        {
            if (myWaterUptakeSource == "species")
            {
                // this module will compute water uptake
                MyWaterCalculations();

                // get the drought effects
                glfWater = WaterDeficitFactor();

                // get the water logging effects (only if there is no drought effect)
                glfAeration = WaterLoggingFactor();
                if (glfWater > 0.999)
                    glfWater = glfAeration;
            }
            //else if myWaterUptakeSource == "AgPasture"
            //      myWaterDemand should have been supplied by MicroClimate (supplied as PotentialEP)
            //      water supply is hold by AgPasture only
            //      myWaterUptake should have been computed by AgPasture (set directly)
            //      glfWater is computed and set by AgPasture
            else if ((myWaterUptakeSource == "SoilArbitrator") || (myWaterUptakeSource == "arbitrator"))
            {
                // water uptake has been calcualted by the resource arbitrator

                // get the array with the amount of water taken up
                GetWaterUptake();
                DoSoilWaterUptake1();

                // get the drought effects
                glfWater = WaterDeficitFactor();

                // get the water logging effects (only if there is no drought effect)
                glfAeration = WaterLoggingFactor();
                if (glfWater > 0.999)
                    glfWater = glfAeration;
            }
            //else
            //      water uptake be calculated by other modules (e.g. SWIM) and supplied as
            //  Note: when AgPasture is doing the water uptake, it can do it using its own calculations or other module's...
        }

        /// <summary>
        /// Gather the amount of available eater and computes the water uptake for this species
        /// </summary>
        /// <remarks>
        /// Using this routine is discourage as it ignores the presence of other species and thus
        /// might result in loss of mass balance or unbalanced supply, i.e. over-supply for one
        /// while under-supply for other species (depending on the order that species are considered)
        /// </remarks>
        private void MyWaterCalculations()
        {
            mySoilAvailableWater = GetSoilAvailableWater();
            // myWaterDemand given by MicroClimate
            if (myWaterUptakeSource.ToLower() == "species")
                mySoilWaterTakenUp = DoSoilWaterUptake();
            //else
            //    uptake is controlled by the sward or by another apsim module
        }

        /// <summary>
        /// Finds out the amount soil water available for this plant (ignoring any other species)
        /// </summary>
        /// <returns>The amount of water available to plants in each layer</returns>
        internal double[] GetSoilAvailableWater()
        {
            double[] result = new double[nLayers];
            SoilCrop soilCropData = (SoilCrop) mySoil.Crop(Name);
            if (useAltWUptake == "no")
            {
                for (int layer = 0; layer < roots.BottomLayer; layer++)
                {
                    result[layer] = Math.Max(0.0, mySoil.Water[layer] - soilCropData.LL[layer] * mySoil.Thickness[layer])
                                    * FractionLayerWithRoots(layer);
                    result[layer] *= soilCropData.KL[layer];
                }
            }
            else
            {
                // Method implemented by RCichota
                // Available Water is function of root density, soil water content, and soil hydraulic conductivity
                // Assumptions: all factors are exponential functions and vary between 0 and 1;
                //   - If root density is equal to ReferenceRLD then plant can explore 90% of the water;
                //   - If soil Ksat is equal to ReferenceKSuptake then soil can supply 90% of its available water;
                //   - If soil water content is at DUL then 90% of its water is available;
                double[] myRLD = RLD;
                double facRLD = 0.0;
                double facCond = 0.0;
                double facWcontent = 0.0;
                for (int layer = 0; layer < roots.BottomLayer; layer++)
                {
                    facRLD = 1 - Math.Pow(10, -myRLD[layer] / ReferenceRLD);
                    facCond = 1 - Math.Pow(10, -mySoil.KS[layer] / ReferenceKSuptake);
                    facWcontent = 1 - Math.Pow(10,
                        -(Math.Max(0.0, mySoil.Water[layer] - mySoil.SoilWater.LL15mm[layer]))
                        / (mySoil.SoilWater.DULmm[layer] - mySoil.SoilWater.LL15mm[layer]));

                    // Theoretical total available water
                    result[layer] = Math.Max(0.0, mySoil.Water[layer] - soilCropData.LL[layer] * mySoil.Thickness[layer])
                                    * FractionLayerWithRoots(layer);
                    // Actual available water
                    result[layer] *= facRLD * facCond * facWcontent;
                }
            }

            return result;
        }

        /// <summary>Computes the actual water uptake and send the deltas to soil module</summary>
        /// <returns>The amount of water taken up for each soil layer</returns>
        /// <exception cref="System.Exception">Error on computing water uptake</exception>
        private double[] DoSoilWaterUptake()
        {
            PMF.WaterChangedType WaterTakenUp = new PMF.WaterChangedType();
            WaterTakenUp.DeltaWater = new double[nLayers];

            double uptakeFraction = Math.Min(1.0, MathUtilities.Divide(myWaterDemand, mySoilAvailableWater.Sum(), 0.0));
            double[] result = new double[nLayers];

            if (useAltWUptake == "no")
            {
                for (int layer = 0; layer < roots.BottomLayer; layer++)
                {
                    result[layer] = mySoilAvailableWater[layer] * uptakeFraction;
                    WaterTakenUp.DeltaWater[layer] = -result[layer];
                }
            }
            else
            {
                // Method implemented by RCichota
                // Uptake is distributed over the profile according to water availability,
                //  this means that water status and root distribution have been taken into account

                for (int layer = 0; layer < roots.BottomLayer; layer++)
                {
                    result[layer] = mySoilAvailableWater[layer] * uptakeFraction;
                    WaterTakenUp.DeltaWater[layer] = -result[layer];
                }
                if (Math.Abs(WaterTakenUp.DeltaWater.Sum() + myWaterDemand) > 0.0001)
                    throw new Exception("Error on computing water uptake");
            }

            // send the delta water taken up
            WaterChanged.Invoke(WaterTakenUp);

            return result;
        }

        /// <summary>Send the delta water taken up to  the soil module</summary>
        private void DoSoilWaterUptake1()
        {
            WaterChangedType WaterTakenUp = new WaterChangedType();
            WaterTakenUp.DeltaWater = new double[nLayers];

            for (int layer = 0; layer < roots.BottomLayer; layer++)
                WaterTakenUp.DeltaWater[layer] = -mySoilWaterTakenUp[layer];

            // send the delta water taken up
            WaterChanged.Invoke(WaterTakenUp);
        }

        #endregion

        #region - Nitrogen uptake processes  -------------------------------------------------------------------------------

        /// <summary>
        /// Gets the amount of nitrogen uptake for this species as computed by the resource Arbitrator
        /// </summary>
        private void GetNitrogenUptake()
        {
            // get the amount of N taken up from soil
            Array.Clear(mySoilNitrogenTakenUp, 0, mySoilNitrogenTakenUp.Length);
            mySoilNuptake = 0.0;
            for (int layer = 0; layer < roots.BottomLayer; layer++)
            {
                mySoilNitrogenTakenUp[layer] = uptakeNH4[layer] + uptakeNO3[layer];
                mySoilNuptake += mySoilNitrogenTakenUp[layer];
            }
            newGrowthN = Nfixation + Nremob2NewGrowth + mySoilNuptake;

            // evaluate whether further remobilisation (from luxury N) is needed
            CalcNLuxuryRemob();
            newGrowthN += NFastRemob3 + NFastRemob2;
        }

        /// <summary>
        /// Gets the amount of nitrogen uptake for this species as computed by the resource Arbitrator
        /// </summary>
        private void GetPotentialNitrogenUptake(SoilState soilState)
        {
            // get soil available N
            GetSoilAvailableN1(soilState);

            // get N demand (optimum and luxury)
            CalcNDemand();

            // get N fixation
            Nfixation = CalcNFixation();

            // evaluate the use of N remobilised and get soil N demand
            CalcSoilNDemand();

            // evaluate the potential N uptake for each layer
            mySoilNuptake = CalcPotentialSoilNUptake(soilState);
        }

        /// <summary>
        /// Consider nitrogen uptake calculations (plus GLFN)
        /// </summary>
        internal void DoNitrogenCalculations()
        {
            NRemobilising = NRemobilisable;
            if (myNitrogenUptakeSource == "species")
            {
                // this module will compute the N uptake
                MyNitrogenCalculations();
                if (newGrowthN > 0.0)
                    glfN = Math.Min(1.0, Math.Max(0.0, MathUtilities.Divide(newGrowthN, NdemandOpt, 1.0)));
                else
                    glfN = 1.0;
            }
            //else if (myNitrogenUptakeSource == "AgPasture")
            //{
            //    NdemandOpt is called by AgPasture
            //    NdemandLux is called by AgPasture
            //    Nfix is called by AgPasture
            //    myNitrogenSupply is hold by AgPasture
            //    soilNdemand is computed by AgPasture
            //    soilNuptake is computed by AgPasture
            //    remob2NewGrowth is computed by AgPasture
            //}
            else if (myNitrogenUptakeSource == "SoilArbitrator")
            {
                // Nitrogen uptake was computed by the resource arbitrator

                // get the amount of N taken up
                GetNitrogenUptake();

                DoSoilNitrogenUptake1();

                if (newGrowthN > 0.0)
                    glfN = Math.Min(1.0, Math.Max(0.0, MathUtilities.Divide(newGrowthN, NdemandOpt, 1.0)));
                else
                    glfN = 1.0;
            }
            //else
            //   N uptake is computed by another module (not implemented yet)
        }

        /// <summary>Performs the computations for N balance and uptake</summary>
        private void MyNitrogenCalculations()
        {
            if (myNitrogenUptakeSource.ToLower() == "species")
            {
                // get soil available N
                GetSoilAvailableN();

                // get N demand (optimum and luxury)
                CalcNDemand();

                // get N fixation
                Nfixation = CalcNFixation();

                // evaluate the use of N remobilised and get soil N demand
                CalcSoilNDemand();

                // get the amount of N taken up from soil
                mySoilNuptake = CalcSoilNUptake();
                newGrowthN = Nfixation + Nremob2NewGrowth + mySoilNuptake;

                // evaluate whether further remobilisation (from luxury N) is needed
                CalcNLuxuryRemob();
                newGrowthN += NFastRemob3 + NFastRemob2;

                // send delta N to the soil model
                DoSoilNitrogenUptake();
            }
            //else
            //    N available is computed in another module

        }

        /// <summary>Computes the N demanded for optimum N content as well as luxury uptake</summary>
        internal void CalcNDemand()
        {
            double toRoot = dGrowthWstress * (1.0 - fShoot);
            double toStol = dGrowthWstress * fShoot * FracToStolon;
            double toLeaf = dGrowthWstress * fShoot * FracToLeaf;
            double toStem = dGrowthWstress * fShoot * (1.0 - FracToStolon - FracToLeaf);

            // N demand for new growth, with optimum N (kg/ha)
            NdemandOpt = (toLeaf * leaves.NConcOptimum) + (toStem * stems.NConcOptimum)
                       + (toStol * stolons.NConcOptimum) + (toRoot * roots.NConcOptimum);

            // get the factor to reduce the demand under elevated CO2
            double fN = NCO2Effects();
            NdemandOpt *= fN;

            // N demand for new growth, with luxury uptake (maximum [N])
            NdemandLux = (toLeaf * leaves.NConcMaximum) + (toStem * stems.NConcMaximum)
                       + (toStol * stolons.NConcMaximum) + (toRoot * roots.NConcMaximum);
            // It is assumed that luxury uptake is not affected by CO2 variations
        }

        /// <summary>Computes the amount of N fixed from atmosphere</summary>
        /// <returns>The amount of N fixed (kgN/ha)</returns>
        internal double CalcNFixation()
        {
            double result = 0.0;

            if (myClock.Today.Date.Day == 31)
                result = 0.0;

            if (isLegume)
            {
                // Start with minimum fixation
                double iniFix = MinimumNFixation * NdemandLux;

                // evaluate N stress
                double Nstress = 1.0;
                if (NdemandLux > 0.0 && (NdemandLux > mySoilAvailableN.Sum() + iniFix))
                    Nstress = MathUtilities.Divide(mySoilAvailableN.Sum(), NdemandLux - iniFix, 1.0);

                // Update N fixation if under N stress
                if (Nstress < 0.99)
                    result = MaximumNFixation - (MaximumNFixation - MinimumNFixation) * Nstress;
                else
                    result = MinimumNFixation;
            }

            return Math.Max(0.0, result) * NdemandLux;
        }

        /// <summary>Perform preliminary N budget and get soil N demand</summary>
        internal void CalcSoilNDemand()
        {
            if (Nfixation - NdemandLux > -0.0001)
            {
                // N demand is fulfilled by fixation alone
                Nfixation = NdemandLux; // should not be needed, but just in case...
                Nremob2NewGrowth = 0.0;
                mySoilNDemand = 0.0;
            }
            else if ((Nfixation + NRemobilising) - NdemandLux > -0.0001)
            {
                // N demand is fulfilled by fixation plus N remobilised from senescent material
                Nremob2NewGrowth = Math.Max(0.0, NdemandLux - Nfixation);
                NRemobilising -= Nremob2NewGrowth;
                mySoilNDemand = 0.0;
            }
            else
            {
                // N demand is greater than fixation and remobilisation of senescent, N uptake is needed
                Nremob2NewGrowth = NRemobilising;
                NRemobilising = 0.0;
                mySoilNDemand = NdemandLux - (Nfixation + Nremob2NewGrowth);
            }

            // variable used by arbitrator
            demandNitrogen = mySoilNDemand;
        }

        /// <summary>
        /// Find out the amount of Nitrogen (NH4 and NO3) in the soil available to plants for each soil layer
        /// </summary>
        internal void GetSoilAvailableN()
        {
            mySoilNH4available = new double[nLayers];
            mySoilNO3available = new double[nLayers];
            mySoilAvailableN = new double[nLayers];

            double facWtaken = 0.0;
            for (int layer = 0; layer < roots.BottomLayer; layer++)
            {
                if (useAltNUptake == "no")
                {
                    // simple way, all N in the root zone is available
                    mySoilNH4available[layer] = mySoil.NH4N[layer] * FractionLayerWithRoots(layer);
                    mySoilNO3available[layer] = mySoil.NO3N[layer] * FractionLayerWithRoots(layer);
                }
                else
                {
                    // Method implemented by RCichota,
                    // N is available following water and a given 'availability' factor (for each N form) and the fraction of water taken up

                    // fraction of available water taken up
                    facWtaken = MathUtilities.Divide(mySoilWaterTakenUp[layer],
                        Math.Max(0.0, mySoil.Water[layer] - mySoil.SoilWater.LL15mm[layer]), 0.0);

                    // Theoretical amount available
                    mySoilNH4available[layer] = mySoil.NH4N[layer] * kuNH4 * FractionLayerWithRoots(layer);
                    mySoilNO3available[layer] = mySoil.NO3N[layer] * kuNO3 * FractionLayerWithRoots(layer);

                    // actual amount available
                    mySoilNH4available[layer] *= facWtaken;
                    mySoilNO3available[layer] *= facWtaken;
                }
                mySoilAvailableN[layer] = mySoilNH4available[layer] + mySoilNO3available[layer];
            }
        }


        /// <summary>
        /// Find out the amount of Nitrogen (NH4 and NO3) in the soil available to plants for each soil layer
        /// </summary>
        internal void GetSoilAvailableN1(SoilState soilState)
        {
            mySoilNH4available = new double[nLayers];
            mySoilNO3available = new double[nLayers];
            mySoilAvailableN = new double[nLayers];

            double facWtaken = 0.0;
            for (int layer = 0; layer < roots.BottomLayer; layer++)
            {
                if (useAltNUptake == "no")
                {
                    // simple way, all N in the root zone is available
                    foreach (ZoneWaterAndN zone in soilState.Zones)
                    {
                        mySoilNH4available[layer] += zone.NH4N[layer] * FractionLayerWithRoots(layer);
                        mySoilNH4available[layer] += zone.NO3N[layer] * FractionLayerWithRoots(layer);
                    }
                }
                else
                {
                    // Method implemented by RCichota,
                    // N is available following water and a given 'availability' factor (for each N form) and the fraction of water taken up

                    // fraction of available water taken up
                    facWtaken = MathUtilities.Divide(mySoilWaterTakenUp[layer],
                        Math.Max(0.0, mySoil.Water[layer] - mySoil.SoilWater.LL15mm[layer]), 0.0);

                    // Theoretical amount available
                    foreach (ZoneWaterAndN zone in soilState.Zones)
                    {
                        mySoilNH4available[layer] += zone.NH4N[layer] * kuNH4 * FractionLayerWithRoots(layer);
                        mySoilNH4available[layer] += zone.NO3N[layer] * kuNO3 * FractionLayerWithRoots(layer);
                    }

                    // actual amount available
                    mySoilNH4available[layer] *= facWtaken;
                    mySoilNO3available[layer] *= facWtaken;
                }
                mySoilAvailableN[layer] = mySoilNH4available[layer] + mySoilNO3available[layer];
            }
        }

        /// <summary>Computes the amount of N to be taken up from the soil</summary>
        /// <returns>The amount of N to be taken up from each soil layer</returns>
        private double CalcPotentialSoilNUptake(SoilState soilState)
        {
            double result;
            myPotentialNH4NUptake = new double[nLayers];
            myPotentialNO3NUptake = new double[nLayers];
            if (mySoilNDemand == 0.0)
            {
                // No demand, no uptake
                result = 0.0;
            }
            else
            {
                if (mySoilAvailableN.Sum() >= mySoilNDemand)
                {
                    // soil can supply all remaining N needed
                    result = mySoilNDemand;
                }
                else
                {
                    // soil cannot supply all N needed. Get the available N
                    result = mySoilAvailableN.Sum();
                }

                double uptakeFraction = Math.Min(1.0, MathUtilities.Divide(result, mySoilAvailableN.Sum(), 0.0));
                for (int layer = 0; layer < roots.BottomLayer; layer++)
                {
                    foreach (ZoneWaterAndN zone in soilState.Zones)
                    {
                        myPotentialNH4NUptake[layer] += zone.NH4N[layer] * uptakeFraction;
                        myPotentialNO3NUptake[layer] += zone.NO3N[layer] * uptakeFraction;
                    }
                }
            }

            return result;
        }

        /// <summary>Computes the amount of N to be taken up from the soil</summary>
        /// <returns>The amount of N to be taken up from each soil layer</returns>
        private double CalcSoilNUptake()
        {
            double result;
            if (mySoilNDemand == 0.0)
            {
                // No demand, no uptake
                result = 0.0;
            }
            else
            {
                if (mySoilAvailableN.Sum() >= mySoilNDemand)
                {
                    // soil can supply all remaining N needed
                    result = mySoilNDemand;
                }
                else
                {
                    // soil cannot supply all N needed. Get the available N
                    result = mySoilAvailableN.Sum();
                }
            }
            return result;
        }

        /// <summary>Computes the remobilisation of luxury N (from tissues 2 and 3)</summary>
        internal void CalcNLuxuryRemob()
        {
            // check whether N demand for optimum growth has been matched
            if (newGrowthN - NdemandOpt > -0.0001)
            {
                // N demand has been matched, no further remobilisation is needed
                NFastRemob3 = 0.0;
                NFastRemob2 = 0.0;
            }
            else
            {
                // all N already considered is not enough for optimum growth, check remobilisation of luxury N
                //  check whether luxury N in plants can be used (luxury uptake is ignored)
                double Nmissing = NdemandOpt - newGrowthN;
                if (Nmissing > NLuxury2 + NLuxury3)
                {
                    // N luxury is still not enough for optimum growth, use up all there is
                    if (NLuxury2 + NLuxury3 > 0)
                    {
                        NFastRemob3 = NLuxury3;
                        NFastRemob2 = NLuxury2;
                        Nmissing -= (NLuxury3 + NLuxury2);
                    }
                }
                else
                {
                    // There is luxury N that can be used for optimum growth, get first from tissue 3
                    if (Nmissing <= NLuxury3)
                    {
                        // tissue 3 is enough
                        NFastRemob3 = Nmissing;
                        NFastRemob2 = 0.0;
                        Nmissing = 0.0;
                    }
                    else
                    {
                        // get first from tissue 3
                        NFastRemob3 = NLuxury3;
                        Nmissing -= NLuxury3;

                        // remaining from tissue 2
                        NFastRemob2 = Nmissing;
                        Nmissing = 0.0;
                    }
                }
            }
        }

        /// <summary>
        /// Computes the distribution of N uptake over the soil profile and send the delta to soil module
        /// </summary>
        /// <exception cref="System.Exception">
        /// Error on computing N uptake
        /// or
        /// N uptake source was not recognised. Please specify it as either \"sward\" or \"species\".
        /// </exception>
        private void DoSoilNitrogenUptake()
        {
            if (myNitrogenUptakeSource.ToLower() == "species")
            {
                // check whether there is any uptake
                if (mySoilAvailableN.Sum() > 0.0 && mySoilNuptake > 0.0)
                {

                    Soils.NitrogenChangedType NUptake = new Soils.NitrogenChangedType();
                    NUptake.Sender = Name;
                    NUptake.SenderType = "Plant";
                    NUptake.DeltaNO3 = new double[nLayers];
                    NUptake.DeltaNH4 = new double[nLayers];

                    mySoilNitrogenTakenUp = new double[nLayers];
                    double uptakeFraction = 0;

                    if (useAltNUptake == "no")
                    {
                        if (mySoilAvailableN.Sum() > 0.0)
                            uptakeFraction = Math.Min(1.0,
                                MathUtilities.Divide(mySoilNuptake, mySoilAvailableN.Sum(), 0.0));

                        for (int layer = 0; layer < roots.BottomLayer; layer++)
                        {
                            NUptake.DeltaNH4[layer] = -mySoil.NH4N[layer] * uptakeFraction;
                            NUptake.DeltaNO3[layer] = -mySoil.NO3N[layer] * uptakeFraction;

                            mySoilNitrogenTakenUp[layer] = -(NUptake.DeltaNH4[layer] + NUptake.DeltaNO3[layer]);
                        }
                    }
                    else
                    {
                        // Method implemented by RCichota,
                        // N uptake is distributed considering water uptake and N availability
                        double[] fNH4Avail = new double[nLayers];
                        double[] fNO3Avail = new double[nLayers];
                        double[] fWUptake = new double[nLayers];
                        double totNH4Available = mySoilAvailableN.Sum();
                        double totNO3Available = mySoilAvailableN.Sum();
                        double totWuptake = mySoilWaterTakenUp.Sum();
                        for (int layer = 0; layer < nLayers; layer++)
                        {
                            fNH4Avail[layer] = Math.Min(1.0,
                                MathUtilities.Divide(mySoilAvailableN[layer], totNH4Available, 0.0));
                            fNO3Avail[layer] = Math.Min(1.0,
                                MathUtilities.Divide(mySoilAvailableN[layer], totNO3Available, 0.0));
                            fWUptake[layer] = Math.Min(1.0,
                                MathUtilities.Divide(mySoilWaterTakenUp[layer], totWuptake, 0.0));
                        }
                        double totFacNH4 = fNH4Avail.Sum() + fWUptake.Sum();
                        double totFacNO3 = fNO3Avail.Sum() + fWUptake.Sum();
                        for (int layer = 0; layer < nLayers; layer++)
                        {
                            uptakeFraction = Math.Min(1.0,
                                MathUtilities.Divide(fNH4Avail[layer] + fWUptake[layer], totFacNH4, 0.0));
                            NUptake.DeltaNH4[layer] = -mySoil.NH4N[layer] * uptakeFraction;

                            uptakeFraction = Math.Min(1.0,
                                MathUtilities.Divide(fNO3Avail[layer] + fWUptake[layer], totFacNO3, 0.0));
                            NUptake.DeltaNO3[layer] = -mySoil.NO3N[layer] * uptakeFraction;

                            mySoilNitrogenTakenUp[layer] = NUptake.DeltaNH4[layer] + NUptake.DeltaNO3[layer];
                        }
                    }

                    //mySoilUptakeN.Sum()   2.2427998752781684  double

                    if (Math.Abs(mySoilNuptake - mySoilNitrogenTakenUp.Sum()) > 0.0001)
                        throw new Exception("Error on computing N uptake");

                    // do the actual N changes
                    NitrogenChanged.Invoke(NUptake);
                }
                else
                {
                    // no uptake, just zero out the array
                    mySoilNitrogenTakenUp = new double[nLayers];
                }
            }
            else
            {
                // N uptake calculated by other modules (e.g., SWIM)
                string msg = "N uptake source was not recognised. Please specify it as either \"sward\" or \"species\".";
                throw new Exception(msg);
            }
        }

        /// <summary>
        /// Send the delta N to the soil module
        /// </summary>
        private void DoSoilNitrogenUptake1()
        {
            Soils.NitrogenChangedType NUptake = new Soils.NitrogenChangedType();
            NUptake.Sender = Name;
            NUptake.SenderType = "Plant";
            NUptake.DeltaNO3 = new double[nLayers];
            NUptake.DeltaNH4 = new double[nLayers];

            for (int layer = 0; layer < roots.BottomLayer; layer++)
            {
                NUptake.DeltaNH4[layer] = -uptakeNH4[layer];
                NUptake.DeltaNO3[layer] = -uptakeNO3[layer];
            }

            // do the actual N changes
            NitrogenChanged.Invoke(NUptake);
        }

        #endregion

        #region - Organic matter processes  --------------------------------------------------------------------------------

        /// <summary>Return a given amount of DM (and N) to surface organic matter</summary>
        /// <param name="amountDM">DM amount to return</param>
        /// <param name="amountN">N amount to return</param>
        private void DoSurfaceOMReturn(double amountDM, double amountN)
        {
            if (BiomassRemoved != null)
            {
                Single dDM = (Single) amountDM;

                PMF.BiomassRemovedType BR = new PMF.BiomassRemovedType();
                String[] type = new String[] {mySpeciesFamily.ToString()};
                Single[] dltdm = new Single[] {(Single) amountDM};
                Single[] dltn = new Single[] {(Single) amountN};
                Single[] dltp = new Single[] {0}; // P not considered here
                Single[] fraction = new Single[] {1}; // fraction is always 1.0 here

                BR.crop_type = "grass"; //TODO: this could be the Name, what is the diff between name and type??
                BR.dm_type = type;
                BR.dlt_crop_dm = dltdm;
                BR.dlt_dm_n = dltn;
                BR.dlt_dm_p = dltp;
                BR.fraction_to_residue = fraction;
                BiomassRemoved.Invoke(BR);
            }
        }

        /// <summary>Return scenescent roots to fresh organic matter pool in the soil</summary>
        /// <param name="amountDM">DM amount to return</param>
        /// <param name="amountN">N amount to return</param>
        private void DoIncorpFomEvent(double amountDM, double amountN)
        {
            Soils.FOMLayerLayerType[] FOMdataLayer = new Soils.FOMLayerLayerType[nLayers];

            // ****  RCichota, Jun/2014
            // root senesced are returned to soil (as FOM) considering return is proportional to root mass

            for (int layer = 0; layer < nLayers; layer++)
            {
                Soils.FOMType fomData = new Soils.FOMType();
                fomData.amount = amountDM * roots.Tissue[0].FractionWt[layer];
                fomData.N = amountN * roots.Tissue[0].FractionWt[layer];
                fomData.C = amountDM * CarbonFractionInDM * roots.Tissue[0].FractionWt[layer];
                fomData.P = 0.0; // P not considered here
                fomData.AshAlk = 0.0; // Ash not considered here

                Soils.FOMLayerLayerType layerData = new Soils.FOMLayerLayerType();
                layerData.FOM = fomData;
                layerData.CNR = 0.0; // not used here
                layerData.LabileP = 0; // not used here

                FOMdataLayer[layer] = layerData;
            }

            if (IncorpFOM != null)
            {
                Soils.FOMLayerType FOMData = new Soils.FOMLayerType();
                FOMData.Type = mySpeciesFamily.ToString();
                FOMData.Layer = FOMdataLayer;
                IncorpFOM.Invoke(FOMData);
            }
        }

        #endregion

        #region - Handling and auxilary processes  -------------------------------------------------------------------------

        /// <summary>Refresh the value of several variables</summary>
        internal void RefreshVariables()
        {
            // reset some variables
            dmDefoliated = 0.0;
            Ndefoliated = 0.0;
            digestDefoliated = 0.0;
        }

        /// <summary>Stores the current state for this species</summary>
        internal void SaveState()
        {
            myPreviousState.leaves.Tissue[0].DM = leaves.Tissue[0].DM;
            myPreviousState.leaves.Tissue[1].DM = leaves.Tissue[1].DM;
            myPreviousState.leaves.Tissue[2].DM = leaves.Tissue[2].DM;
            myPreviousState.leaves.Tissue[3].DM = leaves.Tissue[3].DM;

            myPreviousState.stems.Tissue[0].DM = stems.Tissue[0].DM;
            myPreviousState.stems.Tissue[1].DM = stems.Tissue[1].DM;
            myPreviousState.stems.Tissue[2].DM = stems.Tissue[2].DM;
            myPreviousState.stems.Tissue[3].DM = stems.Tissue[3].DM;

            myPreviousState.stolons.Tissue[0].DM = stolons.Tissue[0].DM;
            myPreviousState.stolons.Tissue[1].DM = stolons.Tissue[1].DM;
            myPreviousState.stolons.Tissue[2].DM = stolons.Tissue[2].DM;

            for (int layer = 0; layer < nLayers; layer++)
                myPreviousState.roots.Tissue[0].DMLayer[layer] = roots.Tissue[0].DMLayer[layer];

            myPreviousState.leaves.Tissue[0].Namount = leaves.Tissue[0].Namount;
            myPreviousState.leaves.Tissue[1].Namount = leaves.Tissue[1].Namount;
            myPreviousState.leaves.Tissue[2].Namount = leaves.Tissue[2].Namount;
            myPreviousState.leaves.Tissue[3].Namount = leaves.Tissue[3].Namount;

            myPreviousState.stems.Tissue[0].Namount = stems.Tissue[0].Namount;
            myPreviousState.stems.Tissue[1].Namount = stems.Tissue[1].Namount;
            myPreviousState.stems.Tissue[2].Namount = stems.Tissue[2].Namount;
            myPreviousState.stems.Tissue[3].Namount = stems.Tissue[3].Namount;

            myPreviousState.stolons.Tissue[0].Namount = stolons.Tissue[0].Namount;
            myPreviousState.stolons.Tissue[1].Namount = stolons.Tissue[1].Namount;
            myPreviousState.stolons.Tissue[2].Namount = stolons.Tissue[2].Namount;

            myPreviousState.roots.Tissue[0].Namount = roots.Tissue[0].Namount;
        }

        /// <summary>Reduction factor for potential growth due to phenology of annual species</summary>
        /// <returns>A factor to reduce plant growth (0-1)</returns>
        private double annualSpeciesReduction()
        {
            double rFactor = 1.0;
            if (phenologicStage == 1 && daysfromEmergence < 60) //decline at the begining due to seed bank effects ???
                rFactor = 0.5 + 0.5 * daysfromEmergence / 60;
            else if (phenologicStage == 2) //decline of photosynthesis when approaching maturity
                rFactor = 1.0 - (double) daysfromAnthesis / daysAnthesisToMaturity;
            return rFactor;
        }

        /// <summary>Computes the variations in root depth and distribution</summary>
        /// <remarks>
        /// Root depth will increase is smaller than maximumRootDepth and there is a positive net DM accumulation.
        /// The increase is of zero-order, given by the RootElongationRate, but it is adjusted for temperature in
        /// a similar fashion as plant DM growth. Note that root depth will never decrease.
        /// The current changes whenever root depth changes, the new value is used to allocate new growth 
        /// to each layer within the root zone
        /// </remarks>
        private void EvaluateRootGrowth()
        {
            dRootDepth = 0.0;
            if (phenologicStage > 0)
            {
                if (((dGrowthRoot-dRootSen) > myEpsilon) && (roots.Depth < MaximumRootDepth))
                {
                    double tempFactor = TemperatureLimitingFactor(Tmean(0.5));
                    dRootDepth = RootElongationRate * tempFactor;
                    roots.Depth = Math.Min(MaximumRootDepth, Math.Max(MinimumRootDepth, roots.Depth + dRootDepth));
                    roots.BottomLayer = RootZoneBottomLayer();
                }
                else
                {
                    // No net growth
                    dRootDepth = 0.0;
                }

                // change root amount due to senescence (no changes in distribution)
                if (dRootSen > myEpsilon)
                    roots.Tissue[0].DM -= dRootSen;

                // change root amount due to growth (distribution may change)
                double[] currentRootTarget = CurrentRootDistributionTarget();
                if (MathUtilities.AreEqual(roots.Tissue[0].FractionWt, currentRootTarget))
                {
                    // No need to change the distribution
                    roots.Tissue[0].DM += dGrowthRoot;
                }
                else
                {
                    // 1. get preliminary distribution, based on average of current and target
                    double[] newRootLayerWt = new double[roots.BottomLayer + 1];
                    double newRootWt = roots.Tissue[0].DM + dGrowthRoot;
                    for (int layer = 0; layer <= roots.BottomLayer; layer++)
                        newRootLayerWt[layer] = newRootWt * (roots.Tissue[0].FractionWt[layer] + currentRootTarget[layer]) / 2.0;

                    // 2. check for excess allocation
                    if (newRootLayerWt.Sum() > newRootWt)
                    {
                        // 3a. adjsut distribution and update root DM
                        double layersTotal = newRootLayerWt.Sum();
                        for (int layer = 0; layer <= roots.BottomLayer; layer++)
                            roots.Tissue[0].DMLayer[layer] = newRootWt * (newRootLayerWt[layer] / layersTotal);
                    }
                    else
                    {
                        // 3b. update root DM 
                        for (int layer = 0; layer <= roots.BottomLayer; layer++)
                            roots.Tissue[0].DMLayer[layer] = newRootLayerWt[layer];
                    }
                }
            }
        }

        /// <summary>Computes the values of LAI (leaf area index) for green and dead plant material</summary>
        private void EvaluateLAI()
        {
            double greenTissue = leaves.DMGreen + (stolons.DMGreen * 0.3); // assuming stolons have 0.3*SLA
            greenTissue /= 10000; // converted from kg/ha to kg/m2
            greenLAI = greenTissue * SpecificLeafArea;

            // Adjust helping to account for resilience after unfavoured conditions
            if (!isLegume && AboveGroundLivedWt < 1000)
            {
                double maxStemLAI = Math.Sqrt(0.1); //TODO make this a parameter
                greenTissue = stems.DMGreen / 10000;
                greenLAI += greenTissue * SpecificLeafArea * Math.Sqrt((1000 - AboveGroundLivedWt) / 10000);
                //greenLAI += greenTissue * maxStemLAI * SpecificLeafArea * Math.Sqrt((1000 - AboveGroundLivedWt) / 1000);
            }
            /* 
             This adjust assumes cover will be bigger for the same amount of DM when DM is low, due to:
             - greater light extinction coefficient, leaves will be more horizontal than in dense high swards
             - more parts (stems) will turn green for photosysnthesis (?)
             - thinner leaves during growth burst following unfavoured conditions
             » Specific leaf area should be reduced (RCichota2014) - TODO
             */

            deadLAI = (leaves.DMDead / 10000) * SpecificLeafArea;
        }

        /// <summary>Compute the average digestibility of aboveground plant material</summary>
        /// <returns>The digestibility of plant material (0-1)</returns>
        private void EvaluateDigestibility()
        {
            double result = 0.0;
            if (AboveGroundWt > 0.0)
            {
                result = (leaves.DigestibilityTotal * leaves.DMTotal)
                       + (stems.DigestibilityTotal * stems.DMTotal)
                       + (stolons.DigestibilityTotal * stolons.DMTotal);
                result /= AboveGroundWt;
            }

            digestHerbage = result;
        }

        /// <summary>Compute the average digestibility of harvested plant material</summary>
        /// <param name="leafLiveWt">removed DM of live leaves</param>
        /// <param name="leafDeadWt">removed DM of dead leaves</param>
        /// <param name="stemLiveWt">removed DM of live stems</param>
        /// <param name="stemDeadWt">removed DM of dead stems</param>
        /// <param name="stolonLiveWt">removed DM of live stolons</param>
        /// <param name="stolonDeadWt">removed DM of dead stolons</param>
        /// <returns>The digestibility of plant material (0-1)</returns>
        private double calcHarvestDigestibility(double leafLiveWt, double leafDeadWt, double stemLiveWt, double stemDeadWt, double stolonLiveWt, double stolonDeadWt)
        {
            double result = 0.0;
            double removedWt = leafLiveWt + leafDeadWt + stemLiveWt + stemDeadWt + stolonLiveWt + stolonDeadWt;
            if (removedWt > 0.0)
            {
                result = (leaves.DigestibilityLive * leafLiveWt) + (leaves.DigestibilityDead * leafDeadWt)
                       + (stems.DigestibilityLive * stemLiveWt) + (stems.DigestibilityDead * stemDeadWt)
                       + (stolons.DigestibilityLive * stolonLiveWt) + (stolons.DigestibilityDead * stolonDeadWt);
                result /= removedWt;
            }

            return result;
        }

        #endregion

        #endregion

        #region Other processes  -------------------------------------------------------------------------------------------

        /// <summary>Harvests the specified type.</summary>
        /// <param name="type">The type.</param>
        /// <param name="amount">The amount.</param>
        public void Harvest(string type, double amount)
        {
            GrazeType GrazeData = new GrazeType();
            GrazeData.amount = amount;
            GrazeData.type = type;
            OnGraze(GrazeData);
        }

        /// <summary>Called when [graze].</summary>
        /// <param name="GrazeData">The graze data.</param>
        [EventSubscribe("Graze")]
        private void OnGraze(GrazeType GrazeData)
        {
            if ((!isAlive) || StandingWt == 0)
                return;

            // get the amount required to remove
            double amountRequired = 0.0;
            if (GrazeData.type.ToLower() == "SetResidueAmount".ToLower())
            {
                // Remove all DM above given residual amount
                amountRequired = Math.Max(0.0, StandingWt - GrazeData.amount);
            }
            else if (GrazeData.type.ToLower() == "SetRemoveAmount".ToLower())
            {
                // Attempt to remove a given amount
                amountRequired = Math.Max(0.0, GrazeData.amount);
            }
            else
            {
                Console.WriteLine("  AgPasture - Method to set amount to remove not recognized, command will be ignored");
            }
            // get the actual amount to remove
            double amountToRemove = Math.Min(amountRequired, HarvestableWt);

            // Do the actual removal
            if (amountRequired > 0.0)
                RemoveDM(amountToRemove);
        }

        /// <summary>
        /// Remove a given amount of DM (and N) from this plant (consider preferences for green/dead material)
        /// </summary>
        /// <param name="AmountToRemove">Amount to remove (kg/ha)</param>
        /// <exception cref="System.Exception">   + Name +  - removal of DM resulted in loss of mass balance</exception>
        public void RemoveDM(double AmountToRemove)
        {
            // check existing amount and what is harvestable
            double PreRemovalDM = AboveGroundWt; // TODO: enable removal of stolons
            double PreRemovalN = AboveGroundN;

            if (HarvestableWt > 0.0)
            {
                // get the DM weights for each pool, consider preference and available DM
                double tempPrefGreen = RelativePreferenceForGreen + (AmountToRemove / HarvestableWt);
                double tempPrefDead = 1.0 + (RelativePreferenceForGreen * (AmountToRemove / HarvestableWt));
                double tempRemovableGreen = Math.Max(0.0, StandingLiveWt - MinimumGreenWt);
                double tempRemovableDead = StandingDeadWt;

                // get partiton between dead and live materials
                double tempTotal = tempRemovableGreen * tempPrefGreen + tempRemovableDead * tempPrefDead;
                double fractionToHarvestGreen = 0.0;
                double fractionToHarvestDead = 0.0;
                if (tempTotal > 0.0)
                {
                    fractionToHarvestGreen = tempRemovableGreen * tempPrefGreen / tempTotal;
                    fractionToHarvestDead = tempRemovableDead * tempPrefDead / tempTotal;
                }

                // get amounts removed
                double RemovingGreenDM = AmountToRemove * fractionToHarvestGreen;
                double RemovingDeadDM = AmountToRemove * fractionToHarvestDead;

                // Fraction of DM remaining in the field
                double fractionRemainingGreen = 1.0;
                if (StandingLiveWt > 0.0)
                    fractionRemainingGreen = Math.Max(0.0, Math.Min(1.0, 1.0 - RemovingGreenDM / StandingLiveWt));
                double fractionRemainingDead = 1.0;
                if (StandingDeadWt > 0.0)
                    fractionRemainingDead = Math.Max(0.0, Math.Min(1.0, 1.0 - RemovingDeadDM / StandingDeadWt));

                // get digestibility of DM being harvested
                digestDefoliated = calcHarvestDigestibility(leaves.DMGreen * fractionToHarvestGreen, leaves.DMDead * fractionToHarvestDead,
                                                            stems.DMGreen * fractionToHarvestGreen, stems.DMDead * fractionToHarvestDead,
                                                            stolons.DMGreen * fractionToHarvestGreen, stolons.DMDead * fractionToHarvestDead);

                // update the various pools
                leaves.Tissue[0].DM *= fractionRemainingGreen;
                leaves.Tissue[1].DM *= fractionRemainingGreen;
                leaves.Tissue[2].DM *= fractionRemainingGreen;
                leaves.Tissue[3].DM *= fractionRemainingDead;
                stems.Tissue[0].DM *= fractionRemainingGreen;
                stems.Tissue[1].DM *= fractionRemainingGreen;
                stems.Tissue[2].DM *= fractionRemainingGreen;
                stems.Tissue[3].DM *= fractionRemainingDead;
                //No stolon remove

                // N remove
                leaves.Tissue[0].Namount *= fractionRemainingGreen;
                leaves.Tissue[1].Namount *= fractionRemainingGreen;
                leaves.Tissue[2].Namount *= fractionRemainingGreen;
                leaves.Tissue[3].Namount *= fractionRemainingDead;
                stems.Tissue[0].Namount *= fractionRemainingGreen;
                stems.Tissue[1].Namount *= fractionRemainingGreen;
                stems.Tissue[2].Namount *= fractionRemainingGreen;
                stems.Tissue[3].Namount *= fractionRemainingDead;

                //C and N remobilised are also removed proportionally
                NRemobilisable *= fractionRemainingGreen;
                CRemobilisable *= fractionRemainingGreen;

                // update Luxury N pools
                NLuxury2 *= fractionRemainingGreen;
                NLuxury3 *= fractionRemainingGreen;

                // check mass balance and set outputs
                dmDefoliated = PreRemovalDM - AboveGroundWt;
                Ndefoliated = PreRemovalN - AboveGroundN;
                if (Math.Abs(dmDefoliated - AmountToRemove) > 0.00001)
                    throw new Exception("  " + Name + " - removal of DM resulted in loss of mass balance");
            }
        }

        /// <summary>Remove biomass from plant</summary>
        /// <param name="RemovalData">Info about what and how much to remove</param>
        /// <remarks>Greater details on how much and which parts are removed is given</remarks>
        [EventSubscribe("RemoveCropBiomass")]
        private void Onremove_crop_biomass(RemoveCropBiomassType RemovalData)
        {
            // NOTE: It is responsability of the calling module to check that the amount of 
            //  herbage in each plant part is correct
            // No checking if the removing amount passed in are too much here

            // ATTENTION: The amounts passed should be in g/m^2

            double fractionToRemove = 0.0;


            // get digestibility of DM being removed
            digestDefoliated = calcHarvestDigestibility(leaves.DMGreen * fractionToRemove, leaves.DMDead * fractionToRemove,
                                                        stems.DMGreen * fractionToRemove, stems.DMDead * fractionToRemove,
                                                        stolons.DMGreen * fractionToRemove, stolons.DMDead * fractionToRemove);

            for (int i = 0; i < RemovalData.dm.Length; i++) // for each pool (green or dead)
            {
                string plantPool = RemovalData.dm[i].pool;
                for (int j = 0; j < RemovalData.dm[i].dlt.Length; j++) // for each part (leaf or stem)
                {
                    string plantPart = RemovalData.dm[i].part[j];
                    double amountToRemove = RemovalData.dm[i].dlt[j] * 10.0; // convert to kgDM/ha
                    if (plantPool.ToLower() == "green" && plantPart.ToLower() == "leaf")
                    {
                        if (LeafGreenWt - amountToRemove > 0.0)
                        {
                            fractionToRemove = MathUtilities.Divide(amountToRemove, LeafGreenWt, 0.0);
                            RemoveFractionDM(fractionToRemove, plantPool, plantPart);
                        }
                    }
                    else if (plantPool.ToLower() == "green" && plantPart.ToLower() == "stem")
                    {
                        if (StemGreenWt - amountToRemove > 0.0)
                        {
                            fractionToRemove = MathUtilities.Divide(amountToRemove, StemGreenWt, 0.0);
                            RemoveFractionDM(fractionToRemove, plantPool, plantPart);
                        }
                    }
                    else if (plantPool.ToLower() == "dead" && plantPart.ToLower() == "leaf")
                    {
                        if (LeafDeadWt - amountToRemove > 0.0)
                        {
                            fractionToRemove = MathUtilities.Divide(amountToRemove, LeafDeadWt, 0.0);
                            RemoveFractionDM(fractionToRemove, plantPool, plantPart);
                        }
                    }
                    else if (plantPool.ToLower() == "dead" && plantPart.ToLower() == "stem")
                    {
                        if (StemDeadWt - amountToRemove > 0.0)
                        {
                            fractionToRemove = MathUtilities.Divide(amountToRemove, StemDeadWt, 0.0);
                            RemoveFractionDM(fractionToRemove, plantPool, plantPart);
                        }
                    }
                }
            }
            RefreshAfterRemove();
        }

        /// <summary>Remove a fraction of DM from a given plant part</summary>
        /// <param name="fractionR">The fraction of DM and N to remove</param>
        /// <param name="pool">The pool to remove from (green or dead)</param>
        /// <param name="part">The part to remove from (leaf or stem)</param>
        public void RemoveFractionDM(double fractionR, string pool, string part)
        {
            if (pool.ToLower() == "green")
            {
                if (part.ToLower() == "leaf")
                {
                    // removing green leaves
                    dmDefoliated += LeafGreenWt * fractionR;
                    Ndefoliated += LeafGreenN * fractionR;

                    leaves.Tissue[0].DM *= fractionR;
                    leaves.Tissue[1].DM *= fractionR;
                    leaves.Tissue[2].DM *= fractionR;

                    leaves.Tissue[0].Namount *= fractionR;
                    leaves.Tissue[1].Namount *= fractionR;
                    leaves.Tissue[2].Namount *= fractionR;
                }
                else if (part.ToLower() == "stem")
                {
                    // removing green stems
                    dmDefoliated += StemGreenWt * fractionR;
                    Ndefoliated += StemGreenN * fractionR;

                    stems.Tissue[0].DM *= fractionR;
                    stems.Tissue[1].DM *= fractionR;
                    stems.Tissue[2].DM *= fractionR;

                    stems.Tissue[0].Namount *= fractionR;
                    stems.Tissue[1].Namount *= fractionR;
                    stems.Tissue[2].Namount *= fractionR;
                }
            }
            else if (pool.ToLower() == "green")
            {
                if (part.ToLower() == "leaf")
                {
                    // removing dead leaves
                    dmDefoliated += LeafDeadWt * fractionR;
                    Ndefoliated += LeafDeadN * fractionR;

                    leaves.Tissue[3].DM *= fractionR;
                    leaves.Tissue[3].Namount *= fractionR;
                }
                else if (part.ToLower() == "stem")
                {
                    // removing dead stems
                    dmDefoliated += StemDeadWt * fractionR;
                    Ndefoliated += StemDeadN * fractionR;

                    stems.Tissue[3].DM *= fractionR;
                    stems.Tissue[3].Namount *= fractionR;
                }
            }
        }

        /// <summary>Performs few actions to update variables after RemoveFractionDM</summary>
        public void RefreshAfterRemove()
        {
            // set values for fractionHarvest (in fact fraction harvested)
            fractionHarvested = MathUtilities.Divide(dmDefoliated, StandingWt + dmDefoliated, 0.0);

            // Update LAI
            EvaluateLAI();

            // Update digestibility
            EvaluateDigestibility();
        }

        /// <summary>Reset this plant state to its initial values</summary>
        public void Reset()
        {
            SetInitialState();
            myPreviousState = new SpeciesStateParameters(nLayers);
        }

        /// <summary>Kills this plant (zero all variables and set to not alive)</summary>
        /// <param name="KillData">Fraction of crop to kill (here always 100%)</param>
        [EventSubscribe("KillCrop")]
        public void OnKillCrop(KillCropType KillData)
        {
            // Return all above ground parts to surface OM
            DoSurfaceOMReturn(AboveGroundWt, AboveGroundN);

            // Incorporate all root mass to soil fresh organic matter
            DoIncorpFomEvent(BelowGroundWt, BelowGroundN);

            ResetZero();

            isAlive = false;
            phenologicStage = -1;
        }

        /// <summary>Reset this plant to zero (kill crop)</summary>
        public void ResetZero()
        {
            // Zero out the DM pools
            leaves.Tissue[0].DM = leaves.Tissue[1].DM = leaves.Tissue[2].DM = leaves.Tissue[3].DM = 0.0;
            stems.Tissue[0].DM = stems.Tissue[1].DM = stems.Tissue[2].DM = stems.Tissue[3].DM = 0.0;
            stolons.Tissue[0].DM = stolons.Tissue[1].DM = stolons.Tissue[2].DM = 0.0;
            roots.Tissue[0].DM = 0.0;
            dmDefoliated = 0.0;

            // Zero out the N pools
            leaves.Tissue[0].Namount = leaves.Tissue[1].Namount = leaves.Tissue[2].Namount = leaves.Tissue[3].Namount = 0.0;
            stems.Tissue[0].Namount = stems.Tissue[1].Namount = stems.Tissue[2].Namount = stems.Tissue[3].Namount = 0.0;
            stolons.Tissue[0].Namount = stolons.Tissue[1].Namount = stolons.Tissue[2].Namount = 0.0;
            roots.Tissue[0].Namount = 0.0;
            Ndefoliated = 0.0;

            digestDefoliated = 0.0;

            myPreviousState = new SpeciesStateParameters(nLayers);
        }

        #endregion

        #region Functions  -------------------------------------------------------------------------------------------------

        /// <summary>Today's weighted average temperature</summary>
        /// <param name="wTmax">Weight to Tmax</param>
        /// <returns>Mean Temperature</returns>
        private double Tmean(double wTmax)
        {
            wTmax = MathUtilities.Bound(wTmax, 0.0, 1.0);
            return (myMetData.MaxT * wTmax) + (myMetData.MinT * (1.0 - wTmax));
        }

        /// <summary>Placeholder for SoilArbitrator</summary>
        /// <param name="soilstate">soilstate</param>
        /// <returns></returns>
        public List<ZoneWaterAndN> GetSWUptakes(SoilState soilstate)
        {
            //throw new NotImplementedException();
            if (IsAlive)
            {
                // Model can only handle one root zone at present
                ZoneWaterAndN MyZone = new ZoneWaterAndN();
                Zone ParentZone = Apsim.Parent(this, typeof (Zone)) as Zone;
                foreach (ZoneWaterAndN Z in soilstate.Zones)
                    if (Z.Name == ParentZone.Name)
                        MyZone = Z;

                double[] supply = GetSoilAvailableWater();
                double Supply = supply.Sum();
                double Demand = myWaterDemand;
                double FractionUsed = 0.0;
                if (Supply > 0.0)
                    FractionUsed = Math.Min(1.0, Demand / Supply);

                // Just send uptake from my zone
                ZoneWaterAndN uptake = new ZoneWaterAndN();
                uptake.Name = MyZone.Name;
                uptake.Water = MathUtilities.Multiply_Value(supply, FractionUsed);
                uptake.NO3N = new double[nLayers];
                uptake.NH4N = new double[nLayers];

                List<ZoneWaterAndN> zones = new List<ZoneWaterAndN>();
                zones.Add(uptake);
                return zones;
            }
            else
                return null;
        }

        /// <summary>Placeholder for SoilArbitrator</summary>
        /// <param name="soilstate">soilstate</param>
        /// <returns></returns>
        public List<ZoneWaterAndN> GetNUptakes(SoilState soilstate)
        {
            //throw new NotImplementedException();

            if (IsAlive)
            {
                // Model can only handle one root zone at present
                ZoneWaterAndN MyZone = new ZoneWaterAndN();
                Zone ParentZone = Apsim.Parent(this, typeof (Zone)) as Zone;
                foreach (ZoneWaterAndN Z in soilstate.Zones)
                    if (Z.Name == ParentZone.Name)
                        MyZone = Z;

                ZoneWaterAndN UptakeDemands = new ZoneWaterAndN();
                GetPotentialNitrogenUptake(soilstate);

                //Pack results into uptake structure
                UptakeDemands.Name = MyZone.Name;
                UptakeDemands.NH4N = myPotentialNH4NUptake;
                UptakeDemands.NO3N = myPotentialNO3NUptake;
                UptakeDemands.Water = new double[nLayers];

                List<ZoneWaterAndN> zones = new List<ZoneWaterAndN>();
                zones.Add(UptakeDemands);
                return zones;
            }
            else
                return null;
        }

        /// <summary>
        /// Set the sw uptake for today
        /// </summary>
        public void SetSWUptake(List<ZoneWaterAndN> zones)
        {
            // Model can only handle one root zone at present
            ZoneWaterAndN MyZone = new ZoneWaterAndN();
            Zone ParentZone = Apsim.Parent(this, typeof (Zone)) as Zone;
            foreach (ZoneWaterAndN Z in zones)
                if (Z.Name == ParentZone.Name)
                    MyZone = Z;

            uptakeWater = new double[nLayers];
            for (int layer = 0; layer < nLayers; layer++)
                uptakeWater[layer] = MyZone.Water[layer];
        }

        /// <summary>
        /// Set the n uptake for today
        /// </summary>
        public void SetNUptake(List<ZoneWaterAndN> zones)
        {
            // Model can only handle one root zone at present
            ZoneWaterAndN MyZone = new ZoneWaterAndN();
            Zone ParentZone = Apsim.Parent(this, typeof (Zone)) as Zone;
            foreach (ZoneWaterAndN Z in zones)
                if (Z.Name == ParentZone.Name)
                    MyZone = Z;

            uptakeNH4 = new double[nLayers];
            uptakeNO3 = new double[nLayers];
            for (int layer = 0; layer < nLayers; layer++)
            {
                uptakeNH4[layer] = MyZone.NH4N[layer];
                uptakeNO3[layer] = MyZone.NO3N[layer];
            }
        }

        /// <summary>Growth limiting factor due to temperature</summary>
        /// <param name="Temp">Temperature for which the limiting factor will be computed</param>
        /// <returns>The value for the limiting factor (0-1)</returns>
        /// <exception cref="System.Exception">Photosynthesis pathway is not valid</exception>
        private double TemperatureLimitingFactor(double Temp)
        {
            double result = 0.0;
            double growthTmax = GrowthTopt + (GrowthTopt - GrowthTmin) / GrowthTq;
            if (PhotosynthesisPathway == PhotosynthesisPathwayType.C3)
            {
                if (Temp > GrowthTmin && Temp < growthTmax)
                {
                    double val1 = Math.Pow((Temp - GrowthTmin), GrowthTq) * (growthTmax - Temp);
                    double val2 = Math.Pow((GrowthTopt - GrowthTmin), GrowthTq) * (growthTmax - GrowthTopt);
                    result = val1 / val2;
                }
            }
            else if (PhotosynthesisPathway == PhotosynthesisPathwayType.C4)
            {
                if (Temp > GrowthTmin)
                {
                    if (Temp > GrowthTopt)
                        Temp = GrowthTopt;

                    double val1 = Math.Pow((Temp - GrowthTmin), GrowthTq) * (growthTmax - Temp);
                    double val2 = Math.Pow((GrowthTopt - GrowthTmin), GrowthTq) * (growthTmax - GrowthTopt);
                    result = val1 / val2;
                }
            }
            else
                throw new Exception("Photosynthesis pathway is not valid");
            return result;
        }

        /// <summary>Effect of temperature on tissue turnover</summary>
        /// <param name="Temp">The temporary.</param>
        /// <returns>Temperature factor (0-1)</returns>
        private double TempFactorForTissueTurnover(double Temp)
        {
            double result = 0.0;
            if (Temp > TissueTurnoverTmin && Temp <= TissueTurnoverTopt)
            {
                result = (Temp - TissueTurnoverTmin) / (TissueTurnoverTopt - TissueTurnoverTmin);  // TODO: implement power function
            }
            else if (Temp > TissueTurnoverTopt)
            {
                result = 1.0;
            }
            return result;
        }

        /// <summary>Photosynthesis reduction factor due to high temperatures (heat stress)</summary>
        /// <returns>The reduction in photosynthesis rate (0-1)</returns>
        private double HeatStress()
        {
            // evaluate recovery from the previous high temperature effects
            double recoverF = 1.0;

            if (highTempEffect < 1.0)
            {
                if (HeatRecoverT > Tmean(0.5))
                    accumT4Heat += (HeatRecoverT - Tmean(0.5));

                if (accumT4Heat < HeatSumT)
                    recoverF = highTempEffect + (1 - highTempEffect) * accumT4Heat / HeatSumT;
            }

            // Evaluate the high temperature factor for today
            double newHeatF = 1.0;
            if (myMetData.MaxT > HeatFullT)
                newHeatF = 0;
            else if (myMetData.MaxT > HeatOnsetT)
                newHeatF = (myMetData.MaxT - HeatOnsetT) / (HeatFullT - HeatOnsetT);

            // If this new high temp. factor is smaller than 1.0, then it is compounded with the old one
            // also, the cumulative heat for recovery is re-started
            if (newHeatF < 1.0)
            {
                highTempEffect = recoverF * newHeatF;
                accumT4Heat = 0;
                recoverF = highTempEffect;
            }

            return recoverF; // TODO: revise this function
        }

        /// <summary>Photosynthesis reduction factor due to low temperatures (cold stress)</summary>
        /// <returns>The reduction in potosynthesis rate (0-1)</returns>
        private double ColdStress()
        {
            //recover from the previous high temp. effect
            double recoverF = 1.0;
            if (lowTempEffect < 1.0)
            {
                if (Tmean(0.5) > ColdRecoverT)
                    accumT4Cold += (Tmean(0.5) - ColdRecoverT);

                if (accumT4Cold < ColdSumT)
                    recoverF = lowTempEffect + (1 - lowTempEffect) * accumT4Cold / ColdSumT;
            }

            //possible new low temp. effect
            double newColdF = 1.0;
            if (myMetData.MinT < ColdFullT)
                newColdF = 0;
            else if (myMetData.MinT < ColdOnsetT)
                newColdF = (myMetData.MinT - ColdFullT) / (ColdOnsetT - ColdFullT);

            // If this new cold temp. effect happens when serious cold effect is still on,
            // compound & then re-start of the recovery from the new effect
            if (newColdF < 1.0)
            {
                lowTempEffect = newColdF * recoverF;
                accumT4Cold = 0;
                recoverF = lowTempEffect;
            }

            return recoverF; // TODO: revise this function
        }

        /// <summary>Photosynthesis factor (reduction or increase) to eleveated [CO2]</summary>
        /// <returns>A factor to adjust photosynthesis due to CO2</returns>
        private double PCO2Effects()
        {
            if (Math.Abs(myMetData.CO2 - ReferenceCO2) < 0.01)
                return 1.0;

            double Fp1 = myMetData.CO2 / (CoefficientCO2EffectOnPhotosynthesis + myMetData.CO2);
            double Fp2 = (ReferenceCO2 + CoefficientCO2EffectOnPhotosynthesis) / ReferenceCO2;

            return Fp1 * Fp2;
        }

        /// <summary>Effect on photosynthesis due to variations in optimum N concentration as affected by CO2</summary>
        /// <returns>A factor to adjust photosynthesis</returns>
        private double PmxNeffect()
        {
            if (isAnnual)
                return 0.0;
            else
            {
                double fN = NCO2Effects();

                double result = 1.0;
                if (leaves.NconcGreen < leaves.NConcOptimum * fN)
                {
                    if (leaves.NconcGreen > leaves.NConcMinimum)
                    {
                        result = MathUtilities.Divide(leaves.NconcGreen - leaves.NConcMinimum, (leaves.NConcOptimum * fN) - leaves.NConcMinimum, 1.0);
                        result = Math.Min(1.0, Math.Max(0.0, result));
                    }
                    else
                    {
                        result = 0.0;
                    }
                }

                return result;
            }
        }

        /// <summary>Plant nitrogen [N] decline to elevated [CO2]</summary>
        /// <returns>A factor to adjust N demand</returns>
        private double NCO2Effects()
        {
            if (Math.Abs(myMetData.CO2 - ReferenceCO2) < 0.01)
                return 1.0;

            double termK = Math.Pow(OffsetCO2EffectOnNuptake - ReferenceCO2, ExponentCO2EffectOnNuptake);
            double termC = Math.Pow(myMetData.CO2 - ReferenceCO2, ExponentCO2EffectOnNuptake);
            double result = (1 - MinimumCO2EffectOnNuptake) * termK / (termK + termC);

            return MinimumCO2EffectOnNuptake + result;
        }

        //Canopy conductance decline to elevated [CO2]
        /// <summary>Conductances the c o2 effects.</summary>
        /// <returns></returns>
        private double ConductanceCO2Effects()
        {
            if (Math.Abs(myMetData.CO2 - ReferenceCO2) < 0.5)
                return 1.0;
            //Hard coded here, not used, should go to Micromet!   - TODO
            double Gmin = 0.2;      //Fc = Gmin when CO2->unlimited
            double Gmax = 1.25;     //Fc = Gmax when CO2 = 0;
            double beta = 2.5;      //curvature factor,

            double aux1 = (1 - Gmin) * Math.Pow(ReferenceCO2, beta);
            double aux2 = (Gmax - 1) * Math.Pow(myMetData.CO2, beta);
            double Fc = (Gmax - Gmin) * aux1 / (aux2 + aux1);
            return Gmin + Fc;
        }

        /// <summary>Growth limiting factor due to soil moisture deficit</summary>
        /// <returns>The limiting factor due to soil water deficit (0-1)</returns>
        internal double WaterDeficitFactor()
        {
            double result = 0.0;

            if (myWaterDemand <= myEpsilon)
                result = 1.0;
            else
                result = mySoilWaterTakenUp.Sum() / myWaterDemand;

            return Math.Max(0.0, Math.Min(1.0, result));
        }

        /// <summary>Growth limiting factor due to excess of water in soil (logging/lack of aeration)</summary>
        /// <returns>The limiting factor due to excess of soil water</returns>
        internal double WaterLoggingFactor()
        {
            double result = 1.0;

            // calculate soil moisture thresholds in the root zone
            double mySWater = 0.0;
            double mySaturation = 0.0;
            double myDUL = 0.0;
            double fractionLayer = 0.0;
            for (int layer = 0; layer < roots.BottomLayer; layer++)
            {
                // fraction of layer with roots 
                fractionLayer = FractionLayerWithRoots(layer);
                // actual soil water content
                mySWater += mySoil.Water[layer] * fractionLayer;
                // water content at saturation
                mySaturation += mySoil.SoilWater.SATmm[layer] * fractionLayer;
                // water content at low threshold for limitation (correspond to minimum water-free pore space)
                if (MinimumWaterFreePorosity <= -myEpsilon)
                    myDUL += mySoil.SoilWater.DULmm[layer] * fractionLayer;
                else
                    myDUL = mySoil.SoilWater.SATmm[layer] * (1.0 - 0.01 * MinimumWaterFreePorosity) * fractionLayer;
            }

            result = 1.0 - WaterLoggingCoefficient * Math.Max(0.0, mySWater - myDUL) / (mySaturation - myDUL);

            return result;
        }

        /// <summary>Effect of water stress on tissue turnover</summary>
        /// <returns>Water stress factor (0-1)</returns>
        private double WaterFactorForTissueTurnover()
        {
            double result = 1.0;
            if (glfWater < TissueTurnoverGLFWopt)
            {
                result = (TissueTurnoverGLFWopt - glfWater) / TissueTurnoverGLFWopt;
                result = (TissueTurnoverWFactorMax - 1.0) * result;
                result = Math.Min(TissueTurnoverWFactorMax, Math.Max(1.0, 1 + result));
            }

            return result;
        }

        /// <summary>Computes the ground cover for the plant, or plant part</summary>
        /// <param name="givenLAI">The LAI for this plant</param>
        /// <returns>Fraction of ground effectively covered (0-1)</returns>
        private double CalcPlantCover(double givenLAI)
        {
            if (givenLAI < myEpsilon) return 0.0;
            return (1.0 - Math.Exp(-LightExtentionCoefficient * givenLAI));
        }

        /// <summary>Compute the target (or ideal) distribution of roots in the soil profile</summary>
        /// <remarks>
        /// This distribution is solely based on root parameters (maximum depth and distribution parameters)
        /// These values will be used to allocate initial rootDM as well as any growth over the profile
        /// </remarks>
        /// <returns>A weighting factor for each soil layer (mm equivalent)</returns>
        private double[] RootDistributionTarget()
        {
            // 1. Base distribution calculated using a combination of linear and power functions:
            //  It considers homogeneous distribution from surface down to a fraction of root depth (DepthForConstantRootProportion),
            //   below this depth the proportion of root decrease following a power function (with exponent ExponentRootDistribution),
            //   it reaches zero slightly below the MaximumRootDepth (defined by rootBottomDistributionFactor), but the function is
            //   truncated at MaximumRootDepth. The values are not normalised.
            //  The values are further adjusted using the values of XF (so there will be less roots in those layers)

            double[] result = new double[nLayers];
            SoilCrop soilCropData = (SoilCrop) mySoil.Crop(Name);
            double depthTop = 0.0;
            double depthBottom = 0.0;
            double depthFirstStage = Math.Min(MaximumRootDepth, DepthForConstantRootProportion);

            for (int layer = 0; layer < nLayers; layer++)
            {
                depthBottom += mySoil.Thickness[layer];
                if (depthTop >= MaximumRootDepth)
                {
                    // totally out of root zone
                    result[layer] = 0.0;
                }
                else if (depthBottom <= depthFirstStage)
                {
                    // totally in the first stage
                    result[layer] = mySoil.Thickness[layer] * soilCropData.XF[layer];
                }
                else
                {
                    // at least partially on second stage
                    double maxRootDepth = MaximumRootDepth * rootBottomDistributionFactor;
                    result[layer] = Math.Pow(maxRootDepth - Math.Max(depthTop, depthFirstStage), ExponentRootDistribution + 1)
                                  - Math.Pow(maxRootDepth - Math.Min(depthBottom, MaximumRootDepth), ExponentRootDistribution + 1);
                    result[layer] /= (ExponentRootDistribution + 1) * Math.Pow(maxRootDepth - depthFirstStage, ExponentRootDistribution);
                    if (depthTop < depthFirstStage)
                    {
                        // partially in first stage
                        result[layer] += depthFirstStage - depthTop;
                    }

                    result[layer] *= soilCropData.XF[layer];
                }

                depthTop += mySoil.Thickness[layer];
            }

            return result;
        }

        /// <summary>Compute the current target distribution of roots in the soil profile</summary>
        /// <remarks>
        /// This distribution is a correction of the target distribution, taking into account the depth of soil
        /// as well as the current rooting depth
        /// </remarks>
        /// <returns>The proportion of root mass expected in each soil layer</returns>
        private double[] CurrentRootDistributionTarget()
        {
            double currentDepth = 0.0;
            double cumProportion = 0.0;
            for (int layer = 0; layer < nLayers; layer++)
            {
                if (currentDepth < roots.Depth)
                {
                    // layer is within the root zone
                    currentDepth += mySoil.Thickness[layer];
                    if (currentDepth <= roots.Depth)
                    {
                        // layer is fully in the root zone
                        cumProportion += roots.TargetDistribution[layer];
                    }
                    else
                    {
                        // layer is partially in the root zone
                        double layerFrac = (roots.Depth - (currentDepth - mySoil.Thickness[layer]))
                                         / (MaximumRootDepth - (currentDepth - mySoil.Thickness[layer]));
                        cumProportion += roots.TargetDistribution[layer] * Math.Min(1.0, Math.Max(0.0, layerFrac));
                    }
                }
                else
                    layer = nLayers;
            }

            double[] result = new double[nLayers];
            if (cumProportion > 0.0)
            {
                for (int layer = 0; layer < nLayers; layer++)
                    result[layer] = roots.TargetDistribution[layer] / cumProportion;
            }

            return result;
        }

        /// <summary>
        /// Compute how much of the layer is actually explored by roots (considering depth only)
        /// </summary>
        /// <param name="layer">The index for the layer being considered</param>
        /// <returns>Fraction of the layer in consideration that is explored by roots</returns>
        internal double FractionLayerWithRoots(int layer)
        {
            double fractionInLayer = 0.0;
            if (layer < roots.BottomLayer)
            {
                double depthTillTopThisLayer = 0.0;
                for (int z = 0; z < layer; z++)
                    depthTillTopThisLayer += mySoil.Thickness[z];
                fractionInLayer = (roots.Depth - depthTillTopThisLayer) / mySoil.Thickness[layer];
                fractionInLayer= Math.Min(1.0, Math.Max(0.0, fractionInLayer));
            }

            return fractionInLayer;
        }

        /// <summary>
        /// Compute the index of the layer at the bottom of the root zone
        /// </summary>
        /// <returns>Index of a layer</returns>
        private int RootZoneBottomLayer()
        {
            int result = 0;
            double currentDepth = 0.0;
            for (int layer = 0; layer < nLayers; layer++)
            {
                if (roots.Depth > currentDepth)
                {
                    result = layer;
                    currentDepth += mySoil.Thickness[layer];
                }
                else
                    layer = nLayers;
            }

            return result;
        }

        /// <summary>
        /// Plant height calculation based on DM
        /// </summary>
        /// <returns>Plant height (mm)</returns>
        private double HeightfromDM()
        {
            //double TodaysHeight = MaxPlantHeight[0] - MinimumHeight;
            double TodaysHeight = MaximumPlantHeight;

            if (0.1 * AboveGroundWt <= MassForMaximumHeight)
            {
                double myX = 0.1 * AboveGroundWt / MassForMaximumHeight;
                double heightF = ExponentHeightFromMass - (ExponentHeightFromMass * myX) + myX;
                heightF *= Math.Pow(myX, ExponentHeightFromMass - 1);
                TodaysHeight *= heightF;
            }
            //return TodaysHeight + MinimumHeight;
            return Math.Max(TodaysHeight, MinimumPlantHeight);
        }

        /// <summary>VPDs this instance.</summary>
        /// <returns></returns>
        /// The following helper functions [VDP and svp] are for calculating Fvdp
        private double VPD()
        {
            double VPDmint = svp(myMetData.MinT) - myMetData.VP;
            VPDmint = Math.Max(VPDmint, 0.0);

            double VPDmaxt = svp(myMetData.MaxT) - myMetData.VP;
            VPDmaxt = Math.Max(VPDmaxt, 0.0);

            double vdp = 0.66 * VPDmaxt + 0.34 * VPDmint;
            return vdp;
        }

        /// <summary>SVPs the specified temporary.</summary>
        /// <param name="temp">The temporary.</param>
        /// <returns></returns>
        private double svp(double temp)  // from Growth.for documented in MicroMet
        {
            return 6.1078 * Math.Exp(17.269 * temp / (237.3 + temp));
        }

        #endregion

        #region Auxiliary classes  ------------------------------------------------------------------------------------

        /// <summary>Stores the DM and N values of all plant parts</summary>
        [Serializable]
        private class SpeciesStateParameters
        {
            /// <summary>The state of leaves (DM and N)</summary>
            internal GenericAboveGroundOrgan leaves;

            /// <summary>The state of sheath/stems (DM and N)</summary>
            internal GenericAboveGroundOrgan stems;

            /// <summary>The state of stolons (DM and N)</summary>
            internal GenericAboveGroundOrgan stolons;

            /// <summary>The state of roots (DM and N)</summary>
            internal GenericBelowGroundOrgan roots;
            //internal RootOrgan roots;

            /// <summary>The constructor</summary>
            public SpeciesStateParameters(int numLayers)
            {
                leaves = new GenericAboveGroundOrgan(4);
                stems = new GenericAboveGroundOrgan(4);
                stolons = new GenericAboveGroundOrgan(3);
                roots = new GenericBelowGroundOrgan(1, numLayers);
            }

            /// <summary>The DM weight above-ground [g/m^2]</summary>
            internal double AboveGroundWt
            {
                get { return leaves.DMTotal + stems.DMTotal + stolons.DMGreen; }
            }

            /// <summary>The live DM weight above-ground [g/m^2]</summary>
            internal double AboveGroundLiveWt
            {
                get { return leaves.DMGreen + stems.DMGreen + stolons.DMGreen; }
            }

            /// <summary>The DM weight below-ground [g/m^2]</summary>
            internal double BelowGroundWt
            {
                get { return roots.DMGreen; }
            }

            /// <summary>The amount of N above ground [g/m^2]</summary>
            internal double AboveGroundN
            {
                get { return leaves.NTotal + stems.NTotal + stolons.NGreen; }
            }

            /// <summary>The amount of N below ground [g/m^2]</summary>
            internal double BelowGroundN
            {
                get { return roots.NGreen; }
            }

            ///// <summary>DM weight of defoliated material (g/m^2)</summary>
            //internal double dmdefoliated;

            ///// <summary>N in defoliated material (g/m^2)</summary>
            //internal double Ndefoliated;

            ///// <summary>N remobilsed from senesced tissue (g/m^2)</summary>
            //internal double Nremob;
        }

        /// <summary>Basic values defining the state of a pasture species</summary>
        [Serializable]
        internal class SpeciesBasicStateSettings
        {
            /// <summary>Plant phenologic stage</summary>
            internal int PhenoStage;

            /// <summary>DM weight for each biomass pool</summary>
            internal double[] DMWeight;

            /// <summary>N amount for each biomass pool</summary>
            internal double[] NAmount;

            /// <summary>Root depth</summary>
            internal double RootDepth;

            /// <summary>Constructor, initialise the arrays</summary>
            public SpeciesBasicStateSettings()
            {
                // there are 12 tissue pools, in order: leaf1, leaf2, leaf3, leaf4, stem1, stem2, stem3, stem4, stolon1, stolon2, stolon3, and root
                DMWeight = new double[12];
                NAmount = new double[12];
            }
        }

        #endregion
    }

    /// <summary>Defines a broken stick (piecewise) function</summary>
    [Serializable]
    public class BrokenStick
    {
        /// <summary>The x</summary>
        public double[] X;
        /// <summary>The y</summary>
        public double[] Y;

        /// <summary>Values the specified new x.</summary>
        /// <param name="newX">The new x.</param>
        /// <returns></returns>
        public double Value(double newX)
        {
            bool DidInterpolate = false;
            return MathUtilities.LinearInterpReal(newX, X, Y, out DidInterpolate);
        }
    }

    #region Plant organs and tissues  ---------------------------------------------------------------------------------

    /// <summary>
    /// Defines a basic generic above-ground organ of a pasture species
    /// </summary>
    /// <remarks>
    /// Each organ (leaf, stem, etc) is defined as a collection of tissues (limited to four)
    /// Leaves and stems have four tissues, stolons three and roots only one.
    /// Three tissues are alive (growing, developed and mature), the fourth represents dead material
    /// Each organ has its own nutrient concentration thresholds (min, max, opt)
    /// Each tissue has a record of DM and nutrient amounts, from which N concentration is computed
    /// The organ has methods to output DM and nutrient as total, live, and dead tissues
    /// The tissue has method to output is digestibility
    /// </remarks>
    [Serializable]
    public class GenericAboveGroundOrgan
    {
        /// <summary>Constructor, Initialise tissues</summary>
        public GenericAboveGroundOrgan(int numTissues)
        {
            DoInitialisation(numTissues);
        }

        /// <summary>Actuallly initialise the tissues</summary>
        internal void DoInitialisation(int numTissues)
        {
            // generally 4 tisues, three live, last is dead material
            TissueCount = numTissues;
            Tissue = new GenericTissue[TissueCount];
            for (int t = 0; t < TissueCount; t++)
                Tissue[t] = new GenericTissue();
        }

        /// <summary>the collection of tissues for this organ</summary>
        internal GenericTissue[] Tissue { get; set; }

        #region Organ Properties (summary of tissues)  ----------------------------------------------------------------

        /// <summary>Number of tissue pools to create</summary>
        internal int TissueCount;

        /// <summary>N concentration for optimal growth [g/g]</summary>
        internal double NConcOptimum = 4.0;

        /// <summary>Maximum N concentration, for luxury uptake [g/g]</summary>
        internal double NConcMaximum = 6.0;

        /// <summary>Minimum N concentration, structural N [g/g]</summary>
        internal double NConcMinimum = 1.2;

        /// <summary>Fraction of new growth that is soluble carbohydrate, i.e. sugars [0-1]</summary>
        internal double SugarFraction { get; set; } = 0.0;

        /// <summary>The total dry matter in this organ [g/m^2]</summary>
        internal double DMTotal
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < TissueCount; t++)
                {
                    result += Tissue[t].DM;
                }

                return result;
            }
        }

        /// <summary>The dry matter in the live (green) tissues [g/m^2]</summary>
        internal double DMGreen
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < TissueCount - 1; t++)
                {
                    result += Tissue[t].DM;
                }

                return result;
            }
        }

        /// <summary>The dry matter in the dead tissues [g/m^2]</summary>
        /// <remarks>Last tissues is assumed to represent dead material</remarks>
        internal double DMDead
        {
            get { return Tissue[TissueCount - 1].DM; }
        }

        /// <summary>The total N amount in this tissue [g/m^2]</summary>
        internal double NTotal
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < TissueCount; t++)
                {
                    result += Tissue[t].Namount;
                }

                return result;
            }
        }

        /// <summary>The N amount in the live (green) tissues [g/m^2]</summary>
        internal double NGreen
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < TissueCount - 1; t++)
                {
                    result += Tissue[t].Namount;
                }

                return result;
            }
        }

        /// <summary>The N amount in the dead tissues [g/m^2]</summary>
        /// <remarks>Last tissues is assumed to represent dead material</remarks>
        internal double NDead
        {
            get { return Tissue[TissueCount - 1].Namount; }
        }

        /// <summary>The average N concentration in this organ [g/g]</summary>
        internal double NconcTotal
        {
            get
            {
                return MathUtilities.Divide(NTotal, DMTotal, 0.0);
            }
        }

        /// <summary>The average N concentration in the live tissues [g/g]</summary>
        internal double NconcGreen
        {
            get
            {
                return MathUtilities.Divide(NGreen, DMGreen, 0.0);
            }
        }

        /// <summary>The average N concentration in dead tissues [g/g]</summary>
        internal double NconcDead
        {
            get
            {
                return MathUtilities.Divide(NDead, DMDead, 0.0);
            }
        }

        /// <summary>The average digestibility of all biomass for this organ [g/g]</summary>
        internal double DigestibilityTotal
        {
            get
            {
                double digestableDM = 0.0;
                for (int t = 0; t < TissueCount; t++)
                {
                    digestableDM += Tissue[t].Digestibility * Tissue[t].DM;
                }

                return MathUtilities.Divide(digestableDM, DMTotal, 0.0);
            }
        }

        /// <summary>The average digestibility of live biomass for this organ [g/g]</summary>
        internal double DigestibilityLive
        {
            get
            {
                double digestableDM = 0.0;
                for (int t = 0; t < TissueCount - 1; t++)
                {
                    digestableDM += Tissue[t].Digestibility * Tissue[t].DM;
                }

                return MathUtilities.Divide(digestableDM, DMGreen, 0.0);
            }
        }

        /// <summary>The average digestibility of dead biomass for this organ [g/g]</summary>
        /// <remarks>Last tissues is assumed to represent dead material</remarks>
        internal double DigestibilityDead
        {
            get { return Tissue[TissueCount - 1].Digestibility; }
        }

        #endregion

        /// <summary>Defines a generic plant tissue</summary>
        internal class GenericTissue
        {
            /// <summary>The dry matter amount [g/m^2]</summary>
            internal double DM { get; set; } = 0.0;

            /// <summary>The N content [g/m^2]</summary>
            internal double Namount { get; set; } = 0.0;

            /// <summary>The P content [g/m^2]</summary>
            internal double Pamount { get; set; } = 0.0;

            /// <summary>The nitrogen concentration [g/g]</summary>
            internal double Nconc
            {
                get { return MathUtilities.Divide(Namount, DM, 0.0); }
                set { Namount = value * DM; }
            }

            /// <summary>The phosphorus concentration [g/g]</summary>
            internal double Pconc
            {
                get { return MathUtilities.Divide(Pamount, DM, 0.0); }
                set { Pamount = value * DM; }
            }

            /// <summary>The digestibility of cell walls [0-1]</summary>
            internal double DigestibilityCellWall { get; set; } = 0.5;

            /// <summary>The digestibility of proteins [0-1]</summary>
            internal double DigestibilityProtein { get; set; } = 1.0;

            /// <summary>The amount of soluble carbohydrate, i.e. sugars [g/m^2]</summary>
            internal double DMSugar { get; set; } = 0.0;

            /// <summary>The dry matter amount [g/g]</summary>
            /// <remarks>Digestibility of sugars is assumed to be 100%</remarks>
            internal double Digestibility
            {
                get
                {
                    double tissueDigestibility = 0.0;
                    if (DM > 0.0)
                    {
                        double fractionSugar = DMSugar / DM;
                        double cnTissue = DM * CarbonFractionInDM / Namount;
                        double ratio1 = CNratioCellWall / cnTissue;
                        double ratio2 = CNratioCellWall / CNratioProtein;
                        double fractionProtein = (ratio1 - (1.0 - fractionSugar)) / (ratio2 - 1.0);
                        double fractionCellWall = 1.0 - fractionSugar - fractionProtein;
                        tissueDigestibility = fractionSugar
                                            + (fractionProtein * DigestibilityProtein)
                                            + (fractionCellWall * DigestibilityCellWall);
                    }

                    return tissueDigestibility;
                }
            }

            #region Constants  --------------------------------------------------------------------

            /// <summary>Average carbon content in plant dry matter</summary>
            const double CarbonFractionInDM = 0.4;

            /// <summary>The C:N ratio of protein</summary>
            const double CNratioProtein = 3.5;

            /// <summary>The C:N ratio of cell wall</summary>
            const double CNratioCellWall = 100.0;

            #endregion
        }
    }

    /// <summary>
    /// Defines a generic root organ of a pasture species
    /// </summary>
    /// <remarks>
    /// Contains the same properties of a BaseOrgan, extended by adding N thresholds
    /// and especially by defining the tissues as arrays (to store values by soil layer)
    /// </remarks>
    [Serializable]
    internal class GenericBelowGroundOrgan
    {
        /// <summary>Constructor, Initialise tissues</summary>
        public GenericBelowGroundOrgan(int numTissues, int numLayers)
        {
            DoInitialisation(numTissues, numLayers);
        }

        /// <summary>Actuallly initialise the tissues</summary>
        internal void DoInitialisation(int numTissues, int numLayers)
        {
            // Two tisses for root, live and dead
            TissueCount = numTissues;
            Tissue = new RootTissue[TissueCount];
            for (int t = 0; t < TissueCount; t++)
                Tissue[t] = new RootTissue(numLayers);
        }

        /// <summary>the collection of tissues for this organ</summary>
        internal RootTissue[] Tissue { get; set; }

        #region Organ Properties (summary of tissues)  ----------------------------------------------------------------

        /// <summary>Number of tissue pools to create</summary>
        internal int TissueCount;

        /// <summary>N concentration for optimal growth [g/g]</summary>
        internal double NConcOptimum = 1.5;

        /// <summary>Maximum N concentration, for luxury uptake [g/g]</summary>
        internal double NConcMaximum = 2.0;

        /// <summary>Minimum N concentration, structural N [g/g]</summary>
        internal double NConcMinimum = 1.0;

        /// <summary>The total dry matter in this organ [g/m^2]</summary>
        internal double DMTotal
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < TissueCount; t++)
                { 
                    result += Tissue[t].DM;
                }

                return result;
            }
        }

        /// <summary>The dry matter in the live tissues [g/m^2]</summary>
        internal double DMGreen
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < TissueCount - 1; t++)
                {
                    result += Tissue[t].DM;
                }

                return result;
            }
        }

        /// <summary>The dry matter in the dead tissues [g/m^2]</summary>
        /// <remarks>Last tissues is assumed to represent dead material</remarks>
        internal double DMDead
        {
            get { return Tissue[TissueCount - 1].DM; }
        }

        /// <summary>The total N amount in this tissue [g/m^2]</summary>
        internal double NTotal
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < TissueCount; t++)
                {
                    result += Tissue[t].Namount;
                }

                return result;
            }
        }

        /// <summary>The N amount in the live tissues [g/m^2]</summary>
        internal double NGreen
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < TissueCount - 1; t++)
                {
                    result += Tissue[t].Namount;
                }

                return result;
            }
        }

        /// <summary>The N amount in the dead tissues [g/m^2]</summary>
        /// <remarks>Last tissues is assumed to represent dead material</remarks>
        internal double NDead
        {
            get { return Tissue[TissueCount - 1].Namount; }
        }

        /// <summary>The average N concentration in this organ [g/g]</summary>
        internal double NconcTotal
        {
            get
            {
                return MathUtilities.Divide(NTotal, DMTotal, 0.0);
            }
        }

        /// <summary>The average N concentration in the live tissues [g/g]</summary>
        internal double NconcGreen
        {
            get
            {
                return MathUtilities.Divide(NGreen, DMGreen, 0.0);
            }
        }

        /// <summary>The average N concentration in dead tissues [g/g]</summary>
        internal double NconcDead
        {
            get
            {
                return MathUtilities.Divide(NDead, DMDead, 0.0);
            }
        }

        /// <summary>The rooting depth [mm]</summary>
        internal double Depth;

        /// <summary>The layer at the bottom of the root zone</summary>
        internal int BottomLayer;

        /// <summary>The target (ideal) dry matter fraction by layer [0-1]</summary>
        internal double[] TargetDistribution;

        #endregion

        /// <summary>Defines a generic root tissue</summary>
        internal class RootTissue
        {
            /// <summary>Constructor, initialise array</summary>
            /// <param name="numLayers">Number of layers in the soil</param>
            public RootTissue(int numLayers)
            {
                nlayers = numLayers;
                DMLayer = new double[nlayers];
                NamountLayer = new double[nlayers];
                PamountLayer = new double[nlayers];
            }

            /// <summary>The number of layers in the soil</summary>
            private int nlayers;

            /// <summary>The dry matter amount [g/m^2]</summary>
            internal double DM
            {
                get { return DMLayer.Sum(); }
                set
                {
                    double[] prevRootFraction = FractionWt;
                    for (int layer = 0; layer < nlayers; layer++)
                        DMLayer[layer] = value * prevRootFraction[layer];
                }
            }

            /// <summary>The dry matter amount by layer [g/m^2]</summary>
            internal double[] DMLayer;

            /// <summary>The N content [g/m^2]</summary>
            internal double Namount
            {
                get { return NamountLayer.Sum(); }
                set
                {
                    for (int layer = 0; layer < nlayers; layer++)
                        NamountLayer[layer] = value * FractionWt[layer];
                }
            }

            /// <summary>The N content by layer [g/m^2]</summary>
            internal double[] NamountLayer;

            /// <summary>The P content amount [g/m^2]</summary>
            internal double Pamount
            {
                get { return PamountLayer.Sum(); }
                set
                {
                    for (int layer = 0; layer < nlayers; layer++)
                        PamountLayer[layer] = value * FractionWt[layer];
                }
            }

            /// <summary>The P content by layer [g/m^2]</summary>
            internal double[] PamountLayer;

            /// <summary>The nitrogen concentration [g/g]</summary>
            internal double Nconc
            {
                get { return MathUtilities.Divide(Namount, DM, 0.0); }
                set { Namount = value * DM; }
            }

            /// <summary>The phosphorus concentration [g/g]</summary>
            internal double Pconc
            {
                get { return MathUtilities.Divide(Pamount, DM, 0.0); }
                set { Pamount = value * DM; }
            }

            /// <summary>The dry matter fraction by layer [0-1]</summary>
            internal double[] FractionWt
            {
                get
                {
                    double[] result = new double[nlayers];
                    for (int layer = 0; layer < nlayers; layer++)
                        result[layer] = MathUtilities.Divide(DMLayer[layer], DM, 0.0);
                    return result;
                }
            }
        }
    }

    /// <summary>Defines a generic above-ground organ of a pasture species</summary>
    internal class GenericAboveGroundOrganOld
    {
        /// <summary>The total luxury N amount in this organ (kg/ha)</summary>
        internal double NLuxuryTotal
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < 2; t++)
                {
                    result += 0;//Tissue[t].DM * Math.Max(0.0, Tissue[t].Nconc - NConcOptimum);
                }

                return result;
            }
        }

        /// <summary>The N amount in mature tissues taht can be reallocated (kg/ha)</summary>
        internal double NReallocatable
        {
            get
            {
                return 0;//Tissue[2].DM * Math.Max(0.0, Tissue[2].Nconc - NConcMinimum); 
            }
        }
    }

    #endregion
}
