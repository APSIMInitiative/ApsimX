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
using Models.Functions;

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
    public class SorghumArbitrator : BaseArbitrator
    {
        #region Links and Input parameters

        ///// <summary>The method used to arbitrate N allocations</summary>
        //[Link(Type = LinkType.Child, ByName = true)]
        //private IArbitrationMethod NArbitrator = null;

        ///// <summary>The method used to arbitrate N allocations</summary>
        //[Link(Type = LinkType.Child, ByName = true)]
        //private IArbitrationMethod DMArbitrator = null;

        ///// <summary>The kgha2gsm</summary>
        //private const double kgha2gsm = 0.1;

        ///// <summary>The list of organs</summary>
        //private List<IArbitration> Organs = new List<IArbitration>();

        [Link]
        private Root root = null;

        [Link]
        private SorghumLeaf leaf = null;

        [Link]
        private Phenology phenology = null;

        #endregion

        #region Main outputs

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

        #endregion
        private List<IModel> uptakeModels = null;
        private List<IModel> zones = null;
        private double stage;
        private IPhase previousPhase;
        private double accumTT;

        ///// <summary>
        ///// Total TTFM accumulated from flowering.
        ///// </summary>
        //[JsonIgnore]
        //public double TTFMFromFlowering { get; private set; }

        /// <summary>
        /// Total TTFM accumulated from flowering.
        /// </summary>
        [Link(Type = LinkType.Path, Path = "[Phenology].TTFMFromFlowering")]
        protected IFunction TTFMFromFlowering = null;

        /// <summary>ThermalTime after Flowering to stop N Uptake</summary>
        [Link(Type = LinkType.Path, Path = "[Root].NUptakeCease")]
        private IFunction NUptakeCease { get; set; }

        /// <summary>
        /// Used in stem DM demand function. Need to reconsider how this works.
        /// </summary>
        [JsonIgnore]
        public double DMPlantMax { get; set; }

        /// <summary>Called at the start of the simulation.</summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">Dummy event data.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            stage = phenology.Stage;
            uptakeModels = Apsim.ChildrenRecursively(Parent, typeof(IUptake));
            zones = Apsim.ChildrenRecursively(this.Parent, typeof(Zone));
            previousPhase = phenology.CurrentPhase;
            DMPlantMax = 9999;
        }

        [EventSubscribe("StartOfDay")]
        private void OnStartOfDay(object sender, EventArgs e)
        {
            WAllocated = 0;
        }

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
            //UpdateTTElapsed();
        }

        /// <summary>Clears this instance.</summary>
        protected override void Clear()
        {
            base.Clear();
            DltTT = 0.0;
            WatSupply = 0.0;
            NMassFlowSupply = 0.0;
            NDiffusionSupply = 0.0;
            //TTFMFromFlowering = 0.0;

            SWAvailRatio = 0.0;
            SDRatio = 0.0;
            PhotoStress = 0.0;
            TotalAvail = 0.0;
            TotalPotAvail = 0.0;
        }

        #region IUptake interface

        /// <summary>
        /// Set the sw uptake for today
        /// </summary>
        public override void SetActualWaterUptake(List<ZoneWaterAndN> zones)
        {

            // Calculate the total water supply across all zones.
            double waterSupply = 0;   //NOTE: This is in L, not mm, to arbitrate water demands for spatial simulations.
            foreach (ZoneWaterAndN Z in zones)
            {
                // Z.Water calculated as Supply * fraction used
                waterSupply += MathUtilities.Sum(Z.Water) * Z.Zone.Area;
            }

            // Calculate total plant water demand.
            WDemand = 0.0; //NOTE: This is in L, not mm, to arbitrate water demands for spatial simulations.
            foreach (IArbitration o in Organs)
                if (o is IHasWaterDemand)
                    WDemand += (o as IHasWaterDemand).CalculateWaterDemand() * Plant.Zone.Area;

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
                StoreWaterVariablesForNitrogenUptake(zone);

            foreach (ZoneWaterAndN zone in zones)
            {
                Plant.Root.DoWaterUptake(zone.Water, zone.Zone.Name);
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

                var soilCrop = Soil.Crop(Plant.Name);
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
                Avail = myZone.AvailableSW;
                PotAvail = myZone.PotentialAvailableSW;
                TotalAvail = myZone.AvailableSW.Sum();
                TotalPotAvail = myZone.PotentialAvailableSW.Sum();

                //used for SWDef PhenologyStress table lookup
                SWAvailRatio = MathUtilities.Bound(MathUtilities.Divide(totalAvail, totalAvailPot, 1.0), 0.0, 10.0);

                //used for SWDef ExpansionStress table lookup
                SDRatio = MathUtilities.Bound(MathUtilities.Divide(totalSupply, WDemand, 1.0), 0.0, 10);

                //used for SwDefPhoto Stress
                PhotoStress = MathUtilities.Bound(MathUtilities.Divide(totalSupply, WDemand, 1.0), 0.0, 1.0);
            }
        }

        ///TotalAvailable divided by TotalPotential - used to lookup PhenologyStress table
        public double SWAvailRatio { get; set; }

        ///TotalSupply divided by WaterDemand - used to lookup ExpansionStress table - when calculating Actual LeafArea and calcStressedLeafArea
        public double SDRatio { get; set; }

        ///Same as SDRatio?? used to calculate Photosynthesis stress in calculating yield (Grain)
        public double PhotoStress { get; set; }

        /// <summary>Available SW by layer.</summary>
        public double[] Avail { get; private set; }

        /// <summary>Pot. Available SW by layer.</summary>
        public double[] PotAvail { get; private set; }

        /// <summary>Total available SW.</summary>
        public double TotalAvail { get; private set; }

        /// <summary>Total potential available SW.</summary>
        public double TotalPotAvail { get; private set; }

        /// <summary>
        /// Calculate the potential N uptake for today. Should return null if crop is not in the ground (this is not true for old sorghum).
        /// </summary>
        public override List<Soils.Arbitrator.ZoneWaterAndN> GetNitrogenUptakeEstimates(SoilState soilstate)
        {
            if (Plant.IsEmerged)
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

                        //double nUptakeCease = NUptakeCease.Value();

                        if (TTFMFromFlowering.Value() > NUptakeCease.Value())
                            totalMassFlow = 0;
                        actualMassFlow = totalMassFlow;

                        if (totalMassFlow < nDemand && TTFMFromFlowering.Value() < NUptakeCease.Value()) // fixme && ttElapsed < nUptakeCease
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

                    N.UptakeSupply[rootIndex] += MathUtilities.Sum(organNO3Supply) * kgha2gsm * zone.Zone.Area / Plant.Zone.Area;  //g/m2
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
        public override void SetActualNitrogenUptakes(List<ZoneWaterAndN> zones)
        {
            if (Plant.IsEmerged)
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

                    for (int i = 0; i < Z.NH4N.Length; ++i)
                        Z.NH4N[i] = 0;
                }

                //NDiffusionSupply = supply - NMassFlowSupply;

                //Reset actual uptakes to each organ based on uptake allocated by soil arbitrator and the organs proportion of potential uptake
                for (int i = 0; i < Organs.Count; i++)
                {
                    N.UptakeSupply[i] = nSupply / Plant.Zone.Area * N.UptakeSupply[i] / N.TotalUptakeSupply * kgha2gsm;
                    if (MathUtilities.IsNegative(N.UptakeSupply[i]))
                        throw new Exception($"-ve uptake supply for organ {(Organs[i] as IModel).Name}");
                }

                //Allocate N that the SoilArbitrator has allocated the plant to each organ
                AllocateUptake(Organs.ToArray(), N, NArbitrator);
                Plant.Root.DoNitrogenUptake(zones);
            }
        }

        #endregion

        //private void UpdateTTElapsed()
        //{
        //    // Can't do this at end of day because it will be too late.
        //    // Can't do this in DoPhenology because it will happen before daily
        //    // phenology development.
        //    int flowering = phenology.StartStagePhaseIndex("Flowering");
        //    int maturity = phenology.EndStagePhaseIndex("Maturity");
        //    if (phenology.Between(flowering, maturity))
        //    {
        //        double dltTT;
        //        if (phenology.CurrentPhase.Start == "Flowering" && phenology.CurrentPhase is GenericPhase)
        //            dltTT = (phenology.CurrentPhase as GenericPhase).ProgressionForTimeStep;
        //        else
        //            dltTT = ((double?)Apsim.Get(this, "[Phenology].DltTTFM.Value()") ?? (double)Apsim.Get(this, "[Phenology].ThermalTime.Value()"));
        //        TTFMFromFlowering += dltTT;
        //    }
        //}

        #region Plant interface methods

        /// <summary>Does the retranslocation.</summary>
        /// <param name="Organs">The organs.</param>
        /// <param name="BAT">The bat.</param>
        /// <param name="arbitrator">The option.</param>
        override public void Retranslocation(IArbitration[] Organs, BiomassArbitrationType BAT, IArbitrationMethod arbitrator)
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

        #endregion

        #region Arbitration step functions
        /// <summary>Does the water limited dm allocations.  Water constaints to growth are accounted for in the calculation of DM supply
        /// and does initial N calculations to work out how much N uptake is required to pass to SoilArbitrator</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoPotentialPlantPartioning")]
        override protected void OnDoPotentialPlantPartioning(object sender, EventArgs e)
        {
            if (Plant.IsEmerged)
            {
                DM.Clear();
                N.Clear();

                DMSupplies();
                DMDemands();
                PotentialDMAllocation();

                leaf.UpdateArea();

                NSupplies();
                NDemands();

                Reallocation(Organs.ToArray(), N, NArbitrator);           // Allocate N available from reallocation to each organ
            }
        }


        #endregion

    }
}