//-----------------------------------------------------------------------
// <copyright file="AgPasture.PastureSpecies.cs" project="AgPasture" solution="APSIMx" company="APSIM Initiative">
//     Copyright (c) APSIM initiative. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
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

        /// <summary>Link to APSIM's Clock (provides time information).</summary>
        [Link]
        private Clock myClock = null;

        /// <summary>Link to APSIM's WeatherFile (provides meteorological information).</summary>
        [Link]
        private IWeather myMetData = null;

        /// <summary>Link to APSIM summary (logs the messages raised during model run).</summary>
        [Link]
        private ISummary mySummary = null;

        /// <summary>Link to the Soil (provodes soil information).</summary>
        [Link]
        private Soil mySoil = null;

        /// <summary>Link to apsim's Resource Arbitrator module.</summary>
        [Link(IsOptional = true)]
        private Arbitrator.Arbitrator apsimArbitrator = null;

        /// <summary>Link to apsim's Resource Arbitrator module.</summary>
        [Link(IsOptional = true)]
        private SoilArbitrator soilArbitrator = null;

        //- Events  ---------------------------------------------------------------------------------------------------

        /// <summary>Invoked for incorporating soil FOM.</summary>
        /// <param name="Data">The data about biomass deposited by this plant into the soil FOM</param>
        public delegate void FOMLayerDelegate(FOMLayerType Data);

        /// <summary>Occurs when plant is detaching senesced roots.</summary>
        public event FOMLayerDelegate IncorpFOM;

        /// <summary>Invoked for incorporating surface OM.</summary>
        /// <param name="Data">The data about biomass deposited by this plant onto the soil surface</param>
        public delegate void BiomassRemovedDelegate(BiomassRemovedType Data);

        /// <summary>Occurs when plant is detaching dead tissues, litter.</summary>
        public event BiomassRemovedDelegate BiomassRemoved;

        /// <summary>Invoked for changing soil water due to uptake.</summary>
        /// <param name="Data">The changes in the amount of water for each soil layer</param>
        public delegate void WaterChangedDelegate(WaterChangedType Data);

        /// <summary>Occurs when plant takes up water.</summary>
        public event WaterChangedDelegate WaterChanged;

        /// <summary>Invoked for changing soil nitrogen due to uptake.</summary>
        /// <param name="Data">The changes in the soil N for each soil layer</param>
        public delegate void NitrogenChangedDelegate(NitrogenChangedType Data);

        /// <summary>Occurs when the plant takes up soil N.</summary>
        public event NitrogenChangedDelegate NitrogenChanged;

        #endregion  --------------------------------------------------------------------------------------------------------  --------------------------------------------------------------------------------------------------------

        #region ICanopy implementation  ------------------------------------------------------------------------------------

        /// <summary>The canopy albedo for this plant (0-1).</summary>
        private double myAlbedo = 0.26;

        /// <summary>Gets or sets the canopy albedo for this plant (0-1).</summary>
        [Units("0-1")]
        public double Albedo
        {
            get { return myAlbedo; }
            set { myAlbedo = value; }
        }

        /// <summary>The maximum stomatal conductance (m/s).</summary>
        private double myGsmax = 0.011;

        /// <summary>Gets or sets the  maximum stomatal conductance (m/s).</summary>
        [Units("m/s")]
        public double Gsmax
        {
            get { return myGsmax; }
            set { myGsmax = value; }
        }

        /// <summary>The solar radiation at which stomatal conductance decreases to 50% (W/m^2).</summary>
        private double myR50 = 200;

        /// <summary>Gets or sets the R50 factor (W/m^2).</summary>
        [Units("W/m^2")]
        public double R50
        {
            get { return myR50; }
            set { myR50 = value; }
        }

        /// <summary>Gets the LAI of live tissues (m^2/m^2).</summary>
        [Description("Leaf area index of green tissues")]
        [Units("m^2/m^2")]
        public double LAI
        {
            get { return LAIGreen; }
        }

        /// <summary>Gets the total LAI, live + dead (m^2/m^2).</summary>
        [Description("Total leaf area index")]
        [Units("m^2/m^2")]
        public double LAITotal
        {
            get { return LAIGreen + LAIDead; }
        }

        /// <summary>Gets the plant's green cover (0-1).</summary>
        [Description("Fraction of soil covered by green tissues")]
        [Units("0-1")]
        public double CoverGreen
        {
            get { return CalcPlantCover(greenLAI); }
        }

        /// <summary>Gets the total plant cover (0-1).</summary>
        [Description("Fraction of soil covered by plant tissues")]
        [Units("0-1")]
        public double CoverTotal
        {
            get { return CalcPlantCover(greenLAI + deadLAI); }
        }

        /// <summary>Gets the canopy height (mm).</summary>
        [Description("The average canopy height")]
        [Units("mm")]
        public double Height
        {
            get { return HeightfromDM(); }
        }

        /// <summary>Gets the canopy depth (mm).</summary>
        [Description("The depth of the canopy")]
        [Units("mm")]
        public double Depth
        {
            get { return Height; }
        }

        // TODO: have to verify how this works (what exactly is needed by MicroClimate
        /// <summary>Plant growth limiting factor, supplied to MicroClimate for calculating potential transpiration.</summary>
        [Description("General growth limiting factor (for MicroClimate)")]
        [Units("0-1")]
        public double FRGR
        {
            get { return 1.0; }
        }

        /// <summary>Potential evapotranspiration, as calculated by MicroClimate (mm).</summary>
        [XmlIgnore]
        [Units("mm")]
        public double PotentialEP
        {
            get { return myWaterDemand; }
            set { myWaterDemand = value; }
        }

        /// <summary>Light profile, energy available for each canopy layer (W/m^2).</summary>
        private CanopyEnergyBalanceInterceptionlayerType[] myLightProfile;

        /// <summary>Gets or sets the light profile for this plant, as calculated by MicroClimate (W/m^2).</summary>
        /// <remarks>This is the intercepted radiation for each layer of the canopy.</remarks>
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

        #endregion  --------------------------------------------------------------------------------------------------------

        #region ICrop implementation  --------------------------------------------------------------------------------------

        /// <summary>Gets a list of cultivar names (not used by AgPasture).</summary>
        public string[] CultivarNames
        {
            get { return null; }
        }

        /// <summary>Sows the plant.</summary>
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

        /// <summary>Returns true if the crop is ready for harvesting.</summary>
        public bool IsReadyForHarvesting
        {
            get { return false; }
        }

        /// <summary>Harvests the crop.</summary>
        public void Harvest()
        {
            throw new NotImplementedException();
        }

        /// <summary>Ends the crop.</summary>
        /// <remarks>All plant material is moved on to surfaceOM and soilFOM.</remarks>
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

        #endregion  --------------------------------------------------------------------------------------------------------

        #region ICrop2 implementation  -------------------------------------------------------------------------------------

        /// <summary>Generic descriptor for this plant (used by Arbitrator).</summary>
        [Description("Generic type of crop")]
        [Units("")]
        public string CropType
        {
            get { return Name; }
        }

        /// <summary>Flag whether the plant in the ground.</summary>
        [XmlIgnore]
        [Units("true/false")]
        public bool PlantInGround
        {
            get { return isAlive; }
        }

        /// <summary>Flag whether the plant has emerged.</summary>
        [XmlIgnore]
        [Units("true/false")]
        public bool PlantEmerged
        {
            get { return phenologicStage > 0; }
        }

        /// <summary>The canopy data for this plant.</summary>
        private CanopyProperties myCanopyProperties = new CanopyProperties();

        /// <summary>Collection of crop canopy properties (used by Arbitrator).</summary>
        [Units("")]
        public CanopyProperties CanopyProperties
        {
            get { return myCanopyProperties; }
        }

        /// <summary>The root data for this plant.</summary>
        RootProperties myRootProperties = new RootProperties();

        /// <summary>Collection of crop root properties (used by Arbitrator).</summary>
        [Units("")]
        public RootProperties RootProperties
        {
            get { return myRootProperties; }
        }

        /// <summary> Water demand for this plant (mm).</summary>
        [XmlIgnore]
        [Units("mm")]
        public double demandWater
        {
            get { return myWaterDemand; }
            set { double dummy = value; }
        }

        /// <summary> The actual supply of water to the plant for each soil layer (mm).</summary>
        [XmlIgnore]
        [Units("mm")]
        public double[] uptakeWater
        {
            get { return null; }
            set { mySoilWaterUptake = value; }
        }

        /// <summary>Nitrogen demand for this plant (kg/ha).</summary>
        [XmlIgnore]
        [Units("kg/ha")]
        public double demandNitrogen
        {
            get
            {
                // Pack the soil information
                ZoneWaterAndN myZone = new ZoneWaterAndN();
                myZone.Name = this.Parent.Name;
                myZone.Water = mySoil.Water;
                myZone.NO3N = mySoil.NO3N;
                myZone.NH4N = mySoil.NH4N;

                // Get the N amount available in the soil
                EvaluateSoilNitrogenAvailable(myZone);

                // Get the N amount fixed through symbiosis
                EvaluateNitrogenFixation();

                // Evaluate the use of N remobilised and get N amount demanded from soil
                EvaluateSoilNitrogenDemand();

                return mySoilNDemand;
            }
            set { double dummy = value; }
        }

        /// <summary>The actual supply of nitrogen (ammonium and nitrate) to the plant for each soil layer (kg/ha).</summary>
        [XmlIgnore]
        [Units("kg/ha")]
        public double[] uptakeNitrogen { get; set; }

        /// <summary>The proportion of nitrogen uptake from each layer in the form of nitrate (0-1).</summary>
        [XmlIgnore]
        [Units("0-1")]
        public double[] uptakeNitrogenPropNO3 { get; set; }

        #endregion  --------------------------------------------------------------------------------------------------------

        #region IUptake implementation  ------------------------------------------------------------------------------------

        /// <summary>Gets the potential plant water uptake for each layer (mm).</summary>
        /// <remarks>The model can only handle one root zone at present.</remarks>
        /// <param name="soilstate">Soil state (current water content)</param>
        /// <returns>Potential water uptake (mm)</returns>
        public List<ZoneWaterAndN> GetSWUptakes(SoilState soilstate)
        {
            if (IsAlive)
            {
                // Get the zone this plant is in
                ZoneWaterAndN myZone = new ZoneWaterAndN();
                Zone parentZone = Apsim.Parent(this, typeof (Zone)) as Zone;
                foreach (ZoneWaterAndN zone in soilstate.Zones)
                    if (zone.Name == parentZone.Name)
                        myZone = zone;

                // Get the amount of water available for this plant
                EvaluateSoilWaterAvailable(myZone);

                // Get the amount of water potentially taken up by this plant
                EvaluateSoilWaterUptake();

                // Pack potential uptake data for this plant
                ZoneWaterAndN myUptakeDemand = new ZoneWaterAndN();
                myUptakeDemand.Name = myZone.Name;
                myUptakeDemand.Water = mySoilWaterUptake;
                myUptakeDemand.NO3N = new double[nLayers];
                myUptakeDemand.NH4N = new double[nLayers];

                List<ZoneWaterAndN> zones = new List<ZoneWaterAndN>();
                zones.Add(myUptakeDemand);
                return zones;
            }
            else
                return null;
        }

        /// <summary>Gets the potential plant N uptake for each layer (mm).</summary>
        /// <remarks>The model can only handle one root zone at present.</remarks>
        /// <param name="soilstate">Soil state (current N contents)</param>
        /// <returns>Potential N uptake (kg/ha)</returns>
        public List<ZoneWaterAndN> GetNUptakes(SoilState soilstate)
        {
            if (IsAlive)
            {
                // Get the zone this plant is in
                ZoneWaterAndN myZone = new ZoneWaterAndN();
                Zone parentZone = Apsim.Parent(this, typeof (Zone)) as Zone;
                foreach (ZoneWaterAndN Z in soilstate.Zones)
                    if (Z.Name == parentZone.Name)
                        myZone = Z;

                // Get the N amount available in the soil
                EvaluateSoilNitrogenAvailable(myZone);

                // Get the N amount fixed through symbiosis
                EvaluateNitrogenFixation();

                // Evaluate the use of N remobilised and get N amount demanded from soil
                EvaluateSoilNitrogenDemand();

                // Get N amount take up from the soil
                EvaluateSoilNitrogenUptake();

                //Pack results into uptake structure
                ZoneWaterAndN myUptakeDemand = new ZoneWaterAndN();
                myUptakeDemand.Name = myZone.Name;
                myUptakeDemand.NH4N = mySoilNH4Uptake;
                myUptakeDemand.NO3N = mySoilNO3Uptake;
                myUptakeDemand.Water = new double[nLayers];

                List<ZoneWaterAndN> zones = new List<ZoneWaterAndN>();
                zones.Add(myUptakeDemand);
                return zones;
            }
            else
                return null;
        }

        /// <summary>Sets the amount of water taken up by this plant (mm).</summary>
        /// <remarks>The model can only handle one root zone at present.</remarks>
        /// <param name="zones">Water uptake from each layer (mm), by zone</param>
        public void SetSWUptake(List<ZoneWaterAndN> zones)
        {
            // Get the zone this plant is in
            ZoneWaterAndN MyZone = new ZoneWaterAndN();
            Zone parentZone = Apsim.Parent(this, typeof (Zone)) as Zone;
            foreach (ZoneWaterAndN Z in zones)
                if (Z.Name == parentZone.Name)
                    MyZone = Z;

            // Get the water uptake from each layer
            for (int layer = 0; layer < nLayers; layer++)
                mySoilWaterUptake[layer] = MyZone.Water[layer];
        }

        /// <summary>Sets the amount of N taken up by this plant (kg/ha).</summary>
        /// <remarks>The model can only handle one root zone at present.</remarks>
        /// <param name="zones">N uptake from each layer (kg/ha), by zone</param>
        public void SetNUptake(List<ZoneWaterAndN> zones)
        {
            // Get the zone this plant is in
            ZoneWaterAndN MyZone = new ZoneWaterAndN();
            Zone parentZone = Apsim.Parent(this, typeof (Zone)) as Zone;
            foreach (ZoneWaterAndN Z in zones)
                if (Z.Name == parentZone.Name)
                    MyZone = Z;

            // Get the N uptake from each layer
            for (int layer = 0; layer < nLayers; layer++)
            {
                mySoilNH4Uptake[layer] = MyZone.NH4N[layer];
                mySoilNO3Uptake[layer] = MyZone.NO3N[layer];
            }
        }

        #endregion  --------------------------------------------------------------------------------------------------------

        #region Model parameters  ------------------------------------------------------------------------------------------

        // NOTE: default parameters describe a generic perennial ryegrass species

        /// <summary>Family type for this plant species (grass/legume/brassica).</summary>
        private PlantFamilyType mySpeciesFamily = PlantFamilyType.Grass;

        /// <summary>Gets or sets the species family type.</summary>
        [Description("Family type for this plant species [grass/legume/brassica]:")]
        [Units("")]
        public PlantFamilyType SpeciesFamily
        {
            get { return mySpeciesFamily; }
            set
            {
                mySpeciesFamily = value;
                isLegume = mySpeciesFamily == PlantFamilyType.Legume;
            }
        }

        /// <summary>Species photosynthetic pathway (C3/C4).</summary>
        private PhotosynthesisPathwayType myPhotosynthesisPathway = PhotosynthesisPathwayType.C3;

        /// <summary>Gets or sets the species photosynthetic pathway (C3/C4).</summary>
        [Description("Metabolic pathway for C fixation during photosynthesis [C3/C4]:")]
        [Units("")]
        public PhotosynthesisPathwayType PhotosynthesisPathway
        {
            get { return myPhotosynthesisPathway; }
            set { myPhotosynthesisPathway = value; }
        }

        // - Initial DM values  ---------------------------------------------------------------------------------------

        /// <summary>Initial above ground DM weight (kg/ha).</summary>
        private double myInitialShootDM = 2000.0;

        /// <summary>Gets or sets the initial above ground DM weight (kg/ha).</summary>
        [Description("Initial above ground DM (leaf, stem, and stolon) [kg/ha]:")]
        [Units("kg/ha")]
        public double InitialShootDM
        {
            get { return myInitialShootDM; }
            set { myInitialShootDM = value; }
        }

        /// <summary>Initial root DM weight (kg/ha).</summary>
        private double myInitialRootDM = 500.0;

        /// <summary>Gets or sets the initial root DM weight (kg/ha).</summary>
        [Description("Initial below ground DM (roots) [kg/ha]:")]
        [Units("kg/ha")]
        public double InitialRootDM
        {
            get { return myInitialRootDM; }
            set { myInitialRootDM = value; }
        }

        /// <summary>Initial rooting depth (mm).</summary>
        private double myInitialRootDepth = 750.0;

        /// <summary>Gets or sets the initial rooting depth (mm).</summary>
        [Description("Initial depth for roots [mm]:")]
        [Units("mm")]
        public double InitialRootDepth
        {
            get { return myInitialRootDepth; }
            set { myInitialRootDepth = value; }
        }

        /// <summary>Initial fractions of DM for each plant part in grass (0-1).</summary>
        private double[] initialDMFractions_grass = {0.15, 0.25, 0.25, 0.05, 0.05, 0.10, 0.10, 0.05, 0.00, 0.00, 0.00};

        /// <summary>Initial fractions of DM for each plant part in legume (0-1).</summary>
        private double[] initialDMFractions_legume = {0.20, 0.25, 0.25, 0.00, 0.02, 0.04, 0.04, 0.00, 0.06, 0.12, 0.12};

        /// <summary>Initial fractions of DM for each plant part in forbs (0-1).</summary>
        private double[] initialDMFractions_forbs = {0.20, 0.20, 0.15, 0.05, 0.15, 0.15, 0.10, 0.00, 0.00, 0.00, 0.00};

        // - Photosysnthesis and respiration  -------------------------------------------------------------------------


        /// <summary>Reference CO2 assimilation rate for photosynthesis (mg CO2/m^2 leaf/s).</summary>
        private double myReferencePhotosynthesisRate = 1.0;

        /// <summary>Gets or sets the reference CO2 assimilation rate for photosynthesis (mg CO2/m^2 leaf/s).</summary>
        [Description("Reference CO2 assimilation rate during photosynthesis [mg CO2/m^2/s]:")]
        [Units("mg/m^2/s")]
        public double ReferencePhotosynthesisRate
        {
            get { return myReferencePhotosynthesisRate; }
            set { myReferencePhotosynthesisRate = value; }
        }

        /// <summary>Gets or sets the leaf photosynthetic efficiency (mg CO2/J).</summary>
        [XmlIgnore]
        [Units("mg CO2/J")]
        public double PhotosyntheticEfficiency = 0.01;

        /// <summary>Gets or sets the photosynthesis curvature parameter (J/kg/s).</summary>
        [XmlIgnore]
        [Units("J/kg/s")]
        public double PhotosynthesisCurveFactor = 0.8;

        /// <summary>Fraction of total radiation that is photosynthetically active [0-1]</summary>
        [XmlIgnore]
        [Units("0-1")]
        public double fractionPAR = 0.5;

        /// <summary>Light extinction coefficient (0-1).</summary>
        private double myLightExtentionCoefficient = 0.5;

        /// <summary>Gets or sets the light extinction coefficient (0-1).</summary>
        [Description("Light extinction coefficient [0-1]:")]
        [Units("0-1")]
        public double LightExtentionCoefficient
        {
            get { return myLightExtentionCoefficient; }
            set { myLightExtentionCoefficient = value; }
        }

        /// <summary>Minimum temperature for growth (oC).</summary>
        private double myGrowthTminimum = 2.0;

        /// <summary>Gets or sets the minimum temperature for growth (oC).</summary>
        [Description("Minimum temperature for growth [oC]:")]
        [Units("oC")]
        public double GrowthTminimum
        {
            get { return myGrowthTminimum; }
            set { myGrowthTminimum = value; }
        }

        /// <summary>Optimum temperature for growth (oC).</summary>
        private double myGrowthToptimum = 20.0;

        /// <summary>Gets or sets the optimum temperature for growth (oC).</summary>
        [Description("Optimum temperature for growth [oC]:")]
        [Units("oC")]
        public double GrowthToptimum
        {
            get { return myGrowthToptimum; }
            set { myGrowthToptimum = value; }
        }

        /// <summary>Curve parameter for growth response to temperature (>0.0).</summary>
        private double myGrowthTq = 1.75;

        /// <summary>Gets or sets the curve parameter for growth response to temperature (>0.0).</summary>
        [Description("Curve parameter for growth response to temperature:")]
        [Units("-")]
        public double GrowthTq
        {
            get { return myGrowthTq; }
            set { myGrowthTq = value; }
        }

        /// <summary>Gets or sets the reference CO2 concentration for photosynthesis (ppm).</summary>
        [XmlIgnore]
        [Units("ppm")]
        public double ReferenceCO2 = 380.0;

        /// <summary>Gets or sets the coefficient controlling the CO2 effect on photosynthesis (ppm).</summary>
        [XmlIgnore]
        [Units("ppm")]
        public double CO2EffectScaleFactor = 700.0;

        /// <summary>Gets or sets the scalling paramenter for the CO2 effects on N uptake (ppm).</summary>
        [XmlIgnore]
        [Units("ppm")]
        public double CO2EffectOffsetFactor = 600.0;

        /// <summary>Gets or sets the minimum value of the CO2 effect on N requirements (0-1).</summary>
        [XmlIgnore]
        [Units("0-1")]
        public double CO2EffectMinimum = 0.7;

        /// <summary>Gets or sets the exponent controlling the CO2 effect on N requirements (>0.0).</summary>
        [XmlIgnore]
        [Units("-")]
        public double CO2EffectExponent = 2.0;

        /// <summary>Flag whether photosynthesis reduction due to heat damage is enabled (yes/no).</summary>
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

        /// <summary>Onset temperature for heat effects on photosynthesis (oC).</summary>
        private double myHeatOnsetTemperature = 28.0;

        /// <summary>Gets or sets the onset temperature for heat effects on photosynthesis (oC).</summary>
        [Description("Onset temperature for heat effects on photosynthesis [oC]:")]
        [Units("oC")]
        public double HeatOnsetTemperature
        {
            get { return myHeatOnsetTemperature; }
            set { myHeatOnsetTemperature = value; }
        }

        /// <summary>Temperature for full heat effect on photosynthesis, growth stops (oC).</summary>
        private double myHeatFullTemperature = 35.0;

        /// <summary>Gets or sets the temperature for full heat effect on photosynthesis, growth stops (oC).</summary>
        [Description("Temperature for full heat effect on photosynthesis [oC]:")]
        [Units("oC")]
        public double HeatFullTemperature
        {
            get { return myHeatFullTemperature; }
            set { myHeatFullTemperature = value; }
        }

        /// <summary>Cumulative degrees-day for recovery from heat stress (oCd).</summary>
        private double myHeatRecoverySumDD = 30.0;

        /// <summary>Gets or sets the cumulative degrees-day for recovery from heat stress (oCd).</summary>
        [Description("Cumulative degrees-day for recovery from heat stress [oCd]:")]
        [Units("oCd")]
        public double HeatRecoverySumDD
        {
            get { return myHeatRecoverySumDD; }
            set { myHeatRecoverySumDD = value; }
        }

        /// <summary>Reference temperature for recovery from heat stress (oC).</summary>
        private double myHeatRecoveryTreference = 25.0;

        /// <summary>Gets or sets the reference temperature for recovery from heat stress (oC).</summary>
        [Description("Reference temperature for recovery from heat stress [oC]:")]
        [Units("oC")]
        public double HeatRecoveryTreference
        {
            get { return myHeatRecoveryTreference; }
            set { myHeatRecoveryTreference = value; }
        }

        /// <summary>Flag whether photosynthesis reduction due to cold damage is enabled (yes/no).</summary>
        [Description("Enable photosynthesis reduction due to cold damage [yes/no]:")]
        [Units("yes/no")]
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

        /// <summary>Onset temperature for cold effects on photosynthesis (oC).</summary>
        private double myColdOnsetTemperature = 0.0;

        /// <summary>Gets or sets the onset temperature for cold effects on photosynthesis (oC).</summary>
        [Description("Onset temperature for cold effects on photosynthesis [oC]:")]
        [Units("oC")]
        public double ColdOnsetTemperature
        {
            get { return myColdOnsetTemperature; }
            set { myColdOnsetTemperature = value; }
        }

        /// <summary>Temperature for full cold effect on photosynthesis, growth stops (oC).</summary>
        private double myColdFullTemperature = -3.0;

        /// <summary>Gets or sets the temperature for full cold effect on photosynthesis, growth stops (oC).</summary>
        [Description("Temperature for full cold effect on photosynthesis [oC]:")]
        [Units("oC")]
        public double mColdFullTemperature
        {
            get { return myColdFullTemperature; }
            set { myColdFullTemperature = value; }
        }

        /// <summary>Cumulative degrees for recovery from cold stress (oCd).</summary>
        private double myColdRecoverySumDD = 20.0;

        /// <summary>Gets or sets the cumulative degrees for recovery from cold stress (oCd).</summary>
        [Description("Cumulative degrees for recovery from cold stress [oCd]:")]
        [Units("oCd")]
        public double ColdRecoverySumDD
        {
            get { return myColdRecoverySumDD; }
            set { myColdRecoverySumDD = value; }
        }

        /// <summary>Reference temperature for recovery from cold stress (oC).</summary>
        private double myColdRecoveryTreference = 0.0;

        /// <summary>Gets or sets the reference temperature for recovery from cold stress (oC).</summary>
        [Description("Reference temperature for recovery from cold stress [oC]:")]
        [Units("oC")]
        public double ColdRecoveryTreference
        {
            get { return myColdRecoveryTreference; }
            set { myColdRecoveryTreference = value; }
        }

        /// <summary>Maintenance respiration coefficient (0-1).</summary>
        private double myMaintenanceRespirationCoefficient = 0.03;

        /// <summary>Gets or sets the maintenance respiration coefficient (0-1).</summary>
        [Description("Maintenance respiration coefficient [0-1]:")]
        [Units("0-1")]
        public double MaintenanceRespirationCoefficient
        {
            get { return myMaintenanceRespirationCoefficient; }
            set { myMaintenanceRespirationCoefficient = value; }
        }

        /// <summary>Growth respiration coefficient (0-1).</summary>
        private double myGrowthRespirationCoefficient = 0.20;

        /// <summary>Gets or sets the growth respiration coefficient (0-1).</summary>
        [Description("Growth respiration coefficient [0-1]:")]
        [Units("0-1")]
        public double GrowthRespirationCoefficient
        {
            get { return myGrowthRespirationCoefficient; }
            set { myGrowthRespirationCoefficient = value; }
        }

        /// <summary>Reference temperature for maintenance respiration (oC).</summary>
        private double myRespirationTreference = 20.0;

        /// <summary>Gets or sets the reference temperature for maintenance respiration (oC).</summary>
        [Description("Reference temperature for maintenance respiration [oC]:")]
        [Units("oC")]
        public double RespirationTreference
        {
            get { return myRespirationTreference; }
            set { myRespirationTreference = value; }
        }

        /// <summary>Exponent controlling the effect of temperature on respiration (>1.0).</summary>
        private double myRespirationExponent = 1.5;

        /// <summary>Gets or sets the exponent controlling the effect of temperature on respiration (>1.0).</summary>
        [Description("Exponent controlling the effect of temperature on respiration [>1]:")]
        [Units("-")]
        public double RespirationExponent
        {
            get { return myRespirationExponent; }
            set { myRespirationExponent = value; }
        }

        // - N concentration  -----------------------------------------------------------------------------------------

        /// <summary>N concentration thresholds for leaves, optimum, minimum and maximum (kg/kg).</summary>
        private double[] myNThresholdsForLeaves = {0.04, 0.05, 0.012};

        /// <summary>Gets or sets the N concentration thresholds for leaves, optimum, minimum and maximum (kg/kg).</summary>
        [Description("N concentration thresholds for leaves (optimum, minimum and maximum) [kg/kg]:")]
        [Units("kg/kg")]
        public double[] NThresholdsForLeaves
        {
            get { return myNThresholdsForLeaves; }
            set { myNThresholdsForLeaves = value; }
        }

        /// <summary>N concentration thresholds for stems, optimum, minimum and maximum (kg/kg).</summary>
        private double[] myNThresholdsForStems = {0.02, 0.025, 0.006};

        /// <summary>Gets or sets the N concentration thresholds for stems, optimum, minimum and maximum (kg/kg).</summary>
        [Description("N concentration thresholds for stems (optimum, minimum and maximum) [kg/kg:]")]
        [Units("kg/kg")]
        public double[] NThresholdsForStems
        {
            get { return myNThresholdsForStems; }
            set { myNThresholdsForStems = value; }
        }

        /// <summary>N concentration thresholds for stolons, optimum, minimum and maximum (kg/kg).</summary>
        private double[] myNThresholdsForStolons = {0.0, 0.0, 0.0};

        /// <summary>Gets or sets the N concentration thresholds for stolons, optimum, minimum and maximum (kg/kg).</summary>
        [Description("N concentration thresholds for stolons (optimum, minimum and maximum) [kg/kg:]")]
        [Units("kg/kg")]
        public double[] NThresholdsForStolons
        {
            get { return myNThresholdsForStolons; }
            set { myNThresholdsForStolons = value; }
        }

        /// <summary>N concentration thresholds for roots, optimum, minimum and maximum (kg/kg).</summary>
        private double[] myNThresholdsForRoots = {0.02, 0.025, 0.006};

        /// <summary>Gets or sets the N concentration thresholds for roots, optimum, minimum and maximum (kg/kg).</summary>
        [Description("N concentration thresholds for roots (optimum, minimum and maximum) [kg/kg:]")]
        [Units("kg/kg")]
        public double[] NThresholdsForRoots
        {
            get { return myNThresholdsForRoots; }
            set { myNThresholdsForRoots = value; }
        }

        // - Germination and emergence  -------------------------------------------------------------------------------

        /// <summary>Gets or sets the cumulative degrees-day needed for seed germination (oCd).</summary>
        [XmlIgnore]
        [Units("oCd")]
        public double DegreesDayForGermination = 100.0;

        /// <summary>The fractions of DM for each plant part at emergence, for all plants (0-1).</summary>
        private double[] EmergenceDMFractions = { 0.60, 0.25, 0.00, 0.00, 0.15, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00 };

        // - DM allocation  -------------------------------------------------------------------------------------------

        /// <summary>Target, or ideal, shoot-root ratio (>0.0).</summary>
        private double myTargetSRratio = 3.0;

        /// <summary>Gets or sets the target, or ideal, shoot-root ratio (>0.0).</summary>
        [Description("Target, or ideal, shoot-root ratio (for DM allocation) [>0.0]:")]
        [Units("")]
        public double TargetSRratio
        {
            get { return myTargetSRratio; }
            set { myTargetSRratio = value; }
        }

        /// <summary>Maximum fraction of DM growth allocated to roots (0-1).</summary>
        private double myMaxRootAllocation = 0.25;

        /// <summary>Gets or sets the maximum fraction of DM growth allocated to roots (0-1).</summary>
        [Description("Maximum fraction of DM growth allocated to roots [0-1]:")]
        [Units("0-1")]
        public double MaxRootAllocation
        {
            get { return myMaxRootAllocation; }
            set { myMaxRootAllocation = value; }
        }

        /// <summary>Maximum effect that soil GLFs have on Shoot-Root ratio (0-1).</summary>
        private double myShootRootGlfFactor = 0.50;

        /// <summary>Gets or sets the maximum effect that soil GLFs have on Shoot-Root ratio (0-1).</summary>
        [Description("Maximum effect that soil GLFs have on Shoot-Root ratio [0-1]:")]
        [Units("0-1")]
        public double ShootRootGlfFactor
        {
            get { return myShootRootGlfFactor; }
            set { myShootRootGlfFactor = value; }
        }

        /// <summary>
        /// Flag whether Shoot:Root ratio should be adjusted to mimic DM allocation during reproductive season (perennial species).
        /// </summary>
        [Description("Adjust Shoot-Root ratio to mimic DM allocation during reproductive season?")]
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

        /// <summary>Reference latitude determining timing for reproductive season (degress).</summary>
        private double myReproSeasonReferenceLatitude = 41.0;

        /// <summary>Gets or sets the reference latitude determining timing for reproductive season (degress).</summary>
        [Description("Reference latitude determining timing for reproductive season [degress]:")]
        [Units("degrees")]
        public double ReproSeasonReferenceLatitude
        {
            get { return myReproSeasonReferenceLatitude; }
            set { myReproSeasonReferenceLatitude = value; }
        }

        /// <summary>Coefficient controlling the time to start the reproductive season as function of latitude (-).</summary>
        private double myReproSeasonTimingCoeff = 0.14;

        /// <summary>Gets or sets the coefficient controlling the time to start the reproductive season as function of latitude (-).</summary>
        [Description("Coefficient controlling the time to start the reproductive season as function of latitude [-]:")]
        [Units("-")]
        public double ReproSeasonTimingCoeff
        {
            get { return myReproSeasonTimingCoeff; }
            set { myReproSeasonTimingCoeff = value; }
        }

        /// <summary>Gets or sets the coefficient controlling the duration of the reproductive season as function of latitude (-).</summary>
        [XmlIgnore]
        [Units("-")]
        public double ReproSeasonDurationCoeff = 2.0;

        /// <summary>Gets or sets the ratio between the length of shoulders and the period with full reproductive growth effect (-).</summary>
        [XmlIgnore]
        [Units("-")]
        public double ReproSeasonShouldersLengthFactor = 1.0;

        /// <summary>Gets or sets the proportion of the onset phase of shoulder period with reproductive growth effect (0-1).</summary>
        [XmlIgnore]
        [Units("0-1")]
        public double ReproSeasonOnsetDurationFactor = 0.60;

        /// <summary>Maximum increase in Shoot-Root ratio during reproductive growth (0-1).</summary>
        private double myReproSeasonMaxAllocationIncrease = 0.50;

        /// <summary>Gets or sets the maximum increase in Shoot-Root ratio during reproductive growth (0-1).</summary>
        [Description("Maximum increase in Shoot-Root ratio during reproductive growth [0-1]:")]
        [Units("0-1")]
        public double ReproSeasonMaxAllocationIncrease
        {
            get { return myReproSeasonMaxAllocationIncrease; }
            set { myReproSeasonMaxAllocationIncrease = value; }
        }

        /// <summary>Coefficient controlling the increase in shoot allocation during reproductive growth as function of latitude (-).</summary>
        private double myReproSeasonAllocationCoeff = 0.10;

        /// <summary>
        /// Gets or sets the coefficient controlling the increase in shoot allocation during reproductive growth as function of latitude (-).
        /// </summary>
        [Description("Coefficient controlling the increase in shoot allocation during reproductive growth as function of latitude [-]:")]
        [Units("-")]
        public double ReproSeasonAllocationCoeff
        {
            get { return myReproSeasonAllocationCoeff; }
            set { myReproSeasonAllocationCoeff = value; }
        }

        /// <summary>Maximum target allocation of new growth to leaves (0-1).</summary>
        private double myFractionLeafMaximum = 0.7;

        /// <summary>Gets or sets the maximum target allocation of new growth to leaves (0-1).</summary>
        [Description("Maximum target allocation of new growth to leaves [0-1]")]
        [Units("0-1")]
        public double FractionLeafMaximum
        {
            get { return myFractionLeafMaximum; }
            set { myFractionLeafMaximum = value; }
        }

        /// <summary>Minimum target allocation of new growth to leaves (0-1).</summary>
        private double myFractionLeafMinimum = 0.7;

        /// <summary>Gets or sets the minimum target allocation of new growth to leaves (0-1).</summary>
        [Description("Minimum target allocation of new growth to leaves [0-1]")]
        [Units("0-1")]
        public double FractionLeafMinimum
        {
            get { return myFractionLeafMinimum; }
            set { myFractionLeafMinimum = value; }
        }

        /// <summary>Shoot DM at which allocation of new growth to leaves start to decrease (kg/ha).</summary>
        private double myFractionLeafDMThreshold = 500;

        /// <summary>Gets or sets the shoot DM at which allocation of new growth to leaves start to decrease (kg/ha).</summary>
        [Description("Shoot DM at which allocation of new growth to leaves start to decrease [kg/ha]")]
        [Units("kg/ha")]
        public double FractionLeafDMThreshold
        {
            get { return myFractionLeafDMThreshold; }
            set { myFractionLeafDMThreshold = value; }
        }

        /// <summary>Shoot DM when allocation to leaves is midway maximum and minimum (kg/ha).</summary>
        private double myFractionLeafDMFactor = 2000;

        /// <summary>Gets or sets the shoot DM when allocation to leaves is midway maximum and minimum (kg/ha).</summary>
        [Description("Shoot DM factor allocation to leaves is midway maximum and minimum [kg/ha]")]
        [Units("kg/ha")]
        public double FractionLeafDMFactor
        {
            get { return myFractionLeafDMFactor; }
            set { myFractionLeafDMFactor = value; }
        }

        /// <summary>Exponent controlling the DM allocation to leaves (>0.0).</summary>
        private double myFractionLeafExponent = 3.0;

        /// <summary>Gets or sets the exponent controlling the DM allocation to leaves (>0.0).</summary>
        [Description("Exponent of function describing DM allocation to leaves [>0.0]")]
        [Units(">0.0")]
        public double FractionLeafExponent
        {
            get { return myFractionLeafExponent; }
            set { myFractionLeafExponent = value; }
        }

        /// <summary>Fraction of new shoot growth allocated to stolons (0-1).</summary>
        private double myFractionToStolon = 0.0;

        /// <summary>Gets or sets the fraction of new shoot growth allocated to stolons (0-1).</summary>
        [Description("Fraction of new shoot growth allocated to stolons [0-1]:")]
        [Units("0-1")]
        public double FractionToStolon
        {
            get { return myFractionToStolon; }
            set { myFractionToStolon = value; }
        }

        /// <summary>Number of live leaves per tiller (-).</summary>
        private double myLiveLeavesPerTiller = 3.0;

        /// <summary>Gets or sets the number of live leaves per tiller (-).</summary>
        [Description("Number of live leaves per tiller [-]:")]
        [Units("-")]
        public double LiveLeavesPerTiller
        {
            get { return myLiveLeavesPerTiller; }
            set { myLiveLeavesPerTiller = value; }
        }

        /// <summary>Specific leaf area (m^2/kg DM).</summary>
        private double mySpecificLeafArea = 20.0;

        /// <summary>Gets or sets the specific leaf area (m^2/kg DM).</summary>
        [Description("Specific leaf area [m^2/kg DM]:")]
        [Units("m^2/kg")]
        public double SpecificLeafArea
        {
            get { return mySpecificLeafArea; }
            set { mySpecificLeafArea = value; }
        }

        /// <summary>Specific root length (m/g DM).</summary>
        private double mySpecificRootLength = 75.0;

        /// <summary>Gets or sets the specific root length (m/g DM).</summary>
        [Description("Specific root length [m/g DM]:")]
        [Units("m/g")]
        public double SpecificRootLength
        {
            get { return mySpecificRootLength; }
            set { mySpecificRootLength = value; }
        }

        /// <summary>Flag whether stem and stolons are considered for computing LAI green (yes/no).</summary>
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

        /// <summary>Gets or sets the fraction of stolon tissue used when computing green LAI (0-1).</summary>
        [XmlIgnore]
        [Units("0-1")]
        public double StolonEffectOnLAI = 0.3;

        /// <summary>Gets or sets the maximum aboveground biomass for using stems when computing LAI (kg/ha).</summary>
        [XmlIgnore]
        [Units("kg/ha")]
        public double ShootMaxEffectOnLAI = 1000;

        /// <summary>Gets or sets the maximum effect of stems when computing green LAI (0-1).</summary>
        [XmlIgnore]
        [Units("0-1")]
        public double MaxStemEffectOnLAI = 0.316227766;

        // Turnover and senescence  -----------------------------------------------------------------------------------

        /// <summary>Daily DM turnover rate for shoot tissues (0-1).</summary>
        private double myTissueTurnoverRateShoot = 0.025;

        /// <summary>Gets or sets the daily DM turnover rate for shoot tissues (0-1).</summary>
        /// <remarks>This is closely related to the leaf apearence rate.</remarks>
        [Description("Daily DM turnover rate for shoot tissues [0-1]:")]
        [Units("0-1")]
        public double TissueTurnoverRateShoot
        {
            get { return myTissueTurnoverRateShoot; }
            set { myTissueTurnoverRateShoot = value; }
        }

        /// <summary>Daily DM turnover rate for root tissue (0-1).</summary>
        private double myTissueTurnoverRateRoot = 0.02;

        /// <summary>Gets or sets the daily DM turnover rate for root tissue (0-1).</summary>
        [Description("Daily DM turnover rate for root tissue [0-1]")]
        [Units("0-1")]
        public double TissueTurnoverRateRoot
        {
            get { return myTissueTurnoverRateRoot; }
            set { myTissueTurnoverRateRoot = value; }
        }

        /// <summary>Gets or sets the relative turnover rate for growing tissues (>0.0).</summary>
        [XmlIgnore]
        [Units("-")]
        public double RelativeTurnoverGrowing = 2.0;

        /// <summary>Daily detachment rate for dead tissues (0-1).</summary>
        private double myDetachmentRate = 0.11;

        /// <summary>Gets or sets the daily detachment rate for dead tissues (0-1).</summary>
        [Description("Daily detachment rate for DM dead [0-1]:")]
        [Units("0-1")]
        public double DetachmentRate
        {
            get { return myDetachmentRate; }
            set { myDetachmentRate = value; }
        }

        /// <summary>Minimum temperature for tissue turnover (oC).</summary>
        private double myTissueTurnoverTmin = 2.0;

        /// <summary>Gets or sets the minimum temperature for tissue turnover (oC).</summary>
        [Description("Minimum temperature for tissue turnover [oC]:")]
        [Units("oC")]
        public double TissueTurnoverTmin
        {
            get { return myTissueTurnoverTmin; }
            set { myTissueTurnoverTmin = value; }
        }

        /// <summary>Reference temperature for tissue turnover (oC).</summary>
        private double myTissueTurnoverTref = 20.0;

        /// <summary>Gets or sets the reference temperature for tissue turnover (oC).</summary>
        [Description("Reference temperature for tissue turnover [oC]:")]
        [Units("oC")]
        public double TissueTurnoverTref
        {
            get { return myTissueTurnoverTref; }
            set { myTissueTurnoverTref = value; }
        }

        /// <summary>Exponent of function for temperature effect on tissue turnover (-).</summary>
        private double myTissueTurnoverTq = 1.0;

        /// <summary>Gets or sets the exponent of function for temperature effect on tissue turnover (-).</summary>
        [Description("Exponent of function for temperature effect on tissue turnover:")]
        [Units("-")]
        public double TissueTurnoverTq
        {
            get { return myTissueTurnoverTq; }
            set { myTissueTurnoverTq = value; }
        }

        /// <summary>Maximum increase in tissue turnover due to water stress (>0.0).</summary>
        private double myTissueTurnoverDroughtMax = 1.0;

        /// <summary>Gets or sets the maximum increase in tissue turnover due to water stress (>0.0).</summary>
        [Description("Maximum increase in tissue turnover due to water deficit:")]
        [Units("-")]
        public double TissueTurnoverDroughtMax
        {
            get { return myTissueTurnoverDroughtMax; }
            set { myTissueTurnoverDroughtMax = value; }
        }

        /// <summary>Minimum GLFwater without effect on tissue turnover (0-1).</summary>
        private double myTissueTurnoverDroughtThreshold = 0.5;

        /// <summary>Gets or sets the minimum GLFwater without effect on tissue turnover (0-1).</summary>
        [Description("Minimum GLFwater without effect on tissue turnover [0-1]:")]
        [Units("0-1")]
        public double TissueTurnoverDroughtThreshold
        {
            get { return myTissueTurnoverDroughtThreshold; }
            set { myTissueTurnoverDroughtThreshold = value; }
        }

        /// <summary>Gets or sets the stock factor increasing tissue turnover rate (>0.0).</summary>
        [XmlIgnore]
        [Units("-")]
        public double TissueTurnoverStockFactor = 0.0;

        /// <summary>Fraction of luxury N remobilisable each day for each tissue age, growing, developed, mature (0-1).</summary>
        private double[] myFractionNLuxuryRemobilisable = {0.0, 0.0, 0.0};

        /// <summary>Gets or sets the fraction of luxury N remobilisable each day for each tissue age, growing, developed, mature (0-1).</summary>
        [Description("Fraction of luxury N remobilisable each day for each tissue age (growing, developed, mature) [0-1]:")]
        [Units("0-1")]
        public double[] FractionNLuxuryRemobilisable
        {
            get { return myFractionNLuxuryRemobilisable; }
            set { myFractionNLuxuryRemobilisable = value; }
        }

        // - N fixation  ----------------------------------------------------------------------------------------------

        /// <summary>Minimum fraction of N demand supplied by biologic N fixation (0-1).</summary>
        private double myMinimumNFixation = 0.0;

        /// <summary>Gets or sets the minimum fraction of N demand supplied by biologic N fixation (0-1).</summary>
        [Description("Minimum fraction of N demand supplied by biologic N fixation [0-1]:")]
        [Units("0-1")]
        public double MinimumNFixation
        {
            get { return myMinimumNFixation; }
            set { myMinimumNFixation = value; }
        }

        /// <summary>Maximum fraction of N demand supplied by biologic N fixation (0-1).</summary>
        private double myMaximumNFixation = 0.0;

        /// <summary>Gets or sets the maximum fraction of N demand supplied by biologic N fixation (0-1).</summary>
        [Description("Maximum fraction of N demand supplied by biologic N fixation [0-1]:")]
        [Units("0-1")]
        public double MaximumNFixation
        {
            get { return myMaximumNFixation; }
            set { myMaximumNFixation = value; }
        }

        // - Growth limiting factors  ---------------------------------------------------------------------------------

        /// <summary>Exponent for modifying the effect of N deficiency on plant growth (>0.0).</summary>
        private double myNDillutionCoefficient = 0.5;

        /// <summary>Gets or sets the exponent for modifying the effect of N deficiency on plant growth (>0.0).</summary>
        [Description("Exponent for modifying the effect of N deficiency on plant growth:")]
        [Units("-")]
        public double NDillutionCoefficient
        {
            get { return myNDillutionCoefficient; }
            set { myNDillutionCoefficient = value; }
        }

        /// <summary>Maximum reduction in plant growth due to water logging, saturated soil (0-1).</summary>
        private double mySoilWaterSaturationFactor = 0.1;

        /// <summary>Gets or sets the maximum reduction in plant growth due to water logging, saturated soil (0-1).</summary>
        [Description("Maximum reduction in plant growth due to water logging (saturated soil) [0-1]:")]
        [Units("0-1")]
        public double SoilWaterSaturationFactor
        {
            get { return mySoilWaterSaturationFactor; }
            set { mySoilWaterSaturationFactor = value; }
        }

        /// <summary>Minimum water-free pore space for growth with no limitations (0-1).</summary>
        private double myMinimumWaterFreePorosity = 0.1;

        /// <summary>Gets or sets the minimum water-free pore space for growth with no limitations (0-1).</summary>
        [Description("Minimum water-free pore space for growth with no limitations [0-1]:")]
        [Units("0-1")]
        public double MinimumWaterFreePorosity
        {
            get { return myMinimumWaterFreePorosity; }
            set { myMinimumWaterFreePorosity = value; }
        }

        /// <summary>Flag whether water logging effect is considered as a cumulative effect, instead of only daily effect (yes/no).</summary>
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

        /// <summary>Gets or sets the daily recovery rate from water logging (0-1).</summary>
        [XmlIgnore]
        public double SoilWaterSaturationRecoveryFactor = 1.0;

        /// <summary>Generic growth limiting factor, represents an arbitrary limitation to potential growth (0-1).</summary>
        private double myGlfGeneric = 1.0;

        /// <summary>Gets or sets a generic growth limiting factor, represents an arbitrary limitation to potential growth (0-1).</summary>
        /// <remarks> This factor can be used to describe the effects of drivers such as disease, etc.</remarks>
        [Description("Generic factor affecting potential plant growth [0-1]:")]
        [Units("0-1")]
        public double GlfGeneric
        {
            get { return myGlfGeneric; }
            set { myGlfGeneric = value; }
        }

        /// <summary>Generic growth limiting factor, represents an arbitrary soil limitation (0-1).</summary>
        private double myGlfSFertility = 1.0;

        /// <summary>Gets or sets a generic growth limiting factor, represents an arbitrary soil limitation (0-1).</summary>
        /// <remarks> This factor can be used to describe the effect of limitation in nutrients other than N.</remarks>
        [Description("Generic growth limiting factor due to soil fertility [0-1]:")]
        [Units("0-1")]
        public double GlfSFertility
        {
            get { return myGlfSFertility; }
            set { myGlfSFertility = value; }
        }

        // - Plant height  --------------------------------------------------------------------------------------------

        /// <summary>Minimum shoot height (mm).</summary>
        private double myMinimumPlantHeight = 25.0;

        /// <summary>Gets or sets the minimum shoot height (mm).</summary>
        [Description("Minimum shoot height [mm]:")]
        [Units("mm")]
        public double MinimumPlantHeight
        {
            get { return myMinimumPlantHeight; }
            set { myMinimumPlantHeight = value; }
        }

        /// <summary>Maximum shoot height (mm).</summary>
        private double myMaximumPlantHeight = 600.0;

        /// <summary>Gets or sets the maximum shoot height (mm).</summary>
        [Description("Maximum shoot height [mm]:")]
        [Units("mm")]
        public double MaximumPlantHeight
        {
            get { return myMaximumPlantHeight; }
            set { myMaximumPlantHeight = value; }
        }

        /// <summary>Exponent of shoot height funtion (>1.0).</summary>
        private double myExponentHeightFromMass = 2.8;

        /// <summary>Gets or sets the exponent of shoot height funtion (>1.0).</summary>
        [Description("Exponent of shoot height funtion [>1.0]:")]
        [Units(">1.0")]
        public double ExponentHeightFromMass
        {
            get { return myExponentHeightFromMass; }
            set { myExponentHeightFromMass = value; }
        }

        /// <summary>DM weight for maximum shoot height (kg/ha).</summary>
        private double myMassForMaximumHeight = 10000;

        /// <summary>Gets or sets the DM weight for maximum shoot height (kg/ha).</summary>
        [Description("DM weight for maximum shoot height[kg/ha]:")]
        [Units("kg/ha")]
        public double MassForMaximumHeight
        {
            get { return myMassForMaximumHeight; }
            set { myMassForMaximumHeight = value; }
        }

        // - Root distribution and height  ----------------------------------------------------------------------------

        /// <summary>Minimum rooting depth, at emergence (mm).</summary>
        private double myMinimumRootDepth = 50.0;

        /// <summary>Gets or sets the minimum rooting depth, at emergence (mm).</summary>
        [Description("Minimum rooting depth, at emergence [mm]:")]
        [Units("mm")]
        public double MinimumRootDepth
        {
            get { return myMinimumRootDepth; }
            set { myMinimumRootDepth = value; }
        }

        /// <summary>Maximum rooting depth (mm).</summary>
        private double myMaximumRootDepth = 750.0;

        /// <summary>Gets or sets the maximum rooting depth (mm).</summary>
        [Description("Maximum rooting depth [mm]:")]
        [Units("mm")]
        public double MaximumRootDepth
        {
            get { return myMaximumRootDepth; }
            set { myMaximumRootDepth = value; }
        }

        /// <summary>Daily root elongation rate at optimum temperature (mm/day).</summary>
        private double myRootElongationRate = 10.0;

        /// <summary>Gets or sets the daily root elongation rate at optimum temperature (mm/day).</summary>
        [Description("Daily root elongation rate at optimum temperature [mm/day]:")]
        [Units("mm/day")]
        public double RootElongationRate
        {
            get { return myRootElongationRate; }
            set { myRootElongationRate = value; }
        }

        /// <summary>Depth coefficient for root distribution, proportion decreases below this value (mm).</summary>
        private double myDepthForConstantRootProportion = 90.0;

        /// <summary>Gets or sets the depth coefficient for root distribution, proportion decreases below this value (mm).</summary>
        [Description("Depth for constant distribution of roots [mm]:")]
        [Units("mm")]
        public double DepthForConstantRootProportion
        {
            get { return myDepthForConstantRootProportion; }
            set { myDepthForConstantRootProportion = value; }
        }

        /// <summary>Exponent for the root distribution, controls the reducion as function of depth (>0.0).</summary>
        private double myExponentRootDistribution = 3.2;

        /// <summary>Gets or sets the exponent for the root distribution, controls the reducion as function of depth (>0.0).</summary>
        [Description("Coefficient for the root distribution [>0.0]:")]
        [Units("-")]
        public double ExponentRootDistribution
        {
            get { return myExponentRootDistribution; }
            set { myExponentRootDistribution = value; }
        }

        /// <summary>Factor to compute root distribution (controls where, below maxRootDepth, the function is zero).</summary>
        private double rootBottomDistributionFactor = 1.05;

        // - Digestibility and feed quality  --------------------------------------------------------------------------

        /// <summary>Digestibility of cell walls for each tissue age, growing, developed, mature and dead (0-1).</summary>
        private double[] myDigestibilitiesCellWall = {0.6, 0.6, 0.6, 0.2};

        /// <summary>Gets or sets the digestibility of cell walls for each tissue age, growing, developed, mature and dead (0-1).</summary>
        [Description("Digestibility of cell wall in plant tissues, by age (growing, developed, mature and dead) [0-1]:")]
        [Units("0-1")]
        public double[] DigestibilitiesCellWall
        {
            get { return myDigestibilitiesCellWall; }
            set { myDigestibilitiesCellWall = value; }
        }

        /// <summary>Gets or sets the digestibility of proteins in plant tissues (0-1).</summary>
        [XmlIgnore]
        [Units("0-1")]
        public double DigestibilitiesProtein = 1.0;

        /// <summary>Gets or sets the soluble fraction of carbohydrates in newly grown tissues (0-1).</summary>
        [XmlIgnore]
        [Units("0-1")]
        public double SugarFractionNewGrowth = 0.0;

        // - Minimum DM and preferences when harvesting  --------------------------------------------------------------

        /// <summary>Minimum above ground green DM, leaf and stems (kg/ha).</summary>
        private double myMinimumGreenWt = 300.0;

        /// <summary>Gets or sets the minimum above ground green DM, leaf and stems (kg/ha).</summary>
        [Description("Minimum above ground green DM [kg/ha]:")]
        [Units("kg/ha")]
        public double MinimumGreenWt
        {
            get { return myMinimumGreenWt; }
            set { myMinimumGreenWt = value; }
        }

        /// <summary>Gets or sets the leaf proportion in the minimum green Wt (0-1).</summary>
        [XmlIgnore]
        [Units("0-1")]
        public double MinimumGreenLeafProp = 0.8;

        /// <summary>Gets or sets the minimum root amount relative to minimum green Wt (>0.0).</summary>
        [XmlIgnore]
        [Units("0-1")]
        public double MinimumGreenRootProp = 0.5;

        /// <summary>Proportion of stolon DM standing, available for removal (0-1).</summary>
        private double myFractionStolonStanding = 0.0;

        /// <summary>Gets or sets the proportion of stolon DM standing, available for removal (0-1).</summary>
        [Description("Proportion of stolon DM standing, available for removal [0-1]:")]
        [Units("0-1")]
        public double FractionStolonStanding
        {
            get { return myFractionStolonStanding; }
            set { myFractionStolonStanding = value; }
        }

        /// <summary>Relative preference for live over dead material during graze (-).</summary>
        private double myPreferenceForGreenOverDead = 1.0;

        /// <summary>Gets or sets the relative preference for live over dead material during graze (-).</summary>
        [Description("Relative preference for live over dead material during graze:")]
        [Units(">0.0")]
        public double PreferenceForGreenOverDead
        {
            get { return myPreferenceForGreenOverDead; }
            set { myPreferenceForGreenOverDead = value; }
        }

        /// <summary>Relative preference for leaf over stem-stolon material during graze (-).</summary>
        private double myPreferenceForLeafOverStems = 1.0;

        /// <summary>Gets or sets the relative preference for leaf over stem-stolon material during graze (-).</summary>
        [Description("Relative preference for leaf over stem-stolon material during graze:")]
        [Units(">0.0")]
        public double PreferenceForLeafOverStems
        {
            get { return myPreferenceForLeafOverStems; }
            set { myPreferenceForLeafOverStems = value; }
        }

        // - Annual species  ------------------------------------------------------------------------------------------

        /// <summary>Gets or sets the day of year when seeds are allowed to germinate</summary>
        [XmlIgnore]
        [Units("day")]
        public int doyGermination = 240;

        /// <summary>Gets or sets the number of days from emergence to anthesis</summary>
        [XmlIgnore]
        [Units("day")]
        public int daysEmergenceToAnthesis = 100;

        /// <summary>Gets or sets the number of days from anthesis to maturity</summary>
        [XmlIgnore]
        [Units("days")]
        public int daysAnthesisToMaturity = 100;

        /// <summary>Gets or sets the cumulative degrees-day from emergence to anthesis</summary>
        [XmlIgnore]
        [Units("oCd")]
        public double degreesDayForAnthesis = 0.0;

        /// <summary>Gets or sets the cumulative degrees-day from anthesis to maturity</summary>
        [XmlIgnore]
        [Units("oCd")]
        public double degreesDayForMaturity = 0.0;

        /// <summary>Gets or sets the number of days from emergence with reduced growth</summary>
        [XmlIgnore]
        [Units("days")]
        public int daysAnnualsFactor = 60;

        // - Other parameters  ----------------------------------------------------------------------------------------

        /// <summary>The FVPD function</summary>
        [XmlIgnore]
        public BrokenStick FVPDFunction = new BrokenStick
        {
            X = new double[] { 0.0, 10.0, 50.0 },
            Y = new double[] { 1.0, 1.0, 1.0 }
        };

        /// <summary>Flag which module will perform the water uptake process</summary>
        internal string MyWaterUptakeSource = "species";

        /// <summary>Flag which method for computing soil available water will be used</summary>
        private PlantAvailableWaterMethod myWaterAvailableMethod = PlantAvailableWaterMethod.Default;

        /// <summary>Flag which method for computing soil available water will be used</summary>
        [Description("Choose the method for computing soil available water:")]
        [Units("")]
        public PlantAvailableWaterMethod WaterAvailableMethod
        {
            get { return myWaterAvailableMethod; }
            set { myWaterAvailableMethod = value; }
        }

        /// <summary>Flag which module will perform the nitrogen uptake process</summary>
        internal string MyNitrogenUptakeSource = "species";

        /// <summary>Flag which method for computing available soil nitrogen will be used</summary>
        private PlantAvailableNitrogenMethod myNitrogenAvailableMethod = PlantAvailableNitrogenMethod.BasicAgPasture;

        /// <summary>Flag which method for computing available soil nitrogen will be used</summary>
        [Description("Choose the method for computing soil available nitrogen:")]
        [Units("")]
        public PlantAvailableNitrogenMethod NitrogenAvailableMethod
        {
            get { return myNitrogenAvailableMethod; }
            set { myNitrogenAvailableMethod = value; }
        }

        /// <summary>Reference value for root length density for the Water and N availability</summary>
        internal double ReferenceRLD = 2.0;

        /// <summary>Exponent controlling the effect of soil moisture variations on water extractability</summary>
        internal double ExponentSoilMoisture = 1.50;

        /// <summary>Reference value of Ksat for water availability function</summary>
        internal double ReferenceKSuptake = 1000.0;

        /// <summary>Exponent of function determining soil extractable N</summary>
        internal double NuptakeSWFactor = 0.25;

        /// <summary>Maximum daily amount of N that can be taken up by the plant [kg/ha]</summary>
        [Description("Maximum daily amount of N that can be taken up by the plant [kg/ha]")]
        [Units("kg/ha")]
        public double MaximumNUptake = 10.0;

        /// <summary>The value for the ammonium uptake coefficient</summary>
        //[Description("Ammonium uptake coefficient")]
        [XmlIgnore]
        public double KNH4 = 1.0;

        /// <summary>The value for the nitrate uptake coefficient</summary>
        //[Description("Nitrate uptake coefficient")]
        [XmlIgnore]
        public double KNO3 = 1.0;

        /// <summary>Availability factor for NH4</summary>
        [XmlIgnore]
        public double kuNH4 = 0.50;

        /// <summary>Availability factor for NO3</summary>
        [XmlIgnore]
        public double kuNO3 = 0.95;

        #endregion  --------------------------------------------------------------------------------------------------------

        #region Private variables  -----------------------------------------------------------------------------------------

        /// <summary>Flag whether several routines are ran by species or are controlled by the Sward.</summary>
        internal bool isSwardControlled = false;

        /// <summary>Flag whether this species is alive (activelly growing).</summary>
        private bool isAlive = true;

        /// <summary>Holds info about state of leaves (DM and N).</summary>
        internal PastureAboveGroundOrgan leaves;

        /// <summary>Holds info about state of sheath/stems (DM and N).</summary>
        internal PastureAboveGroundOrgan stems;

        /// <summary>Holds info about state of stolons (DM and N).</summary>
        internal PastureAboveGroundOrgan stolons;

        /// <summary>Holds info about state of roots (DM and N).</summary>
        internal PastureBelowGroundOrgan roots;

        /// <summary>Holds the basic state variables for this plant (to be used for reset).</summary>
        private SpeciesBasicStateSettings InitialState;

        // Defining the plant type  -----------------------------------------------------------------------------------

        /// <summary>Flag whether this species is annual or perennial.</summary>
        private bool isAnnual = false;

        /// <summary>Flag whether this species is a legume.</summary>
        private bool isLegume = false;

        // Annual species adn phenology  ------------------------------------------------------------------------------

        /// <summary>The phenologic stage (0-2).</summary>
        /// <remarks>0 = germinating, 1 = vegetative, 2 = reproductive, negative for dormant/not sown.</remarks>
        private int phenologicStage = -1;

        /// <summary>The number of days since emergence (days).</summary>
        private double daysSinceEmergence;

        /// <summary>The cumulatve degrees day during vegetative phase (oCd).</summary>
        private double growingGDD;

        /// <summary>The factor for biomass senescence according to phase (0-1).</summary>
        private double phenoFactor;

        /// <summary>The cumulative degrees-day during germination phase (oCd).</summary>
        private double germinationGDD;

        // Photosynthesis, growth, and turnover  ----------------------------------------------------------------------

        /// <summary>The irradiance on top of canopy (J/m^2 leaf/s).</summary>
        private double irradianceTopOfCanopy;

        /// <summary>The gross photosynthesis rate, or C assimilation (kg C/ha/day).</summary>
        private double grossPhotosynthesis;

        /// <summary>The growth respiration rate (kg C/ha/day).</summary>
        private double respirationGrowth;

        /// <summary>The maintenance respiration rate (kg C/ha/day).</summary>
        private double respirationMaintenance;

        /// <summary>The amount of C remobilisable from senesced tissue (kg C/ha/day).</summary>
        private double remobilisableC;

        /// <summary>The amount of C remobilised from senesced tissue (kg C/ha/day).</summary>
        private double remobilisedC;

        /// <summary>Daily net growth potential (kg DM/ha).</summary>
        private double dGrowthPot;

        /// <summary>Daily potential growth after water stress (kg DM/ha).</summary>
        private double dGrowthWstress;

        /// <summary>Daily growth after nutrient stress, actual growth (kg DM/ha).</summary>
        private double dGrowthActual;

        /// <summary>Effective plant growth, actual growth minus senescence (kg DM/ha).</summary>
        private double dGrowthEff;

        /// <summary>Actual growth of shoot (kg/ha).</summary>
        private double dGrowthShootDM;

        /// <summary>Actual growth of roots (kg/ha).</summary>
        private double dGrowthRootDM;

        /// <summary>Actual N allocation into shoot (kg/ha).</summary>
        private double dGrowthShootN;

        /// <summary>Actual N allocation into roots (kg/ha).</summary>
        private double dGrowthRootN;

        /// <summary>DM amount detached from shoot, added to surface OM (kg/ha).</summary>
        private double detachedShootDM;

        /// <summary>N amount in detached tissues from shoot (kg/ha).</summary>
        private double detachedShootN;

        /// <summary>DM amount detached from roots, added to soil FOM (kg/ha).</summary>
        private double detachedRootDM;

        /// <summary>N amount in detached tissues from roots (kg/ha).</summary>
        private double detachedRootN;

        /// <summary>Fraction of new growth allocated to shoot (0-1).</summary>
        private double fractionToShoot;

        /// <summary>Fraction of new shoot growth allocated to leaves (0-1).</summary>
        private double fractionToLeaf;

        /// <summary>Flag whether the factor adjusting Shoot:Root ratio during reproductive season is being used.</summary>
        private bool usingReproSeasonFactor = true;

        /// <summary>The three intervals defining the reproductive season (onset, main phase, and outset).</summary>
        private double[] reproSeasonInterval;

        /// <summary>The day of the year for the start of the reproductive season.</summary>
        private double doyIniReproSeason;

        /// <summary>The relative increase in the shoot-root ratio during reproductive season (0-1).</summary>
        private double allocationIncreaseRepro;

        /// <summary>The daily DM turnover rate for live shoot tissues (0-1).</summary>
        private double gama;

        /// <summary>The daily DM turnover rate for dead shoot tissues (0-1).</summary>
        private double gamaD;

        /// <summary>The daily DM turnover rate for roots tissue (0-1).</summary>
        private double gamaR;

        /// <summary>The daily DM turnover rate for stolon tissue (0-1).</summary>
        private double gamaS;

        /// <summary>The tissue turnover factor due to variations in temperature (0-1).</summary>
        private double ttfTemperature;

        /// <summary>The tissue turnover factor due to variations in moisture (0-1).</summary>
        private double ttfMoistureShoot;

        /// <summary>The tissue turnover factor due to variations in moisture (0-1).</summary>
        private double ttfMoistureRoot;

        /// <summary>The tissue turnover factor due to variations in moisture (0-1).</summary>
        private double ttfLeafNumber;

        // Plant height, LAI and cover  -------------------------------------------------------------------------------

        /// <summary>The plant's green LAI (m^2/m^2).</summary>
        private double greenLAI;

        /// <summary>The plant's dead LAI (m^2/m^2).</summary>
        private double deadLAI;

        /// <summary>Flag whether stem and stolons are considered for computing LAI green (mostly when DM is low).</summary>
        private bool usingStemStolonEffect = true;

        // Root depth and distribution --------------------------------------------------------------------------------

        /// <summary>The daily variation in root depth (mm).</summary>
        private double dRootDepth;

        // water uptake process  --------------------------------------------------------------------------------------

        /// <summary>The amount of water demanded for new growth (mm).</summary>
        private double myWaterDemand;

        /// <summary>The amount of soil available water (mm).</summary>
        private double[] mySoilWaterAvailable;

        /// <summary>The amount of soil water taken up (mm).</summary>
        private double[] mySoilWaterUptake;

        // Amounts and fluxes of N in the plant  ----------------------------------------------------------------------

        /// <summary>The N demand for new growth, with luxury uptake (kg/ha).</summary>
        private double demandLuxuryN;

        /// <summary>The N demand for new growth, at optimum N content (kg/ha).</summary>
        private double demandOptimumN;

        /// <summary>The amount of N fixation from atmosphere, for legumes (kg/ha).</summary>
        private double fixedN;

        /// <summary>The amount of senesced N actually remobilised (kg/ha).</summary>
        private double senescedNRemobilised;

        /// <summary>The amount of luxury N actually remobilised (kg/ha).</summary>
        private double luxuryNRemobilised;

        /// <summary>The amount of N used in new growth (kg/ha).</summary>
        private double dNewGrowthN;

        // N uptake process  ------------------------------------------------------------------------------------------

        /// <summary>The amount of N demanded from the soil (kg/ha).</summary>
        private double mySoilNDemand;

        /// <summary>The amount of NH4-N in the soil available to the plant (kg/ha).</summary>
        private double[] mySoilNH4Available;

        /// <summary>The amount of NO3-N in the soil available to the plant (kg/ha).</summary>
        private double[] mySoilNO3Available;

        /// <summary>The amount of soil NH4-N taken up by the plant (kg/ha).</summary>
        private double[] mySoilNH4Uptake;

        /// <summary>The amount of soil NO3-N taken up by the plant (kg/ha).</summary>
        private double[] mySoilNO3Uptake;

        // growth limiting factors ------------------------------------------------------------------------------------

        /// <summary>The growth factor due to variations in intercepted radiation (0-1).</summary>
        private double glfRadn = 1.0;

        /// <summary>The growth factor due to N variations in atmospheric CO2 (0-1).</summary>
        private double glfCO2 = 1.0;

        /// <summary>The growth factor due to variations in plant N concentration (0-1).</summary>
        private double glfNc = 1.0;

        /// <summary>The growth factor due to variations in air temperature (0-1).</summary>
        private double glfTemp = 1.0;

        /// <summary>Flag whether the factor reducing photosynthesis due to heat damage is being used.</summary>
        private bool usingHeatStressFactor = true;

        /// <summary>Flag whether the factor reducing photosynthesis due to cold damage is being used.</summary>
        private bool usingColdStressFactor = true;

        /// <summary>The growth factor due to heat stress (0-1).</summary>
        private double glfHeat = 1.0;

        /// <summary>The growth factor due to cold stress (0-1).</summary>
        private double glfCold = 1.0;

        /// <summary>The growth limiting factor due to water stress (0-1).</summary>
        private double glfWater = 1.0;

        /// <summary>Flag whether the factor reducing growth due to logging is used on a cumulative basis.</summary>
        private bool usingCumulativeWaterLogging = false;

        /// <summary>The cumulative water logging factor (0-1).</summary>
        private double cumWaterLogging;

        /// <summary>The growth limiting factor due to water logging (0-1).</summary>
        private double glfAeration = 1.0;

        /// <summary>The growth limiting factor due to N stress (0-1).</summary>
        private double glfN = 1.0;

        // Auxiliary variables for temperature stress  ----------------------------------------------------------------

        /// <summary>Growth rate reduction factor due to high temperatures (0-1).</summary>
        private double highTempStress = 1.0;

        /// <summary>Cumulative degress of temperature for recovery from heat damage (oCd).</summary>
        private double accumDDHeat = 0.0;

        /// <summary>Growth rate reduction factor due to low temperatures (0-1).</summary>
        private double lowTempStress = 1.0;

        /// <summary>Cumulative degress of temperature for recovery from cold damage (oCd).</summary>
        private double accumDDCold;

        // Harvest and digestibility  ---------------------------------------------------------------------------------

        /// <summary>The fraction of standing DM harvested (0-1).</summary>
        private double defoliatedFraction;

        /// <summary>The DM amount harvested (kg/ha).</summary>
        private double defoliatedDM;

        /// <summary>The N amount in the harvested material (kg/ha).</summary>
        private double defoliatedN;

        /// <summary>The digestibility of defoliated material (0-1).</summary>
        private double defoliatedDigestibility;

        /// <summary>The digestibility of herbage (0-1).</summary>
        private double herbageDigestibility;

        // general auxiliary variables  -------------------------------------------------------------------------------

        /// <summary>Number of layers in the soil.</summary>
        private int nLayers;

        #endregion  --------------------------------------------------------------------------------------------------------

        #region Constants and auxiliary  -----------------------------------------------------------------------------------

        /// <summary>Average carbon content in plant dry matter (kg/kg).</summary>
        internal const double CarbonFractionInDM = 0.4;

        /// <summary>Factor for converting nitrogen to protein (kg/kg).</summary>
        internal const double NitrogenToProteinFactor = 6.25;

        /// <summary>The C:N ratio of protein (-).</summary>
        internal const double CNratioProtein = 3.5;

        /// <summary>The C:N ratio of cell wall (-).</summary>
        internal const double CNratioCellWall = 100.0;

        /// <summary>Minimum significant difference between two values</summary>
        internal const double Epsilon = 0.000000001;

        /// <summary>A yes or no answer.</summary>
        public enum YesNoAnswer
        {
            /// <summary>a positive answer</summary>
            yes,

            /// <summary>a negative answer</summary>
            no
        }

        /// <summary>List of valid species family names.</summary>
        public enum PlantFamilyType
        {
            /// <summary>A grass species, Poaceae</summary>
            Grass,

            /// <summary>A legume species, Fabaceae</summary>
            Legume,

            /// <summary>A non grass or legume species</summary>
            Forb
        }

        /// <summary>List of valid photosynthesis pathways.</summary>
        public enum PhotosynthesisPathwayType
        {
            /// <summary>A C3 plant</summary>
            C3,

            /// <summary>A C4 plant</summary>
            C4
        }

        /// <summary>List of valid methods to compute plant available water.</summary>
        public enum PlantAvailableWaterMethod
        {
            /// <summary>The APSIM default, using kL</summary>
            Default,

            /// <summary>Alternative, using root length and modified kL</summary>
            AlternativeKL,

            /// <summary>Alternative, using root length and relative Ksat</summary>
            AlternativeKS
        }

        /// <summary>List of valid methods to compute plant available water.</summary>
        public enum PlantAvailableNitrogenMethod
        {
            /// <summary>The AgPasture old default</summary>
            BasicAgPasture,

            /// <summary>The APSIM default, using soil water status</summary>
            DefaultAPSIM,

            /// <summary>Alternative, using root length and water status</summary>
            AlternativeRLD,

            /// <summary>Alternative, using water uptake</summary>
            AlternativeWup
        }

        #endregion  --------------------------------------------------------------------------------------------------------

        #region Model outputs  ---------------------------------------------------------------------------------------------

        /// <summary>Flag whether the plant is alive</summary>
        public bool IsAlive
        {
            get { return PlantStatus == "alive"; }
        }

        /// <summary>Gets the plant status (dead, alive, etc).</summary>
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
        [Description("Plant development stage number")]
        [Units("")]
        public int Stage
        {
            get
            {
                if (isAlive)
                {
                    if (phenologicStage < Epsilon)
                        return 1; //"germination";
                    else
                        return 3; //"vegetative" & "reproductive";
                }
                else
                    return 0; //"out"
            }
        }

        /// <summary>Gets the name of the plant development stage.</summary>
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

        /// <summary>The intercepted solar radiation (W/m^2)</summary>
        [XmlIgnore]
        [Units("W/m^2")]
        public double InterceptedRadn { get; set; }

        #region - DM and C amounts  ----------------------------------------------------------------------------------------

        /// <summary>Gets the total plant C content (kg/ha).</summary>
        [Description("Total amount of C in plants")]
        [Units("kg/ha")]
        public double TotalC
        {
            get { return TotalWt * CarbonFractionInDM; }
        }

        /// <summary>Gets the plant total dry matter weight (kg/ha).</summary>
        [Description("Total plant dry matter weight")]
        [Units("kg/ha")]
        public double TotalWt
        {
            get { return AboveGroundWt + BelowGroundWt; }
        }

        /// <summary>Gets the plant DM weight above ground (kg/ha).</summary>
        [Description("Dry matter weight above ground")]
        [Units("kg/ha")]
        public double AboveGroundWt
        {
            get { return AboveGroundLiveWt + AboveGroundDeadWt; }
        }

        /// <summary>Gets the DM weight of live plant parts above ground (kg/ha).</summary>
        [Description("Dry matter weight of alive plants above ground")]
        [Units("kg/ha")]
        public double AboveGroundLiveWt
        {
            get { return leaves.DMLive + stems.DMLive + stolons.DMLive; }
        }

        /// <summary>Gets the DM weight of dead plant parts above ground (kg/ha).</summary>
        [Description("Dry matter weight of dead plants above ground")]
        [Units("kg/ha")]
        public double AboveGroundDeadWt
        {
            get { return leaves.DMDead + stems.DMDead + stolons.DMDead; }
        }

        /// <summary>Gets the DM weight of the plant below ground (kg/ha).</summary>
        [Description("Dry matter weight below ground")]
        [Units("kg/ha")]
        public double BelowGroundWt
        {
            get { return roots.DMTotal; }
        }

        /// <summary>Gets the total standing DM weight (kg/ha).</summary>
        [Description("Dry matter weight of standing herbage")]
        [Units("kg/ha")]
        public double StandingWt
        {
            get { return leaves.DMTotal + stems.DMTotal + stolons.DMTotal * stolons.FractionStanding; }
        }

        /// <summary>Gets the DM weight of standing live plant material (kg/ha).</summary>
        [Description("Dry matter weight of live standing plants parts")]
        [Units("kg/ha")]
        public double StandingLiveWt
        {
            get { return leaves.DMLive + stems.DMLive + stolons.DMLive * stolons.FractionStanding; }
        }

        /// <summary>Gets the DM weight of standing dead plant material (kg/ha).</summary>
        [Description("Dry matter weight of dead standing plants parts")]
        [Units("kg/ha")]
        public double StandingDeadWt
        {
            get { return leaves.DMDead + stems.DMDead + stolons.DMDead * stolons.FractionStanding; }
        }

        /// <summary>Gets the total DM weight of leaves (kg/ha).</summary>
        [Description("Dry matter weight of leaves")]
        [Units("kg/ha")]
        public double LeafWt
        {
            get { return leaves.DMTotal; }
        }

        /// <summary>Gets the DM weight of live leaves (kg/ha).</summary>
        [Description("Dry matter weight of live leaves")]
        [Units("kg/ha")]
        public double LeafLiveWt
        {
            get { return leaves.DMLive; }
        }

        /// <summary>Gets the DM weight of dead leaves (kg/ha).</summary>
        [Description("Dry matter weight of dead leaves")]
        [Units("kg/ha")]
        public double LeafDeadWt
        {
            get { return leaves.DMDead; }
        }

        /// <summary>Gets the toal DM weight of stems and sheath (kg/ha).</summary>
        [Description("Dry matter weight of stems and sheath")]
        [Units("kg/ha")]
        public double StemWt
        {
            get { return stems.DMTotal; }
        }

        /// <summary>Gets the DM weight of live stems and sheath (kg/ha).</summary>
        [Description("Dry matter weight of alive stems and sheath")]
        [Units("kg/ha")]
        public double StemLiveWt
        {
            get { return stems.DMLive; }
        }

        /// <summary>Gets the DM weight of dead stems and sheath (kg/ha).</summary>
        [Description("Dry matter weight of dead stems and sheath")]
        [Units("kg/ha")]
        public double StemDeadWt
        {
            get { return stems.DMDead; }
        }

        /// <summary>Gets the total DM weight od stolons (kg/ha).</summary>
        [Description("Dry matter weight of stolons")]
        [Units("kg/ha")]
        public double StolonWt
        {
            get { return stolons.DMTotal; }
        }

        /// <summary>Gets the total DM weight of roots (kg/ha).</summary>
        [Description("Dry matter weight of roots")]
        [Units("kg/ha")]
        public double RootWt
        {
            get { return roots.DMTotal; }
        }

        /// <summary>Gets the DM weight of roots for each layer (kg/ha).</summary>
        [Description("Dry matter weight of roots")]
        [Units("kg/ha")]
        public double[] RootLayerWt
        {
            get { return roots.Tissue[0].DMLayer; }
        }

        /// <summary>Gets the DM weight of growing tissues from all above ground organs (kg/ha).</summary>
        [Description("Dry matter weight of growing tissues from all above ground organs")]
        [Units("kg/ha")]
        public double GrowingTissuesWt
        {
            get { return leaves.Tissue[0].DM + stems.Tissue[0].DM + stolons.Tissue[0].DM; }
        }

        /// <summary>Gets the DM weight of developed tissues from all above ground organs (kg/ha).</summary>
        [Description("Dry matter weight of developed tissues from all above ground organs")]
        [Units("kg/ha")]
        public double DevelopedTissuesWt
        {
            get { return leaves.Tissue[1].DM + stems.Tissue[1].DM + stolons.Tissue[1].DM; }
        }

        /// <summary>Gets the DM weight of mature tissues from all above ground organs (kg/ha).</summary>
        [Description("Dry matter weight of mature tissues from all above ground organs")]
        [Units("kg/ha")]
        public double MatureTissuesWt
        {
            get { return leaves.Tissue[2].DM + stems.Tissue[2].DM + stolons.Tissue[2].DM; }
        }

        /// <summary>Gets the DM weight of leaves at stage1, growing (kg/ha).</summary>
        [Description("Dry matter weight of leaves at stage 1 (growing)")]
        [Units("kg/ha")]
        public double LeafStage1Wt
        {
            get { return leaves.Tissue[0].DM; }
        }

        /// <summary>Gets the DM weight of leaves stage2, developing (kg/ha).</summary>
        [Description("Dry matter weight of leaves at stage 2 (developing)")]
        [Units("kg/ha")]
        public double LeafStage2Wt
        {
            get { return leaves.Tissue[1].DM; }
        }

        /// <summary>Gets the DM weight of leaves at stage3, mature (kg/ha).</summary>
        [Description("Dry matter weight of leaves at stage 3 (mature)")]
        [Units("kg/ha")]
        public double LeafStage3Wt
        {
            get { return leaves.Tissue[2].DM; }
        }

        /// <summary>Gets the DM weight of leaves at stage4, dead (kg/ha).</summary>
        [Description("Dry matter weight of leaves at stage 4 (dead)")]
        [Units("kg/ha")]
        public double LeafStage4Wt
        {
            get { return leaves.Tissue[3].DM; }
        }

        /// <summary>Gets the DM weight stems and sheath at stage1, growing (kg/ha).</summary>
        [Description("Dry matter weight of stems at stage 1 (growing)")]
        [Units("kg/ha")]
        public double StemStage1Wt
        {
            get { return stems.Tissue[0].DM; }
        }

        /// <summary>Gets the DM weight of stems and sheath at stage2, developing (kg/ha).</summary>
        [Description("Dry matter weight of stems at stage 2 (developing)")]
        [Units("kg/ha")]
        public double StemStage2Wt
        {
            get { return stems.Tissue[1].DM; }
        }

        /// <summary>Gets the DM weight of stems and sheath at stage3, mature (kg/ha).</summary>
        /// <value>The stage3 stems DM weight.</value>
        [Description("Dry matter weight of stems at stage 3 (mature)")]
        [Units("kg/ha")]
        public double StemStage3Wt
        {
            get { return stems.Tissue[2].DM; }
        }

        /// <summary>Gets the DM weight of stems and sheath at stage4, dead (kg/ha).</summary>
        [Description("Dry matter weight of stems at stage 4 (dead)")]
        [Units("kg/ha")]
        public double StemStage4Wt
        {
            get { return stems.Tissue[3].DM; }
        }

        /// <summary>Gets the DM weight of stolons at stage1, growing (kg/ha).</summary>
        [Description("Dry matter weight of stolons at stage 1 (growing)")]
        [Units("kg/ha")]
        public double StolonStage1Wt
        {
            get { return stolons.Tissue[0].DM; }
        }

        /// <summary>Gets the DM weight of stolons at stage2, developing (kg/ha).</summary>
        [Description("Dry matter weight of stolons at stage 2 (developing)")]
        [Units("kg/ha")]
        public double StolonStage2Wt
        {
            get { return stolons.Tissue[1].DM; }
        }

        /// <summary>Gets the DM weight of stolons at stage3, mature (kg/ha).</summary>
        [Description("Dry matter weight of stolons at stage 3 (mature)")]
        [Units("kg/ha")]
        public double StolonStage3Wt
        {
            get { return stolons.Tissue[2].DM; }
        }

        #endregion  --------------------------------------------------------------------------------------------------------

        #region - C and DM flows  ------------------------------------------------------------------------------------------

        /// <summary>Gets the potential carbon assimilation (kg/ha).</summary>
        [Description("Potential C assimilation")]
        [Units("kgC/ha")]
        public double PotCarbonAssimilation
        {
            get { return grossPhotosynthesis; }
        }

        /// <summary>Gets the carbon loss via respiration (kg/ha).</summary>
        [Description("Loss of C via respiration")]
        [Units("kgC/ha")]
        public double CarbonLossRespiration
        {
            get { return respirationMaintenance; }
        }

        /// <summary>Gets the carbon remobilised from senescent tissue (kg/ha).</summary>
        [Description("C remobilised from senescent tissue")]
        [Units("kgC/ha")]
        public double CarbonRemobilisable
        {
            get { return remobilisableC; }
        }

        /// <summary>Gets the gross potential growth rate (kg DM/ha).</summary>
        [Description("Gross potential growth rate (potential C assimilation)")]
        [Units("kg/ha")]
        public double GrossPotentialGrowthWt
        {
            get { return grossPhotosynthesis / CarbonFractionInDM; }
        }

        /// <summary>Gets the respiration rate (kg DM/ha).</summary>
        [Description("Respiration rate (DM lost via respiration)")]
        [Units("kg/ha")]
        public double RespirationWt
        {
            get { return respirationMaintenance / CarbonFractionInDM; }
        }

        /// <summary>Gets the remobilisation rate (kg DM/ha).</summary>
        [Description("C remobilisation (DM remobilised from old tissue to new growth)")]
        [Units("kg/ha")]
        public double RemobilisationWt
        {
            get { return remobilisableC / CarbonFractionInDM; }
        }

        /// <summary>Gets the net potential growth rate (kg DM/ha).</summary>
        [Description("Net potential growth rate")]
        [Units("kg/ha")]
        public double NetPotentialGrowthWt
        {
            get { return dGrowthPot; }
        }

        /// <summary>Gets the potential growth rate after water stress (kg DM/ha).</summary>
        [Description("Potential growth rate after water stress")]
        [Units("kg/ha")]
        public double PotGrowthWt_Wstress
        {
            get { return dGrowthWstress; }
        }

        /// <summary>Gets the actual growth rate (kg DM/ha).</summary>
        [Description("Actual growth rate, after nutrient stress")]
        [Units("kg/ha")]
        public double ActualGrowthWt
        {
            get { return dGrowthActual; }
        }

        /// <summary>Gets the effective growth rate (kg DM/ha).</summary>
        [Description("Effective growth rate, after turnover")]
        [Units("kg/ha")]
        public double EffectiveGrowthWt
        {
            get { return dGrowthEff; }
        }

        /// <summary>Gets the effective herbage growth rate (kg/ha).</summary>
        [Description("Effective herbage growth rate, above ground")]
        [Units("kg/ha")]
        public double HerbageGrowthWt
        {
            get { return dGrowthShootDM; }
        }

        /// <summary>Gets the effective root growth rate (kg/ha).</summary>
        [Description("Effective root growth rate")]
        [Units("kg/ha")]
        public double RootGrowthWt
        {
            get { return dGrowthRootDM - detachedRootDM; }
        }

        /// <summary>Gets the litter DM weight deposited onto soil surface (kg/ha).</summary>
        [Description("Litter amount deposited onto soil surface")]
        [Units("kg/ha")]
        public double LitterWt
        {
            get { return detachedShootDM; }
        }

        /// <summary>Gets the senesced root DM weight (kg/ha).</summary>
        [Description("Amount of senesced roots added to soil FOM")]
        [Units("kg/ha")]
        public double RootSenescedWt
        {
            get { return detachedRootDM; }
        }

        /// <summary>Gets the gross primary productivity (kg/ha).</summary>
        [Description("Gross primary productivity")]
        [Units("kg/ha")]
        public double GPP
        {
            get { return grossPhotosynthesis / CarbonFractionInDM; }
        }

        /// <summary>Gets the net primary productivity (kg/ha).</summary>
        [Description("Net primary productivity")]
        [Units("kg/ha")]
        public double NPP
        {
            get { return (grossPhotosynthesis - respirationGrowth - respirationMaintenance) / CarbonFractionInDM; }
        }

        /// <summary>Gets the net above-ground primary productivity (kg/ha).</summary>
        [Description("Net above-ground primary productivity")]
        [Units("kg/ha")]
        public double NAPP
        {
            get { return (grossPhotosynthesis - respirationGrowth - respirationMaintenance) * fractionToShoot / CarbonFractionInDM; }
        }

        /// <summary>Gets the net below-ground primary productivity (kg/ha).</summary>
        [Description("Net below-ground primary productivity")]
        [Units("kg/ha")]
        public double NBPP
        {
            get { return (grossPhotosynthesis - respirationGrowth - respirationMaintenance) * (1 - fractionToShoot) / CarbonFractionInDM; }
        }

        #endregion  --------------------------------------------------------------------------------------------------------

        #region - N amounts  -----------------------------------------------------------------------------------------------

        /// <summary>Gets the plant total N content (kg/ha).</summary>
        [Description("Total plant N amount")]
        [Units("kg/ha")]
        public double TotalN
        {
            get { return AboveGroundN + BelowGroundN; }
        }

        /// <summary>Gets the N content in the plant above ground (kg/ha).</summary>
        [Description("N amount of plant parts above ground")]
        [Units("kg/ha")]
        public double AboveGroundN
        {
            get { return AboveGroundLiveN + AboveGroundDeadN; }
        }

        /// <summary>Gets the N content in live plant material above ground (kg/ha).</summary>
        [Description("N amount of alive plant parts above ground")]
        [Units("kg/ha")]
        public double AboveGroundLiveN
        {
            get { return leaves.NLive + stems.NLive + stolons.NLive; }
        }

        /// <summary>Gets the N content of dead plant material above ground (kg/ha).</summary>
        [Description("N amount of dead plant parts above ground")]
        [Units("kg/ha")]
        public double AboveGroundDeadN
        {
            get { return leaves.NDead + stems.NDead + stolons.NDead; }
        }

        /// <summary>Gets the N content of plants below ground (kg/ha).</summary>
        [Description("N amount of plant parts below ground")]
        [Units("kg/ha")]
        public double BelowGroundN
        {
            get { return roots.NTotal; }
        }

        /// <summary>Gets the N content of standing plants (kg/ha).</summary>
        [Description("N amount of standing herbage")]
        [Units("kg/ha")]
        public double StandingN
        {
            get { return leaves.NTotal + stems.NTotal + stolons.NTotal * stolons.FractionStanding; }
        }

        /// <summary>Gets the N content of standing live plant material (kg/ha).</summary>
        [Description("N amount of alive standing herbage")]
        [Units("kg/ha")]
        public double StandingLiveN
        {
            get { return leaves.NLive + stems.NLive + stolons.NLive * stolons.FractionStanding; }
        }

        /// <summary>Gets the N content  of standing dead plant material (kg/ha).</summary>
        [Description("N amount of dead standing herbage")]
        [Units("kg/ha")]
        public double StandingDeadN
        {
            get { return leaves.NDead + stems.NDead + stolons.NDead * stolons.FractionStanding; }
        }

        /// <summary>Gets the total N content of leaves (kg/ha).</summary>
        [Description("N amount in the plant's leaves")]
        [Units("kg/ha")]
        public double LeafN
        {
            get { return leaves.NTotal; }
        }

        /// <summary>Gets the total N content of stems and sheath (kg/ha).</summary>
        [Description("N amount in the plant's stems")]
        [Units("kg/ha")]
        public double StemN
        {
            get { return stems.NTotal; }
        }

        /// <summary>Gets the total N content of stolons (kg/ha).</summary>
        [Description("N amount in the plant's stolons")]
        [Units("kg/ha")]
        public double StolonN
        {
            get { return stolons.NTotal; }
        }

        /// <summary>Gets the total N content of roots (kg/ha).</summary>
        [Description("N amount in the plant's roots")]
        [Units("kg/ha")]
        public double RootN
        {
            get { return roots.NTotal; }
        }

        /// <summary>Gets the N content of live leaves (kg/ha).</summary>
        [Description("N amount in live leaves")]
        [Units("kg/ha")]
        public double LeafLiveN
        {
            get { return leaves.NLive; }
        }

        /// <summary>Gets the N content of dead leaves (kg/ha).</summary>
        [Description("N amount in dead leaves")]
        [Units("kg/ha")]
        public double LeafDeadN
        {
            get { return leaves.NDead; }
        }

        /// <summary>Gets the N content of live stems and sheath (kg/ha).</summary>
        [Description("N amount in live stems")]
        [Units("kg/ha")]
        public double StemGreenN
        {
            get { return stems.NLive; }
        }

        /// <summary>Gets the N content of dead stems and sheath (kg/ha).</summary>
        [Description("N amount in dead sytems")]
        [Units("kg/ha")]
        public double StemDeadN
        {
            get { return stems.NDead; }
        }

        /// <summary>Gets the N content of growing tissues from all above ground organs (kg/ha).</summary>
        [Description("N amount in growing tissues from all above ground organs")]
        [Units("kg/ha")]
        public double GrowingTissuesN
        {
            get { return leaves.Tissue[0].Namount + stems.Tissue[0].Namount + stolons.Tissue[0].Namount; }
        }

        /// <summary>Gets the N content of developed tissues from all above ground organs (kg/ha).</summary>
        [Description("N amount in developed tissues from all above ground organs")]
        [Units("kg/ha")]
        public double DevelopedTissuesN
        {
            get { return leaves.Tissue[1].Namount + stems.Tissue[1].Namount + stolons.Tissue[1].Namount; }
        }

        /// <summary>Gets the N content of mature tissues from all above ground organs (kg/ha).</summary>
        [Description("N amount in mature tissues from all above ground organs")]
        [Units("kg/ha")]
        public double MatureTissuesN
        {
            get { return leaves.Tissue[2].Namount + stems.Tissue[2].Namount + stolons.Tissue[2].Namount; }
        }

        /// <summary>Gets the N content of leaves at stage1, growing (kg/ha).</summary>
        [Description("N amount in leaves at stage 1 (growing)")]
        [Units("kg/ha")]
        public double LeafStage1N
        {
            get { return leaves.Tissue[0].Namount; }
        }

        /// <summary>Gets the N content of leaves at stage2, developing (kg/ha).</summary>
        [Description("N amount in leaves at stage 2 (developing)")]
        [Units("kg/ha")]
        public double LeafStage2N
        {
            get { return leaves.Tissue[1].Namount; }
        }

        /// <summary>Gets the N content of leaves at stage3, mature (kg/ha).</summary>
        [Description("N amount in leaves at stage 3 (mature)")]
        [Units("kg/ha")]
        public double LeafStage3N
        {
            get { return leaves.Tissue[2].Namount; }
        }

        /// <summary>Gets the N content of leaves at stage4, dead (kg/ha).</summary>
        [Description("N amount in leaves at stage 4 (dead)")]
        [Units("kg/ha")]
        public double LeafStage4N
        {
            get { return leaves.Tissue[3].Namount; }
        }

        /// <summary>Gets the N content of stems and sheath at stage1, growing (kg/ha).</summary>
        [Description("N amount in stems at stage 1 (developing)")]
        [Units("kg/ha")]
        public double StemStage1N
        {
            get { return stems.Tissue[0].Namount; }
        }

        /// <summary>Gets the N content of stems and sheath at stage2, developing (kg/ha).</summary>
        [Description("N amount in stems at stage 2 (developing)")]
        [Units("kg/ha")]
        public double StemStage2N
        {
            get { return stems.Tissue[1].Namount; }
        }

        /// <summary>Gets the N content of stems and sheath at stage3, mature (kg/ha).</summary>
        [Description("N amount in stems at stage 3 (mature)")]
        [Units("kg/ha")]
        public double StemStage3N
        {
            get { return stems.Tissue[2].Namount; }
        }

        /// <summary>Gets the N content of stems and sheath at stage4, dead (kg/ha).</summary>
        [Description("N amount in stems at stage 4 (dead)")]
        [Units("kg/ha")]
        public double StemStage4N
        {
            get { return stems.Tissue[3].Namount; }
        }

        /// <summary>Gets the N content of stolons at stage1, growing (kg/ha).</summary>
        [Description("N amount in stolons at stage 1 (developing)")]
        [Units("kg/ha")]
        public double StolonStage1N
        {
            get { return stolons.Tissue[0].Namount; }
        }

        /// <summary>Gets the N content of stolons at stage2, developing (kg/ha).</summary>
        [Description("N amount in stolons at stage 2 (developing)")]
        [Units("kg/ha")]
        public double StolonStage2N
        {
            get { return stolons.Tissue[1].Namount; }
        }

        /// <summary>Gets the N content of stolons as stage3, mature (kg/ha).</summary>
        [Description("N amount in stolons at stage 3 (mature)")]
        [Units("kg/ha")]
        public double StolonStage3N
        {
            get { return stolons.Tissue[2].Namount; }
        }

        #endregion  --------------------------------------------------------------------------------------------------------

        #region - N concentrations  ----------------------------------------------------------------------------------------

        /// <summary>Gets the average N concentration of standing plant material (kg/kg).</summary>
        [Description("Average N concentration in standing plant parts")]
        [Units("kg/kg")]
        public double StandingNConc
        {
            get { return MathUtilities.Divide(StandingN, StandingWt, 0.0); }
        }

        /// <summary>Gets the average N concentration of leaves (kg/kg).</summary>
        [Description("Average N concentration in leaves")]
        [Units("kg/kg")]
        public double LeafNConc
        {
            get { return MathUtilities.Divide(LeafN, LeafWt, 0.0); }
        }

        /// <summary>Gets the average N concentration of stems and sheath (kg/kg).</summary>
        [Description("Average N concentration in stems")]
        [Units("kg/kg")]
        public double StemNConc
        {
            get { return MathUtilities.Divide(StemN, StemWt, 0.0); }
        }

        /// <summary>Gets the average N concentration of stolons (kg/kg).</summary>
        [Description("Average N concentration in stolons")]
        [Units("kg/kg")]
        public double StolonNConc
        {
            get { return MathUtilities.Divide(StolonN, StolonWt, 0.0); }
        }

        /// <summary>Gets the average N concentration of roots (kg/kg).</summary>
        [Description("Average N concentration in roots")]
        [Units("kg/kg")]
        public double RootNConc
        {
            get { return MathUtilities.Divide(RootN, RootWt, 0.0); }
        }

        /// <summary>Gets the N concentration of leaves at stage1, growing (kg/kg).</summary>
        [Description("N concentration of leaves at stage 1 (growing)")]
        [Units("kg/kg")]
        public double LeafStage1NConc
        {
            get { return leaves.Tissue[0].Nconc; }
        }

        /// <summary>Gets the N concentration of leaves at stage2, developing (kg/kg).</summary>
        [Description("N concentration of leaves at stage 2 (developing)")]
        [Units("kg/kg")]
        public double LeafStage2NConc
        {
            get { return leaves.Tissue[1].Nconc; }
        }

        /// <summary>Gets the N concentration of leaves at stage3, mature (kg/kg).</summary>
        [Description("N concentration of leaves at stage 3 (mature)")]
        [Units("kg/kg")]
        public double LeafStage3NConc
        {
            get { return leaves.Tissue[2].Nconc; }
        }

        /// <summary>Gets the N concentration of leaves at stage4, dead (kg/kg).</summary>
        [Description("N concentration of leaves at stage 4 (dead)")]
        [Units("kg/kg")]
        public double LeafStage4NConc
        {
            get { return leaves.Tissue[3].Nconc; }
        }

        /// <summary>Gets the N concentration of stems at stage1, growing (kg/kg).</summary>
        [Description("N concentration of stems at stage 1 (growing)")]
        [Units("kg/kg")]
        public double StemStage1NConc
        {
            get { return stems.Tissue[0].Nconc; }
        }

        /// <summary>Gets the N concentration of stems at stage2, developing (kg/kg).</summary>
        [Description("N concentration of stems at stage 2 (developing)")]
        [Units("kg/kg")]
        public double StemStage2NConc
        {
            get { return stems.Tissue[1].Nconc; }
        }

        /// <summary>Gets the N concentration of stems at stage3, mature (kg/kg).</summary>
        [Description("N concentration of stems at stage 3 (mature)")]
        [Units("kg/kg")]
        public double StemStage3NConc
        {
            get { return stems.Tissue[2].Nconc; }
        }

        /// <summary>Gets the N concentration of stems at stage4, dead (kg/kg).</summary>
        [Description("N concentration of stems at stage 4 (dead)")]
        [Units("kg/kg")]
        public double StemStage4NConc
        {
            get { return stems.Tissue[3].Nconc; }
        }

        /// <summary>Gets the N concentration of stolons at stage1, growing (kg/kg).</summary>
        [Description("N concentration of stolons at stage 1 (growing)")]
        [Units("kg/kg")]
        public double StolonStage1NConc
        {
            get { return stolons.Tissue[0].Nconc; }
        }

        /// <summary>Gets the N concentration of stolons at stage2, developing (kg/kg).</summary>
        [Description("N concentration of stolons at stage 2 (developing)")]
        [Units("kg/kg")]
        public double StolonStage2NConc
        {
            get { return stolons.Tissue[1].Nconc; }
        }

        /// <summary>Gets the N concentration of stolons at stage3, mature (kg/kg).</summary>
        [Description("N concentration of stolons at stage 3 (mature)")]
        [Units("kg/kg")]
        public double StolonStage3NConc
        {
            get { return stolons.Tissue[2].Nconc; }
        }

        #endregion  --------------------------------------------------------------------------------------------------------

        #region - N flows  -------------------------------------------------------------------------------------------------

        /// <summary>Gets amount of N remobilisable from senesced tissue (kg/ha).</summary>
        [Description("Amount of N remobilisable from senesced material")]
        [Units("kg/ha")]
        public double RemobilisableSenescedN
        {
            get
            {
                return leaves.NSenescedRemobilisable + stems.NSenescedRemobilisable + stolons.NSenescedRemobilisable + roots.NSenescedRemobilisable;
            }
        }

        /// <summary>Gets the amount of N remobilised from senesced tissue (kg/ha).</summary>
        [Description("Amount of N remobilised from senesced material")]
        [Units("kg/ha")]
        public double RemobilisedSenescedN
        {
            get { return senescedNRemobilised; }
        }

        /// <summary>Gets the amount of luxury N potentially remobilisable (kg/ha).</summary>
        [Description("Amount of luxury N potentially remobilisable")]
        [Units("kg/ha")]
        public double RemobilisableLuxuryN
        {
            get { return leaves.NLuxuryRemobilisable + stems.NLuxuryRemobilisable + stolons.NLuxuryRemobilisable + roots.NLuxuryRemobilisable; }
        }

        /// <summary>Gets the amount of luxury N remobilised (kg/ha).</summary>
        [Description("Amount of luxury N remobilised")]
        [Units("kg/ha")]
        public double RemobilisedLuxuryN
        {
            get { return luxuryNRemobilised; }
        }

        /// <summary>Gets the amount of atmospheric N fixed (kg/ha).</summary>
        [Description("Amount of atmospheric N fixed")]
        [Units("kg/ha")]
        public double FixedN
        {
            get { return fixedN; }
        }

        /// <summary>Gets the amount of N required with luxury uptake (kg/ha).</summary>
        [Description("Amount of N required with luxury uptake")]
        [Units("kg/ha")]
        public double RequiredLuxuryN
        {
            get { return demandLuxuryN; }
        }

        /// <summary>Gets the amount of N required for optimum N content (kg/ha).</summary>
        [Description("Amount of N required for optimum growth")]
        [Units("kg/ha")]
        public double RequiredOptimumN
        {
            get { return demandOptimumN; }
        }

        /// <summary>Gets the amount of N demanded from soil (kg/ha).</summary>
        [Description("Amount of N demanded from soil")]
        [Units("kg/ha")]
        public double DemandSoilN
        {
            get { return mySoilNDemand; }
        }

        /// <summary>Gets the amount of plant available N in the soil (kg/ha).</summary>
        [Description("Amount of N available in the soil")]
        [Units("kg/ha")]
        public double SoilAvailableN
        {
            get { return mySoilNH4Available.Sum() + mySoilNO3Available.Sum(); }
        }

        /// <summary>Gets the amount of N taken up from soil (kg/ha).</summary>
        [Description("Amount of N uptake")]
        [Units("kg/ha")]
        public double UptakeN
        {
            get { return mySoilNH4Uptake.Sum() + mySoilNO3Uptake.Sum(); }
        }

        /// <summary>Gets the amount of N deposited as litter onto surface OM (kg/ha).</summary>
        [Description("Amount of N deposited as litter onto soil surface")]
        [Units("kg/ha")]
        public double LitterN
        {
            get { return detachedShootN; }
        }

        /// <summary>Gets the amount of N from senesced roots added to soil FOM (kg/ha).</summary>
        [Description("Amount of N from senesced roots added to soil FOM")]
        [Units("kg/ha")]
        public double SenescedRootN
        {
            get { return detachedRootN; }
        }

        /// <summary>Gets the amount of N in new grown tissue (kg/ha).</summary>
        [Description("Amount of N in new growth")]
        [Units("kg/ha")]
        public double ActualGrowthN
        {
            get { return dNewGrowthN; }
        }

        /// <summary>Gets the amount of plant available NH4-N in the soil (kg/ha).</summary>
        [Description("Amount of NH4 N available in the soil")]
        [Units("kg/ha")]
        public double[] SoilNH4Available
        {
            get { return mySoilNH4Available; }
        }

        /// <summary>Gets the amount of plant available NO3-N in the soil (kg/ha).</summary>
        [Description("Amount of NO3 N available in the soil")]
        [Units("kg/ha")]
        public double[] SoilNO3Available
        {
            get { return mySoilNO3Available; }
        }

        /// <summary>Gets the amount of NH4-N taken up from the soil (kg/ha).</summary>
        [Description("Amount of NH4 N uptake")]
        [Units("kg/ha")]
        public double[] SoilNH4Uptake
        {
            get { return mySoilNH4Uptake; }
        }

        /// <summary>Gets the amount of NO3-N taken up from the soil (kg/ha).</summary>
        [Description("Amount of NO3 N uptake")]
        [Units("kg/ha")]
        public double[] SoilNO3Uptake
        {
            get { return mySoilNO3Uptake; }
        }

        #endregion  --------------------------------------------------------------------------------------------------------

        #region - Turnover rates and DM allocation  ------------------------------------------------------------------------

        /// <summary>Gets the turnover rate for live shoot DM (0-1).</summary>
        [Description("Turnover rate for live DM (leaves and stem)")]
        [Units("0-1")]
        public double LiveDMTurnoverRate
        {
            get { return gama; }
        }

        /// <summary>Gets the turnover rate for dead shoot DM (0-1).</summary>
        [Description("Turnover rate for dead DM (leaves and stem)")]
        [Units("0-1")]
        public double DeadDMTurnoverRate
        {
            get { return gamaD; }
        }

        /// <summary>Gets the turnover rate for live DM in stolons (0-1).</summary>
        [Description("DM turnover rate for stolons")]
        [Units("0-1")]
        public double StolonDMTurnoverRate
        {
            get { return gamaS; }
        }

        /// <summary>Gets the turnover rate for live DM in roots (0-1).</summary>
        [Description("DM turnover rate for roots")]
        [Units("0-1")]
        public double RootDMTurnoverRate
        {
            get { return gamaR; }
        }

        /// <summary>Gets the fraction of new growth allocated to shoot (0-1).</summary>
        [Description("Fraction of DM allocated to shoot")]
        [Units("0-1")]
        public double ShootDMAllocation
        {
            get { return fractionToShoot; }
        }

        /// <summary>Gets the fraction of new growth allocated to roots (0-1).</summary>
        [Description("Fraction of DM allocated to roots")]
        [Units("0-1")]
        public double RootDMAllocation
        {
            get { return 1 - fractionToShoot; }
        }

        /// <summary>Gets the fraction of new shoot growth allocated to leaves (0-1).</summary>
        [Description("Fraction of new shoot DM allocated to leaves")]
        [Units("0-1")]
        public double LeafDMAllocation
        {
            get { return fractionToLeaf; }
        }

        #endregion  --------------------------------------------------------------------------------------------------------

        #region - LAI and cover  -------------------------------------------------------------------------------------------

        /// <summary>Gets the plant's green LAI (leaf area index) (-).</summary>
        [Description("Leaf area index of green tissues")]
        [Units("m^2/m^2")]
        public double LAIGreen
        {
            get { return greenLAI; }
        }

        /// <summary>Gets the plant's dead LAI (leaf area index) (-).</summary>
        [Description("Leaf area index of dead tissues")]
        [Units("m^2/m^2")]
        public double LAIDead
        {
            get { return deadLAI; }
        }

        /// <summary>Gets the irradiance on top of canopy (W.m^2/m^2).</summary>
        [Description("Irradiance on the top of canopy")]
        [Units("W.m^2/m^2")]
        public double IrradianceTopCanopy
        {
            get { return irradianceTopOfCanopy; }
        }

        /// <summary>Gets the plant's cover of dead material (0-1).</summary>
        [Description("Fraction of soil covered by dead tissues")]
        [Units("0-1")]
        public double CoverDead
        {
            get { return CalcPlantCover(deadLAI); }
        }

        #endregion  --------------------------------------------------------------------------------------------------------

        #region - Root depth and distribution  -----------------------------------------------------------------------------

        /// <summary>Gets the root depth (mm).</summary>
        [Description("Depth of roots")]
        [Units("mm")]
        public double RootDepth
        {
            get { return roots.Depth; }
        }

        /// <summary>Gets the root frontier (layer at bottom of root zone).</summary>
        [Description("Layer at bottom of root zone")]
        [Units("-")]
        public int RootFrontier
        {
            get { return roots.BottomLayer; }
        }

        /// <summary>Gets the fraction of root dry matter for each soil layer (0-1).</summary>
        [Description("Fraction of root dry matter for each soil layer")]
        [Units("0-1")]
        public double[] RootWtFraction
        {
            get { return roots.Tissue[0].FractionWt; }
        }

        /// <summary>Gets the plant's root length density for each soil layer (mm/mm^3).</summary>
        [Description("Root length density")]
        [Units("mm/mm^3")]
        public double[] RLD
        {
            get
            {
                double[] result = new double[nLayers];
                double totalRootLength = roots.Tissue[0].DM * mySpecificRootLength; // m root/m2 
                totalRootLength *= 0.0000001; // convert into mm root/mm2 soil)
                for (int layer = 0; layer < result.Length; layer++)
                {
                    result[layer] = roots.Tissue[0].FractionWt[layer] * totalRootLength / mySoil.Thickness[layer];
                }
                return result;
            }
        }

        #endregion  --------------------------------------------------------------------------------------------------------

        #region - Water amounts  -------------------------------------------------------------------------------------------

        /// <summary>Gets the lower limit of soil water content for plant uptake (mm^3/mm^3).</summary>
        [Description("Soil water content at lower limit for plant uptake")]
        [Units("mm^3/mm^3")]
        public double[] LL
        {
            get
            {
                SoilCrop soilInfo = (SoilCrop)mySoil.Crop(Name);
                return soilInfo.LL;
            }
        }

        /// <summary>Gets the amount of water demanded by the plant (mm).</summary>
        [Description("Plant water demand")]
        [Units("mm")]
        public double WaterDemand
        {
            get { return myWaterDemand; }
        }

        /// <summary>Gets the amount of soil water available for plant uptake (mm).</summary>
        [Description("Plant soil available water")]
        [Units("mm")]
        public double[] SoilAvailableWater
        {
            get { return mySoilWaterAvailable; }
        }

        /// <summary>Gets the amount of water taken up by the plant (mm).</summary>
        [Description("Plant water uptake")]
        [Units("mm")]
        public double[] WaterUptake
        {
            get { return mySoilWaterUptake; }
        }

        #endregion  --------------------------------------------------------------------------------------------------------

        #region - Growth limiting factors  ---------------------------------------------------------------------------------

        /// <summary>Gets the growth factor due to variations in intercepted radiation (0-1).</summary>
        [Description("Growth factor due to variations in intercepted radiation")]
        [Units("0-1")]
        public double GlfRadnIntercept
        {
            get { return glfRadn; }
        }

        /// <summary>Gets the growth limiting factor due to variations in atmospheric CO2 (0-1).</summary>
        [Description("Growth limiting factor due to variations in atmospheric CO2")]
        [Units("0-1")]
        public double GlfCO2
        {
            get { return glfCO2; }
        }

        /// <summary>Gets the growth limiting factor due to variations in plant N concentration (0-1).</summary>
        [Description("Growth limiting factor due to variations in plant N concentration")]
        [Units("0-1")]
        public double GlfNContent
        {
            get { return glfNc; }
        }

        /// <summary>Gets the growth limiting factor due to variations in air temperature (0-1).</summary>
        [Description("Growth limiting factor due to variations in air temperature")]
        [Units("0-1")]
        public double GlfTemperature
        {
            get { return glfTemp; }
        }

        /// <summary>Gets the growth limiting factor due to heat stress (0-1).</summary>
        [Description("Growth limiting factor due to heat stress")]
        [Units("0-1")]
        public double GlfHeatDamage
        {
            get { return glfHeat; }
        }

        /// <summary>Gets the growth limiting factor due to cold stress (0-1).</summary>
        [Description("Growth limiting factor due to cold stress")]
        [Units("0-1")]
        public double GlfColdDamage
        {
            get { return glfCold; }
        }

        /// <summary>Gets the growth limiting factor due to water deficit (0-1).</summary>
        [Description("Growth limiting factor due to water deficit")]
        [Units("0-1")]
        public double GlfWaterSupply
        {
            get { return glfWater; }
        }

        /// <summary>Gets the growth limiting factor due to water logging (0-1).</summary>
        [Description("Growth limiting factor due to water logging")]
        [Units("0-1")]
        public double GlfWaterLogging
        {
            get { return glfAeration; }
        }

        /// <summary>Gets the growth limiting factor due to soil N availability (0-1).</summary>
        [Description("Growth limiting factor due to soil N availability")]
        [Units("0-1")]
        public double GlfNSupply
        {
            get { return glfN; }
        }

        // TODO: verify that this is really needed
        /// <summary>Gets the vapour pressure deficit factor (0-1).</summary>
        [Description("Effect of vapour pressure on growth (used by micromet)")]
        [Units("0-1")]
        public double FVPD
        {
            get { return FVPDFunction.Value(VPD()); }
        }

        #endregion  --------------------------------------------------------------------------------------------------------

        #region - Harvest variables  ---------------------------------------------------------------------------------------

        /// <summary>Gets the amount of dry matter available for harvest (kg/ha).</summary>
        /// <value>The harvestable DM weight.</value>
        [Description("Amount of dry matter harvestable")]
        [Units("kg/ha")]
        public double HarvestableWt
        {
            get
            {
                return leaves.DMLiveHarvestable + leaves.DMDeadHarvestable
                       + stems.DMLiveHarvestable + stems.DMDeadHarvestable
                       + stolons.DMLiveHarvestable + stolons.DMDeadHarvestable;
            }
        }

        /// <summary>Gets the amount of dry matter harvested (kg/ha).</summary>
        [Description("Amount of plant dry matter removed by harvest")]
        [Units("kg/ha")]
        public double HarvestedWt
        {
            get { return defoliatedDM; }
        }

        /// <summary>Gets the fraction of the plant that was harvested (0-1).</summary>
        [Description("Fraction harvested")]
        [Units("0-1")]
        public double HarvestedFraction
        {
            get { return defoliatedFraction; }
        }

        /// <summary>Gets the amount of plant N removed by harvest (kg/ha).</summary>
        [Description("Amount of plant nitrogen removed by harvest")]
        [Units("kg/ha")]
        public double HarvestedN
        {
            get { return defoliatedN; }
        }

        /// <summary>Gets the average N concentration in harvested DM (kg/kg).</summary>
        [Description("average average N concentration of harvested material")]
        [Units("kg/kg")]
        public double HarvestedNconc
        {
            get { return MathUtilities.Divide(HarvestedN, HarvestedWt, 0.0); }
        }

        /// <summary>Gets the average digestibility of harvested material (0-1).</summary>
        [Description("Average digestibility of harvested meterial")]
        [Units("0-1")]
        public double HarvestedDigestibility
        {
            get { return defoliatedDigestibility; }
        }

        /// <summary>Gets the average ME (metabolisable energy) of harvested DM (MJ/ha).</summary>
        [Description("Average ME of harvested material")]
        [Units("(MJ/ha)")]
        public double HarvestedME
        {
            get { return 16 * defoliatedDigestibility * HarvestedWt; }
        }

        /// <summary>Gets the average herbage digestibility (0-1).</summary>
        [Description("Average digestibility of standing herbage")]
        [Units("0-1")]
        public double HerbageDigestibility
        {
            get { return herbageDigestibility; }
        }

        /// <summary>Gets the average herbage ME, metabolisable energy (MJ/ha).</summary>
        [Description("Average ME of standing herbage")]
        [Units("(MJ/ha)")]
        public double HerbageME
        {
            get { return 16 * herbageDigestibility * AboveGroundWt; }
        }

        #endregion  --------------------------------------------------------------------------------------------------------

        #endregion  --------------------------------------------------------------------------------------------------------

        #region Initialisation methods  ------------------------------------------------------------------------------------

        /// <summary>Performs the initialisation procedures for this species (set DM, N, LAI, etc).</summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data</param>
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

            // initialise parameters for biomass removal
            InitBiomassRemovals();

            // check whether there is a resouce arbitrator, it will control the uptake
            if (apsimArbitrator != null)
            {
                MyWaterUptakeSource = "Arbitrator";
                MyNitrogenUptakeSource = "Arbitrator";
            }

            // check whether there is a resouce arbitrator, it will control the uptake
            if (soilArbitrator != null)
            {
                MyWaterUptakeSource = "SoilArbitrator";
                MyNitrogenUptakeSource = "SoilArbitrator";
            }
        }

        /// <summary>Initialises arrays to same length as soil layers.</summary>
        private void InitiliaseSoilArrays()
        {
            mySoilWaterAvailable = new double[nLayers];
            mySoilWaterUptake = new double[nLayers];
            mySoilNH4Available = new double[nLayers];
            mySoilNO3Available = new double[nLayers];
            mySoilNH4Uptake = new double[nLayers];
            mySoilNO3Uptake = new double[nLayers];
        }

        /// <summary>Initialises, checks, and saves the varibles representing the initial plant state.</summary>
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
            leaves.NConcOptimum = myNThresholdsForLeaves[0];
            leaves.NConcMinimum = myNThresholdsForLeaves[1];
            leaves.NConcMaximum = myNThresholdsForLeaves[2];

            stems.NConcOptimum = myNThresholdsForStems[0];
            stems.NConcMinimum = myNThresholdsForStems[1];
            stems.NConcMaximum = myNThresholdsForStems[2];

            stolons.NConcOptimum = myNThresholdsForStolons[0];
            stolons.NConcMinimum = myNThresholdsForStolons[1];
            stolons.NConcMaximum = myNThresholdsForStolons[2];

            roots.NConcOptimum = myNThresholdsForRoots[0];
            roots.NConcMinimum = myNThresholdsForRoots[1];
            roots.NConcMaximum = myNThresholdsForRoots[2];

            // 3. Save initial state (may be used later for reset)
            InitialState = new SpeciesBasicStateSettings();
            if (myInitialShootDM > Epsilon)
            {
                // DM is positive, plant is on the ground and able to grow straightaway
                InitialState.PhenoStage = 1;
                for (int pool = 0; pool < 11; pool++)
                    InitialState.DMWeight[pool] = initialDMFractions[pool] * myInitialShootDM;
                InitialState.DMWeight[11] = myInitialRootDM;
                InitialState.RootDepth = myInitialRootDepth;
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
            else if (myInitialShootDM > -Epsilon)
            {
                // DM is zero, plant has just sown and is able to germinate
                InitialState.PhenoStage = 0;
            }
            else
            {
                //DM is negative, plant is not yet in the ground 
                InitialState.PhenoStage = -1;
            }

            // 4. Set the minimum green DM, and stolon standing
            leaves.MinimumLiveDM = myMinimumGreenWt * MinimumGreenLeafProp;
            stems.MinimumLiveDM = myMinimumGreenWt * (1.0 - MinimumGreenLeafProp);
            stolons.MinimumLiveDM = 0.0;
            roots.MinimumLiveDM = myMinimumGreenWt * MinimumGreenRootProp;
            stolons.FractionStanding = myFractionStolonStanding;

            // 5. Set remobilisation rate for luxury N in each tissue
            roots.Tissue[0].FractionNLuxuryRemobilisable = myFractionNLuxuryRemobilisable[0];
            for (int tissue = 0; tissue < 3; tissue++)
            {
                leaves.Tissue[tissue].FractionNLuxuryRemobilisable = myFractionNLuxuryRemobilisable[tissue];
                stems.Tissue[tissue].FractionNLuxuryRemobilisable = myFractionNLuxuryRemobilisable[tissue];
                stolons.Tissue[tissue].FractionNLuxuryRemobilisable = myFractionNLuxuryRemobilisable[tissue];
            }

            // 6. Set the digestibility parameters for each tissue
            for (int tissue = 0; tissue < 4; tissue++)
            {
                leaves.Tissue[tissue].DigestibilityCellWall = myDigestibilitiesCellWall[tissue];
                leaves.Tissue[tissue].DigestibilityProtein = DigestibilitiesProtein;

                stems.Tissue[tissue].DigestibilityCellWall = myDigestibilitiesCellWall[tissue];
                stems.Tissue[tissue].DigestibilityProtein = DigestibilitiesProtein;

                stolons.Tissue[tissue].DigestibilityCellWall = myDigestibilitiesCellWall[tissue];
                stolons.Tissue[tissue].DigestibilityProtein = DigestibilitiesProtein;
            }

            leaves.Tissue[0].FractionSugarNewGrowth = SugarFractionNewGrowth;
            stems.Tissue[0].FractionSugarNewGrowth = SugarFractionNewGrowth;
            stolons.Tissue[0].FractionSugarNewGrowth = SugarFractionNewGrowth;
            //NOTE: roots are not considered for digestibility
        }

        /// <summary>
        /// Sets the initial parameters for this plant, including DM and N content of various pools plus plant height and root depth.
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

        /// <summary>Set the plant state at germination.</summary>
        internal void SetEmergenceState()
        {
            // 1. Set the above ground DM, equals MinimumGreenWt
            leaves.Tissue[0].DM = myMinimumGreenWt * EmergenceDMFractions[0];
            leaves.Tissue[1].DM = myMinimumGreenWt * EmergenceDMFractions[1];
            leaves.Tissue[2].DM = myMinimumGreenWt * EmergenceDMFractions[2];
            leaves.Tissue[3].DM = myMinimumGreenWt * EmergenceDMFractions[3];
            stems.Tissue[0].DM = myMinimumGreenWt * EmergenceDMFractions[4];
            stems.Tissue[1].DM = myMinimumGreenWt * EmergenceDMFractions[5];
            stems.Tissue[2].DM = myMinimumGreenWt * EmergenceDMFractions[6];
            stems.Tissue[3].DM = myMinimumGreenWt * EmergenceDMFractions[7];
            stolons.Tissue[0].DM = myMinimumGreenWt * EmergenceDMFractions[8];
            stolons.Tissue[1].DM = myMinimumGreenWt * EmergenceDMFractions[9];
            stolons.Tissue[2].DM = myMinimumGreenWt * EmergenceDMFractions[10];

            // 2. Set root depth and DM (root DM equals shoot)
            roots.Depth = myMinimumRootDepth;
            roots.BottomLayer = RootZoneBottomLayer();
            double[] rootFractions = CurrentRootDistributionTarget();
            for (int layer = 0; layer < nLayers; layer++)
                roots.Tissue[0].DMLayer[layer] = roots.MinimumLiveDM * rootFractions[layer];

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

        /// <summary>Initialises the parameters to compute factor increasing shoot allocation during reproductive growth.</summary>
        /// <remarks>
        /// Reproductive phase of perennial is not simulated by the model, the ReproductiveGrowthFactor attempts to mimic the main
        ///  effect, which is a higher allocation of DM to shoot during this period. The beginning and length of the reproductive
        ///  phase is computed as function of latitude (it occurs later in spring and is shorter the further the location is from
        ///  the equator). The extent at which allocation to shoot increases is also a function of latitude, maximum allocation is
        ///  greater for higher latitudes. Shoulder periods occur before and after the main phase, in these allocation transictions
        ///  between default allocation and that of the main phase.
        /// </remarks>
        private void InitReproductiveGrowthFactor()
        {
            reproSeasonInterval = new double[3];

            // compute the day to start the main phase (period with maximum DM allocation to shoot)
            double doyWinterSolstice = (myMetData.Latitude < 0.0) ? 172 : 355;
            double reproAux = Math.Exp(-myReproSeasonTimingCoeff * (Math.Abs(myMetData.Latitude) - myReproSeasonReferenceLatitude));
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
            reproAux = Math.Exp(-myReproSeasonAllocationCoeff * (Math.Abs(myMetData.Latitude) - myReproSeasonReferenceLatitude));
            allocationIncreaseRepro = myReproSeasonMaxAllocationIncrease / (1.0 + reproAux);
        }

        /// <summary>Initialises the default biomass removal fractions</summary>
        private void InitBiomassRemovals()
        {
            // leaves, harvest
            OrganBiomassRemovalType removalFractions = new OrganBiomassRemovalType();
            removalFractions.FractionLiveToRemove = 0.5;
            removalFractions.FractionDeadToRemove = 0.5;
            removalFractions.FractionLiveToResidue = 0.0;
            removalFractions.FractionDeadToResidue = 0.0;
            leaves.SetRemovalFractions("Harvest", removalFractions);
            // graze
            removalFractions.FractionLiveToRemove = 0.5;
            removalFractions.FractionDeadToRemove = 0.5;
            removalFractions.FractionLiveToResidue = 0.0;
            removalFractions.FractionDeadToResidue = 0.0;
            leaves.SetRemovalFractions("Graze", removalFractions);
            // Cut
            removalFractions.FractionLiveToRemove = 0.5;
            removalFractions.FractionDeadToRemove = 0.5;
            removalFractions.FractionLiveToResidue = 0.0;
            removalFractions.FractionDeadToResidue = 0.0;
            leaves.SetRemovalFractions("Cut", removalFractions);

            // stems, harvest
            removalFractions.FractionLiveToRemove = 0.5;
            removalFractions.FractionDeadToRemove = 0.5;
            removalFractions.FractionLiveToResidue = 0.0;
            removalFractions.FractionDeadToResidue = 0.0;
            stems.SetRemovalFractions("Harvest", removalFractions);
            // graze
            removalFractions.FractionLiveToRemove = 0.5;
            removalFractions.FractionDeadToRemove = 0.5;
            removalFractions.FractionLiveToResidue = 0.0;
            removalFractions.FractionDeadToResidue = 0.0;
            stems.SetRemovalFractions("Graze", removalFractions);
            // Cut
            removalFractions.FractionLiveToRemove = 0.5;
            removalFractions.FractionDeadToRemove = 0.5;
            removalFractions.FractionLiveToResidue = 0.0;
            removalFractions.FractionDeadToResidue = 0.0;
            stems.SetRemovalFractions("Cut", removalFractions);

            // Stolons, harvest
            removalFractions.FractionLiveToRemove = 0.5;
            removalFractions.FractionDeadToRemove = 0.0;
            removalFractions.FractionLiveToResidue = 0.0;
            removalFractions.FractionDeadToResidue = 0.0;
            stolons.SetRemovalFractions("Harvest", removalFractions);
            // graze
            removalFractions.FractionLiveToRemove = 0.5;
            removalFractions.FractionDeadToRemove = 0.0;
            removalFractions.FractionLiveToResidue = 0.0;
            removalFractions.FractionDeadToResidue = 0.0;
            stolons.SetRemovalFractions("Graze", removalFractions);
            // Cut
            removalFractions.FractionLiveToRemove = 0.5;
            removalFractions.FractionDeadToRemove = 0.0;
            removalFractions.FractionLiveToResidue = 0.0;
            removalFractions.FractionDeadToResidue = 0.0;
            stolons.SetRemovalFractions("Cut", removalFractions);
        }

        /// <summary>Initialises the variables in canopy properties.</summary>
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

        /// <summary>Initialises the variables in root properties</summary>
        private void InitialiseRootsProperties()
        {
            // Used in Val's Arbitrator (via ICrop2)
            SoilCrop soilCrop = this.mySoil.Crop(Name) as SoilCrop;

            myRootProperties.RootDepth = roots.Depth;
            myRootProperties.KL = soilCrop.KL;
            myRootProperties.MinNO3ConcForUptake = new double[nLayers];
            myRootProperties.MinNH4ConcForUptake = new double[nLayers];
            myRootProperties.KNH4 = KNH4; //TODO: check these coefficients
            myRootProperties.KNO3 = KNO3;
            myRootProperties.LowerLimitDep = new double[nLayers];
            myRootProperties.UptakePreferenceByLayer = new double[nLayers];
            myRootProperties.RootExplorationByLayer = new double[nLayers];
            for (int layer = 0; layer < nLayers; layer++)
            {
                myRootProperties.LowerLimitDep[layer] = soilCrop.LL[layer] * mySoil.Thickness[layer];
                myRootProperties.MinNH4ConcForUptake[layer] = 0.0;
                myRootProperties.MinNO3ConcForUptake[layer] = 0.0;
                myRootProperties.UptakePreferenceByLayer[layer] = 1.0;
                myRootProperties.RootExplorationByLayer[layer] = FractionLayerWithRoots(layer);
            }
            myRootProperties.RootLengthDensityByVolume = RLD;
        }

        #endregion  --------------------------------------------------------------------------------------------------------

        #region Daily processes  -------------------------------------------------------------------------------------------

        /// <summary>EventHandler - preparation before the main daily processes.</summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data</param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            // 1. Zero out several variables
            RefreshVariables();
        }

        /// <summary>Performs the calculations for potential growth.</summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data</param>
        [EventSubscribe("DoPotentialPlantGrowth")]
        private void OnDoPotentialPlantGrowth(object sender, EventArgs e)
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
                    // Evaluate tissue turnover and get remobilisation (C and N)
                    EvaluateTissueTurnoverRates();

                    // Get the potential gross growth
                    CalcDailyPotentialGrowth();

                    // Evaluate potential allocation of today's growth
                    EvaluateGrowthAllocation();

                    // Evaluate the water supply, demand & uptake
                    DoWaterCalculations();

                    // Get the potential growth after water limitations
                    CalcGrowthAfterWaterLimitations();

                    // Get the N amount demanded for optimum growth and luxury uptake
                    EvaluateNitrogenDemand();
                }
            }
            //else { // Growth is controlled by Sward (all species) }
        }

        /// <summary>Performs the calculations for actual growth.</summary>
        /// <param name="sender">The sender</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data</param>
        [EventSubscribe("DoPlantGrowth")]
        private void OnDoPlantGrowth(object sender, EventArgs e)
        {
            if (isAlive && !isSwardControlled)
            {
                if (phenologicStage > 0)
                {
                    // Evaluate the nitrogen soil demand, supply, and uptake
                    DoNitrogenCalculations();

                    // Get the actual growth, after nutrient limitations but before senescence
                    CalcGrowthAfterNutrientLimitations();

                    // Evaluate actual allocation of today's growth
                    EvaluateNewGrowthAllocation();

                    // Get the effective growth, after all limitations and senescence
                    DoEffectiveGrowth();

                    // Send detached material to other modules (litter to surfacesOM, roots to soilFOM) 
                    DoSurfaceOMReturn(detachedShootDM, detachedShootN);
                    DoIncorpFomEvent(detachedRootDM, detachedRootN);
                }
            }
            //else { // Growth is controlled by Sward (all species) }
        }

        #region - Plant growth processes  ----------------------------------------------------------------------------------

        /// <summary>Evaluates the phenologic stage of annual plants.</summary>
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
                growingGDD += Math.Max(0.0, Tmean(0.5) - myGrowthTminimum);

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

                    phenoFactor1 = MathUtilities.Divide(daysSinceEmergence - daysEmergenceToAnthesis, daysAnthesisToMaturity, 1.0);
                    phenoFactor2 = MathUtilities.Divide(growingGDD - degreesDayForAnthesis, degreesDayForMaturity, 1.0);
                }

                // set the phenologic factor (fraction of current phase)
                phenoFactor = Math.Max(phenoFactor1, phenoFactor2);
            }
        }

        /// <summary>Computes the daily progress through germination.</summary>
        /// <returns>Fraction of germination phase completed (0-1)</returns>
        internal double DailyGerminationProgress()
        {
            germinationGDD += Math.Max(0.0, Tmean(0.5) - myGrowthTminimum);
            return MathUtilities.Divide(germinationGDD, DegreesDayForGermination, 1.0);
        }

        /// <summary>Calculates the potential plant growth.</summary>
        internal void CalcDailyPotentialGrowth()
        {
            // Get today's gross potential photosynthetic rate (kgC/ha/day)
            grossPhotosynthesis = DailyPotentialPhotosynthesis();

            // Get respiration rates (kgC/ha/day)
            respirationMaintenance = DailyMaintenanceRespiration();
            respirationGrowth = DailyGrowthRespiration();

            // Get C remobilisation (kgC/ha/day) (got from tissue turnover) - TODO: implement C remobilisation
            remobilisedC = remobilisableC;

            // Net potential growth (kg/ha/day)
            dGrowthPot = Math.Max(0.0, grossPhotosynthesis - respirationGrowth + remobilisedC - respirationMaintenance);
            dGrowthPot /= CarbonFractionInDM;
        }

        /// <summary>Calculates the growth after water limitations.</summary>
        internal void CalcGrowthAfterWaterLimitations()
        {
            // get the limitation factor due to water deficiency (drought)
            glfWater = WaterDeficitFactor();

            // get the limitation factor due to water logging (lack of aeration)
            glfAeration = WaterLoggingFactor();

            // adjust today's growth for limitations related to soil water
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
            // get total N to allocate in new growth
            dNewGrowthN = fixedN + senescedNRemobilised + UptakeN + luxuryNRemobilised;

            // get the limitation factor due to soil N deficiency
            double glfNit = 1.0;
            if (dNewGrowthN > Epsilon)
            {
                glfN = Math.Min(1.0, Math.Max(0.0, MathUtilities.Divide(dNewGrowthN, demandOptimumN, 1.0)));

                // adjust the glfN
                glfNit = Math.Pow(glfN, myNDillutionCoefficient);
            }
            else
                glfN = 1.0;

            // adjust today's growth for limitations related to soil nutrient supply
            dGrowthActual = dGrowthWstress * Math.Min(glfNit, myGlfSFertility);
        }

        /// <summary>Calculates the plant effective growth and update DM, N, LAI and digestibility.</summary>
        internal void DoEffectiveGrowth()
        {
            // Effective, or net, growth
            dGrowthEff = (dGrowthShootDM - detachedShootDM) + (dGrowthRootDM - detachedRootDM);

            // Save some variables for mass balance check
            double preTotalWt = TotalWt;
            double preTotalN = TotalN;

            // Update each organ, returns test for mass balance
            if (leaves.DoOrganUpdate() == false)
                throw new ApsimXException(this, "Growth and tissue turnover resulted in loss of mass balance for leaves");

            if (stems.DoOrganUpdate() == false)
                throw new ApsimXException(this, "Growth and tissue turnover resulted in loss of mass balance for stems");

            if (stolons.DoOrganUpdate() == false)
                throw new Exception("Growth and tissue turnover resulted in loss of mass balance for stolons");

            if (roots.DoOrganUpdate() == false)
                throw new ApsimXException(this, "Growth and tissue turnover resulted in loss of mass balance for roots");

            // Check for loss of mass balance of total plant
            if (Math.Abs(preTotalWt + dGrowthActual - detachedShootDM - detachedRootDM - TotalWt) > Epsilon)
                throw new ApsimXException(this, "  " + Name + " - Growth and tissue turnover resulted in loss of mass balance");

            if (Math.Abs(preTotalN + dNewGrowthN - senescedNRemobilised - luxuryNRemobilised - detachedShootN - detachedRootN - TotalN) > Epsilon)
                throw new ApsimXException(this, "  " + Name + " - Growth and tissue turnover resulted in loss of mass balance");

            // Update LAI
            EvaluateLAI();

            // Update digestibility
            EvaluateDigestibility();
        }

        /// <summary>Computes the plant's gross potential growth rate.</summary>
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
            //   at dawn and dusk (first and last quarters of the day)
            double Pmax1 = myReferencePhotosynthesisRate * tempGlf1 * glfCO2 * glfNc;
            //   at bright light (half of sunlight length, middle of day)
            double Pmax2 = myReferencePhotosynthesisRate * tempGlf2 * glfCO2 * glfNc;

            // Day light length, converted to seconds
            double myDayLength = 3600 * myMetData.CalculateDayLength(-6);

            // Photosynthetically active radiation, converted from MJ/m2.day to J/m2.s
            double interceptedPAR = fractionPAR * InterceptedRadn * 1000000.0 / myDayLength;

            // Irradiance at top of canopy in the middle of the day (J/m2 leaf/s)
            irradianceTopOfCanopy = interceptedPAR * myLightExtentionCoefficient * (4.0 / 3.0);

            //Photosynthesis per leaf area under full irradiance at the top of the canopy (mg CO2/m^2 leaf/s)
            double Pl1 = SingleLeafPhotosynthesis(0.5 * irradianceTopOfCanopy, Pmax1);
            double Pl2 = SingleLeafPhotosynthesis(irradianceTopOfCanopy, Pmax2);

            // Photosynthesis per leaf area for the day (mg CO2/m^2 leaf/day)
            double Pl_Daily = myDayLength * (Pl1 + Pl2) * 0.5;

            // Radiation effects (for reporting purposes only)
            glfRadn = MathUtilities.Divide((0.25 * Pl1) + (0.75 * Pl2), (0.25 * Pmax1) + (0.75 * Pmax2), 1.0);

            // Photosynthesis for whole canopy, per ground area (mg CO2/m^2/day)
            double Pc_Daily = Pl_Daily * CoverGreen / myLightExtentionCoefficient;

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
            return GrossPhotosynthesis * Math.Min(glfHeat, glfCold) * myGlfGeneric;
        }

        /// <summary>Compute the photosynthetic rate for a single leaf.</summary>
        /// <param name="IL">Instantaneous intercepted radiation (J/m2 leaf/s)</param>
        /// <param name="Pmax">Max photosynthetic rate (mg CO2/m^2 leaf/s)</param>
        /// <returns>the photosynthetic rate (mgCO2/m^2 leaf/s)</returns>
        private double SingleLeafPhotosynthesis(double IL, double Pmax)
        {
            double photoAux1 = PhotosyntheticEfficiency * IL + Pmax;
            double photoAux2 = 4 * PhotosynthesisCurveFactor * PhotosyntheticEfficiency * IL * Pmax;
            double Pl = (0.5 / PhotosynthesisCurveFactor) * (photoAux1 - Math.Sqrt(Math.Pow(photoAux1, 2) - photoAux2));
            return Pl;
        }

        /// <summary>Computes the plant's loss of C due to maintenance respiration.</summary>
        /// <returns>The amount of C lost to atmosphere (kgC/ha)</returns>
        private double DailyMaintenanceRespiration()
        {
            // Temperature effects on respiration
            double Teffect = TemperatureEffectOnRespiration(Tmean(0.5));

            // Total DM converted to C (kg/ha)
            double liveBiomassC = (AboveGroundLiveWt + roots.DMLive) * CarbonFractionInDM;
            double result = liveBiomassC * myMaintenanceRespirationCoefficient * Teffect * glfNc;
            return Math.Max(0.0, result);
        }

        /// <summary>Computes the plant's loss of C due to growth respiration.</summary>
        /// <returns>The amount of C lost to atmosphere (kgC/ha)</returns>
        private double DailyGrowthRespiration()
        {
            return grossPhotosynthesis * myGrowthRespirationCoefficient;
        }

        /// <summary>Computes the allocation of new growth to all tissues in each organ.</summary>
        internal void EvaluateNewGrowthAllocation()
        {
            if (dGrowthActual > Epsilon)
            {
                // Get the actual growth above and below ground
                dGrowthShootDM = dGrowthActual * fractionToShoot;
                dGrowthRootDM = Math.Max(0.0, dGrowthActual - dGrowthShootDM);

                // Get the fractions of new growth to allocate to each plant organ
                double toLeaf = fractionToShoot * fractionToLeaf;
                double toStem = fractionToShoot * (1.0 - myFractionToStolon - fractionToLeaf);
                double toStolon = fractionToShoot * myFractionToStolon;
                double toRoot = 1.0 - fractionToShoot;

                // Allocate new DM growth to the growing tissues
                leaves.Tissue[0].DMTransferedIn += toLeaf * dGrowthActual;
                stems.Tissue[0].DMTransferedIn += toStem * dGrowthActual;
                stolons.Tissue[0].DMTransferedIn += toStolon * dGrowthActual;
                roots.Tissue[0].DMTransferedIn += toRoot * dGrowthActual;

                // Evaluate allocation of N
                if (dNewGrowthN > demandOptimumN)
                {
                    // There is more N than needed for basic demand (i.e. there is luxury uptake)
                    // 1. Allocate optimum N
                    leaves.Tissue[0].NTransferedIn = leaves.Tissue[0].DMTransferedIn * leaves.NConcOptimum;
                    stems.Tissue[0].NTransferedIn = stems.Tissue[0].DMTransferedIn * stems.NConcOptimum;
                    stolons.Tissue[0].NTransferedIn = stolons.Tissue[0].DMTransferedIn * stolons.NConcOptimum;
                    roots.Tissue[0].NTransferedIn = roots.Tissue[0].DMTransferedIn * roots.NConcOptimum;

                    double NAllocated = leaves.Tissue[0].NTransferedIn + stems.Tissue[0].NTransferedIn
                                      + stolons.Tissue[0].NTransferedIn + roots.Tissue[0].NTransferedIn;
                    double NtoAllocate = Math.Max(0.0, dNewGrowthN - NAllocated);

                    // Allocate remaining as luxury N (based on relative luxury demand)
                    double Nsum = Math.Max(0.0, (toLeaf * (leaves.NConcMaximum - leaves.NConcOptimum))
                                              + (toStem * (stems.NConcMaximum - stems.NConcOptimum))
                                              + (toStolon * (stolons.NConcMaximum - stolons.NConcOptimum))
                                              + (toRoot * (roots.NConcMaximum - roots.NConcOptimum)));
                    if (Nsum > Epsilon)
                    {
                        leaves.Tissue[0].NTransferedIn += NtoAllocate * toLeaf * (leaves.NConcMaximum - leaves.NConcOptimum) / Nsum;
                        stems.Tissue[0].NTransferedIn += NtoAllocate * toStem * (stems.NConcMaximum - stems.NConcOptimum) / Nsum;
                        stolons.Tissue[0].NTransferedIn += NtoAllocate * toStolon * (stolons.NConcMaximum - stolons.NConcOptimum) / Nsum;
                        roots.Tissue[0].NTransferedIn += NtoAllocate * toRoot * (roots.NConcMaximum - roots.NConcOptimum) / Nsum;
                    }
                    else
                    {
                        // something went horribly wrong to get here
                        throw new Exception("Allocation of new growth could not be completed");
                    }
                }
                else
                {
                    // Available N was not enough to meet basic demand, allocate N taken up (based on optimum N content)
                    double Nsum = (toLeaf * leaves.NConcOptimum) + (toStem * stems.NConcOptimum)
                                + (toStolon * stolons.NConcOptimum) + (toRoot * roots.NConcOptimum);
                    if (Nsum > Epsilon)
                    {
                        leaves.Tissue[0].NTransferedIn += dNewGrowthN * toLeaf * leaves.NConcOptimum / Nsum;
                        stems.Tissue[0].NTransferedIn += dNewGrowthN * toStem * stems.NConcOptimum / Nsum;
                        stolons.Tissue[0].NTransferedIn += dNewGrowthN * toStolon * stolons.NConcOptimum / Nsum;
                        roots.Tissue[0].NTransferedIn += dNewGrowthN * toRoot * roots.NConcOptimum / Nsum;
                    }
                    else
                    {
                        // something went horribly wrong to get here
                        throw new Exception("Allocation of new growth could not be completed");
                    }
                }

                // Update N variables
                dGrowthShootN = leaves.Tissue[0].NTransferedIn + stems.Tissue[0].NTransferedIn + stolons.Tissue[0].NTransferedIn;
                dGrowthRootN = roots.Tissue[0].NTransferedIn;

                // Evaluate root elongation and allocate new growth in each layer
                EvaluateRootElongation();
                DoRootGrowthAllocation();
            }
            else
            {
                // no actual growth, just zero out some variables
                dGrowthShootDM = 0.0;
                dGrowthRootDM = 0.0;
            }
        }

        /// <summary>Computes the turnover rates for each tissue pool of all plant organs.</summary>
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
            ttfLeafNumber = 3.0 / myLiveLeavesPerTiller; // three refers to the number of stages used in the model

            // Get the moisture factor for root tissue turnover
            ttfMoistureRoot = 2.0 - Math.Min(glfWater, glfAeration);

            //stocking rate affecting transfer of dead to litter (default as 0 for now - should be read in)
            double SR = 0;
            double StockFac2Litter = TissueTurnoverStockFactor * SR;

            // Turnover rate for leaf and stem tissues
            gama = myTissueTurnoverRateShoot * ttfTemperature * ttfMoistureShoot * ttfLeafNumber;

            // Turnover rate for dead to litter (detachment)
            double digestDead = (leaves.DigestibilityDead * leaves.DMDead) + (stems.DigestibilityDead * stems.DMDead);
            digestDead = MathUtilities.Divide(digestDead, leaves.DMDead + stems.DMDead, 0.0);
            gamaD = myDetachmentRate * ttfMoistureLitter * digestDead / CarbonFractionInDM;
            gamaD += StockFac2Litter;

            // Turnover rate for roots
            gamaR = myTissueTurnoverRateRoot * ttfTemperature * ttfMoistureRoot;

            if ((gama > 1.0) || (gamaD > 1.0) || (gamaR > 1.0))
                throw new ApsimXException(this, " AgPasture computed a tissue turnover rate greater than one");
            if ((gama < 0.0) || (gamaD < 0.0) || (gamaR < 0.0))
                throw new ApsimXException(this, " AgPasture computed a negative tissue turnover rate");

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

                // Turnover rate for stolon
                if (isLegume)
                {
                    // base rate is the same as for the other above ground organs
                    gamaS = gama;

                    // Adjust stolon turnover due to defoliation (increases stolon senescence)
                    gamaS += defoliatedFraction * (1.0 - gamaS);

                    gamaS = MathUtilities.Bound(gamaS, 0.0, 1.0);
                }
                else
                    gamaS = 0.0;

                // Check that senescence will not result in dmGreen < dmGreenmin (perennials only)
                if (!isAnnual)
                {
                    //only relevant for leaves+stems
                    double currentGreenDM = leaves.DMLive + stems.DMLive;
                    double currentMatureDM = leaves.Tissue[2].DM + stems.Tissue[2].DM;
                    double dmGreenToBe = currentGreenDM - (currentMatureDM * gama);
                    double minimumStandingLive = leaves.MinimumLiveDM + stems.MinimumLiveDM;
                    if (dmGreenToBe < minimumStandingLive)
                    {
                        double gamaAdjusted = MathUtilities.Divide(currentGreenDM - minimumStandingLive, currentMatureDM, 0.0);
                        gamaAdjusted = MathUtilities.Bound(gamaAdjusted, 0.0, 1.0);
                        gamaR *= gamaAdjusted / gama;
                        gama = gamaAdjusted;
                    }

                    // set a minimum for roots too
                    if (roots.DMLive * (1.0 - gamaR) < minimumStandingLive * MinimumGreenRootProp)
                        gamaR = 0.0;
                }

                //// Do the actual turnover, update DM and N

                // Leaves and stems
                double[] turnoverRates = new double[] { gama * RelativeTurnoverGrowing, gama, gama, gamaD };
                leaves.DoTissueTurnover(turnoverRates);
                stems.DoTissueTurnover(turnoverRates);

                // Stolons
                if (isLegume)
                {
                    turnoverRates = new double[] { gamaS * RelativeTurnoverGrowing, gamaS, gamaS, 1.0 };
                    stolons.DoTissueTurnover(turnoverRates);
                }

                // Roots (only 2 tissues)
                turnoverRates = new double[] { gamaR, 1.0 };
                roots.DoTissueTurnover(turnoverRates);

                // TODO: consider C remobilisation
                // ChRemobSugar = dSenescedRoot * KappaCRemob;
                // ChRemobProtein = dSenescedRoot * (roots.Tissue[0].Nconc - roots.NConcMinimum) * CNratioProtein * FacCNRemob;
                // senescedRootDM -= ChRemobSugar + ChRemobProtein;
                // CRemobilisable += ChRemobSugar + ChRemobProtein;

                // C remobilised from senesced tissues to be used in new growth (converted from carbohydrate to C)
                remobilisableC += 0.0;
                remobilisableC *= CarbonFractionInDM;

                // Get the amounts detached today
                detachedShootDM = leaves.DMDetached + stems.DMDetached + stolons.DMDetached;
                detachedShootN = leaves.NDetached + stems.NDetached + stolons.NDetached;
                detachedRootDM = roots.DMDetached;
                detachedRootN = roots.NDetached;
            }
        }

        #endregion  --------------------------------------------------------------------------------------------------------

        #region - Water uptake processes  ----------------------------------------------------------------------------------

        /// <summary>Performs the water uptake calculations.</summary>
        internal void DoWaterCalculations()
        {
            if (MyWaterUptakeSource == "species")
            {
                // this module will compute water uptake

                // Pack the soil information
                ZoneWaterAndN myZone = new ZoneWaterAndN();
                myZone.Name = this.Parent.Name;
                myZone.Water = mySoil.Water;
                myZone.NO3N = mySoil.NO3N;
                myZone.NH4N = mySoil.NH4N;

                // Get the amount of soil available water
                EvaluateSoilWaterAvailable(myZone);

                // Get the amount of water taken up
                EvaluateSoilWaterUptake();

                // Send the delta water to soil water module
                DoSoilWaterUptake();
            }
            else if ((MyWaterUptakeSource == "SoilArbitrator") || (MyWaterUptakeSource == "Arbitrator"))
            {
                // water uptake has been calculated by a resource arbitrator
                DoSoilWaterUptake();
            }
            else
            {
                // water uptake is computed by another module (e.g. SWIM) and supplied by OnWaterUptakesCalculated
                throw new NotImplementedException();
            }
        }

        /// <summary>Finds out the amount of plant available water in the soil.</summary>
        /// <param name="myZone">Soil information</param>
        internal void EvaluateSoilWaterAvailable(ZoneWaterAndN myZone)
        {
            if (myWaterAvailableMethod == PlantAvailableWaterMethod.Default)
                mySoilWaterAvailable = PlantAvailableSoilWaterDefault(myZone);
            else if (myWaterAvailableMethod == PlantAvailableWaterMethod.AlternativeKL)
                mySoilWaterAvailable = PlantAvailableSoilWaterAlternativeKL(myZone);
            else if (myWaterAvailableMethod == PlantAvailableWaterMethod.AlternativeKS)
                mySoilWaterAvailable = PlantAvailableSoilWaterAlternativeKS(myZone);
        }

        /// <summary>Estimates the amount of plant available water in each soil layer of the root zone.</summary>
        /// <remarks>This is the default APSIM method, with kl representing the daily rate for water extraction</remarks>
        /// <param name="myZone">Soil information</param>
        /// <returns>Amount of available water (mm)</returns>
        private double[] PlantAvailableSoilWaterDefault(ZoneWaterAndN myZone)
        {
            double[] result = new double[nLayers];
            SoilCrop soilCropData = (SoilCrop)mySoil.Crop(Name);
            for (int layer = 0; layer <= roots.BottomLayer; layer++)
            {
                result[layer] = Math.Max(0.0, myZone.Water[layer] - (soilCropData.LL[layer] * mySoil.Thickness[layer]));
                result[layer] *= FractionLayerWithRoots(layer) * soilCropData.KL[layer];
            }

            return result;
        }

        /// <summary>Estimates the amount of plant available  water in each soil layer of the root zone.</summary>
        /// <remarks>
        /// This is an alternative method, kl representing a soil limiting factor for water extraction (clayey soils have lower values)
        ///  this is further modiied by soil water content (a reduction for dry soil). A plant related factor is defined based on root
        ///  length density (limiting conditions when RLD is below ReferenceRLD)
        /// </remarks>
        /// <param name="myZone">Soil information</param>
        /// <returns>Amount of available water (mm)</returns>
        private double[] PlantAvailableSoilWaterAlternativeKL(ZoneWaterAndN myZone)
        {
            double[] result = new double[nLayers];
            SoilCrop soilCropData = (SoilCrop)mySoil.Crop(Name);
            double rldFac;
            double swFac;
            for (int layer = 0; layer <= roots.BottomLayer; layer++)
            {
                rldFac = Math.Min(1.0, RLD[layer] / ReferenceRLD);
                if (mySoil.SoilWater.SWmm[layer] >= mySoil.SoilWater.DULmm[layer])
                    swFac = 1.0;
                else if (mySoil.SoilWater.SWmm[layer] <= mySoil.SoilWater.LL15mm[layer])
                    swFac = 0.0;
                else
                {
                    double waterRatio = (myZone.Water[layer] - mySoil.SoilWater.LL15mm[layer]) /
                                        (mySoil.SoilWater.DULmm[layer] - mySoil.SoilWater.LL15mm[layer]);
                    swFac = 1.0 - Math.Pow(1.0 - waterRatio, ExponentSoilMoisture);
                }

                result[layer] = Math.Max(0.0, myZone.Water[layer] - (soilCropData.LL[layer] * mySoil.Thickness[layer]));
                result[layer] *= FractionLayerWithRoots(layer) * Math.Min(1.0, soilCropData.KL[layer] * swFac * rldFac);
            }

            return result;
        }

        /// <summary>Estimates the amount of plant available water in each soil layer of the root zone.</summary>
        /// <remarks>
        /// This is an alternative method, which does not use kl. A factor based on Ksat is used instead. This is further modified
        ///  by soil water content and a plant related factor, defined based on root length density. All three factors are normalised 
        ///  (using ReferenceKSat and ReferenceRLD for KSat and root and DUL for soil water content). The effect of all factors are
        ///  assumed to vary between zero and one following exponential functions, such that the effect is 90% at the reference value.
        /// </remarks>
        /// <param name="myZone">Soil information</param>
        /// <returns>Amount of available water (mm)</returns>
        private double[] PlantAvailableSoilWaterAlternativeKS(ZoneWaterAndN myZone)
        {
            double[] result = new double[nLayers];
            double condFac = 0.0;
            double rldFac = 0.0;
            double swFac = 0.0;
            SoilCrop soilCropData = (SoilCrop)mySoil.Crop(Name);
            for (int layer = 0; layer <= roots.BottomLayer; layer++)
            {
                condFac = 1.0 - Math.Pow(10, -mySoil.KS[layer] / ReferenceKSuptake);
                rldFac = 1.0 - Math.Pow(10, -RLD[layer] / ReferenceRLD);
                if (mySoil.SoilWater.SWmm[layer] >= mySoil.SoilWater.DULmm[layer])
                    swFac = 1.0;
                else if (mySoil.SoilWater.SWmm[layer] <= mySoil.SoilWater.LL15mm[layer])
                    swFac = 0.0;
                else
                {
                    double waterRatio = (myZone.Water[layer] - mySoil.SoilWater.LL15mm[layer]) /
                                        (mySoil.SoilWater.DULmm[layer] - mySoil.SoilWater.LL15mm[layer]);
                    swFac = 1.0 - Math.Pow(1.0 - waterRatio, ExponentSoilMoisture);
                }

                // Theoretical total available water
                result[layer] = Math.Max(0.0, myZone.Water[layer] - soilCropData.LL[layer]) * mySoil.Thickness[layer];

                // Actual available water
                result[layer] *= FractionLayerWithRoots(layer) * rldFac * condFac * swFac;
            }

            return result;
        }


        /// <summary>Adjusts the values of available water by a given fraction.</summary>
        /// <remarks>This is needed while using sward to control water processes</remarks>
        /// <param name="fraction">Fraction to adjust the current values</param>
        internal void UpdateAvailableWater(double fraction)
        {
            for (int layer = 0; layer <= roots.BottomLayer; layer++)
            {
                mySoilWaterAvailable[layer] *= fraction;
            }
        }

        /// <summary>Computes the potential plant water uptake.</summary>
        internal void EvaluateSoilWaterUptake()
        {
            // 1. Get the amount of soil water available
            double supply = mySoilWaterAvailable.Sum();

            // 2. Get the amount of soil water demanded
            double demand = myWaterDemand;

            // 3. Estimate fraction of water used up
            double fractionUsed = 0.0;
            if (supply > Epsilon)
                fractionUsed = Math.Min(1.0, demand / supply);

            // 4. Get the amount of water actually taken up
            mySoilWaterUptake = MathUtilities.Multiply_Value(mySoilWaterAvailable, fractionUsed);
        }

        /// <summary>Sends the delta water to the soil module.</summary>
        private void DoSoilWaterUptake()
        {
            if (mySoilWaterUptake.Sum() > Epsilon)
            {
                WaterChangedType waterTakenUp = new WaterChangedType();
                waterTakenUp.DeltaWater = new double[nLayers];
                for (int layer = 0; layer <= roots.BottomLayer; layer++)
                    waterTakenUp.DeltaWater[layer] = -mySoilWaterUptake[layer];

                if (WaterChanged != null)
                    WaterChanged.Invoke(waterTakenUp);
            }
        }

        /// <summary>Gets the water uptake for each layer as calculated by an external module (SWIM).</summary>
        /// <param name="SoilWater">The soil water uptake data.</param>
        [EventSubscribe("WaterUptakesCalculated")]
        private void OnWaterUptakesCalculated(WaterUptakesCalculatedType SoilWater)
        {
            foreach (WaterUptakesCalculatedUptakesType cropUptake in SoilWater.Uptakes)
            {
                if (cropUptake.Name == Name)
                {
                    for (int layer = 0; layer < cropUptake.Amount.Length; layer++)
                        mySoilWaterUptake[layer] = cropUptake.Amount[layer];
                }
            }
        }

        #endregion  --------------------------------------------------------------------------------------------------------

        #region - Nitrogen uptake processes  -------------------------------------------------------------------------------

        /// <summary>Performs the nitrogen uptake calculations.</summary>
        internal void DoNitrogenCalculations()
        {
            if (MyNitrogenUptakeSource == "species")
            {
                // this module will compute nitrogen uptake

                // Pack the soil information
                ZoneWaterAndN myZone = new ZoneWaterAndN();
                myZone.Name = this.Parent.Name;
                myZone.Water = mySoil.Water;
                myZone.NO3N = mySoil.NO3N;
                myZone.NH4N = mySoil.NH4N;

                // Get the N amount available in the soil
                EvaluateSoilNitrogenAvailable(myZone);

                // Get the N amount fixed through symbiosis
                EvaluateNitrogenFixation();

                // Evaluate the use of N remobilised and get N amount demanded from soil
                EvaluateSoilNitrogenDemand();

                // Get N amount take up from the soil
                EvaluateSoilNitrogenUptake();

                // Evaluate whether remobilisation of luxury N is needed
                EvaluateNLuxuryRemobilisation();

                // Send delta N to the soil model
                DoSoilNitrogenUptake();
            }
            else if (MyNitrogenUptakeSource == "SoilArbitrator")
            {
                // Nitrogen uptake was computed by the resource arbitrator

                // Evaluate whether remobilisation of luxury N is needed
                EvaluateNLuxuryRemobilisation();

                // Send delta N to the soil model
                DoSoilNitrogenUptake();
            }
            else if (MyNitrogenUptakeSource == "Arbitrator")
            {
                // Nitrogen uptake was computed by the resource arbitrator

                // gather the uptake values
                if (MyNitrogenUptakeSource == "Arbitrator")
                {
                    for (int layer = 0; layer <= roots.BottomLayer; layer++)
                    {
                        mySoilNH4Uptake[layer] = uptakeNitrogen[layer] * (1.0 - uptakeNitrogenPropNO3[layer]);
                        mySoilNO3Uptake[layer] = uptakeNitrogen[layer] * uptakeNitrogenPropNO3[layer];
                    }
                }

                // Evaluate whether remobilisation of luxury N is needed
                EvaluateNLuxuryRemobilisation();
            }
            else
            {
                // N uptake is computed by another module (e.g. SWIM) and supplied by OnNitrogenUptakesCalculated
                throw new NotImplementedException();
            }
        }

        /// <summary>Computes the amount of nitrogen demand for optimum N content as well as luxury uptake.</summary>
        internal void EvaluateNitrogenDemand()
        {
            double toRoot = dGrowthWstress * (1.0 - fractionToShoot);
            double toStol = dGrowthWstress * fractionToShoot * myFractionToStolon;
            double toLeaf = dGrowthWstress * fractionToShoot * fractionToLeaf;
            double toStem = dGrowthWstress * fractionToShoot * (1.0 - myFractionToStolon - fractionToLeaf);

            // N demand for new growth, with optimum N (kg/ha)
            demandOptimumN = (toLeaf * leaves.NConcOptimum) + (toStem * stems.NConcOptimum)
                       + (toStol * stolons.NConcOptimum) + (toRoot * roots.NConcOptimum);

            // get the factor to reduce the demand under elevated CO2
            double fN = NFactorDueToCO2();
            demandOptimumN *= fN;

            // N demand for new growth, with luxury uptake (maximum [N])
            demandLuxuryN = (toLeaf * leaves.NConcMaximum) + (toStem * stems.NConcMaximum)
                       + (toStol * stolons.NConcMaximum) + (toRoot * roots.NConcMaximum);
            // It is assumed that luxury uptake is not affected by CO2 variations
        }

        /// <summary>Computes the amount of atmospheric nitrogen fixed through symbiosis.</summary>
        internal void EvaluateNitrogenFixation()
        {
            fixedN = 0.0;
            if (isLegume && demandLuxuryN > Epsilon)
            {
                // Start with minimum fixation
                fixedN = myMinimumNFixation * demandLuxuryN;

                // Evaluate N stress
                double Nstress = Math.Max(0.0, MathUtilities.Divide(SoilAvailableN, demandLuxuryN - fixedN, 1.0));

                // Update N fixation if under N stress
                if (Nstress < 0.99)
                    fixedN += (myMaximumNFixation - myMinimumNFixation) * (1.0 - Nstress) * demandLuxuryN;
            }
        }

        /// <summary>Evaluates the use of remobilised nitrogen and computes soil nitrogen demand.</summary>
        internal void EvaluateSoilNitrogenDemand()
        {
            double fracRemobilised = 0.0;
            if (demandLuxuryN - fixedN < Epsilon)
            {
                // N demand is fulfilled by fixation alone
                senescedNRemobilised = 0.0;
                mySoilNDemand = 0.0;
            }
            else if (demandLuxuryN - (fixedN + RemobilisableSenescedN) < Epsilon)
            {
                // N demand is fulfilled by fixation plus N remobilised from senesced material
                senescedNRemobilised = Math.Max(0.0, demandLuxuryN - fixedN);
                mySoilNDemand = 0.0;
                fracRemobilised = MathUtilities.Divide(senescedNRemobilised, RemobilisableSenescedN, 0.0);
            }
            else
            {
                // N demand is greater than fixation and remobilisation, N uptake is needed
                senescedNRemobilised = RemobilisableSenescedN;
                mySoilNDemand = demandLuxuryN - (fixedN + senescedNRemobilised);
                fracRemobilised = 1.0;
            }

            // Update N remobilised in each organ
            if (senescedNRemobilised > Epsilon)
            {
                leaves.Tissue[leaves.TissueCount - 1].DoRemobiliseN(fracRemobilised);
                stems.Tissue[stems.TissueCount - 1].DoRemobiliseN(fracRemobilised);
                stolons.Tissue[stolons.TissueCount - 1].DoRemobiliseN(fracRemobilised);
                roots.Tissue[roots.TissueCount - 1].DoRemobiliseN(fracRemobilised);
            }
        }

        /// <summary>Finds out the amount of plant available nitrogen (NH4 and NO3) in the soil.</summary>
        /// <param name="myZone">Soil information</param>
        internal void EvaluateSoilNitrogenAvailable(ZoneWaterAndN myZone)
        {
            if (myNitrogenAvailableMethod == PlantAvailableNitrogenMethod.BasicAgPasture)
                PlantAvailableSoilNBasicAgPasture(myZone);
            else if (myNitrogenAvailableMethod == PlantAvailableNitrogenMethod.DefaultAPSIM)
                PlantAvailableSoilNDefaultAPSIM(myZone);
            else if (myNitrogenAvailableMethod == PlantAvailableNitrogenMethod.AlternativeRLD)
                PlantAvailableSoilNAlternativeRLD(myZone);
            else if (myNitrogenAvailableMethod == PlantAvailableNitrogenMethod.AlternativeWup)
                PlantAvailableSoilNAlternativeWup(myZone);
        }

        /// <summary>Estimates the amount of plant available nitrogen in each soil layer of the root zone.</summary>
        /// <remarks>This is a basic method, used as default in old AgPasture, all N in the root zone is available</remarks>
        /// <param name="myZone">Soil information</param>
        private void PlantAvailableSoilNBasicAgPasture(ZoneWaterAndN myZone)
        {
            double layerFrac; // the fraction of layer within the root zone
            for (int layer = 0; layer <= roots.BottomLayer; layer++)
            {
                layerFrac = FractionLayerWithRoots(layer);
                mySoilNH4Available[layer] = myZone.NH4N[layer] * layerFrac;
                mySoilNO3Available[layer] = myZone.NO3N[layer] * layerFrac;
            }
        }

        /// <summary>Estimates the amount of plant available nitrogen in each soil layer of the root zone.</summary>
        /// <remarks>
        /// This method approximates the default approach in APSIM plants (method 3 in Plant1 models)
        /// Soil water status and uptake coefficient control the availability, which is a square function of N content.
        /// Uptake is capped for a maximum value plants can take in one day.
        /// </remarks>
        /// <param name="myZone">Soil information</param>
        private void PlantAvailableSoilNDefaultAPSIM(ZoneWaterAndN myZone)
        {
            double layerFrac; // the fraction of layer within the root zone
            double swFac;  // the soil water factor
            double bdFac;  // the soil density factor
            double potAvailableN; // potential available N
            for (int layer = 0; layer <= roots.BottomLayer; layer++)
            {
                layerFrac = FractionLayerWithRoots(layer);
                bdFac = 100.0 / (mySoil.Thickness[layer] * mySoil.BD[layer]);
                if (myZone.Water[layer] >= mySoil.SoilWater.DULmm[layer])
                    swFac = 1.0;
                else if (myZone.Water[layer] <= mySoil.SoilWater.LL15mm[layer])
                    swFac = 0.0;
                else
                {
                    double waterRatio = (myZone.Water[layer] - mySoil.SoilWater.LL15mm[layer]) /
                                        (mySoil.SoilWater.DULmm[layer] - mySoil.SoilWater.LL15mm[layer]);
                    swFac = 1.0 - Math.Pow(1.0 - waterRatio, ExponentSoilMoisture);
                }

                // get NH4 available
                potAvailableN = Math.Pow(myZone.NH4N[layer] * layerFrac, 2.0) * swFac * bdFac * KNH4;
                mySoilNH4Available[layer] = Math.Min(myZone.NH4N[layer] * layerFrac, potAvailableN);

                // get NO3 available
                potAvailableN = Math.Pow(myZone.NO3N[layer] * layerFrac, 2.0) * swFac * bdFac * KNO3;
                mySoilNO3Available[layer] = Math.Min(myZone.NO3N[layer] * layerFrac, potAvailableN);
            }

            // check for maximum uptake
            potAvailableN = mySoilNH4Available.Sum() + mySoilNO3Available.Sum();
            if (potAvailableN > MaximumNUptake)
            {
                double upFraction = MaximumNUptake / potAvailableN;
                for (int layer = 0; layer <= roots.BottomLayer; layer++)
                {
                    mySoilNH4Available[layer] *= upFraction;
                    mySoilNO3Available[layer] *= upFraction;
                }
            }
        }

        /// <summary>Estimates the amount of plant available nitrogen in each soil layer of the root zone.</summary>
        /// <remarks>
        /// This method considers soil water status and root length density to define factors controlling N availability.
        /// Soil water stauts is used to define a factor that varies from zero at LL, below which no uptake can happen, 
        ///  to one at DUL, above which no restrictions to uptake exist.
        /// Root length density is used to define a factor varying from zero if there are no roots to one when root length
        ///  density is equal to a ReferenceRLD, above which there are no restrictions for uptake.
        /// Factors for each N form can also alter the amount available.
        /// Uptake is caped for a maximum value plants can take in one day.
        /// </remarks>
        /// <param name="myZone">Soil information</param>
        private void PlantAvailableSoilNAlternativeRLD(ZoneWaterAndN myZone)
        {
            double layerFrac; // the fraction of layer within the root zone
            double swFac;  // the soil water factor
            double rldFac;  // the root density factor
            double potAvailableN; // potential available N
            for (int layer = 0; layer <= roots.BottomLayer; layer++)
            {
                layerFrac = FractionLayerWithRoots(layer);
                rldFac = Math.Min(1.0, MathUtilities.Divide(RLD[layer], ReferenceRLD, 1.0));
                if (myZone.Water[layer] >= mySoil.SoilWater.DULmm[layer])
                    swFac = 1.0;
                else if (myZone.Water[layer] <= mySoil.SoilWater.LL15mm[layer])
                    swFac = 0.0;
                else
                {
                    double waterRatio = (myZone.Water[layer] - mySoil.SoilWater.LL15mm[layer]) /
                                        (mySoil.SoilWater.DULmm[layer] - mySoil.SoilWater.LL15mm[layer]);
                    swFac = 1.0 - Math.Pow(1.0 - waterRatio, ExponentSoilMoisture);
                }

                // get NH4 available
                potAvailableN = myZone.NH4N[layer] * layerFrac;
                mySoilNH4Available[layer] = potAvailableN * Math.Min(1.0, swFac * rldFac * kuNH4);

                // get NO3 available
                potAvailableN = myZone.NO3N[layer] * layerFrac;
                mySoilNO3Available[layer] = potAvailableN * Math.Min(1.0, swFac * rldFac * kuNO3);
            }

            // check for maximum uptake
            potAvailableN = mySoilNH4Available.Sum() + mySoilNO3Available.Sum();
            if (potAvailableN > MaximumNUptake)
            {
                double upFraction = MaximumNUptake / potAvailableN;
                for (int layer = 0; layer <= roots.BottomLayer; layer++)
                {
                    mySoilNH4Available[layer] *= upFraction;
                    mySoilNO3Available[layer] *= upFraction;
                }
            }
        }

        /// <summary>Estimates the amount of plant available nitrogen in each soil layer of the root zone.</summary>
        /// <remarks>
        /// This method considers soil water as the main factor controlling N availability/uptake.
        /// Availability is given by the proportion of water taken up in each layer, further modified by uptake factors
        /// Uptake is caped for a maximum value plants can take in one day.
        /// </remarks>
        /// <param name="myZone">Soil information</param>
        private void PlantAvailableSoilNAlternativeWup(ZoneWaterAndN myZone)
        {
            double layerFrac; // the fraction of layer within the root zone
            double swuFac;  // the soil water factor
            double potAvailableN; // potential available N
            for (int layer = 0; layer <= roots.BottomLayer; layer++)
            {
                layerFrac = FractionLayerWithRoots(layer);
                swuFac = MathUtilities.Divide(mySoilWaterUptake[layer], myZone.Water[layer], 0.0);

                // get NH4 available
                potAvailableN = myZone.NH4N[layer] * layerFrac;
                mySoilNH4Available[layer] = potAvailableN * Math.Min(1.0, swuFac * kuNH4);

                // get NO3 available
                potAvailableN = myZone.NO3N[layer] * layerFrac;
                mySoilNO3Available[layer] = potAvailableN * Math.Min(1.0, swuFac * kuNO3);
            }

            // check for maximum uptake
            potAvailableN = mySoilNH4Available.Sum() + mySoilNO3Available.Sum();
            if (potAvailableN > MaximumNUptake)
            {
                double upFraction = MaximumNUptake / potAvailableN;
                for (int layer = 0; layer <= roots.BottomLayer; layer++)
                {
                    mySoilNH4Available[layer] *= upFraction;
                    mySoilNO3Available[layer] *= upFraction;
                }
            }
        }

        /// <summary>Adjusts the values of available NH4 and NO3 by a given fraction.</summary>
        /// <remarks>This is needed while using sward to control N processes</remarks>
        /// <param name="nh4Fraction">Fraction to adjust the current NH4 values</param>
        /// <param name="no3Fraction">Fraction to adjust the current NO3 values</param>
        internal void UpdateAvailableNitrogen(double nh4Fraction, double no3Fraction)
        {
            for (int layer = 0; layer <= roots.BottomLayer; layer++)
            {
                mySoilNH4Available[layer] *= nh4Fraction;
                mySoilNO3Available[layer] *= no3Fraction;
            }
        }

        /// <summary>Computes the potential amount of nitrogen taken up by the plant.</summary>
        internal void EvaluateSoilNitrogenUptake()
        {
            // 1. Get the amount of soil N available
            double supply = mySoilNH4Available.Sum() + mySoilNO3Available.Sum();

            // 2. Get the amount of soil N demanded
            double demand = mySoilNDemand;

            // 3. Estimate fraction of N used up
            double fractionUsed = 0.0;
            if (supply > Epsilon)
                fractionUsed = Math.Min(1.0, demand / supply);

            // 4. Get the amount of N actually taken up
            mySoilNH4Uptake = MathUtilities.Multiply_Value(mySoilNH4Available, fractionUsed);
            mySoilNO3Uptake = MathUtilities.Multiply_Value(mySoilNO3Available, fractionUsed);
        }

        /// <summary>Computes the amount of nitrogen remobilised from tissues with N content above optimum.</summary>
        internal void EvaluateNLuxuryRemobilisation()
        {
            // check whether there is still demand for N (only match demand for growth at optimum N conc.)
            // check whether there is any luxury N remobilisable
            double Nmissing = demandOptimumN - (fixedN + senescedNRemobilised + UptakeN);
            if ((Nmissing > Epsilon) && (RemobilisableLuxuryN > Epsilon))
            {
                // all N already considered is not enough to match demand for growth, check remobilisation of luxury N
                if (Nmissing >= RemobilisableLuxuryN)
                {
                    // N luxury is just or not enough for optimum growth, use up all there is
                    if (RemobilisableLuxuryN > Epsilon)
                    {
                        luxuryNRemobilised = RemobilisableLuxuryN;
                        Nmissing -= luxuryNRemobilised;

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

                        luxuryNRemobilised += Nusedup;
                        Nmissing -= Nusedup;
                        if (Nmissing <= Epsilon) tissue = 0;
                    }
                }
            }
        }

        /// <summary>Sends the delta nitrogen to the soil module.</summary>
        private void DoSoilNitrogenUptake()
        {
            if ((mySoilNH4Uptake.Sum() + mySoilNO3Uptake.Sum()) > Epsilon)
            {
                NitrogenChangedType nitrogenTakenUp = new NitrogenChangedType();
                nitrogenTakenUp.Sender = Name;
                nitrogenTakenUp.SenderType = "Plant";
                nitrogenTakenUp.DeltaNO3 = new double[nLayers];
                nitrogenTakenUp.DeltaNH4 = new double[nLayers];

                for (int layer = 0; layer <= roots.BottomLayer; layer++)
                {
                    nitrogenTakenUp.DeltaNH4[layer] = -mySoilNH4Uptake[layer];
                    nitrogenTakenUp.DeltaNO3[layer] = -mySoilNO3Uptake[layer];
                }

                if (NitrogenChanged != null)
                    NitrogenChanged.Invoke(nitrogenTakenUp);
            }
        }

        #endregion  --------------------------------------------------------------------------------------------------------

        #region - Organic matter processes  --------------------------------------------------------------------------------

        /// <summary>Returns a given amount of DM (and N) to surface organic matter.</summary>
        /// <param name="amountDM">DM amount to return (kg/ha)</param>
        /// <param name="amountN">N amount to return (kg/ha)</param>
        private void DoSurfaceOMReturn(double amountDM, double amountN)
        {
            if (BiomassRemoved != null)
            {
                BiomassRemovedType biomassData = new BiomassRemovedType();
                string[] type = {mySpeciesFamily.ToString()};
                float[] dltdm = {(float) amountDM};
                float[] dltn = {(float) amountN};
                float[] dltp = {0f}; // P not considered here
                float[] fraction = {1f}; // fraction is always 1.0 here

                biomassData.crop_type = "grass"; //TODO: this could be the Name, what is the diff between name and type??
                biomassData.dm_type = type;
                biomassData.dlt_crop_dm = dltdm;
                biomassData.dlt_dm_n = dltn;
                biomassData.dlt_dm_p = dltp;
                biomassData.fraction_to_residue = fraction;
                BiomassRemoved.Invoke(biomassData);
            }
        }

        /// <summary>Returns a given amount of DM (and N) to fresh organic matter pool in the soil</summary>
        /// <param name="amountDM">DM amount to return (kg/ha)</param>
        /// <param name="amountN">N amount to return (kg/ha)</param>
        private void DoIncorpFomEvent(double amountDM, double amountN)
        {
            FOMLayerLayerType[] FOMdataLayer = new FOMLayerLayerType[nLayers];

            // ****  RCichota, Jun/2014
            // root senesced are returned to soil (as FOM) considering return is proportional to root mass

            for (int layer = 0; layer < nLayers; layer++)
            {
                FOMType fomData = new FOMType();
                fomData.amount = amountDM * roots.Tissue[0].FractionWt[layer];
                fomData.N = amountN * roots.Tissue[0].FractionWt[layer];
                fomData.C = amountDM * CarbonFractionInDM * roots.Tissue[0].FractionWt[layer];
                fomData.P = 0.0; // P not considered here
                fomData.AshAlk = 0.0; // Ash not considered here

                FOMLayerLayerType layerData = new FOMLayerLayerType();
                layerData.FOM = fomData;
                layerData.CNR = 0.0; // not used here
                layerData.LabileP = 0; // not used here

                FOMdataLayer[layer] = layerData;
            }

            if (IncorpFOM != null)
            {
                FOMLayerType FOMData = new FOMLayerType();
                FOMData.Type = mySpeciesFamily.ToString();
                FOMData.Layer = FOMdataLayer;
                IncorpFOM.Invoke(FOMData);
            }
        }

        #endregion  --------------------------------------------------------------------------------------------------------

        #region - Handling and auxilary processes  -------------------------------------------------------------------------

        /// <summary>Zeroes out the value of several variables.</summary>
        internal void RefreshVariables()
        {
            // reset variables for whole plant
            defoliatedDM = 0.0;
            defoliatedN = 0.0;
            defoliatedDigestibility = 0.0;

            dGrowthShootDM = 0.0;
            dGrowthShootN = 0.0;
            dGrowthRootDM = 0.0;
            dGrowthRootN = 0.0;

            detachedShootDM = 0.0;
            detachedShootN = 0.0;
            detachedRootDM = 0.0;
            detachedRootN = 0.0;

            senescedNRemobilised = 0.0;
            luxuryNRemobilised = 0.0;

            mySoilWaterAvailable = new double[nLayers];
            mySoilWaterUptake = new double[nLayers];
            mySoilNH4Available = new double[nLayers];
            mySoilNO3Available = new double[nLayers];
            mySoilNH4Uptake = new double[nLayers];
            mySoilNO3Uptake = new double[nLayers];

            // reset transfer variables for all tissues in each organ
            leaves.DoCleanTransferAmounts();
            stems.DoCleanTransferAmounts();
            stolons.DoCleanTransferAmounts();
            roots.DoCleanTransferAmounts();
        }

        /// <summary>Computes a growth factor for annual species, related to phenology/population.</summary>
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

        /// <summary>Calculates the factor increasing shoot allocation during reproductive growth.</summary>
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

        /// <summary>Computes the allocations into shoot and leaves of todays growth.</summary>
        internal void EvaluateGrowthAllocation()
        {
            EvaluateAllocationToShoot();
            EvaluateAllocationToLeaf();
        }

        /// <summary>Calculates the fraction of new growth allocated to shoot.</summary>
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
                double glfFactor = 1.0 - myShootRootGlfFactor * (1.0 - Math.Pow(glfMin, 1.0 / myShootRootGlfFactor));

                // get the current shoot/root ratio (partiton will try to make this value closer to targetSR)
                double currentSR = MathUtilities.Divide(AboveGroundLiveWt, roots.DMLive, 1000000.0);

                // get the factor for the reproductive season of perennials (increases shoot allocation during spring)
                double reproFac = 1.0;
                if (usingReproSeasonFactor && !isAnnual)
                    reproFac = CalcReproductiveGrowthFactor();

                // get today's target SR
                double targetSR = myTargetSRratio * reproFac;

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
            if ((1 - fractionToShoot) > myMaxRootAllocation)
                fractionToShoot = 1 - myMaxRootAllocation;
        }

        /// <summary>Computes the fraction of new shoot DM that is allocated to leaves.</summary>
        /// <remarks>
        /// This method is used to reduce the propotion of leaves as plants grow, this is used for species that 
        ///  allocate proportionally more DM to stolon/stems when the whole plant's DM is high.
        /// To avoid too little allocation to leaves in case of grazing the current leaf:stem ratio is evaluated
        ///  and used to modify the targeted value in a similar way as shoot:root ratio.
        /// </remarks>
        private void EvaluateAllocationToLeaf()
        {
            // compute new target FractionLeaf
            double targetFLeaf = myFractionLeafMaximum;
            if ((myFractionLeafMinimum < myFractionLeafMaximum) && (AboveGroundLiveWt > myFractionLeafDMThreshold))
            {
                double fLeafAux = (AboveGroundLiveWt - myFractionLeafDMThreshold) / (myFractionLeafDMFactor - myFractionLeafDMThreshold);
                fLeafAux = Math.Pow(fLeafAux, myFractionLeafExponent);
                targetFLeaf = myFractionLeafMinimum + (myFractionLeafMaximum - myFractionLeafMinimum) / (1.0 + fLeafAux);
            }

            // get current leaf:stem ratio
            double currentLS = leaves.DMLive / (stems.DMLive + stolons.DMLive);

            // get today's target leaf:stem ratio
            double targetLS = targetFLeaf / (1 - targetFLeaf);

            // adjust leaf:stem ratio, to avoid excess allocation to stem/stolons
            double newLS = targetLS * targetLS / currentLS;

            fractionToLeaf = newLS / (1 + newLS);
        }

        /// <summary>Computes the variations in root depth.</summary>
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
                if (((dGrowthRootDM - detachedRootDM) > Epsilon) && (roots.Depth < myMaximumRootDepth))
                {
                    double tempFactor = TemperatureLimitingFactor(Tmean(0.5));
                    dRootDepth = myRootElongationRate * tempFactor;
                    roots.Depth = Math.Min(myMaximumRootDepth, Math.Max(myMinimumRootDepth, roots.Depth + dRootDepth));
                    roots.BottomLayer = RootZoneBottomLayer();
                }
                else
                {
                    // No net growth
                    dRootDepth = 0.0;
                }
            }
        }

        /// <summary>Computes the allocation of new growth to roots for each layer.</summary>
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

        /// <summary>Calculates the plant height as function of DM.</summary>
        /// <returns>Plant height (mm)</returns>
        internal double HeightfromDM()
        {
            double TodaysHeight = myMaximumPlantHeight;

            if (StandingWt <= myMassForMaximumHeight)
            {
                double massRatio = StandingWt / myMassForMaximumHeight;
                double heightF = myExponentHeightFromMass - (myExponentHeightFromMass * massRatio) + massRatio;
                heightF *= Math.Pow(massRatio, myExponentHeightFromMass - 1);
                TodaysHeight *= heightF;
            }

            return Math.Max(TodaysHeight, myMinimumPlantHeight);
        }

        /// <summary>Computes the values of LAI (leaf area index) for green and dead plant material.</summary>
        /// <remarks>This method considers leaves plus an additional effect of stems and stolons</remarks>
        private void EvaluateLAI()
        {
            // Get the amount of green tissue of leaves (converted from kg/ha to kg/m2)
            double greenTissue = leaves.DMLive / 10000;
            greenLAI = greenTissue * mySpecificLeafArea;

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

                greenLAI += greenTissue * mySpecificLeafArea;

                /* This adjust helps on resilience after unfavoured conditions (implemented by F.Li, not present in EcoMod)
                 It is assumed that green cover will be bigger for the same amount of DM when compared to using only leaves due
                  to the recruitment of green tissue from stems and stolons. Thus it mimics:
                 - greater light extinction coefficient, leaves will be more horizontal than in dense high swards
                 - more parts (stems) turning green for photosysnthesis
                 - thinner leaves during growth burst following unfavoured conditions
                 » TODO: It would be better if variations in SLA or ext. coeff. would be explicitly considered (RCichota, 2014)
                */
            }

            deadLAI = (leaves.DMDead / 10000) * mySpecificLeafArea;
        }

        /// <summary>Compute the average digestibility of above-ground plant material.</summary>
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

            herbageDigestibility = result;
        }

        /// <summary>Compute the average digestibility of harvested plant material.</summary>
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

        #endregion  --------------------------------------------------------------------------------------------------------

        #endregion  --------------------------------------------------------------------------------------------------------

        #region Other processes  -------------------------------------------------------------------------------------------

        /// <summary>Harvests the crop.</summary>
        /// <param name="removalData">type and fraction to remove</param>
        public void Harvest(RemovalFractions removalData)
        {
            RemoveBiomass("Harvest", removalData);
        }

        /// <summary>Removes plant material simulating a graze event.</summary>
        /// <param name="type">The type of amount being defined (SetResidueAmount or SetRemoveAmount)</param>
        /// <param name="amount">The DM amount (kg/ha)</param>
        /// <exception cref="System.Exception"> Type of amount to remove on graze not recognized (use 'SetResidueAmount' or 'SetRemoveAmount'</exception>
        public void Graze(string type, double amount)
        {
            if (isAlive && HarvestableWt > Epsilon)
            {
                // Get the amount required to remove
                double amountRequired;
                if (type.ToLower() == "setresidueamount")
                {
                    // Remove all DM above given residual amount
                    amountRequired = Math.Max(0.0, StandingWt - amount);
                }
                else if (type.ToLower() == "setremoveamount")
                {
                    // Remove a given amount
                    amountRequired = Math.Max(0.0, amount);
                }
                else
                {
                    throw new ApsimXException(this, "Type of amount to remove on graze not recognized (use \'SetResidueAmount\' or \'SetRemoveAmount\'");
                }

                // Get the actual amount to remove
                double amountToRemove = Math.Min(amountRequired, HarvestableWt);

                // Do the actual removal
                if (amountRequired > Epsilon)
                    RemoveDM(amountToRemove);

            }
            else
                mySummary.WriteWarning(this, " Could not graze due to lack of DM available");
        }

        /// <summary>Removes a given amount of DM (and N) from this plant.</summary>
        /// <remarks>
        /// This method uses preferences for green/dead material to partition the amount to remove between plant parts.
        /// NOTE: This metod should only be called after testing the HarvestableWt is greater than zero.
        /// </remarks>
        /// <param name="amountToRemove">Amount to remove (kg/ha)</param>
        /// <exception cref="System.Exception">Removal of DM resulted in loss of mass balance</exception>
        public void RemoveDMold(double amountToRemove)
        {
            // get existing DM and N amounts
            double PreRemovalDM = AboveGroundWt;
            double PreRemovalN = AboveGroundN;

            // get the DM weights for each pool, consider preference and available DM
            //double tempPrefGreen = PrefGreen + (PrefDead * fractionToRemoved);
            //double tempPrefDead = PrefDead + (PrefGreen * fractionToRemoved);
            double tempPrefGreen = myPreferenceForGreenOverDead + (amountToRemove / HarvestableWt);
            double tempPrefDead = 1.0 + tempPrefGreen;
            double tempRemovableGreen = Math.Max(0.0, StandingLiveWt - myMinimumGreenWt);
            double tempRemovableDead = StandingDeadWt;

            // get partition between dead and live materials
            double tempTotal = tempRemovableGreen * tempPrefGreen + tempRemovableDead * tempPrefDead;
            double fractionToHarvestGreen = 0.0;
            double fractionToHarvestDead = 0.0;
            fractionToHarvestGreen = tempRemovableGreen * tempPrefGreen / tempTotal;
            fractionToHarvestDead = tempRemovableDead * tempPrefDead / tempTotal;

            // get amounts removed
            double RemovingGreenDM = amountToRemove * fractionToHarvestGreen;
            double RemovingDeadDM = amountToRemove * fractionToHarvestDead;

            // Fraction of DM remaining in the field
            double fractionRemainingGreen = 1.0;
            if (StandingLiveWt > Epsilon)
                fractionRemainingGreen = Math.Max(0.0, Math.Min(1.0, 1.0 - RemovingGreenDM / StandingLiveWt));
            double fractionRemainingDead = 1.0;
            if (StandingDeadWt > Epsilon)
                fractionRemainingDead = Math.Max(0.0, Math.Min(1.0, 1.0 - RemovingDeadDM / StandingDeadWt));

            // get digestibility of DM being harvested
            defoliatedDigestibility = calcHarvestDigestibility(leaves.DMLive * fractionToHarvestGreen, leaves.DMDead * fractionToHarvestDead,
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
            leaves.Tissue[3].NRemobilisable *= fractionRemainingGreen;
            stems.Tissue[3].NRemobilisable *= fractionRemainingGreen;
            //NSenescedRemobilisable *= fractionRemainingGreen;
            remobilisableC *= fractionRemainingGreen;

            // update Luxury N pools
            //NLuxuryRemobilisable *= fractionRemainingGreen;

            // save fraction defoliated (to be used on tissue turnover)
            defoliatedFraction = MathUtilities.Divide(defoliatedDM, defoliatedDM + AboveGroundWt, 0.0); //TODO: it should use StandingLiveWt

            // check mass balance and set outputs
            defoliatedDM = PreRemovalDM - AboveGroundWt;
            defoliatedN = PreRemovalN - AboveGroundN;
            if (Math.Abs(defoliatedDM - amountToRemove) > Epsilon)
                throw new ApsimXException(this, " Removal of DM resulted in loss of mass balance");
        }

        /// <summary>Removes a given amount of DM (and N) from this plant.</summary>
        /// <param name="amountToRemove">The DM amount to remove (kg/ha)</param>
        /// <returns>Amount actually removed (kg/ha)</returns>
        internal double RemoveDM(double amountToRemove)
        {
            // get existing DM and N amounts
            double preRemovalDM = AboveGroundWt;
            double preRemovalN = AboveGroundN;

            // Compute the fraction of each tissue to be removed
            double[] fracRemoving = new double[5];
            if (amountToRemove - HarvestableWt > -Epsilon)
            {
                // All existing DM is removed
                for (int i = 0; i < 5; i++)
                    fracRemoving[i] = 1.0;
            }
            else
            {
                // Initialise the fractions to be removed (this need to be normalised)
                fracRemoving[0] = leaves.DMLiveHarvestable * myPreferenceForGreenOverDead * myPreferenceForLeafOverStems;
                fracRemoving[1] = stems.DMLiveHarvestable * myPreferenceForGreenOverDead;
                fracRemoving[2] = stolons.DMLiveHarvestable * myPreferenceForGreenOverDead;
                fracRemoving[3] = leaves.DMDeadHarvestable * myPreferenceForLeafOverStems;
                fracRemoving[4] = stems.DMDeadHarvestable;

                // Get fraction potentially removable (maximum fraction of each tissue in the removing amount)
                double[] fracRemovable = new double[5];
                fracRemovable[0] = leaves.DMLiveHarvestable / amountToRemove;
                fracRemovable[1] = stems.DMLiveHarvestable / amountToRemove;
                fracRemovable[2] = stolons.DMLiveHarvestable / amountToRemove;
                fracRemovable[3] = leaves.DMDeadHarvestable / amountToRemove;
                fracRemovable[4] = stems.DMDeadHarvestable / amountToRemove;

                // Normalise the fractions of each tissue to be removed, they should add to one
                double totalFrac = fracRemoving.Sum();
                for (int i = 0; i < 5; i++)
                    fracRemoving[i] = Math.Min(fracRemovable[i], fracRemoving[i] / totalFrac);

                // Iterate until sum of fractions to remove is equal to one
                //  The initial normalised fractions are based on preference and existing DM. Because the value of fracRemoving is limited
                //   to fracRemovable, the sum of fracRemoving may not be equal to one, as it should be. We need to iterate adjusting the
                //   values of fracRemoving until we get a sum close enough to one. The previous values are used as weighting factors for
                //   computing new ones at each iteration.
                int count = 1;
                totalFrac = fracRemoving.Sum();
                while (1.0 - totalFrac > Epsilon)
                {
                    count += 1;
                    for (int i = 0; i < 5; i++)
                        fracRemoving[i] = Math.Min(fracRemovable[i], fracRemoving[i] / totalFrac);
                    totalFrac = fracRemoving.Sum();
                    if (count > 1000)
                    {
                        mySummary.WriteWarning(this, " AgPasture could not harvest all the DM required for " + Name);
                        break;
                    }
                }
                //mySummary.WriteMessage(this, " AgPasture " + Name + " needed " + count + " iterations to solve parttion of removed DM");
            }

            // get digestibility of DM being harvested (do this before updating pools)
            double greenDigestibility = (leaves.DigestibilityLive * fracRemoving[0]) + (stems.DigestibilityLive * fracRemoving[1])
                                        + (stolons.DigestibilityLive * fracRemoving[2]);
            double deadDigestibility = (leaves.DigestibilityDead * fracRemoving[3]) + (stems.DigestibilityDead * fracRemoving[4]);
            defoliatedDigestibility = greenDigestibility + deadDigestibility;

            // update the various pools
            // Leaves
            double fracRemaining = Math.Max(0.0, 1.0 - MathUtilities.Divide(amountToRemove * fracRemoving[0], leaves.DMLive, 0.0));
            int t;
            for (t = 0; t < 3; t++)
            {
                leaves.Tissue[t].DM *= fracRemaining;
                leaves.Tissue[t].Namount *= fracRemaining;
                //                leaves.Tissue[t].NRemobilisable *= fracRemaining;
            }
            fracRemaining = Math.Max(0.0, 1.0 - MathUtilities.Divide(amountToRemove * fracRemoving[3], leaves.DMDead, 0.0));
            leaves.Tissue[t].DM *= fracRemaining;
            leaves.Tissue[t].Namount *= fracRemaining;
            //            leaves.Tissue[t].NRemobilisable *= fracRemaining;

            // Stems
            fracRemaining = Math.Max(0.0, 1.0 - MathUtilities.Divide(amountToRemove * fracRemoving[1], stems.DMLive, 0.0));
            for (t = 0; t < 3; t++)
            {
                stems.Tissue[t].DM *= fracRemaining;
                stems.Tissue[t].Namount *= fracRemaining;
                //                stems.Tissue[t].NRemobilisable *= fracRemaining;
            }
            fracRemaining = Math.Max(0.0, 1.0 - MathUtilities.Divide(amountToRemove * fracRemoving[4], stems.DMDead, 0.0));
            stems.Tissue[t].DM *= fracRemaining;
            stems.Tissue[t].Namount *= fracRemaining;
            //            stems.Tissue[t].NRemobilisable *= fracRemaining;

            // Stolons
            fracRemaining = Math.Max(0.0, 1.0 - MathUtilities.Divide(amountToRemove * fracRemoving[2], stolons.DMLive, 0.0));
            for (t = 0; t < 3; t++)
            {
                stolons.Tissue[t].DM *= fracRemaining;
                stolons.Tissue[t].Namount *= fracRemaining;
                //                stolons.Tissue[t].NRemobilisable *= fracRemaining;
            }

            // Check balance and set outputs
            defoliatedDM = preRemovalDM - AboveGroundWt;
            defoliatedN = preRemovalN - AboveGroundN;
            if (Math.Abs(defoliatedDM - amountToRemove) > Epsilon)
                throw new Exception("  AgPasture - removal of DM resulted in loss of mass balance");
            else
                mySummary.WriteMessage(this, "Biomass removed from " + Name + " by grazing: " + defoliatedDM.ToString("#0.0") + "kg/ha");

            // Update LAI and herbage digestibility
            EvaluateLAI();
            EvaluateDigestibility();

            return defoliatedDM;
        }

        /// <summary>Removes part of the crop biomass.</summary>
        public void RemoveBiomass(string removalType, RemovalFractions removalData = null)
        {
            // Get the fractions to remove from leaves
            double[][] removalFractions = new double[4][];
            removalFractions[0] = new double[2];
            OrganBiomassRemovalType defaultFractions = leaves.GetRemovalFractions(removalType);
            OrganBiomassRemovalType userFractions = removalData.GetFractionsForOrgan("Leaves");
            if (userFractions == null)
            {
                if (defaultFractions == null)
                    throw new ApsimXException(this, "Could not find biomass removal defaults for " + removalType
                                                    + " and no removal fractions were supplied for leaves");
                else
                {
                    removalFractions[0][0] = defaultFractions.FractionLiveToRemove + defaultFractions.FractionLiveToResidue;
                    removalFractions[0][1] = defaultFractions.FractionDeadToRemove + defaultFractions.FractionDeadToResidue;
                }
            }
            else
            {
                removalFractions[0][0] = MathUtilities.Bound(userFractions.FractionLiveToRemove + userFractions.FractionLiveToResidue, 0.0, 1.0);
                removalFractions[0][1] = MathUtilities.Bound(userFractions.FractionDeadToRemove + userFractions.FractionDeadToResidue, 0.0, 1.0);
            }

            // Get the fractions to remove from stems
            removalFractions[1] = new double[2];
            defaultFractions = stems.GetRemovalFractions(removalType);
            userFractions = removalData.GetFractionsForOrgan("Stems");
            if (userFractions == null)
            {
                if (defaultFractions == null)
                    throw new ApsimXException(this, "Could not find biomass removal defaults for " + removalType
                                                    + " and no removal fractions were supplied for stems");
                else
                {
                    removalFractions[1][0] = defaultFractions.FractionLiveToRemove + defaultFractions.FractionLiveToResidue;
                    removalFractions[1][1] = defaultFractions.FractionDeadToRemove + defaultFractions.FractionDeadToResidue;
                }
            }
            else
            {
                removalFractions[1][0] = MathUtilities.Bound(userFractions.FractionLiveToRemove + userFractions.FractionLiveToResidue, 0.0, 1.0);
                removalFractions[1][1] = MathUtilities.Bound(userFractions.FractionDeadToRemove + userFractions.FractionDeadToResidue, 0.0, 1.0);
            }

            // Get the fractions to remove from stolons
            removalFractions[2] = new double[2];
            defaultFractions = stolons.GetRemovalFractions(removalType);
            userFractions = removalData.GetFractionsForOrgan("Stolons");
            if (userFractions == null)
            {
                if (defaultFractions == null)
                    throw new ApsimXException(this, "Could not find biomass removal defaults for " + removalType
                                                    + " and no removal fractions were supplied for stolons");
                else
                {
                    removalFractions[2][0] = defaultFractions.FractionLiveToRemove + defaultFractions.FractionLiveToResidue;
                    removalFractions[2][1] = defaultFractions.FractionDeadToRemove + defaultFractions.FractionDeadToResidue;
                }
            }
            else
            {
                removalFractions[2][0] = MathUtilities.Bound(userFractions.FractionLiveToRemove + userFractions.FractionLiveToResidue, 0.0, 1.0);
                removalFractions[2][1] = MathUtilities.Bound(userFractions.FractionDeadToRemove + userFractions.FractionDeadToResidue, 0.0, 1.0);
            }

            // Get the total amount required to remove
            double amountToRemove = (leaves.DMLiveHarvestable - leaves.MinimumLiveDM) * removalFractions[0][0];
            amountToRemove += leaves.DMDeadHarvestable * removalFractions[0][1];
            amountToRemove += (stems.DMLiveHarvestable - stems.MinimumLiveDM) * removalFractions[1][0];
            amountToRemove += stems.DMDeadHarvestable * removalFractions[1][1];
            amountToRemove += (stolons.DMLiveHarvestable - stolons.MinimumLiveDM * stolons.FractionStanding) * removalFractions[2][0];
            amountToRemove += stolons.DMDeadHarvestable * removalFractions[2][1];

            // get digestibility of DM being harvested (do this before updating pools)
            double greenDigestibility = (leaves.DigestibilityLive * removalFractions[0][0]) + (stems.DigestibilityLive * removalFractions[1][0])
                                        + (stolons.DigestibilityLive * removalFractions[2][0]);
            double deadDigestibility = (leaves.DigestibilityDead * removalFractions[0][1]) + (stems.DigestibilityDead * removalFractions[1][1]);
            defoliatedDigestibility = greenDigestibility + deadDigestibility;

            // Remove the biomass
            double preRemovalDM = AboveGroundWt;
            double preRemovalN = AboveGroundN;
            DoRemoveBiomass(removalFractions);

            // Check balance and set outputs
            defoliatedDM = preRemovalDM - AboveGroundWt;
            defoliatedN = preRemovalN - AboveGroundN;
            if (Math.Abs(defoliatedDM - amountToRemove) > Epsilon)
                throw new Exception("  AgPasture - biomass removal resulted in loss of mass balance");
            else
                mySummary.WriteMessage(this, "Biomass removed from " + Name + " by " + removalType + "ing: " + defoliatedDM.ToString("#0.0") + "kg/ha");

            // Update LAI and herbage digestibility
            EvaluateLAI();
            EvaluateDigestibility();
        }

        /// <summary>Removes given fractions of bioamss from each organ</summary>
        /// <param name="fractionToRemove">Fractions to remove</param>
        private void DoRemoveBiomass(double[][] fractionToRemove)
        {
            // Leaves, live and dead
            double fracRemaining = Math.Max(0.0, 1.0 - fractionToRemove[0][0]);
            int t;
            for (t = 0; t < 3; t++)
            {
                leaves.Tissue[t].DM *= fracRemaining;
                leaves.Tissue[t].Namount *= fracRemaining;
                //                leaves.Tissue[t].NRemobilisable *= fracRemaining;
            }
            fracRemaining = Math.Max(0.0, 1.0 - fractionToRemove[0][1]);
            leaves.Tissue[t].DM *= fracRemaining;
            leaves.Tissue[t].Namount *= fracRemaining;
            //            leaves.Tissue[t].NRemobilisable *= fracRemaining;

            // Stems, live and dead
            fracRemaining = Math.Max(0.0, 1.0 - fractionToRemove[1][0]);
            for (t = 0; t < 3; t++)
            {
                stems.Tissue[t].DM *= fracRemaining;
                stems.Tissue[t].Namount *= fracRemaining;
                //                stems.Tissue[t].NRemobilisable *= fracRemaining;
            }
            fracRemaining = Math.Max(0.0, 1.0 - fractionToRemove[1][1]);
            stems.Tissue[t].DM *= fracRemaining;
            stems.Tissue[t].Namount *= fracRemaining;
            //            stems.Tissue[t].NRemobilisable *= fracRemaining;

            // Stolons, live only
            fracRemaining = Math.Max(0.0, 1.0 - fractionToRemove[1][0]);
            for (t = 0; t < 3; t++)
            {
                stolons.Tissue[t].DM *= fracRemaining;
                stolons.Tissue[t].Namount *= fracRemaining;
                //                stolons.Tissue[t].NRemobilisable *= fracRemaining;
            }
        }

        /// <summary>Resets this plant state to its initial values.</summary>
        public void Reset()
        {
            leaves.DoResetOrgan();
            stems.DoResetOrgan();
            stolons.DoResetOrgan();
            roots.DoResetOrgan();
            SetInitialState();
        }

        /// <summary>Kill parts of this plant.</summary>
        /// <param name="fractioToKill">Fraction of crop to be killed (0-1)</param>
        public void KillCrop(double fractioToKill)
        {
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

        /// <summary>Resets this plant's variables to zero.</summary>
        public void ResetZero()
        {
            // Zero out the DM and N pools is all organs and tissues
            leaves.DoResetOrgan();
            stems.DoResetOrgan();
            stolons.DoResetOrgan();
            roots.DoResetOrgan();

            // Zero out the variables for whole plant
            defoliatedDM = 0.0;
            defoliatedN = 0.0;
            defoliatedDigestibility = 0.0;

            dGrowthShootDM = 0.0;
            dGrowthShootN = 0.0;
            dGrowthRootDM = 0.0;
            dGrowthRootN = 0.0;

            detachedShootDM = 0.0;
            detachedShootN = 0.0;
            detachedRootDM = 0.0;
            detachedRootN = 0.0;

            senescedNRemobilised = 0.0;
            luxuryNRemobilised = 0.0;
        }

        #endregion  --------------------------------------------------------------------------------------------------------

        #region Functions  -------------------------------------------------------------------------------------------------

        /// <summary>Today's weighted average temperature.</summary>
        /// <param name="wTmax">Weight to Tmax</param>
        /// <returns>Average Temperature (oC)</returns>
        private double Tmean(double wTmax)
        {
            wTmax = MathUtilities.Bound(wTmax, 0.0, 1.0);
            return (myMetData.MaxT * wTmax) + (myMetData.MinT * (1.0 - wTmax));
        }

        /// <summary>Growth limiting factor due to temperature.</summary>
        /// <param name="temperature">Temperature for which the limiting factor will be computed</param>
        /// <returns>The value for the limiting factor (0-1)</returns>
        /// <exception cref="System.Exception">Photosynthesis pathway is not valid</exception>
        private double TemperatureLimitingFactor(double temperature)
        {
            double result = 0.0;
            double growthTmax = myGrowthToptimum + (myGrowthToptimum - myGrowthTminimum) / myGrowthTq;
            if (myPhotosynthesisPathway == PhotosynthesisPathwayType.C3)
            {
                if (temperature > myGrowthTminimum && temperature < growthTmax)
                {
                    double val1 = Math.Pow((temperature - myGrowthTminimum), myGrowthTq) * (growthTmax - temperature);
                    double val2 = Math.Pow((myGrowthToptimum - myGrowthTminimum), myGrowthTq) * (growthTmax - myGrowthToptimum);
                    result = val1 / val2;
                }
            }
            else if (myPhotosynthesisPathway == PhotosynthesisPathwayType.C4)
            {
                if (temperature > myGrowthTminimum)
                {
                    if (temperature > myGrowthToptimum)
                        temperature = myGrowthToptimum;

                    double val1 = Math.Pow((temperature - myGrowthTminimum), myGrowthTq) * (growthTmax - temperature);
                    double val2 = Math.Pow((myGrowthToptimum - myGrowthTminimum), myGrowthTq) * (growthTmax - myGrowthToptimum);
                    result = val1 / val2;
                }
            }
            else
                throw new Exception("Photosynthesis pathway is not valid");
            return result;
        }

        /// <summary>Computes the effects of temperature on respiration</summary>
        /// <returns>Temperature factor</returns>
        private double TemperatureEffectOnRespirationNew(double temperature)
        {
            double result;
            if (temperature <= 0.0)
            {
                // too cold, no respiration
                result = 0.0;
            }
            else
            {
                double scalef = 1.0 - Math.Exp(-1.0);
                double baseEffect = 1.0 - Math.Exp(-Math.Pow(temperature / myRespirationTreference, myRespirationExponent));
                result = baseEffect / scalef;
            }

            return result;
        }

        /// <summary>Computes the effects of temperature on respiration</summary>
        /// <returns>Temperature factor</returns>
        private double TemperatureEffectOnRespiration(double temperature)
        {
            double Teffect = 0;
            if (temperature > myGrowthTminimum)
            {
                if (Tmean(0.5) < myGrowthToptimum)
                {
                    Teffect = TemperatureLimitingFactor(temperature);
                }
                else
                {
                    Teffect = Math.Min(1.25, Tmean(0.5) / myGrowthToptimum);
                    // Using growthTopt as reference temperature, and maximum of 1.25
                    Teffect *= TemperatureLimitingFactor(myGrowthToptimum);
                }
            }

            return Teffect;
        }

        /// <summary>Effect of temperature on tissue turnover.</summary>
        /// <param name="temperature">The temporary.</param>
        /// <returns>Temperature factor (0-1)</returns>
        private double TempFactorForTissueTurnover(double temperature)
        {
            double result = 0.0;
            if (temperature > myTissueTurnoverTmin && temperature <= myTissueTurnoverTref)
            {
                result = Math.Pow((temperature - myTissueTurnoverTmin) / (myTissueTurnoverTref - myTissueTurnoverTmin), myTissueTurnoverTq);
            }
            else if (temperature > myTissueTurnoverTref)
            {
                result = 1.0;
            }
            return result;
        }

        /// <summary>Computes the reduction factor for photosynthesis due to heat damage.</summary>
        /// <remarks>Stress computed as function of daily maximum temperature, recovery based on average temp.</remarks>
        /// <returns>The reduction in photosynthesis rate (0-1)</returns>
        private double HeatStress()
        {
            if (usingHeatStressFactor)
            {
                double heatFactor;
                if (myMetData.MaxT > myHeatFullTemperature)
                {
                    // very high temperature, full stress
                    heatFactor = 0.0;
                    accumDDHeat = 0.0;
                }
                else if (myMetData.MaxT > myHeatOnsetTemperature)
                {
                    // high temperature, add some stress
                    heatFactor = highTempStress * (myHeatFullTemperature - myMetData.MaxT) / (myHeatFullTemperature - myHeatOnsetTemperature);
                    accumDDHeat = 0.0;
                }
                else
                {
                    // cool temperature, same stress as yesterday
                    heatFactor = highTempStress;
                }

                // check recovery factor
                double recoveryFactor = 0.0;
                if (myMetData.MaxT <= myHeatOnsetTemperature)
                    recoveryFactor = (1.0 - heatFactor) * (accumDDHeat / myHeatRecoverySumDD);

                // accumulate temperature
                accumDDHeat += Math.Max(0.0, myHeatRecoveryTreference - Tmean(0.5));

                // heat stress
                highTempStress = Math.Min(1.0, heatFactor + recoveryFactor);

                return highTempStress;
            }
            return 1.0;
        }

        /// <summary>Computes the reduction factor for photosynthesis due to cold damage (frost).</summary>
        /// <remarks>Stress computed as function of daily minimum temperature, recovery based on average temp.</remarks>
        /// <returns>The reduction in photosynthesis rate (0-1)</returns>
        private double ColdStress()
        {
            if (usingColdStressFactor)
            {
                double coldFactor;
                if (myMetData.MinT < myColdFullTemperature)
                {
                    // very low temperature, full stress
                    coldFactor = 0.0;
                    accumDDCold = 0.0;
                }
                else if (myMetData.MinT < myColdOnsetTemperature)
                {
                    // low temperature, add some stress
                    coldFactor = lowTempStress * (myMetData.MinT - myColdFullTemperature) / (myColdOnsetTemperature - myColdFullTemperature);
                    accumDDCold = 0.0;
                }
                else
                {
                    // warm temperature, same stress as yesterday
                    coldFactor = lowTempStress;
                }

                // check recovery factor
                double recoveryFactor = 0.0;
                if (myMetData.MinT >= myColdOnsetTemperature)
                    recoveryFactor = (1.0 - coldFactor) * (accumDDCold / myColdRecoverySumDD);

                // accumulate temperature
                accumDDCold += Math.Max(0.0, Tmean(0.5) - myColdRecoveryTreference);

                // cold stress
                lowTempStress = Math.Min(1.0, coldFactor + recoveryFactor);

                return lowTempStress;
            }
            else
                return 1.0;
        }

        /// <summary>Computes the relative effect of atmospheric CO2 on photosynthesis.</summary>
        /// <returns>A factor to adjust photosynthesis due to CO2 (0-1)</returns>
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
        /// <returns>A factor to adjust photosynthesis due to N concentration (0-1)</returns>
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

        /// <summary>Computes the variation in optimum N in leaves due to atmospheric CO2.</summary>
        /// <returns>A factor to adjust optimum N in leaves (0-1)</returns>
        private double NFactorDueToCO2()
        {
            if (Math.Abs(myMetData.CO2 - ReferenceCO2) < 0.01)
                return 1.0;

            double factorCO2 = Math.Pow((CO2EffectOffsetFactor - ReferenceCO2) / (myMetData.CO2 - ReferenceCO2), CO2EffectExponent);
            double effect = (CO2EffectMinimum + factorCO2) / (1 + factorCO2);

            return effect;
        }

        /// <summary>Computes the variation in stomata conductances due to variation in atmospheric CO2.</summary>
        /// <returns>Stomata conductuctance (m/s)</returns>
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

        /// <summary>Computes the growth limiting factor due to soil moisture deficit.</summary>
        /// <returns>The limiting factor due to soil water deficit (0-1)</returns>
        internal double WaterDeficitFactor()
        {
            double factor = MathUtilities.Divide(mySoilWaterUptake.Sum(), myWaterDemand, 1.0);
            return Math.Max(0.0, Math.Min(1.0, factor));
        }

        /// <summary>Computes the growth limiting factor due to excess of water in the soil (water logging/lack of aeration).</summary>
        /// <remarks>
        /// Growth is limited if soil water content is above a given threshold (defined by MinimumWaterFreePorosity), which
        ///  will be the soil DUL is MinimumWaterFreePorosity is set to a negative value. If usingCumulativeWaterLogging, 
        ///  growth is limited by the cumulative value of the logging effect, with maximum increment in one day equal to 
        ///  SoilWaterSaturationFactor. Recovery happens if water content is below the threshold set by MinimumWaterFreePorosity
        /// </remarks>
        /// <returns>The limiting factor due to excess in soil water (0-1)</returns>
        internal double WaterLoggingFactor()
        {
            double effect;
            double mySWater = 0.0;
            double mySAT = 0.0;
            double myDUL = 0.0;
            double fractionLayer;

            // gather water status over the root zone
            for (int layer = 0; layer <= roots.BottomLayer; layer++)
            {
                // fraction of layer with roots 
                fractionLayer = FractionLayerWithRoots(layer);
                // actual soil water content
                mySWater += mySoil.Water[layer] * fractionLayer;
                // water content at saturation
                mySAT += mySoil.SoilWater.SATmm[layer] * fractionLayer;
                // water content at low threshold for limitation (correspond to minimum water-free pore space)
                if (myMinimumWaterFreePorosity <= -Epsilon)
                    myDUL += mySoil.SoilWater.DULmm[layer] * fractionLayer;
                else
                    myDUL = mySoil.SoilWater.SATmm[layer] * (1.0 - myMinimumWaterFreePorosity) * fractionLayer;
            }

            if (mySWater > myDUL)
                effect = mySoilWaterSaturationFactor * (mySWater - myDUL) / (mySAT - myDUL);
            else
                effect = -SoilWaterSaturationRecoveryFactor;

            cumWaterLogging = MathUtilities.Bound(cumWaterLogging + effect, 0.0, 1.0);

            if (usingCumulativeWaterLogging)
                effect = 1.0 - cumWaterLogging;
            else
                effect = MathUtilities.Bound(1.0 - effect, 0.0, 1.0);

            return effect;
        }

        /// <summary>Computes the effect of water stress on tissue turnover.</summary>
        /// <remarks>Tissue turnover is higher under water stress, GLFwater is used to mimic that effect.</remarks>
        /// <returns>Water stress factor for tissue turnover (0-1)</returns>
        private double WaterFactorForTissueTurnover()
        {
            double effect = 1.0;
            if (Math.Min(glfWater, glfAeration) < myTissueTurnoverDroughtThreshold)
            {
                effect = (myTissueTurnoverDroughtThreshold - Math.Min(glfWater, glfAeration)) / myTissueTurnoverDroughtThreshold;
                effect = 1.0 + myTissueTurnoverDroughtMax * effect;
            }

            return effect;
        }

        /// <summary>Computes the ground cover for the plant, or plant part.</summary>
        /// <param name="givenLAI">The LAI for this plant</param>
        /// <returns>Fraction of ground effectively covered (0-1)</returns>
        private double CalcPlantCover(double givenLAI)
        {
            if (givenLAI < Epsilon) return 0.0;
            return (1.0 - Math.Exp(-myLightExtentionCoefficient * givenLAI));
        }

        /// <summary>Computes the target (or ideal) distribution of roots in the soil profile.</summary>
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
            SoilCrop soilCropData = (SoilCrop)mySoil.Crop(Name);
            double depthTop = 0.0;
            double depthBottom = 0.0;
            double depthFirstStage = Math.Min(myMaximumRootDepth, myDepthForConstantRootProportion);

            for (int layer = 0; layer < nLayers; layer++)
            {
                depthBottom += mySoil.Thickness[layer];
                if (depthTop >= myMaximumRootDepth)
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
                    double maxRootDepth = myMaximumRootDepth * rootBottomDistributionFactor;
                    result[layer] = Math.Pow(maxRootDepth - Math.Max(depthTop, depthFirstStage), myExponentRootDistribution + 1)
                                  - Math.Pow(maxRootDepth - Math.Min(depthBottom, myMaximumRootDepth), myExponentRootDistribution + 1);
                    result[layer] /= (myExponentRootDistribution + 1) * Math.Pow(maxRootDepth - depthFirstStage, myExponentRootDistribution);
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

        /// <summary>Computes the current target distribution of roots in the soil profile.</summary>
        /// <remarks>
        /// This distribution is a correction of the target distribution, taking into account the depth of soil
        /// as well as the current rooting depth
        /// </remarks>
        /// <returns>The proportion of root mass expected in each soil layer (0-1)</returns>
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
                                         / (myMaximumRootDepth - (currentDepth - mySoil.Thickness[layer]));
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

        /// <summary>Computes how much of the layer is actually explored by roots (considering depth only).</summary>
        /// <param name="layer">The index for the layer being considered</param>
        /// <returns>Fraction of the layer in consideration that is explored by roots (0-1)</returns>
        internal double FractionLayerWithRoots(int layer)
        {
            double fractionInLayer = 0.0;
            if (layer < roots.BottomLayer)
            {
                fractionInLayer = 1.0;
            }
            else if (layer == roots.BottomLayer)
            {
                double depthTillTopThisLayer = 0.0;
                for (int z = 0; z < layer; z++)
                    depthTillTopThisLayer += mySoil.Thickness[z];
                fractionInLayer = (roots.Depth - depthTillTopThisLayer) / mySoil.Thickness[layer];
                fractionInLayer = Math.Min(1.0, Math.Max(0.0, fractionInLayer));
            }

            return fractionInLayer;
        }

        /// <summary>Computes the index of the layer at the bottom of the root zone</summary>
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

        /// <summary>Computes the vapour pressure deficit.</summary>
        /// <returns>The vapour pressure deficit (hPa?)</returns>
        private double VPD()
        {
            //TODO: this can possibly be deleted (not use and calculated in MicroClimate)
            double VPDmint = svp(myMetData.MinT) - myMetData.VP;
            VPDmint = Math.Max(VPDmint, 0.0);

            double VPDmaxt = svp(myMetData.MaxT) - myMetData.VP;
            VPDmaxt = Math.Max(VPDmaxt, 0.0);

            double vdp = 0.66 * VPDmaxt + 0.34 * VPDmint;
            return vdp;
        }

        /// <summary>Saturate vapour pressure in the air.</summary>
        /// <param name="temp">The air temperature (oC)</param>
        /// <returns>The saturated vapour presure (hPa?)</returns>
        private double svp(double temp)
        {
            return 6.1078 * Math.Exp(17.269 * temp / (237.3 + temp));
        }

        #endregion  --------------------------------------------------------------------------------------------------------

        #region Auxiliary classes  ------------------------------------------------------------------------------------

        /// <summary>Basic values defining the state of a pasture species</summary>
        [Serializable]
        internal class SpeciesBasicStateSettings
        {
            /// <summary>Plant phenologic stage</summary>
            internal int PhenoStage;

            /// <summary>DM weight for each biomass pool (kg/ha)</summary>
            internal double[] DMWeight;

            /// <summary>N amount for each biomass pool (kg/ha)</summary>
            internal double[] NAmount;

            /// <summary>Root depth (mm)</summary>
            internal double RootDepth;

            /// <summary>Constructor, initialise the arrays</summary>
            public SpeciesBasicStateSettings()
            {
                // there are 12 tissue pools, in order: leaf1, leaf2, leaf3, leaf4, stem1, stem2, stem3, stem4, stolon1, stolon2, stolon3, and root
                DMWeight = new double[12];
                NAmount = new double[12];
            }
        }

        #endregion  --------------------------------------------------------------------------------------------------------
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
            bool didInterpolate;
            return MathUtilities.LinearInterpReal(newX, X, Y, out didInterpolate);
        }
    }
}
