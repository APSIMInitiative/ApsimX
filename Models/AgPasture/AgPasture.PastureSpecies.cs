//-----------------------------------------------------------------------
// <copyright file="AgPasture.PastureSpecies.cs" project="AgPasture" solution="APSIMx" company="APSIM Initiative">
//     Copyright (c) APSIM initiative. All rights reserved.
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
        [Description("The average height of plants (mm)")]
        [Units("mm")]
        public double Height
        {
            get { return HeightfromDM(); }
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
            // Return all above ground parts to surface OM
            DoSurfaceOMReturn(AboveGroundWt, AboveGroundN);

            // Incorporate all root mass to soil fresh organic matter
            DoIncorpFomEvent(BelowGroundWt, BelowGroundN);

            // zero all variables
            ResetZero();
            isAlive = false;
            phenologicStage = -1;
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
        [XmlIgnore]
        public double PhotosyntheticEfficiency { get; set; } = 0.01;

        /// <summary>Photosynthesis curvature parameter [J/kg/s]</summary>
        [XmlIgnore]
        public double PhotosynthesisCurveFactor { get; set; } = 0.8;

        /// <summary>Fraction of total radiation that is photosynthetically active [0-1]</summary>
        [XmlIgnore]
        private double fractionPAR { get; set; } = 0.5;

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

        /// <summary>Reference CO2 concentration for photosynthesis [ppm]</summary>
        [Description("Reference CO2 concentration for photosynthesis [ppm]:")]
        [Units("ppm")]
        public double ReferenceCO2 { get; set; } = 380.0;

        /// <summary> Coefficient for the function describing the CO2 effect on photosynthesis [ppm CO2]</summary>
        [Description("Coefficient for the function describing the CO2 effect on photosynthesis [ppm CO2]:")]
        [Units("ppm")]
        public double CO2EffectScaleFactor { get; set; } = 700.0;

        /// <summary>Scalling paramenter for the CO2 effects on N uptake [ppm Co2]</summary>
        [Description("Scalling paramenter for the CO2 effects on N requirement [ppm Co2]:")]
        [Units("ppm")]
        public double CO2EffectOffsetFactor { get; set; } = 600.0;

        /// <summary>Minimum value for the effect of CO2 on N requirement [0-1]</summary>
        [Description("Minimum value for the effect of CO2 on N requirement [0-1]:")]
        [Units("0-1")]
        public double CO2EffectMinimum { get; set; } = 0.7;

        /// <summary>Exponent of the function describing the effect of CO2 on N requirement</summary>
        [Description("Exponent of the function describing the effect of CO2 on N requirement:")]
        [Units("-")]
        public double CO2EffectExponent { get; set; } = 2.0;

        /// <summary>Flag whether photosynthesis reduction due to heat damage is enabled [yes/no]</summary>
        [Description("Enable photosynthesis reduction due to heat damage [yes/no]:")]
        [Units("oC")]
        public YesNoAnswer UseHeatStressFactor
        {
            get
            {
                if (usingHeatStressFactor)
                    return YesNoAnswer.yes;
                else
                    return YesNoAnswer.no;
            }
            set { usingHeatStressFactor = (value == YesNoAnswer.yes); }
        }

        /// <summary>Onset temperature for heat effects on photosynthesis [oC]</summary>
        [Description("Onset temperature for heat effects on photosynthesis [oC]:")]
        [Units("oC")]
        public double HeatOnsetTemp { get; set; } = 28.0;

        /// <summary>Temperature for full heat effect on photosynthesis (growth stops) [oC]</summary>
        [Description("Temperature for full heat effect on photosynthesis [oC]:")]
        [Units("oC")]
        public double HeatFullTemp { get; set; } = 35.0;

        /// <summary>Cumulative degrees-day for recovery from heat stress [oCd]</summary>
        [Description("Cumulative degrees-day for recovery from heat stress [oCd]:")]
        [Units("oCd")]
        public double HeatSumDD { get; set; } = 30.0;

        /// <summary>Reference temperature for recovery from heat stress [oC]</summary>
        [Description("Reference temperature for recovery from heat stress [oC]:")]
        [Units("oC")]
        public double HeatRecoverTemp { get; set; } = 25.0;

        /// <summary>Flag whether photosynthesis reduction due to cold damage is enabled [yes/no]</summary>
        [Description("Enable photosynthesis reduction due to cold damage [yes/no]:")]
        [Units("oC")]
        public YesNoAnswer UseColdStressFactor
        {
            get
            {
                if (usingColdStressFactor)
                    return YesNoAnswer.yes;
                else
                    return YesNoAnswer.no;
            }
            set { usingColdStressFactor = (value == YesNoAnswer.yes); }
        }

        /// <summary>Onset temperature for cold effects on photosynthesis [oC]</summary>
        [Description("Onset temperature for cold effects on photosynthesis [oC]:")]
        [Units("oC")]
        public double ColdOnsetTemp { get; set; } = 0.0;

        /// <summary>Temperature for full cold effect on photosynthesis (growth stops) [oC]</summary>
        [Description("Temperature for full cold effect on photosynthesis [oC]:")]
        [Units("oC")]
        public double ColdFullTemp { get; set; } = -3.0;

        /// <summary>Cumulative degrees for recovery from cold stress [oCd]</summary>
        [Description("Cumulative degrees for recovery from cold stress [oCd]:")]
        [Units("oCd")]
        public double ColdSumDD { get; set; } = 20.0;

        /// <summary>Reference temperature for recovery from cold stress [oC]</summary>
        [Description("Reference temperature for recovery from cold stress [oC]:")]
        [Units("oC")]
        public double ColdRecoverTemp { get; set; } = 0.0;

        /// <summary> Maintenance respiration coefficient - Fraction of DM consumed by respiration [0-1]</summary>
        [Description("Maintenance respiration coefficient [0-1]:")]
        [Units("0-1")]
        public double MaintenanceRespirationCoefficient { get; set; } = 0.03;

        /// <summary>Growth respiration coefficient - fraction of photosynthesis CO2 not assimilated (0-1)</summary>
        [Description("Growth respiration coefficient [0-1]:")]
        [Units("0-1")]
        public double GrowthRespirationCoefficient { get; set; } = 0.20;

        // - Germination and emergence  -------------------------------------------------------------------------------

        /// <summary>Cumulative degrees-day needed for seed germination [oCd]</summary>
        [Description("Cumulative degrees-day needed for seed germination [oCd]:")]
        [Units("oCd")]
        public double DegreesDayForGermination { get; set; } = 100.0;

        /// <summary>The fractions of DM for each plant part at emergence (all plants)</summary>
        private double[] EmergenceDMFractions = new double[]
        {0.60, 0.25, 0.00, 0.00, 0.15, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00};

        // - DM allocation  -------------------------------------------------------------------------------------------

        /// <summary>Target, or ideal, shoot-root ratio</summary>
        [Description("Target, or ideal, shoot-root ratio (for DM allocation) [>0.0]:")]
        [Units("")]
        public double TargetSRratio { get; set; } = 3.0;

        /// <summary>Maximum fraction of DM allocated to roots (from daily growth) [0-1]</summary>
        [Description("Maximum fraction of DM allocated to roots (from daily growth) [0-1]:")]
        [Units("0-1")]
        public double MaxRootAllocation { get; set; } = 0.25;

        /// <summary>Maximum effect that soil GLFs have on Shoot-Root ratio [0-1]</summary>
        [Description("Maximum effect that soil GLFs have on Shoot-Root ratio [0-1]:")]
        [Units("0-1")]
        public double ShootRootGlfFactor { get; set; } = 0.50;

        /// <summary>
        /// Flag whether Shoot:Root ratio should be adjusted to mimic DM allocation reproductive season (perennial species)
        /// </summary>
        [Description("Adjust Shoot-Root ratio to mimic DM allocation during reproductive season (perennials)?")]
        [Units("yes/no")]
        public YesNoAnswer UseReproSeasonFactor
        {
            get
            {
                if (usingReproSeasonFactor)
                    return YesNoAnswer.yes;
                else
                    return YesNoAnswer.no;
            }
            set { usingReproSeasonFactor = (value == YesNoAnswer.yes); }
        }

        /// <summary>Reference latitude determining timing for reproductive season [degress]</summary>
        [Description("Reference latitude determining timing for reproductive season [degress]:")]
        [Units("degrees")]
        public double ReproSeasonReferenceLatitude { get; set; } = 41.0;

        /// <summary>Coefficient controling the time to start the reproductive season as function of latitude [-]</summary>
        [Description("Coefficient controling the time to start the reproductive season as function of latitude [-]:")]
        [Units("-")]
        public double ReproSeasonTimingCoeff { get; set; } = 0.14;

        /// <summary>Coefficient controling the duration of the reproductive season as function of latitude [-]</summary>
        [Description("Coefficient controling the duration of the reproductive season as function of latitude [-]:")]
        [Units("-")]
        public double ReproSeasonDurationCoeff { get; set; } = 2.0;

        /// <summary>Ratio between the length of shoulders and the period with full reproductive growth effect [-]</summary>
        [Description("Ratio between the length of shoulders and the period with full reproductive growth effect [-]:")]
        [Units("-")]
        public double ReproSeasonShouldersLengthFactor { get; set; } = 1.0;

        /// <summary>Proportion of the shoulder length period prior to period with full reproductive growth effect [0-1]</summary>
        [Description("Proportion of the shoulder length period prior to period with full reproductive growth effect [0-1]:")]
        [Units("0-1")]
        public double ReproSeasonOnsetDurationFactor { get; set; } = 0.60;

        /// <summary>Maximum increase in Shoot-Root ratio during reproductive growth [>1.0]</summary>
        [Description("Maximum increase in Shoot-Root ratio during reproductive growth [>1.0]:")]
        [Units(">1.0")]
        public double ReproSeasonMaxAllocationIncrease { get; set; } = 0.50;

        /// <summary>Coefficient controling the increase in shoot allocation during reproductive growth as function of latitude [-]</summary>
        [Description("Coefficient controling the increase in shoot allocation during reproductive growth as function of latitude [-]:")]
        [Units("-")]
        public double ReproSeasonAllocationCoeff { get; set; } = 0.10;

        /// <summary>Maximum target allocation of new growth to leaves [0-1]</summary>
        [Description("Maximum target allocation of new growth to leaves [0-1]")]
        [Units("0-1")]
        private double FractionLeafMaximum { get; set; } = 0.7;

        /// <summary>Minimum target allocation of new growth to leaves [0-1]</summary>
        [Description("Minimum target allocation of new growth to leaves [0-1]")]
        [Units("0-1")]
        private double FractionLeafMinimum { get; set; } = 0.7;

        /// <summary>Shoot DM at which allocation of new growth to leaves start to decrease [g/m^2]</summary>
        [Description("Shoot DM at which allocation of new growth to leaves start to decrease [g/m^2]")]
        [Units("g/m^2")]
        private double FractionLeafDMThreshold { get; set; } = 100;

        /// <summary>Shoot DM factor, when allocation to leaves is midway maximum and minimum [g/m^2]</summary>
        [Description("Shoot DM factor, when allocation to leaves is midway maximum and minimum [g/m^2]")]
        [Units("g/m^2")]
        private double FractionLeafDMFactor { get; set; } = 500;

        /// <summary>Exponent of function describing DM allocation to leaves [>0.0]</summary>
        [Description("Exponent of function describing DM allocation to leaves [>0.0]")]
        [Units(">0.0")]
        private double FractionLeafExponent { get; set; } = 3.0;

        /// <summary>Fraction of new shoot growth allocated to stolons [0-1]</summary>
        [Description("Fraction of new shoot growth allocated to stolons [0-1]:")]
        [Units("0-1")]
        public double FractionToStolon { get; set; } = 0.0;

        /// <summary>Number of live leaves per tiller [-]</summary>
        [Description("Number of live leaves per tiller [-]:")]
        [Units("-")]
        public double LiveLeavesPerTiller { get; set; } = 3.0;

        /// <summary>Specific leaf area [m^2/kg DM]</summary>
        [Description("Specific leaf area [m^2/kg DM]:")]
        [Units("m^2/kg")]
        public double SpecificLeafArea { get; set; } = 20.0;

        /// <summary>Specific root length [m/g DM]</summary>
        [Description("Specific root length [m/g DM]:")]
        [Units("m/g")]
        public double SpecificRootLength { get; set; } = 75.0;

        /// <summary>Flag whether stem and stolons are considered for computing LAI green (mostly when DM is low)</summary>
        [Description("Use stems and stolons effect on LAI?")]
        [Units("yes/no")]
        public YesNoAnswer UseStemStolonEffectOnLAI
        {
            get
            {
                if (usingStemStolonEffect)
                    return YesNoAnswer.yes;
                else
                    return YesNoAnswer.no;
            }
            set { usingStemStolonEffect = (value == YesNoAnswer.yes); }
        }

        /// <summary>Fraction of stolon that can be considered green tissue when computing LAI</summary>
        [XmlIgnore]
        public double StolonEffectOnLAI { get; set; } = 0.3;

        /// <summary>Maximum aboveground biomass for stems to be considered when computing LAI [kg/ha]</summary>
        [XmlIgnore]
        public double ShootMaxEffectOnLAI { get; set; } = 1000;

        /// <summary>Maximum effect of stems considered when computing LAI</summary>
        [XmlIgnore]
        public double MaxStemEffectOnLAI { get; set; } = 0.316227766;

        // Turnover and senescence  -----------------------------------------------------------------------------------

        /// <summary>Daily DM turnover rate for shoot tissue [0-1]</summary>
        [Description("Daily DM turnover rate for shoot tissue  [0-1]:")]
        [Units("0-1")]
        public double TissueTurnoverRateShoot { get; set; } = 0.025;

        /// <summary>Daily DM turnover rate for root tissue [0-1]</summary>
        [Description("Daily DM turnover rate for root tissue [0-1]")]
        [Units("0-1")]
        public double TissueTurnoverRateRoot { get; set; } = 0.02;

        /// <summary>Relative turnover rate for growing tissue [-]</summary>
        [XmlIgnore]
        public double RelativeTurnoverGrowing { get; set; } = 2.0;

        /// <summary>Daily detachment rate for DM dead [0-1]</summary>
        [Description("Daily detachment rate for DM dead [0-1]:")]
        [Units("0-1")]
        public double DetachmentRate { get; set; } = 0.11;

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
        public double TissueTurnoverDroughtMax { get; set; } = 1.0;

        /// <summary>Minimum GLFwater without effect on tissue turnover [0-1], below this value tissue turnover increases</summary>
        [Description("Minimum GLFwater without effect on tissue turnover [0-1]")]
        [Units("0-1")]
        public double TissueTurnoverDroughtThreshold { get; set; } = 0.5;

        /// <summary>Stock factor for increasing tissue turnover rate</summary>
        [XmlIgnore]
        [Units("-")]
        public double StockParameter { get; set; } = 0.05;

        /// <summary>Fraction of luxury N remobilisable each day, for each tissue age (growing, developed, mature) [0-1]</summary>
        [Description("Fraction of luxury N remobilisable each day, for each tissue age (growing, developed, mature) [0-1]:")]
        [Units("0-1")]
        public double[] FractionNLuxuryRemobilisable { get; set; } = {0.0, 0.0, 0.0};

        /// <summary>Fraction of non-utilised remobilised N that is kept on dead material [0-1]</summary>
        [XmlIgnore]
        [Units("0-1")]
        public double FractionNRemobKept { get; set; } = 1.0;

        /// <summary>Fraction of C in senescent carbohydrate DM that is remobilised to new growth [0-1]</summary>
        [XmlIgnore]
        [Units("0-1")]
        public double FractionSenescedCarbRemob { get; set; } = 0.0;

        /// <summary>Fraction of C in senescent protein DM that is remobilised to new growth [0-1]</summary>
        [XmlIgnore]
        [Units("0-1")]
        public double FactionSenescedProteinRemob { get; set; } = 0.0;

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

        /// <summary>Exponent for modifying the effect of N deficiency on plant growth.</summary>
        [Description("Exponent for modifying the effect of N deficiency on plant growth:")]
        [Units("-")]
        public double NDillutionCoefficient { get; set; } = 0.5;

        /// <summary>Maximum reduction in plant growth due to water logging (saturated soil) [0-1]</summary>
        [Description("Maximum reduction in plant growth due to water logging (saturated soil) [0-1]:")]
        [Units("0-1")]
        public double SoilWaterSaturationFactor { get; set; } = 0.1;

        /// <summary>Minimum water-free pore space for growth with no limitations [0-1]</summary>
        [Description("Minimum water-free pore space for growth with no limitations [0-1]:")]
        [Units("0-1")]
        public double MinimumWaterFreePorosity { get; set; } = 0.1;

        /// <summary>
        /// Flag whether water logging effect is considered as a cumulative effect (instead of only daily effect)
        /// </summary>
        [Description("Use cumulative effects on plant growth due to water logging?")]
        [Units("yes/no")]
        public YesNoAnswer UseCumulativeWaterLoggingEffect
        {
            get
            {
                if (usingCumulativeWaterLogging)
                    return YesNoAnswer.yes;
                else
                    return YesNoAnswer.no;
            }
            set { usingCumulativeWaterLogging = (value == YesNoAnswer.yes); }
        }

        /// <summary>Daily recovery rate from water logging [0-1]</summary>
        [XmlIgnore]
        public double SoilWaterSaturationRecoveryFactor { get; set; } = 1.0;

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
        [XmlIgnore]
        public double[] DigestibilitiesProtein { get; set; } = {1.0, 1.0, 1.0, 1.0};

        /// <summary>Soluble fraction of carbohydrates in newly grown tissues for each plant organ (leaf, stem, stolon, root) [0-1]</summary>
        [XmlIgnore]
        public double[] SugarFractionNewGrowth { get; set; } = { 0.5, 0.5, 0.5, 0.0 };

        // - Minimum DM and preferences when harvesting  --------------------------------------------------------------

        /// <summary>Minimum above ground green DM [kg DM/ha]</summary>
        [Description("Minimum above ground green DM [kg DM/ha]:")]
        [Units("kg/ha")]
        public double MinimumGreenWt { get; set; } = 300.0;  // TODO this should be leaves only

        /// <summary>Minimum root amount in proportion to minimum green Wt</summary>
        public double MinimumRootProp { get; set; } = 0.5;

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
        private int doyGermination = 240;

        /// <summary>The number of days from emergence to anthesis</summary>
        private int daysEmergenceToAnthesis = 100;

        /// <summary>The number of days from anthesis to maturity</summary>
        private int daysAnthesisToMaturity = 100;

        /// <summary>The cumulative degrees-day from emergence to anthesis</summary>
        private double degreesDayForAnthesis = 0.0;

        /// <summary>The cumulative degrees-day from anthesis to maturity</summary>
        private double degreesDayForMaturity = 0.0;

        /// <summary>The number of days from emergence with reduced growth</summary>
        private int daysAnnualsFactor = 60;

        // - Other parameters  ----------------------------------------------------------------------------------------

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
        internal PastureAboveGroundOrgan leaves;

        /// <summary>State of sheath/stems (DM and N)</summary>
        internal PastureAboveGroundOrgan stems;

        /// <summary>State of stolons (DM and N)</summary>
        internal PastureAboveGroundOrgan stolons;

        /// <summary>State of roots (DM and N)</summary>
        internal PastureBelowGroundOrgan roots;

        /// <summary>Basic state variables for this plant (to be used for reset)</summary>
        private SpeciesBasicStateSettings InitialState;

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
        private double daysSinceEmergence = 0;

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
        private double grossPhotosynthesis = 0.0;

        /// <summary>The growth respiration rate (C loss)</summary>
        private double respirationGrowth = 0.0;

        /// <summary>The maintenance respiration rate (C loss)</summary>
        private double respirationMaintenance = 0.0;

        /// <summary>The amount of C remobilisable from senesced tissue</summary>
        private double remobilisableC = 0.0;

        /// <summary>The amount of C remobilised from senesced tissue</summary>
        private double remobilisedC = 0.0;

        /// <summary>Daily net growth potential (kgDM/ha)</summary>
        private double dGrowthPot;

        /// <summary>Daily potential growth after water stress</summary>
        private double dGrowthWstress;

        /// <summary>Daily growth after nutrient stress (actual growth)</summary>
        private double dGrowthActual;

        /// <summary>Effective plant growth (actual growth minus senescence)</summary>
        private double dGrowthEff;

        /// <summary>Actual growth of shoot</summary>
        private double dGrowthShootDM;

        /// <summary>Actual growth of roots</summary>
        private double dGrowthRootDM;

        /// <summary>Actual N allocation into shoot</summary>
        private double dGrowthShootN;

        /// <summary>Actual N allocation into roots</summary>
        private double dGrowthRootN;

        /// <summary>DM amount senesced shoot</summary>
        private double senescedShootDM;

        /// <summary>N amount in senesced tissues from shoot</summary>
        private double senescedShootN;

        /// <summary>DM amount detached from shoot (added to surface OM)</summary>
        private double detachedShootDM;

        /// <summary>N amount in detached tissues from shoot</summary>
        private double detachedShootN;

        /// <summary>DM amount in senesced roots</summary>
        private double senescedRootDM;

        /// <summary>N amount in senesced tissues from roots</summary>
        private double senescedRootN;

        /// <summary>DM amount in detached roots (added to soil FOM)</summary>
        private double detachedRootDM;

        /// <summary>N amount in detached tissues from roots</summary>
        private double detachedRootN;

        /// <summary>Fraction of new growth allocated to shoot (0-1)</summary>
        private double fractionToShoot;

        /// <summary>Fraction of new shoot growth allocated to leaves (0-1)</summary>
        public double fractionToLeaf;

        /// <summary>Flag whether the factor adjusting Shoot:Root ratio during reproductive season is being used</summary>
        private bool usingReproSeasonFactor = true;

        /// <summary>The three intervals defining the reproductive season (onset, main phase, and outset)</summary>
        private double[] reproSeasonInterval;

        /// <summary>The day of the year for the start of the reproductive season</summary>
        private double doyIniReproSeason;

        /// <summary>The relative increase in the shoot-root ratio during reproductive season</summary>
        private double allocationIncreaseRepro;

        /// <summary>The daily DM turnover rate (from tissue 1 to 2, then to 3, then to 4)</summary>
        private double gama = 0.0;

        /// <summary>The daily DM turnover rate for stolons</summary>
        private double gamaS = 0.0; // for stolons

        /// <summary>The daily DM turnover rate for dead tissue (from tissue 4 to litter)</summary>
        private double gamaD = 0.0;

        /// <summary>The daily DM turnover rate for roots</summary>
        private double gamaR = 0.0;

        /// <summary>The tissue turnover factor due to variations in temperature</summary>
        private double ttfTemperature = 0.0;

        /// <summary>The tissue turnover factor due to variations in moisture</summary>
        private double ttfMoistureShoot = 0.0;

        /// <summary>The tissue turnover factor due to variations in moisture</summary>
        private double ttfMoistureRoot = 0.0;

        /// <summary>The tissue turnover factor due to variations in moisture</summary>
        private double ttfLeafNumber = 0.0;

        /// <summary>The cumulative degrees-day during germination phase</summary>
        private double germinationGDD = 0.0;

        // Plant height, LAI and cover  -------------------------------------------------------------------------------

        /// <summary>The plant's green LAI</summary>
        private double greenLAI;

        /// <summary>The plant's dead LAI</summary>
        private double deadLAI;

        /// <summary>Flag whether stem and stolons are considered for computing LAI green (mostly when DM is low)</summary>
        private bool usingStemStolonEffect = true;

        // Root depth and distribution --------------------------------------------------------------------------------

        /// <summary>The daily variation in root depth</summary>
        private double dRootDepth = 50;

        // Amounts and fluxes of N in the plant  ----------------------------------------------------------------------

        /// <summary>The N demand for new growth, with luxury uptake</summary>
        private double NdemandLux;

        /// <summary>The N demand for new growth, at optimum N content</summary>
        private double NdemandOpt;

        /// <summary>The amount of N fixation from atmosphere (for legumes)</summary>
        internal double Nfixation = 0.0;

        /// <summary>The amount of senesced N available for remobilisation</summary>
        private double NSenescedRemobilisable = 0.0;

        /// <summary>The amount of senesced N actually remobilised</summary>
        private double NSenescedRemobilised = 0.0;

        /// <summary>The amount of N used in new growth</summary>
        internal double newGrowthN = 0.0;

        /// <summary>The amount of luxury N (above Nopt) available for remobilisation</summary>
        private double NLuxuryRemobilisable;

        /// <summary>The amount of luxury N actually remobilised</summary>
        private double NLuxuryRemobilised;

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

        /// <summary>Flag whether the factor reducing photosynthesis due to heat damage is being used</summary>
        private bool usingHeatStressFactor = true;

        /// <summary>Flag whether the factor reducing photosynthesis due to cold damage is being used</summary>
        private bool usingColdStressFactor = true;

        /// <summary>The growth factor due to heat stress</summary>
        private double glfHeat = 1.0;
        
        /// <summary>The growth factor due to cold stress</summary>
        private double glfCold = 1.0;

        /// <summary>The growth limiting factor due to water stress</summary>
        internal double glfWater = 1.0;

        /// <summary>Flag whether the factor reducing growth due to logging is used on a cumulative basis</summary>
        private bool usingCumulativeWaterLogging = false;

        /// <summary>The cumulative water logging factor</summary>
        private double cumWaterLogging = 0.0;

        /// <summary>The growth limiting factor due to water logging</summary>
        internal double glfAeration = 1.0;

        /// <summary>The growth limiting factor due to N stress</summary>
        internal double glfN = 1.0;

        // Auxiliary variables for temperature stress  ----------------------------------------------------------------

        /// <summary>Growth rate reduction factor due to high temperatures</summary>
        private double highTempStress = 1.0;

        /// <summary>Cumulative degress of temperature for recovery from heat damage</summary>
        private double accumDDHeat = 0.0;

        /// <summary>Growth rate reduction factor due to low temperatures</summary>
        private double lowTempStress = 1.0;

        /// <summary>Cumulative degress of temperature for recovery from cold damage</summary>
        private double accumDDCold = 0.0;

        // Harvest and digestibility  ---------------------------------------------------------------------------------

        /// <summary>The fraction of DM harvested (defoliated)</summary>
        private double fractionDefoliated = 0.0;

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
        internal const double CarbonFractionInDM = 0.4;

        /// <summary>Factor for converting nitrogen to protein</summary>
        internal const double NitrogenToProteinFactor = 6.25;

        /// <summary>The C:N ratio of protein</summary>
        internal const double CNratioProtein = 3.5;

        /// <summary>The C:N ratio of cell wall</summary>
        internal const double CNratioCellWall = 100.0;

        /// <summary>Minimum significant difference between two values</summary>
        internal const double Epsilon = 0.000000001;

        /// <summary>A yes or no answer</summary>
        public enum YesNoAnswer
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
                    if(phenologicStage < Epsilon)
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
            get { return AboveGroundLiveWt + AboveGroundDeadWt; }
        }

        /// <summary>Gets the DM weight of live plant parts above ground.</summary>
        /// <value>The above ground DM weight of live plant parts.</value>
        [Description("Dry matter weight of alive plants above ground")]
        [Units("kgDM/ha")]
        public double AboveGroundLiveWt
        {
            get { return leaves.DMLive + stems.DMLive + stolons.DMLive; }
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
            get { return leaves.DMLive + stems.DMLive; }
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
            get { return leaves.DMLive; }
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
            get { return stems.DMLive; }
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

        /// <summary>Gets the DM weight of growing tissues from all above ground organs.</summary>
        /// <value>The DM weight of growing tissues.</value>
        [Description("Dry matter weight of growing tissues from all above ground organs")]
        [Units("kgDM/ha")]
        public double GrowingTissuesWt
        {
            get { return leaves.Tissue[0].DM + stems.Tissue[0].DM + stolons.Tissue[0].DM; }
        }

        /// <summary>Gets the DM weight of developed tissues from all above ground organs.</summary>
        /// <value>The DM weight of developed tissues.</value>
        [Description("Dry matter weight of developed tissues from all above ground organs")]
        [Units("kgDM/ha")]
        public double DevelopedTissuesWt
        {
            get { return leaves.Tissue[1].DM + stems.Tissue[1].DM + stolons.Tissue[1].DM; }
        }

        /// <summary>Gets the DM weight of mature tissues from all above ground organs.</summary>
        /// <value>The DM weight of mature tissues.</value>
        [Description("Dry matter weight of mature tissues from all above ground organs")]
        [Units("kgDM/ha")]
        public double MatureTissuesWt
        {
            get { return leaves.Tissue[2].DM + stems.Tissue[2].DM + stolons.Tissue[2].DM; }
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
            get { return grossPhotosynthesis; }
        }

        /// <summary>Gets the carbon loss via respiration.</summary>
        /// <value>The carbon loss via respiration.</value>
        [Description("Loss of C via respiration")]
        [Units("kgC/ha")]
        public double CarbonLossRespiration
        {
            get { return respirationMaintenance; }
        }

        /// <summary>Gets the carbon remobilised from senescent tissue.</summary>
        /// <value>The carbon remobilised.</value>
        [Description("C remobilised from senescent tissue")]
        [Units("kgC/ha")]
        public double CarbonRemobilisable
        {
            get { return remobilisableC; }
        }

        /// <summary>Gets the gross potential growth rate.</summary>
        /// <value>The potential C assimilation, in DM equivalent.</value>
        [Description("Gross potential growth rate (potential C assimilation)")]
        [Units("kgDM/ha")]
        public double GrossPotentialGrowthWt
        {
            get { return grossPhotosynthesis / CarbonFractionInDM; }
        }

        /// <summary>Gets the respiration rate.</summary>
        /// <value>The loss of C due to respiration, in DM equivalent.</value>
        [Description("Respiration rate (DM lost via respiration)")]
        [Units("kgDM/ha")]
        public double RespirationWt
        {
            get { return respirationMaintenance / CarbonFractionInDM; }
        }

        /// <summary>Gets the remobilisation rate.</summary>
        /// <value>The C remobilised, in DM equivalent.</value>
        [Description("C remobilisation (DM remobilised from old tissue to new growth)")]
        [Units("kgDM/ha")]
        public double RemobilisationWt
        {
            get { return remobilisableC / CarbonFractionInDM; }
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
            get { return dGrowthShootDM; }
        }

        /// <summary>Gets the effective root growth rate.</summary>
        /// <value>The root growth DM weight.</value>
        [Description("Effective root growth rate")]
        [Units("kgDM/ha")]
        public double RootGrowthWt
        {
            get { return dGrowthRootDM - detachedRootDM; }
        }

        /// <summary>Gets the litter DM weight deposited onto soil surface.</summary>
        /// <value>The litter DM weight deposited.</value>
        [Description("Litter amount deposited onto soil surface")]
        [Units("kgDM/ha")]
        public double LitterWt
        {
            get { return detachedShootDM; }
        }

        /// <summary>Gets the senesced root DM weight.</summary>
        /// <value>The senesced root DM weight.</value>
        [Description("Amount of senesced roots added to soil FOM")]
        [Units("kgDM/ha")]
        public double RootSenescedWt
        {
            get { return detachedRootDM; }
        }

        /// <summary>Gets the gross primary productivity.</summary>
        /// <value>The gross primary productivity.</value>
        [Description("Gross primary productivity")]
        [Units("kgDM/ha")]
        public double GPP
        {
            get { return grossPhotosynthesis / CarbonFractionInDM; }
        }

        /// <summary>Gets the net primary productivity.</summary>
        /// <value>The net primary productivity.</value>
        [Description("Net primary productivity")]
        [Units("kgDM/ha")]
        public double NPP
        {
            get { return (grossPhotosynthesis - respirationGrowth - respirationMaintenance) / CarbonFractionInDM; }
        }

        /// <summary>Gets the net above-ground primary productivity.</summary>
        /// <value>The net above-ground primary productivity.</value>
        [Description("Net above-ground primary productivity")]
        [Units("kgDM/ha")]
        public double NAPP
        {
            get { return (grossPhotosynthesis - respirationGrowth - respirationMaintenance) * fractionToShoot / CarbonFractionInDM; }
        }

        /// <summary>Gets the net below-ground primary productivity.</summary>
        /// <value>The net below-ground primary productivity.</value>
        [Description("Net below-ground primary productivity")]
        [Units("kgDM/ha")]
        public double NBPP
        {
            get { return (grossPhotosynthesis - respirationGrowth - respirationMaintenance) * (1 - fractionToShoot) / CarbonFractionInDM; }
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
            get { return leaves.NLive + stems.NLive + stolons.NLive; }
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
            get { return leaves.NLive + stems.NLive; }
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
            get { return leaves.NLive; }
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
            get { return stems.NLive; }
        }

        /// <summary>Gets the N content of dead stems and sheath.</summary>
        /// <value>The dead stem N content.</value>
        [Description("N amount in dead sytems")]
        [Units("kgN/ha")]
        public double StemDeadN
        {
            get { return stems.NDead; }
        }

        /// <summary>Gets the N content of growing tissues from all above ground organs.</summary>
        /// <value>The N content of growing tissues.</value>
        [Description("N amount in growing tissues from all above ground organs")]
        [Units("kgN/ha")]
        public double GrowingTissuesN
        {
            get { return leaves.Tissue[0].Namount + stems.Tissue[0].Namount + stolons.Tissue[0].Namount; }
        }

        /// <summary>Gets the N content of developed tissues from all above ground organs.</summary>
        /// <value>The N content of developed tissues.</value>
        [Description("N amount in developed tissues from all above ground organs")]
        [Units("kgN/ha")]
        public double DevelopedTissuesN
        {
            get { return leaves.Tissue[1].Namount + stems.Tissue[1].Namount + stolons.Tissue[1].Namount; }
        }

        /// <summary>Gets the N content of mature tissues from all above ground organs.</summary>
        /// <value>The N content of mature tissues.</value>
        [Description("N amount in mature tissues from all above ground organs")]
        [Units("kgN/ha")]
        public double MatureTissuesN
        {
            get { return leaves.Tissue[2].Namount + stems.Tissue[2].Namount + stolons.Tissue[2].Namount; }
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
        public double RemobilisableSenescedN
        {
            get { return NSenescedRemobilisable; }
        }

        /// <summary>Gets the amount of N remobilised from senesced tissue.</summary>
        /// <value>The remobilised N amount.</value>
        [Description("Amount of N remobilised from senesced material")]
        [Units("kgN/ha")]
        public double RemobilisedSenescedN
        {
            get { return NSenescedRemobilised; }
        }

        /// <summary>Gets the amount of luxury N potentially remobilisable.</summary>
        /// <value>The remobilisable luxury N amount.</value>
        [Description("Amount of luxury N potentially remobilisable")]
        [Units("kgN/ha")]
        public double RemobilisableLuxuryN
        {
            get { return NLuxuryRemobilisable; }
        }

        /// <summary>Gets the amount of luxury N remobilised.</summary>
        /// <value>The remobilised luxury N amount.</value>
        [Description("Amount of luxury N remobilised")]
        [Units("kgN/ha")]
        public double RemobilisedLuxuryN
        {
            get { return NLuxuryRemobilised; }
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
            get { return detachedShootN; }
        }

        /// <summary>Gets the amount of N from senesced roots added to soil FOM.</summary>
        /// <value>The senesced root N amount.</value>
        [Description("Amount of N from senesced roots added to soil FOM")]
        [Units("kgN/ha")]
        public double SenescedRootN
        {
            get { return detachedRootN; }
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
            get { return fractionToShoot; }
        }

        /// <summary>Gets the DM allocation to roots.</summary>
        /// <value>The root dm allocation.</value>
        [Description("Fraction of DM allocated to roots")]
        [Units("0-1")]
        public double RootDMAllocation
        {
            get { return 1 - fractionToShoot; }
        }


        /// <summary>Gets the DM allocation to leaves.</summary>
        /// <value>The leaf DM allocation.</value>
        [Description("Fraction of DM allocated to leaves")]
        [Units("0-1")]
        public double LeafDMAllocation
        {
            get { return fractionToLeaf; }
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
            leaves = new PastureAboveGroundOrgan(4);
            stems = new PastureAboveGroundOrgan(4);
            stolons = new PastureAboveGroundOrgan(4);
            roots = new PastureBelowGroundOrgan(2, nLayers);

            // initialise soil water and N variables
            InitiliaseSoilArrays();

            // Check and save initial state
            CheckInitialState();

            // set initial plant state
            SetInitialState();

            // initialise parameter for DM allocation during reproductive season
            InitReproductiveGrowthFactor();

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

        /// <summary>Initialise arrays to same length as soil layers</summary>
        private void InitiliaseSoilArrays()
        {
            mySoilAvailableWater = new double[nLayers];
            mySoilWaterTakenUp = new double[nLayers];
            mySoilNH4available = new double[nLayers];
            mySoilNO3available = new double[nLayers];
            mySoilAvailableN = new double[nLayers];
            mySoilNitrogenTakenUp = new double[nLayers];
        }

        /// <summary>Initialise, check, and save the varibles representing the initial plant state</summary>
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
            if (InitialShootDM > Epsilon)
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
            else if (InitialShootDM > -Epsilon)
            {
                // DM is zero, plant has just sown and is able to germinate
                InitialState.PhenoStage = 0;
            }
            else
            {
                //DM is negative, plant is not yet in the ground 
                InitialState.PhenoStage = -1;
            }

            // 3. Set remobilisation rate for luxury N in each tissue
            roots.Tissue[0].FractionNLuxuryRemobilisable = FractionNLuxuryRemobilisable[0];
            for (int tissue = 0; tissue < 3; tissue++)
            {
                leaves.Tissue[tissue].FractionNLuxuryRemobilisable = FractionNLuxuryRemobilisable[tissue];
                stems.Tissue[tissue].FractionNLuxuryRemobilisable = FractionNLuxuryRemobilisable[tissue];
                stolons.Tissue[tissue].FractionNLuxuryRemobilisable = FractionNLuxuryRemobilisable[tissue];
            }

            // 4. Set the digestibility parameters for each tissue
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

            leaves.Tissue[0].FractionSugarNewGrowth = SugarFractionNewGrowth[0];
            stems.Tissue[0].FractionSugarNewGrowth = SugarFractionNewGrowth[1];
            stolons.Tissue[0].FractionSugarNewGrowth = SugarFractionNewGrowth[2];
            //NOTE: roots are not considered for digestibility
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

        /// <summary>Initialise parameters to compute factor increasing shoot allocation during reproductive growth</summary>
        /// <remarks>
        /// Reproductive phase of perennial is not simulated by the model, the ReproductiveGrowthFactor attempts to mimic the main
        ///  effect, which is a higher allocation of DM to shoot during this period. The beginning and length of the reproductive
        ///  phase is computed as function of latitude (it occurs later in spring and is shorter the further the location is from
        ///  the equator). The extent at which allocation to shoot increases is also a function of latitude, maximum allocation is
        ///  greater for higher latitudes. Shoulder periods occur before and after the main phase, in these allocation transictions
        ///  between default allocation and that of the main phase.
        /// </remarks>
        internal void InitReproductiveGrowthFactor()
        {
            reproSeasonInterval = new double[3];

            // compute the day to start the main phase (period with maximum DM allocation to shoot)
            double doyWinterSolstice = (myMetData.Latitude < 0.0) ? 172 : 355;
            double reproAux = Math.Exp(-ReproSeasonTimingCoeff * (Math.Abs(myMetData.Latitude) - ReproSeasonReferenceLatitude));
            double doyIniPlateau = doyWinterSolstice + 0.5 * 365.25 / (1.0 + reproAux);

            // compute the duration of the main phase (minimum of about 15 days, maximum of six months)
            reproSeasonInterval[1] = (365.25 / 24.0);
            reproSeasonInterval[1] += (365.25 * 11.0 / 24.0) * Math.Pow(1.0 - (Math.Abs(myMetData.Latitude) / 90.0), ReproSeasonDurationCoeff);

            // compute the duration of the onset and outset phases (shoulders - maximum of six months)
            reproAux = Math.Min(365.25 / 2.0, reproSeasonInterval[1] * ReproSeasonShouldersLengthFactor);
            reproSeasonInterval[0] = reproAux * ReproSeasonOnsetDurationFactor;
            reproSeasonInterval[2] = reproAux * (1.0 - ReproSeasonOnsetDurationFactor);

            // get the day for the start of reproductive season
            doyIniReproSeason = doyIniPlateau - reproSeasonInterval[0];
            if (doyIniReproSeason < 0.0) doyIniReproSeason += 365.25;

            // compute the factor to augment shoot:root ratio at main phase
            reproAux = Math.Exp(-ReproSeasonAllocationCoeff * (Math.Abs(myMetData.Latitude) - ReproSeasonReferenceLatitude));
            allocationIncreaseRepro = ReproSeasonMaxAllocationIncrease / (1.0 + reproAux);
        }

        /// <summary>Initialise the variables in canopy properties</summary>
        private void InitialiseCanopy()
        {
            // Used in Val's Arbitrator (via ICrop2)
            myCanopyProperties.Name = Name;
            myCanopyProperties.CoverGreen = CoverGreen;
            myCanopyProperties.CoverTot = CoverTotal;
            myCanopyProperties.CanopyDepth = Depth;
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
        }

        /// <summary>Performs the plant growth calculations</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoPlantGrowth")]
        private void OnDoPlantGrowth(object sender, EventArgs e)
        {
            if (isAlive && !isSwardControlled)
            {
                // check phenology of annuals
                if (isAnnual)
                    EvaluatePhenologyAnnuals();

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

                    // step 01 - Get potential growth
                    CalcDailyPotentialGrowth();

                    // step 01.5 - Get potential allocation of today's growth
                    EvaluateGrowthAllocation();

                    // Water demand, supply, and uptake
                    DoWaterCalculations();

                    // step 02 - Potential growth after water limitations
                    CalcGrowthAfterWaterLimitations();

                    // Nitrogen demand, supply, and uptake
                    DoNitrogenCalculations();

                    // step 03 - Growth after nutrient limitations, but before senescence
                    CalcGrowthAfterNutrientLimitations();

                    // Partition new growth into various tissues
                    EvaluateNewGrowthAllocation();

                    // Compute tissue turnover and remobilisation (C and N)
                    EvaluateTissueTurnoverRates();

                    // step 04 - Effective growth, after all limitations and senescence
                    CalcEffectiveGrowth();

                    // Send amounts of litter and senesced roots to other modules
                    DoSurfaceOMReturn(detachedShootDM, detachedShootN);
                    DoIncorpFomEvent(detachedRootDM, detachedRootN);
                }
            }
            //else
            //    Growth is controlled by Sward (all species)
        }

        #region - Plant growth processes  ----------------------------------------------------------------------------------

        /// <summary>Evaluates the phenologic stage of annual plants</summary>
        /// <remarks>
        /// This method keeps track of days after emergence as well as cumulative degrees days, it uses both to evaluate the progress
        ///  through each phase. The two approaches are used concomitantly to enable some basic sensitivity to environmental factors,
        ///  but also to ensure that plants will complete their cycle (as the controls used here are rudimentary).
        /// This method also update the value of phenoFactor, using the estimated progress through the current phenologic phase.
        /// </remarks>
        private void EvaluatePhenologyAnnuals()
        {
            // check whether germination started
            if (myClock.Today.DayOfYear == doyGermination)
            {
                // just allowed to germinate
                phenologicStage = 0;
            }

            if (phenologicStage > 0)
            {
                double phenoFactor1;
                double phenoFactor2;

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

                // set the phenologic factor (fraction of current phase)
                phenoFactor = Math.Max(phenoFactor1, phenoFactor2);
            }
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
            // Get today's gross potential photosynthetic rate (kgC/ha/day)
            grossPhotosynthesis = DailyPotentialPhotosynthesis();

            // Get respiration rates (kgC/ha/day)
            respirationMaintenance = DailyMaintenanceRespiration();
            respirationGrowth = DailyGrowthRespiration();

            // Get C remobilisation (kgC/ha/day) (got from tissue turnover) - TODO: implement C remobilisation
            remobilisedC = remobilisableC;

            // Net potential growth (kgDM/ha/day)
            dGrowthPot = Math.Max(0.0, grossPhotosynthesis - respirationGrowth + remobilisedC - respirationMaintenance);
            dGrowthPot /= CarbonFractionInDM;
        }

        /// <summary>Calculates the growth with water limitations.</summary>
        internal void CalcGrowthAfterWaterLimitations()
        {
            // Adjust today's growth for limitations related to soil water
            dGrowthWstress = dGrowthPot * Math.Min(glfWater, glfAeration);
        }

        /// <summary>Calculates the actual plant growth (after all growth limitations, before senescence).</summary>
        /// <remarks>
        /// Here the limitiation due to soil fertility are considered, the model simulates N deficiency only, but a generic user-settable
        ///  limitiation factor (GlfSFertility) can be used to mimic limitation due to other soil related factors (e.g. phosphorus)
        /// The GLF due to N stress is modified here to account for N dillution effects:
        /// Many plants, especially grasses, can keep growth even when N supply is below optimum; the N concentration is reduced
        ///  in the plant tissues. This is represented hereby adjusting the effect of N deficiency using a power function. When the exponent
        ///  is 1.0, the reductionin growth is linearly proportional to N deficiency, a greater value results in less reduction in growth.
        /// For many plants the value should be smaller than 1.0. For grasses, the exponent is typically around 0.5.
        /// </remarks>
        internal void CalcGrowthAfterNutrientLimitations()
        {
            // Adjust the glfN
            double glfNit = Math.Pow(glfN, NDillutionCoefficient);

            // Adjust todays growth for any soil nutrient limitation
            dGrowthActual = dGrowthWstress * Math.Min(glfNit, GlfSFertility);
        }

        /// <summary>Calculates the plant effective growth (include changes in LAI and root depth).</summary>
        internal void CalcEffectiveGrowth()
        {
            // Effective, or net, growth
            dGrowthEff = (dGrowthShootDM - detachedShootDM) + (dGrowthRootDM - detachedRootDM);

            // Save some variables for mass balance check
            double preAboveGroundLiveWt = AboveGroundLiveWt;
            double preBelowGroundWt = BelowGroundWt;
            double preTotalWt = TotalWt;
            double preTotalN = TotalN;

            // Update each organ
            leaves.DoOrganUpdate();
            stems.DoOrganUpdate();
            stolons.DoOrganUpdate();
            roots.DoOrganUpdate();

            // Check for loss of mass balance
            double myDelta = dGrowthShootDM - senescedShootDM;
            bool testLive = Math.Abs(AboveGroundLiveWt - preAboveGroundLiveWt - myDelta) > Epsilon;
            myDelta = dGrowthRootDM - detachedRootDM;
            bool testRoot = Math.Abs(BelowGroundWt - preBelowGroundWt - myDelta) > Epsilon;
            myDelta = dGrowthActual - detachedShootDM - detachedRootDM;
            bool testTotal = Math.Abs(TotalWt - preTotalWt - myDelta) > Epsilon;
            //if (testLive || testRoot || testTotal)
            //    throw new Exception("  " + Name + " - Growth and tissue turnover resulted in loss of mass balance for DM");

            //myDelta = newGrowthN - detachedShootN - detachedRootN;
            //testTotal = Math.Abs(TotalN - preTotalN - myDelta) > Epsilon;
            //if (testLive || testRoot || testTotal)
            //    throw new Exception("  " + Name + " - Growth and tissue turnover resulted in loss of mass balance for N");

            // Update LAI
            EvaluateLAI();

            // Update digestibility
            EvaluateDigestibility();
        }

        /// <summary>Computes the plant's gross potential growth rate</summary>
        /// <returns>The potential amount of C assimilated via photosynthesis (kgC/ha)</returns>
        private double DailyPotentialPhotosynthesis()
        {
            // CO2 effects on Pmax
            glfCO2 = CO2EffectOnPhotosynthesis();

            // N concentration effects on Pmax
            glfNc = NConcentrationEffect();

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

            // Gross photosynthesis, converted to kg C/ha/day
            double GrossPhotosynthesis = CarbonAssim * 10; // convert from g/m2 to kg/ha (= 10000/1000)

            // Consider the extreme temperature effects (in practice only one temp stress factor is < 1)
            glfHeat = HeatStress();
            glfCold = ColdStress();

            // Consider phenologically related reduction in photosynthesis for annual species
            if (isAnnual)
                GrossPhotosynthesis *= AnnualSpeciesGrowthFactor();

            // Actual gross photosynthesis (gross potential growth - kg C/ha/day)
            return GrossPhotosynthesis * Math.Min(glfHeat, glfCold) * GlfGeneric;
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
            double liveBiomassC = (AboveGroundLiveWt + BelowGroundWt) * CarbonFractionInDM;
            double result = liveBiomassC * MaintenanceRespirationCoefficient * Teffect * glfNc;
            return Math.Max(0.0, result);
        }

        /// <summary>Computes the plant's loss of C due to growth respiration</summary>
        /// <returns>The amount of C lost to atmosphere (kgC/ha)</returns>
        private double DailyGrowthRespiration()
        {
            return grossPhotosynthesis * GrowthRespirationCoefficient;
        }

        /// <summary>Computes the allocation of new growth to all tissues in each organ</summary>
        internal void EvaluateNewGrowthAllocation()
        {
            if (dGrowthActual > Epsilon)
            {
                // Get the actual growth above and below ground
                dGrowthShootDM = dGrowthActual * fractionToShoot;
                dGrowthRootDM = Math.Max(0.0, dGrowthActual - dGrowthShootDM);

                // Get the fractions of new growth to allocate to each plant organ
                double toLeaf = fractionToShoot * fractionToLeaf;
                double toStem = fractionToShoot * (1.0 - FractionToStolon - fractionToLeaf);
                double toStolon = fractionToShoot * FractionToStolon;
                double toRoot = 1.0 - fractionToShoot;

                // Allocate new DM growth to the growing tissues
                leaves.Tissue[0].DMTransferedIn += toLeaf * dGrowthActual;
                stems.Tissue[0].DMTransferedIn += toStem * dGrowthActual;
                stolons.Tissue[0].DMTransferedIn += toStolon * dGrowthActual;
                roots.Tissue[0].DMTransferedIn += toRoot * dGrowthActual;

                // Evaluate allocation of N
                // Partitioning N based on DM fractions and on max [N] in plant parts
                double Nsum = (toLeaf * leaves.NConcMaximum) + (toStem * stems.NConcMaximum)
                            + (toStolon * stolons.NConcMaximum) + (toRoot * roots.NConcMaximum);
                double toLeafN = toLeaf * MathUtilities.Divide(leaves.NConcMaximum, Nsum, 0.0);
                double toStemN = toStem * MathUtilities.Divide(stems.NConcMaximum, Nsum, 0.0);
                double toStolonN = toStolon * MathUtilities.Divide(stolons.NConcMaximum, Nsum, 0.0);
                double toRootN = toRoot * MathUtilities.Divide(roots.NConcMaximum, Nsum, 0.0);

                // Allocate N from new growth to the first tissue pools
                leaves.Tissue[0].NTransferedIn += toLeafN * newGrowthN;
                stems.Tissue[0].NTransferedIn += toStemN * newGrowthN;
                stolons.Tissue[0].NTransferedIn += toStolonN * newGrowthN;
                roots.Tissue[0].NTransferedIn += toRootN * newGrowthN;

                // Update N variables
                dGrowthShootN = leaves.Tissue[0].NTransferedIn + stems.Tissue[0].NTransferedIn + stolons.Tissue[0].NTransferedIn;
                dGrowthRootN = roots.Tissue[0].NTransferedIn;

                // Evaluate root elongation and allocate new growth in each layer
                EvaluateRootElongation();
                DoRootGrowthAllocation();

                // update tissues (to be delete)
                leaves.DoOrganUpdate();
                stems.DoOrganUpdate();
                stolons.DoOrganUpdate();
                roots.DoOrganUpdate();
                leaves.DoCleanTransferAmounts();
                stems.DoCleanTransferAmounts();
                stolons.DoCleanTransferAmounts();
                roots.DoCleanTransferAmounts();
            }
            else
            {
                // no actual growth, just zero out some variables
                dGrowthShootDM = 0.0;
                dGrowthRootDM = 0.0;
            }
        }

        /// <summary>Computes the turnover rates for each tissue pool of all plant organs</summary>
        /// <remarks>
        /// The rate are passe on to each organ and the amounts potentially turned over are computed for each tissue.
        /// The turnover rates are affected by variations in soil water and air temperature. For leaves the number of leaves
        ///  per tiller (LiveLeavesPerTiller, a parameter specific for each species) also influences the turnover rate.
        /// The C and N amounts potentially available for remobilisation are also computed in here.
        /// </remarks>
        internal void EvaluateTissueTurnoverRates()
        {
            // Get the temperature factor for tissue turnover
            ttfTemperature = TempFactorForTissueTurnover(Tmean(0.5));

            // Get the moisture factor for shoot tissue turnover
            ttfMoistureShoot = WaterFactorForTissueTurnover();

            // TODO: find a way to use todays GLFwater, or to compute an alternative one

            // Get the moisture factor for littering rate (detachment)
            double ttfMoistureLitter = Math.Pow(glfWater, 3);

            // Consider the number of leaves
            ttfLeafNumber = 3.0 / LiveLeavesPerTiller; // three refers to the number of stages used in the model

            // Get the moisture factor for root tissue turnover
            ttfMoistureRoot = 2.0 - Math.Min(glfWater, glfAeration);

            //stocking rate affecting transfer of dead to litter (default as 0 for now - should be read in)
            double SR = 0;
            double StockFac2Litter = StockParameter * SR;

            // Turnover rate for leaf and stem tissues
            gama = TissueTurnoverRateShoot * ttfTemperature * ttfMoistureShoot;

            // Turnover rate for dead to litter (detachment)
            double digestDead = (leaves.DigestibilityDead * leaves.DMDead) + (stems.DigestibilityDead * stems.DMDead);
            digestDead = MathUtilities.Divide(digestDead, leaves.DMDead + stems.DMDead, 0.0);
            gamaD = DetachmentRate * ttfMoistureLitter * digestDead / CarbonFractionInDM;
            gamaD += StockFac2Litter;

            // Turnover rate for roots
            gamaR = TissueTurnoverRateRoot * ttfTemperature * ttfMoistureRoot;

            // Check whether any adjust on turnover rates are needed
            if ((gama + gamaD + gamaR) > Epsilon)
            {
                // Check phenology effect for annuals
                if (isAnnual && phenologicStage > 0)
                {
                    if (phenologicStage == 1)
                    {
                        //vegetative, turnover is zero at emergence and increases with age
                        gama *= phenoFactor;
                        gamaR *= Math.Pow(phenoFactor, 2.0);
                        gamaD *= phenoFactor;
                    }
                    else if (phenologicStage == 2)
                    {
                        //reproductive, turnover increases with age and reach one at maturity
                        gama += (1.0 - gama) * Math.Pow(phenoFactor, 2.0);
                        gamaR = (1.0 - gamaR) * Math.Pow(phenoFactor, 3.0);
                        gamaD = (1.0 - gamaD) * Math.Pow(phenoFactor, 3.0);
                    }
                }

                // Adjust turnover if senescence will result in dmGreen < dmGreenmin (perennials only)
                if (!isAnnual)
                {
                    double dmGreenToBe = AboveGroundLiveWt + dGrowthShootDM - (MatureTissuesWt * gama);
                    if (dmGreenToBe < MinimumGreenWt)
                    {
                        double gama_adj = MathUtilities.Divide(AboveGroundLiveWt + dGrowthShootDM - MinimumGreenWt, MatureTissuesWt, 0.0);
                        gamaR *= gama_adj / gama;
                        gamaD *= gama_adj / gama;
                        gama = gama_adj;
                    }

                    // set a minimum for roots too
                    if (roots.DMLive * (1.0 - gamaR) < MinimumGreenWt * MinimumRootProp)
                        gamaR = 0.0;
                }

                // Turnover rate for stolon
                if (isLegume)
                {
                    // base rate is the same as for the other above ground organs
                    gamaS = gama;

                    // Adjust stolon turnover due to defoliation (increases stolon senescence)
                    gamaS += fractionDefoliated * (1.0 - gamaS);
                }
                else
                    gamaS = 0.0;

                //// Do the actual turnover, update DM and N

                // Leaves and stems
                double[] turnoverRates = new double[] {gama * RelativeTurnoverGrowing, gama, gama, gamaD};
                leaves.DoTissueTurnover(turnoverRates);
                stems.DoTissueTurnover(turnoverRates);

                // Stolons
                if (isLegume)
                {
                    turnoverRates = new double[] {gamaS * RelativeTurnoverGrowing, gamaS, gamaS, 1.0};
                    stolons.DoTissueTurnover(turnoverRates);
                }

                // Roots (only 2 tissues)
                turnoverRates = new double[] {gamaR, 1.0};
                roots.DoTissueTurnover(turnoverRates);

                // TODO: consider C remobilisation
                // ChRemobSugar = dSenescedRoot * KappaCRemob;
                // ChRemobProtein = dSenescedRoot * (roots.Tissue[0].Nconc - roots.NConcMinimum) * CNratioProtein * FacCNRemob;
                // senescedRootDM -= ChRemobSugar + ChRemobProtein;
                // CRemobilisable += ChRemobSugar + ChRemobProtein;

                // N remobilised from senesced tissues to be potentially used for growth today
                NSenescedRemobilisable = leaves.NSenescedRemobilisable + stems.NSenescedRemobilisable
                                       + stolons.NSenescedRemobilisable + roots.NSenescedRemobilisable;

                // C remobilised from senesced tissues to be used in new growth (converted from carbohydrate to C)
                remobilisableC += 0.0;
                remobilisableC *= CarbonFractionInDM;

                // Get the amounts senesced and detached today
                senescedShootDM = leaves.DMSenesced + stems.DMSenesced + stolons.DMSenesced;
                senescedShootN = leaves.NSenesced + stems.NSenesced + stolons.NSenesced;
                detachedShootDM = leaves.DMDetached + stems.DMDetached + stolons.DMDetached;
                detachedShootN = leaves.NDetached + stems.NDetached + stolons.NDetached;
                senescedRootDM = roots.DMSenesced;
                senescedRootN = roots.NSenesced;
                detachedRootDM = roots.DMDetached;
                detachedRootN = roots.NDetached;
            }

            // N remobilisable from luxury N to be potentially used for new growth
            NLuxuryRemobilisable = leaves.NLuxuryRemobilisable + stems.NLuxuryRemobilisable
                                 + stolons.NLuxuryRemobilisable + roots.NLuxuryRemobilisable;
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
                if (Math.Abs(WaterTakenUp.DeltaWater.Sum() + myWaterDemand) > Epsilon)
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
            newGrowthN = Nfixation + NSenescedRemobilised + mySoilNuptake;

            // evaluate whether further remobilisation (from luxury N) is needed
            EvaluateNLuxuryRemobilisation();
            newGrowthN += NLuxuryRemobilised;
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
            if (myNitrogenUptakeSource == "species")
            {
                // this module will compute the N uptake
                MyNitrogenCalculations();
                if (newGrowthN > Epsilon)
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

                if (newGrowthN > Epsilon)
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
                newGrowthN = Nfixation + NSenescedRemobilised + mySoilNuptake;

                // evaluate whether further remobilisation (from luxury N) is needed
                EvaluateNLuxuryRemobilisation();
                newGrowthN += NLuxuryRemobilised;

                // send delta N to the soil model
                DoSoilNitrogenUptake();
            }
            //else
            //    N available is computed in another module

        }

        /// <summary>Computes the N demanded for optimum N content as well as luxury uptake</summary>
        internal void CalcNDemand()
        {
            double toRoot = dGrowthWstress * (1.0 - fractionToShoot);
            double toStol = dGrowthWstress * fractionToShoot * FractionToStolon;
            double toLeaf = dGrowthWstress * fractionToShoot * fractionToLeaf;
            double toStem = dGrowthWstress * fractionToShoot * (1.0 - FractionToStolon - fractionToLeaf);

            // N demand for new growth, with optimum N (kg/ha)
            NdemandOpt = (toLeaf * leaves.NConcOptimum) + (toStem * stems.NConcOptimum)
                       + (toStol * stolons.NConcOptimum) + (toRoot * roots.NConcOptimum);

            // get the factor to reduce the demand under elevated CO2
            double fN = NFactorDueToCO2();
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
                if (NdemandLux > Epsilon && (NdemandLux > mySoilAvailableN.Sum() + iniFix))
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
            double fracRemobilised = 0.0;
            if (Nfixation - NdemandLux > -0.0001)
            {
                // N demand is fulfilled by fixation alone
                Nfixation = NdemandLux; // should not be needed, but just in case...
                NSenescedRemobilised = 0.0;
                mySoilNDemand = 0.0;
            }
            else if (NdemandLux-(Nfixation + NSenescedRemobilisable) < Epsilon)
            {
                // N demand is fulfilled by fixation plus N remobilised from senescent material
                NSenescedRemobilised = Math.Max(0.0, NdemandLux - Nfixation);
                mySoilNDemand = 0.0;
                fracRemobilised = NSenescedRemobilised / NSenescedRemobilisable;
            }
            else
            {
                // N demand is greater than fixation and remobilisation of senescent, N uptake is needed
                NSenescedRemobilised = NSenescedRemobilisable;
                mySoilNDemand = NdemandLux - (Nfixation + NSenescedRemobilised);
                fracRemobilised = 1.0;
            }

            // Update N remobilised in each organ
            if (NSenescedRemobilised > Epsilon)
            {
                leaves.Tissue[leaves.TissueCount - 1].DoRemobiliseN(fracRemobilised);
                stems.Tissue[stems.TissueCount - 1].DoRemobiliseN(fracRemobilised);
                stolons.Tissue[stolons.TissueCount - 1].DoRemobiliseN(fracRemobilised);
                roots.Tissue[roots.TissueCount - 1].DoRemobiliseN(fracRemobilised);
            }

            // variable used by arbitrator - TODO: put this is a better place
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
        internal void EvaluateNLuxuryRemobilisation()
        {
            // check whether there is still demand for N (only match demand for growth at optimum N conc.)
            // check whether there is any luxury N remobilisable
            double Nmissing = NdemandOpt - newGrowthN;
            if ((Nmissing > Epsilon) && (NLuxuryRemobilisable > Epsilon))
            {
                // all N already considered is not enough to match demand for growth, check remobilisation of luxury N
                if (Nmissing >= NLuxuryRemobilisable)
                {
                    // N luxury is just or not enough for optimum growth, use up all there is
                    if (NLuxuryRemobilisable > Epsilon)
                    {
                        NLuxuryRemobilised = NLuxuryRemobilisable;
                        Nmissing -= NLuxuryRemobilised;

                        // remove the luxury N
                        for (int tissue = 0; tissue < 3; tissue++)
                        {
                            leaves.Tissue[tissue].DoRemobiliseN(1.0);
                            stems.Tissue[tissue].DoRemobiliseN(1.0);
                            stolons.Tissue[tissue].DoRemobiliseN(1.0);
                            if (tissue == 0)
                                roots.Tissue[tissue].DoRemobiliseN(1.0);
                        }
                    }
                }
                else
                {
                    // Available luxury N is enough for optimum growth, go through tissues and get what is needed, start on mature
                    double Nluxury;
                    double Nusedup;
                    double fracRemobilised;
                    for (int tissue = 2; tissue >= 0; tissue--)
                    {
                        Nluxury = leaves.Tissue[tissue].NRemobilisable + stems.Tissue[tissue].NRemobilisable + stolons.Tissue[tissue].NRemobilisable;
                        if (tissue == 0)
                            Nluxury += roots.Tissue[tissue].NRemobilisable;
                        Nusedup = Math.Min(Nluxury, Nmissing);
                        fracRemobilised = MathUtilities.Divide(Nusedup, Nluxury, 0.0);
                        leaves.Tissue[tissue].DoRemobiliseN(fracRemobilised);
                        stems.Tissue[tissue].DoRemobiliseN(fracRemobilised);
                        stolons.Tissue[tissue].DoRemobiliseN(fracRemobilised);
                        if (tissue == 0)
                            roots.Tissue[tissue].DoRemobiliseN(fracRemobilised);

                        NLuxuryRemobilised += Nusedup;
                        Nmissing -= Nusedup;
                        if (Nmissing <= Epsilon) tissue = 0;
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
            // reset variables for whole plant
            dmDefoliated = 0.0;
            Ndefoliated = 0.0;
            digestDefoliated = 0.0;

            dGrowthShootDM = 0.0;
            dGrowthShootN = 0.0;
            dGrowthRootDM = 0.0;
            dGrowthRootN = 0.0;

            senescedShootDM = 0.0;
            senescedShootN = 0.0;
            detachedShootDM = 0.0;
            detachedShootN = 0.0;
            senescedRootDM = 0.0;
            senescedRootN = 0.0;
            detachedRootDM = 0.0;
            detachedRootN = 0.0;

            NSenescedRemobilised = 0.0;
            NSenescedRemobilisable = 0.0;
            NLuxuryRemobilisable = 0.0;
            NLuxuryRemobilised = 0.0;

            // reset transfer variables for all tissues in each organ
            leaves.DoCleanTransferAmounts();
            stems.DoCleanTransferAmounts();
            stolons.DoCleanTransferAmounts();
            roots.DoCleanTransferAmounts();
        }

        /// <summary>Computes a growth factor for annual species, related to phenology/population</summary>
        /// <returns>A growth factor (0-1)</returns>
        private double AnnualSpeciesGrowthFactor()
        {
            double rFactor = 1.0;
            if (phenologicStage == 1 && daysSinceEmergence < daysAnnualsFactor)
            {
                // reduction at the begining due to population effects ???
                rFactor -= 0.5 * (1.0 - (daysSinceEmergence / daysAnnualsFactor));
            }
            else if (phenologicStage == 2)
            {
                // decline of photosynthesis when approaching maturity
                rFactor -= (daysSinceEmergence - daysEmergenceToAnthesis) / daysAnthesisToMaturity;
            }

            return rFactor;
        }

        /// <summary>Calculate the factor increasing shoot allocation during reproductive growth</summary>
        /// <remarks>
        /// This mimics the changes in DM allocation during reproductive season; allocation to shoot increases up to a maximum
        ///  value (defined by allocationIncreaseRepro). This value is used during the main phase, two shoulder periods are
        ///  defined on either side of the main phase (duration is given by reproSeasonInterval, translated into days of year),
        ///  Onset phase goes between doyA and doyB, main pahse between doyB and doyC, and outset between doyC and doyD.
        /// Note: The days have to be set as doubles or the division operations will be rounded and be sligtly wrong.
        /// </remarks>
        /// <returns>A factor to correct shoot allocation</returns>
        private double CalcReproductiveGrowthFactor()
        {
            double result = 1.0;
            int yearLength = 365 + (DateTime.IsLeapYear(myClock.Today.Year) ? 1 : 0);
            double doy = myClock.Today.DayOfYear;
            double doyA = doyIniReproSeason;
            double doyB = doyA + reproSeasonInterval[0];
            double doyC = doyB + reproSeasonInterval[1];
            double doyD = doyC + reproSeasonInterval[2];

            if (doy > doyA)
            {
                if (doy <= doyB)
                    result += allocationIncreaseRepro * (doy - doyA) / (doyB - doyA);
                else if (doy <= doyC)
                    result += allocationIncreaseRepro;
                else if (doy <= doyD)
                    result += allocationIncreaseRepro * (1 - (doy - doyC) / (doyD - doyC));
            }
            else
            {
                // check whether the high allocation period goes across the year (should only be needed for southern hemisphere)
                if ((doyC > yearLength) && (doy <= doyC - yearLength))
                    result += allocationIncreaseRepro;
                else if ((doyD > yearLength) && (doy <= doyD - yearLength))
                    result += allocationIncreaseRepro * (1 - (yearLength + doy - doyC) / (doyD - doyC));
            }

            return result;
        }

        /// <summary>Computes the allocations into shoot and leaves of todays growth</summary>
        internal void EvaluateGrowthAllocation()
        {
            EvaluateAllocationToShoot();
            EvaluateAllocationToLeaf();
        }

        /// <summary>Calculates the fraction of new growth allocated to shoot</summary>
        /// <remarks>
        /// Allocation of new growth to shoot is a function of the current and a target (ideal) Shoot-Root ratio; it is further
        ///  modified according to soil's growth limiting factors (plants favour root growth when water or N are limiting).
        /// The target Shoot-Root ratio will be adjusted (increased) during spring for mimicking changes in DM allocation during
        ///  the reproductive season if usingReproSeasonFactor.
        /// The allocation to shoot may be further modified to ensure a minimum allocation (= 1.0 - MaxRootAllocation).
        /// </remarks>
        private void EvaluateAllocationToShoot()
        {
            if (roots.DMLive > Epsilon)
            {
                // get the soil related growth limiting factor (the smaller this is the higher the allocation of DM to roots)
                double glfMin = Math.Min(Math.Min(glfWater, glfAeration), glfN);

                // get the actual effect of limiting factors on SR (varies between one and ShootRootGlfFactor)
                double glfFactor = 1.0 - ShootRootGlfFactor * (1.0 - Math.Pow(glfMin, 1.0 / ShootRootGlfFactor));

                // get the current shoot/root ratio (partiton will try to make this value closer to targetSR)
                double currentSR = MathUtilities.Divide(AboveGroundLiveWt, roots.DMLive, 1000000.0);

                // get the factor for the reproductive season of perennials (increases shoot allocation during spring)
                double reproFac = 1.0;
                if (usingReproSeasonFactor && !isAnnual)
                    reproFac = CalcReproductiveGrowthFactor();

                // get today's target SR
                double targetSR = TargetSRratio * reproFac;

                // update todays shoot:root partition
                double growthSR = targetSR * glfFactor * targetSR / currentSR;

                // compute fraction to shoot
                fractionToShoot = growthSR / (1.0 + growthSR);
            }
            else
            {
                // use default value, this should not happen (might happen if plant is dead)
                fractionToShoot = 1.0;
            }

            // check for maximum root allocation (kept here mostly for backward compatibility)
            if ((1 - fractionToShoot) > MaxRootAllocation)
                fractionToShoot = 1 - MaxRootAllocation;
        }

        /// <summary>Computes the fraction of new shoot DM that is allocated to leaves</summary>
        /// <remarks>
        /// This method is used to reduce the propotion of leaves as plants grow, this is used for species that 
        ///  allocate proportionally more DM to stolon/stems when the whole plant's DM is high.
        /// To avoid too little allocation to leaves in case of grazing the current leaf:stem ratio is evaluated
        ///  and used to modify the targeted value in a similar way as shoot:root ratio.
        /// </remarks>
        private void EvaluateAllocationToLeaf()
        {
            // compute new target FractionLeaf
            double targetFLeaf = FractionLeafMaximum;
            if ((FractionLeafMinimum < FractionLeafMaximum) && (AboveGroundLiveWt > FractionLeafDMThreshold))
            {
                double fLeafAux = (AboveGroundLiveWt - FractionLeafDMThreshold) / (FractionLeafDMFactor - FractionLeafDMThreshold);
                fLeafAux = Math.Pow(fLeafAux, FractionLeafExponent);
                targetFLeaf = FractionLeafMinimum + (FractionLeafMaximum - FractionLeafMinimum) / (1.0 + fLeafAux);
            }

            // get current leaf:stem ratio
            double currentLS = leaves.DMLive / (stems.DMLive + stolons.DMLive);

            // get today's target leaf:stem ratio
            double targetLS = targetFLeaf / (1 - targetFLeaf);

            // adjust leaf:stem ratio, to avoid excess allocation to stem/stolons
            double newLS = targetLS * targetLS / currentLS;

            fractionToLeaf = newLS / (1 + newLS);
        }

        /// <summary>Computes the variations in root depth</summary>
        /// <remarks>
        /// Root depth will increase if it is smaller than maximumRootDepth and there is a positive net DM accumulation.
        /// The depth increase rate is of zero-order type, given by the RootElongationRate, but it is adjusted for temperature
        ///  in a similar fashion as plant DM growth. Note that currently root depth never decreases.
        /// </remarks>
        private void EvaluateRootElongation()
        {
            // Check changes in root depth
            dRootDepth = 0.0;
            if (phenologicStage > 0)
            {
                if (((dGrowthRootDM - detachedRootDM) > Epsilon) && (roots.Depth < MaximumRootDepth))
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
            }
        }

        /// <summary>Computes the allocation of new growth to roots for each layer</summary>
        /// <remarks>
        /// The current target distribution for roots changes whenever then root depth changes, this is then used to allocate 
        ///  new growth to each layer within the root zone. The existing distribution is used on any DM removal, so it may
        ///  take some time for the actual distribution to evolve to be equal to the target.
        /// </remarks>
        private void DoRootGrowthAllocation()
        {
            if (dGrowthRootDM > Epsilon)
            {
                // root DM is changing due to growth, check potential changes in distribution
                double[] growthRootFraction;
                double[] currentRootTarget = CurrentRootDistributionTarget();
                if (MathUtilities.AreEqual(roots.Tissue[0].FractionWt, currentRootTarget))
                {
                    // no need to change the distribution
                    growthRootFraction = roots.Tissue[0].FractionWt;
                }
                else
                {
                    // root distribution should change, get preliminary distribution (average of current and target)
                    growthRootFraction = new double[nLayers];
                    for (int layer = 0; layer <= roots.BottomLayer; layer++)
                        growthRootFraction[layer] = 0.5 * (roots.Tissue[0].FractionWt[layer] + currentRootTarget[layer]);

                    // normalise distribution of allocation
                    double layersTotal = growthRootFraction.Sum();
                    for (int layer = 0; layer <= roots.BottomLayer; layer++)
                        growthRootFraction[layer] = growthRootFraction[layer] / layersTotal;
                }

                // allocate new growth to each layer in the root zone
                for (int layer = 0; layer <= roots.BottomLayer; layer++)
                {
                    roots.Tissue[0].DMLayersTransferedIn[layer] = dGrowthRootDM * growthRootFraction[layer];
                    roots.Tissue[0].NLayersTransferedIn[layer] = dGrowthRootN * growthRootFraction[layer];
                }
            }
        }

        /// <summary>Calculates the plant height as function of DM</summary>
        /// <returns>Plant height (mm)</returns>
        internal double HeightfromDM()
        {
            double TodaysHeight = MaximumPlantHeight;

            // TODO: remove the multiplier (0.1) when DM is given in g/m2
            if (StandingWt * 0.1 <= MassForMaximumHeight)
            {
                double massRatio = StandingWt * 0.1 / MassForMaximumHeight;
                double heightF = ExponentHeightFromMass - (ExponentHeightFromMass * massRatio) + massRatio;
                heightF *= Math.Pow(massRatio, ExponentHeightFromMass - 1);
                TodaysHeight *= heightF;
            }

            return Math.Max(TodaysHeight, MinimumPlantHeight);
        }

        /// <summary>Computes the values of LAI (leaf area index) for green and dead plant material</summary>
        /// <remarks>This method considers leaves plus an additional effect of stems and stolons</remarks>
        private void EvaluateLAI()
        {
            // Get the amount of green tissue of leaves (converted from kg/ha to kg/m2)
            double greenTissue = leaves.DMLive / 10000;
            greenLAI = greenTissue * SpecificLeafArea;

            if (usingStemStolonEffect)
            {
                // Get a proportion of green tissue from stolons
                greenTissue = stolons.DMLive * StolonEffectOnLAI / 10000;

                // Consider some green tissue from sheath/stems and stolons
                if (!isLegume && AboveGroundLiveWt < ShootMaxEffectOnLAI)
                {
                    double shootFactor = MaxStemEffectOnLAI * Math.Sqrt(1.0 - (AboveGroundLiveWt / ShootMaxEffectOnLAI));
                    greenTissue += stems.DMLive * shootFactor / 10000;
                }

                greenLAI += greenTissue * SpecificLeafArea;

                /* This adjust helps on resilience after unfavoured conditions (implemented by F.Li, not present in EcoMod)
                 It is assumed that green cover will be bigger for the same amount of DM when compared to using only leaves due
                  to the recruitment of green tissue from stems and stolons. Thus it mimics:
                 - greater light extinction coefficient, leaves will be more horizontal than in dense high swards
                 - more parts (stems) turning green for photosysnthesis
                 - thinner leaves during growth burst following unfavoured conditions
                 » TODO: It would be better if variations in SLA or ext. coeff. would be explicitly considered (RCichota, 2014)
                */
            }

            deadLAI = (leaves.DMDead / 10000) * SpecificLeafArea;
        }

        /// <summary>Compute the average digestibility of aboveground plant material</summary>
        /// <returns>The digestibility of plant material (0-1)</returns>
        private void EvaluateDigestibility()
        {
            double result = 0.0;
            if (AboveGroundWt > Epsilon)
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
            if (removedWt > Epsilon)
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
            if (amountRequired > Epsilon)
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

            if (HarvestableWt > Epsilon)
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
                if (tempTotal > Epsilon)
                {
                    fractionToHarvestGreen = tempRemovableGreen * tempPrefGreen / tempTotal;
                    fractionToHarvestDead = tempRemovableDead * tempPrefDead / tempTotal;
                }

                // get amounts removed
                double RemovingGreenDM = AmountToRemove * fractionToHarvestGreen;
                double RemovingDeadDM = AmountToRemove * fractionToHarvestDead;

                // Fraction of DM remaining in the field
                double fractionRemainingGreen = 1.0;
                if (StandingLiveWt > Epsilon)
                    fractionRemainingGreen = Math.Max(0.0, Math.Min(1.0, 1.0 - RemovingGreenDM / StandingLiveWt));
                double fractionRemainingDead = 1.0;
                if (StandingDeadWt > Epsilon)
                    fractionRemainingDead = Math.Max(0.0, Math.Min(1.0, 1.0 - RemovingDeadDM / StandingDeadWt));

                // get digestibility of DM being harvested
                digestDefoliated = calcHarvestDigestibility(leaves.DMLive * fractionToHarvestGreen, leaves.DMDead * fractionToHarvestDead,
                                                            stems.DMLive * fractionToHarvestGreen, stems.DMDead * fractionToHarvestDead,
                                                            stolons.DMLive * fractionToHarvestGreen, stolons.DMDead * fractionToHarvestDead);

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
                NSenescedRemobilisable *= fractionRemainingGreen;
                remobilisableC *= fractionRemainingGreen;

                // update Luxury N pools
                NLuxuryRemobilisable *= fractionRemainingGreen;

                // save fraction defoliated (to be used on tissue turnover)
                fractionDefoliated = MathUtilities.Divide(dmDefoliated, dmDefoliated + AboveGroundWt, 0.0); //TODO: it should use StandingLiveWt

                // check mass balance and set outputs
                dmDefoliated = PreRemovalDM - AboveGroundWt;
                Ndefoliated = PreRemovalN - AboveGroundN;
                if (Math.Abs(dmDefoliated - AmountToRemove) > Epsilon)
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
            digestDefoliated = calcHarvestDigestibility(leaves.DMLive * fractionToRemove, leaves.DMDead * fractionToRemove,
                                                        stems.DMLive * fractionToRemove, stems.DMDead * fractionToRemove,
                                                        stolons.DMLive * fractionToRemove, stolons.DMDead * fractionToRemove);

            for (int i = 0; i < RemovalData.dm.Length; i++) // for each pool (green or dead)
            {
                string plantPool = RemovalData.dm[i].pool;
                for (int j = 0; j < RemovalData.dm[i].dlt.Length; j++) // for each part (leaf or stem)
                {
                    string plantPart = RemovalData.dm[i].part[j];
                    double amountToRemove = RemovalData.dm[i].dlt[j] * 10.0; // convert to kgDM/ha
                    if (plantPool.ToLower() == "green" && plantPart.ToLower() == "leaf")
                    {
                        if (LeafGreenWt - amountToRemove > Epsilon)
                        {
                            fractionToRemove = MathUtilities.Divide(amountToRemove, LeafGreenWt, 0.0);
                            RemoveFractionDM(fractionToRemove, plantPool, plantPart);
                        }
                    }
                    else if (plantPool.ToLower() == "green" && plantPart.ToLower() == "stem")
                    {
                        if (StemGreenWt - amountToRemove > Epsilon)
                        {
                            fractionToRemove = MathUtilities.Divide(amountToRemove, StemGreenWt, 0.0);
                            RemoveFractionDM(fractionToRemove, plantPool, plantPart);
                        }
                    }
                    else if (plantPool.ToLower() == "dead" && plantPart.ToLower() == "leaf")
                    {
                        if (LeafDeadWt - amountToRemove > Epsilon)
                        {
                            fractionToRemove = MathUtilities.Divide(amountToRemove, LeafDeadWt, 0.0);
                            RemoveFractionDM(fractionToRemove, plantPool, plantPart);
                        }
                    }
                    else if (plantPool.ToLower() == "dead" && plantPart.ToLower() == "stem")
                    {
                        if (StemDeadWt - amountToRemove > Epsilon)
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
            leaves.DoResetOrgan();
            stems.DoResetOrgan();
            stolons.DoResetOrgan();
            roots.DoResetOrgan();
            SetInitialState();
        }

        /// <summary>Kill parts of this plant</summary>
        /// <param name="KillData">Fraction of crop to be killed</param>
        [EventSubscribe("KillCrop")]
        public void OnKillCrop(KillCropType KillData)
        {
            double fractioToKill = MathUtilities.Bound(KillData.KillFraction, 0.0, 1.0);
            if (fractioToKill < 1.0)
            {
                // transfer fracton of live tissues into dead, will be detached later
                leaves.DoKillOrgan(fractioToKill);
                stems.DoKillOrgan(fractioToKill);
                stolons.DoKillOrgan(fractioToKill);
                roots.DoKillOrgan(fractioToKill);
            }
            else
            {
                // kill off the plant
                EndCrop();
            }
        }

        /// <summary>Reset this plant to zero (kill crop)</summary>
        public void ResetZero()
        {
            // Zero out the DM and N pools is all organs and tissues
            leaves.DoResetOrgan();
            stems.DoResetOrgan();
            stolons.DoResetOrgan();
            roots.DoResetOrgan();

            // Zero out the variables for whole plant
            dmDefoliated = 0.0;
            Ndefoliated = 0.0;
            digestDefoliated = 0.0;

            dGrowthShootDM = 0.0;
            dGrowthShootN = 0.0;
            dGrowthRootDM = 0.0;
            dGrowthRootN = 0.0;

            senescedShootDM = 0.0;
            senescedShootN = 0.0;
            detachedShootDM = 0.0;
            detachedShootN = 0.0;
            senescedRootDM = 0.0;
            senescedRootN = 0.0;
            detachedRootDM = 0.0;
            detachedRootN = 0.0;

            NSenescedRemobilised = 0.0;
            NSenescedRemobilisable = 0.0;
            NLuxuryRemobilisable = 0.0;
            NLuxuryRemobilised = 0.0;
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
                if (Supply > Epsilon)
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

        /// <summary>Computes the reduction factor for photosynthesis due to heat damage</summary>
        /// <remarks>Stress computed as function of daily maximum temperature, recovery based on average temp.</remarks>
        /// <returns>The reduction in photosynthesis rate (0-1)</returns>
        private double HeatStress()
        {
            if (usingHeatStressFactor)
            {
                double heatFactor;
                if (myMetData.MaxT > HeatFullTemp)
                {
                    // very high temperature, full stress
                    heatFactor = 0.0;
                    accumDDHeat = 0.0;
                }
                else if (myMetData.MaxT > HeatOnsetTemp)
                {
                    // high temperature, add some stress
                    heatFactor = highTempStress * (HeatFullTemp - myMetData.MaxT) / (HeatFullTemp - HeatOnsetTemp);
                    accumDDHeat = 0.0;
                }
                else
                {
                    // cool temperature, same stress as yesterday
                    heatFactor = highTempStress;
                }

                // check recovery factor
                double recoveryFactor = 0.0;
                if (myMetData.MaxT <= HeatOnsetTemp)
                    recoveryFactor = (1.0 - heatFactor) *(accumDDHeat / HeatSumDD);

                // accumulate temperature
                accumDDHeat += Math.Max(0.0, HeatRecoverTemp - Tmean(0.5));

                // heat stress
                highTempStress = Math.Min(1.0, heatFactor + recoveryFactor);

                return highTempStress;
            }
            return 1.0;
        }

        /// <summary>Computes the reduction factor for photosynthesis due to cold damage (frost)</summary>
        /// <remarks>Stress computed as function of daily minimum temperature, recovery based on average temp.</remarks>
        /// <returns>The reduction in photosynthesis rate (0-1)</returns>
        private double ColdStress()
        {
            if (usingColdStressFactor)
            {
                double coldFactor;
                if (myMetData.MinT < ColdFullTemp)
                {
                    // very low temperature, full stress
                    coldFactor = 0.0;
                    accumDDCold = 0.0;
                }
                else if (myMetData.MinT < ColdOnsetTemp)
                {
                    // low temperature, add some stress
                    coldFactor = lowTempStress * (myMetData.MinT - ColdFullTemp) / (ColdOnsetTemp - ColdFullTemp);
                    accumDDCold = 0.0;
                }
                else
                {
                    // warm temperature, same stress as yesterday
                    coldFactor = lowTempStress;
                }

                // check recovery factor
                double recoveryFactor = 0.0;
                if (myMetData.MinT >= ColdOnsetTemp)
                    recoveryFactor = (1.0 - coldFactor) * (accumDDCold / ColdSumDD);

                // accumulate temperature
                accumDDCold += Math.Max(0.0, Tmean(0.5) - ColdRecoverTemp);
                
                // cold stress
                lowTempStress = Math.Min(1.0, coldFactor + recoveryFactor);

                return lowTempStress;
            }
            else
                return 1.0;
        }

        /// <summary>Computes the relative effect of atmospheric CO2 on photosynthesis.</summary>
        /// <returns>A factor to adjust photosynthesis due to CO2</returns>
        private double CO2EffectOnPhotosynthesis()
        {
            if (Math.Abs(myMetData.CO2 - ReferenceCO2) < 0.01)
                return 1.0;

            double termActual = myMetData.CO2 / (myMetData.CO2 + CO2EffectScaleFactor);
            double termReference = (ReferenceCO2 + CO2EffectScaleFactor) / ReferenceCO2;
            return termActual * termReference;
        }

        /// <summary>Computes the relative effect of leaf N concentration on photosynthesis.</summary>
        /// <remarks>
        /// This mimics the effect that N concentration have on the amount of chlorophyll (assumed directly proportional to N conc).
        /// The effect is adjusted by a function of atmospheric CO2 (plants need less N at high CO2).
        /// </remarks>
        /// <returns>A factor to adjust photosynthesis due to N concentration</returns>
        private double NConcentrationEffect()
        {
            // get variation in N optimum due to CO2
            double fN = NFactorDueToCO2();

            // get chlorophyll effect
            double effect = 0.0;
            if (leaves.NconcLive > leaves.NConcMinimum)
            {
                if (leaves.NconcLive < leaves.NConcOptimum * fN)
                    effect = MathUtilities.Divide(leaves.NconcLive - leaves.NConcMinimum, (leaves.NConcOptimum * fN) - leaves.NConcMinimum, 1.0);
                else
                    effect = 1.0;
            }

            effect = MathUtilities.Bound(effect, 0.0, 1.0);
            return effect;
        }

        /// <summary>Computes the variation in optimum N in leaves due to atmospheric CO2</summary>
        /// <returns>A factor to adjust optimum N in leaves</returns>
        private double NFactorDueToCO2()
        {
            if (Math.Abs(myMetData.CO2 - ReferenceCO2) < 0.01)
                return 1.0;

            double factorCO2 = Math.Pow((CO2EffectOffsetFactor - ReferenceCO2) / (myMetData.CO2 - ReferenceCO2), CO2EffectExponent);
            double effect = (CO2EffectMinimum + factorCO2) / (1 + factorCO2);

            return effect;
        }

        /// <summary>Computes the variation in stomata conductances due to variation in atmospheric CO2.</summary>
        /// <returns>Stomata conductuctance</returns>
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

            if (myWaterDemand <= Epsilon)
                result = 1.0;
            else
                result = mySoilWaterTakenUp.Sum() / myWaterDemand;

            return Math.Max(0.0, Math.Min(1.0, result));
        }

        /// <summary>Growth limiting factor due to excess of water in the soil (logging/lack of aeration)</summary>
        /// <remarks>
        /// Growth is limited if soil water content is above a given threshold (defined by MinimumWaterFreePorosity), which
        ///  will be the soil DUL is MinimumWaterFreePorosity is set to a negative value. If usingCumulativeWaterLogging, 
        ///  growth is limited by the cumulative value of the logging effect, with maximum increment in one day equal to 
        ///  SoilWaterSaturationFactor. Recovery happens if water content is below the threshold set by MinimumWaterFreePorosity
        /// </remarks>
        /// <returns>The limiting factor due to excess in soil water</returns>
        internal double WaterLoggingFactor()
        {
            double effect = 0.0;
            double mySWater = 0.0;
            double mySAT = 0.0;
            double myDUL = 0.0;
            double fractionLayer = 0.0;

            // gather water status over the root zone
            for (int layer = 0; layer < roots.BottomLayer; layer++)
            {
                // fraction of layer with roots 
                fractionLayer = FractionLayerWithRoots(layer);
                // actual soil water content
                mySWater += mySoil.Water[layer] * fractionLayer;
                // water content at saturation
                mySAT += mySoil.SoilWater.SATmm[layer] * fractionLayer;
                // water content at low threshold for limitation (correspond to minimum water-free pore space)
                if (MinimumWaterFreePorosity <= -Epsilon)
                    myDUL += mySoil.SoilWater.DULmm[layer] * fractionLayer;
                else
                    myDUL = mySoil.SoilWater.SATmm[layer] * (1.0 - MinimumWaterFreePorosity) * fractionLayer;
            }

            if (mySWater > myDUL)
                effect = SoilWaterSaturationFactor * (mySWater - myDUL) / (mySAT - myDUL);
            else
                effect = -SoilWaterSaturationRecoveryFactor;

            cumWaterLogging = MathUtilities.Bound(cumWaterLogging + effect, 0.0, 1.0);

            if (usingCumulativeWaterLogging)
                effect = 1.0 - cumWaterLogging;
            else
                effect = MathUtilities.Bound(1.0 - effect, 0.0, 1.0);

            return effect;
        }

        /// <summary>Effect of water stress on tissue turnover</summary>
        /// <remarks>Tissue turnover is higher under water stress, GLFwater to mimic that effect</remarks>
        /// <returns>Water stress factor for tissue turnover (0-1)</returns>
        private double WaterFactorForTissueTurnover()
        {
            double effect = 1.0;
            if (Math.Min(glfWater, glfAeration) < TissueTurnoverDroughtThreshold)
            {
                effect = (TissueTurnoverDroughtThreshold - Math.Min(glfWater, glfAeration)) / TissueTurnoverDroughtThreshold;
                effect = 1.0 + TissueTurnoverDroughtMax * effect;
            }

            return effect;
        }

        /// <summary>Computes the ground cover for the plant, or plant part</summary>
        /// <param name="givenLAI">The LAI for this plant</param>
        /// <returns>Fraction of ground effectively covered (0-1)</returns>
        private double CalcPlantCover(double givenLAI)
        {
            if (givenLAI < Epsilon) return 0.0;
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
            if (cumProportion > Epsilon)
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
}
