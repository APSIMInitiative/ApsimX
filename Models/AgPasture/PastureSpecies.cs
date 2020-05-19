namespace Models.AgPasture
{
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
    using Models.Functions;
    using Models.PMF.Interfaces;
    using Models.Surface;

    /// <summary>
    /// # [Name]
    /// Describes a pasture species.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Zone))]
    public class PastureSpecies : ModelCollectionFromResource, IPlant, ICanopy, IUptake, IPlantDamage
    {
        #region Links, events and delegates  -------------------------------------------------------------------------------

        ////- Links >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Link to APSIM's Clock (provides time information).</summary>
        [Link]
        private Clock myClock = null;

        /// <summary>Link to the zone this pasture species resides in.</summary>
        [Link]
        private Zone zone = null;

        /// <summary>Link to APSIM's WeatherFile (provides meteorological information).</summary>
        [Link]
        private IWeather myMetData = null;

        /// <summary>Link to APSIM summary (logs the messages raised during model run).</summary>
        [Link]
        private ISummary mySummary = null;

        /// <summary>Link to the Soil (provides soil information).</summary>
        [Link]
        private Soil mySoil = null;

        /// <summary>Link to Apsim's Resource Arbitrator module.</summary>
        [Link(IsOptional = true)]
        private SoilArbitrator soilArbitrator = null;

        ////- Events >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Invoked for incorporating surface OM.</summary>
        /// <param name="Data">The data about biomass deposited by this plant onto the soil surface</param>
        public delegate void BiomassRemovedDelegate(BiomassRemovedType Data);

        /// <summary>Occurs when plant is detaching dead tissues, litter.</summary>
        public event BiomassRemovedDelegate BiomassRemoved;

        #endregion  --------------------------------------------------------------------------------------------------------  --------------------------------------------------------------------------------------------------------

        #region ICanopy implementation  ------------------------------------------------------------------------------------

        /// <summary>Canopy albedo for this plant (0-1).</summary>
        private double myAlbedo = 0.26;

        /// <summary>Gets or sets the canopy albedo for this plant (0-1).</summary>
        [Units("0-1")]
        public double Albedo
        {
            get { return myAlbedo; }
            set { myAlbedo = value; }
        }

        /// <summary>Maximum stomatal conductance (m/s).</summary>
        private double myGsmax = 0.011;

        /// <summary>Gets or sets the  maximum stomatal conductance (m/s).</summary>
        [Units("m/s")]
        public double Gsmax
        {
            get { return myGsmax; }
            set { myGsmax = value; }
        }

        /// <summary>Solar radiation at which stomatal conductance decreases to 50% (W/m^2).</summary>
        private double myR50 = 200;

        /// <summary>Gets or sets the R50 factor (W/m^2).</summary>
        [Units("W/m^2")]
        public double R50
        {
            get { return myR50; }
            set { myR50 = value; }
        }

        /// <summary>Gets the LAI of live tissues (m^2/m^2).</summary>
        //[Description("Leaf area index of green tissues")]
        [Units("m^2/m^2")]
        [XmlIgnore]
        public double LAI
        {
            get 
            { 
                return LAIGreen; 
            }
            set
            {
                LAIGreen = value;
            }
        }

        /// <summary>Gets the total LAI, live + dead (m^2/m^2).</summary>
        //[Description("Total leaf area index")]
        [Units("m^2/m^2")]
        [XmlIgnore]
        public double LAITotal
        {
            get { return LAIGreen + LAIDead; }
        }

        /// <summary>Gets the plant's green cover (0-1).</summary>
        //[Description("Fraction of soil covered by green tissues")]
        [Units("0-1")]
        [XmlIgnore]
        public double CoverGreen
        {
            get { return CalcPlantCover(greenLAI); }
        }

        /// <summary>Gets the total plant cover (0-1).</summary>
        //[Description("Fraction of soil covered by plant tissues")]
        [Units("0-1")]
        [XmlIgnore]
        public double CoverTotal
        {
            get { return CalcPlantCover(greenLAI + deadLAI); }
        }

        /// <summary>Gets the average canopy height (mm).</summary>
        //[Description("Average canopy height")]
        [Units("mm")]
        [XmlIgnore]
        public double Height
        {
            get { return HeightfromDM(); }
        }

        /// <summary>Gets the canopy depth (mm).</summary>
        //[Description("The depth of the canopy")]
        [Units("mm")]
        [XmlIgnore]
        public double Depth
        {
            get { return Height; }
        }

        /// <summary>Gets the width of the canopy (mm).</summary>
        //[Description("The width of the canopy")]
        [Units("mm")]
        [XmlIgnore]
        public double Width
        {
            get { return 0; }
        }

        // TODO: have to verify how this works (what exactly is needed by MicroClimate
        /// <summary>Plant growth limiting factor, supplied to MicroClimate for calculating potential transpiration.</summary>
        //[Description("General growth limiting factor (for MicroClimate)")]
        [Units("0-1")]
        [XmlIgnore]
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

                // (RCichota, May-2017) Made intercepted radiation equal to solar radiation and implemented the variable 'effective cover'.
                // To compute photosynthesis AgPasture needs radiation on top of canopy, but MicroClimate only passes the value of total
                //  intercepted radiation (over all canopy). Here it is assumed/defined that solar radiation is indeed the best value for
                //  AgPasture to use in its photosynthesis (agrees with the implementation in Ecomod).
                // The 'effective cover' is computed using an 'effective light extinction coefficient', which is obtained based on the 
                //  value for intercepted radiation supplied by MicroClimate. This is the light extinction coefficient that result in the
                //  same total intercepted radiation, but using solar radiation on top of canopy.
                //  (note that this value is only used in the calculation of photosynthesis).
                // TODO: this approach may have to be amended when multi-layer canopies are used (the thought behind the approach here
                //  is that things like shading (which would reduce Radn on top of canopy) are irrelevant).
                RadiationTopOfCanopy = myMetData.Radn;
                effectiveGreenCover = 0.0;
                if (RadiationTopOfCanopy > 0.0)
                {
                    double AuxVar = 0.0;
                    if (InterceptedRadn < RadiationTopOfCanopy)
                        AuxVar = Math.Log(1.0 - InterceptedRadn / RadiationTopOfCanopy);
                    double myEffectiveLightExtinctionCoefficient = MathUtilities.Divide(-AuxVar, greenLAI, 0.0);
                    if (myEffectiveLightExtinctionCoefficient * greenLAI > Epsilon)
                        effectiveGreenCover = 1.0 - Math.Exp(-myEffectiveLightExtinctionCoefficient * greenLAI);
                }
            }
        }

        #endregion  --------------------------------------------------------------------------------------------------------

        #region ICrop implementation  --------------------------------------------------------------------------------------

        /// <summary>Gets a value indicating how leguminous a plant is</summary>
        [XmlIgnore]
        public double Legumosity
        {
            get
            {
                if (SpeciesFamily == PastureSpecies.PlantFamilyType.Legume)
                    return 1;
                else
                    return 0;
            }
        }

        /// <summary>Gets a value indicating whether the biomass is from a c4 plant or not</summary>
        [XmlIgnore]
        public bool IsC4 { get { return PhotosyntheticPathway == PastureSpecies.PhotosynthesisPathwayType.C4; } }

        /// <summary>Gets a list of cultivar names (not used by AgPasture).</summary>
        public string[] CultivarNames
        {
            get { return null; }
        }

        /// <summary>Sows the plant.</summary>
        /// <param name="cultivar">The cultivar type</param>
        /// <param name="population">The number of plants per area</param>
        /// <param name="depth">The sowing depth</param>
        /// <param name="rowSpacing">The space between rows</param>
        /// <param name="maxCover">The maximum ground cover (optional)</param>
        /// <param name="budNumber">The number of buds (optional)</param>
        /// <param name="rowConfig">The row configuration.</param>
        /// <remarks>
        /// For AgPasture species the sow parameters are not used, the command to sow simply enables the plant to grow. This is done
        /// by setting the plant status to 'alive'. From this point germination processes takes place and eventually emergence occurs.
        /// At emergence, plant DM is set to its default minimum value, allocated according to EmergenceFractions and with
        /// optimum N concentration. Plant height and root depth are set to their minimum values.
        /// </remarks>
        public void Sow(string cultivar, double population, double depth, double rowSpacing, double maxCover = 1, double budNumber = 1, double rowConfig = 1)
        {
            if (isAlive)
                mySummary.WriteWarning(this, " Cannot sow the pasture species \"" + Name + "\", as it is already growing");
            else
            {
                RefreshVariables();
                isAlive = true;
                phenologicStage = 0;
                mySummary.WriteMessage(this, " The pasture species \"" + Name + "\" has been sown today");
            }
        }

        /// <summary>Flag whether the crop is ready for harvesting.</summary>
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
            DoAddDetachedShootToSurfaceOM(AboveGroundWt, AboveGroundN);

            // Incorporate all root mass to soil fresh organic matter
            foreach (PastureBelowGroundOrgan root in root)
                root.DoEndCrop();

            // zero all variables
            RefreshVariables();
            leaf.DoResetOrgan();
            stem.DoResetOrgan();
            stolon.DoResetOrgan();
            foreach (PastureBelowGroundOrgan root in root)
                root.DoResetOrgan();

            // clean up secondary variables
            greenLAI = 0.0;
            deadLAI = 0.0;

            isAlive = false;
            phenologicStage = -1;
        }

        #endregion  --------------------------------------------------------------------------------------------------------

        #region IUptake implementation  ------------------------------------------------------------------------------------

        /// <summary>Gets the potential plant water uptake for each layer (mm).</summary>
        /// <remarks>The model can only handle one root zone at present.</remarks>
        /// <param name="soilstate">The soil state (current water content)</param>
        /// <returns>The potential water uptake (mm)</returns>
        public List<ZoneWaterAndN> GetWaterUptakeEstimates(SoilState soilstate)
        {
            if (IsAlive)
            {
                // Get all water supplies.
                double waterSupply = 0;  //NOTE: This is in L, not mm, to arbitrate water demands for spatial simulations.

                List<double[]> supplies = new List<double[]>();
                List<Zone> zones = new List<Zone>();
                foreach (ZoneWaterAndN zone in soilstate.Zones)
                {
                    // Find the zone in our root zones.
                    PastureBelowGroundOrgan myRoot = root.Find(root => root.myZoneName == zone.Zone.Name);
                    if (myRoot != null)
                    {
                        double[] organSupply = myRoot.EvaluateSoilWaterAvailable(zone);
                        if (organSupply != null)
                        {
                            supplies.Add(organSupply);
                            zones.Add(zone.Zone);
                            waterSupply += MathUtilities.Sum(organSupply) * zone.Zone.Area;
                        }
                    }
                }

                // 2. Get the amount of soil water demanded NOTE: This is in L, not mm,
                Zone parentZone = Apsim.Parent(this, typeof(Zone)) as Zone;
                double waterDemand = myWaterDemand * parentZone.Area;

                // 3. Estimate fraction of water used up
                double fractionUsed = 0.0;
                if (waterSupply > Epsilon)
                    fractionUsed = Math.Min(1.0, waterDemand / waterSupply);

                // Apply demand supply ratio to each zone and create a ZoneWaterAndN structure
                // to return to caller.
                List<ZoneWaterAndN> ZWNs = new List<ZoneWaterAndN>();
                for (int i = 0; i < supplies.Count; i++)
                {
                    // Just send uptake from my zone
                    ZoneWaterAndN uptake = new ZoneWaterAndN(zones[i]);
                    uptake.Water = MathUtilities.Multiply_Value(supplies[i], fractionUsed);
                    uptake.NO3N = new double[uptake.Water.Length];
                    uptake.NH4N = new double[uptake.Water.Length];
                    uptake.PlantAvailableNO3N = new double[uptake.Water.Length];
                    uptake.PlantAvailableNH4N = new double[uptake.Water.Length];
                    ZWNs.Add(uptake);
                }
                return ZWNs;
            }
            else
                return null;
        }

        /// <summary>Gets the potential plant N uptake for each layer (mm).</summary>
        /// <remarks>The model can only handle one root zone at present.</remarks>
        /// <param name="soilstate">The soil state (current N contents)</param>
        /// <returns>The potential N uptake (kg/ha)</returns>
        public List<ZoneWaterAndN> GetNitrogenUptakeEstimates(SoilState soilstate)
        {
            if (IsAlive)
            {
                double NSupply = 0;//NOTE: This is in kg, not kg/ha, to arbitrate N demands for spatial simulations.

                List<ZoneWaterAndN> zones = new List<ZoneWaterAndN>();

                // Get the zone this plant is in
                Zone parentZone = Apsim.Parent(this, typeof(Zone)) as Zone;
                foreach (ZoneWaterAndN zone in soilstate.Zones)
                {
                    PastureBelowGroundOrgan myRoot = root.Find(root => root.myZoneName == zone.Zone.Name);
                    if (myRoot != null)
                    {
                        ZoneWaterAndN UptakeDemands = new ZoneWaterAndN(zone.Zone);
                        zones.Add(UptakeDemands);

                        // Get the N amount available in the soil
                        myRoot.EvaluateSoilNitrogenAvailable(zone, mySoilWaterUptake);

                        UptakeDemands.NO3N = myRoot.mySoilNO3Available;
                        UptakeDemands.NH4N = myRoot.mySoilNH4Available;
                        UptakeDemands.PlantAvailableNO3N = new double[zone.NO3N.Length];
                        UptakeDemands.PlantAvailableNH4N = new double[zone.NO3N.Length];
                        UptakeDemands.Water = new double[zone.NO3N.Length];

                        NSupply += (MathUtilities.Sum(myRoot.mySoilNH4Available) + MathUtilities.Sum(myRoot.mySoilNO3Available)) * zone.Zone.Area;
                    }
                }

                // Get the N amount fixed through symbiosis - calculates fixedN
                EvaluateNitrogenFixation();

                // Evaluate the use of N remobilised and get N amount demanded from soil
                EvaluateSoilNitrogenDemand();

                // 2. Get the amount of soil N demanded
                double NDemand = mySoilNDemand * parentZone.Area; //NOTE: This is in kg, not kg/ha, to arbitrate N demands for spatial simulations.

                // 3. Estimate fraction of N used up
                double fractionUsed = 0.0;
                if (NSupply > Epsilon)
                    fractionUsed = Math.Min(1.0, NDemand / NSupply);

                mySoilNH4Uptake = MathUtilities.Multiply_Value(mySoilNH4Available, fractionUsed);
                mySoilNO3Uptake = MathUtilities.Multiply_Value(mySoilNO3Available, fractionUsed);

                //Reduce the PotentialUptakes that we pass to the soil arbitrator
                foreach (ZoneWaterAndN UptakeDemands in zones)
                {
                    UptakeDemands.NO3N = MathUtilities.Multiply_Value(UptakeDemands.NO3N, fractionUsed);
                    UptakeDemands.NH4N = MathUtilities.Multiply_Value(UptakeDemands.NH4N, fractionUsed);
                }

                return zones;
            }
            else
                return null;
        }

        /// <summary>Sets the amount of water taken up by this plant (mm).</summary>
        /// <remarks>The model can only handle one root zone at present.</remarks>
        /// <param name="zones">The water uptake from each layer (mm), by zone</param>
        public void SetActualWaterUptake(List<ZoneWaterAndN> zones)
        {
            Array.Clear(mySoilWaterUptake, 0, mySoilWaterUptake.Length);

            foreach (ZoneWaterAndN zone in zones)
            {
                // Find the zone in our root zones.
                PastureBelowGroundOrgan myRoot = root.Find(root => root.myZoneName == zone.Zone.Name);
                if (myRoot != null)
                {
                    mySoilWaterUptake = MathUtilities.Add(mySoilWaterUptake, zone.Water);

                    if (mySoilWaterUptake.Sum() > Epsilon)
                        myRoot.mySoil.SoilWater.RemoveWater(zone.Water);
                }
            }
        }

        /// <summary>Sets the amount of N taken up by this plant (kg/ha).</summary>
        /// <remarks>The model can only handle one root zone at present.</remarks>
        /// <param name="zones">The N uptake from each layer (kg/ha), by zone</param>
        public void SetActualNitrogenUptakes(List<ZoneWaterAndN> zones)
        {
            Array.Clear(mySoilNH4Uptake, 0, mySoilNH4Uptake.Length);
            Array.Clear(mySoilNO3Uptake, 0, mySoilNO3Uptake.Length);

            foreach (ZoneWaterAndN zone in zones)
            {
                PastureBelowGroundOrgan myRoot = root.Find(root => root.myZoneName == zone.Zone.Name);
                if (myRoot != null)
                {
                    myRoot.NO3.SetKgHa(SoluteSetterType.Plant, MathUtilities.Subtract(myRoot.NO3.kgha, zone.NO3N));
                    myRoot.NH4.SetKgHa(SoluteSetterType.Plant, MathUtilities.Subtract(myRoot.NH4.kgha, zone.NH4N));

                    mySoilNH4Uptake = MathUtilities.Add(mySoilNH4Uptake, zone.NH4N);
                    mySoilNO3Uptake = MathUtilities.Add(mySoilNO3Uptake, zone.NO3N);
                }
            }
        }

        #endregion  --------------------------------------------------------------------------------------------------------

        #region Model parameters  ------------------------------------------------------------------------------------------

        // NOTE: default parameters describe a generic perennial ryegrass species

        ////- General parameters (name and type) >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Family type for this plant species (grass/legume/forb).</summary>
        private PlantFamilyType mySpeciesFamily = PlantFamilyType.Grass;

        /// <summary>Gets or sets the family type for this plant species (grass/legume/forb).</summary>
        //[Description("Family type for this plant species [grass/legume/forb]:")]
        [Units("-")]
        public PlantFamilyType SpeciesFamily
        {
            get { return mySpeciesFamily; }
            set
            {
                mySpeciesFamily = value;
                isLegume = mySpeciesFamily == PlantFamilyType.Legume;
            }
        }

        /// <summary>Species metabolic pathway of C fixation during photosynthesys (C3/C4).</summary>
        public PhotosynthesisPathwayType PhotosyntheticPathway { get; set; } = PhotosynthesisPathwayType.C3;



        ////- Initial state parameters (replace the default values) >>> - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Initial above ground DM weight (kgDM/ha).</summary>
        [Description("Initial above ground DM weight")]
        [Units("kgDM/ha")]
        public double InitialShootDM { get; set; }


        /// <summary>Initial below ground DM weight (kgDM/ha).</summary>
        [Description("Initial below ground DM weight")]
        [Units("kgDM/ha")]
        public double InitialRootDM { get; set; }



        /// <summary>Initial rooting depth (mm).</summary>
        [Description("Initial rooting depth")]
        [Units("mm")]
        public double InitialRootDepth { get; set; }



        /// <summary>Initial fractions of DM for each plant part in grasses (0-1).</summary>
        [XmlIgnore]
        public double[] initialDMFractionsGrasses { get; set; } = { 0.15, 0.25, 0.25, 0.05, 0.05, 0.10, 0.10, 0.05, 0.00, 0.00, 0.00 };

        /// <summary>Initial fractions of DM for each plant part in legumes (0-1).</summary>
        [XmlIgnore]
        public double[] initialDMFractionsLegumes { get; set; } = { 0.16, 0.23, 0.22, 0.05, 0.03, 0.05, 0.05, 0.01, 0.04, 0.08, 0.08 };

        /// <summary>Initial fractions of DM for each plant part in forbs (0-1).</summary>
        [XmlIgnore]
        public double[] initialDMFractionsForbs { get; set; } = { 0.20, 0.20, 0.15, 0.05, 0.10, 0.15, 0.10, 0.05, 0.00, 0.00, 0.00 };

        ////- Potential growth (photosynthesis) >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Reference leaf CO2 assimilation rate for photosynthesis (mg CO2/m^2Leaf/s).</summary>
        [Units("mg/m^2/s")]
        public double ReferencePhotosyntheticRate { get; set; } = 1.0;



        /// <summary>Gets or sets the leaf photosynthetic efficiency (mg CO2/J).</summary>
        [XmlIgnore]
        [Units("mg CO2/J")]
        public double PhotosyntheticEfficiency { get; set; } = 0.01;

        /// <summary>Gets or sets the photosynthesis curvature parameter (J/kg/s).</summary>
        [XmlIgnore]
        [Units("J/kg/s")]
        public double PhotosynthesisCurveFactor { get; set; } = 0.8;

        /// <summary>Gets or sets the fraction of radiation that is photosynthetically active (0-1).</summary>
        [XmlIgnore]
        [Units("0-1")]
        public double FractionPAR { get; set; } = 0.5;

        /// <summary>Light extinction coefficient (0-1).</summary>
        [Units("0-1")]
        public double LightExtinctionCoefficient { get; set; } = 0.5;



        /// <summary>Reference CO2 concentration for photosynthesis (ppm).</summary>
        [XmlIgnore]
        [Units("ppm")]
        public double ReferenceCO2 { get; set; } = 380.0;

        /// <summary>Scaling parameter for the CO2 effect on photosynthesis (ppm).</summary>
        [XmlIgnore]
        [Units("ppm")]
        public double CO2EffectScaleFactor { get; set; } = 700.0;

        /// <summary>Scaling parameter for the CO2 effects on N requirements (ppm).</summary>
        [XmlIgnore]
        [Units("ppm")]
        public double CO2EffectOffsetFactor { get; set; } = 600.0;

        /// <summary>Minimum value for the CO2 effect on N requirements (0-1).</summary>
        [XmlIgnore]
        [Units("0-1")]
        public double CO2EffectMinimum { get; set; } = 0.7;

        /// <summary>Exponent controlling the CO2 effect on N requirements (>0.0).</summary>
        [XmlIgnore]
        [Units("-")]
        public double CO2EffectExponent { get; set; } = 2.0;

        /// <summary>Minimum temperature for growth (oC).</summary>
        [Units("oC")]
        public double GrowthTminimum { get; set; } = 1.0;



        /// <summary>Optimum temperature for growth (oC).</summary>
        [Units("oC")]
        public double GrowthToptimum { get; set; } = 20.0;



        /// <summary>Curve parameter for growth response to temperature (>0.0).</summary>
        [Units("-")]
        public double GrowthTEffectExponent { get; set; } = 1.7;


        /// <summary>Enable photosynthesis reduction due to heat damage (yes/no).</summary>
        [Units("yes/no")]
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
        [Units("oC")]
        public double HeatOnsetTemperature { get; set; } = 28.0;



        /// <summary>Temperature for full heat effect on photosynthesis, growth stops (oC).</summary>
        [Units("oC")]
        public double HeatFullTemperature { get; set; } = 35.0;



        /// <summary>Cumulative degrees-day for recovery from heat stress (oCd).</summary>
        [Units("oCd")]
        public double HeatRecoverySumDD { get; set; } = 30.0;



        /// <summary>Reference temperature for recovery from heat stress (oC).</summary>
        [Units("oC")]
        public double HeatRecoveryTReference { get; set; } = 25.0;



        /// <summary>Enable photosynthesis reduction due to cold damage is enabled (yes/no).</summary>
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
        [Units("oC")]
        public double ColdOnsetTemperature { get; set; } = 1.0;



        /// <summary>Temperature for full cold effect on photosynthesis, growth stops (oC).</summary>
        [Units("oC")]
        public double ColdFullTemperature { get; set; } = -5.0;


        /// <summary>Cumulative degrees for recovery from cold stress (oCd).</summary>
        [Units("oCd")]
        public double ColdRecoverySumDD { get; set; } = 25.0;



        /// <summary>Reference temperature for recovery from cold stress (oC).</summary>
        [Units("oC")]
        public double ColdRecoveryTReference { get; set; } = 0.0;



        ////- Respiration parameters >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Maintenance respiration coefficient (0-1).</summary>
        [Units("0-1")]
        public double MaintenanceRespirationCoefficient { get; set; } = 0.03;



        /// <summary>Growth respiration coefficient (0-1).</summary>
        [Units("0-1")]
        public double GrowthRespirationCoefficient { get; set; } = 0.25;



        /// <summary>Reference temperature for maintenance respiration (oC).</summary>
        [Units("oC")]
        public double RespirationTReference { get; set; } = 20.0;



        /// <summary>Exponent controlling the effect of temperature on respiration (>1.0).</summary>
        [Units("-")]
        private double myRespirationExponent = 1.5;


        /// <summary>N concentration thresholds for roots, optimum, minimum and maximum (kgN/kgDM).</summary>
        //[Description("optimum, minimum and maximum")]
        [Units("kg/kg")]
        public double[] NThresholdsForRoots { get; set; } = { 0.02, 0.006, 0.025 };



        ////- Germination and emergence >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Cumulative degrees-day needed for seed germination (oCd).</summary>
        [Units("oCd")]
        public double DegreesDayForGermination { get; set; } = 125;



        /// <summary>The fractions of DM for each plant part at emergence, for all plants (0-1).</summary>
        private double[] emergenceDMFractions = { 0.60, 0.25, 0.00, 0.00, 0.15, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00 };

        ////- Allocation of new growth >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Target, or ideal, shoot-root ratio (>0.0).</summary>
        [Units("-")]
        public double TargetShootRootRatio { get; set; } = 4.0;



        /// <summary>Maximum fraction of DM growth allocated to roots (0-1).</summary>
        [Units("0-1")]
        public double MaxRootAllocation { get; set; } = 0.25;



        /// <summary>Maximum effect that soil GLFs have on Shoot-Root ratio (0-1).</summary>
        [Units("0-1")]
        public double ShootRootGlfFactor { get; set; } = 0.50;



        // - Effect of reproductive season ....................................
        /// <summary>
        /// Adjust Shoot:Root ratio to mimic DM allocation during reproductive season (perennial species)?.
        /// </summary>
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

        /// <summary>Reference latitude determining timing for reproductive season (degrees).</summary>
        [Units("degrees")]
        public double ReproSeasonReferenceLatitude { get; set; } = 41.0;



        /// <summary>Coefficient controlling the time to start the reproductive season as function of latitude (-).</summary>
        [Units("-")]
        public double ReproSeasonTimingCoeff { get; set; } = 0.14;



        /// <summary>Coefficient controlling the duration of the reproductive season as function of latitude (-).</summary>
        [Units("-")]
        public double ReproSeasonDurationCoeff { get; set; } = 2.0;

        /// <summary>Ratio between the length of shoulders and the period with full reproductive growth effect (-).</summary>
        [Units("-")]
        public double ReproSeasonShouldersLengthFactor { get; set; } = 1.0;

        /// <summary>Proportion of the onset phase of shoulder period with reproductive growth effect (0-1).</summary>
        [Units("0-1")]
        public double ReproSeasonOnsetDurationFactor { get; set; } = 0.60;

        /// <summary>Maximum increase in Shoot-Root ratio during reproductive growth (0-1).</summary>
        [Units("0-1")]
        public double ReproSeasonMaxAllocationIncrease { get; set; } = 0.50;


        /// <summary>Coefficient controlling the increase in shoot allocation during reproductive growth as function of latitude (-).</summary>
        [Units("-")]
        public double ReproSeasonAllocationCoeff { get; set; } = 0.10;



        /// <summary>Maximum target allocation of new growth to leaves (0-1).</summary>
        [Units("0-1")]
        public double FractionLeafMaximum { get; set; } = 0.7;



        /// <summary>Minimum target allocation of new growth to leaves (0-1).</summary>
        [Units("0-1")]
        public double FractionLeafMinimum { get; set; } = 0.7;


        /// <summary>Shoot DM at which allocation of new growth to leaves start to decrease (kgDM/ha).</summary>
        [Units("kg/ha")]
        public double FractionLeafDMThreshold { get; set; } = 500;



        /// <summary>Shoot DM when allocation to leaves is midway maximum and minimum (kgDM/ha).</summary>
        [Units("kg/ha")]
        public double FractionLeafDMFactor { get; set; } = 2000;



        /// <summary>Exponent of the function controlling the DM allocation to leaves (>0.0).</summary>
        [Units(">0.0")]
        public double FractionLeafExponent { get; set; } = 3.0;



        /// <summary>Fraction of new shoot growth to be allocated to stolons (0-1).</summary>
        [Units("0-1")]
        public double FractionToStolon { get; set; } = 0.0;



        /// <summary>Specific leaf area (m^2/kgDM).</summary>
        [Units("m^2/kg")]
        public double SpecificLeafArea { get; set; } = 25.0;

        /// <summary>Fraction of stolon tissue used when computing green LAI (0-1).</summary>
        [Units("0-1")]
        public double StolonEffectOnLAI { get; set; } = 0.0;

        /// <summary>Maximum aboveground biomass for considering stems when computing LAI (kgDM/ha).</summary>
        [Units("kg/ha")]
        public double ShootMaxEffectOnLAI { get; set; } = 1000;



        /// <summary>Maximum fraction of stem tissue used when computing green LAI (0-1).</summary>
        [Units("0-1")]
        public double MaxStemEffectOnLAI { get; set; } = 1.0;



        ////- Tissue turnover and senescence >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Number of live leaves per tiller (-).</summary>
        [Units("-")]
        public double LiveLeavesPerTiller { get; set; } = 3.0;



        /// <summary>Reference daily DM turnover rate for shoot tissues (0-1).</summary>
        /// <remarks>This is closely related to the leaf appearance rate.</remarks>
        [Units("0-1")]
        public double TissueTurnoverRateShoot { get; set; } = 0.05;



        /// <summary>Reference daily DM turnover rate for root tissues (0-1).</summary>
        [Units("0-1")]
        public double TissueTurnoverRateRoot { get; set; } = 0.02;



        /// <summary>Relative turnover rate for emerging tissues (>0.0).</summary>
        [Units("-")]
        public double RelativeTurnoverEmerging { get; set; } = 2.0;

        /// <summary>Reference daily detachment rate for dead tissues (0-1).</summary>
        [Units("0-1")]
        public double DetachmentRateShoot { get; set; } = 0.08;



        /// <summary>Minimum temperature for tissue turnover (oC).</summary>
        [Units("oC")]
        public double TurnoverTemperatureMin { get; set; } = 2.0;



        /// <summary>Reference temperature for tissue turnover (oC).</summary>
        [Units("oC")]
        public double TurnoverTemperatureRef { get; set; } = 20.0;



        /// <summary>Exponent of function for temperature effect on tissue turnover (>0.0).</summary>
        [Units("-")]
        public double TurnoverTemperatureExponent { get; set; } = 1.0;



        /// <summary>Maximum increase in tissue turnover due to water deficit (>0.0).</summary>
        [Units("-")]
        public double TurnoverDroughtEffectMax { get; set; } = 1.0;



        /// <summary>Minimum GLFwater without effect on tissue turnover (0-1).</summary>
        [Units("0-1")]
        public double TurnoverDroughtThreshold { get; set; } = 0.5;



        /// <summary>Coefficient controlling detachment rate as function of moisture (>0.0).</summary>
        [Units("-")]
        public double DetachmentDroughtCoefficient { get; set; } = 3.0;

        /// <summary>Minimum effect of drought on detachment rate (0-1).</summary>
        [Units("0-1")]
        public double DetachmentDroughtEffectMin { get; set; } = 0.1;


        /// <summary>Factor increasing tissue turnover rate due to stock trampling (>0.0).</summary>
        [Units("-")]
        public double TurnoverStockFactor { get; set; } = 0.01;

        /// <summary>Coefficient of function increasing the turnover rate due to defoliation (>0.0).</summary>
        [Units("-")]
        public double TurnoverDefoliationCoefficient { get; set; } = 0.5;



        /// <summary>Minimum significant daily effect of defoliation on tissue turnover rate (0-1).</summary>
        [Units("/day")]
        public double TurnoverDefoliationEffectMin { get; set; } = 0.025;

        /// <summary>Effect of defoliation on root turnover rate relative to stolon (0-1).</summary>
        [Units("0-1")]
        public double TurnoverDefoliationRootEffect { get; set; } = 0.1;

        ////- N fixation (for legumes) >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Minimum fraction of N demand supplied by biologic N fixation (0-1).</summary>
        [Units("0-1")]
        public double MinimumNFixation { get; set; }  = 0.0;

        

        /// <summary>Maximum fraction of N demand supplied by biologic N fixation (0-1).</summary>
        [Units("0-1")]
        public double MaximumNFixation { get; set; } = 0.0;

        

        /// <summary>Respiration cost factor due to the presence of symbiont bacteria (kgC/kgC in roots).</summary>
        [Units("kg/kg")]
        public double SymbiontCostFactor { get; set; } = 0.0;

        

        /// <summary>Respiration cost factor due to the activity of symbiont bacteria (kgC/kgN fixed).</summary>
        [Units("kg/kg")]
        public double NFixingCostFactor { get; set; } = 0.0;

        

        ////- Growth limiting factors >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Maximum reduction in plant growth due to water logging (saturated soil) (0-1).</summary>
        [Units("0-1")]
        public double SoilSaturationEffectMax { get; set; } = 0.1;

        

        /// <summary>Minimum water-free pore space for growth with no limitations (0-1).</summary>
        /// <remarks>A negative value indicates that porosity at DUL will be used.</remarks>
        [Units("0-1")]
        public double MinimumWaterFreePorosity { get; set; } = -1.0;

        

        /// <summary>Maximum daily recovery rate from water logging (0-1).</summary>
        [Units("0-1")]
        public double SoilSaturationRecoveryFactor { get; set; } = 0.25;

        

        /// <summary>Exponent to modify the effect of N deficiency on plant growth (>0.0).</summary>
        [Units("-")]
        public double NDillutionCoefficient { get; set; } = 0.5;



        /// <summary>Generic growth limiting factor that represents an arbitrary limitation to potential growth (0-1).</summary>
        /// <remarks> This factor can be used to describe the effects of drivers such as disease, etc.</remarks>
        [Units("0-1")]
        public double GlfGeneric { get; set; } = 1.0;



        /// <summary>Generic growth limiting factor that represents an arbitrary soil limitation (0-1).</summary>
        /// <remarks> This factor can be used to describe the effect of limitation in nutrients other than N.</remarks>
        [Units("0-1")]
        public double GlfSoilFertility { get; set; } = 1.0;

        

        ////- Plant height >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Minimum plant height (mm).</summary>
        [Units("mm")]
        public double PlantHeightMinimum { get; set; } = 25.0;

        

        /// <summary>Maximum plant height (mm).</summary>
        [Units("mm")]
        public double PlantHeightMaximum { get; set; } = 600.0;

        

        /// <summary>DM weight above ground for maximum plant height (kgDM/ha).</summary>
        [Units("kg/ha")]
        public double PlantHeightMassForMax { get; set; } = 10000;

        

        /// <summary>Exponent controlling shoot height as function of DM weight (>1.0).</summary>
        [Units(">1.0")]
        public double PlantHeightExponent { get; set; } = 2.8;

        ////- Digestibility and feed quality >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Digestibility of cell walls for each tissue age, emerging, developing, mature and dead (0-1).</summary>
        //[Description("emerging, developing, mature and dead")]
        [Units("0-1")]
        public double[] DigestibilitiesCellWall { get; set; } = { 0.6, 0.6, 0.6, 0.2 };

        ////- Harvest limits and preferences >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Minimum above ground green DM, leaf and stems (kgDM/ha).</summary>
        [Units("kg/ha")]
        public double MinimumGreenWt { get; set; } = 100.0;

       

        /// <summary>Leaf proportion in the minimum green Wt (0-1).</summary>
        [Units("0-1")]
        public double MinimumGreenLeafProp { get; set; } = 0.8;

        /// <summary>Minimum root amount relative to minimum green Wt (>0.0).</summary>
        [Units("0-1")]
        public double MinimumGreenRootProp { get; set; } = 0.5;

        /// <summary>Relative preference for live over dead material during graze (>0.0).</summary>
        [Units("-")]
        public double PreferenceForGreenOverDead { get; set; } = 1.0;

        /// <summary>Relative preference for leaf over stem-stolon material during graze (>0.0).</summary>
        [Units("-")]
        public double PreferenceForLeafOverStems { get; set; } = 1.0;

        ////- Soil related (water and N uptake) >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Flag which module will perform the water uptake process.</summary>
        internal string MyWaterUptakeSource = "species";

        /// <summary>Choose the method to calculate soil available water.</summary>
        [Units("-")]
        public PlantAvailableWaterMethod WaterAvailableMethod { get; set; } = PlantAvailableWaterMethod.DefaultAPSIM;

        

        /// <summary>Flag which module will perform the nitrogen uptake process.</summary>
        internal string MyNitrogenUptakeSource = "species";

        /// <summary>Choose the method to calculate available soil nitrogen.</summary>
        [Units("-")]
        public PlantAvailableNitrogenMethod NitrogenAvailableMethod { get; set; } = PlantAvailableNitrogenMethod.BasicAgPasture;

        

        /// <summary>Maximum fraction of water or N in the soil that is available to plants.</summary>
        /// <remarks>This is used to limit the amount taken up and avoid issues with very small numbers</remarks>
        [Units("0-1")]
        public double MaximumFractionAvailable { get; set; } = 0.999;

        /// <summary>Reference value for root length density for Water and N availability.</summary>
        [Units("mm/mm^3")]
        public double ReferenceRLD { get; set; } = 5.0;

        /// <summary>Exponent controlling the effect of soil moisture variations on water extractability.</summary>
        [Units("-")]
        public double ExponentSoilMoisture { get; set; } = 1.50;

        /// <summary>Reference value of Ksat for water availability function.</summary>
        [Units("mm/day")]
        public double ReferenceKSuptake { get; set; } = 15.0;

        /// <summary>Exponent of function determining soil extractable N.</summary>
        [Units("-")]
        public double NuptakeSWFactor { get; set; } = 0.25;

        /// <summary>Maximum daily amount of N that can be taken up by the plant (kg/ha).</summary>
        [Units("kg/ha")]
        public double MaximumNUptake { get; set; } = 10.0;

        /// <summary>Ammonium uptake coefficient.</summary>
        [Units("0-1")]
        public double KNH4 { get; set; } = 1.0;

        /// <summary>Nitrate uptake coefficient.</summary>
        [Units("0-1")]
        public double KNO3 { get; set; } = 1.0;

        /// <summary>Availability factor for NH4.</summary>
        [Units("-")]
        public double kuNH4 { get; set; } = 0.50;

        /// <summary>Availability factor for NO3.</summary>
        [Units("-")]
        public double kuNO3 { get; set; } = 0.95;

        ////- Parameters for annual species >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Gets or sets the day of year when seeds are allowed to germinate.</summary>
        [Units("day")]
        public int doyGermination = 275;

        /// <summary>Gets or sets the number of days from emergence to anthesis.</summary>
        [Units("day")]
        public int daysEmergenceToAnthesis = 120;

        /// <summary>Gets or sets the number of days from anthesis to maturity.</summary>
        [Units("days")]
        public int daysAnthesisToMaturity = 85;

        /// <summary>Gets or sets the cumulative degrees-day from emergence to anthesis (oCd).</summary>
        [Units("oCd")]
        public double degreesDayForAnthesis = 1100.0;

        /// <summary>Gets or sets the cumulative degrees-day from anthesis to maturity (oCd).</summary>
        [Units("oCd")]
        public double degreesDayForMaturity = 900.0;

        /// <summary>Gets or sets the number of days from emergence with reduced growth.</summary>
        [Units("days")]
        public int daysAnnualsFactor = 45;

        ////- Other parameters >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Describes the FVPD function.</summary>
        [Units("0-1")]
        public LinearInterpolationFunction FVPDFunction 
            = new LinearInterpolationFunction(x: new double[] { 0.0, 10.0, 50.0 },
                                              y: new double[] { 1.0, 1.0, 1.0 });

        #endregion  --------------------------------------------------------------------------------------------------------

        #region Private variables  -----------------------------------------------------------------------------------------

        ////- Plant parts and state >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Holds info about state of leaves (DM and N).</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private PastureAboveGroundOrgan leaf = null;

        /// <summary>Holds info about state of sheath/stems (DM and N).</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private PastureAboveGroundOrgan stem = null;

        /// <summary>Holds info about state of stolons (DM and N).</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private PastureAboveGroundOrgan stolon = null;

        /// <summary>Holds the info about state of roots (DM and N). It is a list of root organs, one for each zone where roots are growing.</summary>
        [Link(Type = LinkType.Child)]
        internal List<PastureBelowGroundOrgan> root = null;

        /// <summary>Above ground organs.</summary>
        [Link(Type = LinkType.Child)]
        public List<PastureAboveGroundOrgan> AboveGroundOrgans { get; private set; }

        /// <summary>Flag whether this species is alive (actively growing).</summary>
        private bool isAlive = false;

        /// <summary>Flag whether several routines are ran by species or are controlled by the Sward.</summary>
        internal bool isSwardControlled = false;

        /// <summary>Number of layers in the soil.</summary>
        private int nLayers;

        ////- Defining the plant type >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Flag whether this species is annual or perennial.</summary>
        private bool isAnnual = false;

        /// <summary>Flag whether this species is a legume.</summary>
        private bool isLegume = false;

        ////- Annuals and phenology >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Phenological stage of plant (0-2).</summary>
        /// <remarks>0 = germinating, 1 = vegetative, 2 = reproductive, negative for dormant/not sown.</remarks>
        private int phenologicStage = -1;

        /// <summary>Number of days since emergence (days).</summary>
        private double daysSinceEmergence;

        /// <summary>Cumulative degrees day during vegetative phase (oCd).</summary>
        private double cumulativeDDVegetative;

        /// <summary>Factor describing progress through phenological phases (0-1).</summary>
        private double phenoFactor;

        /// <summary>Cumulative degrees-day during germination phase (oCd).</summary>
        private double cumulativeDDGermination;

        ////- Photosynthesis, growth, and turnover >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Base gross photosynthesis rate, before damages (kg C/ha/day).</summary>
        private double basePhotosynthesis;

        /// <summary>Gross photosynthesis rate, after considering damages (kg C/ha/day).</summary>
        private double grossPhotosynthesis;

        /// <summary>Growth respiration rate (kg C/ha/day).</summary>
        private double respirationGrowth;

        /// <summary>Maintenance respiration rate (kg C/ha/day).</summary>
        private double respirationMaintenance;

        /// <summary>N fixation costs (kg C/ha/day).</summary>
        private double costNFixation;

        /// <summary>Amount of C remobilisable from senesced tissue (kg C/ha/day).</summary>
        private double remobilisableC;

        /// <summary>Amount of C remobilised from senesced tissue (kg C/ha/day).</summary>
        private double remobilisedC;

        /// <summary>Daily net growth potential (kg DM/ha).</summary>
        private double dGrowthPot;

        /// <summary>Daily potential growth after water stress (kg DM/ha).</summary>
        private double dGrowthAfterWaterLimitations;

        /// <summary>Daily growth after nutrient stress, actual growth (kg DM/ha).</summary>
        private double dGrowthAfterNutrientLimitations;

        /// <summary>Effective plant growth, actual growth minus senescence (kg DM/ha).</summary>
        private double dGrowthNet;

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

        /// <summary>Intervals defining the three reproductive season phases (onset, main phase, and outset).</summary>
        private double[] reproSeasonInterval;

        /// <summary>Day of the year for the start of the reproductive season.</summary>
        private double doyIniReproSeason;

        /// <summary>Relative increase in the shoot-root ratio during reproductive season (0-1).</summary>
        private double allocationIncreaseRepro;

        /// <summary>Daily DM turnover rate for live shoot tissues (0-1).</summary>
        private double gama;

        /// <summary>Daily DM turnover rate for dead shoot tissues (0-1).</summary>
        private double gamaD;

        /// <summary>Daily DM turnover rate for roots tissue (0-1).</summary>
        private double gamaR;

        /// <summary>Daily DM turnover rate for stolon tissue (0-1).</summary>
        private double gamaS;

        /// <summary>Tissue turnover factor due to variations in temperature (0-1).</summary>
        private double ttfTemperature;

        /// <summary>Tissue turnover factor for shoot due to variations in moisture (0-1).</summary>
        private double ttfMoistureShoot;

        /// <summary>Tissue turnover factor for roots due to variations in moisture (0-1).</summary>
        private double ttfMoistureRoot;

        /// <summary>Tissue turnover adjusting factor for number of leaves (0-1).</summary>
        private double ttfLeafNumber;

        /// <summary>Effect of defoliation on stolon turnover (0-1).</summary>
        private double cumDefoliationFactor;

        ////- Plant height, LAI and cover >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>LAI of green plant tissues (m^2/m^2).</summary>
        private double greenLAI;

        /// <summary>LAI of dead plant tissues (m^2/m^2).</summary>
        private double deadLAI;

        /// <summary>Effective cover for computing photosynthesis (0-1).</summary>
        private double effectiveGreenCover;

        ////- Amounts and fluxes of N in the plant >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Amount of N demanded for new growth, with luxury uptake (kg/ha).</summary>
        private double demandLuxuryN;

        /// <summary>Amount of N demanded for new growth, at optimum N content (kg/ha).</summary>
        private double demandOptimumN;

        /// <summary>Amount of N fixation from atmosphere, for legumes (kg/ha).</summary>
        private double fixedN;

        /// <summary>Amount of senesced N actually remobilised (kg/ha).</summary>
        private double senescedNRemobilised;

        /// <summary>Amount of luxury N actually remobilised (kg/ha).</summary>
        private double luxuryNRemobilised;

        /// <summary>Amount of N used up in new growth (kg/ha).</summary>
        private double dNewGrowthN;

        ////- N uptake process >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Amount of N demanded from the soil (kg/ha).</summary>
        private double mySoilNDemand;

        /// <summary>Amount of NH4-N in the soil available to the plant (kg/ha).</summary>
        [XmlIgnore]
        private double[] mySoilNH4Available
        {
            get
            {
                double[] available = new double[nLayers];
                foreach (PastureBelowGroundOrgan root in root)
                    for (int layer = 0; layer < nLayers; layer++)
                        available[layer] += root.mySoilNH4Available[layer];
                return available;
            }
        }

        /// <summary>Amount of NO3-N in the soil available to the plant (kg/ha).</summary>
        [XmlIgnore]
        private double[] mySoilNO3Available
        {
            get
            {
                double[] available = new double[nLayers];
                foreach (PastureBelowGroundOrgan root in root)
                    for (int layer = 0; layer < nLayers; layer++)
                        available[layer] += root.mySoilNO3Available[layer];
                return available;
            }
        }

        /// <summary>Amount of soil NH4-N taken up by the plant (kg/ha).</summary>
        private double[] mySoilNH4Uptake;

        /// <summary>Amount of soil NO3-N taken up by the plant (kg/ha).</summary>
        private double[] mySoilNO3Uptake;

        ////- Water uptake process >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Amount of water demanded for new growth (mm).</summary>
        private double myWaterDemand;

        /// <summary>Amount of plant available water in the soil (mm).</summary>
        private double[] mySoilWaterAvailable;

        /// <summary>Amount of soil water taken up (mm).</summary>
        private double[] mySoilWaterUptake;

        ////- Growth limiting factors >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Growth factor due to variations in intercepted radiation (0-1).</summary>
        private double glfRadn = 1.0;

        /// <summary>Growth factor due to N variations in atmospheric CO2 (0-1).</summary>
        private double glfCO2 = 1.0;

        /// <summary>Growth factor due to variations in plant N concentration (0-1).</summary>
        private double glfNc = 1.0;

        /// <summary>Growth factor due to variations in air temperature (0-1).</summary>
        private double glfTemp = 1.0;

        /// <summary>Flag whether the factor reducing photosynthesis due to heat damage is being used.</summary>
        private bool usingHeatStressFactor = true;

        /// <summary>Flag whether the factor reducing photosynthesis due to cold damage is being used.</summary>
        private bool usingColdStressFactor = true;

        /// <summary>Growth factor due to heat stress (0-1).</summary>
        private double glfHeat = 1.0;

        /// <summary>Auxiliary growth reduction factor due to high temperatures (0-1).</summary>
        private double highTempStress = 1.0;

        /// <summary>Cumulative degrees of temperature for recovery from heat damage (oCd).</summary>
        private double cumulativeDDHeat;

        /// <summary>Growth factor due to cold stress (0-1).</summary>
        private double glfCold = 1.0;

        /// <summary>Auxiliary growth reduction factor due to low temperatures (0-1).</summary>
        private double lowTempStress = 1.0;

        /// <summary>Cumulative degrees of temperature for recovery from cold damage (oCd).</summary>
        private double cumulativeDDCold;

        /// <summary>Growth limiting factor due to water stress (0-1).</summary>
        private double glfWaterSupply = 1.0;

        /// <summary>Cumulative water logging factor (0-1).</summary>
        private double cumWaterLogging;

        /// <summary>Growth limiting factor due to water logging (0-1).</summary>
        private double glfWaterLogging = 1.0;

        /// <summary>Growth limiting factor due to N stress (0-1).</summary>
        private double glfNSupply = 1.0;

        /// <summary>Temperature effects on respiration (0-1).</summary>
        private double tempEffectOnRespiration;

        ////- Harvest and digestibility >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Fraction of standing DM harvested (0-1).</summary>
        private double defoliatedFraction;

        /// <summary>Fraction of standing DM harvested (0-1), used on tissue turnover.</summary>
        private double myDefoliatedFraction;

        /// <summary>Digestibility of defoliated material (0-1).</summary>
        private double defoliatedDigestibility;

        #endregion  --------------------------------------------------------------------------------------------------------

        #region Constants and enums  ---------------------------------------------------------------------------------------

        /// <summary>Average carbon content in plant dry matter (kg/kg).</summary>
        internal const double CarbonFractionInDM = 0.4;

        /// <summary>Average potential ME concentration in herbage material (MJ/kg)</summary>
        internal const double PotentialMEOfHerbage = 16.0;

        /// <summary>Factor for converting nitrogen to protein (kg/kg).</summary>
        internal const double NitrogenToProteinFactor = 6.25;

        /// <summary>Carbon to nitrogen ratio of proteins (-).</summary>
        internal const double CNratioProtein = 3.5;

        /// <summary>Carbon to nitrogen ratio of cell walls (-).</summary>
        internal const double CNratioCellWall = 100.0;

        /// <summary>Minimum significant difference between two values.</summary>
        internal const double Epsilon = 0.000000001;

        /// <summary>A yes or no answer.</summary>
        public enum YesNoAnswer
        {
            /// <summary>A positive answer.</summary>
            yes,

            /// <summary>A negative answer.</summary>
            no
        }

        /// <summary>List of valid species family names.</summary>
        public enum PlantFamilyType
        {
            /// <summary>A grass species, Poaceae.</summary>
            Grass,

            /// <summary>A legume species, Fabaceae.</summary>
            Legume,

            /// <summary>A non grass or legume species.</summary>
            Forb
        }

        /// <summary>List of valid photosynthesis pathways.</summary>
        public enum PhotosynthesisPathwayType
        {
            /// <summary>A C3 plant.</summary>
            C3,

            /// <summary>A C4 plant.</summary>
            C4
        }

        /// <summary>List of valid methods to compute plant available water.</summary>
        public enum PlantAvailableWaterMethod
        {
            /// <summary>APSIM default method, using kL.</summary>
            DefaultAPSIM,

            /// <summary>Alternative method, using root length and modified kL.</summary>
            AlternativeKL,

            /// <summary>Alternative method, using root length and relative Ksat.</summary>
            AlternativeKS
        }

        /// <summary>List of valid methods to compute plant available water.</summary>
        public enum PlantAvailableNitrogenMethod
        {
            /// <summary>AgPasture old default method, all N available.</summary>
            BasicAgPasture,

            /// <summary>APSIM default method, using soil water status.</summary>
            DefaultAPSIM,

            /// <summary>Alternative method, using root length and water status.</summary>
            AlternativeRLD,

            /// <summary>Alternative method, using water uptake.</summary>
            AlternativeWup
        }

        #endregion  --------------------------------------------------------------------------------------------------------

        #region Model outputs  ---------------------------------------------------------------------------------------------

        ////- General properties >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Gets the flag signalling whether plant is alive (true/false).</summary>
        //[Description("Flag signalling whether plant is alive")]
        [Units("true/false")]
        public bool IsAlive
        {
            get { return PlantStatus == "alive"; }
        }

        /// <summary>Gets the plant status (dead, alive, etc.).</summary>
        //[Description("Plant status (dead, alive, etc.)")]
        [Units("-")]
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
        /// <remarks>0 = germinating, 1 = vegetative, 2 = reproductive, negative for dormant/not sown.</remarks>
        //[Description("Plant development stage number")]
        [Units("-")]
        public int Stage
        {
            get
            {
                if (isAlive)
                    return phenologicStage;
                else
                    return -1; //"out"
            }
        }

        /// <summary>Gets or sets the solar radiation intercepted by the plant's canopy (MJ/m^2/day).</summary>
        [XmlIgnore]
        [Units("MJ/m^2/day")]
        public double InterceptedRadn { get; set; }

        /// <summary>Gets or sets the radiance on top of the plant's canopy (MJ/m^2/day).</summary>
        [XmlIgnore]
        [Units("MJ/m^2/day")]
        public double RadiationTopOfCanopy { get; set; }

        ////- DM and C outputs >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Gets the total amount of C in the plant (kgC/ha).</summary>
        //[Description("Total amount of C in the plant")]
        [Units("kg/ha")]
        public double TotalC
        {
            get { return TotalWt * CarbonFractionInDM; }
        }

        /// <summary>Gets the total dry matter weight of plant (kgDM/ha).</summary>
        //[Description("Total dry matter weight of plant")]
        [Units("kg/ha")]
        public double TotalWt
        {
            get { return AboveGroundWt + BelowGroundWt; }
        }

        /// <summary>Gets the dry matter weight of the plant above ground (kgDM/ha).</summary>
        //[Description("Dry matter weight of the plant above ground")]
        [Units("kg/ha")]
        public double AboveGroundWt
        {
            get { return AboveGroundLiveWt + AboveGroundDeadWt; }
        }

        /// <summary>Gets the dry matter weight of live tissues above ground (kgDM/ha).</summary>
        //[Description("Dry matter weight of live tissues above ground")]
        [Units("kg/ha")]
        public double AboveGroundLiveWt
        {
            get { return leaf.DMLive + stem.DMLive + stolon.DMLive; }
        }

        /// <summary>Gets the dry matter weight of dead tissues above ground (kgDM/ha).</summary>
        //[Description("Dry matter weight of dead tissues above ground")]
        [Units("kg/ha")]
        public double AboveGroundDeadWt
        {
            get { return leaf.DMDead + stem.DMDead + stolon.DMDead; }
        }

        /// <summary>Gets the dry matter weight of the plant below ground (kgDM/ha).</summary>
        //[Description("Dry matter weight of the plant below ground")]
        [Units("kg/ha")]
        public double BelowGroundWt
        {
            get
            {
                double dmTotal = root[0].DMTotal;
                //foreach (PastureBelowGroundOrgan root in roots)
                //    dmTotal += root.DMTotal;
                // TODO: currently only the roots at the main/home zone are considered, must add the other zones too
                return dmTotal;
            }
        }

        /// <summary>Gets the dry matter weight of live tissues below ground (kgDM/ha).</summary>
        //[Description("Dry matter weight of live tissues below ground")]
        [Units("kg/ha")]
        public double BelowGroundLiveWt
        {
            get
            {
                double dmTotal = root[0].DMLive;
                //foreach (PastureBelowGroundOrgan root in roots)
                //    dmTotal += root.DMLive;
                // TODO: currently only the roots at the main/home zone are considered, must add the other zones too
                return dmTotal;
            }
        }
        
        /// <summary>Gets the dry matter weight of plant's leaves (kgDM/ha).</summary>
        //[Description("Dry matter weight of plant's leaves")]
        [Units("kg/ha")]
        public double LeafWt
        {
            get { return leaf.DMTotal; }
        }

        /// <summary>Gets the dry matter weight of live leaves (kgDM/ha).</summary>
        //[Description("Dry matter weight of live leaves")]
        [Units("kg/ha")]
        public double LeafLiveWt
        {
            get { return leaf.DMLive; }
        }

        /// <summary>Gets the dry matter weight of dead leaves (kgDM/ha).</summary>
        //[Description("Dry matter weight of dead leaves")]
        [Units("kg/ha")]
        public double LeafDeadWt
        {
            get { return leaf.DMDead; }
        }

        /// <summary>Gets the dry matter weight of plant's stems and sheath (kgDM/ha).</summary>
        //[Description("Dry matter weight of plant's stems and sheath")]
        [Units("kg/ha")]
        public double StemWt
        {
            get { return stem.DMTotal; }
        }

        /// <summary>Gets the dry matter weight of alive stems and sheath (kgDM/ha).</summary>
        //[Description("Dry matter weight of alive stems and sheath")]
        [Units("kg/ha")]
        public double StemLiveWt
        {
            get { return stem.DMLive; }
        }

        /// <summary>Gets the dry matter weight of dead stems and sheath (kgDM/ha).</summary>
        //[Description("Dry matter weight of dead stems and sheath")]
        [Units("kg/ha")]
        public double StemDeadWt
        {
            get { return stem.DMDead; }
        }

        /// <summary>Gets the dry matter weight of plant's stolons (kgDM/ha).</summary>
        //[Description("Dry matter weight of plant's stolons")]
        [Units("kg/ha")]
        public double StolonWt
        {
            get { return stolon.DMTotal; }
        }

        /// <summary>Gets the dry matter weight of plant's roots (kgDM/ha).</summary>
        //[Description("Dry matter weight of plant's roots")]
        [Units("kg/ha")]
        public double RootWt
        {
            get
            {
                double rootWt = root[0].DMTotal;
                //foreach (PastureBelowGroundOrgan root in roots)
                //    rootWt += root.DMTotal;
                // TODO: currently only the roots at the main/home zone are considered, must add the other zones too

                return rootWt;
            }
        }

        ////- N amount outputs >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Gets the total amount of N in the plant (kgN/ha).</summary>
        //[Description("Total amount of N in the plant")]
        [Units("kg/ha")]
        public double TotalN
        {
            get { return AboveGroundN + BelowGroundN; }
        }

        /// <summary>Gets the amount of N in the plant above ground (kgN/ha).</summary>
        //[Description("Amount of N in the plant above ground")]
        [Units("kg/ha")]
        public double AboveGroundN
        {
            get { return AboveGroundLiveN + AboveGroundDeadN; }
        }

        /// <summary>Gets the amount of N in live tissues above ground (kgN/ha).</summary>
        //[Description("Amount of N in live tissues above ground")]
        [Units("kg/ha")]
        public double AboveGroundLiveN
        {
            get { return leaf.NLive + stem.NLive + stolon.NLive; }
        }

        /// <summary>Gets the amount of N in dead tissues above ground (kgN/ha).</summary>
        //[Description("Amount of N in dead tissues above ground")]
        [Units("kg/ha")]
        public double AboveGroundDeadN
        {
            get { return leaf.NDead + stem.NDead + stolon.NDead; }
        }

        /// <summary>Gets the amount of N in the plant below ground (kgN/ha).</summary>
        //[Description("Amount of N in the plant below ground")]
        [Units("kg/ha")]
        public double BelowGroundN
        {
            get
            {
                double nTotal = root[0].NTotal;
                //foreach (PastureBelowGroundOrgan root in roots)
                //    nTotal += root.NTotal;
                // TODO: currently only the roots at the main/home zone are considered, must add the other zones too
                return nTotal;
            }
        }
        /// <summary>Gets the amount of N in live tissues below ground (kgN/ha).</summary>
        //[Description("Amount of N in live tissues below ground")]
        [Units("kg/ha")]
        public double BelowGroundLiveN
        {
            get
            {
                double nTotal = root[0].NLive;
                //foreach (PastureBelowGroundOrgan root in roots)
                //    nTotal += root.NLive;
                // TODO: currently only the roots at the main/home zone are considered, must add the other zones too
                return nTotal;
            }
        }

        /// <summary>Gets the amount of N in the plant's leaves (kgN/ha).</summary>
        //[Description("Amount of N in the plant's leaves")]
        [Units("kg/ha")]
        public double LeafN
        {
            get { return leaf.NTotal; }
        }

        /// <summary>Gets the amount of N in live leaves (kgN/ha).</summary>
        //[Description("Amount of N in live leaves")]
        [Units("kg/ha")]
        public double LeafLiveN
        {
            get { return leaf.NLive; }
        }

        /// <summary>Gets the amount of N in dead leaves (kgN/ha).</summary>
        //[Description("Amount of N in dead leaves")]
        [Units("kg/ha")]
        public double LeafDeadN
        {
            get { return leaf.NDead; }
        }

        /// <summary>Gets the amount of N in the plant's stems and sheath (kgN/ha).</summary>
        //[Description("Amount of N in the plant's stems and sheath")]
        [Units("kg/ha")]
        public double StemN
        {
            get { return stem.NTotal; }
        }

        /// <summary>Gets the amount of N in live stems and sheath (kgN/ha).</summary>
        //[Description("Amount of N in live stems and sheath")]
        [Units("kg/ha")]
        public double StemLiveN
        {
            get { return stem.NLive; }
        }

        /// <summary>Gets the amount of N in dead stems and sheath (kgN/ha).</summary>
        //[Description("Amount of N in dead stems and sheath")]
        [Units("kg/ha")]
        public double StemDeadN
        {
            get { return stem.NDead; }
        }

        /// <summary>Gets the amount of N in the plant's stolons (kgN/ha).</summary>
        //[Description("Amount of N in the plant's stolons")]
        [Units("kg/ha")]
        public double StolonN
        {
            get { return stolon.NTotal; }
        }

        /// <summary>Gets the amount of N in the plant's roots (kgN/ha).</summary>
        //[Description("Amount of N in the plant's roots")]
        [Units("kg/ha")]
        public double RootN
        {
            get
            {
                double nTotal = root[0].NTotal;
                //foreach (PastureBelowGroundOrgan root in roots)
                //    nTotal += root.NTotal;
                // TODO: currently only the roots at the main/home zone are considered, must add the other zones too
                return nTotal;
            }
        }

        ////- N concentration outputs >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Gets the average N concentration in the plant above ground (kgN/kgDM).</summary>
        //[Description("Average N concentration in the plant above ground")]
        [Units("kg/kg")]
        public double AboveGroundNConc
        {
            get { return MathUtilities.Divide(AboveGroundN, AboveGroundWt, 0.0); }
        }


        /// <summary>Gets the average N concentration in plant's leaves (kgN/kgDM).</summary>
        //[Description("Average N concentration in plant's leaves")]
        [Units("kg/kg")]
        public double LeafNConc
        {
            get { return MathUtilities.Divide(LeafN, LeafWt, 0.0); }
        }

        /// <summary>Gets the average N concentration in plant's stems (kgN/kgDM).</summary>
        //[Description("Average N concentration in plant's stems")]
        [Units("kg/kg")]
        public double StemNConc
        {
            get { return MathUtilities.Divide(StemN, StemWt, 0.0); }
        }

        /// <summary>Gets the average N concentration in plant's stolons (kgN/kgDM).</summary>
        //[Description("Average N concentration in plant's stolons")]
        [Units("kg/kg")]
        public double StolonNConc
        {
            get { return MathUtilities.Divide(StolonN, StolonWt, 0.0); }
        }

        /// <summary>Gets the average N concentration in plant's roots (kgN/kgDM).</summary>
        //[Description("Average N concentration in plant's roots")]
        [Units("kg/kg")]
        public double RootNConc
        {
            get { return MathUtilities.Divide(RootN, RootWt, 0.0); }
        }

        ////- DM growth and senescence outputs >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Gets the base potential photosynthetic rate, before damages, in carbon equivalent (kgC/ha).</summary>
        //[Description("Base potential photosynthetic rate, before damages, in carbon equivalent")]
        [Units("kg/ha")]
        public double BasePotentialPhotosynthesisC
        {
            get { return basePhotosynthesis; }
        }

        /// <summary>Gets the gross potential photosynthetic rate, after considering damages, in carbon equivalent (kgC/ha).</summary>
        //[Description("Gross potential photosynthetic rate, after considering damages, in carbon equivalent")]
        [Units("kg/ha")]
        public double GrossPotentialPhotosynthesisC
        {
            get { return grossPhotosynthesis; }
        }

        /// <summary>Gets the respiration costs expressed in carbon equivalent (kgC/ha).</summary>
        //[Description("Respiration costs expressed in carbon equivalent")]
        [Units("kg/ha")]
        public double RespirationLossC
        {
            get { return respirationMaintenance + respirationGrowth; }
        }

        /// <summary>Gets the n fixation costs expressed in carbon equivalent (kgC/ha).</summary>
        //[Description("N fixation costs expressed in carbon equivalent")]
        [Units("kg/ha")]
        public double NFixationCostC
        {
            get { return 0.0; }
        }

        /// <summary>Gets the remobilised carbon from senesced tissues (kgC/ha).</summary>
        //[Description("Remobilised carbon from senesced tissues")]
        [Units("kg/ha")]
        public double RemobilisedSenescedC
        {
            get { return remobilisableC; }
        }

        /// <summary>Gets the gross potential growth rate (kgDM/ha).</summary>
        //[Description("Gross potential growth rate")]
        [Units("kg/ha")]
        public double GrossPotentialGrowthWt
        {
            get { return grossPhotosynthesis / CarbonFractionInDM; }
        }

        /// <summary>Gets the net potential growth rate, after respiration (kgDM/ha).</summary>
        //[Description("Net potential growth rate, after respiration")]
        [Units("kg/ha")]
        public double NetPotentialGrowthWt
        {
            get { return dGrowthPot; }
        }

        /// <summary>Gets the net potential growth rate after water stress (kgDM/ha).</summary>
        //[Description("Net potential growth rate after water stress")]
        [Units("kg/ha")]
        public double NetPotentialGrowthAfterWaterWt
        {
            get { return dGrowthAfterWaterLimitations; }
        }

        /// <summary>Gets the net potential growth rate after nutrient stress (kgDM/ha).</summary>
        //[Description("Net potential growth rate after nutrient stress")]
        [Units("kg/ha")]
        public double NetPotentialGrowthAfterNutrientWt
        {
            get { return dGrowthAfterNutrientLimitations; }
        }

        /// <summary>Gets the net, or actual, plant growth rate (kgDM/ha).</summary>
        //[Description("Net, or actual, plant growth rate")]
        [Units("kg/ha")]
        public double NetGrowthWt
        {
            get { return dGrowthNet; }
        }

        /// <summary>Gets the net herbage growth rate (above ground) (kgDM/ha).</summary>
        //[Description("Net herbage growth rate (above ground)")]
        [Units("kg/ha")]
        public double HerbageGrowthWt
        {
            get { return dGrowthShootDM - detachedShootDM; }
        }

        /// <summary>Gets the net root growth rate (kgDM/ha).</summary>
        //[Description("Net root growth rate")]
        [Units("kg/ha")]
        public double RootGrowthWt
        {
            get { return dGrowthRootDM - detachedRootDM; }
        }

        /// <summary>Gets the dry matter weight of detached dead material deposited onto soil surface (kgDM/ha).</summary>
        //[Description("Dry matter weight of detached dead material deposited onto soil surface")]
        [Units("kg/ha")]
        public double LitterDepositionWt
        {
            get { return detachedShootDM; }
        }

        /// <summary>Gets the dry matter weight of detached dead roots added to soil FOM (kgDM/ha).</summary>
        //[Description("Dry matter weight of detached dead roots added to soil FOM")]
        [Units("kg/ha")]
        public double RootDetachedWt
        {
            get { return detachedRootDM; }
        }

        /// <summary>Gets the gross primary productivity (kgC/ha).</summary>
        //[Description("Gross primary productivity")]
        [Units("kg/ha")]
        public double GPP
        {
            get { return grossPhotosynthesis / CarbonFractionInDM; }
        }

        /// <summary>Gets the net primary productivity (kgC/ha).</summary>
        //[Description("Net primary productivity")]
        [Units("kg/ha")]
        public double NPP
        {
            get { return (grossPhotosynthesis - respirationGrowth - respirationMaintenance) / CarbonFractionInDM; }
        }

        /// <summary>Gets the net above-ground primary productivity (kgC/ha).</summary>
        //[Description("Net above-ground primary productivity")]
        [Units("kg/ha")]
        public double NAPP
        {
            get { return (grossPhotosynthesis - respirationGrowth - respirationMaintenance) * fractionToShoot / CarbonFractionInDM; }
        }

        /// <summary>Gets the net below-ground primary productivity (kgC/ha).</summary>
        //[Description("Net below-ground primary productivity")]
        [Units("kg/ha")]
        public double NBPP
        {
            get { return (grossPhotosynthesis - respirationGrowth - respirationMaintenance) * (1 - fractionToShoot) / CarbonFractionInDM; }
        }

        ////- N flows outputs >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Gets the amount of senesced N potentially remobilisable (kgN/ha).</summary>
        //[Description("Amount of senesced N potentially remobilisable")]
        [Units("kg/ha")]
        public double RemobilisableSenescedN
        {
            get
            {
                return leaf.NSenescedRemobilisable + stem.NSenescedRemobilisable +
                       stolon.NSenescedRemobilisable + root[0].NSenescedRemobilisable;
                // TODO: currently only the roots at the main/home zone are considered, must add the other zones too
            }
        }

        /// <summary>Gets the amount of senesced N actually remobilised (kgN/ha).</summary>
        //[Description("Amount of senesced N actually remobilised")]
        [Units("kg/ha")]
        public double RemobilisedSenescedN
        {
            get { return senescedNRemobilised; }
        }

        /// <summary>Gets the amount of luxury N potentially remobilisable (kgN/ha).</summary>
        //[Description("Amount of luxury N potentially remobilisable")]
        [Units("kg/ha")]
        public double RemobilisableLuxuryN
        {
            get
            {
                return leaf.NLuxuryRemobilisable + stem.NLuxuryRemobilisable +
                           stolon.NLuxuryRemobilisable + root[0].NLuxuryRemobilisable;
                // TODO: currently only the roots at the main/home zone are considered, must add the other zones too
            }
        }

        /// <summary>Gets the amount of luxury N actually remobilised (kgN/ha).</summary>
        //[Description("Amount of luxury N actually remobilised")]
        [Units("kg/ha")]
        public double RemobilisedLuxuryN
        {
            get { return luxuryNRemobilised; }
        }

        /// <summary>Gets the amount of atmospheric N fixed by symbiosis (kgN/ha).</summary>
        //[Description("Amount of atmospheric N fixed by symbiosis")]
        [Units("kg/ha")]
        public double FixedN
        {
            get { return fixedN; }
        }

        /// <summary>Gets the amount of N required with luxury uptake (kgN/ha).</summary>
        //[Description("Amount of N required with luxury uptake")]
        [Units("kg/ha")]
        public double DemandAtLuxuryN
        {
            get { return demandLuxuryN; }
        }

        /// <summary>Gets the amount of N required for optimum growth (kgN/ha).</summary>
        //[Description("Amount of N required for optimum growth")]
        [Units("kg/ha")]
        public double DemandAtOptimumN
        {
            get { return demandOptimumN; }
        }

        /// <summary>Gets the amount of N demanded from the soil (kgN/ha).</summary>
        //[Description("Amount of N demanded from the soil")]
        [Units("kg/ha")]
        public double SoilDemandN
        {
            get { return mySoilNDemand; }
        }

        /// <summary>Gets the amount of plant available N in the soil (kgN/ha).</summary>
        //[Description("Amount of plant available N in the soil")]
        [Units("kg/ha")]
        public double SoilAvailableN
        {
            get { return mySoilNH4Available.Sum() + mySoilNO3Available.Sum(); }
        }

        /// <summary>Gets the amount of N taken up from the soil (kgN/ha).</summary>
        //[Description("Amount of N taken up from the soil")]
        [Units("kg/ha")]
        public double SoilUptakeN
        {
            get { return mySoilNH4Uptake.Sum() + mySoilNO3Uptake.Sum(); }
        }

        /// <summary>Gets the amount of N in detached dead material deposited onto soil surface (kgN/ha).</summary>
        //[Description("Amount of N in detached dead material deposited onto soil surface")]
        [Units("kg/ha")]
        public double LitterDepositionN
        {
            get { return detachedShootN; }
        }

        /// <summary>Gets the amount of N in detached dead roots added to soil FOM (kgN/ha).</summary>
        //[Description("Amount of N in detached dead roots added to soil FOM")]
        [Units("kg/ha")]
        public double RootDetachedN
        {
            get { return detachedRootN; }
        }

        /// <summary>Gets the amount of N in new growth (kgN/ha).</summary>
        //[Description("Amount of N in new growth")]
        [Units("kg/ha")]
        public double NetGrowthN
        {
            get { return dNewGrowthN; }
        }

        /// <summary>Gets the amount of plant available NH4-N in each soil layer (kgN/ha).</summary>
        //[Description("Amount of plant available NH4-N in each soil layer")]
        [Units("kg/ha")]
        public double[] SoilNH4Available
        {
            get { return mySoilNH4Available; }
        }

        /// <summary>Gets the amount of plant available NO3-N in each soil layer (kgN/ha).</summary>
        //[Description("Amount of plant available NO3-N in each soil layer")]
        [Units("kg/ha")]
        public double[] SoilNO3Available
        {
            get { return mySoilNO3Available; }
        }

        /// <summary>Gets the amount of NH4-N taken up from each soil layer (kgN/ha).</summary>
        //[Description("Amount of NH4-N taken up from each soil layer")]
        [Units("kg/ha")]
        public double[] SoilNH4Uptake
        {
            get { return mySoilNH4Uptake; }
        }

        /// <summary>Gets the amount of NO3-N taken up from each soil layer (kgN/ha).</summary>
        //[Description("Amount of NO3-N taken up from each soil layer")]
        [Units("kg/ha")]
        public double[] SoilNO3Uptake
        {
            get { return mySoilNO3Uptake; }
        }

        ////- Water related outputs >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Gets the soil water content at lower limit for plant uptake ().</summary>
        //[Description("Soil water content at lower limit for plant uptake")]
        [Units("mm^3/mm^3")]
        public double[] LL
        {
            get
            {
                SoilCrop soilInfo = (SoilCrop)mySoil.Crop(Name);
                return soilInfo.LL;
            }
        }

        /// <summary>Gets or sets the amount of water demanded by the plant (mm).</summary>
        [XmlIgnore]
        [Units("mm")]
        public double WaterDemand
        {
            get { return myWaterDemand; }
            set { myWaterDemand = value; }
        }

        /// <summary>Gets the amount of plant available water in each soil layer (mm).</summary>
        //[Description("Amount of plant available water in each soil layer")]
        [Units("mm")]
        public double[] WaterAvailable
        {
            get { return mySoilWaterAvailable; }
        }

        /// <summary>Gets the amount of water taken up from each soil layer (mm).</summary>
        //[Description("Amount of water taken up from each soil layer")]
        [Units("mm")]
        public double[] WaterUptake
        {
            get { return mySoilWaterUptake; }
        }

        ////- Growth limiting factors >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Gets the growth factor due to variations in intercepted radiation (0-1).</summary>
        //[Description("Growth factor due to variations in intercepted radiation")]
        [Units("0-1")]
        public double GlfRadnIntercept
        {
            get { return glfRadn; }
        }

        /// <summary>Gets the growth factor due to variations in atmospheric CO2 (0-1).</summary>
        //[Description("Growth factor due to variations in atmospheric CO2")]
        [Units("0-1")]
        public double GlfCO2
        {
            get { return glfCO2; }
        }

        /// <summary>Gets the growth factor due to variations in plant N concentration (0-1).</summary>
        //[Description("Growth factor due to variations in plant N concentration")]
        [Units("0-1")]
        public double GlfNContent
        {
            get { return glfNc; }
        }

        /// <summary>Gets the growth factor due to variations in air temperature (0-1).</summary>
        //[Description("Growth factor due to variations in air temperature")]
        [Units("0-1")]
        public double GlfTemperature
        {
            get { return glfTemp; }
        }

        /// <summary>Gets the growth factor due to heat damage stress (0-1).</summary>
        //[Description("Growth factor due to heat damage stress")]
        [Units("0-1")]
        public double GlfHeatDamage
        {
            get { return glfHeat; }
        }

        /// <summary>Gets the growth factor due to cold damage stress (0-1).</summary>
        //[Description("Growth factor due to cold damage stress")]
        [Units("0-1")]
        public double GlfColdDamage
        {
            get { return glfCold; }
        }

        /// <summary>Gets the growth limiting factor due to water deficit (0-1).</summary>
        //[Description("Growth limiting factor due to water deficit")]
        [Units("0-1")]
        public double GlfWaterSupply
        {
            get { return glfWaterSupply; }
        }

        /// <summary>Gets the growth limiting factor due to water logging (0-1).</summary>
        //[Description("Growth limiting factor due to water logging")]
        [Units("0-1")]
        public double GlfWaterLogging
        {
            get { return glfWaterLogging; }
        }

        /// <summary>Gets the growth limiting factor due to soil N availability (0-1).</summary>
        //[Description("Growth limiting factor due to soil N availability")]
        [Units("0-1")]
        public double GlfNSupply
        {
            get { return glfNSupply; }
        }

        // TODO: verify that this is really needed
        /// <summary>Gets the effect of vapour pressure on growth (used by micromet) (0-1).</summary>
        //[Description("Effect of vapour pressure on growth (used by micromet)")]
        [Units("0-1")]
        public double FVPD
        {
            get { return FVPDFunction.ValueForX(VPD()); }
        }

        /// <summary>Gets the temperature factor for respiration (0-1).</summary>
        //[Description("Temperature factor for respiration")]
        [Units("0-1")]
        public double TemperatureFactorRespiration
        {
            get { return tempEffectOnRespiration; }
        }

        ////- DM allocation and turnover rates >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Gets the fraction of new growth allocated to shoot (0-1).</summary>
        //[Description("Fraction of new growth allocated to shoot")]
        [Units("0-1")]
        public double FractionGrowthToShoot
        {
            get { return fractionToShoot; }
        }

        /// <summary>Gets the fraction of new growth allocated to roots (0-1).</summary>
        //[Description("Fraction of new growth allocated to roots")]
        [Units("0-1")]
        public double FractionGrowthToRoot
        {
            get { return 1 - fractionToShoot; }
        }

        /// <summary>Gets the fraction of new shoot growth allocated to leaves (0-1).</summary>
        //[Description("Fraction of new shoot growth allocated to leaves")]
        [Units("0-1")]
        public double FractionGrowthToLeaf
        {
            get { return fractionToLeaf; }
        }

        /// <summary>Gets the turnover rate for live shoot tissues (leaves and stem) (0-1).</summary>
        //[Description("Turnover rate for live shoot tissues (leaves and stem)")]
        [Units("0-1")]
        public double TurnoverRateLiveShoot
        {
            get { return gama; }
        }

        /// <summary>Gets the turnover rate for dead shoot tissues (leaves and stem) (0-1).</summary>
        //[Description("Turnover rate for dead shoot tissues (leaves and stem)")]
        [Units("0-1")]
        public double TurnoverRateDeadShoot
        {
            get { return gamaD; }
        }

        /// <summary>Gets the turnover rate for stolon tissues (0-1).</summary>
        //[Description("Turnover rate for stolon tissues")]
        [Units("0-1")]
        public double TurnoverRateStolons
        {
            get { return gamaS; }
        }

        /// <summary>Gets the turnover rate for roots tissues (0-1).</summary>
        //[Description("Turnover rate for roots tissues")]
        [Units("0-1")]
        public double TurnoverRateRoots
        {
            get { return gamaR; }
        }

        /// <summary>Gets the temperature factor for tissue turnover (0-1).</summary>
        //[Description("Temperature factor for tissue turnover")]
        [Units("0-1")]
        public double TemperatureFactorTurnover
        {
            get { return ttfTemperature; }
        }

        /// <summary>Gets the moisture factor for tissue turnover (0-1).</summary>
        //[Description("Moisture factor for tissue turnover")]
        [Units("0-1")]
        public double MoistureFactorTurnover
        {
            get { return ttfMoistureShoot; }
        }

        ////- LAI and cover outputs >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Gets the leaf area index of green tissues (m^2/m^2).</summary>
        //[Description("Leaf area index of green tissues")]
        [Units("m^2/m^2")]
        [XmlIgnore]
        public double LAIGreen
        {
            get { return greenLAI; }
            set { greenLAI = value; }
        }

        /// <summary>Gets the leaf area index of dead tissues (m^2/m^2).</summary>
        //[Description("Leaf area index of dead tissues")]
        [Units("m^2/m^2")]
        public double LAIDead
        {
            get { return deadLAI; }
        }

        /// <summary>Gets the fraction of soil covered by dead tissues (0-1).</summary>
        //[Description("Fraction of soil covered by dead tissues")]
        [Units("0-1")]
        public double CoverDead
        {
            get { return CalcPlantCover(deadLAI); }
        }

        ////- Root depth and distribution >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Gets the average depth of root zone (mm).</summary>
        //[Description("Average depth of root zone")]
        [Units("mm")]
        public double RootDepth
        {
            get { return root[0].Depth; }
        }

        /// <summary>Gets the layer at bottom of root zone ().</summary>
        //[Description("Layer at bottom of root zone")]
        [Units("-")]
        public int RootFrontier
        {
            get { return root[0].BottomLayer; }
        }

        ////- Harvest outputs >>> - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Get above ground biomass</summary>
        [Units("g/m2")]
        public Biomass AboveGround
        {
            get
            {
                Biomass mass = new Biomass();
                mass.StructuralWt = (leaf.StandingHerbageWt + stem.StandingHerbageWt + stolon.StandingHerbageWt) / 10.0; // to g/m2
                mass.StructuralN = (leaf.StandingHerbageN + stem.StandingHerbageN + stolon.StandingHerbageN) / 10.0;    // to g/m2
                mass.DMDOfStructural = leaf.DigestibilityLive;
                return mass;
            }
        }

        /// <summary>Get above ground biomass</summary>
        [Units("g/m2")]
        public Biomass AboveGroundHarvestable
        {
            get
            {
                Biomass mass = new Biomass();
                mass.StructuralWt = Harvestable.Wt / 10.0; // to g/m2
                mass.StructuralN = Harvestable.N / 10.0;    // to g/m2
                mass.DMDOfStructural = Harvestable.Digestibility;
                return mass;
            }
        }

        /// <summary>Dry matter and N available for harvesting (kgDM/ha).</summary>
        public AGPBiomass Harvestable
        {
            get
            {
                return new AGPBiomass()
                {
                    Wt = leaf.DMTotalHarvestable + stem.DMTotalHarvestable + stolon.DMTotalHarvestable,
                    N = leaf.NTotalHarvestable + stem.NTotalHarvestable + stolon.NTotalHarvestable,
                    Digestibility = MathUtilities.Divide(leaf.StandingDigestibility * leaf.DMTotal +
                                                         stem.StandingDigestibility * stem.DMTotal +
                                                         stolon.StandingDigestibility * stolon.DMTotal,
                                                         leaf.DMTotalHarvestable + stem.DMTotalHarvestable + stolon.DMTotalHarvestable, 0.0)
                };
            }
        }

        /// <summary>Standing dry matter and N (kgDM/ha).</summary>
        public AGPBiomass Standing
        {
            get
            {
                return new AGPBiomass()
                {
                    Wt = leaf.StandingHerbageWt + stem.StandingHerbageWt + stolon.StandingHerbageWt,
                    N = leaf.StandingHerbageN + stem.StandingHerbageN + stolon.StandingHerbageN,
                    Digestibility = MathUtilities.Divide(leaf.StandingDigestibility * leaf.StandingHerbageWt +
                                                         stem.StandingDigestibility * stem.StandingHerbageWt +
                                                         stolon.StandingDigestibility * stolon.StandingHerbageWt,
                                                         leaf.StandingHerbageWt + stem.StandingHerbageWt + stolon.StandingHerbageWt, 0.0)
                };
            }
        }

        /// <summary>Standing live dry matter and N (kgDM/ha).</summary>
        public AGPBiomass StandingLive
        {
            get
            {
                return new AGPBiomass()
                {
                    Wt = leaf.StandingLiveHerbageWt + stem.StandingLiveHerbageWt + stolon.StandingLiveHerbageWt,
                    N = leaf.StandingLiveHerbageN + stem.StandingLiveHerbageN + stolon.StandingLiveHerbageN,
                    Digestibility = MathUtilities.Divide(leaf.StandingLiveDigestibility * leaf.StandingLiveHerbageWt +
                                                         stem.StandingLiveDigestibility * stem.StandingLiveHerbageWt +
                                                         stolon.StandingLiveDigestibility * stolon.StandingLiveHerbageWt,
                                                         leaf.StandingLiveHerbageWt + stem.StandingLiveHerbageWt + stolon.StandingLiveHerbageWt, 0.0)
                };
            }
        }

        /// <summary>Standing dead dry matter and N (kgDM/ha).</summary>
        public AGPBiomass StandingDead
        {
            get
            {
                return new AGPBiomass()
                {
                    Wt = leaf.StandingDeadHerbageWt + stem.StandingDeadHerbageWt + stolon.StandingDeadHerbageWt,
                    N = leaf.StandingDeadHerbageN + stem.StandingDeadHerbageN + stolon.StandingDeadHerbageN,
                    Digestibility = MathUtilities.Divide(leaf.StandingDeadDigestibility * leaf.StandingDeadHerbageWt +
                                                         stem.StandingDeadDigestibility * stem.StandingDeadHerbageWt +
                                                         stolon.StandingDeadDigestibility * stolon.StandingDeadHerbageWt,
                                                         leaf.StandingDeadHerbageWt + stem.StandingDeadHerbageWt + stolon.StandingDeadHerbageWt, 0.0)
                };
            }
        }

        /// <summary>Live dry matter and N available for harvesting.</summary>
        public AGPBiomass HarvestableLive
        {
            get
            {
                return new AGPBiomass()
                {
                    Wt = leaf.DMLiveHarvestable + stem.DMLiveHarvestable + stolon.DMLiveHarvestable,
                    N = leaf.NLiveHarvestable + stem.NLiveHarvestable + stolon.NLiveHarvestable
                };
            }
        }

        /// <summary>Dead dry matter and N available for harvesting.</summary>
        public AGPBiomass HarvestableDead
        {
            get
            {
                return new AGPBiomass()
                {
                    Wt = leaf.DMDeadHarvestable + stem.DMDeadHarvestable + stolon.DMDeadHarvestable,
                    N = leaf.NDeadHarvestable + stem.NDeadHarvestable + stolon.NDeadHarvestable
                };
            }
        }

        /// <summary>The amount of plant dry matter removed by harvest (kgDM/ha).</summary>
        [Units("kg/ha")]
        public double HarvestedWt { get { return leaf.DMRemoved + stem.DMRemoved + stolon.DMRemoved; } }

        /// <summary>Gets the fraction of available dry matter actually harvested ().</summary>
        //[Description("Fraction of available dry matter actually harvested")]
        [Units("0-1")]
        public double HarvestedFraction
        {
            get { return defoliatedFraction; }
        }

        /// <summary>The amount of N removed by harvest (kg/ha).</summary>
        [Units("kg/ha")]
        public double HarvestedN { get { return leaf.NRemoved + stem.NRemoved + stolon.NRemoved; } }

        /// <summary>Gets the average N concentration in harvested material (kgN/kgDM).</summary>
        //[Description("Average N concentration in harvested material")]
        [Units("kg/kg")]
        public double HarvestedNConc
        {
            get { return MathUtilities.Divide(HarvestedN, HarvestedWt, 0.0); }
        }

        /// <summary>Gets the average digestibility of harvested material (0-1).</summary>
        //[Description("Average digestibility of harvested material")]
        [Units("0-1")]
        public double HarvestedDigestibility
        {
            get { return defoliatedDigestibility; }
        }

        /// <summary>Gets the average metabolisable energy concentration of harvested material (MJ/kgDM).</summary>
        //[Description("Average metabolisable energy concentration of harvested material")]
        [Units("MJ/kg")]
        public double HarvestedME
        {
            get { return PotentialMEOfHerbage * defoliatedDigestibility; }
        }


        #region Tissue outputs  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Emerging tissues from all above ground organs.</summary>
        [XmlIgnore]
        public TissuesHelper EmergingTissue { get; private set; }

        /// <summary>Developing tissues from all above ground organs.</summary>
        [XmlIgnore]
        public TissuesHelper DevelopingTissue { get; private set; }

        /// <summary>Mature tissues from all above ground organs.</summary>
        [XmlIgnore]
        public TissuesHelper MatureTissue { get; private set; }

        /// <summary>Dead tissues from all above ground organs.</summary>
        [XmlIgnore]
        public TissuesHelper DeadTissue { get; private set; }

        /// <summary>A list of organs that can be damaged.</summary>
        public List<IOrganDamage> Organs
        {
            get
            {
                var organsThatCanBeDamaged = new List<IOrganDamage>() { leaf, stem, stolon };
                return organsThatCanBeDamaged;
            }
        }

        /// <summary>Plant population.</summary>
        public double Population { get { return 0; } }

        /// <summary>Amount of assimilate available to be damaged.</summary>
        public double AssimilateAvailable => throw new NotImplementedException();

        #endregion  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        #endregion  --------------------------------------------------------------------------------------------------------

        #region Initialisation methods  ------------------------------------------------------------------------------------

        private List<RootZone> RootZonesInitialisations = new List<RootZone>();

        /// <summary>
        /// Add a zone where roots are to grow.
        /// </summary>
        public void AddZone(string zoneName, double rootDepth, double rootDM)
        {
            RootZonesInitialisations.Add(new RootZone() { ZoneName = zoneName, RootDepth = rootDepth, RootDM = rootDM, });
        }

        /// <summary>Performs the initialisation procedures for this species (set DM, N, LAI, etc.).</summary>
        /// <param name="sender">The sender model</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            EmergingTissue = new TissuesHelper(new GenericTissue[] { leaf.EmergingTissue, stem.EmergingTissue, stolon.EmergingTissue });
            DevelopingTissue = new TissuesHelper(new GenericTissue[] { leaf.DevelopingTissue, stem.DevelopingTissue, stolon.DevelopingTissue });
            MatureTissue = new TissuesHelper(new GenericTissue[] { leaf.MatureTissue, stem.MatureTissue, stolon.MatureTissue });
            DeadTissue = new TissuesHelper(new GenericTissue[] { leaf.DeadTissue, stem.DeadTissue, stolon.DeadTissue });

            // get the number of layers in the soil profile
            nLayers = mySoil.Thickness.Length;

            // set the base or main root zone (use 2 tissues, one live other dead), more zones can be added by user
            root[0].Initialise(zone, InitialRootDM, InitialRootDepth,
                               MinimumGreenWt * MinimumGreenRootProp, 
                               WaterAvailableMethod, NitrogenAvailableMethod,
                               KNH4, KNO3, MaximumNUptake, kuNH4, kuNO3,
                               ReferenceKSuptake, ReferenceRLD, ExponentSoilMoisture);

            // add any other zones that have been given at initialisation
            foreach (RootZone rootZone in RootZonesInitialisations)
            {
                // find the zone and get its soil
                Zone zone = Apsim.Find(this, rootZone.ZoneName) as Zone;
                if (zone == null)
                    throw new Exception("Cannot find zone: " + rootZone.ZoneName);

                var newRootOrgan = Apsim.Clone(root[0]) as PastureBelowGroundOrgan;
                // add the zone to the list
                newRootOrgan.Initialise(zone, 
                                        rootZone.RootDM, rootZone.RootDepth,
                                        MinimumGreenWt * MinimumGreenRootProp,
                                        WaterAvailableMethod, NitrogenAvailableMethod,
                                        KNH4, KNO3, MaximumNUptake, kuNH4, kuNO3,
                                        ReferenceKSuptake, ReferenceRLD, ExponentSoilMoisture);
                root.Add(newRootOrgan);
            }

            // initialise soil water and N variables
            InitiliaseSoilArrays();

            // Set the minimum green DM
            leaf.MinimumLiveDM = MinimumGreenWt * MinimumGreenLeafProp;
            stem.MinimumLiveDM = MinimumGreenWt * (1.0 - MinimumGreenLeafProp);
            stolon.MinimumLiveDM = 0.0;

            // set initial plant state
            SetInitialState();

            // initialise parameter for DM allocation during reproductive season
            InitReproductiveGrowthFactor();

            // initialise parameters for biomass removal
            InitBiomassRemovals();

            // check whether there is a resource arbitrator, it will control the uptake
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
            mySoilNH4Uptake = new double[nLayers];
            mySoilNO3Uptake = new double[nLayers];
        }

        /// <summary>
        /// Sets the initial parameters for this plant, including DM and N content of various pools plus plant height and root depth.
        /// </summary>
        private void SetInitialState()
        {
            // 1. Choose the appropriate DM partition, based on species family
            double[] initialDMFractions;
            if (mySpeciesFamily == PlantFamilyType.Grass)
                initialDMFractions = initialDMFractionsGrasses;
            else if (mySpeciesFamily == PlantFamilyType.Legume)
                initialDMFractions = initialDMFractionsLegumes;
            else
                initialDMFractions = initialDMFractionsForbs;

            // Determine what biomass to reset the organs to. If a negative InitialShootDM
            // was specified by user then that means the plant isn't sown yet so reset
            // the organs to zero biomass. This is the reason Max is used below.
            var shootDM = Math.Max(0.0, InitialShootDM);
            var rootDM = Math.Max(0.0, InitialRootDM);

            // Perform the organ resets.
            leaf.Reset(emergingWt: initialDMFractions[0] * shootDM,
                       developingWt: initialDMFractions[1] * shootDM,
                       matureWt: initialDMFractions[2] * shootDM,
                       deadWt: initialDMFractions[3] * shootDM);

            stem.Reset(emergingWt: initialDMFractions[4] * shootDM,
                       developingWt: initialDMFractions[5] * shootDM,
                       matureWt: initialDMFractions[6] * shootDM,
                       deadWt: initialDMFractions[7] * shootDM);

            stolon.Reset(emergingWt: initialDMFractions[8] * shootDM,
                         developingWt: initialDMFractions[9] * shootDM,
                         matureWt: initialDMFractions[10] * shootDM,
                         deadWt: 0.0);

            root[0].Reset(rootDM, InitialRootDepth);
    
            // Set initial phenological stage
            if (MathUtilities.IsGreaterThan(InitialShootDM, 0))
                phenologicStage = 1;
            else if (MathUtilities.FloatsAreEqual(InitialShootDM, 0))
                phenologicStage = 0;
            else
                phenologicStage = -1;

            if (phenologicStage >= 0)
                isAlive = true;

            // Calculate the values for LAI
            EvaluateLAI();
        }

        /// <summary>Set the plant state at germination.</summary>
        internal void SetEmergenceState()
        {
            leaf.ResetEmergence(emergingWt: MinimumGreenWt * emergenceDMFractions[0],
                                developingWt: MinimumGreenWt * emergenceDMFractions[1],
                                matureWt: MinimumGreenWt * emergenceDMFractions[1],
                                deadWt: MinimumGreenWt * emergenceDMFractions[3]);
            stem.ResetEmergence(emergingWt: MinimumGreenWt * emergenceDMFractions[4],
                                developingWt: MinimumGreenWt * emergenceDMFractions[5],
                                matureWt: MinimumGreenWt * emergenceDMFractions[6],
                                deadWt: MinimumGreenWt * emergenceDMFractions[7]);
            stolon.ResetEmergence(emergingWt: MinimumGreenWt * emergenceDMFractions[8],
                                  developingWt: MinimumGreenWt * emergenceDMFractions[9],
                                  matureWt: MinimumGreenWt * emergenceDMFractions[10],
                                  deadWt: 0.0);

            root[0].Reset(root[0].MinimumLiveDM, root[0].RootDepthMinimum);

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
        ///  greater for higher latitudes. Shoulder periods occur before and after the main phase, in these allocation transitions
        ///  between default allocation and that of the main phase.
        /// </remarks>
        private void InitReproductiveGrowthFactor()
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

        /// <summary>Initialises the default biomass removal fractions.</summary>
        private void InitBiomassRemovals()
        {
            // leaves, stems
            var removalFractions = new OrganBiomassRemovalType()
            {
                FractionLiveToRemove = 0.5,
                FractionDeadToRemove = 0.5
            };
            leaf.SetRemovalFractions("Harvest", removalFractions);
            leaf.SetRemovalFractions("Graze", removalFractions);
            leaf.SetRemovalFractions("Cut", removalFractions);
            stem.SetRemovalFractions("Harvest", removalFractions);
            stem.SetRemovalFractions("Graze", removalFractions);
            stem.SetRemovalFractions("Cut", removalFractions);

            // stolon
            var stolonRemovalFractions2 = new OrganBiomassRemovalType()
            {
                FractionLiveToRemove = 0.5,
                FractionDeadToRemove = 0.0
            };
            stolon.SetRemovalFractions("Harvest", stolonRemovalFractions2);
            stolon.SetRemovalFractions("Graze", stolonRemovalFractions2);
            stolon.SetRemovalFractions("Cut", stolonRemovalFractions2);
        }

        #endregion  --------------------------------------------------------------------------------------------------------

        #region Daily processes  -------------------------------------------------------------------------------------------

        /// <summary>EventHandler - preparation before the main daily processes.</summary>
        /// <param name="sender">The sender model</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data</param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            // 1. Zero out several variables
            RefreshVariables();
        }

        /// <summary>Zeroes out the value of several variables.</summary>
        internal void RefreshVariables()
        {
            // reset variables for whole plant
            defoliatedFraction = 0.0;
            defoliatedDigestibility = 0.0;

            grossPhotosynthesis = 0.0;
            dGrowthPot = 0.0;
            dGrowthAfterWaterLimitations = 0.0;
            dGrowthAfterNutrientLimitations = 0.0;
            dGrowthNet = 0.0;
            dNewGrowthN = 0.0;
            dGrowthShootDM = 0.0;
            dGrowthShootN = 0.0;
            dGrowthRootDM = 0.0;
            dGrowthRootN = 0.0;

            detachedShootDM = 0.0;
            detachedShootN = 0.0;
            detachedRootDM = 0.0;
            detachedRootN = 0.0;

            demandOptimumN = 0.0;
            demandLuxuryN = 0.0;
            fixedN = 0.0;

            senescedNRemobilised = 0.0;
            luxuryNRemobilised = 0.0;

            mySoilWaterAvailable = new double[nLayers];
            mySoilWaterUptake = new double[nLayers];
            mySoilNH4Uptake = new double[nLayers];
            mySoilNO3Uptake = new double[nLayers];

            // reset transfer variables for all tissues in each organ
            leaf.DoCleanTransferAmounts();
            stem.DoCleanTransferAmounts();
            stolon.DoCleanTransferAmounts();
            foreach (PastureBelowGroundOrgan root in root)
                root.DoCleanTransferAmounts();
        }

        /// <summary>Performs the calculations for potential growth.</summary>
        /// <param name="sender">The sender model</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data</param>
        [EventSubscribe("DoPotentialPlantGrowth")]
        private void OnDoPotentialPlantGrowth(object sender, EventArgs e)
        {
            if (isAlive && !isSwardControlled)
            {
                // check phenology of annuals
                if (isAnnual)
                    EvaluatePhenologyOfAnnuals();

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
                    EvaluateAllocationToShoot();
                    EvaluateAllocationToLeaf();

                    // Get the potential growth after water limitations
                    CalcGrowthAfterWaterLimitations();

                    // Get the N amount demanded for optimum growth and luxury uptake
                    EvaluateNitrogenDemand();
                }
            }
            //else { // Growth is controlled by Sward (all species) }
        }

        /// <summary>Performs the calculations for actual growth.</summary>
        /// <param name="sender">The sender model</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data</param>
        [EventSubscribe("DoActualPlantGrowth")]
        private void OnDoActualPlantGrowth(object sender, EventArgs e)
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
                    DoActualGrowthAndAllocation();

                    // Send detached material to other modules (litter to surfacesOM, roots to soilFOM) 
                    DoAddDetachedShootToSurfaceOM(detachedShootDM, detachedShootN);
                    root[0].Tissue[0].DetachBiomass(detachedRootDM, detachedRootN);
                    //foreach (PastureBelowGroundOrgan root in rootZones)
                    //    root.DoDetachBiomass(root.DMDetached, root.NDetached);
                    // TODO: currently only the roots at the main/home zone are considered, must add the other zones too
                }
            }
            //else { // Growth is controlled by Sward (all species) }
        }

        #region - Plant growth processes  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Computes the daily progress through germination.</summary>
        /// <returns>The fraction of the germination phase completed (0-1)</returns>
        internal double DailyGerminationProgress()
        {
            cumulativeDDGermination += Math.Max(0.0, Tmean(0.5) - GrowthTminimum);
            return MathUtilities.Divide(cumulativeDDGermination, DegreesDayForGermination, 1.0);
        }

        /// <summary>Calculates the daily potential plant growth.</summary>
        internal void CalcDailyPotentialGrowth()
        {
            // Get today's gross potential photosynthetic rate (kgC/ha/day)
            grossPhotosynthesis = DailyPotentialPhotosynthesis();

            // Get respiration rates (kgC/ha/day)
            respirationMaintenance = DailyMaintenanceRespiration();
            respirationGrowth = DailyGrowthRespiration();

            // Get N fixation costs (base)
            if (isLegume)
                costNFixation = DailyNFixationCosts();

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
            glfWaterSupply = WaterDeficitFactor();

            // get the limitation factor due to water logging (lack of aeration)
            glfWaterLogging = WaterLoggingFactor();

            // adjust today's growth for limitations related to soil water
            dGrowthAfterWaterLimitations = dGrowthPot * Math.Min(glfWaterSupply, glfWaterLogging);
        }

        /// <summary>Calculates the actual plant growth (after all growth limitations, before senescence).</summary>
        /// <remarks>
        /// Here the limitation due to soil fertility are considered, the model simulates N deficiency only, but a generic user-settable
        ///  limitation factor (GlfSFertility) can be used to mimic limitation due to other soil related factors (e.g. phosphorus)
        /// The GLF due to N stress is modified here to account for N dilution effects:
        /// Many plants, especially grasses, can keep growth even when N supply is below optimum; the N concentration is reduced
        ///  in the plant tissues. This is represented hereby adjusting the effect of N deficiency using a power function. When the exponent
        ///  is 1.0, the reduction in growth is linearly proportional to N deficiency, a greater value results in less reduction in growth.
        /// For many plants the value should be smaller than 1.0. For grasses, the exponent is typically around 0.5.
        /// </remarks>
        internal void CalcGrowthAfterNutrientLimitations()
        {
            // get total N to allocate in new growth
            dNewGrowthN = fixedN + senescedNRemobilised + SoilUptakeN + luxuryNRemobilised;

            // get the limitation factor due to soil N deficiency
            double glfNit = 1.0;
            if (dGrowthAfterWaterLimitations > Epsilon)
            {
                if (dNewGrowthN > Epsilon)
                {
                    glfNSupply = Math.Min(1.0, Math.Max(0.0, MathUtilities.Divide(dNewGrowthN, demandOptimumN, 1.0)));

                    // adjust the glfN
                    glfNit = Math.Pow(glfNSupply, NDillutionCoefficient);
                }
                else
                {
                    glfNSupply = 0.0;
                    glfNit = 0.0;
                }
            }
            else
                glfNSupply = 1.0;

            // adjust today's growth for limitations related to soil nutrient supply
            dGrowthAfterNutrientLimitations = dGrowthAfterWaterLimitations * Math.Min(glfNit, GlfSoilFertility);
        }

        /// <summary>Computes the plant's gross potential growth rate.</summary>
        /// <returns>The potential amount of C assimilated via photosynthesis (kgC/ha)</returns>
        private double DailyPotentialPhotosynthesis()
        {
            // CO2 effects on Pmax
            glfCO2 = CO2EffectOnPhotosynthesis();

            // N concentration effects on Pmax
            glfNc = NConcEffectOnPhotosynthesis();

            // Temperature effects to Pmax
            double tempGlf1 = TemperatureLimitingFactor(Tmean(0.5));
            double tempGlf2 = TemperatureLimitingFactor(Tmean(0.75));

            //Temperature growth factor (for reporting purposes only)
            glfTemp = (0.25 * tempGlf1) + (0.75 * tempGlf2);

            // Potential photosynthetic rate (mg CO2/m^2 leaf/s)
            //   at dawn and dusk (first and last quarters of the day)
            double Pmax1 = ReferencePhotosyntheticRate * tempGlf1 * glfCO2 * glfNc;
            //   at bright light (half of sunlight length, middle of day)
            double Pmax2 = ReferencePhotosyntheticRate * tempGlf2 * glfCO2 * glfNc;

            // Day light length, converted to seconds
            double myDayLength = 3600 * myMetData.CalculateDayLength(-6);

            // Photosynthetically active radiation, converted from MJ/m2.day to J/m2.s
            double interceptedPAR = MathUtilities.Divide(FractionPAR * RadiationTopOfCanopy * 1000000.0, myDayLength, 0.0);

            // Photosynthetically active radiation, for the middle of the day (J/m2 leaf/s)
            interceptedPAR *= LightExtinctionCoefficient * (4.0 / 3.0);

            //Photosynthesis per leaf area under full irradiance at the top of the canopy (mg CO2/m^2 leaf/s)
            double Pl1 = SingleLeafPhotosynthesis(0.5 * interceptedPAR, Pmax1);
            double Pl2 = SingleLeafPhotosynthesis(interceptedPAR, Pmax2);

            // Photosynthesis per leaf area for the day (mg CO2/m^2 leaf/day)
            double Pl_Daily = myDayLength * (Pl1 + Pl2) * 0.5;

            // Radiation effects (for reporting purposes only)
            glfRadn = MathUtilities.Divide((0.25 * Pl1) + (0.75 * Pl2), (0.25 * Pmax1) + (0.75 * Pmax2), 1.0);

            // Photosynthesis for whole canopy, per ground area (mg CO2/m^2/day)
            double Pc_Daily = Pl_Daily * effectiveGreenCover / LightExtinctionCoefficient;

            //  Carbon assimilation per leaf area (g C/m^2/day)
            double carbonAssimilation = Pc_Daily * 0.001 * (12.0 / 44.0); // Convert from mgCO2 to gC           

            // Gross photosynthesis, converted to kg C/ha/day
            basePhotosynthesis = carbonAssimilation * 10; // convert from g/m2 to kg/ha (= 10000/1000)

            // Consider the extreme temperature effects (in practice only one temp stress factor is < 1)
            glfHeat = HeatStress();
            glfCold = ColdStress();

            // Consider reduction in photosynthesis for annual species, related to phenology
            if (isAnnual)
                basePhotosynthesis *= AnnualSpeciesGrowthFactor();

            // Actual gross photosynthesis (gross potential growth - kg C/ha/day)
            return basePhotosynthesis * Math.Min(glfHeat, glfCold) * GlfGeneric;
        }

        /// <summary>Compute the photosynthetic rate for a single leaf.</summary>
        /// <param name="IL">The instantaneous intercepted radiation (J/m2 leaf/s)</param>
        /// <param name="Pmax">The maximum photosynthetic rate (mg CO2/m^2 leaf/s)</param>
        /// <returns>The potential photosynthetic rate (mgCO2/m^2 leaf/s)</returns>
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
            tempEffectOnRespiration = TemperatureEffectOnRespiration(Tmean(0.5));

            // Total DM converted to C (kg/ha)
            double liveBiomassC = (AboveGroundLiveWt + BelowGroundLiveWt) * CarbonFractionInDM;
            double result = liveBiomassC * MaintenanceRespirationCoefficient * tempEffectOnRespiration * glfNc;
            return Math.Max(0.0, result);
        }

        /// <summary>Computes the plant's loss of C due to growth respiration.</summary>
        /// <returns>The amount of C lost to atmosphere (kgC/ha)</returns>
        private double DailyGrowthRespiration()
        {
            return grossPhotosynthesis * GrowthRespirationCoefficient;
        }

        /// <summary>Computes the turnover rates for each tissue pool of all plant organs.</summary>
        /// <remarks>
        /// The rates are passed on to each organ and the amounts potentially turned over are computed for each tissue.
        /// The turnover rates are affected by variations in soil water and air temperature. For leaves the number of leaves
        ///  per tiller (LiveLeavesPerTiller, a parameter specific for each species) also influences the turnover rate.
        /// The C and N amounts potentially available for remobilisation are also computed in here.
        /// </remarks>
        internal void EvaluateTissueTurnoverRates()
        {
            // Get the temperature factor for tissue turnover
            ttfTemperature = TempFactorForTissueTurnover(Tmean(0.5));

            // Get the moisture factor for shoot tissue turnover
            ttfMoistureShoot = MoistureEffectOnTissueTurnover();

            // TODO: find a way to use today's GLFwater, or to compute an alternative one

            // Get the moisture factor for littering rate (detachment)
            double ttfMoistureLitter = MoistureEffectOnDetachment();

            // Consider the number of leaves
            ttfLeafNumber = 3.0 / LiveLeavesPerTiller; // three refers to the number of stages used in the model

            // Get the moisture factor for root tissue turnover
            ttfMoistureRoot = 2.0 - Math.Min(glfWaterSupply, glfWaterLogging);

            //stocking rate affecting transfer of dead to litter (default to 0 for now - should be read in - TODO: Update/delete this function)
            double SR = 0;
            double StockFac2Litter = TurnoverStockFactor * SR;

            // Turnover rate for leaf and stem tissues
            gama = TissueTurnoverRateShoot * ttfTemperature * ttfMoistureShoot * ttfLeafNumber;

            // Get the factor due to defoliation (increases turnover)
            double defoliationFactor = DefoliationEffectOnTissueTurnover();

            // Turnover rate for stolons
            if (isLegume)
            {
                // base rate is the same as for the other above ground organs, but consider defoliation effect
                gamaS = gama + defoliationFactor * (1.0 - gama);
            }
            else
                gamaS = 0.0;

            // Turnover rate for roots
            gamaR = TissueTurnoverRateRoot * ttfTemperature * ttfMoistureRoot;
            gamaR += TurnoverDefoliationRootEffect * defoliationFactor * (1.0 - gamaR);

            // Turnover rate for dead material (littering or detachment)
            double digestDead = (leaf.DigestibilityDead * leaf.DMDead) + (stem.DigestibilityDead * stem.DMDead);
            digestDead = MathUtilities.Divide(digestDead, leaf.DMDead + stem.DMDead, 0.0);
            gamaD = DetachmentRateShoot * ttfMoistureLitter * digestDead / CarbonFractionInDM;
            gamaD += StockFac2Litter;

            if ((gama > 1.0) || (gamaS > 1.0) || (gamaD > 1.0) || (gamaR > 1.0))
                throw new ApsimXException(this, " AgPasture computed a tissue turnover rate greater than one");
            if ((gama < 0.0) || (gamaS < 0.0) || (gamaD < 0.0) || (gamaR < 0.0))
                throw new ApsimXException(this, " AgPasture computed a negative tissue turnover rate");

            // Check phenology effect for annuals
            if (isAnnual && phenologicStage > 0)
            {
                if (phenologicStage == 1)
                {
                    //vegetative, turnover is zero at emergence and increases with age
                    gama *= Math.Pow(phenoFactor, 0.5);
                    gamaR *= Math.Pow(phenoFactor, 2.0);
                    gamaD *= Math.Pow(phenoFactor, 2.0);
                }
                else if (phenologicStage == 2)
                {
                    //reproductive, turnover increases with age and reach one at maturity
                    gama += (1.0 - gama) * Math.Pow(phenoFactor, 2.0);
                    gamaR += (1.0 - gamaR) * Math.Pow(phenoFactor, 3.0);
                    gamaD += (1.0 - gamaD) * Math.Pow(phenoFactor, 3.0);
                }
            }

            // Check that senescence will not result in dmGreen < dmGreenmin
            if (gama > 0.0)
            {
                //only relevant for leaves+stems
                double currentGreenDM = leaf.DMLive + stem.DMLive;
                double currentMatureDM = leaf.Tissue[2].DM.Wt + stem.Tissue[2].DM.Wt;
                double dmGreenToBe = currentGreenDM - (currentMatureDM * gama);
                double minimumStandingLive = leaf.MinimumLiveDM + stem.MinimumLiveDM;
                if (dmGreenToBe < minimumStandingLive)
                {
                    double gamaBase = gama;
                    gama = MathUtilities.Divide(currentGreenDM - minimumStandingLive, currentMatureDM, 0.0);

                    // reduce stolon and root turnover too (half of the reduction in leaf/stem)
                    double dmFactor = 0.5 * (gamaBase + gama) / gamaBase;
                    gamaS *= dmFactor;
                    gamaR *= dmFactor;
                }
            }

            // Check minimum DM for roots too
            if (root[0].DMLive * (1.0 - gamaR) < root[0].MinimumLiveDM)
            {
                if (root[0].DMLive <= root[0].MinimumLiveDM)
                    gamaR = 0.0;
                else
                    gamaR = MathUtilities.Divide(root[0].DMLive - root[0].MinimumLiveDM, root[0].DMLive, 0.0);
                // TODO: currently only the roots at the main/home zone are considered, must add the other zones too
            }

            // Make sure rates are within bounds
            gama = MathUtilities.Bound(gama, 0.0, 1.0);
            gamaS = MathUtilities.Bound(gamaS, 0.0, 1.0);
            gamaR = MathUtilities.Bound(gamaR, 0.0, 1.0);
            gamaD = MathUtilities.Bound(gamaD, 0.0, 1.0);

            // Do the actual turnover, update DM and N
            // - Leaves and stems
            double[] turnoverRates = new double[] { gama * RelativeTurnoverEmerging, gama, gama, gamaD };
            leaf.DoTissueTurnover(turnoverRates);
            stem.DoTissueTurnover(turnoverRates);

            // - Stolons
            if (isLegume)
            {
                turnoverRates = new double[] { gamaS * RelativeTurnoverEmerging, gamaS, gamaS, 1.0 };
                stolon.DoTissueTurnover(turnoverRates);
            }

            // - Roots (only 2 tissues)
            turnoverRates = new double[] { gamaR, 1.0 };

            var rootBiomassDetached = root[0].DoTissueTurnover(turnoverRates);
            //foreach (PastureBelowGroundOrgan root in roots)
            //    root.DoTissueTurnover(turnoverRates);
            // TODO: currently only the roots at the main/home zone are considered, must add the other zones too

            // TODO: consider C remobilisation
            // ChRemobSugar = dSenescedRoot * KappaCRemob;
            // ChRemobProtein = dSenescedRoot * (roots.Tissue[0].Nconc - roots.NConcMinimum) * CNratioProtein * FacCNRemob;
            // senescedRootDM -= ChRemobSugar + ChRemobProtein;
            // CRemobilisable += ChRemobSugar + ChRemobProtein;

            // C remobilised from senesced tissues to be used in new growth (converted from carbohydrate to C)
            remobilisableC += 0.0;
            remobilisableC *= CarbonFractionInDM;

            // Get the amounts detached today
            detachedShootDM = leaf.DMDetached + stem.DMDetached + stolon.DMDetached;
            detachedShootN = leaf.NDetached + stem.NDetached + stolon.NDetached;
            detachedRootDM = rootBiomassDetached.Wt;
            detachedRootN = rootBiomassDetached.N;
            //foreach (PastureBelowGroundOrgan root in roots)
            //{
            //    detachedRootDM += root.DMDetached;
            //    detachedRootN += root.NDetached;
            // TODO: currently only the roots at the main/home zone are considered, must add the other zones too
            //}
        }

        /// <summary>Computes the allocation of new growth to all tissues in each organ.</summary>
        internal void EvaluateNewGrowthAllocation()
        {
            if (dGrowthAfterNutrientLimitations > Epsilon)
            {
                // Get the actual growth above and below ground
                dGrowthShootDM = dGrowthAfterNutrientLimitations * fractionToShoot;
                dGrowthRootDM = Math.Max(0.0, dGrowthAfterNutrientLimitations - dGrowthShootDM);

                // Get the fractions of new growth to allocate to each plant organ
                double toLeaf = fractionToShoot * fractionToLeaf;
                double toStem = fractionToShoot * (1.0 - FractionToStolon - fractionToLeaf);
                double toStolon = fractionToShoot * FractionToStolon;
                double toRoot = 1.0 - fractionToShoot;

                double dmToRoot = 0;
                double nToRoot = 0;

                // Allocate new DM growth to the growing tissues
                leaf.Tissue[0].DMTransferedIn += toLeaf * dGrowthAfterNutrientLimitations;
                stem.Tissue[0].DMTransferedIn += toStem * dGrowthAfterNutrientLimitations;
                stolon.Tissue[0].DMTransferedIn += toStolon * dGrowthAfterNutrientLimitations;
                dmToRoot = toRoot * dGrowthAfterNutrientLimitations;
                // TODO: currently only the roots at the main / home zone are considered, must add the other zones too

                // Evaluate allocation of N
                if (dNewGrowthN > demandOptimumN)
                {
                    // Available N was more than enough to meet basic demand (i.e. there is luxury uptake)
                    // allocate N taken up based on maximum N content
                    double Nsum = (toLeaf * leaf.NConcMaximum) + (toStem * stem.NConcMaximum)
                                + (toStolon * stolon.NConcMaximum) + (toRoot * root[0].NConcMaximum);
                    if (Nsum > Epsilon)
                    {
                        leaf.Tissue[0].NTransferedIn += dNewGrowthN * toLeaf * leaf.NConcMaximum / Nsum;
                        stem.Tissue[0].NTransferedIn += dNewGrowthN * toStem * stem.NConcMaximum / Nsum;
                        stolon.Tissue[0].NTransferedIn += dNewGrowthN * toStolon * stolon.NConcMaximum / Nsum;
                        nToRoot = dNewGrowthN * toRoot * root[0].NConcMaximum / Nsum;
                        // TODO: currently only the roots at the main / home zone are considered, must add the other zones too
                    }
                    else
                    {
                        // something went horribly wrong to get here
                        throw new ApsimXException(this, "Allocation of new growth could not be completed");
                    }
                }
                else
                {
                    // Available N was not enough to meet basic demand, allocate N taken up based on optimum N content
                    double Nsum = (toLeaf * leaf.NConcOptimum) + (toStem * stem.NConcOptimum)
                                + (toStolon * stolon.NConcOptimum) + (toRoot * root[0].NConcOptimum);
                    if (Nsum > Epsilon)
                    {
                        leaf.Tissue[0].NTransferedIn += dNewGrowthN * toLeaf * leaf.NConcOptimum / Nsum;
                        stem.Tissue[0].NTransferedIn += dNewGrowthN * toStem * stem.NConcOptimum / Nsum;
                        stolon.Tissue[0].NTransferedIn += dNewGrowthN * toStolon * stolon.NConcOptimum / Nsum;
                        nToRoot = dNewGrowthN * toRoot * root[0].NConcOptimum / Nsum;
                        // TODO: currently only the roots at the main / home zone are considered, must add the other zones too
                    }
                    else
                    {
                        // something went horribly wrong to get here
                        throw new ApsimXException(this, "Allocation of new growth could not be completed");
                    }
                }
                var rootGrowth = root[0].Tissue[0].SetNewGrowthAllocation(dmToRoot, nToRoot);

                // Update N variables
                dGrowthShootN = leaf.Tissue[0].NTransferedIn + stem.Tissue[0].NTransferedIn + stolon.Tissue[0].NTransferedIn;
                dGrowthRootN = rootGrowth.N;
                //foreach (PastureBelowGroundOrgan root in roots)
                //    dGrowthRootN += root.Tissue[0].NTransferedIn;
                // TODO: currently only the roots at the main / home zone are considered, must add the other zones too

                // Evaluate root elongation and allocate new growth in each layer
                if (phenologicStage > 0)
                    root[0].EvaluateRootElongation(dGrowthRootDM, detachedRootDM, TemperatureLimitingFactor(Tmean(0.5)));

                root[0].DoRootGrowthAllocation(dGrowthRootDM, dGrowthRootN);
            }
            else
            {
                // no actual growth, just zero out some variables
                dGrowthShootDM = 0.0;
                dGrowthRootDM = 0.0;
            }
        }

        /// <summary>Calculates the plant actual growth and update DM, N, LAI and digestibility.</summary>
        internal void DoActualGrowthAndAllocation()
        {
            // Effective, or net, growth
            dGrowthNet = (dGrowthShootDM - detachedShootDM) + (dGrowthRootDM - detachedRootDM);

            // Save some variables for mass balance check
            double preTotalWt = AboveGroundWt + BelowGroundWt;
            double preTotalN = AboveGroundN + BelowGroundN;

            // Update each organ, returns test for mass balance
            if (leaf.DoOrganUpdate() == false)
                throw new ApsimXException(this, "Growth and tissue turnover resulted in loss of mass balance for leaves");

            if (stem.DoOrganUpdate() == false)
                throw new ApsimXException(this, "Growth and tissue turnover resulted in loss of mass balance for stems");

            if (stolon.DoOrganUpdate() == false)
                throw new ApsimXException(this, "Growth and tissue turnover resulted in loss of mass balance for stolons");

            root[0].DoOrganUpdate();

            // TODO: currently only the roots at the main / home zone are considered, must add the other zones too

            double postTotalWt = AboveGroundWt + BelowGroundWt;
            double postTotalN = AboveGroundN + BelowGroundN;

            // Check for loss of mass balance in the whole plant
            if (!MathUtilities.FloatsAreEqual(preTotalWt + dGrowthAfterNutrientLimitations - detachedShootDM - detachedRootDM - postTotalWt,0))
                throw new ApsimXException(this, "  " + Name + " - Growth and tissue turnover resulted in loss of mass balance");

            if (!MathUtilities.FloatsAreEqual(preTotalN + dNewGrowthN - senescedNRemobilised - luxuryNRemobilised - detachedShootN - detachedRootN - postTotalN,0))
                throw new ApsimXException(this, "  " + Name + " - Growth and tissue turnover resulted in loss of mass balance");

            // Update LAI
            EvaluateLAI();

            // Update digestibility
            EvaluateDigestibility();
        }

        /// <summary>Evaluates the phenological stage of annual plants.</summary>
        /// <remarks>
        /// This method keeps track of days after emergence as well as cumulative degrees days, it uses both to evaluate the progress
        ///  through each phase. The two approaches are used concomitantly to enable some basic sensitivity to environmental factors,
        ///  but also to ensure that plants will complete their cycle (as the controls used here are rudimentary).
        /// This method also update the value of phenoFactor, using the estimated progress through the current phenological stage.
        /// </remarks>
        private void EvaluatePhenologyOfAnnuals()
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
                cumulativeDDVegetative += Math.Max(0.0, Tmean(0.5) - GrowthTminimum);

                // Note, germination is considered together with perennials in DailyGerminationProgress

                // check development over vegetative growth
                if ((daysSinceEmergence == daysEmergenceToAnthesis) || (cumulativeDDVegetative >= degreesDayForAnthesis))
                {
                    phenologicStage = 2;
                    cumulativeDDVegetative = Math.Max(cumulativeDDVegetative, degreesDayForAnthesis);
                }

                phenoFactor1 = MathUtilities.Divide(daysSinceEmergence, daysEmergenceToAnthesis, 1.0);
                phenoFactor2 = MathUtilities.Divide(cumulativeDDVegetative, degreesDayForAnthesis, 1.0);

                // check development over reproductive growth
                if (phenologicStage > 1)
                {
                    if ((daysSinceEmergence == daysEmergenceToAnthesis + daysAnthesisToMaturity) || (cumulativeDDVegetative >= degreesDayForMaturity))
                    {
                        cumulativeDDVegetative = Math.Max(cumulativeDDVegetative, degreesDayForMaturity);
                        EndCrop();
                    }

                    phenoFactor1 = MathUtilities.Divide(daysSinceEmergence - daysEmergenceToAnthesis, daysAnthesisToMaturity, 1.0);
                    phenoFactor2 = MathUtilities.Divide(cumulativeDDVegetative - degreesDayForAnthesis, degreesDayForMaturity, 1.0);
                }

                // set the phenology factor (fraction of current phase)
                phenoFactor = Math.Max(phenoFactor1, phenoFactor2);
            }
        }

        #endregion  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        #region - Water uptake processes  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

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

        /// <summary>Gets the water uptake for each layer as calculated by an external module (SWIM).</summary>
        /// <param name="SoilWater">The soil water uptake data</param>
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

        #endregion  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        #region - Nitrogen uptake processes - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Performs the nitrogen uptake calculations.</summary>
        internal void DoNitrogenCalculations()
        {
            if (MyNitrogenUptakeSource == "species")
            {
                throw new NotImplementedException();
            }
            else if (MyNitrogenUptakeSource == "SoilArbitrator")
            {
                // Nitrogen uptake was computed by the resource arbitrator

                // Evaluate whether remobilisation of luxury N is needed
                EvaluateLuxuryNRemobilisation();
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
            double toRoot = dGrowthAfterWaterLimitations * (1.0 - fractionToShoot);
            double toStol = dGrowthAfterWaterLimitations * fractionToShoot * FractionToStolon;
            double toLeaf = dGrowthAfterWaterLimitations * fractionToShoot * fractionToLeaf;
            double toStem = dGrowthAfterWaterLimitations * fractionToShoot * (1.0 - FractionToStolon - fractionToLeaf);

            // N demand for new growth, with optimum N (kg/ha)
            demandOptimumN = (toLeaf * leaf.NConcOptimum) + (toStem * stem.NConcOptimum)
                       + (toStol * stolon.NConcOptimum) + (toRoot * root[0].NConcOptimum);

            // get the factor to reduce the demand under elevated CO2
            double fN = NOptimumVariationDueToCO2();
            demandOptimumN *= fN;

            // N demand for new growth, with luxury uptake (maximum [N])
            demandLuxuryN = (toLeaf * leaf.NConcMaximum) + (toStem * stem.NConcMaximum)
                       + (toStol * stolon.NConcMaximum) + (toRoot * root[0].NConcMaximum);
            // It is assumed that luxury uptake is not affected by CO2 variations
        }

        /// <summary>Computes the amount of atmospheric nitrogen fixed through symbiosis.</summary>
        internal void EvaluateNitrogenFixation()
        {
            double adjNDemand = demandOptimumN * GlfSoilFertility;
            if (isLegume && adjNDemand > Epsilon)
            {
                // Start with minimum fixation
                fixedN = MinimumNFixation * adjNDemand;

                // Evaluate N stress
                double Nstress = Math.Max(0.0, MathUtilities.Divide(SoilAvailableN, adjNDemand - fixedN, 1.0));

                // Update N fixation if under N stress
                if (Nstress < 0.99)
                    fixedN += (MaximumNFixation - MinimumNFixation) * (1.0 - Nstress) * adjNDemand;
            }
        }

        /// <summary>Calculates the costs of N fixation</summary>
        /// <remarks>
        /// This approach separates maintenance and activity costs, based roughly on results from:
        ///   Rainbird RM, Hitz WD, Hardy RWF 1984. Experimental determination of the respiration associated with soybean/rhizobium 
        ///     nitrogenase function, nodule maintenance, and total nodule nitrogen fixation. Plant Physiology 75(1): 49-53.
        ///   Voisin AS, Salon C, Jeudy C, Warembourg FR 2003. Symbiotic N2 fixation activity in relation to C economy of Pisum sativum L.
        ///     as a function of plant phenology. Journal of Experimental Botany 54(393): 2733-2744.
        ///   Minchin FR, Witty JF 2005. Respiratory/carbon costs of symbiotic nitrogen fixation in legumes. In: Lambers H, Ribas-Carbo 
        ///     M eds. Plant Respiration. Advances in Photosynthesis and Respiration, Springer Netherlands. Pp. 195-205.
        /// NOTE: This procedure will use today's DM for maintenance costs, but yesterday's fixedN for activity (as today's fixation has 
        ///  not been calculated yet).
        /// </remarks>
        /// <returns>The amount of carbon spent on N fixation (kg/ha)</returns>
        private double DailyNFixationCosts()
        {
            double fixationCost = 0.0;
            if ((SymbiontCostFactor > Epsilon) || (NFixingCostFactor > Epsilon))
            {
                //  respiration cost of symbiont (presence of rhizobia is assumed to be proportional to root mass)
                double Tfactor = TemperatureEffectOnRespiration(Tmean(0.5));
                double maintenanceCost = BelowGroundLiveWt * CarbonFractionInDM * SymbiontCostFactor * Tfactor;

                //  respiration cost of actual N fixation (assumed as a simple linear function of N fixed)
                double activityCost = fixedN * NFixingCostFactor;

                fixationCost = maintenanceCost + activityCost;
            }

            return fixationCost;
        }

        /// <summary>Evaluates the use of remobilised nitrogen and computes soil nitrogen demand.</summary>
        internal void EvaluateSoilNitrogenDemand()
        {
            double fracRemobilised = 0.0;
            double adjNDemand = demandLuxuryN * GlfSoilFertility;
            if (adjNDemand - fixedN < Epsilon)
            {
                // N demand is fulfilled by fixation alone
                senescedNRemobilised = 0.0;
                mySoilNDemand = 0.0;
            }
            else if (adjNDemand - (fixedN + RemobilisableSenescedN) < Epsilon)
            {
                // N demand is fulfilled by fixation plus N remobilised from senesced material
                senescedNRemobilised = Math.Max(0.0, adjNDemand - fixedN);
                mySoilNDemand = 0.0;
                fracRemobilised = MathUtilities.Divide(senescedNRemobilised, RemobilisableSenescedN, 0.0);
            }
            else
            {
                // N demand is greater than fixation and remobilisation, N uptake is needed
                senescedNRemobilised = RemobilisableSenescedN;
                mySoilNDemand = adjNDemand * GlfSoilFertility - (fixedN + senescedNRemobilised);
                fracRemobilised = 1.0;
            }

            // Update N remobilised in each organ
            if (senescedNRemobilised > Epsilon)
            {
                leaf.Tissue[leaf.Tissue.Length - 1].DoRemobiliseN(fracRemobilised);
                stem.Tissue[stem.Tissue.Length - 1].DoRemobiliseN(fracRemobilised);
                stolon.Tissue[stolon.Tissue.Length - 1].DoRemobiliseN(fracRemobilised);
                root[0].Tissue[root[0].Tissue.Length - 1].DoRemobiliseN(fracRemobilised);
                //foreach (PastureBelowGroundOrgan root in roots)
                //    root.Tissue[root.Tissue.Length - 1].DoRemobiliseN(fracRemobilised);
                // TODO: currently only the roots at the main / home zone are considered, must add the other zones too
            }
        }

        /// <summary>Computes the amount of luxury nitrogen remobilised into new growth.</summary>
        internal void EvaluateLuxuryNRemobilisation()
        {
            // check whether there is still demand for N (only match demand for growth at optimum N conc.)
            // check whether there is any luxury N remobilisable
            double Nmissing = demandOptimumN * GlfSoilFertility - (fixedN + senescedNRemobilised + SoilUptakeN);
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
                            leaf.Tissue[tissue].DoRemobiliseN(1.0);
                            stem.Tissue[tissue].DoRemobiliseN(1.0);
                            stolon.Tissue[tissue].DoRemobiliseN(1.0);
                            if (tissue == 0)
                            {
                                root[0].Tissue[tissue].DoRemobiliseN(1.0);
                                //foreach (PastureBelowGroundOrgan root in roots)
                                //    root.Tissue[tissue].DoRemobiliseN(1.0);
                                // TODO: currently only the roots at the main / home zone are considered, must add the other zones too
                            }
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
                        Nluxury = leaf.Tissue[tissue].NRemobilisable + stem.Tissue[tissue].NRemobilisable + stolon.Tissue[tissue].NRemobilisable;
                        if (tissue == 0)
                        {
                            Nluxury += root[0].Tissue[tissue].NRemobilisable;
                            //foreach (PastureBelowGroundOrgan root in roots)
                            //    Nluxury += root.Tissue[tissue].NRemobilisable;
                            // TODO: currently only the roots at the main / home zone are considered, must add the other zones too
                        }
                        Nusedup = Math.Min(Nluxury, Nmissing);
                        fracRemobilised = MathUtilities.Divide(Nusedup, Nluxury, 0.0);
                        leaf.Tissue[tissue].DoRemobiliseN(fracRemobilised);
                        stem.Tissue[tissue].DoRemobiliseN(fracRemobilised);
                        stolon.Tissue[tissue].DoRemobiliseN(fracRemobilised);
                        if (tissue == 0)
                        {
                            root[0].Tissue[tissue].DoRemobiliseN(fracRemobilised);
                            //foreach (PastureBelowGroundOrgan root in roots)
                            //    root.Tissue[tissue].DoRemobiliseN(fracRemobilised);
                            // TODO: currently only the roots at the main / home zone are considered, must add the other zones too
                        }

                        luxuryNRemobilised += Nusedup;
                        Nmissing -= Nusedup;
                        if (Nmissing <= Epsilon) tissue = 0;
                    }
                }
            }
        }

        #endregion  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        #region - Organic matter processes  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Adds a given amount of detached plant material (DM and N) to the surface organic matter.</summary>
        /// <param name="amountDM">The DM amount to send (kg/ha)</param>
        /// <param name="amountN">The N amount to send (kg/ha)</param>
        private void DoAddDetachedShootToSurfaceOM(double amountDM, double amountN)
        {
            if (amountDM + amountN > 0.0)
            {
                if (BiomassRemoved != null)
                {
                    BiomassRemovedType biomassData = new BiomassRemovedType();
                    string[] type = { mySpeciesFamily.ToString() };
                    float[] dltdm = { (float)amountDM };
                    float[] dltn = { (float)amountN };
                    float[] dltp = { 0f }; // P not considered here
                    float[] fraction = { 1f }; // fraction is always 1.0 here

                    biomassData.crop_type = "grass"; //TODO: this could be the Name, what is the diff between name and type??
                    biomassData.dm_type = type;
                    biomassData.dlt_crop_dm = dltdm;
                    biomassData.dlt_dm_n = dltn;
                    biomassData.dlt_dm_p = dltp;
                    biomassData.fraction_to_residue = fraction;
                    BiomassRemoved.Invoke(biomassData);
                }
            }
        }

        #endregion  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        #region - DM allocation and related processes - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Gets the allocations into shoot and leaves of today's growth.</summary>
        internal void GetAllocationFractions()
        {
            // this is used when Sward is controlling growth. TODO: delete this when removing control from SWARD
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
            if (BelowGroundLiveWt > Epsilon)
            {
                // get the soil related growth limiting factor (the smaller this is the higher the allocation of DM to roots)
                double glfMin = Math.Min(Math.Min(glfWaterSupply, glfWaterLogging), glfNSupply);

                // get the actual effect of limiting factors on SR (varies between one and ShootRootGlfFactor)
                double glfFactor = 1.0 - ShootRootGlfFactor * (1.0 - Math.Pow(glfMin, 1.0 / ShootRootGlfFactor));

                // get the current shoot/root ratio (partition will try to make this value closer to targetSR)
                double currentSR = MathUtilities.Divide(AboveGroundLiveWt, BelowGroundLiveWt, double.MaxValue);

                // get the factor for the reproductive season of perennials (increases shoot allocation during spring)
                double reproFac = 1.0;
                if (usingReproSeasonFactor && !isAnnual)
                    reproFac = CalcReproductiveGrowthFactor();

                // get today's target SR
                double targetSR = TargetShootRootRatio * reproFac;

                // update today's shoot:root partition
                double growthSR = MathUtilities.Divide(targetSR * glfFactor * targetSR, currentSR, double.MaxValue - 1.5);

                // compute fraction to shoot
                fractionToShoot = growthSR / (1.0 + growthSR);
            }
            else
            {
                // use default value, this should not happen (might happen if plant is dead)
                fractionToShoot = 1.0;
            }

            // check for maximum root allocation (kept here mostly for backward compatibility)
            if ((1.0 - fractionToShoot) > MaxRootAllocation)
                fractionToShoot = 1.0 - MaxRootAllocation;
        }

        /// <summary>Computes the fraction of new shoot DM that is allocated to leaves.</summary>
        /// <remarks>
        /// This method is used to reduce the proportion of leaves as plants grow, this is used for species that 
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

            if (leaf.DMLive > 0.0)
            {
                // get current leaf:stem ratio
                double currentLS = MathUtilities.Divide(leaf.DMLive, stem.DMLive + stolon.DMLive, double.MaxValue);

                // get today's target leaf:stem ratio
                double targetLS = targetFLeaf / (1.0 - targetFLeaf);

                // adjust leaf:stem ratio, to avoid excess allocation to stem/stolons
                double newLS = MathUtilities.Divide(targetLS * targetLS, currentLS, double.MaxValue - 1.5);

                fractionToLeaf = newLS / (1.0 + newLS);
            }
            else
                fractionToLeaf = FractionLeafMaximum;
        }

        /// <summary>Calculates the plant height as function of DM.</summary>
        /// <returns>The plant height (mm)</returns>
        internal double HeightfromDM()
        {
            double TodaysHeight = PlantHeightMaximum;

            if (isAlive)
            {
                if (Harvestable.Wt <= PlantHeightMassForMax)
                {
                    double massRatio = Harvestable.Wt / PlantHeightMassForMax;
                    double heightF = PlantHeightExponent - (PlantHeightExponent * massRatio) + massRatio;
                    heightF *= Math.Pow(massRatio, PlantHeightExponent - 1);
                    TodaysHeight = Math.Max(TodaysHeight * heightF, PlantHeightMinimum);
                }
            }
            else
                TodaysHeight = 0.0;
            return TodaysHeight;
        }

        /// <summary>Computes the values of LAI (leaf area index) for green and dead plant material.</summary>
        /// <remarks>This method considers leaves plus an additional effect of stems and stolons</remarks>
        private void EvaluateLAI()
        {
            // Get the amount of green tissue of leaves (converted from kg/ha to kg/m2)
            double greenTissue = leaf.DMLive / 10000.0;

            // Get a proportion of green tissue from stolons
            greenTissue += stolon.DMLive * StolonEffectOnLAI / 10000.0;

            // Consider some green tissue from stems (if DM is very low)
            if (!isLegume && AboveGroundLiveWt < ShootMaxEffectOnLAI)
            {
                double shootFactor = MaxStemEffectOnLAI * Math.Sqrt(1.0 - (AboveGroundLiveWt / ShootMaxEffectOnLAI));
                greenTissue += stem.DMLive * shootFactor / 10000.0;

                /* This adjust helps on resilience after unfavoured conditions (implemented by F.Li, not present in EcoMod)
                   It is assumed that green cover will be bigger for the same amount of DM when compared to using only leaves
                     due to the recruitment of green tissue from stems. Thus it mimics:
                     - greater light extinction coefficient, leaves will be more horizontal than in dense high swards
                     - more parts (stems) turning green for photosynthesis
                     - thinner leaves during growth burst following unfavoured conditions
                     » TODO: It would be better if variations in SLA or ext. coeff. would be explicitly considered (RCichota, 2014)
                */
            }

            // Get the leaf area index for all green tissues
            greenLAI = greenTissue * SpecificLeafArea;

            // Get the leaf area index for dead tissues
            deadLAI = (leaf.DMDead / 10000.0) * SpecificLeafArea;
        }

        #endregion  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        #endregion  --------------------------------------------------------------------------------------------------------

        #region Intermittent processes  ------------------------------------------------------------------------------------

        /// <summary>Kills a fraction of this plant.</summary>
        /// <param name="fractionToKill">The fraction of crop to be killed (0-1)</param>
        public void KillCrop(double fractionToKill)
        {
            if (fractionToKill < 1.0)
            {
                // transfer fraction of live tissues into dead, will be detached later
                leaf.DoKillOrgan(fractionToKill);
                stem.DoKillOrgan(fractionToKill);
                stolon.DoKillOrgan(fractionToKill);
                foreach (PastureBelowGroundOrgan root in root)
                    root.DoKillOrgan(fractionToKill);
            }
            else
            {
                // kill off the plant
                EndCrop();
            }
        }

        /// <summary>Resets this plant state to its initial values.</summary>
        public void Reset()
        {
            leaf.DoResetOrgan();
            stem.DoResetOrgan();
            stolon.DoResetOrgan();
            foreach (PastureBelowGroundOrgan root in root)
                root.DoResetOrgan();
            SetInitialState();
        }

        /// <summary>Removes plant material simulating a graze event.</summary>
        /// <param name="type">The type of amount being defined (SetResidueAmount or SetRemoveAmount)</param>
        /// <param name="amount">The DM amount (kg/ha)</param>
        /// <exception cref="System.Exception"> Type of amount to remove on graze not recognized (use 'SetResidueAmount' or 'SetRemoveAmount'</exception>
        public void RemoveBiomass(string type, double amount)
        {
            if (isAlive && Harvestable.Wt > Epsilon)
            {
                // Get the amount required to remove
                double amountRequired;
                if (type.ToLower() == "setresidueamount")
                {
                    // Remove all DM above given residual amount
                    amountRequired = Math.Max(0.0, AboveGroundWt - amount);
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
                double amountToRemove = Math.Max(0.0, Math.Min(amountRequired, Harvestable.Wt));

                // Do the actual removal
                if (amountToRemove > Epsilon)
                    RemoveBiomass(amountToRemove);

            }
            else
                mySummary.WriteWarning(this, " Could not graze due to lack of DM available");
        }

        /// <summary>Removes a given amount of biomass (and N) from the plant.</summary>
        /// <param name="amountToRemove">The amount of biomass to remove (kg/ha)</param>
        public Biomass RemoveBiomass(double amountToRemove)
        {
            // get existing DM and N amounts
            double preRemovalDMShoot = AboveGroundWt;
            double preRemovalNShoot = AboveGroundN;

            if (amountToRemove > Epsilon)
            {
                // Compute the fraction of each tissue to be removed
                double[] fracRemoving = new double[6];
                if (amountToRemove - Harvestable.Wt > -Epsilon)
                {
                    // All existing DM is removed
                    amountToRemove = Harvestable.Wt;
                    fracRemoving[0] = MathUtilities.Divide(leaf.DMLiveHarvestable, Harvestable.Wt, 0.0);
                    fracRemoving[1] = MathUtilities.Divide(stem.DMLiveHarvestable, Harvestable.Wt, 0.0);
                    fracRemoving[2] = MathUtilities.Divide(stolon.DMLiveHarvestable, Harvestable.Wt, 0.0);
                    fracRemoving[3] = MathUtilities.Divide(leaf.DMDeadHarvestable, Harvestable.Wt, 0.0);
                    fracRemoving[4] = MathUtilities.Divide(stem.DMDeadHarvestable, Harvestable.Wt, 0.0);
                    fracRemoving[5] = MathUtilities.Divide(stolon.DMDeadHarvestable, Harvestable.Wt, 0.0);
                }
                else
                {
                    // Initialise the fractions to be removed (these will be normalised later)
                    fracRemoving[0] = leaf.DMLiveHarvestable * PreferenceForGreenOverDead * PreferenceForLeafOverStems;
                    fracRemoving[1] = stem.DMLiveHarvestable * PreferenceForGreenOverDead;
                    fracRemoving[2] = stolon.DMLiveHarvestable * PreferenceForGreenOverDead;
                    fracRemoving[3] = leaf.DMDeadHarvestable * PreferenceForLeafOverStems;
                    fracRemoving[4] = stem.DMDeadHarvestable;
                    fracRemoving[5] = stolon.DMDeadHarvestable;

                    // Get fraction potentially removable (maximum fraction of each tissue in the removing amount)
                    double[] fracRemovable = new double[6];
                    fracRemovable[0] = leaf.DMLiveHarvestable / amountToRemove;
                    fracRemovable[1] = stem.DMLiveHarvestable / amountToRemove;
                    fracRemovable[2] = stolon.DMLiveHarvestable / amountToRemove;
                    fracRemovable[3] = leaf.DMDeadHarvestable / amountToRemove;
                    fracRemovable[4] = stem.DMDeadHarvestable / amountToRemove;
                    fracRemovable[5] = stolon.DMDeadHarvestable / amountToRemove;

                    // Normalise the fractions of each tissue to be removed, they should add to one
                    double totalFrac = fracRemoving.Sum();
                    for (int i = 0; i < 6; i++)
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
                        for (int i = 0; i < 6; i++)
                            fracRemoving[i] = Math.Min(fracRemovable[i], fracRemoving[i] / totalFrac);
                        totalFrac = fracRemoving.Sum();
                        if (count > 1000)
                        {
                            mySummary.WriteWarning(this, " AgPasture could not remove or graze all the DM required for " + Name);
                            break;
                        }
                    }
                }

                // Get digestibility of DM being harvested (do this before updating pools)
                double greenDigestibility = (leaf.DigestibilityLive * fracRemoving[0]) + (stem.DigestibilityLive * fracRemoving[1])
                                            + (stolon.DigestibilityLive * fracRemoving[2]);
                double deadDigestibility = (leaf.DigestibilityDead * fracRemoving[3]) + (stem.DigestibilityDead * fracRemoving[4]) + (stolon.DigestibilityDead * fracRemoving[5]);
                defoliatedDigestibility = greenDigestibility + deadDigestibility;

                // Remove biomass from the organs.
                leaf.RemoveBiomass(
                    new OrganBiomassRemovalType() 
                    {
                        FractionLiveToRemove = Math.Max(0.0, MathUtilities.Divide(amountToRemove * fracRemoving[0], leaf.DMLiveHarvestable, 0.0)),
                        FractionDeadToRemove = Math.Max(0.0, MathUtilities.Divide(amountToRemove * fracRemoving[3], leaf.DMDeadHarvestable, 0.0))
                    });
                stem.RemoveBiomass(
                    new OrganBiomassRemovalType()
                    {
                        FractionLiveToRemove = Math.Max(0.0, MathUtilities.Divide(amountToRemove * fracRemoving[1], stem.DMLiveHarvestable, 0.0)),
                        FractionDeadToRemove = Math.Max(0.0, MathUtilities.Divide(amountToRemove * fracRemoving[4], stem.DMDeadHarvestable, 0.0))
                    });
                stolon.RemoveBiomass(
                    new OrganBiomassRemovalType()
                    {
                        FractionLiveToRemove = Math.Max(0.0, MathUtilities.Divide(amountToRemove * fracRemoving[2], stolon.DMLiveHarvestable, 0.0)),
                        FractionDeadToRemove = Math.Max(0.0, MathUtilities.Divide(amountToRemove * fracRemoving[5], stolon.DMDeadHarvestable, 0.0))
                    });

                // Update LAI and herbage digestibility
                EvaluateLAI();
                EvaluateDigestibility();
            }

            // Set outputs and check balance
            var defoliatedDM = preRemovalDMShoot - AboveGroundWt;
            var defoliatedN = preRemovalNShoot - AboveGroundN;
            if (!MathUtilities.FloatsAreEqual(defoliatedDM, amountToRemove))
                throw new ApsimXException(this, "  AgPasture " + Name + " - removal of DM resulted in loss of mass balance");
            else
                mySummary.WriteMessage(this, " Biomass removed from " + Name + " by grazing: " + defoliatedDM.ToString("#0.0") + "kg/ha");

            return new Biomass()
            {
                StructuralWt = defoliatedDM,
                StructuralN = defoliatedN,
                DMDOfStructural = defoliatedDigestibility
            };
        }

        /// <summary>
        /// Remove biomass from an organ.
        /// </summary>
        /// <param name="organName">Name of organ.</param>
        /// <param name="biomassRemoveType">Name of event that triggered this biomass remove call.</param>
        /// <param name="biomassToRemove">Biomass to remove.</param>
        public void RemoveBiomass(string organName, string biomassRemoveType, OrganBiomassRemovalType biomassToRemove)
        {
            var organ = Organs.Find(o => o.Name == organName);
            if (organ == null)
                throw new Exception("Cannot find organ to remove biomass from. Organ: " + organName);
            if (organ is PastureAboveGroundOrgan)
                (organ as PastureAboveGroundOrgan).RemoveBiomass(biomassToRemove);
            else if (organ is PastureBelowGroundOrgan)
                (organ as PastureBelowGroundOrgan).RemoveBiomass(biomassRemoveType, biomassToRemove);
        }

        #endregion  --------------------------------------------------------------------------------------------------------

        #region Auxiliary functions and processes  -------------------------------------------------------------------------

        /// <summary>Computes a growth factor for annual species, related to phenology/population.</summary>
        /// <returns>A growth factor (0-1)</returns>
        private double AnnualSpeciesGrowthFactor()
        {
            double rFactor = 1.0;
            if (phenologicStage == 1 && daysSinceEmergence < daysAnnualsFactor)
            {
                // reduction at the beginning due to population effects ???
                rFactor -= 0.5 * (1.0 - (daysSinceEmergence / daysAnnualsFactor));
            }
            else if (phenologicStage == 2)
            {
                // decline of photosynthesis when approaching maturity
                rFactor -= (daysSinceEmergence - daysEmergenceToAnthesis) / daysAnthesisToMaturity;
            }

            return rFactor;
        }

        /// <summary>Computes the relative effect of atmospheric CO2 on photosynthesis.</summary>
        /// <returns>A factor to adjust photosynthesis (0-1)</returns>
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
        /// This mimics the effect that N concentration have on the amount of chlorophyll (assumed directly proportional to N conc.).
        /// The effect is itself adjusted by a factor function of atmospheric CO2 (plants need less N at high CO2).
        /// </remarks>
        /// <returns>A factor to adjust photosynthesis (0-1)</returns>
        private double NConcEffectOnPhotosynthesis()
        {
            // get variation in N optimum due to CO2
            double fN = NOptimumVariationDueToCO2();

            // get chlorophyll effect
            double effect = 0.0;
            if (leaf.NconcLive > leaf.NConcMinimum)
            {
                if (leaf.NconcLive < leaf.NConcOptimum * fN)
                    effect = MathUtilities.Divide(leaf.NconcLive - leaf.NConcMinimum, (leaf.NConcOptimum * fN) - leaf.NConcMinimum, 1.0);
                else
                    effect = 1.0;
            }

            effect = MathUtilities.Bound(effect, 0.0, 1.0);
            return effect;
        }

        /// <summary>Computes the variation in optimum N in leaves due to atmospheric CO2.</summary>
        /// <returns>A factor to adjust optimum N in leaves (0-1)</returns>
        private double NOptimumVariationDueToCO2()
        {
            if (Math.Abs(myMetData.CO2 - ReferenceCO2) < 0.01)
                return 1.0;

            double factorCO2 = Math.Pow((CO2EffectOffsetFactor - ReferenceCO2) / (myMetData.CO2 - ReferenceCO2), CO2EffectExponent);
            double effect = (CO2EffectMinimum + factorCO2) / (1 + factorCO2);

            return effect;
        }

        /// <summary>Computes the variation in stomata conductance due to variation in atmospheric CO2.</summary>
        /// <returns>The stomata conductance (m/s)</returns>
        private double CO2EffectOnConductance()
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

        /// <summary>Today's weighted average temperature.</summary>
        /// <param name="wTmax">The weight of Tmax with respect to Tmin</param>
        /// <returns>The average Temperature (oC)</returns>
        private double Tmean(double wTmax)
        {
            wTmax = MathUtilities.Bound(wTmax, 0.0, 1.0);
            return (myMetData.MaxT * wTmax) + (myMetData.MinT * (1.0 - wTmax));
        }

        /// <summary>Computes the reduction factor for photosynthesis due to heat damage.</summary>
        /// <remarks>Stress computed as function of daily maximum temperature, recovery based on average temperature</remarks>
        /// <returns>A factor to adjust photosynthesis (0-1)</returns>
        private double HeatStress()
        {
            if (usingHeatStressFactor)
            {
                double heatFactor;
                if (myMetData.MaxT > HeatFullTemperature)
                {
                    // very high temperature, full stress
                    heatFactor = 0.0;
                    cumulativeDDHeat = 0.0;
                }
                else if (myMetData.MaxT > HeatOnsetTemperature)
                {
                    // high temperature, add some stress
                    heatFactor = highTempStress * (HeatFullTemperature - myMetData.MaxT) / (HeatFullTemperature - HeatOnsetTemperature);
                    cumulativeDDHeat = 0.0;
                }
                else
                {
                    // cool temperature, same stress as yesterday
                    heatFactor = highTempStress;
                }

                // check recovery factor
                double recoveryFactor = 0.0;
                if (myMetData.MaxT <= HeatOnsetTemperature)
                    recoveryFactor = (1.0 - heatFactor) * (cumulativeDDHeat / HeatRecoverySumDD);

                // accumulate temperature
                cumulativeDDHeat += Math.Max(0.0, HeatRecoveryTReference - Tmean(0.5));

                // heat stress
                highTempStress = Math.Min(1.0, heatFactor + recoveryFactor);

                return highTempStress;
            }
            return 1.0;
        }

        /// <summary>Computes the reduction factor for photosynthesis due to cold damage (frost).</summary>
        /// <remarks>Stress computed as function of daily minimum temperature, recovery based on average temperature</remarks>
        /// <returns>A factor to adjust photosynthesis (0-1)</returns>
        private double ColdStress()
        {
            if (usingColdStressFactor)
            {
                double coldFactor;
                if (myMetData.MinT < ColdFullTemperature)
                {
                    // very low temperature, full stress
                    coldFactor = 0.0;
                    cumulativeDDCold = 0.0;
                }
                else if (myMetData.MinT < ColdOnsetTemperature)
                {
                    // low temperature, add some stress
                    coldFactor = lowTempStress * (myMetData.MinT - ColdFullTemperature) / (ColdOnsetTemperature - ColdFullTemperature);
                    cumulativeDDCold = 0.0;
                }
                else
                {
                    // warm temperature, same stress as yesterday
                    coldFactor = lowTempStress;
                }

                // check recovery factor
                double recoveryFactor = 0.0;
                if (myMetData.MinT >= ColdOnsetTemperature)
                    recoveryFactor = (1.0 - coldFactor) * (cumulativeDDCold / ColdRecoverySumDD);

                // accumulate temperature
                cumulativeDDCold += Math.Max(0.0, Tmean(0.5) - ColdRecoveryTReference);

                // cold stress
                lowTempStress = Math.Min(1.0, coldFactor + recoveryFactor);

                return lowTempStress;
            }
            else
                return 1.0;
        }

        /// <summary>Growth limiting factor due to temperature.</summary>
        /// <param name="temperature">The temperature</param>
        /// <returns>A factor to adjust photosynthesis (0-1)</returns>
        /// <exception cref="System.Exception">Photosynthesis pathway is not valid</exception>
        private double TemperatureLimitingFactor(double temperature)
        {
            double result = 0.0;
            double growthTmax = GrowthToptimum + (GrowthToptimum - GrowthTminimum) / GrowthTEffectExponent;
            if (PhotosyntheticPathway == PhotosynthesisPathwayType.C3)
            {
                if (temperature > GrowthTminimum && temperature < growthTmax)
                {
                    double val1 = Math.Pow((temperature - GrowthTminimum), GrowthTEffectExponent) * (growthTmax - temperature);
                    double val2 = Math.Pow((GrowthToptimum - GrowthTminimum), GrowthTEffectExponent) * (growthTmax - GrowthToptimum);
                    result = val1 / val2;
                }
            }
            else if (PhotosyntheticPathway == PhotosynthesisPathwayType.C4)
            {
                if (temperature > GrowthTminimum)
                {
                    if (temperature > GrowthToptimum)
                        temperature = GrowthToptimum;

                    double val1 = Math.Pow((temperature - GrowthTminimum), GrowthTEffectExponent) * (growthTmax - temperature);
                    double val2 = Math.Pow((GrowthToptimum - GrowthTminimum), GrowthTEffectExponent) * (growthTmax - GrowthToptimum);
                    result = val1 / val2;
                }
            }
            else
                throw new ApsimXException(this, "Photosynthetic pathway is not valid");
            return result;
        }

        /// <summary>Computes the effects of temperature on respiration.</summary>
        /// <param name="temperature">The temperature</param>
        /// <returns>A factor to adjust plant respiration (0-1)</returns>
        private double TemperatureEffectOnRespiration(double temperature)
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
                double baseEffect = 1.0 - Math.Exp(-Math.Pow(temperature / RespirationTReference, myRespirationExponent));
                result = baseEffect / scalef;
            }

            return result;
        }

        /// <summary>Effect of temperature on tissue turnover.</summary>
        /// <param name="temperature">The temperature</param>
        /// <returns>A factor to adjust tissue turnover (0-1)</returns>
        private double TempFactorForTissueTurnover(double temperature)
        {
            double result = 0.0;
            if (temperature > TurnoverTemperatureMin && temperature <= TurnoverTemperatureRef)
            {
                result = Math.Pow((temperature - TurnoverTemperatureMin) / (TurnoverTemperatureRef - TurnoverTemperatureMin), TurnoverTemperatureExponent);
            }
            else if (temperature > TurnoverTemperatureRef)
            {
                result = 1.0;
            }
            return result;
        }

        /// <summary>Computes the growth limiting factor due to soil moisture deficit.</summary>
        /// <returns>A limiting factor for plant growth (0-1)</returns>
        internal double WaterDeficitFactor()
        {
            double factor = MathUtilities.Divide(mySoilWaterUptake.Sum(), myWaterDemand, 1.0);
            return Math.Max(0.0, Math.Min(1.0, factor));
        }

        /// <summary>Computes the growth limiting factor due to excess of water in the soil (water logging/lack of aeration).</summary>
        /// <remarks>
        /// Growth is limited if soil water content is above a given threshold (defined by MinimumWaterFreePorosity), which
        ///  will be the soil DUL is MinimumWaterFreePorosity is set to a negative value. When water content is greater than
        ///  this water-free porosity growth will be limited. The function is based on the cumulative water logging, which means
        ///  that limitation are more severe if water logging conditions are persistent. Maximum increment in one day equals the 
        ///  SoilWaterSaturationFactor and cannot be greater than one. Recovery happens every if water content is below the full
        ///  saturation, and is proportional to the water-free porosity.
        /// </remarks>
        /// <returns>A limiting factor for plant growth (0-1)</returns>
        internal double WaterLoggingFactor()
        {
            double todaysEffect;
            double mySWater = 0.0;  // actual soil water content
            double myWSat = 0.0;    // water content at saturation
            double myWMinP = 0.0;   // water content at minimum water-free porosity
            double fractionLayer;   // fraction of layer with roots 

            // gather water status over the root zone
            for (int layer = 0; layer <= root[0].BottomLayer; layer++)
            {
                fractionLayer = FractionLayerWithRoots(layer);
                mySWater += mySoil.Water[layer] * fractionLayer;
                myWSat += mySoil.SATmm[layer] * fractionLayer;
                if (MinimumWaterFreePorosity <= -Epsilon)
                    myWMinP += mySoil.DULmm[layer] * fractionLayer;
                else
                    myWMinP = mySoil.SATmm[layer] * (1.0 - MinimumWaterFreePorosity) * fractionLayer;
            }

            if (mySWater > myWMinP)
            {
                todaysEffect = SoilSaturationEffectMax * (mySWater - myWMinP) / (myWSat - myWMinP);
                // allow some recovery of any water logging from yesterday is the soil is not fully saturated
                todaysEffect -= SoilSaturationRecoveryFactor * (myWSat - mySWater) / (myWSat - myWMinP) * cumWaterLogging;
            }
            else
                todaysEffect = -SoilSaturationRecoveryFactor;

            cumWaterLogging = MathUtilities.Bound(cumWaterLogging + todaysEffect, 0.0, 1.0);

            return 1.0 - cumWaterLogging;
        }

        /// <summary>Computes the effect of water stress on tissue turnover.</summary>
        /// <remarks>Tissue turnover is higher under water stress, GLFwater is used to mimic that effect.</remarks>
        /// <returns>A factor for adjusting tissue turnover (0-1)</returns>
        private double MoistureEffectOnTissueTurnover()
        {
            double effect = 1.0;
            if (Math.Min(glfWaterSupply, glfWaterLogging) < TurnoverDroughtThreshold)
            {
                effect = (TurnoverDroughtThreshold - Math.Min(glfWaterSupply, glfWaterLogging)) / TurnoverDroughtThreshold;
                effect = 1.0 + TurnoverDroughtEffectMax * effect;
            }

            return effect;
        }

        /// <summary>Computes the effect of defoliation on stolon/root turnover rate.</summary>
        /// <remarks>
        /// This approach spreads the effect over a few days after a defoliation, starting large and decreasing with time.
        /// It is assumed that a defoliation of 100% of harvestable material will result in a full decay of stolons.
        /// </remarks>
        /// <returns>A factor for adjusting tissue turnover (0-1)</returns>
        private double DefoliationEffectOnTissueTurnover()
        {
            double defoliationEffect = 0.0;
            cumDefoliationFactor += myDefoliatedFraction;
            if (cumDefoliationFactor > 0.0)
            {
                double todaysFactor = Math.Pow(cumDefoliationFactor, TurnoverDefoliationCoefficient + 1.0);
                todaysFactor /= (TurnoverDefoliationCoefficient + 1.0);
                if (cumDefoliationFactor - todaysFactor < TurnoverDefoliationEffectMin)
                {
                    defoliationEffect = cumDefoliationFactor;
                    cumDefoliationFactor = 0.0;
                }
                else
                {
                    defoliationEffect = cumDefoliationFactor - todaysFactor;
                    cumDefoliationFactor = todaysFactor;
                }
            }

            // clear fraction defoliated after use
            myDefoliatedFraction = 0.0;

            return defoliationEffect;
        }

        /// <summary>Compute the effect of drought on detachment rate.</summary>
        /// <remarks>Drought will decrease the rate of littering.</remarks>
        /// <returns>A factor for adjusting the detachment rate(0-1)</returns>
        private double MoistureEffectOnDetachment()
        {
            double effect = Math.Pow(glfWaterSupply, DetachmentDroughtCoefficient);
            effect *= Math.Max(0.0, 1.0 - DetachmentDroughtEffectMin);
            return DetachmentDroughtEffectMin + effect;
        }

        /// <summary>Calculates the factor increasing shoot allocation during reproductive growth.</summary>
        /// <remarks>
        /// This mimics the changes in DM allocation during reproductive season; allocation to shoot increases up to a maximum
        ///  value (defined by allocationIncreaseRepro). This value is used during the main phase, two shoulder periods are
        ///  defined on either side of the main phase (duration is given by reproSeasonInterval, translated into days of year),
        ///  Onset phase goes between doyA and doyB, main phase between doyB and doyC, and outset between doyC and doyD.
        /// Note: The days have to be set as doubles or the division operations will be rounded and be slightly wrong.
        /// </remarks>
        /// <returns>A factor to adjust DM allocation to shoot</returns>
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

        /// <summary>Computes the ground cover for the plant, or plant part.</summary>
        /// <param name="givenLAI">The LAI</param>
        /// <returns>The fraction of light effectively intercepted (MJ/MJ)</returns>
        private double CalcPlantCover(double givenLAI)
        {
            if (givenLAI < Epsilon) return 0.0;
            return (1.0 - Math.Exp(-LightExtinctionCoefficient * givenLAI));
        }

        /// <summary>Computes how much of the layer is actually explored by roots (considering depth only).</summary>
        /// <param name="layer">The index for the layer being considered</param>
        /// <returns>The fraction of the layer that is explored by roots (0-1)</returns>
        internal double FractionLayerWithRoots(int layer)
        {
            double fractionInLayer = 0.0;
            if (layer < root[0].BottomLayer)
            {
                fractionInLayer = 1.0;
            }
            else if (layer == root[0].BottomLayer)
            {
                double depthTillTopThisLayer = 0.0;
                for (int z = 0; z < layer; z++)
                    depthTillTopThisLayer += mySoil.Thickness[z];
                fractionInLayer = (root[0].Depth - depthTillTopThisLayer) / mySoil.Thickness[layer];
                fractionInLayer = Math.Min(1.0, Math.Max(0.0, fractionInLayer));
            }

            return fractionInLayer;
        }

        /// <summary>Gets the index of the layer at the bottom of the root zone.</summary>
        private int RootZoneBottomLayer()
        {
            int result = 0;
            double currentDepth = 0.0;
            for (int layer = 0; layer < nLayers; layer++)
            {
                if (root[0].Depth > currentDepth)
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
        /// <returns>The saturated vapour pressure (hPa?)</returns>
        private double svp(double temp)
        {
            return 6.1078 * Math.Exp(17.269 * temp / (237.3 + temp));
        }

        /// <summary>Computes the average digestibility of above-ground plant material.</summary>
        private void EvaluateDigestibility()
        {
            double result = 0.0;
            if (AboveGroundWt > Epsilon)
            {
                result = (leaf.DigestibilityTotal * leaf.DMTotal)
                       + (stem.DigestibilityTotal * stem.DMTotal)
                       + (stolon.DigestibilityTotal * stolon.DMTotal);
                result /= AboveGroundWt;
            }
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
                result = (leaf.DigestibilityLive * leafLiveWt) + (leaf.DigestibilityDead * leafDeadWt)
                       + (stem.DigestibilityLive * stemLiveWt) + (stem.DigestibilityDead * stemDeadWt)
                       + (stolon.DigestibilityLive * stolonLiveWt) + (stolon.DigestibilityDead * stolonDeadWt);
                result /= removedWt;
            }

            return result;
        }

        /// <summary>
        /// Set the plant leaf area index.
        /// </summary>
        /// <param name="deltaLAI">Delta LAI.</param>
        public void ReduceCanopy(double deltaLAI)
        {
            if (LAI > 0)
            {
                var prop = deltaLAI / LAI;
                leaf.RemoveBiomass(new OrganBiomassRemovalType() { FractionLiveToRemove = prop * leaf.DMLive });
            }
        }

        /// <summary>
        /// Set the plant root length density.
        /// </summary>
        /// <param name="deltaRLD">New root length density.</param>
        public void ReduceRootLengthDensity(double deltaRLD)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Remove an amount of assimilate from the plant.
        /// </summary>
        /// <param name="deltaAssimilate">The amount of assimilate to remove (g/m2).</param>
        public void RemoveAssimilate(double deltaAssimilate)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Reduce the plant population.
        /// </summary>
        /// <param name="newPlantPopulation">The new plant population.</param>
        public void ReducePopulation(double newPlantPopulation)
        {
            throw new Exception("AgPasture does not simulate a plant population and so plant population cannot be reduced.");
        }

        #endregion  --------------------------------------------------------------------------------------------------------
    }
}
