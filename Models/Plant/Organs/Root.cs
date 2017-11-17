namespace Models.PMF.Organs
{
    using APSIM.Shared.Utilities;
    using Library;
    using Models.Core;
    using Models.PMF.Functions;
    using Models.PMF.Interfaces;
    using Models.Soils;
    using Models.Soils.Arbitrator;
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;

    ///<summary>
    /// The generic root model calculates root growth in terms of rooting depth, biomass accumulation and subsequent root length density in each sol layer. 
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
    /// A daily DM demand is provided to the organ abitrator and a DM supply returned. By default, 100% of the dry matter (DM) demanded from the root is structural.  
    /// The daily loss of roots is calculated using a SenescenceRate function.  All senesced material is automatically detached and added to the soil FOM.  
    /// 
    /// **Nitrogen Demands**
    /// 
    /// The daily structural N demand from root is the product of total DM demand and the minimum N concentration.  Any N above this is considered Storage 
    /// and can be used for retranslocation and/or reallocation is the respective factors are set to values other then zero.  
    /// 
    /// **Nitrogen Uptake**
    /// 
    /// Potential N uptake by the root system is calculated for each soil layer that the roots have extended into.  
    /// In each layer potential uptake is calculated as the product of the mineral nitrogen in the layer, a factor controlling the rate of extraction
    /// (kNO3 or kNH4), the concentration of N form (ppm), and a soil moisture factor (NUptakeSWFactor) which typically decreases as the soil dries.  
    /// 
    ///     _NO3 uptake = NO3<sub>i</sub> x KNO3 x NO3<sub>ppm, i</sub> x NUptakeSWFactor_
    ///     
    ///     _NH4 uptake = NH4<sub>i</sub> x KNH4 x NH4<sub>ppm, i</sub> x NUptakeSWFactor_
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
    public class Root : BaseOrgan, IWaterNitrogenUptake, IArbitration
    {
        #region Links

        #endregion

        #region Parameters
        /// <summary>The DM demand function</summary>
        [Link]
        [Units("g/m2/d")]
        IFunction DMDemandFunction = null;

        /// <summary>Link to the KNO3 link</summary>
        [Link]
        IFunction KNO3 = null;

        /// <summary>Link to the KNH4 link</summary>
        [Link]
        IFunction KNH4 = null;

        /// <summary>Soil water factor for N Uptake</summary>
        [Link]
        IFunction NUptakeSWFactor = null;

        /// <summary>Gets or sets the initial biomass dry matter weight</summary>
        [Link]
        [Units("g/plant")]
        IFunction InitialDM = null;

        /// <summary>Gets or sets the specific root length</summary>
        [Link]
        [Units("m/g")]
        IFunction SpecificRootLength = null;

        /// <summary>The nitrogen demand switch</summary>
        [Link]
        IFunction NitrogenDemandSwitch = null;

        /// <summary>The N retranslocation factor</summary>
        [Link(IsOptional = true)]
        [Units("/d")]
        IFunction NRetranslocationFactor = null;

        /// <summary>The N reallocation factor</summary>
        [Link(IsOptional = true)]
        [Units("/d")]
        IFunction NReallocationFactor = null;

        /// <summary>The DM retranslocation factor</summary>
        [Link(IsOptional = true)]
        [Units("/d")]
        IFunction DMRetranslocationFactor = null;

        /// <summary>The DM reallocation factor</summary>
        [Link(IsOptional = true)]
        [Units("/d")]
        IFunction DMReallocationFactor = null;

        /// <summary>The biomass senescence rate</summary>
        [Link]
        [Units("/d")]
        IFunction SenescenceRate = null;

        /// <summary>The root front velocity</summary>
        [Link]
        [Units("mm/d")]
        public IFunction RootFrontVelocity = null;

        /// <summary>The DM structural fraction</summary>
        [Link(IsOptional = true)]
        [Units("g/g")]
        IFunction StructuralFraction = null;

        /// <summary>The maximum N concentration</summary>
        [Link]
        [Units("g/g")]
        IFunction MaximumNConc = null;

        /// <summary>The minimum N concentration</summary>
        [Link]
        [Units("g/g")]
        IFunction MinimumNConc = null;

        /// <summary>The critical N concentration</summary>
        [Link(IsOptional = true)]
        [Units("g/g")]
        IFunction CriticalNConc = null;

        /// <summary>The maximum daily N uptake</summary>
        [Link]
        [Units("kg N/ha")]
        IFunction MaxDailyNUptake = null;

        /// <summary>The kl modifier</summary>
        [Link]
        [Units("0-1")]
        IFunction KLModifier = null;

        /// <summary>The Maximum Root Depth</summary>
        [Link]
        [Units("mm")]
        public IFunction MaximumRootDepth = null;
        
        /// <summary>Dry matter efficiency function</summary>
        [Link]
        public IFunction DMConversionEfficiency = null;

        /// <summary>The cost for remobilisation</summary>
        [Link]
        public IFunction RemobilisationCost = null;

        /// <summary>Link to biomass removal model</summary>
        [ChildLink]
        public BiomassRemoval biomassRemovalModel = null;

        /// <summary>A list of other zone names to grow roots in</summary>
        public List<string> ZoneNamesToGrowRootsIn { get; set; }

        /// <summary>The root depths for each addition zone.</summary>
        public List<double> ZoneRootDepths { get; set; }

        /// <summary>The live weights for each addition zone.</summary>
        public List<double> ZoneInitialDM { get; set; }

        private double BiomassToleranceValue = 0.0000000001;   // 10E-10
        
        /// <summary>Do we need to recalculate (expensive operation) live and dead</summary>
        private bool needToRecalculateLiveDead = true;
        private Biomass liveBiomass = new Biomass();
        private Biomass deadBiomass = new Biomass();
        #endregion

        #region States

        /// <summary>A list of all zones to grow roots in</summary>
        [XmlIgnore]
        public List<ZoneState> Zones { get; set; }

        /// <summary>The zone where the plant is growing</summary>
        [XmlIgnore]
        public ZoneState PlantZone { get; set; }

        /// <summary>The DM supply for retranslocation</summary>
        private double DMRetranslocationSupply = 0.0;

        /// <summary>The DM supply for reallocation</summary>
        private double DMReallocationSupply = 0.0;

        /// <summary>The N supply for retranslocation</summary>
        private double NRetranslocationSupply = 0.0;

        /// <summary>The N supply for reallocation</summary>
        private double NReallocationSupply = 0.0;

        /// <summary>The structural DM demand</summary>
        private double StructuralDMDemand = 0.0;

        /// <summary>The non structural DM demand</summary>
        private double StorageDMDemand = 0.0;

        /// <summary>The metabolic DM demand</summary>
        private double MetabolicDMDemand = 0.0;

        /// <summary>The structural N demand</summary>
        private double StructuralNDemand = 0.0;

        /// <summary>The non structural N demand</summary>
        private double StorageNDemand = 0.0;

        /// <summary>The metabolic N demand</summary>
        private double MetabolicNDemand = 0.0;

        #endregion

        #region Outputs

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

        /// <summary>Gets the root length density.</summary>
        [Units("mm/mm3")]
        public double[] LengthDensity
        {
            get
            {
                double[] value;
                if (PlantZone == null)
                    value = new double[0];
                else
                {
                    value = new double[PlantZone.soil.Thickness.Length];
                    for (int i = 0; i < PlantZone.soil.Thickness.Length; i++)
                        value[i] = PlantZone.LayerLive[i].Wt * SpecificRootLength.Value() * 1000 / 1000000 / PlantZone.soil.Thickness[i];
                }
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
        public double Depth { get { return (PlantZone != null )? PlantZone.Depth:0; } }

        /// <summary>Layer live</summary>
        [XmlIgnore]
        public Biomass[] LayerLive { get { if (PlantZone != null) return PlantZone.LayerLive; else return new Biomass[0]; } }

        /// <summary>Layer dead.</summary>
        [XmlIgnore]
        public Biomass[] LayerDead { get { if (PlantZone != null) return PlantZone.LayerDead; else return new Biomass[0]; } }

        /// <summary>Gets or sets the length.</summary>
        [XmlIgnore]
        public double Length { get { return PlantZone.Length; } }

        /// <summary>Gets or sets the water uptake.</summary>
        [Units("mm")]
        public double WaterUptake
        {
            get
            {
                double uptake = 0;
                foreach (ZoneState zone in Zones)
                    uptake = uptake + MathUtilities.Sum(zone.Uptake);
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

        #endregion

        #region Functions

        /// <summary>Constructor</summary>
        public Root()
        {
            Zones = new List<ZoneState>();
            ZoneNamesToGrowRootsIn = new List<string>();
            ZoneRootDepths = new List<double>();
            ZoneInitialDM = new List<double>();
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
                    if (soil.Crop(Plant.Name) == null)
                        throw new Exception("Cannot find a soil crop parameterisation for " + Plant.Name);
                    ZoneState newZone = new ZoneState(Plant, this, soil, ZoneRootDepths[i], ZoneInitialDM[i], Plant.Population, MaximumNConc.Value());
                    Zones.Add(newZone);
                }
            }
            needToRecalculateLiveDead = true;
        }

        /// <summary>Does the water uptake.</summary>
        /// <param name="Amount">The amount.</param>
        /// <param name="zoneName">Zone name to do water uptake in</param>
        public void DoWaterUptake(double[] Amount, string zoneName)
        {
            ZoneState zone = Zones.Find(z => z.Name == zoneName);
            if (zone == null)
                throw new Exception("Cannot find a zone called " + zoneName);

            zone.Uptake = MathUtilities.Multiply_Value(Amount, -1.0);
            zone.soil.SoilWater.dlt_sw_dep = zone.Uptake;
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
                    zone.solutes.Subtract("NO3", thisZone.NO3N);
                    zone.solutes.Subtract("NH4", thisZone.NH4N);

                    zone.NitUptake = MathUtilities.Multiply_Value(MathUtilities.Add(thisZone.NO3N, thisZone.NH4N), -1);
                }
            }
        }

        /// <summary>Called when crop is ending</summary>
        [EventSubscribe("PlantEnding")]
        protected void DoPlantEnding(object sender, EventArgs e)
        {
            //Send all root biomass to soil FOM
            DoRemoveBiomass(null, new OrganBiomassRemovalType() { FractionLiveToResidue = 1.0 });
            Clear();
        }

        /// <summary>Clears this instance.</summary>
        protected void Clear()
        {
            Live.Clear();
            Dead.Clear();
            PlantZone.Clear();
            Zones.Clear();
            needToRecalculateLiveDead = true;
        }

        /// <summary>Gets a factor to account for root zone Water tension weighted for root mass.</summary>
        [Units("0-1")]
        public double WaterTensionFactor
        {
            get
            {
                double MeanWTF = 0;

                double liveWt = Live.Wt;
                if (liveWt > 0)
                    foreach (ZoneState Z in Zones)
                    {
                        double[] paw = Z.soil.PAW;
                        double[] pawc = Z.soil.PAWC;
                        Biomass[] layerLiveForZone = Z.LayerLive;
                        for (int i = 0; i < Z.LayerLive.Length; i++)
                            MeanWTF += layerLiveForZone[i].Wt / liveWt * MathUtilities.Bound(2 * paw[i] / pawc[i], 0, 1);
                    }

                return MeanWTF;
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        /// <exception cref="ApsimXException">Cannot find a soil crop parameterisation for  + Name</exception>
        [EventSubscribe("Commencing")]
        private new void OnSimulationCommencing(object sender, EventArgs e)
        {
            Soil soil = Apsim.Find(this, typeof(Soil)) as Soil;
            if (soil == null)
                throw new Exception("Cannot find soil");
            if (soil.Crop(Plant.Name) == null && soil.Weirdo == null)
                throw new Exception("Cannot find a soil crop parameterisation for " + Plant.Name);

            PlantZone = new ZoneState(Plant, this, soil, 0, InitialDM.Value(), Plant.Population, MaximumNConc.Value());
            Zones = new List<ZoneState>();
            base.OnSimulationCommencing(sender, e);
        }

        /// <summary>Called when crop is sown</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantSowing")]
        private void OnPlantSowing(object sender, SowPlant2Type data)
        {
            if (data.Plant == Plant)
            {
                PlantZone.Initialise(Plant.SowingData.Depth, InitialDM.Value(), Plant.Population, MaximumNConc.Value());
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
            if (Plant.IsEmerged)
            {
                PlantZone.Length = MathUtilities.Sum(LengthDensity);
                DoSupplyCalculations(); //TODO: This should be called from the Arbitrator, OnDoPotentialPlantPartioning
            }
        }

        /// <summary>Computes the DM and N amounts that are made available for new growth</summary>
        public void DoSupplyCalculations()
        {
            DMRetranslocationSupply = AvailableDMRetranslocation();
            DMReallocationSupply = AvailableDMReallocation();
            NRetranslocationSupply = AvailableNRetranslocation();
            NReallocationSupply = AvailableNReallocation();
        }

        /// <summary>Does the nutrient allocations.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoActualPlantGrowth")]
        private void OnDoActualPlantGrowth(object sender, EventArgs e)
        {
            if (Plant.IsAlive)
            {
                foreach (ZoneState Z in Zones)
                    Z.GrowRootDepth();
                // Do Root Senescence
                DoRemoveBiomass(null, new OrganBiomassRemovalType() { FractionLiveToResidue = SenescenceRate.Value() });
            }
        }

        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantEnding")]
        private void OnPlantEnding(object sender, EventArgs e)
        {
            Biomass total = Live + Dead;

            if (total.Wt > 0.0)
            {
                Detached.Add(Live);
                Detached.Add(Dead);
                SurfaceOrganicMatter.Add(total.Wt * 10, total.N * 10, 0, Plant.CropType, Name);
            }
            Clear();
        }

        #endregion

        #region IArbitrator interface

        /// <summary>Calculate and return the dry matter supply (g/m2)</summary>
        public override BiomassSupplyType CalculateDryMatterSupply()
        {
            dryMatterSupply.Fixation = 0.0;
            dryMatterSupply.Retranslocation = DMRetranslocationSupply;
            dryMatterSupply.Reallocation = DMReallocationSupply;
            return dryMatterSupply;
        }

        /// <summary>Calculate and return the nitrogen supply (g/m2)</summary>
        public override BiomassSupplyType CalculateNitrogenSupply()
        {
            nitrogenSupply.Fixation = 0.0;
            nitrogenSupply.Uptake = 0.0;
            nitrogenSupply.Retranslocation = NRetranslocationSupply;
            nitrogenSupply.Reallocation = NReallocationSupply;

            return nitrogenSupply;
        }

        /// <summary>Calculate and return the dry matter demand (g/m2)</summary>
        public override BiomassPoolType CalculateDryMatterDemand()
        {
            if (Plant.SowingData.Depth < PlantZone.Depth)
            {
                StructuralDMDemand = DemandedDMStructural();
                StorageDMDemand = DemandedDMStorage();
                TotalDMDemand = StructuralDMDemand + StorageDMDemand + MetabolicDMDemand;
                ////This sum is currently not necessary as demand is not calculated on a layer basis.
                //// However it might be some day... and can consider non structural too
            }

            dryMatterDemand.Structural = StructuralDMDemand;
            dryMatterDemand.Storage = StorageDMDemand;

            return dryMatterDemand;
        }

        /// <summary>Calculate and return the nitrogen demand (g/m2)</summary>
        public override BiomassPoolType CalculateNitrogenDemand()
        {
            // This is basically the old/original function with added metabolicN.
            // Calculate N demand based on amount of N needed to bring root N content in each layer up to maximum.

            double NitrogenSwitch = (NitrogenDemandSwitch == null) ? 1.0 : NitrogenDemandSwitch.Value();
            double criticalN = (CriticalNConc == null) ? MinimumNConc.Value() : CriticalNConc.Value();

            StructuralNDemand = 0.0;
            MetabolicNDemand = 0.0;
            StorageNDemand = 0.0;
            foreach (ZoneState Z in Zones)
            {
                Z.StructuralNDemand = new double[Z.soil.Thickness.Length];
                Z.StorageNDemand = new double[Z.soil.Thickness.Length];
                //Note: MetabolicN is assumed to be zero

                double NDeficit = 0.0;
                for (int i = 0; i < Z.LayerLive.Length; i++)
                {
                    Z.StructuralNDemand[i] = Z.LayerLive[i].PotentialDMAllocation * MinimumNConc.Value() * NitrogenSwitch;
                    NDeficit = Math.Max(0.0, MaximumNConc.Value() * (Z.LayerLive[i].Wt + Z.LayerLive[i].PotentialDMAllocation) - (Z.LayerLive[i].N + Z.StructuralNDemand[i]));
                    Z.StorageNDemand[i] = Math.Max(0, NDeficit - Z.StructuralNDemand[i]) * NitrogenSwitch;

                    StructuralNDemand += Z.StructuralNDemand[i];
                    StorageNDemand += Z.StorageNDemand[i];
                }
            }
            nitrogenDemand.Structural = StructuralNDemand;
            nitrogenDemand.Storage = StorageNDemand;
            nitrogenDemand.Metabolic = MetabolicNDemand;
            return nitrogenDemand;
        }

        /// <summary>Computes the amount of structural DM demanded.</summary>
        public double DemandedDMStructural()
        {
            if (DMConversionEfficiency.Value() > 0.0)
            {
                double demandedDM = DMDemandFunction.Value();
                if (StructuralFraction != null)
                    demandedDM *= StructuralFraction.Value() / DMConversionEfficiency.Value();
                else
                    demandedDM /= DMConversionEfficiency.Value();

                return demandedDM;
            }
            // Conversion efficiency is zero!!!!
            return 0.0;
        }

        /// <summary>Computes the amount of non structural DM demanded.</summary>
        public double DemandedDMStorage()
        {
            if ((DMConversionEfficiency.Value() > 0.0) && (StructuralFraction != null))
            {
                double rootLiveStructuralWt = 0.0;
                double rootLiveStorageWt = 0.0;
                foreach (ZoneState Z in Zones)
                    for (int i = 0; i < Z.LayerLive.Length; i++)
                    {
                        rootLiveStructuralWt += Z.LayerLive[i].StructuralWt;
                        rootLiveStorageWt += Z.LayerLive[i].StorageWt;
                    }

                double theoreticalMaximumDM = (rootLiveStructuralWt + StructuralDMDemand) / StructuralFraction.Value();
                double baseAllocated = rootLiveStructuralWt + rootLiveStorageWt + StructuralDMDemand;
                double demandedDM = Math.Max(0.0, theoreticalMaximumDM - baseAllocated) / DMConversionEfficiency.Value();
                return demandedDM;
            }
            // Either there is no Storage fraction or conversion efficiency is zero!!!!
            return 0.0;
        }

        /// <summary>Computes the amount of DM available for retranslocation.</summary>
        public double AvailableDMRetranslocation()
        {
            if (DMRetranslocationFactor != null)
            {
                double rootLiveStorageWt = 0.0;
                foreach (ZoneState Z in Zones)
                    for (int i = 0; i < Z.LayerLive.Length; i++)
                        rootLiveStorageWt += Z.LayerLive[i].StorageWt;

                double availableDM = Math.Max(0.0, rootLiveStorageWt - DMReallocationSupply) * DMRetranslocationFactor.Value();
                if (availableDM < -BiomassToleranceValue)
                    throw new Exception("Negative DM retranslocation value computed for " + Name);

                return availableDM;
            }
            else
            { // By default retranslocation is turned off!!!!
                return 0.0;
            }
        }

        /// <summary>Computes the amount of DM available for reallocation.</summary>
        public double AvailableDMReallocation()
        {
            if (DMReallocationFactor != null)
            {
                double rootLiveStorageWt = 0.0;
                foreach (ZoneState Z in Zones)
                    for (int i = 0; i < Z.LayerLive.Length; i++)
                        rootLiveStorageWt += Z.LayerLive[i].StorageWt;

                double availableDM = rootLiveStorageWt * SenescenceRate.Value() * DMReallocationFactor.Value();
                if (availableDM < -BiomassToleranceValue)
                    throw new Exception("Negative DM reallocation value computed for " + Name);
                return availableDM;
            }
            // By default reallocation is turned off!!!!
            return 0.0;
        }

        /// <summary>Sets the dry matter potential allocation.</summary>
        public override void SetDryMatterPotentialAllocation(BiomassPoolType dryMatter)
        {
            if (PlantZone.Uptake == null)
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
                    for (int layer = 0; layer < Z.soil.Thickness.Length; layer++)
                        Z.LayerLive[layer].PotentialDMAllocation = dryMatter.Structural * RAw[layer] / TotalRAw;
                }
                needToRecalculateLiveDead = true;
            }
        }

        /// <summary>Sets the dry matter allocation.</summary>
        public override void SetDryMatterAllocation(BiomassAllocationType dryMatter)
        {
            double TotalRAw = 0;
            foreach (ZoneState Z in Zones)
                TotalRAw += MathUtilities.Sum(Z.CalculateRootActivityValues());

            Allocated.StructuralWt = dryMatter.Structural;
            Allocated.StorageWt = dryMatter.Storage;
            Allocated.MetabolicWt = dryMatter.Metabolic;

            if (TotalRAw == 0 && Allocated.Wt > 0)
                throw new Exception("Error trying to partition root biomass");

            foreach (ZoneState Z in Zones)
                Z.PartitionRootMass(TotalRAw, Allocated.Wt);
            needToRecalculateLiveDead = true;
        }

        /// <summary>Computes the N amount available for retranslocation.</summary>
        /// <remarks>This is limited to ensure Nconc does not go below MinimumNConc</remarks>
        public double AvailableNRetranslocation()
        {
            if (NRetranslocationFactor != null)
            {
                double labileN = 0.0;
                foreach (ZoneState Z in Zones)
                    for (int i = 0; i < Z.LayerLive.Length; i++)
                        labileN += Math.Max(0.0, Z.LayerLive[i].StorageN - Z.LayerLive[i].StorageWt * MinimumNConc.Value());

                double availableN = Math.Max(0.0, labileN - NReallocationSupply) * NRetranslocationFactor.Value();
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
        public double AvailableNReallocation()
        {
            if (NReallocationFactor != null)
            {
                double rootLiveStorageN = 0.0;
                foreach (ZoneState Z in Zones)
                    for (int i = 0; i < Z.LayerLive.Length; i++)
                        rootLiveStorageN += Z.LayerLive[i].StorageN;

                double availableN = rootLiveStorageN * SenescenceRate.Value() * NReallocationFactor.Value();
                if (availableN < -BiomassToleranceValue)
                    throw new Exception("Negative N reallocation value computed for " + Name);

                return availableN;
            }
            else
            {  // By default reallocation is turned off!!!!
                return 0.0;
            }
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

                for (int layer = 0; layer < thickness.Length; layer++)
                {
                    if (myZone.LayerLive[layer].Wt > 0)
                    {
                        RWC[layer] = (water[layer] - ll15mm[layer]) / (dulmm[layer] - ll15mm[layer]);
                        RWC[layer] = Math.Max(0.0, Math.Min(RWC[layer], 1.0));
                        double SWAF = NUptakeSWFactor.Value(layer);

                        double kno3 = KNO3.Value(layer);
                        double NO3ppm = zone.NO3N[layer] * (100.0 / (bd[layer] * thickness[layer]));
                        NO3Supply[layer] = Math.Min(zone.NO3N[layer] * kno3 * NO3ppm * SWAF, (MaxDailyNUptake.Value() - NO3Uptake));
                        NO3Uptake += NO3Supply[layer];

                        double knh4 = KNH4.Value(layer);
                        double NH4ppm = zone.NH4N[layer] * (100.0 / (bd[layer] * thickness[layer]));
                        NH4Supply[layer] = Math.Min(zone.NH4N[layer] * knh4 * NH4ppm * SWAF, (MaxDailyNUptake.Value() - NH4Uptake));
                        NH4Uptake += NH4Supply[layer];
                    }
                }
            }
        }

        /// <summary>Sets the n allocation.</summary>
        public override void SetNitrogenAllocation(BiomassAllocationType nitrogen)
        {
            double totalStructuralNDemand = 0;
            double totalNDemand = 0;

            foreach (ZoneState Z in Zones)
            {
                totalStructuralNDemand += MathUtilities.Sum(Z.StructuralNDemand);
                totalNDemand += MathUtilities.Sum(Z.StructuralNDemand) + MathUtilities.Sum(Z.StorageNDemand);
            }
            NTakenUp = nitrogen.Uptake;
            Allocated.StructuralN = nitrogen.Structural;
            Allocated.StorageN = nitrogen.Storage;
            Allocated.MetabolicN = nitrogen.Metabolic;

            double surplus = Allocated.N - totalNDemand;
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
                    double totalStorageNDemand = MathUtilities.Sum(Z.StorageNDemand);
                    if (totalStorageNDemand > 0)
                    {
                        double NonStructFrac = Z.StorageNDemand[i] / totalStorageNDemand;
                        Z.LayerLive[i].StorageN += nitrogen.Storage * NonStructFrac;
                        NAllocated += nitrogen.Storage * NonStructFrac;
                    }
                }
            }
            needToRecalculateLiveDead = true;

            if (!MathUtilities.FloatsAreEqual(NAllocated - Allocated.N, 0.0))
                throw new Exception("Error in N Allocation: " + Name);
        }

        /// <summary>Gets or sets the minimum nconc.</summary>
        public override double MinNconc { get { return MinimumNConc.Value(); } }

        /// <summary>Gets the total biomass</summary>
        public Biomass Total { get { return Live + Dead; } }

        /// <summary>Gets the total grain weight</summary>
        [Units("g/m2")]
        public double Wt { get { return Total.Wt; } }

        /// <summary>Gets the total grain N</summary>
        [Units("g/m2")]
        public double N { get { return Total.N; } }

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
                double[] kl = myZone.soil.KL(Plant.Name);
                double[] ll = myZone.soil.LL(Plant.Name);

                double[] supply = new double[myZone.soil.Thickness.Length];
                LayerMidPointDepth = Soil.ToMidPoints(myZone.soil.Thickness);
                for (int layer = 0; layer < myZone.soil.Thickness.Length; layer++)
                {
                    if (layer <= Soil.LayerIndexOfDepth(myZone.Depth, myZone.soil.Thickness))
                    {
                        supply[layer] = Math.Max(0.0, kl[layer] * KLModifier.Value(layer) *
                            (zone.Water[layer] - ll[layer] * myZone.soil.Thickness[layer]) * Soil.ProportionThroughLayer(layer, myZone.Depth, myZone.soil.Thickness));
                    }
                }
                return supply;
            }            
        }

        #endregion

        #region Biomass Removal
        /// <summary>Removes biomass from root layers when harvest, graze or cut events are called.</summary>
        /// <param name="biomassRemoveType">Name of event that triggered this biomass remove call.</param>
        /// <param name="removal">The fractions of biomass to remove</param>
        public override void DoRemoveBiomass(string biomassRemoveType, OrganBiomassRemovalType removal)
        {
            biomassRemovalModel.RemoveBiomassToSoil(biomassRemoveType, removal, PlantZone.LayerLive, PlantZone.LayerDead, Removed, Detached);
        }
        #endregion

    }
}