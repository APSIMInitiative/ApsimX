using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Functions;
using Models.Soils;
using System.Xml.Serialization;
using Models.PMF.Interfaces;
using Models.Soils.Arbitrator;
using APSIM.Shared.Utilities;

namespace Models.PMF.Organs
{
    ///<summary>
    /// The generic root model calculates root growth in terms of rooting depth, biomass accumulation and subsequent 
    /// root length density.
    /// 
    /// **Root Growth**
    /// 
    /// Roots grow downward through the soil profile and rate is determined by RootFrontVelocity. The RootFrontVelocity 
    /// is also influenced by the extension resistance posed by the soil, paramterised using the soil XF value.
    /// 
    /// **Dry Matter Demands**
    /// 
    /// 100% of the dry matter (DM) demanded from the root is structural. The daily DM demand from root is calculated as a 
    /// proportion of total DM supply using a PartitionFraction function. The daily loss of roots is calculated using
    /// a SenescenceRate function.
    /// 
    /// **Nitrogen Demands**
    /// 
    /// The daily structural N demand from root is the product of total DM demand and a nitrogen concentration of MinimumNConc%.
    /// 
    /// **Nitrogen Uptake**
    /// 
    /// Potential N uptake by the root system is calculated for each soil layer that the roots have extended into.
    /// In each layer potential uptake is calculated as the product of the mineral nitrogen in the layer, a factor 
    /// controlling the rate of extraction (kNO<sub>3</sub> and kNH<sub>4</sub>), the concentration of of N (ppm) 
    /// and a soil moisture factor which decreases as the soil dries. Nitrogen uptake demand is limited to the maximum 
    /// of potential uptake and the plants N demand.  Uptake N demand is then passed to the soil arbitrator which 
    /// determines how much of their Nitrogen uptake demand each plant instance will be allowed to take up.
    /// 
    /// **Water Uptake**
    /// 
    /// Potential water uptake by the root system is calculated for each soil layer that the roots have extended into.
    /// In each layer potential uptake is calculated as the product of the available Water in the layer, and a factor 
    /// controlling the rate of extraction (KL). The KL values are set in the soil and may be further modified by the crop
    /// via KLModifier, KNO3 and KN4.
    ///</summary>
    [Serializable]
    [Description("Root Class")]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class Root : BaseOrgan, BelowGround
    {
        #region Links and events

        /// <summary>The arbitrator</summary>
        [Link]
        OrganArbitrator Arbitrator = null;

        /// <summary>Link to the KNO3 link</summary>
        [Link]
        LinearInterpolationFunction KNO3 = null;

        /// <summary>Soil water factor for N Uptake</summary>
        [Link]
        LinearInterpolationFunction NUptakeSWFactor = null;

        /// <summary>Link to the KNH4 link</summary>
        [Link]
        LinearInterpolationFunction KNH4 = null;

        /// <summary>Occurs when [incorp fom].</summary>
        public event FOMLayerDelegate IncorpFOM;

        #endregion

        #region Parameters

        /// <summary>Gets or sets the initial DM for this organ.</summary>
        [Link]
        [Units("g/plant")]
        IFunction InitialDM = null;

        /// <summary>Gets or sets the the specific root length.</summary>
        [Link]
        [Units("m/g")]
        IFunction SpecificRootLength = null;

        /// <summary>The nitrogen demand switch</summary>
        [Link]
        IFunction NitrogenDemandSwitch = null;

        /// <summary>The senescence rate</summary>
        [Link]
        [Units("/d")]
        IFunction SenescenceRate = null;
        
        /// <summary>The root front velocity</summary>
        [Link]
        [Units("mm/d")]
        IFunction RootFrontVelocity = null;
        
        /// <summary>The partition fraction</summary>
        [Link]
        [Units("0-1")]
        IFunction PartitionFraction = null;
        
        /// <summary>The maximum n conc</summary>
        [Link]
        [Units("g/g")]
        IFunction MaximumNConc = null;
        
        /// <summary>The maximum daily n uptake</summary>
        [Link]
        [Units("kg N/ha")]
        IFunction MaxDailyNUptake = null;
        
        /// <summary>The minimum n conc</summary>
        [Link(IsOptional = true)]
        [Units("g/g")]
        IFunction MinimumNConc = null;
        
        /// <summary>The kl modifier</summary>
        [Link]
        [Units("0-1")]
        LinearInterpolationFunction KLModifier = null;
        
        /// <summary>The Maximum Root Depth</summary>
        [Link(IsOptional = true)]
        [Units("0-1")]
        IFunction MaximumRootDepth = null;
        
        /// <summary>The proportion of biomass repired each day</summary>
        [Link(IsOptional = true)]
        public IFunction MaintenanceRespirationFunction = null;

        /// <summary>The kgha2gsm</summary>
        public const double kgha2gsm = 0.1;

        /// <summary>A list of other zone names to grow roots in</summary>
        public List<string> ZoneNamesToGrowRootsIn { get; set; }

        /// <summary>The root depths for each addition zone.</summary>
        public List<double> ZoneRootDepths { get; set; }

        /// <summary>The live weights for each addition zone.</summary>
        public List<double> ZoneInitialDM { get; set; }

        #endregion

        #region States

        class ZoneState
        {
            /// <summary>Name of the parent plant</summary>
            public string plantName;

            /// <summary>Lower limit</summary>
            public double[] LL = null;

            /// <summary>Exploration factor</summary>
            public double[] XF = null;

            /// <summary>KL</summary>
            public double[] KL = null;

            /// <summary>The soil in this zone</summary>
            public Soil soil = null;
          
            /// <summary>Name of zone.</summary>
            public string Name;

            /// <summary>The uptake</summary>
            public double[] Uptake = null;

            /// <summary>The delta n h4</summary>
            public double[] DeltaNH4;

            /// <summary>The delta n o3</summary>
            public double[] DeltaNO3;

            /// <summary>Holds actual DM allocations to use in allocating N to structural and Non-Structural pools</summary>
            [XmlIgnore]
            [Units("g/2")]
            public double[] DMAllocated { get; set; }

            /// <summary>Demand for structural N, set when Ndemand is called and used again in N allocation</summary>
            [XmlIgnore]
            [Units("g/2")]
            public double[] StructuralNDemand { get; set; }

            /// <summary>Demand for Non-structural N, set when Ndemand is called and used again in N allocation</summary>
            [XmlIgnore]
            [Units("g/m2")]
            public double[] NonStructuralNDemand { get; set; }

            /// <summary>The Nuptake</summary>
            public double[] NitUptake = null;

            /// <summary>Gets or sets the nuptake supply.</summary>
            public double NuptakeSupply { get; set; }

            /// <summary>Gets or sets the layer live.</summary>
            [XmlIgnore]
            public Biomass[] LayerLive { get; set; }

            /// <summary>Gets or sets the layer dead.</summary>
            [XmlIgnore]
            public Biomass[] LayerDead { get; set; }

            /// <summary>Gets or sets the length.</summary>
            [XmlIgnore]
            public double Length { get; set; }

            /// <summary>Gets or sets the depth.</summary>
            [XmlIgnore]
            [Units("mm")]
            public double Depth { get; set; }

            /// <summary>Gets depth or the mid point of the cuttent layer under examination</summary>
            [XmlIgnore]
            [Units("mm")]
            public double LayerMidPointDepth { get; set; }

            /// <summary>Constructor</summary>
            /// <param name="soil">The soil in the zone.</param>
            /// <param name="plantName">The name of the parent plant.</param>
            /// <param name="depth">Root depth (mm)</param>
            /// <param name="initialDM">Initial dry matter</param>
            /// <param name="population">plant population</param>
            /// <param name="maxNConc">maximum n concentration</param>
            public ZoneState(Soil soil, string plantName, double depth, double initialDM, double population, double maxNConc)
            {
                this.plantName = plantName;
                this.soil = soil;
                if (this.soil.Crop(plantName) == null)
                    throw new Exception("Cannot find a soil crop parameterisation for " + plantName);
                LL = (this.soil.Crop(plantName) as SoilCrop).LL;
                XF = (this.soil.Crop(plantName) as SoilCrop).XF;
                KL = (this.soil.Crop(plantName) as SoilCrop).KL;

                Clear();
                Zone zone = Apsim.Parent(soil, typeof(Zone)) as Zone;
                if (zone == null)
                    throw new Exception("Soil " + soil + " is not in a zone.");
                Name = zone.Name;
                Initialise(depth, initialDM, population, maxNConc);
            }

            /// <summary>Initialise the zone.</summary>
            /// <param name="depth">Root depth (mm)</param>
            /// <param name="initialDM">Initial dry matter</param>
            /// <param name="population">plant population</param>
            /// <param name="maxNConc">maximum n concentration</param>
            public void Initialise(double depth, double initialDM, double population, double maxNConc)
            {
                Depth = depth;
                double AccumulatedDepth = 0;
                double InitialLayers = 0;
                for (int layer = 0; layer < soil.Thickness.Length; layer++)
                {
                    if (AccumulatedDepth < Depth)
                        InitialLayers += 1;
                    AccumulatedDepth += soil.Thickness[layer];
                }
                for (int layer = 0; layer < soil.Thickness.Length; layer++)
                {
                    if (layer <= InitialLayers - 1)
                    {
                        //distribute root biomass evenly through root depth
                        LayerLive[layer].StructuralWt = initialDM / InitialLayers * population;
                        LayerLive[layer].StructuralN = initialDM / InitialLayers * maxNConc * population;
                    }
                }
            }

            /// <summary>Clears this instance.</summary>
            public void Clear()
            {
                Uptake = null;
                NitUptake = null;
                DeltaNO3 = new double[soil.Thickness.Length];
                DeltaNH4 = new double[soil.Thickness.Length];

                Length = 0.0;
                Depth = 0.0;

                if (LayerLive == null || LayerLive.Length == 0)
                {
                    LayerLive = new Biomass[soil.Thickness.Length];
                    LayerDead = new Biomass[soil.Thickness.Length];
                    for (int i = 0; i < soil.Thickness.Length; i++)
                    {
                        LayerLive[i] = new Biomass();
                        LayerDead[i] = new Biomass();
                    }
                }
                else
                {
                    for (int i = 0; i < soil.Thickness.Length; i++)
                    {
                        LayerLive[i].Clear();
                        LayerDead[i].Clear();
                    }
                }
            }
        }

        /// <summary>A list of all zones to grow roots in</summary>
        [NonSerialized]
        private List<ZoneState> zones = new List<ZoneState>();

        /// <summary>The zone where the plant is growing</summary>
        [NonSerialized]
        private ZoneState plantZone = null;
        #endregion
        
        #region Class Properties

        /// <summary>Gets the root length density.</summary>
        [Units("mm/mm3")]
        public double[] LengthDensity
        {
            get
            {
                double[] value = new double[plantZone.soil.Thickness.Length];
                for (int i = 0; i < plantZone.soil.Thickness.Length; i++)
                    value[i] = plantZone.LayerLive[i].Wt * SpecificRootLength.Value * 1000 / 1000000 / plantZone.soil.Thickness[i];
                return value;
            }
        }

        ///<Summary>Sum Non-Structural N demand for all layers</Summary>
        [Units("g/m2")]
        [XmlIgnore]
        public double TotalNonStructuralNDemand { get; set; }

        ///<Summary>Sum Structural N demand for all layers</Summary>
        [Units("g/m2")]
        [XmlIgnore]
        public double TotalStructuralNDemand { get; set; }

        ///<Summary>Sum N demand for all layers</Summary>
        [Units("g/m2")]
        [XmlIgnore]
        public double TotalNDemand { get; set; }

        ///<Summary>Total N Allocated to roots</Summary>
        [Units("g/m2")]
        [XmlIgnore]
        public double TotalNAllocated { get; set; }

        ///<Summary>Total DM Demanded by roots</Summary>
        [Units("g/m2")]
        [XmlIgnore]
        public double TotalDMDemand { get; set; }

        ///<Summary>Total DM Allocated to roots</Summary>
        [Units("g/m2")]
        [XmlIgnore]
        public double TotalDMAllocated { get; set; }

        ///<Summary>The amount of N taken up after arbitration</Summary>
        [Units("g/m2")]
        [XmlIgnore]
        public double NTakenUp { get; set; }

        /// <summary>Root depth.</summary>
        [XmlIgnore]
        public double Depth { get { return plantZone.Depth; } }

        /// <summary>Layer live</summary>
        [XmlIgnore]
        public Biomass[] LayerLive { get { return plantZone.LayerLive; } }

        /// <summary>Layer dead.</summary>
        [XmlIgnore]
        public Biomass[] LayerDead { get { return plantZone.LayerDead; } }

        /// <summary>Gets or sets the length.</summary>
        [XmlIgnore]
        public double Length { get { return plantZone.Length; } }

        #endregion
    
        #region Functions

        /// <summary>Constructor</summary>
        public Root()
        {
            ZoneNamesToGrowRootsIn = new List<string>();
            ZoneRootDepths = new List<double>();
            ZoneInitialDM = new List<double>();
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

            plantZone = new ZoneState(soil, Plant.Name, 0, InitialDM.Value, Plant.Population, MaximumNConc.Value);
            zones = new List<ZoneState>();
        }

        /// <summary>Called when crop is sown</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantSowing")]
        private void OnPlantSowing(object sender, SowPlant2Type data)
        {
            if (data.Plant == Plant)
            {
                plantZone.Initialise(Plant.SowingData.Depth, InitialDM.Value, Plant.Population, MaximumNConc.Value);
                InitialiseZones();
            }
        }

        /// <summary>Initialise all zones.</summary>
        private void InitialiseZones()
        {
            zones.Clear();
            zones.Add(plantZone);
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
                    ZoneState newZone = new ZoneState(soil, Plant.Name, ZoneRootDepths[i], ZoneInitialDM[i], Plant.Population, MaximumNConc.Value);
                    zones.Add(newZone);
                }
            }
        }

        /// <summary>Event from sequencer telling us to do our potential growth.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoPotentialPlantGrowth")]
        private void OnDoPotentialPlantGrowth(object sender, EventArgs e)
        {
            if (Plant.IsEmerged)
                plantZone.Length = MathUtilities.Sum(LengthDensity);

        }

        /// <summary>Does the nutrient allocations.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoActualPlantGrowth")]
        private void OnDoActualPlantGrowth(object sender, EventArgs e)
        {

            if (Plant.IsAlive)
            {
                // Do Root Front Advance
                int RootLayer = LayerIndex(plantZone.Depth);

                plantZone.Depth = plantZone.Depth + RootFrontVelocity.Value * plantZone.XF[RootLayer];

                //Limit root depth for impeded layers
                double MaxDepth = 0;
                for (int i = 0; i < plantZone.soil.Thickness.Length; i++)
                    if (plantZone.XF[i] > 0)
                        MaxDepth += plantZone.soil.Thickness[i];
                //Limit root depth for the crop specific maximum depth
                if (MaximumRootDepth != null)
                    MaxDepth = Math.Min(MaximumRootDepth.Value, MaxDepth);

                plantZone.Depth = Math.Min(plantZone.Depth, MaxDepth);

                // Do Root Senescence
                DoRootBiomassRemoval(SenescenceRate.Value);
            }
        }

        /// <summary>Return true if the specified zone is known to ROOT</summary>
        /// <param name="zoneName">The zone name to look for</param>
        public bool HaveRootsInZone(string zoneName)
        {
            return zones.Find(z => z.Name == zoneName) != null;
        }

        /// <summary>Does the water uptake.</summary>
        /// <param name="Amount">The amount.</param>
        /// <param name="zoneName">Zone name to do water uptake in</param>
        public override void DoWaterUptake(double[] Amount, string zoneName)
        {
            ZoneState zone = zones.Find(z => z.Name == zoneName);
            if (zone == null)
                throw new Exception("Cannot find a zone called " + zoneName);
				
			zone.Uptake = MathUtilities.Multiply_Value(Amount, -1.0);
            zone.soil.SoilWater.dlt_sw_dep = zone.Uptake;
        }

        /// <summary>Does the Nitrogen uptake.</summary>
        /// <param name="zonesFromSoilArbitrator">List of zones from soil arbitrator</param>
        public override void DoNitrogenUptake(List<ZoneWaterAndN> zonesFromSoilArbitrator)
        {
            foreach (ZoneWaterAndN thisZone in zonesFromSoilArbitrator)
            {
                ZoneState zone = zones.Find(z => z.Name == thisZone.Name);
                if (zone == null)
                    throw new Exception("Cannot find a zone called " + thisZone.Name);

                // Send the delta water back to SoilN that we're going to uptake.
                NitrogenChangedType NitrogenUptake = new NitrogenChangedType();
                NitrogenUptake.DeltaNO3 = MathUtilities.Multiply_Value(thisZone.NO3N, -1.0);
                NitrogenUptake.DeltaNH4 = MathUtilities.Multiply_Value(thisZone.NH4N, -1.0);

                zone.NitUptake = MathUtilities.Add(NitrogenUptake.DeltaNO3, NitrogenUptake.DeltaNH4);
                zone.soil.SoilNitrogen.SetNitrogenChanged(NitrogenUptake);
            }
        }
        
        /// <summary>Layers the index.</summary>
        /// <param name="depth">The depth.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Depth deeper than bottom of soil profile</exception>
        private int LayerIndex(double depth)
        {
            double CumDepth = 0;
            for (int i = 0; i < plantZone.soil.Thickness.Length; i++)
            {
                CumDepth = CumDepth + plantZone.soil.Thickness[i];
                if (CumDepth >= depth) { return i; }
            }
            throw new Exception("Depth deeper than bottom of soil profile");
        }
        
        /// <summary>Roots the proportion.</summary>
        /// <param name="layer">The layer.</param>
        /// <param name="root_depth">The root_depth.</param>
        /// <returns></returns>
        private double RootProportion(int layer, double root_depth)
        {
            double depth_to_layer_bottom = 0;   // depth to bottom of layer (mm)
            double depth_to_layer_top = 0;      // depth to top of layer (mm)
            double depth_to_root = 0;           // depth to root in layer (mm)
            double depth_of_root_in_layer = 0;  // depth of root within layer (mm)
            // Implementation Section ----------------------------------
            for (int i = 0; i <= layer; i++)
                depth_to_layer_bottom += plantZone.soil.Thickness[i];
            depth_to_layer_top = depth_to_layer_bottom - plantZone.soil.Thickness[layer];
            depth_to_root = Math.Min(depth_to_layer_bottom, root_depth);
            depth_of_root_in_layer = Math.Max(0.0, depth_to_root - depth_to_layer_top);

            return depth_of_root_in_layer / plantZone.soil.Thickness[layer];
        }
        
        /// <summary>Called when crop is ending</summary>
        public override void DoPlantEnding()
        {
            //Send all root biomass to soil FOM
            DoRootBiomassRemoval(1.0);
            Clear();
        }

        /// <summary>Clears this instance.</summary>
        protected override void Clear()
        {
            base.Clear();
            plantZone.Clear();
            zones.Clear();
        }

        /// <summary>Performs the removal of roots</summary>
        /// <param name="detachFraction">Fraction to send to residue (soil FOM)</param>
        /// <param name="removeFraction">Fraction to remove from the system</param>
        private void DoRootBiomassRemoval(double detachFraction, double removeFraction = 0.0)
        {
            //NOTE: at the moment Root has no Dead pool
            FOMLayerLayerType[] FOMLayers = new FOMLayerLayerType[plantZone.soil.Thickness.Length];
            double RemainingFraction = 1.0 - (detachFraction + removeFraction);
            double detachingWt = 0.0;
            double detachingN = 0.0;
            for (int layer = 0; layer < plantZone.soil.Thickness.Length; layer++)
            {
                detachingWt = plantZone.LayerLive[layer].Wt * detachFraction;
                detachingN = plantZone.LayerLive[layer].N * detachFraction;
                RemovedWt += plantZone.LayerLive[layer].Wt * removeFraction;
                RemovedN += plantZone.LayerLive[layer].N * removeFraction;
                DetachedWt += detachingWt;
                DetachedN += detachingN;

                plantZone.LayerLive[layer].StructuralWt *= RemainingFraction;
                plantZone.LayerLive[layer].NonStructuralWt *= RemainingFraction;
                plantZone.LayerLive[layer].MetabolicWt *= RemainingFraction;

                plantZone.LayerLive[layer].StructuralN *= RemainingFraction;
                plantZone.LayerLive[layer].NonStructuralN *= RemainingFraction;
                plantZone.LayerLive[layer].MetabolicN *= RemainingFraction;

                FOMType fom = new FOMType();
                fom.amount = (float) (detachingWt * 10);
                fom.N = (float) (detachingN * 10);
                fom.C = (float) (0.40 * detachingWt * 10);
                fom.P = 0.0;
                fom.AshAlk = 0.0;

                FOMLayerLayerType Layer = new FOMLayerLayerType();
                Layer.FOM = fom;
                Layer.CNR = 0.0;
                Layer.LabileP = 0.0;
                FOMLayers[layer] = Layer;
            }
            FOMLayerType FomLayer = new FOMLayerType();
            FomLayer.Type = Plant.CropType;
            FomLayer.Layer = FOMLayers;
            IncorpFOM.Invoke(FomLayer);
        }

        #endregion

        #region Arbitrator method calls
        /// <summary>Gets or sets the dm demand.</summary>
        public override BiomassPoolType DMDemand
        {
            get
            {
                double Demand = 0;
                if (Plant.IsAlive && Plant.SowingData.Depth < plantZone.Depth)
                    Demand = Arbitrator.DMSupply * PartitionFraction.Value;
                TotalDMDemand = Demand;//  The is not really necessary as total demand is always not calculated on a layer basis so doesn't need summing.  However it may some day
                return new BiomassPoolType { Structural = Demand };
            }
        }

        /// <summary>Sets the dm potential allocation.</summary>
        public override BiomassPoolType DMPotentialAllocation
        {
            set
            {
                if (plantZone.Uptake == null)
                    throw new Exception("No water and N uptakes supplied to root. Is Soil Arbitrator included in the simulation?");
           
                if (plantZone.Depth <= 0)
                    return; //cannot allocate growth where no length

                if (DMDemand.Structural == 0)
                    if (value.Structural < 0.000000000001) { }//All OK
                    else
                        throw new Exception("Invalid allocation of potential DM in" + Name);
                // Calculate Root Activity Values for water and nitrogen
                double[] RAw = new double[plantZone.soil.Thickness.Length];
                double[] RAn = new double[plantZone.soil.Thickness.Length];
                double TotalRAw = 0;
                double TotalRAn = 0; ;

                for (int layer = 0; layer < plantZone.soil.Thickness.Length; layer++)
                {
                    if (layer <= LayerIndex(plantZone.Depth))
                        if (plantZone.LayerLive[layer].Wt > 0)
                        {
                            RAw[layer] = plantZone.Uptake[layer] / plantZone.LayerLive[layer].Wt
                                       * plantZone.soil.Thickness[layer]
                                       * RootProportion(layer, plantZone.Depth);
                            RAw[layer] = Math.Max(RAw[layer], 1e-20);  // Make sure small numbers to avoid lack of info for partitioning

                            RAn[layer] = (plantZone.DeltaNO3[layer] + plantZone.DeltaNH4[layer]) / plantZone.LayerLive[layer].Wt
                                           * plantZone.soil.Thickness[layer]
                                           * RootProportion(layer, plantZone.Depth);
                            RAn[layer] = Math.Max(RAw[layer], 1e-10);  // Make sure small numbers to avoid lack of info for partitioning
                        }
                        else if (layer > 0)
                        {
                            RAw[layer] = RAw[layer - 1];
                            RAn[layer] = RAn[layer - 1];
                        }
                        else
                        {
                            RAw[layer] = 0;
                            RAn[layer] = 0;
                        }
                    TotalRAw += RAw[layer];
                    TotalRAn += RAn[layer];
                }
                double allocated = 0;
                for (int layer = 0; layer < plantZone.soil.Thickness.Length; layer++)
                {
                    if (TotalRAw > 0)

                        plantZone.LayerLive[layer].PotentialDMAllocation = value.Structural * RAw[layer] / TotalRAw;
                    else if (value.Structural > 0)
                        throw new Exception("Error trying to partition potential root biomass");
                    allocated += (TotalRAw > 0) ? value.Structural * RAw[layer] / TotalRAw : 0;
                }
            }
        }

        /// <summary>Sets the dm allocation.</summary>
        public override BiomassAllocationType DMAllocation
        {
            set
            {
                TotalDMAllocated = value.Structural;
                plantZone.DMAllocated = new double[plantZone.soil.Thickness.Length];
            
                // Calculate Root Activity Values for water and nitrogen
                double[] RAw = new double[plantZone.soil.Thickness.Length];
                double[] RAn = new double[plantZone.soil.Thickness.Length];
                double TotalRAw = 0;
                double TotalRAn = 0;

                if (plantZone.Depth <= 0)
                    return; // cannot do anything with no depth
                for (int layer = 0; layer < plantZone.soil.Thickness.Length; layer++)
                {
                    if (layer <= LayerIndex(plantZone.Depth))
                        if (plantZone.LayerLive[layer].Wt > 0)
                        {
                            RAw[layer] = plantZone.Uptake[layer] / plantZone.LayerLive[layer].Wt
                                       * plantZone.soil.Thickness[layer]
                                       * RootProportion(layer, plantZone.Depth);
                            RAw[layer] = Math.Max(RAw[layer], 1e-20);  // Make sure small numbers to avoid lack of info for partitioning

                            RAn[layer] = (plantZone.DeltaNO3[layer] + plantZone.DeltaNH4[layer]) / plantZone.LayerLive[layer].Wt
                                       * plantZone.soil.Thickness[layer]
                                       * RootProportion(layer, plantZone.Depth);
                            RAn[layer] = Math.Max(RAw[layer], 1e-10);  // Make sure small numbers to avoid lack of info for partitioning

                        }
                        else if (layer > 0)
                        {
                            RAw[layer] = RAw[layer - 1];
                            RAn[layer] = RAn[layer - 1];
                        }
                        else
                        {
                            RAw[layer] = 0;
                            RAn[layer] = 0;
                        }
                    TotalRAw += RAw[layer];
                    TotalRAn += RAn[layer];
                }
                for (int layer = 0; layer < plantZone.soil.Thickness.Length; layer++)
                {
                    if (TotalRAw > 0)
                    {
                        plantZone.LayerLive[layer].StructuralWt += value.Structural * RAw[layer] / TotalRAw;
                        plantZone.DMAllocated[layer] += value.Structural * RAw[layer] / TotalRAw;
                    }
                    else if (value.Structural > 0)
                        throw new Exception("Error trying to partition root biomass");
                        
                }
            }
        }

        /// <summary>Gets or sets the n demand.</summary>
        [Units("g/m2")]
        public override BiomassPoolType NDemand
        {
            get
            {
                plantZone.StructuralNDemand = new double[plantZone.soil.Thickness.Length];
                plantZone.NonStructuralNDemand = new double[plantZone.soil.Thickness.Length];
            
                //Calculate N demand based on amount of N needed to bring root N content in each layer up to maximum
                double _NitrogenDemandSwitch = 1;
                if (NitrogenDemandSwitch != null) //Default of 1 means demand is always truned on!!!!
                    _NitrogenDemandSwitch = NitrogenDemandSwitch.Value;
                for (int i = 0; i < plantZone.LayerLive.Length; i++)
                {
                    plantZone.StructuralNDemand[i] = plantZone.LayerLive[i].PotentialDMAllocation * MinNconc *  _NitrogenDemandSwitch;
                    double NDeficit = Math.Max(0.0, MaximumNConc.Value * (plantZone.LayerLive[i].Wt + plantZone.LayerLive[i].PotentialDMAllocation) - (plantZone.LayerLive[i].N + plantZone.StructuralNDemand[i]));
                    plantZone.NonStructuralNDemand[i] = Math.Max(0, NDeficit - plantZone.StructuralNDemand[i]) * _NitrogenDemandSwitch;
                }
                TotalNonStructuralNDemand = MathUtilities.Sum(plantZone.NonStructuralNDemand);
                TotalStructuralNDemand = MathUtilities.Sum(plantZone.StructuralNDemand);
                TotalNDemand = TotalNonStructuralNDemand + TotalStructuralNDemand;
                return new BiomassPoolType { Structural = TotalStructuralNDemand, NonStructural = TotalNonStructuralNDemand };
            }
        }

        /// <summary>Gets the nitrogne supply from the specified zone.</summary>
        /// <param name="zone">The zone.</param>
        public override double[] NO3NSupply(ZoneWaterAndN zone)
        {
            return CalcNUptake(zone.NO3N, zone.Name, KNO3);
        }

        /// <summary>Gets the nitrogne supply from the specified zone.</summary>
        /// <param name="zone">The zone.</param>
        public override double[] NH4NSupply(ZoneWaterAndN zone)
        {
            return CalcNUptake(zone.NH4N, zone.Name, KNH4);
        }

        /// <summary>Calculate NO3 or NH4 uptake</summary>
        /// <param name="N">The array of NO3 or NH4 values</param>
        /// <param name="zoneName">The zone name to calculate uptake from.</param>
        /// <param name="K">The K modifier</param>
        private double[] CalcNUptake(double[] N, string zoneName, LinearInterpolationFunction K)
        {
            ZoneState myZone = zones.Find(z => z.Name == zoneName);
            if (myZone != null)
            {
                double[] Nsupply = new double[myZone.soil.Thickness.Length];
                double NUptake = 0;
                for (int layer = 0; layer < myZone.soil.Thickness.Length; layer++)
                {
                    if (myZone.LayerLive[layer].Wt > 0)
                    {
                        double RWC = 0;
                        RWC = (myZone.soil.Water[layer] - myZone.soil.SoilWater.LL15mm[layer]) / (myZone.soil.SoilWater.DULmm[layer] - myZone.soil.SoilWater.LL15mm[layer]);
                        RWC = Math.Max(0.0, Math.Min(RWC, 1.0));
                        double k = K.ValueForX(LengthDensity[layer]);
                        double SWAF = NUptakeSWFactor.ValueForX(RWC);
                        double Nppm = N[layer] * (100.0 / (myZone.soil.BD[layer] * myZone.soil.Thickness[layer]));
                        Nsupply[layer] = Math.Min(N[layer] * k * Nppm * SWAF, (MaxDailyNUptake.Value - NUptake));
                        NUptake += Nsupply[layer];
                    }
                }
                return Nsupply;
            }
            return null;
        }

        /// <summary>Sets the n allocation.</summary>
        public override BiomassAllocationType NAllocation
        {
            set
            {
                NTakenUp = value.Uptake;
                TotalNAllocated = value.Structural + value.NonStructural;
                double surpluss = TotalNAllocated - TotalNDemand;
                if (surpluss > 0.000000001)
                     { throw new Exception("N Allocation to roots exceeds Demand"); }
                
                double NAllocated = 0;
                for (int i = 0; i < plantZone.LayerLive.Length; i++)
                {
                    if (TotalStructuralNDemand > 0)
                    {
                        double StructFrac = plantZone.StructuralNDemand[i] / TotalStructuralNDemand;
                        plantZone.LayerLive[i].StructuralN += value.Structural * StructFrac;
                        NAllocated += value.Structural * StructFrac;
                    }
                    if (TotalNonStructuralNDemand > 0)
                    {
                        double NonStructFrac = plantZone.NonStructuralNDemand[i] / TotalNonStructuralNDemand;
                        plantZone.LayerLive[i].NonStructuralN += value.NonStructural * NonStructFrac;
                        NAllocated += value.NonStructural * NonStructFrac;
                    }
                }

                if (!MathUtilities.FloatsAreEqual(NAllocated - TotalNAllocated, 0.0))
                    throw new Exception("Error in N Allocation: " + Name);
                
            }
        }

        /// <summary>Gets or sets the minimum nconc.</summary>
        public override double MinNconc
        {
            get
            {
                if (MinimumNConc != null)
                    return MinimumNConc.Value;
                else
                    return 0.01;
            }
        }

        /// <summary>Gets or sets the water supply.</summary>
        /// <param name="zone">The zone.</param>
        public override double[] WaterSupply(ZoneWaterAndN zone)
        {
            ZoneState myZone = zones.Find(z => z.Name == zone.Name);
            if (myZone == null)
                return null;

            double[] supply = new double[myZone.soil.Thickness.Length];
            double[] layerMidPoints = Soil.ToMidPoints(myZone.soil.Thickness);
            for (int layer = 0; layer < myZone.soil.Thickness.Length; layer++)
            {
                if (layer <= LayerIndex(myZone.Depth))
                    supply[layer] = Math.Max(0.0, myZone.KL[layer] * KLModifier.ValueForX(layerMidPoints[layer]) *
                        (zone.Water[layer] - myZone.LL[layer] * myZone.soil.Thickness[layer]) * RootProportion(layer, myZone.Depth));
            }

            return supply;
        }

        /// <summary>Gets or sets the water uptake.</summary>
        [Units("mm")]
        public double WaterUptake
        {
            get
            {
                double uptake = MathUtilities.Sum(plantZone.Uptake);
                foreach (ZoneState zone in zones)
                    uptake = uptake + MathUtilities.Sum(plantZone.Uptake);
                return -uptake;
            }
        }

        /// <summary>Gets or sets the water uptake.</summary>
        [Units("mm")]
        public double[] WaterUptakeByZone
        {
            get
            {
                List<double> uptakes = new List<double>();
                uptakes.Add(-MathUtilities.Sum(plantZone.Uptake));
                foreach (ZoneState zone in zones)
                    uptakes.Add(-MathUtilities.Sum(zone.Uptake));
                    
                return uptakes.ToArray();
            }
        }

        /// <summary>Gets or sets the water uptake.</summary>
        [Units("kg/ha")]
        public override double NUptake
        {
            get
            {
                double uptake = MathUtilities.Sum(plantZone.NitUptake);
                foreach (ZoneState zone in zones)
                    uptake = MathUtilities.Sum(zone.NitUptake);
                return uptake;
            }
        }
        #endregion

        #region Biomass Removal
        /// <summary>Removes biomass from root layers when harvest, graze or cut events are called.</summary>
        public override void DoRemoveBiomass(OrganBiomassRemovalType value)
        {
            //NOTE: roots don't have dead biomass
            double totalFractionToRemove = value.FractionLiveToRemove + value.FractionLiveToResidue;
            if (totalFractionToRemove > 1.0 || totalFractionToRemove < 0)
                throw new Exception("The sum of FractionToResidue and FractionToRemove sent is greater than 1 or less than 0.");
            
            DoRootBiomassRemoval(value.FractionLiveToResidue, value.FractionLiveToRemove);
        }
        #endregion

    }
}
