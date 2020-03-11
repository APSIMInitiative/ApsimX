using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Models.PMF.Interfaces;
using Models.Soils.Arbitrator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Models.PMF.Organs;
using Models.PMF.Phen;
using Newtonsoft.Json;

namespace Models.PMF
{
    ///<summary>
    /// The Arbitrator class determines the allocation of dry matter (DM) and Nitrogen between each of the organs in the crop model. Each organ can have up to three different pools of biomass:
    /// 
    /// * **Structural biomass** which is essential for growth and remains within the organ once it is allocated there.
    /// * **Metabolic biomass** which generally remains within an organ but is able to be re-allocated when the organ senesces and may be retranslocated when demand is high relative to supply.
    /// * **Storage biomass** which is partitioned to organs when supply is high relative to demand and is available for retranslocation to other organs whenever supply from uptake, fixation, or re-allocation is lower than demand.
    /// 
    /// The process followed for biomass arbitration is shown in Figure [FigureNumber]. Arbitration calculations are triggered by a series of events (shown below) that are raised every day.  For these calculations, at each step the Arbitrator exchange information with each organ, so the basic computations of demand and supply are done at the organ level, using their specific parameters. 
    /// 
    /// 1. **doPotentialPlantGrowth**.  When this event occurs, each organ class executes code to determine their potential growth, biomass supplies and demands.  In addition to demands for structural, non-structural and metabolic biomass (DM and N) each organ may have the following biomass supplies: 
    /// 	* **Fixation supply**.  From photosynthesis (DM) or symbiotic fixation (N)
    /// 	* **Uptake supply**.  Typically uptake of N from the soil by the roots but could also be uptake by other organs (eg foliage application of N).
    /// 	* **Retranslocation supply**.  Storage biomass that may be moved from organs to meet demands of other organs.
    /// 	* **Reallocation supply**. Biomass that can be moved from senescing organs to meet the demands of other organs.
    /// 2. **doPotentialPlantPartitioning.** On this event the Arbitrator first executes the DoDMSetup() method to gather the DM supplies and demands from each organ, these values are computed at the organ level.  It then executes the DoPotentialDMAllocation() method which works out how much biomass each organ would be allocated assuming N supply is not limiting and sends these allocations to the organs.  Each organ then uses their potential DM allocation to determine their N demand (how much N is needed to produce that much DM) and the arbitrator calls DoNSetup() to gather the N supplies and demands from each organ and begin N arbitration.  Firstly DoNReallocation() is called to redistribute N that the plant has available from senescing organs.  After this step any unmet N demand is considered as plant demand for N uptake from the soil (N Uptake Demand).
    /// 3. **doNutrientArbitration.** When this event occurs, the soil arbitrator gets the N uptake demands from each plant (where multiple plants are growing in competition) and their potential uptake from the soil and determines how much of their demand that the soil is able to provide.  This value is then passed back to each plant instance as their Nuptake and doNUptakeAllocation() is called to distribute this N between organs.  
    /// 4. **doActualPlantPartitioning.**  On this event the arbitrator call DoNRetranslocation() and DoNFixation() to satisfy any unmet N demands from these sources.  Finally, DoActualDMAllocation is called where DM allocations to each organ are reduced if the N allocation is insufficient to achieve the organs minimum N concentration and final allocations are sent to organs. 
    /// 
    /// ![Alt Text](ArbitratorSequenceDiagram.PNG)
    /// 
    /// **Figure [FigureNumber]:**  Schematic showing the procedure for arbitration of biomass partitioning.  Pink boxes represent events that occur every day and their numbering shows the order of calculations. Blue boxes represent the methods that are called when these events occur.  Orange boxes contain properties that make up the organ/arbitrator interface.  Green boxes are organ specific properties.
    /// </summary>

    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Plant))]
    public class SorghumArbitrator: Model, IUptake, IArbitrator, ICustomDocumentation
    {
        ///1. Links
        ///------------------------------------------------------------------------------------------------

        /// <summary>The top level plant object in the Plant Modelling Framework</summary>
        [Link]
        private Plant plant = null;

        /// <summary>The method used to arbitrate N allocations</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IArbitrationMethod nArbitrator = null;

        /// <summary>The method used to arbitrate N allocations</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IArbitrationMethod dmArbitrator = null;

        [Link]
        private Root root = null;

        [Link]
        private SorghumLeaf leaf = null;

        [Link]
        private Phenology phenology = null;

        /// <summary>The soil</summary>
        [Link]
        public Soils.Soil Soil = null;
        ///2. Private And Protected Fields
        /// -------------------------------------------------------------------------------------------------
        //private List<IModel> uptakeModels = null;
        //private List<IModel> zones = null;
        //private double stage;
        //private IPhase previousPhase;
        private double accumTT;

        /// <summary>The kgha2gsm</summary>
        protected const double kgha2gsm = 0.1;

        /// <summary>The list of organs</summary>
        protected List<IArbitration> Organs = new List<IArbitration>();

        /// <summary>A list of organs or suborgans that have watardemands</summary>
        protected List<IHasWaterDemand> WaterDemands = new List<IHasWaterDemand>();

        ///4. Public Events And Enums
        /// -------------------------------------------------------------------------------------------------

        /// <summary>Occurs when a plant is about to be sown.</summary>
        public event EventHandler SetDMSupply;
        /// <summary>Occurs when a plant is about to be sown.</summary>
        public event EventHandler SetNSupply;
        /// <summary>Occurs when a plant is about to be sown.</summary>
        public event EventHandler SetDMDemand;
        /// <summary>Occurs when a plant is about to be sown.</summary>
        public event EventHandler SetNDemand;


        ///5. Public Properties
        /// --------------------------------------------------------------------------------------------------

        /// <summary>The variables for DM</summary>
        [XmlIgnore]
        public BiomassArbitrationType DM { get; private set; }

        /// <summary>The variables for N</summary>
        [XmlIgnore]
        public BiomassArbitrationType N { get; private set; }

        //// <summary>Gets the dry mass supply relative to dry mass demand.</summary>
        /// <value>The dry mass supply.</value>
        [XmlIgnore]
        public double FDM { get { return DM == null ? 0 : MathUtilities.Divide(DM.TotalPlantSupply, DM.TotalPlantDemand, 0); } }

        /// <summary>Gets the dry mass supply relative to dry structural demand plus metabolic demand.</summary>
        /// <value>The dry mass supply.</value>
        [XmlIgnore]
        public double StructuralCarbonSupplyDemand { get { return DM == null ? 0 : MathUtilities.Divide(DM.TotalPlantSupply, (DM.TotalStructuralDemand + DM.TotalMetabolicDemand), 0); } }

        /// <summary>Gets the delta wt.</summary>
        /// <value>The delta wt.</value>
        public double DeltaWt { get { return DM == null ? 0 : (DM.End - DM.Start); } }

        /// <summary>Gets the n supply relative to N demand.</summary>
        /// <value>The n supply.</value>
        [XmlIgnore]
        public double FN { get { return N == null ? 0 : MathUtilities.Divide(N.TotalPlantSupply, N.TotalPlantDemand, 0); } }

        /// <summary>Gets the water demand.</summary>
        /// <value>The water demand.</value>
        [XmlIgnore]
        public double WDemand { get; protected set; }

        /// <summary>Gets the water Supply.</summary>
        /// <value>The water supply.</value>
        [XmlIgnore]
        public double WSupply { get; protected set; }

        /// <summary>Gets the water allocated in the plant (taken up).</summary>
        /// <value>The water uptake.</value>
        [XmlIgnore]
        public double WAllocated { get; protected set; }


        /// <summary>Gets or sets MassFlow during NitrogenUptake Calcs</summary>
        [XmlIgnore]
        public double[] MassFlow { get; private set; }

        /// <summary>Gets or sets Diffusion during NitrogenUptake Calcs</summary>
        [XmlIgnore]
        public double[] Diffusion { get; private set; }

        /// <summary>
        /// Today's dltTT.
        /// </summary>
        public double DltTT { get; set; }

        /// <summary>Gets the water Supply.</summary>
        /// <value>The water supply.</value>
        public double WatSupply { get; set; }

        /// <summary>Gets the water demand.</summary>
        /// <value>The water demand.</value>
        public double NMassFlowSupply { get; private set; }

        /// <summary>Gets the water demand.</summary>
        /// <value>The water demand.</value>
        public double NDiffusionSupply { get; private set; }
        
        /// <summary>
        /// Total TTFM accumulated from flowering.
        /// </summary>
        [JsonIgnore]
        public double TTFMFromFlowering { get; private set; }

        /// <summary>
        /// Used in stem DM demand function. Need to reconsider how this works.
        /// </summary>
        [JsonIgnore]
        public double DMPlantMax { get; set; }

        ///TotalAvailable divided by TotalPotential - used to lookup PhenologyStress table
        public double SWAvailRatio { get; set; }

        ///TotalSupply divided by WaterDemand - used to lookup ExpansionStress table - when calculating Actual LeafArea and calcStressedLeafArea
        public double SDRatio { get; set; }

        /////Same as SDRatio?? used to calculate Photosynthesis stress in calculating yield (Grain)
        //public double PhotoStress { get; set; }

        ///// <summary>Available SW by layer.</summary>
        //public double[] Avail { get; private set; }

        ///// <summary>Pot. Available SW by layer.</summary>
        //public double[] PotAvail { get; private set; }

        //  /// <summary>Total available SW.</summary>
        //public double TotalAvail { get; private set; }

        // // <summary>Total potential available SW.</summary>
        //public double TotalPotAvail { get; private set; }

        ///6. Public methods
        /// -----------------------------------------------------------------------------------------------------------

        /// <summary>Things the plant model does when the simulation starts</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        virtual protected void OnSimulationCommencing(object sender, EventArgs e) { Clear(); }

        /// <summary>Called at the start of the simulation.</summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">Dummy event data.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            //stage = phenology.Stage;
            //uptakeModels = Apsim.ChildrenRecursively(Parent, typeof(IUptake));
            //zones = Apsim.ChildrenRecursively(this.Parent, typeof(Zone));
            //previousPhase = phenology.CurrentPhase;
            DMPlantMax = 9999;
        }

        /// <summary>Called when crop is sowing</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantSowing")]
        virtual protected void OnPlantSowing(object sender, SowPlant2Type data)
        {
            List<IArbitration> organsToArbitrate = new List<IArbitration>();
            List<IHasWaterDemand> Waterdemands = new List<IHasWaterDemand>();

            foreach (Model Can in Apsim.FindAll(plant, typeof(IHasWaterDemand)))
                Waterdemands.Add(Can as IHasWaterDemand);

            foreach (IOrgan organ in plant.Organs)
                if (organ is IArbitration)
                    organsToArbitrate.Add(organ as IArbitration);


            Organs = organsToArbitrate;
            WaterDemands = Waterdemands;
            DM = new BiomassArbitrationType("DM", Organs);
            N = new BiomassArbitrationType("N", Organs);
        }

        //[EventSubscribe("StartOfDay")]
        //private void OnStartOfDay(object sender, EventArgs e)
        //{
        //    WAllocated = 0;
        //}

        [EventSubscribe("DoPhenology")]
        private void OnEndOfDay(object sender, EventArgs e)
        {
            DltTT = (double)Apsim.Get(this, "[Phenology].DltTT.Value()");
            accumTT += DltTT;
        }

        [EventSubscribe("PostPhenology")]
        private void PostPhenology(object sender, EventArgs e)
        {
            if (DMPlantMax > 9990)
            {
                double ttNow = accumTT;
                double ttToFlowering = (double)Apsim.Get(this, "[Phenology].TTToFlowering.Value()");
                double dmPlantMaxTT = (double)Apsim.Get(this, "[Grain].PgrT1.Value()");
                if (ttNow > dmPlantMaxTT + ttToFlowering)
                    DMPlantMax = (double)Apsim.Get(this, "[Stem].Live.Wt");
            }
        }

        [EventSubscribe("DoPotentialPlantGrowth")]
        private void DoPotentialPlantGrowth(object sender, EventArgs e)
        {
            for (int i = 0; i < Organs.Count; i++)
                N.UptakeSupply[i] = 0;
            UpdateTTElapsed();
        }

        /// Local methods for setting up supplies and demands
        /// <summary>Accumulate all of the Organ DM Supplies </summary>
        public virtual void DMSupplies()
        {
            // Setup DM supplies from each organ
            SetDMSupply?.Invoke(this, new EventArgs());
            BiomassSupplyType[] supplies = Organs.Select(organ => organ.DMSupply).ToArray();

            double totalWt = Organs.Sum(o => o.Total.Wt);
            DM.GetSupplies(supplies, totalWt);

        }

        /// <summary>Calculate all of the Organ DM Demands </summary>
        public virtual void DMDemands()
        {
            // Setup DM demands for each organ  
            SetDMDemand?.Invoke(this, new EventArgs());
            BiomassPoolType[] demands = Organs.Select(organ => organ.DMDemand).ToArray();
            DM.GetDemands(demands);
        }

        /// <summary>Calculate all of the Organ N Supplies </summary>
        public virtual void NSupplies()
        {
            // Setup N supplies from each organ
            SetNSupply?.Invoke(this, new EventArgs());
            BiomassSupplyType[] supplies = Organs.Select(organ => organ.NSupply).ToArray();
            double totalN = Organs.Sum(o => o.Total.N);
            N.GetSupplies(supplies, totalN);

        }

        /// <summary>Calculate all of the Organ N Demands </summary>
        public virtual void NDemands()
        {
            // Setup N demands
            SetNDemand?.Invoke(this, new EventArgs());
            BiomassPoolType[] demands = Organs.Select(organ => organ.NDemand).ToArray();
            N.GetDemands(demands);

        }

        /// <summary>Clears this instance.</summary>
        protected void Clear()
        {
            DM = new BiomassArbitrationType("DM", Organs);
            N = new BiomassArbitrationType("N", Organs);

            DltTT = 0.0;
            WatSupply = 0.0;
            NMassFlowSupply = 0.0;
            NDiffusionSupply = 0.0;
            TTFMFromFlowering = 0.0;
            
            SWAvailRatio = 0.0;
            SDRatio = 0.0;
            //PhotoStress = 0.0;
        }

        /// <summary>
        /// Calculate the potential sw uptake for today
        /// </summary>
        public List<ZoneWaterAndN> GetWaterUptakeEstimates(SoilState soilstate)
        {
            if (plant.IsAlive)
            {
                // Get all water supplies.
                double waterSupply = 0;  //NOTE: This is in L, not mm, to arbitrate water demands for spatial simulations.

                List<double[]> supplies = new List<double[]>();
                List<ZoneWaterAndN> zones = new List<ZoneWaterAndN>();
                foreach (ZoneWaterAndN zone in soilstate.Zones)
                    foreach (IOrgan o in Organs)
                        if (o is IWaterNitrogenUptake)
                        {
                            double[] organSupply = (o as IWaterNitrogenUptake).CalculateWaterSupply(zone);
                            if (organSupply != null)
                            {
                                supplies.Add(organSupply);
                                zones.Add(zone);
                                waterSupply += MathUtilities.Sum(organSupply) * zone.Zone.Area;
                            }
                        }

                // Calculate total water demand.
                double waterDemand = 0; //NOTE: This is in L, not mm, to arbitrate water demands for spatial simulations.

                foreach (IHasWaterDemand WD in WaterDemands)
                    waterDemand += WD.CalculateWaterDemand() * plant.Zone.Area;

                // Calculate demand / supply ratio.
                double fractionUsed = 0;
                if (waterSupply > 0)
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

        /// <summary>
        /// Set the sw uptake for today
        /// </summary>
        public void SetActualWaterUptake(List<ZoneWaterAndN> zones)
        {

            // Calculate the total water supply across all zones.
            double waterSupply = 0;   //NOTE: This is in L, not mm, to arbitrate water demands for spatial simulations.
            foreach (ZoneWaterAndN Z in zones)
            {
                // Z.Water calculated as Supply * fraction used
                waterSupply += MathUtilities.Sum(Z.Water) * Z.Zone.Area;
            }
            WSupply = waterSupply;
            // Calculate total plant water demand.
            WDemand = 0.0; //NOTE: This is in L, not mm, to arbitrate water demands for spatial simulations.
            foreach (IArbitration o in Organs)
                if (o is IHasWaterDemand)
                    WDemand += (o as IHasWaterDemand).CalculateWaterDemand() * plant.Zone.Area;

            // Calculate the fraction of water demand that has been given to us.
            double fraction = 1;
            if (MathUtilities.IsPositive(WDemand))
                fraction = Math.Min(1.0, waterSupply / WDemand);

            // Proportionally allocate supply across organs.
            WAllocated = 0.0;

            foreach (IHasWaterDemand WD in WaterDemands)
            {
                double demand = WD.CalculateWaterDemand();
                double allocation = fraction * demand;
                WD.WaterAllocation = allocation;
                WAllocated += allocation;
            }

            // Give the water uptake for each zone to Root so that it can perform the uptake
            // i.e. Root will do pass the uptake to the soil water balance.
            foreach (ZoneWaterAndN zone in zones)
            {
                plant.Root.DoWaterUptake(zone.Water, zone.Zone.Name);
                StoreWaterVariablesForNitrogenUptake(zone);
            }
        }

        private void StoreWaterVariablesForNitrogenUptake(ZoneWaterAndN zoneWater)
        {
            ZoneState myZone = root.Zones.Find(z => z.Name == zoneWater.Zone.Name);
            if (myZone != null)
            {
                //store Water variables for N Uptake calculation
                //Old sorghum doesn't do actualUptake of Water until end of day
                myZone.StartWater = new double[myZone.soil.Thickness.Length];
                myZone.AvailableSW = new double[myZone.soil.Thickness.Length];
                myZone.PotentialAvailableSW = new double[myZone.soil.Thickness.Length];
                myZone.Supply = new double[myZone.soil.Thickness.Length];

                var soilCrop = Soil.Crop(plant.Name);
                double[] kl = soilCrop.KL;

                double[] llDep = MathUtilities.Multiply(soilCrop.LL, myZone.soil.Thickness);

                if (root.Depth != myZone.Depth)
                    myZone.Depth += 0; // wtf??

                var currentLayer = myZone.soil.LayerIndexOfDepth(myZone.Depth);
                var currentLayerProportion = myZone.soil.ProportionThroughLayer(currentLayer, myZone.Depth);
                for (int layer = 0; layer <= currentLayer; ++layer)
                {
                    myZone.StartWater[layer] = myZone.soil.Water[layer];

                    myZone.AvailableSW[layer] = Math.Max(myZone.soil.Water[layer] - llDep[layer], 0);
                    myZone.PotentialAvailableSW[layer] = myZone.soil.DULmm[layer] - llDep[layer];

                    if (layer == currentLayer)
                    {
                        myZone.AvailableSW[layer] *= currentLayerProportion;
                        myZone.PotentialAvailableSW[layer] *= currentLayerProportion;
                    }

                    var proportion = root.rootProportionInLayer(layer, myZone);
                    myZone.Supply[layer] = Math.Max(myZone.AvailableSW[layer] * kl[layer] * proportion, 0.0);
                }
                var totalAvail = myZone.AvailableSW.Sum();
                var totalAvailPot = myZone.PotentialAvailableSW.Sum();
                var totalSupply = myZone.Supply.Sum();
                WatSupply = totalSupply; 

                // Set reporting variables.
                //Avail = myZone.AvailableSW;
                //PotAvail = myZone.PotentialAvailableSW;

                //used for SWDef PhenologyStress table lookup
                SWAvailRatio = MathUtilities.Bound(MathUtilities.Divide(totalAvail, totalAvailPot, 1.0),0.0,10.0);

                //used for SWDef ExpansionStress table lookup
                SDRatio = MathUtilities.Bound(MathUtilities.Divide(totalSupply, WDemand, 1.0), 0.0, 10);

                //used for SwDefPhoto Stress
                //PhotoStress = MathUtilities.Bound(MathUtilities.Divide(totalSupply, WDemand, 1.0), 0.0, 1.0);
            }
        }
               
        /// <summary>
        /// Calculate the potential N uptake for today. Should return null if crop is not in the ground (this is not true for old sorghum).
        /// </summary>
        public virtual List<Soils.Arbitrator.ZoneWaterAndN> GetNitrogenUptakeEstimates(SoilState soilstate)
        {
            if (plant.IsEmerged)
            {
                var nSupply = 0.0;//NOTE: This is in kg, not kg/ha, to arbitrate N demands for spatial simulations.

                //this function is called 4 times as part of estimates
                //shouldn't set public variables in here

                var grainIndex = 0;
                var rootIndex = 1;
                var leafIndex = 2;
                var stemIndex = 4;

                var rootDemand = N.StructuralDemand[rootIndex] + N.MetabolicDemand[rootIndex];
                var stemDemand = /*N.StructuralDemand[stemIndex] + */N.MetabolicDemand[stemIndex];
                var leafDemand = N.MetabolicDemand[leafIndex];
                var grainDemand = N.StructuralDemand[grainIndex] + N.MetabolicDemand[grainIndex];
                //have to correct the leaf demand calculation
                var leaf = Organs[leafIndex] as SorghumLeaf;
                var leafAdjustment = leaf.calculateClassicDemandDelta();
                
                //double NDemand = (N.TotalPlantDemand - N.TotalReallocation) / kgha2gsm * Plant.Zone.Area; //NOTE: This is in kg, not kg/ha, to arbitrate N demands for spatial simulations.
                //old sorghum uses g/m^2 - need to convert after it is used to calculate actual diffusion
                // leaf adjustment is not needed here because it is an adjustment for structural demand - we only look at metabolic here.

                // dh - In old sorghum, root only has one type of NDemand - it doesn't have a structural/metabolic division.
                // In new apsim, root only uses structural, metabolic is always 0. Therefore, we have to include root's structural
                // NDemand in this calculation.

                // dh - In old sorghum, totalDemand is metabolic demand for all organs. However in new apsim, grain has no metabolic
                // demand, so we must include its structural demand in this calculation.
                double totalDemand = N.TotalMetabolicDemand + N.StructuralDemand[rootIndex] + N.StructuralDemand[grainIndex];
                double nDemand = Math.Max(0, totalDemand - grainDemand); // to replicate calcNDemand in old sorghum 
                List<ZoneWaterAndN> zones = new List<ZoneWaterAndN>();

                foreach (ZoneWaterAndN zone in soilstate.Zones)
                {
                    ZoneWaterAndN UptakeDemands = new ZoneWaterAndN(zone.Zone);

                    UptakeDemands.NO3N = new double[zone.NO3N.Length];
                    UptakeDemands.NH4N = new double[zone.NH4N.Length];
                    UptakeDemands.PlantAvailableNO3N = new double[zone.NO3N.Length];
                    UptakeDemands.PlantAvailableNH4N = new double[zone.NO3N.Length];
                    UptakeDemands.Water = new double[UptakeDemands.NO3N.Length];

                    //only using Root to get Nitrogen from - temporary code for sorghum
                    var root = Organs[rootIndex] as Root;

                    //Get Nuptake supply from each organ and set the PotentialUptake parameters that are passed to the soil arbitrator
                    
                    //at present these 2arrays arenot being used within the CalculateNitrogenSupply function
                    //sorghum uses Diffusion & Massflow variables currently
                    double[] organNO3Supply = new double[zone.NO3N.Length]; //kg/ha - dltNo3 in old apsim
                    double[] organNH4Supply = new double[zone.NH4N.Length];

                    ZoneState myZone = root.Zones.Find(z => z.Name == zone.Zone.Name);
                    if (myZone != null)
                    {
                        CalculateNitrogenSupply(myZone, zone);

                        //new code
                        double[] diffnAvailable = new double[myZone.Diffusion.Length];
                        for (var i = 0; i < myZone.Diffusion.Length; ++i)
                        {
                            diffnAvailable[i] = myZone.Diffusion[i] - myZone.MassFlow[i];
                        }
                        var totalMassFlow = MathUtilities.Sum(myZone.MassFlow); //g/m^2
                        var totalDiffusion = MathUtilities.Sum(diffnAvailable);//g/m^2

                        var potentialSupply = totalMassFlow + totalDiffusion;
                        var actualDiffusion = 0.0;
                        var actualMassFlow = DltTT > 0 ? totalMassFlow : 0.0;
                        var maxDiffusionConst = root.MaxDiffusion.Value();

                        double NUptakeCease = (Apsim.Find(this, "NUptakeCease") as Functions.IFunction).Value();
                        if (TTFMFromFlowering > NUptakeCease)
                            totalMassFlow = 0;
                        actualMassFlow = totalMassFlow;
                        
                        if (totalMassFlow < nDemand && TTFMFromFlowering < NUptakeCease) // fixme && ttElapsed < nUptakeCease
                        {
                            actualDiffusion = MathUtilities.Bound(nDemand - totalMassFlow, 0.0, totalDiffusion);
                            actualDiffusion = MathUtilities.Divide(actualDiffusion, maxDiffusionConst, 0.0);

                            var nsupplyFraction = root.NSupplyFraction.Value();
                            var maxRate = root.MaxNUptakeRate.Value();

                            var maxUptakeRateFrac = Math.Min(1.0, (potentialSupply / root.NSupplyFraction.Value())) * root.MaxNUptakeRate.Value();
                            var maxUptake = Math.Max(0, maxUptakeRateFrac * DltTT - actualMassFlow);
                            actualDiffusion = Math.Min(actualDiffusion, maxUptake);
                        }

                        NDiffusionSupply = actualDiffusion;
                        NMassFlowSupply = actualMassFlow;

                        //adjust diffusion values proportionally
                        //make sure organNO3Supply is in kg/ha
                        for (int layer = 0; layer < organNO3Supply.Length; layer++)
                        {
                            var massFlowLayerFraction = MathUtilities.Divide(myZone.MassFlow[layer], totalMassFlow, 0.0);
                            var diffusionLayerFraction = MathUtilities.Divide(diffnAvailable[layer], totalDiffusion, 0.0);
                            //organNH4Supply[layer] = massFlowLayerFraction * root.MassFlow[layer];
                            organNO3Supply[layer] = (massFlowLayerFraction * actualMassFlow +
                                diffusionLayerFraction * actualDiffusion) / kgha2gsm;  //convert to kg/ha
                        }
                    }
                    //originalcode
                    UptakeDemands.NO3N = MathUtilities.Add(UptakeDemands.NO3N, organNO3Supply); //Add uptake supply from each organ to the plants total to tell the Soil arbitrator
                    if (UptakeDemands.NO3N.Any(n => MathUtilities.IsNegative(n)))
                        throw new Exception("-ve no3 uptake demand");
                    UptakeDemands.NH4N = MathUtilities.Add(UptakeDemands.NH4N, organNH4Supply);

                    N.UptakeSupply[rootIndex] += MathUtilities.Sum(organNO3Supply) * kgha2gsm * zone.Zone.Area / plant.Zone.Area;  //g/m2
                    if (MathUtilities.IsNegative(N.UptakeSupply[rootIndex]))
                        throw new Exception($"-ve uptake supply for organ {(Organs[rootIndex] as IModel).Name}");
                    nSupply += MathUtilities.Sum(organNO3Supply) * zone.Zone.Area;
                    zones.Add(UptakeDemands);
                }

                return zones;
            }
            return null;
        }

        private void CalculateNitrogenSupply(ZoneState myZone, ZoneWaterAndN zone)
        {
            myZone.MassFlow = new double[myZone.soil.Thickness.Length];
            myZone.Diffusion = new double[myZone.soil.Thickness.Length];

            int currentLayer = myZone.soil.LayerIndexOfDepth(myZone.Depth);
            for (int layer = 0; layer <= currentLayer; layer++)
            {
                var swdep = myZone.StartWater[layer]; //mm
                var dltSwdep = myZone.WaterUptake[layer];
                
                //NO3N is in kg/ha - old sorghum used g/m^2
                var no3conc = MathUtilities.Divide(zone.NO3N[layer] * kgha2gsm, swdep, 0);
                var no3massFlow = no3conc * (-dltSwdep);
                myZone.MassFlow[layer] = Math.Min(no3massFlow, zone.NO3N[layer] * kgha2gsm);

                //diffusion
                var swAvailFrac = MathUtilities.Divide(myZone.AvailableSW[layer], myZone.PotentialAvailableSW[layer], 0);
                //old sorghum stores N03 in g/ms not kg/ha
                var no3Diffusion = MathUtilities.Bound(swAvailFrac, 0.0, 1.0) * (zone.NO3N[layer] * kgha2gsm);

                myZone.Diffusion[layer] = Math.Min(no3Diffusion, zone.NO3N[layer] * kgha2gsm);

                if (layer == currentLayer)
                {
                    var proportion = myZone.soil.ProportionThroughLayer(currentLayer, myZone.Depth);
                    myZone.Diffusion[layer] *= proportion;
                }

                //NH4Supply[layer] = no3massFlow;
                //onyl 2 fields passed in for returning data. 
                //actual uptake needs to distinguish between massflow and diffusion
                //sorghum calcs don't use nh4 - so using that temporarily
            }
        }

        /// <summary>
        /// Set the sw uptake for today
        /// </summary>
        public virtual void SetActualNitrogenUptakes(List<ZoneWaterAndN> zones)
        {
            if (plant.IsEmerged)
            {
                // Calculate the total no3 and nh4 across all zones.
                var nSupply = 0.0;//NOTE: This is in kg, not kg/ha, to arbitrate N demands for spatial simulations.

                //NMassFlowSupply = 0.0; //rewporting variables
                //NDiffusionSupply = 0.0;
                var supply = 0.0;
                foreach (ZoneWaterAndN Z in zones)
                {
                    supply += MathUtilities.Sum(Z.NO3N);
                    //NMassFlowSupply += MathUtilities.Sum(Z.NH4N);
                    nSupply += supply * Z.Zone.Area;

                    for(int i = 0; i < Z.NH4N.Length; ++i)
                        Z.NH4N[i] = 0;
                }

                //NDiffusionSupply = supply - NMassFlowSupply;

                //Reset actual uptakes to each organ based on uptake allocated by soil arbitrator and the organs proportion of potential uptake
                for (int i = 0; i < Organs.Count; i++)
                {
                    N.UptakeSupply[i] = nSupply / plant.Zone.Area * N.UptakeSupply[i] / N.TotalUptakeSupply * kgha2gsm;
                    if (MathUtilities.IsNegative(N.UptakeSupply[i]))
                        throw new Exception($"-ve uptake supply for organ {(Organs[i] as IModel).Name}");
                }

                //Allocate N that the SoilArbitrator has allocated the plant to each organ
                AllocateUptake(Organs.ToArray(), N, nArbitrator);
                plant.Root.DoNitrogenUptake(zones);
            }
        }
        
        private void UpdateTTElapsed()
        {
            // Can't do this at end of day because it will be too late.
            // Can't do this in DoPhenology because it will happen before daily
            // phenology development.
            int flowering = phenology.StartStagePhaseIndex("Flowering");
            int maturity = phenology.EndStagePhaseIndex("Maturity");
            if (phenology.Between(flowering, maturity))
            {
                double dltTT;
                if (phenology.CurrentPhase.Start == "Flowering" && phenology.CurrentPhase is GenericPhase)
                    dltTT = (phenology.CurrentPhase as GenericPhase).ProgressionForTimeStep;
                else
                    dltTT = ((double?)Apsim.Get(this, "[Phenology].DltTTFM.Value()") ?? (double)Apsim.Get(this, "[Phenology].ThermalTime.Value()"));
                TTFMFromFlowering += dltTT;
            }
        }

        /// <summary>Sends the potential dm allocations.</summary>
        /// <param name="Organs">The organs.</param>
        /// <exception cref="System.Exception">Mass Balance Error in Photosynthesis DM Allocation</exception>
        virtual public void SendPotentialDMAllocations(IArbitration[] Organs)
        {
            //  Allocate to meet Organs demands
            DM.Allocated = DM.TotalStructuralAllocation + DM.TotalMetabolicAllocation + DM.TotalStorageAllocation;

            // Then check it all adds up
            if (MathUtilities.IsGreaterThan(DM.Allocated, DM.TotalPlantSupply))
                throw new Exception("Potential DM allocation by " + this.Name + " exceeds DM supply.   Thats not really possible so something has gone a miss");
            if (MathUtilities.IsGreaterThan(DM.Allocated, DM.TotalPlantDemand))
                throw new Exception("Potential DM allocation by " + this.Name + " exceeds DM Demand.   Thats not really possible so something has gone a miss");

            // Send potential DM allocation to organs to set this variable for calculating N demand
            for (int i = 0; i < Organs.Length; i++)
                Organs[i].SetDryMatterPotentialAllocation(new BiomassPoolType
                {
                    Structural = DM.StructuralAllocation[i],  //Need to seperate metabolic and structural allocations
                    Metabolic = DM.MetabolicAllocation[i],  //This wont do anything currently
                    Storage = DM.StorageAllocation[i], //Nor will this do anything
                });
        }

        /// <summary>Does the re allocation.</summary>
        /// <param name="Organs">The organs.</param>
        /// <param name="BAT">The bat.</param>
        /// <param name="arbitrator">The arbitrator.</param>
        virtual public void Reallocation(IArbitration[] Organs, BiomassArbitrationType BAT, IArbitrationMethod arbitrator)
        {
            double BiomassReallocated = 0;
            if (BAT.TotalReallocationSupply > 0.00000000001)
            {
                arbitrator.DoAllocation(Organs, BAT.TotalReallocationSupply, ref BiomassReallocated, BAT);

                //Then calculate how much biomass is realloced from each supplying organ based on relative reallocation supply
                for (int i = 0; i < Organs.Length; i++)
                    if (BAT.ReallocationSupply[i] > 0)
                    {
                        double RelativeSupply = BAT.ReallocationSupply[i] / BAT.TotalReallocationSupply;
                        BAT.Reallocation[i] += BiomassReallocated * RelativeSupply;
                    }
                BAT.TotalReallocation = MathUtilities.Sum(BAT.Reallocation);
            }
        }

        /// <summary>Does the uptake.</summary>
        /// <param name="Organs">The organs.</param>
        /// <param name="BAT">The bat.</param>
        /// <param name="arbitrator">The option.</param>
        virtual public void AllocateUptake(IArbitration[] Organs, BiomassArbitrationType BAT, IArbitrationMethod arbitrator)
        {
            double BiomassTakenUp = 0;
            if (BAT.TotalUptakeSupply > 0.00000000001)
            {
                arbitrator.DoAllocation(Organs, BAT.TotalUptakeSupply, ref BiomassTakenUp, BAT);
                // Then calculate how much N is taken up by each supplying organ based on relative uptake supply
                for (int i = 0; i < Organs.Length; i++)
                    BAT.Uptake[i] += BiomassTakenUp * MathUtilities.Divide(BAT.UptakeSupply[i], BAT.TotalUptakeSupply, 0);
            }
        }

        /// <summary>Does the retranslocation.</summary>
        /// <param name="Organs">The organs.</param>
        /// <param name="BAT">The bat.</param>
        /// <param name="arbitrator">The option.</param>
        public void Retranslocation(IArbitration[] Organs, BiomassArbitrationType BAT, IArbitrationMethod arbitrator)
        {
            if (MathUtilities.IsPositive(BAT.TotalRetranslocationSupply))
            {
                var nArbitrator = arbitrator as SorghumArbitratorN;
                if (nArbitrator != null)
                {
                    nArbitrator.DoRetranslocation(Organs, BAT, DM);
                }
                else
                {
                    double BiomassRetranslocated = 0;
                    if (MathUtilities.IsPositive(BAT.TotalRetranslocationSupply))
                    {
                        var phenology = Apsim.Find(this, typeof(Phen.Phenology)) as Phen.Phenology;
                        if (phenology.Beyond("EndGrainFill"))
                            return;
                        arbitrator.DoAllocation(Organs, BAT.TotalRetranslocationSupply, ref BiomassRetranslocated, BAT);

                        int leafIndex = 2;
                        int stemIndex = 4;

                        double grainDifferential = BiomassRetranslocated;

                        if (grainDifferential > 0)
                        {
                            // Retranslocate from stem.
                            double stemWtAvail = BAT.RetranslocationSupply[stemIndex];
                            double stemRetrans = Math.Min(grainDifferential, stemWtAvail);
                            BAT.Retranslocation[stemIndex] += stemRetrans;
                            grainDifferential -= stemRetrans;

                            double leafWtAvail = BAT.RetranslocationSupply[leafIndex];
                            double leafRetrans = Math.Min(grainDifferential, leafWtAvail);
                            BAT.Retranslocation[leafIndex] += Math.Min(grainDifferential, leafWtAvail);
                            grainDifferential -= leafRetrans;
                        }
                    }
                }
            }
        }

        /// <summary>Does the water limited dm allocations.  Water constaints to growth are accounted for in the calculation of DM supply
        /// and does initial N calculations to work out how much N uptake is required to pass to SoilArbitrator</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoPotentialPlantPartioning")]
        protected void OnDoPotentialPlantPartioning(object sender, EventArgs e)
        {
            if (plant.IsEmerged)
            {
                DM.Clear();
                N.Clear();

                DMSupplies();
                DMDemands();

                Reallocation(Organs.ToArray(), DM, dmArbitrator);         // Allocate supply of reallocated DM to organs
                AllocateFixation(Organs.ToArray(), DM, dmArbitrator);             // Allocate supply of fixed DM (photosynthesis) to organs
                Retranslocation(Organs.ToArray(), DM, dmArbitrator);      // Allocate supply of retranslocated DM to organs
                SendPotentialDMAllocations(Organs.ToArray());               // Tell each organ what their potential growth is so organs can calculate their N demands

                leaf.UpdateArea();

                NSupplies();
                NDemands();

                Reallocation(Organs.ToArray(), N, nArbitrator);           // Allocate N available from reallocation to each organ
            }
        }

        /// <summary>Does the fixation.</summary>
        /// <param name="Organs">The organs.</param>
        /// <param name="BAT">The bat.</param>
        /// <param name="arbitrator">The option.</param>
        /// <exception cref="System.Exception">Crop is trying to Fix excessive amounts of BAT.  Check partitioning coefficients are giving realistic nodule size and that FixationRatePotential is realistic</exception>
        virtual public void AllocateFixation(IArbitration[] Organs, BiomassArbitrationType BAT, IArbitrationMethod arbitrator)
        {
            double BiomassFixed = 0;
            if (BAT.TotalFixationSupply > 0.00000000001)
            {
                arbitrator.DoAllocation(Organs, BAT.TotalFixationSupply, ref BiomassFixed, BAT);

                //Set the sink limitation variable.  BAT.NotAllocated changes after each allocation step so it must be caught here and assigned as sink limitation
                BAT.SinkLimitation = BAT.NotAllocated;

                // Then calculate how much resource is fixed from each supplying organ based on relative fixation supply
                if (BiomassFixed > 0)
                    for (int i = 0; i < Organs.Length; i++)
                    {
                        if (BAT.FixationSupply[i] > 0.00000000001)
                        {
                            double RelativeSupply = BAT.FixationSupply[i] / BAT.TotalFixationSupply;
                            BAT.Fixation[i] = BiomassFixed * RelativeSupply;
                        }
                    }
            }
        }

        /// <summary>Sends the dm allocations.</summary>
        /// <param name="Organs">The organs.</param>
        virtual public void SetDryMatterAllocations(IArbitration[] Organs)
        {
            // Send DM allocations to all Plant Organs
            for (int i = 0; i < Organs.Length; i++)
                Organs[i].SetDryMatterAllocation(new BiomassAllocationType
                {
                    Respired = DM.Respiration[i],
                    Reallocation = DM.Reallocation[i],
                    Retranslocation = DM.Retranslocation[i],
                    Structural = DM.StructuralAllocation[i],
                    Storage = DM.StorageAllocation[i],
                    Metabolic = DM.MetabolicAllocation[i],
                });
        }

        /// <summary>Sends the nutrient allocations.</summary>
        /// <param name="Organs">The organs.</param>
        virtual public void SetNitrogenAllocations(IArbitration[] Organs)
        {
            // Send N allocations to all Plant Organs
            for (int i = 0; i < Organs.Length; i++)
            {
                if ((N.StructuralAllocation[i] < -0.00000001) || (N.MetabolicAllocation[i] < -0.00000001) || (N.StorageAllocation[i] < -0.00000001))
                    throw new Exception("-ve N Allocation");
                if (N.StructuralAllocation[i] < 0.0)
                    N.StructuralAllocation[i] = 0.0;
                if (N.MetabolicAllocation[i] < 0.0)
                    N.MetabolicAllocation[i] = 0.0;
                if (N.StorageAllocation[i] < 0.0)
                    N.StorageAllocation[i] = 0.0;
                Organs[i].SetNitrogenAllocation(new BiomassAllocationType
                {
                    Structural = N.StructuralAllocation[i], //This needs to be seperated into components
                    Metabolic = N.MetabolicAllocation[i],
                    Storage = N.StorageAllocation[i],
                    Fixation = N.Fixation[i],
                    Reallocation = N.Reallocation[i],
                    Retranslocation = N.Retranslocation[i],
                    Uptake = N.Uptake[i]
                });
            }

            //Finally Check Mass balance adds up
            N.End = 0;
            for (int i = 0; i < Organs.Length; i++)
                N.End += Organs[i].Total.N;
            N.BalanceError = (N.End - (N.Start + N.TotalPlantSupply));
            if (N.BalanceError > 0.05)
                throw new Exception("N Mass balance violated!!!!.  Daily Plant N increment is greater than N supply");
            N.BalanceError = (N.End - (N.Start + N.TotalPlantDemand));
            if (N.BalanceError > 0.001)
                throw new Exception("N Mass balance violated!!!!  Daily Plant N increment is greater than N demand");
            DM.End = 0;
            for (int i = 0; i < Organs.Length; i++)
                DM.End += Organs[i].Total.Wt;
            DM.BalanceError = (DM.End - (DM.Start + DM.TotalPlantSupply));
            if (DM.BalanceError > 0.0001)
                throw new Exception("DM Mass Balance violated!!!!  Daily Plant Wt increment is greater than DM supplied by photosynthesis and DM remobilisation");
            DM.BalanceError = (DM.End - (DM.Start + DM.TotalStructuralDemand + DM.TotalMetabolicDemand + DM.TotalStorageDemand));
            if (DM.BalanceError > 0.0001)
                throw new Exception("DM Mass Balance violated!!!!  Daily Plant Wt increment is greater than the sum of structural DM demand, metabolic DM demand and Storage DM capacity");
        }

        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
        {
            if (IncludeInDocumentation)
            {
                // add a heading.
                tags.Add(new AutoDocumentation.Heading(Name, headingLevel));

                // write description of this class.
                AutoDocumentation.DocumentModelSummary(this, tags, headingLevel, indent, false);

                // write children.
                foreach (IModel child in Apsim.Children(this, typeof(Memo)))
                    AutoDocumentation.DocumentModel(child, tags, headingLevel + 1, indent);
            }
        }

    }
}