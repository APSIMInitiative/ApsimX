namespace Models.PMF.Organs
{
    using APSIM.Shared.Utilities;
    using Library;
    using Models.Core;
    using Models.Interfaces;
    using Models.Functions;
    using Models.PMF.Interfaces;
    using Models.Soils;
    using Models.Soils.Arbitrator;
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;

    ///<summary>
    /// # [Name]
    /// The generic root model calculates root growth in terms of rooting depth, biomass accumulation and subsequent root length density in each soil layer. 
    /// 
    /// **Root Growth**
    /// 
    /// Roots grow downwards through the soil profile, with initial depth determined by sowing depth and the growth rate determined by RootFrontVelocity. 
    /// The RootFrontVelocity is modified by multiplying it by the soil's XF value; which represents any resistance posed by the soil to root extension. 
    /// Root depth is also constrained by a maximum root depth.
    /// 
    /// Root length growth is calculated using the daily DM partitioned to roots and a specific root length.  Root proliferation in layers is calculated using an approach similar to the generalised equimarginal criterion used in economics.  The uptake of water and N per unit root length is used to partition new root material into layers of higher 'return on investment'.
    /// 
    /// **Dry Matter Demands**
    /// 
    /// A daily DM demand is provided to the organ arbitrator and a DM supply returned. By default, 100% of the dry matter (DM) demanded from the root is structural.  
    /// The daily loss of roots is calculated using a SenescenceRate function.  All senesced material is automatically detached and added to the soil FOM.  
    /// 
    /// **Nitrogen Demands**
    /// 
    /// The daily structural N demand from root is the product of total DM demand and the minimum N concentration.  Any N above this is considered Storage 
    /// and can be used for retranslocation and/or reallocation as the respective factors are set to values other then zero.  
    /// 
    /// **Nitrogen Uptake**
    /// 
    /// Potential N uptake by the root system is calculated for each soil layer (i) that the roots have extended into.  
    /// In each layer potential uptake is calculated as the product of the mineral nitrogen in the layer, a factor controlling the rate of extraction
    /// (kNO3 or kNH4), the concentration of N form (ppm), and a soil moisture factor (NUptakeSWFactor) which typically decreases as the soil dries.  
    /// 
    ///     _NO3 uptake = NO3<sub>i</sub> x kNO3 x NO3<sub>ppm, i</sub> x NUptakeSWFactor_
    ///     
    ///     _NH4 uptake = NH4<sub>i</sub> x kNH4 x NH4<sub>ppm, i</sub> x NUptakeSWFactor_
    /// 
    /// Nitrogen uptake demand is limited to the maximum daily potential uptake (MaxDailyNUptake) and the plants N demand. 
    /// The demand for soil N is then passed to the soil arbitrator which determines how much of the N uptake demand
    /// each plant instance will be allowed to take up.
    /// 
    /// **Water Uptake**
    /// 
    /// Potential water uptake by the root system is calculated for each soil layer that the roots have extended into.  
    /// In each layer potential uptake is calculated as the product of the available water in the layer (water above LL limit) 
    /// and a factor controlling the rate of extraction (KL).  The values of both LL and KL are set in the soil interface and
    /// KL may be further modified by the crop via the KLModifier function.  
    /// 
    /// _SW uptake = (SW<sub>i</sub> - LL<sub>i</sub>) x KL<sub>i</sub> x KLModifier_
    /// 
    ///</summary>
    [Serializable]
    [Description("Root Class")]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Plant))]
    public class Root : Model, IWaterNitrogenUptake, IArbitration, IOrgan, IOrganDamage
    {
        /// <summary>Tolerance for biomass comparisons</summary>
        private double BiomassToleranceValue = 0.0000000001;

        /// <summary>The plant</summary>
        [Link]
        protected Plant parentPlant = null;

        /// <summary>The surface organic matter model</summary>
        [Link]
        public ISurfaceOrganicMatter SurfaceOrganicMatter = null;

        /// <summary>Link to biomass removal model</summary>
        [Link(Type = LinkType.Child)]
        private BiomassRemoval biomassRemovalModel = null;

        /// <summary>The DM demand function</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/m2/d")]
        private BiomassDemand dmDemands = null;

        /// <summary>Factors for assigning priority to DM demands</summary>
        [Link(IsOptional = true, Type = LinkType.Child, ByName = true)]
        [Units("g/m2/d")]
        private BiomassDemand dmDemandPriorityFactors = null;

        /// <summary>Link to the KNO3 link</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction kno3 = null;

        /// <summary>Link to the KNH4 link</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction knh4 = null;

        /// <summary>Soil water factor for N Uptake</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction nUptakeSWFactor = null;

        /// <summary>Initial wt</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public BiomassDemand InitialWt = null;

        /// <summary>Gets or sets the specific root length</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("m/g")]
        private IFunction specificRootLength = null;

        /// <summary>The N demand function</summary>
        [Link(Type = LinkType.Child, ByName = true, IsOptional = true)]
        [Units("g/m2/d")]
        private BiomassDemand nDemands = null;

        /// <summary>The nitrogen root calc switch</summary>
        [Link(Type = LinkType.Child, ByName = true, IsOptional = true)]
        public IFunction RootFrontCalcSwitch = null;

        /// <summary>The N retranslocation factor</summary>
        [Link(Type = LinkType.Child, ByName = true, IsOptional = true)]
        [Units("/d")]
        private IFunction nRetranslocationFactor = null;

        /// <summary>The N reallocation factor</summary>
        [Link(Type = LinkType.Child, ByName = true, IsOptional = true)]
        [Units("/d")]
        private IFunction nReallocationFactor = null;

        /// <summary>The DM retranslocation factor</summary>
        [Link(Type = LinkType.Child, ByName = true, IsOptional = true)]
        [Units("/d")]
        private IFunction dmRetranslocationFactor = null;

        /// <summary>The DM reallocation factor</summary>
        [Link(Type = LinkType.Child, ByName = true, IsOptional = true)]
        [Units("/d")]
        private IFunction dmReallocationFactor = null;

        /// <summary>The biomass senescence rate</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("/d")]
        private IFunction senescenceRate = null;

        /// <summary>The root front velocity</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("mm/d")]
        private IFunction rootFrontVelocity = null;

        /// <summary>The maximum N concentration</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/g")]
        private IFunction maximumNConc = null;

        /// <summary>The minimum N concentration</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("g/g")]
        private IFunction minimumNConc = null;

        /// <summary>The critical N concentration</summary>
        [Link(Type = LinkType.Child, ByName = true, IsOptional = true)]
        [Units("g/g")]
        private IFunction criticalNConc = null;
 
        /// <summary>The maximum daily N uptake</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("kg N/ha")]
        private IFunction maxDailyNUptake = null;

        /// <summary>The kl modifier</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("0-1")]
        private IFunction klModifier = null;

        /// <summary>The Maximum Root Depth</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("mm")]
        private IFunction maximumRootDepth = null;
        
        /// <summary>Dry matter efficiency function</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction dmConversionEfficiency = null;
        
        /// <summary>Carbon concentration</summary>
        [Units("-")]
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction carbonConcentration = null;

        /// <summary>The cost for remobilisation</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction remobilisationCost = null;

        /// <summary>The proportion of biomass respired each day</summary> 
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("/d")]
        private IFunction maintenanceRespirationFunction = null;

        /// <summary>Do we need to recalculate (expensive operation) live and dead</summary>
        private bool needToRecalculateLiveDead = true;

        /// <summary>Live biomass</summary>
        private Biomass liveBiomass = new Biomass();

        /// <summary>Dead biomass</summary>
        private Biomass deadBiomass = new Biomass();

        /// <summary>The dry matter supply</summary>
        public BiomassSupplyType DMSupply {get; set;}

        /// <summary>The nitrogen supply</summary>
        public BiomassSupplyType NSupply { get; set; }

        /// <summary>The dry matter demand</summary>
        public BiomassPoolType DMDemand { get; set; }

        /// <summary>The dry matter demand</summary>
        public BiomassPoolType DMDemandPriorityFactor { get; set; }

        /// <summary>Structural nitrogen demand</summary>
        public BiomassPoolType NDemand { get; set; }

        /// <summary>The dry matter potentially being allocated</summary>
        public BiomassPoolType potentialDMAllocation { get; set; }

        /// <summary>The DM supply for retranslocation</summary>
        private double dmRetranslocationSupply = 0.0;

        /// <summary>The DM supply for reallocation</summary>
        private double dmMReallocationSupply = 0.0;

        /// <summary>The N supply for retranslocation</summary>
        private double nRetranslocationSupply = 0.0;

        /// <summary>The N supply for reallocation</summary>
        private double nReallocationSupply = 0.0;

        /// <summary>Constructor</summary>
        public Root()
        {
            Zones = new List<ZoneState>();
            ZoneNamesToGrowRootsIn = new List<string>();
            ZoneRootDepths = new List<double>();
            ZoneInitialDM = new List<BiomassDemand>();
        }

        /// <summary>Gets a value indicating whether the biomass is above ground or not</summary>
        public bool IsAboveGround { get { return false; } }

        /// <summary>A list of other zone names to grow roots in</summary>
        [XmlIgnore]
        public List<string> ZoneNamesToGrowRootsIn { get; set; }

        /// <summary>The root depths for each addition zone.</summary>
        [XmlIgnore]
        public List<double> ZoneRootDepths { get; set; }

        /// <summary>The live weights for each addition zone.</summary>
        [XmlIgnore]
        public List<BiomassDemand> ZoneInitialDM { get; set; }

        /// <summary>A list of all zones to grow roots in</summary>
        [XmlIgnore]
        public List<ZoneState> Zones { get; set; }

        /// <summary>The zone where the plant is growing</summary>
        [XmlIgnore]
        public ZoneState PlantZone { get; set; }

        /// <summary>Gets the live biomass.</summary>
        [XmlIgnore]
        [Units("g/m^2")]
        public Biomass Live
        {
            get
            {
                RecalculateLiveDead();
                return liveBiomass;
            }
        }

        /// <summary>Gets the dead biomass.</summary>
        [XmlIgnore]
        [Units("g/m^2")]
        public Biomass Dead
        {
            get
            {
                RecalculateLiveDead();
                return deadBiomass;
            }
        }

        /// <summary>Gets the root length density.</summary>
        [Units("mm/mm3")]
        public double[] LengthDensity
        {
            get
            {
                if (PlantZone == null)    // Can be null in autodoc
                    return new double[0]; 
                double[] value;
                value = new double[PlantZone.soil.Thickness.Length];
                double SRL = specificRootLength.Value();
                for (int i = 0; i < PlantZone.soil.Thickness.Length; i++)
                    value[i] = PlantZone.LayerLive[i].Wt * RootLengthDensityModifierDueToDamage * SRL * 1000 / 1000000 / PlantZone.soil.Thickness[i];
                return value;
            }
        }

        ///<Summary>Total DM demanded by roots</Summary>
        [Units("g/m2")]
        [XmlIgnore]
        public double TotalDMDemand { get; set; }

        ///<Summary>The amount of N taken up after arbitration</Summary>
        [Units("g/m2")]
        [XmlIgnore]
        public double NTakenUp { get; set; }

        /// <summary>Root depth.</summary>
        [XmlIgnore]
        public double Depth { get { return PlantZone.Depth; } }

        /// <summary>Layer live</summary>
        [XmlIgnore]
        public Biomass[] LayerLive { get { return PlantZone.LayerLive; } }

        /// <summary>Layer dead.</summary>
        [XmlIgnore]
        public Biomass[] LayerDead { get { return PlantZone.LayerDead; } }

        /// <summary>Gets or sets the water uptake.</summary>
        [Units("mm")]
        public double WaterUptake
        {
            get
            {
                double uptake = 0;
                foreach (ZoneState zone in Zones)
                    uptake = uptake + MathUtilities.Sum(zone.WaterUptake);
                return -uptake;
            }
        }

        /// <summary>Gets or sets the water uptake.</summary>
        [Units("kg/ha")]
        public double NUptake
        {
            get
            {
                double uptake = 0;
                foreach (ZoneState zone in Zones)
                    uptake = MathUtilities.Sum(zone.NitUptake);
                return uptake;
            }
        }

        /// <summary>Gets or sets the mid points of each layer</summary>
        [XmlIgnore]
        public double[] LayerMidPointDepth { get; private set; }

        /// <summary>Gets or sets root water content</summary>
        [XmlIgnore]
        public double[] RWC { get; private set; }

        /// <summary>Gets a factor to account for root zone Water tension weighted for root mass.</summary>
        [Units("0-1")]
        public double WaterTensionFactor
        {
            get
            {
                if (PlantZone == null)
                    return 0;

                double MeanWTF = 0;

                double liveWt = Live.Wt;
                if (liveWt > 0)
                    foreach (ZoneState Z in Zones)
                    {
                        double[] paw = Z.soil.PAW;
                        double[] pawc = Z.soil.PAWC;
                        Biomass[] layerLiveForZone = Z.LayerLive;
                        for (int i = 0; i < Z.LayerLive.Length; i++)
                            if (pawc[i] > 0)
                            {
                                MeanWTF += layerLiveForZone[i].Wt / liveWt * MathUtilities.Bound(2 * paw[i] / pawc[i], 0, 1);
                            }
                    }

                return MeanWTF;
            }
        }

        /// <summary>Gets a factor to account for root zone Water tension weighted for root mass.</summary>
        [Units("0-1")]
        public double PlantWaterPotentialFactor
        {
            get
            {
                if (PlantZone == null)
                    return 0;

                double MeanWTF = 0;

                double liveWt = Live.Wt;
                if (liveWt > 0)
                    foreach (ZoneState Z in Zones)
                    {
                        double[] paw = Z.soil.PAW;
                        double[] pawc = Z.soil.PAWC;
                        Biomass[] layerLiveForZone = Z.LayerLive;
                        for (int i = 0; i < Z.LayerLive.Length; i++)
                            MeanWTF += layerLiveForZone[i].Wt / liveWt * MathUtilities.Bound(paw[i] / pawc[i], 0, 1);
                    }

                return MeanWTF;
            }
        }

        /// <summary>Gets or sets the minimum nconc.</summary>
        public double MinNconc { get { return minimumNConc.Value(); } }

        /// <summary>Gets the critical nconc.</summary>
        public double CritNconc { get { return criticalNConc.Value(); } }

        /// <summary>Gets the total biomass</summary>
        public Biomass Total { get { return Live + Dead; } }

        /// <summary>Gets the total grain weight</summary>
        [Units("g/m2")]
        public double Wt { get { return Total.Wt; } }

        /// <summary>Gets the total grain N</summary>
        [Units("g/m2")]
        public double N { get { return Total.N; } }

        /// <summary>Gets the total (live + dead) N concentration (g/g)</summary>
        [XmlIgnore]
        public double Nconc { get{ return MathUtilities.Divide(N,Wt,0.0);}}
        
        /// <summary>Gets or sets the n fixation cost.</summary>
        [XmlIgnore]
        public double NFixationCost { get { return 0; } }

        /// <summary>Growth Respiration</summary>
        /// [Units("CO_2")]
        public double GrowthRespiration { get; set; }

        /// <summary>The amount of mass lost each day from maintenance respiration</summary>
        public double MaintenanceRespiration { get; set; }

        /// <summary>Gets the biomass allocated (represented actual growth)</summary>
        [XmlIgnore]
        public Biomass Allocated { get; set; }

        /// <summary>Gets the biomass senesced (transferred from live to dead material)</summary>
        [XmlIgnore]
        public Biomass Senesced { get; set; }

        /// <summary>Gets the DM amount detached (sent to soil/surface organic matter) (g/m2)</summary>
        [XmlIgnore]
        public Biomass Detached { get; set; }

        /// <summary>Gets the DM amount removed from the system (harvested, grazed, etc) (g/m2)</summary>
        [XmlIgnore]
        public Biomass Removed { get; set; }

        /// <summary>Gets the potential DM allocation for this computation round.</summary>
        public BiomassPoolType DMPotentialAllocation { get { return potentialDMAllocation; } }

        /// <summary>Gets or sets the root length modifier due to root damage (0-1).</summary>
        [XmlIgnore]
        public double RootLengthDensityModifierDueToDamage { get; set; } = 1.0;

        /// <summary>Returns true if the KL modifier due to root damage is active or not.</summary>
        private bool IsKLModiferDueToDamageActive { get; set; } = false;

        /// <summary>Gets the KL modifier due to root damage (0-1).</summary>
        private double KLModiferDueToDamage(int layerIndex)
        {
            var threshold = 0.01;
            if (!IsKLModiferDueToDamageActive)
                return 1;
            else if (LengthDensity[layerIndex] < 0)
                return 0;
            else if (LengthDensity[layerIndex] >= threshold)
                return 1;
            else
                return (1 / threshold) * LengthDensity[layerIndex];
        }

        /// <summary>Does the water uptake.</summary>
        /// <param name="Amount">The amount.</param>
        /// <param name="zoneName">Zone name to do water uptake in</param>
        public void DoWaterUptake(double[] Amount, string zoneName)
        {
            ZoneState zone = Zones.Find(z => z.Name == zoneName);
            if (zone == null)
                throw new Exception("Cannot find a zone called " + zoneName);

            zone.WaterUptake = MathUtilities.Multiply_Value(Amount, -1.0);
            zone.soil.SoilWater.RemoveWater(Amount);
        }

        /// <summary>Does the Nitrogen uptake.</summary>
        /// <param name="zonesFromSoilArbitrator">List of zones from soil arbitrator</param>
        public void DoNitrogenUptake(List<ZoneWaterAndN> zonesFromSoilArbitrator)
        {
            foreach (ZoneWaterAndN thisZone in zonesFromSoilArbitrator)
            {
                ZoneState zone = Zones.Find(z => z.Name == thisZone.Zone.Name);
                if (zone != null)
                {
                    zone.NO3.SetKgHa(SoluteSetterType.Plant, MathUtilities.Subtract(zone.NO3.kgha, thisZone.NO3N));
                    zone.NH4.SetKgHa(SoluteSetterType.Plant, MathUtilities.Subtract(zone.NH4.kgha, thisZone.NH4N));

                    zone.NitUptake = MathUtilities.Multiply_Value(MathUtilities.Add(thisZone.NO3N, thisZone.NH4N), -1);
                }
            }
        }

        /// <summary>Calculate and return the dry matter supply (g/m2)</summary>
        [EventSubscribe("SetDMSupply")]
        private void SetDMSupply(object sender, EventArgs e)
        {
            DMSupply.Fixation = 0.0;
            DMSupply.Retranslocation = dmRetranslocationSupply;
            DMSupply.Reallocation = dmMReallocationSupply;
        }

        /// <summary>Calculate and return the nitrogen supply (g/m2)</summary>
        [EventSubscribe("SetNSupply")]
        private void SetNSupply(object sender, EventArgs e)
        {
            NSupply.Fixation = 0.0;
            NSupply.Uptake = 0.0;
            NSupply.Retranslocation = nRetranslocationSupply;
            NSupply.Reallocation = nReallocationSupply;
        }

        /// <summary>Calculate and return the dry matter demand (g/m2)</summary>
        [EventSubscribe("SetDMDemand")]
        private void SetDMDemand(object sender, EventArgs e)
        {
            if (parentPlant.SowingData?.Depth <= PlantZone.Depth)
            {
                if (dmConversionEfficiency.Value() > 0.0)
                {
                    DMDemand.Structural = (dmDemands.Structural.Value() / dmConversionEfficiency.Value() + remobilisationCost.Value());
                    DMDemand.Storage = Math.Max(0, dmDemands.Storage.Value() / dmConversionEfficiency.Value()) ;
                    DMDemand.Metabolic = 0;
                }
                else
                { // Conversion efficiency is zero!!!!
                    DMDemand.Structural = 0;
                    DMDemand.Storage = 0;
                    DMDemand.Metabolic = 0;
                }

                if (dmDemandPriorityFactors != null)
                {
                    DMDemandPriorityFactor.Structural = dmDemandPriorityFactors.Structural.Value();
                    DMDemandPriorityFactor.Metabolic = dmDemandPriorityFactors.Metabolic.Value();
                    DMDemandPriorityFactor.Storage = dmDemandPriorityFactors.Storage.Value();
                }
                else
                {
                    DMDemandPriorityFactor.Structural = 1.0;
                    DMDemandPriorityFactor.Metabolic = 1.0;
                    DMDemandPriorityFactor.Storage = 1.0;
                }
            }
        }

        /// <summary>Calculate and return the nitrogen demand (g/m2)</summary>
        [EventSubscribe("SetNDemand")]
        private void SetNDemand(object sender, EventArgs e)
        {
            NDemand.Structural = nDemands.Structural.Value();
            if (NDemand.Structural < 0)
                throw new Exception("Structural N demand function in root returning negative, check parameterisation");
            NDemand.Metabolic = nDemands.Metabolic.Value();
            if (NDemand.Metabolic < 0)
                throw new Exception("Structural N demand function in root returning negative, check parameterisation");
            NDemand.Storage = nDemands.Storage.Value();
            if (NDemand.Storage < 0)
                throw new Exception("Structural N demand function in root returning negative, check parameterisation");

            foreach (ZoneState Z in Zones)
            {
                Z.StructuralNDemand = new double[Z.soil.Thickness.Length];
                Z.StorageNDemand = new double[Z.soil.Thickness.Length];
                Z.MetabolicNDemand = new double[Z.soil.Thickness.Length];
                //Note: MetabolicN is assumed to be zero

                double[] RAw = Z.CalculateRootActivityValues();
                for (int i = 0; i < Z.LayerLive.Length; i++)
                {
                    Z.StructuralNDemand[i] = NDemand.Structural;
                    Z.StorageNDemand[i] = NDemand.Storage;
                    Z.MetabolicNDemand[i] = NDemand.Metabolic;
                }
            }
        }

        /// <summary>Sets the dry matter potential allocation.</summary>
        public void SetDryMatterPotentialAllocation(BiomassPoolType dryMatter)
        {

            if (PlantZone.WaterUptake == null)
                throw new Exception("No water and N uptakes supplied to root. Is Soil Arbitrator included in the simulation?");

            if (PlantZone.Depth <= 0)
                return; //cannot allocate growth where no length

            if (DMDemand.Structural == 0 && dryMatter.Structural > 0.000000000001)
                throw new Exception("Invalid allocation of potential DM in" + Name);


            double TotalRAw = 0;
            foreach (ZoneState Z in Zones)
                TotalRAw += MathUtilities.Sum(Z.CalculateRootActivityValues());

            if (TotalRAw == 0 && dryMatter.Structural > 0)
                throw new Exception("Error trying to partition potential root biomass");

            if (TotalRAw > 0)
            {
                foreach (ZoneState Z in Zones)
                {
                    double[] RAw = Z.CalculateRootActivityValues();
                    Z.PotentialDMAllocated = new double[Z.soil.Thickness.Length];

                    for (int layer = 0; layer < Z.soil.Thickness.Length; layer++)
                        Z.PotentialDMAllocated[layer] = dryMatter.Structural * RAw[layer] / TotalRAw;
                }
                needToRecalculateLiveDead = true;
            }

            potentialDMAllocation.Structural = dryMatter.Structural;
            potentialDMAllocation.Metabolic = dryMatter.Metabolic;
            potentialDMAllocation.Storage = dryMatter.Storage;
        }

        /// <summary>Sets the dry matter allocation.</summary>
        public void SetDryMatterAllocation(BiomassAllocationType dryMatter)
        {
            double TotalRAw = 0;
            foreach (ZoneState Z in Zones)
                TotalRAw += MathUtilities.Sum(Z.CalculateRootActivityValues());

            Allocated.StructuralWt = dryMatter.Structural * dmConversionEfficiency.Value();
            Allocated.StorageWt = dryMatter.Storage * dmConversionEfficiency.Value();
            Allocated.MetabolicWt = dryMatter.Metabolic * dmConversionEfficiency.Value();
            // GrowthRespiration with unit CO2 
            // GrowthRespiration is calculated as 
            // Allocated CH2O from photosynthesis "1 / DMConversionEfficiency.Value()", converted 
            // into carbon through (12 / 30), then minus the carbon in the biomass, finally converted into 
            // CO2 (44/12).
            double growthRespFactor = ((1.0 / dmConversionEfficiency.Value()) * (12.0 / 30.0) - 1.0 * carbonConcentration.Value()) * 44.0 / 12.0;
            GrowthRespiration = (Allocated.StructuralWt + Allocated.StorageWt + Allocated.MetabolicWt) * growthRespFactor;
            if (TotalRAw == 0 && Allocated.Wt > 0)
                throw new Exception("Error trying to partition root biomass");

            foreach (ZoneState Z in Zones)
                Z.PartitionRootMass(TotalRAw, Allocated.Wt);
            needToRecalculateLiveDead = true;
        }

        /// <summary>Gets the nitrogen supply from the specified zone.</summary>
        /// <param name="zone">The zone.</param>
        /// <param name="NO3Supply">The returned NO3 supply</param>
        /// <param name="NH4Supply">The returned NH4 supply</param>
        public void CalculateNitrogenSupply(ZoneWaterAndN zone, ref double[] NO3Supply, ref double[] NH4Supply)
        {
            ZoneState myZone = Zones.Find(z => z.Name == zone.Zone.Name);
            if (myZone != null)
            {
                if (RWC == null || RWC.Length != myZone.soil.Thickness.Length)
                    RWC = new double[myZone.soil.Thickness.Length];

                double NO3Uptake = 0;
                double NH4Uptake = 0;

                double[] thickness = myZone.soil.Thickness;
                double[] water = myZone.soil.Water;
                double[] ll15mm = myZone.soil.LL15mm;
                double[] dulmm = myZone.soil.DULmm;
                double[] bd = myZone.soil.BD;

                double accuDepth = 0;
                if (RootFrontCalcSwitch?.Value() >= 1.0)
                {
                    if (myZone.MassFlow == null || myZone.MassFlow.Length != myZone.soil.Thickness.Length)
                        myZone.MassFlow = new double[myZone.soil.Thickness.Length];
                    if (myZone.Diffusion == null || myZone.Diffusion.Length != myZone.soil.Thickness.Length)
                        myZone.Diffusion = new double[myZone.soil.Thickness.Length];

                    var currentLayer = myZone.soil.LayerIndexOfDepth(myZone.Depth);
                    for (int layer = 0; layer <= currentLayer; layer++)
                    {
                        var swdep = water[layer]; //mm
                        var flow = myZone.WaterUptake[layer];
                        var yest_swdep = swdep - flow;
                        //NO3N is in kg/ha - old sorghum used g/m^2
                        var no3conc = zone.NO3N[layer] * kgha2gsm / yest_swdep; //to equal old sorghum
                        var no3massFlow = no3conc * (-flow);
                        myZone.MassFlow[layer] = no3massFlow;

                        //diffusion
                        var swAvailFrac = RWC[layer] = (water[layer] - ll15mm[layer]) / (dulmm[layer] - ll15mm[layer]);
                        //old sorghum stores N03 in g/ms not kg/ha
                        var no3Diffusion = MathUtilities.Bound(swAvailFrac, 0.0, 1.0) * (zone.NO3N[layer] * kgha2gsm); 

                        if (layer == currentLayer)
                        {
                            var proportion = myZone.soil.ProportionThroughLayer(currentLayer, myZone.Depth);
                            no3Diffusion *= proportion;
                        }

                        myZone.Diffusion[layer] = no3Diffusion;

                        //NH4Supply[layer] = no3massFlow;
                        //onyl 2 fields passed in for returning data. 
                        //actual uptake needs to distinguish between massflow and diffusion
                        //sorghum calcs don't use nh4 - so using that temporarily
                    }
                }
                else
                {
                    for (int layer = 0; layer < thickness.Length; layer++)
                    {
                        accuDepth += thickness[layer];
                        if (myZone.LayerLive[layer].Wt > 0)
                        {
                            double factorRootDepth = Math.Max(0, Math.Min(1, 1 - (accuDepth - Depth) / thickness[layer]));
                            RWC[layer] = (water[layer] - ll15mm[layer]) / (dulmm[layer] - ll15mm[layer]);
                            RWC[layer] = Math.Max(0.0, Math.Min(RWC[layer], 1.0));
                            double SWAF = nUptakeSWFactor.Value(layer);

                            double kno3 = this.kno3.Value(layer);
                            double NO3ppm = zone.NO3N[layer] * (100.0 / (bd[layer] * thickness[layer]));
                            NO3Supply[layer] = Math.Min(zone.NO3N[layer] * kno3 * NO3ppm * SWAF * factorRootDepth, (maxDailyNUptake.Value() - NO3Uptake));
                            NO3Uptake += NO3Supply[layer];

                            double knh4 = this.knh4.Value(layer);
                            double NH4ppm = zone.NH4N[layer] * (100.0 / (bd[layer] * thickness[layer]));
                            NH4Supply[layer] = Math.Min(zone.NH4N[layer] * knh4 * NH4ppm * SWAF * factorRootDepth, (maxDailyNUptake.Value() - NH4Uptake));
                            NH4Uptake += NH4Supply[layer];
                        }
                    }
                }
            }
        }

        /// <summary>Sets the n allocation.</summary>
        public void SetNitrogenAllocation(BiomassAllocationType nitrogen)
        {
            double totalStructuralNDemand = 0;
            double totalStorageNDemand = 0;
            double totalMetabolicDemand = 0;

            foreach (ZoneState Z in Zones)
            {
                totalStructuralNDemand += MathUtilities.Sum(Z.StructuralNDemand);
                totalStorageNDemand += MathUtilities.Sum(Z.StorageNDemand);
                totalMetabolicDemand += MathUtilities.Sum(Z.MetabolicNDemand);
            }
            NTakenUp = nitrogen.Uptake;
            Allocated.StructuralN = nitrogen.Structural;
            Allocated.StorageN = nitrogen.Storage;
            Allocated.MetabolicN = nitrogen.Metabolic;

            double surplus = Allocated.N - totalStructuralNDemand - totalStorageNDemand;
            if (surplus > 0.000000001)
                throw new Exception("N Allocation to roots exceeds Demand");
            double NAllocated = 0;

            foreach (ZoneState Z in Zones)
            {
                for (int i = 0; i < Z.LayerLive.Length; i++)
                {
                    if (totalStructuralNDemand > 0)
                    {
                        double StructFrac = Z.StructuralNDemand[i] / totalStructuralNDemand;
                        Z.LayerLive[i].StructuralN += nitrogen.Structural * StructFrac;
                        NAllocated += nitrogen.Structural * StructFrac;
                    }

                    if (totalStorageNDemand > 0)
                    {
                        double NonStructFrac = Z.StorageNDemand[i] / totalStorageNDemand;
                        Z.LayerLive[i].StorageN += nitrogen.Storage * NonStructFrac;
                        NAllocated += nitrogen.Storage * NonStructFrac;
                    }

                    if (totalMetabolicDemand > 0)
                    {
                        double MetabolFrac = Z.MetabolicNDemand[i] / totalMetabolicDemand;
                        Z.LayerLive[i].MetabolicN += nitrogen.Metabolic * MetabolFrac;
                        NAllocated += nitrogen.Metabolic * MetabolFrac;
                    }
                }
            }
            needToRecalculateLiveDead = true;

            if (!MathUtilities.FloatsAreEqual(NAllocated - Allocated.N, 0.0))
                throw new Exception("Error in N Allocation: " + Name);
        }

        /// <summary>Remove maintenance respiration from live component of organs.</summary>
        /// <param name="respiration">The respiration to remove</param>
        public virtual void RemoveMaintenanceRespiration(double respiration)
        {
            double total = Live.MetabolicWt + Live.StorageWt;
            if (respiration > total)
            {
                throw new Exception("Respiration is more than total biomass of metabolic and storage in live component.");
            }
            Live.MetabolicWt = Live.MetabolicWt - (respiration * Live.MetabolicWt / total);
            Live.StorageWt = Live.StorageWt - (respiration * Live.StorageWt / total);
        }

        /// <summary>Gets or sets the water supply.</summary>
        /// <param name="zone">The zone.</param>
        public double[] CalculateWaterSupply(ZoneWaterAndN zone)
        {
            ZoneState myZone = Zones.Find(z => z.Name == zone.Zone.Name);
            if (myZone == null)
                return null;

            if (myZone.soil.Weirdo != null)
                return new double[myZone.soil.Thickness.Length]; //With Weirdo, water extraction is not done through the arbitrator because the time step is different.
            else
            {
                var currentLayer = PlantZone.soil.LayerIndexOfDepth(Depth);
                var soilCrop = myZone.soil.Crop(parentPlant.Name);
                if (RootFrontCalcSwitch?.Value() >= 1.0)
                {
                    double[] kl = soilCrop.KL;
                    double[] ll = soilCrop.LL;

                    double[] lldep = new double[myZone.soil.Thickness.Length];
                    double[] available = new double[myZone.soil.Thickness.Length];

                    double[] supply = new double[myZone.soil.Thickness.Length];
                    LayerMidPointDepth = myZone.soil.DepthMidPoints;
                    for (int layer = 0; layer <= currentLayer; layer++)
                    {
                        lldep[layer] = ll[layer] * myZone.soil.Thickness[layer];
                        available[layer] = Math.Max(zone.Water[layer] - lldep[layer], 0.0);
                        if (currentLayer == layer)
                        {
                            var layerproportion = myZone.soil.ProportionThroughLayer(layer, myZone.Depth);
                            available[layer] *= layerproportion;
                        }

                        var proportionThroughLayer = rootProportionInLayer(layer, myZone);
                        var klMod = klModifier.Value(layer);
                        supply[layer] = Math.Max(0.0, kl[layer] * klMod * KLModiferDueToDamage(layer) * available[layer] * proportionThroughLayer);
                    }

                    return supply;
                }
                else
                {
                    double[] kl = soilCrop.KL;
                    double[] ll = soilCrop.LL;

                    double[] supply = new double[myZone.soil.Thickness.Length];
                    LayerMidPointDepth = myZone.soil.DepthMidPoints;
                    for (int layer = 0; layer < myZone.soil.Thickness.Length; layer++)
                    {
                        if (layer <= myZone.soil.LayerIndexOfDepth(myZone.Depth))
                        {
                            supply[layer] = Math.Max(0.0, kl[layer] * klModifier.Value(layer) * KLModiferDueToDamage(layer) *
                            (zone.Water[layer] - ll[layer] * myZone.soil.Thickness[layer]) * rootProportionInLayer(layer, myZone));
                        }
                    }
                    return supply;
                }
            }            
        }

        /// <summary>Calculate the proportion of root in a layer within a zone.</summary>
        /// <param name="layer">The zone.</param>
        /// <param name="zone">The zone.</param>
        public double rootProportionInLayer(int layer, ZoneState zone)
        {
            if (RootFrontCalcSwitch?.Value() >= 1.0)
            {
                /* Row Spacing and configuration (skip) are used to calculate semicircular root front to give
                    proportion of the layer occupied by the roots. */
                double top;

                top = layer == 0 ? 0 : MathUtilities.Sum(zone.soil.Thickness, 0, layer - 1);

                if (top > zone.Depth)
                    return 0;

                double bottom = top + zone.soil.Thickness[layer];

                double rootArea;
                IFunction calcType = Apsim.Child(this, "RootAreaCalcType") as IFunction;
                if (calcType != null && MathUtilities.FloatsAreEqual(calcType.Value(), 1))
                {
                    rootArea = GetRootArea(top, bottom, zone.RootFront, zone.RightDist);
                    rootArea += GetRootArea(top, bottom, zone.RootFront, zone.LeftDist);
                }
                else
                {
                    rootArea = calcRootArea(zone, top, bottom, zone.RightDist);    // Right side
                    rootArea += calcRootArea(zone, top, bottom, zone.LeftDist);          // Left Side
                }

                double soilArea = (zone.RightDist + zone.LeftDist) * (bottom - top);

                return Math.Max(0.0, MathUtilities.Divide(rootArea, soilArea, 0.0));
            }
                
            return zone.soil.ProportionThroughLayer(layer, zone.Depth);
        }

        //------------------------------------------------------------------------------------------------
        //sorghum specific variables
        /// <summary>Gets the RootFront</summary>
        public double RootAngle { get; set; } = 45;

        /// <summary>Link to the KNO3 link</summary>
        [Link(Type = LinkType.Child, ByName = true, IsOptional = true)]
        public IFunction RootDepthStressFactor = null;

        /// <summary>Maximum Nitrogen Uptake Rate</summary>
        [Link(Type = LinkType.Child, ByName = true, IsOptional = true)]
        public IFunction MaxNUptakeRate = null;

        /// <summary>Maximum Nitrogen Uptake Rate</summary>
        [Link(Type = LinkType.Child, ByName = true, IsOptional = true)]
        public IFunction NSupplyFraction = null;

        /// <summary>Used to calc maximim diffusion rate</summary>
        [Link(Type = LinkType.Child, ByName = true, IsOptional = true)]
        public IFunction DltThermalTime = null;

        /// <summary>Used to calc maximim diffusion rate</summary>
        [Link(Type = LinkType.Child, ByName = true, IsOptional = true)]
        public IFunction MaxDiffusion = null;

        /// <summary>The kgha2gsm</summary>
        protected const double kgha2gsm = 0.1;

        double DegToRad(double degs)
        {
            return degs * Math.PI / 180.0;
        }

        double RadToDeg(double rads)
        {
            return rads * 180.0 / Math.PI;
        }

        double calcRootArea(ZoneState zone, double top, double bottom, double hDist)
        {
            if (zone.RootFront == 0.0)
            {
                return 0.0;
            }

            double depth, depthInLayer;

            zone.RootSpread = zone.RootFront * Math.Tan(DegToRad(RootAngle));   //Semi minor axis

            if (zone.RootFront >= bottom)
            {
                depth = (bottom - top) / 2.0 + top;
                depthInLayer = bottom - top;
            }
            else
            {
                depth = (zone.RootFront - top) / 2.0 + top;
                depthInLayer = zone.RootFront - top;
            }
            double xDist = zone.RootSpread * Math.Sqrt(1 - (Math.Pow(depth, 2) / Math.Pow(zone.RootFront, 2)));

            return Math.Min(depthInLayer * xDist, depthInLayer * hDist);
        }

        double GetRootArea(double top, double bottom, double rootLength, double hDist)
        {
            // get the area occupied by roots in a semi-circular section between top and bottom
            double SDepth, rootArea;

            // intersection of roots and Section
            if (rootLength <= hDist)
                SDepth = 0.0;
            else
                SDepth = Math.Sqrt(Math.Pow(rootLength, 2) - Math.Pow(hDist, 2));

            // Rectangle - SDepth past bottom of this area
            if (SDepth >= bottom)
                rootArea = (bottom - top) * hDist;
            else               // roots Past top
            {
                double Theta = 2 * Math.Acos(MathUtilities.Divide(Math.Max(top, SDepth), rootLength, 0));
                double topArea = (Math.Pow(rootLength, 2) / 2.0 * (Theta - Math.Sin(Theta))) / 2.0;

                // bottom down
                double bottomArea = 0;
                if (rootLength > bottom)
                {
                    Theta = 2 * Math.Acos(bottom / rootLength);
                    bottomArea = (Math.Pow(rootLength, 2) / 2.0 * (Theta - Math.Sin(Theta))) / 2.0;
                }
                // rectangle
                if (SDepth > top)
                    topArea = topArea + (SDepth - top) * hDist;
                rootArea = topArea - bottomArea;
            }
            return rootArea;
        }

        /// <summary>Removes biomass from root layers when harvest, graze or cut events are called.</summary>
        /// <param name="biomassRemoveType">Name of event that triggered this biomass remove call.</param>
        /// <param name="amountToRemove">The fractions of biomass to remove</param>
        public void RemoveBiomass(string biomassRemoveType, OrganBiomassRemovalType amountToRemove)
        {
            biomassRemovalModel.RemoveBiomassToSoil(biomassRemoveType, amountToRemove, PlantZone.LayerLive, PlantZone.LayerDead, Removed, Detached);
            needToRecalculateLiveDead = true;

            // Commented out code below because about 10 validation files failed on Jenkins
            // e.g. Chicory, Oats
            //if (biomassRemoveType != null && biomassRemoveType != "Harvest")
            //    IsKLModiferDueToDamageActive = true;
        }

        /// <summary>Initialise all zones.</summary>
        private void InitialiseZones()
        {
            Zones.Clear();
            Zones.Add(PlantZone);
            if (ZoneRootDepths.Count != ZoneNamesToGrowRootsIn.Count ||
                ZoneRootDepths.Count != ZoneInitialDM.Count)
                throw new Exception("The root zone variables (ZoneRootDepths, ZoneNamesToGrowRootsIn, ZoneInitialDM) need to have the same number of values");

            for (int i = 0; i < ZoneNamesToGrowRootsIn.Count; i++)
            {
                Zone zone = Apsim.Find(this, ZoneNamesToGrowRootsIn[i]) as Zone;
                if (zone != null)
                {
                    Soil soil = Apsim.Find(zone, typeof(Soil)) as Soil;
                    if (soil == null)
                        throw new Exception("Cannot find soil in zone: " + zone.Name);
                    if (soil.Crop(parentPlant.Name) == null)
                        throw new Exception("Cannot find a soil crop parameterisation for " + parentPlant.Name);
                    ZoneState newZone = new ZoneState(parentPlant, this, soil, ZoneRootDepths[i], ZoneInitialDM[i], parentPlant.Population, maximumNConc.Value(),
                                                      rootFrontVelocity, maximumRootDepth, remobilisationCost);
                    Zones.Add(newZone);
                }
            }
            needToRecalculateLiveDead = true;
        }

        /// <summary>Clears this instance.</summary>
        private void Clear()
        {
            Live.Clear();
            Dead.Clear();
            PlantZone.Clear();
            Zones.Clear();
            needToRecalculateLiveDead = true;
        }

        /// <summary>Clears the transferring biomass amounts.</summary>
        private void ClearBiomassFlows()
        {
            Allocated.Clear();
            Senesced.Clear();
            Detached.Clear();
            Removed.Clear();
        }

        /// <summary>Recalculate live and dead biomass if necessary</summary>
        private void RecalculateLiveDead()
        {
            if (needToRecalculateLiveDead)
            {
                needToRecalculateLiveDead = false;
                liveBiomass.Clear();
                deadBiomass.Clear();
                if (PlantZone != null)
                {
                    foreach (Biomass b in PlantZone.LayerLive)
                        liveBiomass.Add(b);
                    foreach (Biomass b in PlantZone.LayerDead)
                        deadBiomass.Add(b);
                }
            }
        }

        /// <summary>Computes the DM and N amounts that are made available for new growth</summary>
        private void DoSupplyCalculations()
        {
            dmMReallocationSupply = AvailableDMReallocation();
            dmRetranslocationSupply = AvailableDMRetranslocation();
            nReallocationSupply = AvailableNReallocation();
            nRetranslocationSupply = AvailableNRetranslocation();
        }

        /// <summary>Computes root total water supply.</summary>
        public double TotalExtractableWater()
        {
            var soilCrop = PlantZone.soil.Crop(parentPlant.Name);

            double[] LL = soilCrop.LL;
            double[] KL = soilCrop.KL;
            double[] SWmm = PlantZone.soil.Water;
            double[] DZ = PlantZone.soil.Thickness;

            double supply = 0;
            for (int layer = 0; layer < LL.Length; layer++)
            {
                if (layer <= PlantZone.soil.LayerIndexOfDepth(Depth))
                    supply += Math.Max(0.0, KL[layer] * klModifier.Value(layer) * KLModiferDueToDamage(layer) * (SWmm[layer] - LL[layer] * DZ[layer]) *
                        rootProportionInLayer(layer, PlantZone));
            }
            return supply;
        }

        /// <summary>Plant Avaliable water supply used by sorghum.</summary>
        /// <summary>It adds an extra layer proportion calc to extractableWater calc.</summary>
        public double PlantAvailableWaterSupply()
        {
            var soilCrop = PlantZone.soil.Crop(parentPlant.Name);

            double[] LL = soilCrop.LL;
            double[] KL = soilCrop.KL;
            double[] SWmm = PlantZone.soil.Water;
            double[] DZ = PlantZone.soil.Thickness;
            double[] available = new double[PlantZone.soil.Thickness.Length];
            double[] supply = new double[PlantZone.soil.Thickness.Length];

            var currentLayer = PlantZone.soil.LayerIndexOfDepth(Depth);
            var layertop = MathUtilities.Sum(PlantZone.soil.Thickness, 0, Math.Max(0, currentLayer - 1));
            var layerBottom = MathUtilities.Sum(PlantZone.soil.Thickness, 0, currentLayer);
            var layerProportion = Math.Min(MathUtilities.Divide(Depth - layertop, layerBottom - layertop, 0.0), 1.0);

            for (int layer = 0; layer < LL.Length; layer++)
            {
                if (layer <= currentLayer)
                {
                    available[layer] = Math.Max(0.0, SWmm[layer] - LL[layer] * DZ[layer]);
                }
            }
            available[currentLayer] *= layerProportion;

            double supplyTotal = 0;
            for (int layer = 0; layer < LL.Length; layer++)
            {
                if (layer <= currentLayer)
                {
                    var propoortion = rootProportionInLayer(layer, PlantZone);
                    var kl = KL[layer];
                    var klmod = klModifier.Value(layer);

                    supply[layer] = Math.Max(0.0, available[layer] * KL[layer] * klModifier.Value(layer) * KLModiferDueToDamage(layer) *
                        rootProportionInLayer(layer, PlantZone));

                    supplyTotal += supply[layer];
                }
            }
            return supplyTotal;
        }

        /// <summary>Computes the amount of DM available for reallocation.</summary>
        private double AvailableDMReallocation()
        {
            if (dmReallocationFactor != null)
            {
                double rootLiveStorageWt = 0.0;
                foreach (ZoneState Z in Zones)
                    for (int i = 0; i < Z.LayerLive.Length; i++)
                        rootLiveStorageWt += Z.LayerLive[i].StorageWt;

                double availableDM = rootLiveStorageWt * senescenceRate.Value() * dmReallocationFactor.Value();
                if (availableDM < -BiomassToleranceValue)
                    throw new Exception("Negative DM reallocation value computed for " + Name);
                return availableDM;
            }
            // By default reallocation is turned off!!!!
            return 0.0;
        }

        /// <summary>Computes the N amount available for retranslocation.</summary>
        /// <remarks>This is limited to ensure Nconc does not go below MinimumNConc</remarks>
        private double AvailableNRetranslocation()
        {
            if (nRetranslocationFactor != null)
            {
                double labileN = 0.0;
                double minNConc = minimumNConc.Value();
                foreach (ZoneState Z in Zones)
                    for (int i = 0; i < Z.LayerLive.Length; i++)
                        labileN += Math.Max(0.0, Z.LayerLive[i].StorageN - Z.LayerLive[i].StorageWt * minNConc);

                double availableN = Math.Max(0.0, labileN - nReallocationSupply) * nRetranslocationFactor.Value();
                if (availableN < -BiomassToleranceValue)
                    throw new Exception("Negative N retranslocation value computed for " + Name);

                return availableN;
            }
            else
            {  // By default retranslocation is turned off!!!!
                return 0.0;
            }
        }

        /// <summary>Computes the N amount available for reallocation.</summary>
        private double AvailableNReallocation()
        {
            if (nReallocationFactor != null)
            {
                double rootLiveStorageN = 0.0;
                foreach (ZoneState Z in Zones)
                    for (int i = 0; i < Z.LayerLive.Length; i++)
                        rootLiveStorageN += Z.LayerLive[i].StorageN;

                double availableN = rootLiveStorageN * senescenceRate.Value() * nReallocationFactor.Value();
                if (availableN < -BiomassToleranceValue)
                    throw new Exception("Negative N reallocation value computed for " + Name);

                return availableN;
            }
            else
            {  // By default reallocation is turned off!!!!
                return 0.0;
            }
        }

        /// <summary>Computes the amount of DM available for retranslocation.</summary>
        private double AvailableDMRetranslocation()
        {
            if (dmRetranslocationFactor != null)
            {
                double rootLiveStorageWt = 0.0;
                foreach (ZoneState Z in Zones)
                    for (int i = 0; i < Z.LayerLive.Length; i++)
                        rootLiveStorageWt += Z.LayerLive[i].StorageWt;

                double availableDM = Math.Max(0.0, rootLiveStorageWt - dmMReallocationSupply) * dmRetranslocationFactor.Value();
                if (availableDM < -BiomassToleranceValue)
                    throw new Exception("Negative DM retranslocation value computed for " + Name);

                return availableDM;
            }
            else
            { // By default retranslocation is turned off!!!!
                return 0.0;
            }
        }

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        /// <exception cref="ApsimXException">Cannot find a soil crop parameterisation for  + Name</exception>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            Soil soil = Apsim.Find(this, typeof(Soil)) as Soil;
            if (soil == null)
                throw new Exception("Cannot find soil");
            if (soil.Weirdo == null && soil.Crop(parentPlant.Name) == null)
                throw new Exception("Cannot find a soil crop parameterisation for " + parentPlant.Name);

            PlantZone = new ZoneState(parentPlant, this, soil, 0, InitialWt, parentPlant.Population, maximumNConc.Value(),
                                      rootFrontVelocity, maximumRootDepth, remobilisationCost);
            Zones = new List<ZoneState>();
            DMDemand = new BiomassPoolType();
            DMDemandPriorityFactor = new BiomassPoolType();
            NDemand = new BiomassPoolType();
            DMSupply = new BiomassSupplyType();
            NSupply = new BiomassSupplyType();
            potentialDMAllocation = new BiomassPoolType();
            Allocated = new Biomass();
            Senesced = new Biomass();
            Detached = new Biomass();
            Removed = new Biomass();
        }

        /// <summary>Called when [do daily initialisation].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            if (parentPlant.IsAlive || parentPlant.IsEnding)
                ClearBiomassFlows();
        }

        /// <summary>Called when crop is sown</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantSowing")]
        private void OnPlantSowing(object sender, SowPlant2Type data)
        {
            if (data.Plant == parentPlant)
            {
                //sorghum calcs
                PlantZone.Initialise(parentPlant.SowingData.Depth, InitialWt, parentPlant.Population, maximumNConc.Value());
                InitialiseZones();

                needToRecalculateLiveDead = true;
            }
        }
        

        /// <summary>Event from sequencer telling us to do our potential growth.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoPotentialPlantGrowth")]
        private void OnDoPotentialPlantGrowth(object sender, EventArgs e)
        {
            if (parentPlant.IsEmerged)
            {
                DoSupplyCalculations(); //TODO: This should be called from the Arbitrator, OnDoPotentialPlantPartioning
            }
        }

        /// <summary>Does the nutrient allocations.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoActualPlantGrowth")]
        private void OnDoActualPlantGrowth(object sender, EventArgs e)
        {
            if (parentPlant.IsAlive)
            {
                foreach (ZoneState Z in Zones)
                    Z.GrowRootDepth();

                // Do Root Senescence
                RemoveBiomass(null, new OrganBiomassRemovalType() { FractionLiveToResidue = senescenceRate.Value() });

                // Do maintenance respiration
                MaintenanceRespiration = 0;
                if (maintenanceRespirationFunction != null && (Live.MetabolicWt + Live.StorageWt) > 0)
                {
                    MaintenanceRespiration += Live.MetabolicWt * maintenanceRespirationFunction.Value();
                    MaintenanceRespiration += Live.StorageWt * maintenanceRespirationFunction.Value();
                }
                needToRecalculateLiveDead = true;
            }
        }

        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantEnding")]
        private void OnPlantEnding(object sender, EventArgs e)
        {
            if (Wt > 0.0)
            {
                Detached.Add(Live);
                Detached.Add(Dead);
                RemoveBiomass(null, new OrganBiomassRemovalType() { FractionLiveToResidue = 1.0 });
            }

            Clear();
        }
    }
}
