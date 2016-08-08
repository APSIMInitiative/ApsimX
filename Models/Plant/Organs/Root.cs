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
    /// The generic root model calculates root growth in terms of rooting depth, biomass accumulation and subsequent root length density.
    ///</summary>
    /// \param InitialDM <b>(Constant)</b> The initial dry weight of root (\f$g mm^{-2}\f$. CHECK).
    /// \param SpecificRootLength <b>(Constant)</b> The length of the specific root 
    ///     (\f$m g^{-1}\f$. CHECK).
    /// \param KNO3 <b>(Constant)</b> Fraction of extractable soil NO3 (\f$K_{NO3}\f$, unitless).  
    /// \param KNH4 <b>(Constant)</b> Fraction of extractable soil NH4 (\f$K_{NH4}\f$, unitless).  
    /// \param NitrogenDemandSwitch <b>(IFunction)</b> Whether to switch on nitrogen demand 
    ///     when nitrogen deficit is calculated (0 or 1, unitless).
    /// \param RootFrontVelocity <b>(IFunction)</b> The daily growth speed of root depth 
    ///     (\f$mm d^{-1}\f$. CHECK).
    /// \param PartitionFraction <b>(IFunction)</b> The fraction of biomass partitioning 
    ///     into root (0-1, unitless).
    /// \param KLModifier <b>(IFunction)</b> The modifier for KL factor which is defined as 
    ///     the fraction of available water able to be extracted per day, and empirically 
    ///     derived incorporating both plant and soil factors which limit rate of water 
    ///     update (0-1, unitless).
    /// \param TemperatureEffect <b>(IFunction)</b> 
    ///     The temperature effects on root depth growth (0-1, unitless).
    /// \param MaximumNConc <b>(IFunction)</b> 
    ///     Maximum nitrogen concentration (\f$g m^{-2}\f$. CHECK).
    /// \param MinimumNConc <b>(IFunction)</b> 
    ///     Minimum nitrogen concentration (\f$g m^{-2}\f$. CHECK).
    /// \param MaxDailyNUptake <b>(IFunction)</b> 
    ///     Maximum daily nitrogen update (\f$kg ha^{-1}\f$. CHECK).
    /// 
    /// \param SenescenceRate <b>(IFunction, Optional)</b> The daily senescence rate of 
    ///     root length (0-1, unitless).
    ///     
    /// \retval Length Total root length (mm).
    /// \retval Depth Root depth (mm).
    /// 
    ///<remarks>
    /// 
    /// Potential root growth 
    /// ------------------------
    ///  
    /// Actual root growth
    /// ------------------------
    /// 
    /// Nitrogen deficit 
    /// ------------------------
    /// 
    ///</remarks>
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
        [Link(IsOptional = true)]
        [Units("/d")]
        IFunction SenescenceRate = null;
        /// <summary>The temperature effect</summary>
        [Link(IsOptional = true)]
        [Units("0-1")]
        IFunction TemperatureEffect = null;
        /// <summary>The root front velocity</summary>
        [Link]
        [Units("mm/d")]
        IFunction RootFrontVelocity = null;
        /// <summary>The partition fraction</summary>
        [Link(IsOptional = true)]
        [Units("0-1")]
        IFunction PartitionFraction = null;
        /// <summary>The maximum n conc</summary>
        [Link(IsOptional = true)]
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
        IFunction KLModifier = null;
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
            /// <summary>The soil in this zone</summary>
            public Soil soil = null;

            /// <summary>The soil crop in this zone</summary>
            public SoilCrop soilCrop = null;
            
            /// <summary>Name of zone.</summary>
            public string Name;

            /// <summary>The uptake</summary>
            public double[] Uptake = null;
            /// <summary>The delta n h4</summary>
            public double[] DeltaNH4;
            /// <summary>The delta n o3</summary>
            public double[] DeltaNO3;
            /// <summary>
            /// Holds actual DM allocations to use in allocating N to structural and Non-Structural pools
            /// </summary>
            [XmlIgnore]
            [Units("g/2")]
            public double[] DMAllocated { get; set; }
            /// <summary>
            /// Demand for structural N, set when Ndemand is called and used again in N allocation
            /// </summary>
            [XmlIgnore]
            [Units("g/2")]
            public double[] StructuralNDemand { get; set; }
            /// <summary>
            /// Demand for Non-structural N, set when Ndemand is called and used again in N allocation
            /// </summary>
            [XmlIgnore]
            [Units("g/m2")]
            public double[] NonStructuralNDemand { get; set; }
            /// <summary>The _ senescence rate</summary>
            public double mySenescenceRate = 0;
            /// <summary>The Nuptake</summary>
            public double[] NitUptake = null;

            /// <summary>Gets or sets the nuptake supply.</summary>
            public double NuptakeSupply { get; set; }

            /// <summary>Gets or sets the layer live.</summary>
            /// <value>The layer live.</value>
            [XmlIgnore]
            public Biomass[] LayerLive { get; set; }
            /// <summary>Gets or sets the layer dead.</summary>
            /// <value>The layer dead.</value>
            [XmlIgnore]
            public Biomass[] LayerDead { get; set; }
            /// <summary>Gets or sets the length.</summary>
            /// <value>The length.</value>
            [XmlIgnore]
            public double Length { get; set; }

            /// <summary>Gets or sets the depth.</summary>
            /// <value>The depth.</value>
            [XmlIgnore]
            [Units("mm")]
            public double Depth { get; set; }

            /// <summary>Gets depth or the mid point of the cuttent layer under examination</summary>
            /// <value>The depth.</value>
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
                this.soil = soil;
                soilCrop = this.soil.Crop(plantName) as SoilCrop;
                if (soilCrop == null)
                    throw new Exception("Cannot find a soil crop parameterisation for " + plantName);
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
            private void Initialise(double depth, double initialDM, double population, double maxNConc)
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
                mySenescenceRate = 0.0;
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
        /// <summary>Gets a value indicating whether this instance is growing.</summary>
        private bool isGrowing { get { return (Plant.IsAlive && Plant.SowingData.Depth < plantZone.Depth); } }


        /// <summary>Gets the l ldep.</summary>
        /// <value>The l ldep.</value>
        [Units("mm")]
        double[] LLdep
        {
            get
            {
                double[] value = new double[plantZone.soil.Thickness.Length];
                for (int i = 0; i < plantZone.soil.Thickness.Length; i++)
                    value[i] = plantZone.soilCrop.LL[i] * plantZone.soil.Thickness[i];
                return value;
            }
        }

        /// <summary>Gets the root length density.</summary>
        /// <value>The current length density.</value>
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
        ///<Summary>Superfloruis docummentation added to get solution compilling</Summary>
        [Units("g/m2")]
        [XmlIgnore]
        public double TotalNAllocated { get; set; }
        ///<Summary>Superfloruis docummentation added to get solution compilling</Summary>
        [Units("g/m2")]
        [XmlIgnore]
        public double TotalDMDemand { get; set; }
        ///<Summary>Superfloruis docummentation added to get solution compilling</Summary>
        [Units("g/m2")]
        [XmlIgnore]
        public double TotalDMAllocated { get; set; }
        ///<Summary>The amount of N taken up after arbitration</Summary>
        [Units("g/m2")]
        [XmlIgnore]
        public double NTakenUp { get; set; }

        /// <summary>Plant depth.</summary>
        [XmlIgnore]
        public double Depth
        {
            get
            {
                if (plantZone == null)
                    return 0;
                else
                    return plantZone.Depth;
            }
        }
        /// <summary>Layer mid point depth.</summary>
        [XmlIgnore]
        public double LayerMidPointDepth
        {
            get
            {
                if (plantZone == null)
                    return 0;
                else
                    return plantZone.LayerMidPointDepth;
            }
        }
        /// <summary>Layer live</summary>
        [XmlIgnore]
        public Biomass[] LayerLive
        {
            get
            {
                if (plantZone == null)
                    return null;
                else
                    return plantZone.LayerLive;
            }
        }
        /// <summary>Layer dead.</summary>
        [XmlIgnore]
        public Biomass[] LayerDead
        {
            get
            {
                if (plantZone == null)
                    return null;
                else
                    return plantZone.LayerDead;
            }
        }

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

            plantZone = new ZoneState(soil, Plant.Name, 0, InitialDM.Value, Plant.Population, MaxNconc);

        }

        /// <summary>Called when crop is sown</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantSowing")]
        private void OnPlantSowing(object sender, SowPlant2Type data)
        {
            if (data.Plant == Plant)
            {
                Soil soil = Apsim.Find(this, typeof(Soil)) as Soil;
                if (soil == null)
                    throw new Exception("Cannot find soil");

                plantZone = new ZoneState(soil, Plant.Name, Plant.SowingData.Depth, InitialDM.Value, Plant.Population, MaxNconc);
                InitialiseZones();
            }
        }

        /// <summary>Initialise all zones.</summary>
        private void InitialiseZones()
        {
            zones = new List<ZoneState>();
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
                    ZoneState newZone = new ZoneState(soil, Plant.Name, ZoneRootDepths[i], ZoneInitialDM[i], Plant.Population, MaxNconc);
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
            {
                plantZone.mySenescenceRate = 0;
                if (SenescenceRate != null) //Default of zero means no senescence
                    plantZone.mySenescenceRate = SenescenceRate.Value;

                /*  if (Live.Wt == 0)
                  {
                      //determine how many layers to put initial DM into.
                      Depth = Plant.SowingData.Depth;
                      double AccumulatedDepth = 0;
                      double InitialLayers = 0;
                      for (int layer = 0; layer < Soil.Thickness.Length; layer++)
                      {
                          if (AccumulatedDepth < Depth)
                              InitialLayers += 1;
                          AccumulatedDepth += Soil.SoilWater.Thickness[layer];
                      }
                      for (int layer = 0; layer < Soil.Thickness.Length; layer++)
                      {
                          if (layer <= InitialLayers - 1)
                          {
                              //dirstibute root biomass evently through root depth
                              LayerLive[layer].StructuralWt = InitialDM / InitialLayers * Plant.Population;
                              LayerLive[layer].StructuralN = InitialDM / InitialLayers * MaxNconc * Plant.Population;
                          }
                      }
               
                  }
                  */
                plantZone.Length = 0;
                for (int layer = 0; layer < plantZone.soil.Thickness.Length; layer++)
                    plantZone.Length += LengthDensity[layer];
            }
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
                double TEM = (TemperatureEffect == null) ? 1 : TemperatureEffect.Value;

                plantZone.Depth = plantZone.Depth + RootFrontVelocity.Value * plantZone.soilCrop.XF[RootLayer] * TEM;

                //Limit root depth for impeded layers
                double MaxDepth = 0;
                for (int i = 0; i < plantZone.soil.Thickness.Length; i++)
                    if (plantZone.soilCrop.XF[i] > 0)
                        MaxDepth += plantZone.soil.Thickness[i];
                //Limit root depth for the crop specific maximum depth
                if (MaximumRootDepth != null)
                    MaxDepth = Math.Min(MaximumRootDepth.Value, MaxDepth);

                plantZone.Depth = Math.Min(plantZone.Depth, MaxDepth);

                // Do Root Senescence
                if (plantZone.mySenescenceRate > 0.0)
                    DoRootBiomassRemoval(plantZone.mySenescenceRate);
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
        /// <param name="NO3NAmount">The NO3NAmount.</param>
        /// <param name="NH4NAmount">The NH4NAmount.</param>
        /// <param name="zoneName">zone name</param>
        public override void DoNitrogenUptake(double[] NO3NAmount, double[] NH4NAmount, string zoneName)
        {
            ZoneState zone = zones.Find(z => z.Name == zoneName);
            if (zone == null)
                throw new Exception("Cannot find a zone called " + zoneName);

            // Send the delta water back to SoilN that we're going to uptake.
            NitrogenChangedType NitrogenUptake = new NitrogenChangedType();
            NitrogenUptake.DeltaNO3 = MathUtilities.Multiply_Value(NO3NAmount, -1.0);
            NitrogenUptake.DeltaNH4 = MathUtilities.Multiply_Value(NH4NAmount, -1.0);

            zone.NitUptake = MathUtilities.Add(NitrogenUptake.DeltaNO3, NitrogenUptake.DeltaNH4);
            zone.soil.SoilNitrogen.SetNitrogenChanged(NitrogenUptake);
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
        
        /// <summary>Soils the n supply.</summary>
        /// <param name="NO3Supply">The n o3 supply.</param>
        /// <param name="NH4Supply">The n h4 supply.</param>
        private void SoilNSupply(double[] NO3Supply, double[] NH4Supply)
        {
            double[] no3ppm = new double[plantZone.soil.Thickness.Length];
            double[] nh4ppm = new double[plantZone.soil.Thickness.Length];

            for (int layer = 0; layer < plantZone.soil.Thickness.Length; layer++)
            {
                if (plantZone.LayerLive[layer].Wt > 0)
                {
                    double kno3 = KNO3.ValueForX(LengthDensity[layer]);
                    double knh4 = KNH4.ValueForX(LengthDensity[layer]);
                    double RWC = 0;
                    RWC = (plantZone.soil.Water[layer] - plantZone.soil.SoilWater.LL15mm[layer]) / (plantZone.soil.SoilWater.DULmm[layer] - plantZone.soil.SoilWater.LL15mm[layer]);
                    RWC = Math.Max(0.0, Math.Min(RWC, 1.0));
                    double SWAF = NUptakeSWFactor.ValueForX(RWC);
                    no3ppm[layer] = plantZone.soil.NO3N[layer] * (100.0 / (plantZone.soil.BD[layer] * plantZone.soil.Thickness[layer]));
                    NO3Supply[layer] = plantZone.soil.NO3N[layer] * kno3 * no3ppm[layer] * SWAF;
                    nh4ppm[layer] = plantZone.soil.NH4N[layer] * (100.0 / (plantZone.soil.BD[layer] * plantZone.soil.Thickness[layer]));
                    NH4Supply[layer] = plantZone.soil.NH4N[layer] * knh4 * nh4ppm[layer] * SWAF;
                }
                else
                {
                    NO3Supply[layer] = 0;
                    NH4Supply[layer] = 0;
                }
            }
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
        /// <value>The dm demand.</value>
        public override BiomassPoolType DMDemand
        {
            get
            {
                double Demand = 0;
                if ((isGrowing)&&(PartitionFraction != null))
                    Demand = Arbitrator.DMSupply * PartitionFraction.Value;
                TotalDMDemand = Demand;//  The is not really necessary as total demand is always not calculated on a layer basis so doesn't need summing.  However it may some day
                return new BiomassPoolType { Structural = Demand };
            }
        }

        /// <summary>Sets the dm potential allocation.</summary>
        /// <value>The dm potential allocation.</value>
        /// <exception cref="System.Exception">
        /// Invalid allocation of potential DM in + Name
        /// or
        /// Error trying to partition potential root biomass
        /// </exception>
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
        /// <value>The dm allocation.</value>
        /// <exception cref="System.Exception">Error trying to partition root biomass</exception>
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
        /// <value>The n demand.</value>
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
                int i = -1;
                foreach (Biomass Layer in plantZone.LayerLive)
                {
                    i += 1;
                    plantZone.StructuralNDemand[i] = Layer.PotentialDMAllocation * MinNconc *  _NitrogenDemandSwitch;
                    double NDeficit = Math.Max(0.0, MaxNconc * (Layer.Wt + Layer.PotentialDMAllocation) - (Layer.N + plantZone.StructuralNDemand[i]));
                    plantZone.NonStructuralNDemand[i] = Math.Max(0, NDeficit - plantZone.StructuralNDemand[i]) * _NitrogenDemandSwitch;
                }
                TotalNonStructuralNDemand = MathUtilities.Sum(plantZone.NonStructuralNDemand);
                TotalStructuralNDemand = MathUtilities.Sum(plantZone.StructuralNDemand);
                TotalNDemand = TotalNonStructuralNDemand + TotalStructuralNDemand;
                return new BiomassPoolType { Structural = TotalStructuralNDemand, NonStructural = TotalNonStructuralNDemand };
            }
        }

        /// <summary>Gets or sets the n supply.</summary>
        /// <value>The n supply.</value>
        public override BiomassSupplyType NSupply
        {
            get
            {
                return new BiomassSupplyType()
                {
                    Reallocation = 0.0,
                    Retranslocation = 0.0,
                    Uptake = AvailableNUptake()
                };
            }
        }

        /// <summary>Gets the N amount available for uptake</summary>
        /// <returns>N available to be taken up</returns>
        public double AvailableNUptake()
        {
            if (plantZone.soil.Thickness != null)
            {
                double[] no3supply = new double[plantZone.soil.Thickness.Length];
                double[] nh4supply = new double[plantZone.soil.Thickness.Length];
                SoilNSupply(no3supply, nh4supply);
                double NSupply = (Math.Min(MathUtilities.Sum(no3supply), MaxDailyNUptake.Value) +
                                  Math.Min(MathUtilities.Sum(nh4supply), MaxDailyNUptake.Value)) * kgha2gsm;
                return NSupply;
            }
            else
            { //Soil not initialised yet
                return 0.0;
            }
        }

        /// <summary>Gets the nitrogne supply from the specified zone.</summary>
        /// <param name="zone">The zone.</param>
        public override double[] NO3NSupply(ZoneWaterAndN zone)
        {
            ZoneState myZone = zones.Find(z => z.Name == zone.Name);
            if (myZone != null)
            {
                double[] NO3 = zone.NO3N;

                double[] NO3Supply = new double[myZone.soil.Thickness.Length];

                double[] no3ppm = new double[myZone.soil.Thickness.Length];

                double NO3uptake = 0;

                for (int layer = 0; layer < myZone.soil.Thickness.Length; layer++)
                {
                    if (myZone.LayerLive[layer].Wt > 0)
                    {
                        double RWC = 0;
                        RWC = (myZone.soil.Water[layer] - myZone.soil.SoilWater.LL15mm[layer]) / (myZone.soil.SoilWater.DULmm[layer] - myZone.soil.SoilWater.LL15mm[layer]);
                        RWC = Math.Max(0.0, Math.Min(RWC, 1.0));
                        double kno3 = KNO3.ValueForX(LengthDensity[layer]);
                        double SWAF = NUptakeSWFactor.ValueForX(RWC);
                        no3ppm[layer] = NO3[layer] * (100.0 / (myZone.soil.BD[layer] * myZone.soil.Thickness[layer]));
                        NO3Supply[layer] = Math.Min(NO3[layer] * kno3 * no3ppm[layer] * SWAF, (MaxDailyNUptake.Value - NO3uptake));
                        NO3uptake += NO3Supply[layer];
                    }
                    else
                    {
                        NO3Supply[layer] = 0;
                    }
                }

                return NO3Supply;
            }
            return null;
        }
        /// <summary>Gets the ammonium uptake supply for the given nitrogen state.</summary>
        /// <param name="zone">The zone</param>
        public override double[] NH4NSupply(ZoneWaterAndN zone)
        {
            ZoneState myZone = zones.Find(z => z.Name == zone.Name);
            if (myZone != null)
            {
                double[] NH4 = zone.NH4N;

                double[] NH4Supply = new double[myZone.soil.Thickness.Length];

                double[] NH4ppm = new double[myZone.soil.Thickness.Length];

                double NH4uptake = 0;

                for (int layer = 0; layer < myZone.soil.Thickness.Length; layer++)
                {
                    if (myZone.LayerLive[layer].Wt > 0)
                    {
                        double RWC = 0;
                        RWC = (myZone.soil.Water[layer] - myZone.soil.SoilWater.LL15mm[layer]) / (myZone.soil.SoilWater.DULmm[layer] - myZone.soil.SoilWater.LL15mm[layer]);
                        RWC = Math.Max(0.0, Math.Min(RWC, 1.0));
                        double knh4 = KNH4.ValueForX(LengthDensity[layer]);
                        double SWAF = NUptakeSWFactor.ValueForX(RWC);
                        NH4ppm[layer] = NH4Supply[layer] * (100.0 / (myZone.soil.BD[layer] * myZone.soil.Thickness[layer]));
                        NH4Supply[layer] = Math.Min(NH4[layer] * knh4 * NH4ppm[layer] * SWAF, (MaxDailyNUptake.Value - NH4uptake));
                        NH4uptake += NH4Supply[layer];
                    }
                    else
                    {
                        NH4Supply[layer] = 0;
                    }
                }

                return NH4Supply;
            }

            return null;
        }
        /// <summary>Sets the n allocation.</summary>
        /// <value>The n allocation.</value>
        /// <exception cref="System.Exception">
        /// Cannot Allocate N to roots in layers when demand is zero
        /// or
        /// Error in N Allocation:  + Name
        /// or
        /// Request for N uptake exceeds soil N supply
        /// </exception>
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
                int i = -1;
                foreach (Biomass Layer in plantZone.LayerLive)
                {
                    i += 1;
                    if (TotalStructuralNDemand > 0)
                    {
                        double StructFrac = plantZone.StructuralNDemand[i] / TotalStructuralNDemand;
                        Layer.StructuralN += value.Structural * StructFrac;
                        NAllocated += value.Structural * StructFrac;
                    }
                    if (TotalNonStructuralNDemand > 0)
                    {
                        double NonStructFrac = plantZone.NonStructuralNDemand[i] / TotalNonStructuralNDemand;
                        Layer.NonStructuralN += value.NonStructural * NonStructFrac;
                        NAllocated += value.NonStructural * NonStructFrac;
                    }
                }
                if (!MathUtilities.FloatsAreEqual(NAllocated - TotalNAllocated, 0.0))
                {
                    throw new Exception("Error in N Allocation: " + Name);
                }
            }
        }
        /// <summary>Gets or sets the maximum nconc.</summary>
        /// <value>The maximum nconc.  Has a default of 0.01</value>
        public override double MaxNconc
        {
            get
            {
                if (MaximumNConc != null)
                    return MaximumNConc.Value;
                else
                    return 0.01; 
            }
        }
        /// <summary>Gets or sets the minimum nconc.</summary>
        /// <value>The minimum nconc. Has a default of 0.01</value>
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
            if (myZone != null)
            {
                double[] SW = zone.Water;
                double[] supply = new double[myZone.soil.Thickness.Length];

                double depth_to_layer_bottom = 0;   // depth to bottom of layer (mm)
                double depth_to_layer_top = 0;      // depth to top of layer (mm)

                for (int layer = 0; layer < myZone.soil.Thickness.Length; layer++)
                {
                    depth_to_layer_bottom += myZone.soil.Thickness[layer];
                    depth_to_layer_top = depth_to_layer_bottom - myZone.soil.Thickness[layer];
                    myZone.LayerMidPointDepth = (depth_to_layer_bottom + depth_to_layer_top) / 2;

                    if (layer <= LayerIndex(myZone.Depth))
                        supply[layer] = Math.Max(0.0, myZone.soilCrop.KL[layer] * KLModifier.Value *
                            (SW[layer] - myZone.soilCrop.LL[layer] * myZone.soil.Thickness[layer]) * RootProportion(layer, myZone.Depth));
                    else
                        supply[layer] = 0;
                }

                return supply;
            }
            else
                return null;
        }

        /// <summary>Gets or sets the water uptake.</summary>
        /// <value>The water uptake.</value>
        [Units("mm")]
        public override double WaterUptake
        {
            get { return plantZone.Uptake == null ? 0.0 : -MathUtilities.Sum(plantZone.Uptake); }
        }
        
        /// <summary>Gets or sets the water uptake.</summary>
        /// <value>The water uptake.</value>
        [Units("kg/ha")]
        public override double NUptake
        {
            get {return plantZone.NitUptake == null ? 0.0 : -MathUtilities.Sum(plantZone.NitUptake);}
        }
        #endregion

        #region Biomass Removal
        /// <summary>Removes biomass from root layers when harvest, graze or cut events are called.</summary>
        public override void DoRemoveBiomass(OrganBiomassRemovalType value)
        {
            //NOTE: roots don't have dead biomass
            double totalFractionToRemove = value.FractionLiveToRemove + value.FractionLiveToResidue;
            if (totalFractionToRemove > 1.0)
            {
                throw new Exception("The sum of FractionToResidue and FractionToRemove sent with your "
                                    + "!!!!PLACE HOLDER FOR EVENT SENDER!!!!"
                                    + " is greater than 1.  Had this execption not triggered you would be removing more biomass from "
                                    + Name + " than there is to remove");
            }
            else if (totalFractionToRemove > 0.0)
            {
                DoRootBiomassRemoval(value.FractionLiveToResidue, value.FractionLiveToRemove);
            }
        }

        #endregion

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public override void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            // add a heading.
            Name = this.Name;
            tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

            tags.Add(new AutoDocumentation.Paragraph(Name + " is parameterised using the PMF Root class which provides the core functions of taking up water and nutrients from the soil.  It is parameterised as follows.", indent));

            // write memos.
            foreach (IModel memo in Apsim.Children(this, typeof(Memo)))
                memo.Document(tags, -1, indent);

            //Describe root growth
            tags.Add(new AutoDocumentation.Heading("Root Growth", headingLevel+1));
            tags.Add(new AutoDocumentation.Paragraph("Roots grow downward through the soil profile and rate is determined by:",indent));
            foreach (IModel child in Apsim.Children(this, typeof(IModel)))
            {
                if (child.Name == "RootFrontVelocity")
                    child.Document(tags, headingLevel + 5, indent + 1);
            }

            if (TemperatureEffect.GetType() == typeof(Constant))
            {
                //Temp having no effect so no need to document
            }
            else
            {
                tags.Add(new AutoDocumentation.Paragraph("The RootFrontVelocity described above is influenced by temperature as:", indent));
                foreach (IModel child in Apsim.Children(this, typeof(IModel)))
                {
                    if (child.Name == "TemperatureEffect")
                        child.Document(tags, headingLevel + 5, indent + 1);
                }
            }

            tags.Add(new AutoDocumentation.Paragraph("The RootFrontVelocity is also influenced by the extension resistance posed by the soil, paramterised using the soil XF value", indent));

            tags.Add(new AutoDocumentation.Heading("Drymatter Demands", headingLevel + 1));
            // Describe biomass Demand
            tags.Add(new AutoDocumentation.Paragraph("100% of the DM demanded from the root is structural", indent));

            tags.Add(new AutoDocumentation.Paragraph("The daily DM demand from root is calculated as a proportion of total DM supply using:", indent));
            foreach (IModel child in Apsim.Children(this, typeof(IModel)))
            {
                if (child.Name == "PartitionFraction")
                    child.Document(tags, headingLevel + 5, indent + 1);
            }
            
            tags.Add(new AutoDocumentation.Heading("Nitrogen Demands", headingLevel + 1));
            tags.Add(new AutoDocumentation.Paragraph("The daily structural N demand from " + this.Name + " is the product of Total DM demand and a Nitrogen concentration of " + MinNconc * 100 + "%", indent));
            if (NitrogenDemandSwitch != null)
            {
                tags.Add(new AutoDocumentation.Paragraph("The Nitrogen demand swith is a multiplier applied to nitrogen demand so it can be turned off at certain phases.  For the " + Name + " Organ it is set as:", indent));
                foreach (IModel child in Apsim.Children(this, typeof(IModel)))
                {
                    if (child.Name == "NitrogenDemandSwitch")
                        child.Document(tags, headingLevel + 5, indent);
                }
            }

            tags.Add(new AutoDocumentation.Heading("Nitrogen Uptake", headingLevel + 1));
            tags.Add(new AutoDocumentation.Paragraph("potential N uptake by the root system is calculated for each soil layer that the roots have extended into.", indent));
            tags.Add(new AutoDocumentation.Paragraph("In each layer potential uptake is calculated as the product of the mineral nitrogen in the layer, a factor controllint the rate of extraction (kNO<sub>3</sub> and kNH<sub>4</sub>), the concentration of of N (ppm) and a soil moisture factor which decreases as the soil dries.", indent));
            tags.Add(new AutoDocumentation.Paragraph("Nitrogen uptake demand is limited to the maximum of potential uptake and the plants N demand.  Uptake N demand is then passed to the soil arbitrator which determines how much of their Nitrogen uptake demand each plant instance will be allowed to take up:", indent));

            tags.Add(new AutoDocumentation.Heading("Water Uptake", headingLevel + 1));
            tags.Add(new AutoDocumentation.Paragraph("Potential water uptake by the root system is calculated for each soil layer that the roots have extended into.", indent));
            tags.Add(new AutoDocumentation.Paragraph("In each layer potential uptake is calculated as the product of the available Water in the layer, and a factor controllint the rate of extraction (kl)", indent));
            tags.Add(new AutoDocumentation.Paragraph("The kl values are set in the soil and may be further modified by the crop.  are calculated in relation to root length density in each layer as :", indent));
            foreach (IModel child in Apsim.Children(this, typeof(IModel)))
                {
                    if (child.Name == "KLModifier" || child.Name == "KNO3" || child.Name == "KNH4")
                        child.Document(tags, headingLevel + 5, indent + 1);
                }
                
            // write Other functions.
            bool NonStandardFunctions = false;
            foreach (IModel child in Apsim.Children(this, typeof(IModel)))
            {
                if  ((child.GetType() != typeof(Memo))
                    | (child.GetType() != typeof(Biomass))
                    | (child.Name != "MaximumNConc")
                    | (child.Name != "MinimumNConc")
                    | (child.Name != "NitrogenDemandSwitch")
                    | (child.Name != "KLModifier")
                    | (child.Name != "SoilWaterEffect")
                    | (child.Name != "MaximumDailyUptake")
                    | (child.Name != "SenescenceRate") | (child.Name != "MaximumNConc")
                    | (child.Name != "TemperatureEffect")
                    | (child.Name != "MaximumRootDepth")
                    | (child.Name != "KLModifier")
                    | (child.Name != "RootFrontVelocity")
                    | (child.Name != "PartitionFraction"))
                    {
                        NonStandardFunctions = true;
                    }
            }

            if (NonStandardFunctions)
            {
                tags.Add(new AutoDocumentation.Heading("Other functionality", headingLevel + 1));
                tags.Add(new AutoDocumentation.Paragraph("In addition to the core functionality and parameterisation described above, the " + this.Name + " organ has additional functions used to provide paramters for core functions and create additional functionality", indent));
                foreach (IModel child in Apsim.Children(this, typeof(IModel)))
                {
                    if ((child.GetType() == typeof(Memo))
                    | (child is Biomass)
                    | (child.Name != "MaximumNConc")
                    | (child.Name != "MinimumNConc")
                    | (child.Name != "NitrogenDemandSwitch")
                    | (child.Name != "KLModifier")
                    | (child.Name != "SoilWaterEffect")
                    | (child.Name != "MaximumDailyUptake")
                    | (child.Name != "SenescenceRate") | (child.Name != "MaximumNConc")
                    | (child.Name != "TemperatureEffect")
                    | (child.Name != "MaximumRootDepth")
                    | (child.Name != "KLModifier")
                    | (child.Name != "RootFrontVelocity")
                    | (child.Name != "PartitionFraction"))
                    {//Already documented 
                    }
                    else
                    {
                        //tags.Add(new AutoDocumentation.Heading(child.Name, headingLevel + 2));
                        child.Document(tags, headingLevel + 2, indent + 1);
                    }
                }
            }
        }
    }
}
