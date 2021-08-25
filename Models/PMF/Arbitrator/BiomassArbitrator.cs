using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Models.PMF.Arbitrator;
using Models.PMF.Interfaces;
using Models.Soils.Arbitrator;
using System;
using System.Collections.Generic;
using System.Linq;
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
    /// ![Alt Text](ArbitratorSequenceDiagram.png)
    /// 
    /// **Figure [FigureNumber]:**  Schematic showing the procedure for arbitration of biomass partitioning.  Pink boxes represent events that occur every day and their numbering shows the order of calculations. Blue boxes represent the methods that are called when these events occur.  Orange boxes contain properties that make up the organ/arbitrator interface.  Green boxes are organ specific properties.
    /// </summary>

    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(IPlant))]
    public class BiomassArbitrator : Model, IUptake, ICustomDocumentation, ITotalDMFixationSupply
    {
        ///1. Links
        ///------------------------------------------------------------------------------------------------

        /// <summary>The top level plant object in the Plant Modelling Framework</summary>
        [Link]
        private Plant plant = null;

        /// <summary>The zone.</summary>
        [Link(Type = LinkType.Ancestor)]
        protected IZone zone = null;

        ///2. Private And Protected Fields
        /// -------------------------------------------------------------------------------------------------

        /// <summary>The kgha2gsm</summary>
        protected const double kgha2gsm = 0.1;

        /// <summary>The list of organs</summary>
        protected List<ISubscribeToBiomassArbitration> Organs = new List<ISubscribeToBiomassArbitration>();

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
        [JsonIgnore]
        public BiomassArbitrationStates DM { get; private set; }

        /// <summary>The variables for N</summary>
        [JsonIgnore]
        public BiomassArbitrationStates N { get; private set; }

        //// <summary>Gets the dry mass supply relative to dry mass demand.</summary>
        /// <value>The dry mass supply.</value>
        [JsonIgnore]
        public double FDM { get { return DM == null ? 0 : MathUtilities.Divide(DM.TotalPlantSupply, DM.TotalPlantDemand, 0); } }

        /// <summary>Gets the dry mass supply relative to dry structural demand plus metabolic demand.</summary>
        /// <value>The dry mass supply.</value>
        [JsonIgnore]
        public double StructuralCarbonSupplyDemand { get { return DM == null ? 0 : MathUtilities.Divide(DM.TotalPlantSupply, (DM.TotalStructuralDemand + DM.TotalMetabolicDemand), 0); } }

        /// <summary>Gets the delta wt.</summary>
        /// <value>The delta wt.</value>
        public double DeltaWt { get { return DM == null ? 0 : (DM.End - DM.Start); } }

        /// <summary>Gets the n supply relative to N demand.</summary>
        /// <value>The n supply.</value>
        [JsonIgnore]
        public double FN { get { return N == null ? 0 : MathUtilities.Divide(N.TotalPlantSupply, N.TotalPlantDemand, 0); } }

        ///6. Public methods
        /// -----------------------------------------------------------------------------------------------------------

        /// <summary>Things the plant model does when the simulation starts</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        virtual protected void OnSimulationCommencing(object sender, EventArgs e) 
        {
            Clear();
            List<IHasWaterDemand> Waterdemands = new List<IHasWaterDemand>();

            foreach (Model Can in plant.FindAllInScope<IHasWaterDemand>())
                Waterdemands.Add(Can as IHasWaterDemand);

            WaterDemands = Waterdemands;
        }
        
        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantSowing")]
        virtual protected void OnPlantSowing(object sender, SowingParameters data)
        {
            List<ISubscribeToBiomassArbitration> organsToArbitrate = new List<ISubscribeToBiomassArbitration>();

            foreach (IOrgan organ in plant.Organs)
                if (organ is ISubscribeToBiomassArbitration)
                    organsToArbitrate.Add(organ as ISubscribeToBiomassArbitration);

            Organs = organsToArbitrate;
            DM = new BiomassArbitrationStates("DM", Organs);
            N = new BiomassArbitrationStates("N", Organs);

        }

        /// <summary>Total DM supply from photosynthesis needed for partitioning fraction function</summary>
        public double TotalDMFixationSupply { get { return DM.Fixation == null ? 0 : DM.Fixation.TotalSupply; } }


        /// First get all demands and supplies, send potential DM allocations and do N reallocation so N uptake demand can be calculated

        /// <summary>Does the water limited dm allocations.  Water constaints to growth are accounted for in the calculation of DM supply
        /// and does initial N calculations to work out how much N uptake is required to pass to SoilArbitrator</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoPotentialPlantPartioning")]
        virtual protected void OnDoPotentialPlantPartioning(object sender, EventArgs e)
        {
            if (plant.IsEmerged)
            {
                DM.Clear();
                N.Clear();

                // Calculate DM Supplies and Demands
                DMSupplies();
                DMDemands();

                // Calculate potential DM allocaiton without nutrient limitation

                DoAllocation(DM.ReAllocation, DM);
                DoAllocation(DM.Fixation, DM);
                DoAllocation(DM.ReTranslocation, DM);
                SendPotentialDMAllocations(DM);
                
                // Calculate N supplies and Demands
                NSupplies();
                NDemands();

                // Calculate N Reallocation
                DoAllocation(N.ReAllocation, N);
            }
        }

        /// <summary>
        /// Set the sw uptake for today
        /// </summary>
        public virtual void SetActualNitrogenUptakes(List<ZoneWaterAndN> zones)
        {
            if (plant.IsEmerged)
            {
                SetActualUptakes(zones, Organs.ToArray());
                //Allocate N that the SoilArbitrator has allocated the plant to each organ
                DoAllocation(N.Uptake, N);
                int count = 0;
                foreach (IWaterNitrogenUptake u in plant.FindAllChildren<IWaterNitrogenUptake>())
                {
                    if (count > 0)
                        throw new Exception("Two organs have IWaterNitrogenUptake");
                    u.DoNitrogenUptake(zones);
                    count += 1;
                }
            }
        }

        /// <summary>Does the nutrient allocations.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoActualPlantPartioning")]
        virtual protected void OnDoActualPlantPartioning(object sender, EventArgs e)
        {
            if (plant.IsEmerged)
            {
                //ordering within the arbitration items is important - uses the order in the tree
                //Do the rest of the N partitioning, revise DM allocations if N is limited and do DM and N allocations
                DoAllocation(N.Fixation, N);
                DoAllocation(N.ReTranslocation, N);
                
                NutrientConstrainedDMAllocation();
                AllocateDM();
                AllocateN();
            }
        }
        
        /// <summary>Send potential DM allocations.</summary>
        virtual protected void SendPotentialDMAllocations(BiomassArbitrationStates BAS)
        {

            //  Allocate to meet Organs demands
            BAS.Allocated = BAS.TotalStructuralAllocation + BAS.TotalMetabolicAllocation + BAS.TotalStorageAllocation;

            // Then check it all adds up
            if (MathUtilities.IsGreaterThan(BAS.Allocated, BAS.TotalPlantSupply))
                throw new Exception("Potential DM allocation by " + this.Name + " exceeds DM supply.   Thats not really possible so something has gone a miss");
            if (MathUtilities.IsGreaterThan(BAS.Allocated, BAS.TotalPlantDemand))
                throw new Exception("Potential DM allocation by " + this.Name + " exceeds DM Demand.   Thats not really possible so something has gone a miss");

            // Send potential DM allocation to organs to set this variable for calculating N demand
            for (int i = 0; i < Organs.Count; i++)
                Organs[i].Carbon.SetDryMatterPotentialAllocation(new BiomassPoolType
                {
                    Structural = BAS.StructuralAllocation[i],
                    Metabolic = BAS.MetabolicAllocation[i],
                    Storage = BAS.StorageAllocation[i],
                });
        }

        /// <summary>Determines Nutrient limitations to DM allocations</summary>
        public void NutrientConstrainedDMAllocation()
        {
            double PreNStressDMAllocation = DM.Allocated;
            for (int i = 0; i < Organs.Count; i++)
                N.TotalAllocation[i] = N.StructuralAllocation[i] + N.MetabolicAllocation[i] + N.StorageAllocation[i];

            N.Allocated = N.TotalAllocation.Sum();

            //To introduce functionality for other nutrients we need to repeat this for loop for each new nutrient type
            // Calculate posible growth based on Minimum N requirement of organs
            for (int i = 0; i < Organs.Count; i++)
            {
                double TotalNDemand = N.StructuralDemand[i] + N.MetabolicDemand[i] + N.StorageDemand[i];
                if (N.TotalAllocation[i] > TotalNDemand || MathUtilities.FloatsAreEqual(N.TotalAllocation[i], TotalNDemand))
                    N.ConstrainedGrowth[i] = 100000000; //given high value so where there is no N deficit in organ and N limitation to growth  
                else
                    if (N.TotalAllocation[i] == 0 | Organs[i].MinNconc == 0)
                    N.ConstrainedGrowth[i] = 0;
                else
                    N.ConstrainedGrowth[i] = N.TotalAllocation[i] / Organs[i].MinNconc;
            }

            // Reduce DM allocation below potential if insufficient N to reach Min n Conc or if DM was allocated to fixation
            for (int i = 0; i < Organs.Count; i++)
                if ((DM.MetabolicAllocation[i] + DM.StructuralAllocation[i]) != 0)
                {
                    double MetabolicProportion = DM.MetabolicAllocation[i] / (DM.MetabolicAllocation[i] + DM.StructuralAllocation[i] + DM.StorageAllocation[i]);
                    double StructuralProportion = DM.StructuralAllocation[i] / (DM.MetabolicAllocation[i] + DM.StructuralAllocation[i] + DM.StorageAllocation[i]);
                    double StorageProportion = DM.StorageAllocation[i] / (DM.MetabolicAllocation[i] + DM.StructuralAllocation[i] + DM.StorageAllocation[i]);
                    DM.MetabolicAllocation[i] = Math.Min(DM.MetabolicAllocation[i], N.ConstrainedGrowth[i] * MetabolicProportion);
                    DM.StructuralAllocation[i] = Math.Min(DM.StructuralAllocation[i], N.ConstrainedGrowth[i] * StructuralProportion);  //To introduce effects of other nutrients Need to include Plimited and Klimited growth in this min function
                    DM.StorageAllocation[i] = Math.Min(DM.StorageAllocation[i], N.ConstrainedGrowth[i] * StorageProportion);  //To introduce effects of other nutrients Need to include Plimited and Klimited growth in this min function

                    //Question.  Why do I not restrain non-structural DM allocations.  I think this may be wrong and require further thought HEB 15-1-2015
                }
            //Recalculated DM Allocation totals
            DM.Allocated = DM.TotalStructuralAllocation + DM.TotalMetabolicAllocation + DM.TotalStorageAllocation;
            DM.NutrientLimitation = (PreNStressDMAllocation - DM.Allocated);
        }

        /// <summary>Allocates the DM to the different organs.</summary>
        public void AllocateDM()
        {
            for (int i = 0; i < Organs.Count; i++)
                Organs[i].Carbon.SetDryMatterAllocation(new BiomassAllocationType
                {
                    Respired = DM.Respiration[i],
                    Reallocation = DM.ReAllocation.Allocated[i],
                    Retranslocation = DM.ReTranslocation.Allocated[i],
                    Structural = DM.StructuralAllocation[i],
                    Storage = DM.StorageAllocation[i],
                    Metabolic = DM.MetabolicAllocation[i],
                });

            DM.End = 0;
            for (int i = 0; i < Organs.Count; i++)
                DM.End += Organs[i].Total.Wt;
            DM.BalanceError = (DM.End - (DM.Start + DM.TotalPlantSupply));
            if (DM.BalanceError > 0.0001)
                throw new Exception("Mass Balance violated!!!!  Daily Plant increment is greater than that supplied by photosynthesis and DM remobilisation");
            DM.BalanceError = (DM.End - (DM.Start + DM.TotalStructuralDemand + DM.TotalMetabolicDemand + DM.TotalStorageDemand));
            if (DM.BalanceError > 0.0001)
                throw new Exception("DM Mass Balance violated!!!!  Daily Plant Wt increment is greater than the sum of structural DM demand, metabolic DM demand and Storage DM capacity");
        }

        /// <summary>Allocate the nutrient allocations.</summary>
        public void AllocateN()
        {
            // Send N allocations to all Plant Organs
            for (int i = 0; i < Organs.Count; i++)
            {
                if ((N.StructuralAllocation[i] < -0.00000001) || (N.MetabolicAllocation[i] < -0.00000001) || (N.StorageAllocation[i] < -0.00000001))
                    throw new Exception("-ve N Allocation");
                if (N.StructuralAllocation[i] < 0.0)
                    N.StructuralAllocation[i] = 0.0;
                if (N.MetabolicAllocation[i] < 0.0)
                    N.MetabolicAllocation[i] = 0.0;
                if (N.StorageAllocation[i] < 0.0)
                    N.StorageAllocation[i] = 0.0;
                Organs[i].Nutrients[0].SetNitrogenAllocation(new BiomassAllocationType
                {
                    Structural = N.StructuralAllocation[i], //This needs to be seperated into components
                    Metabolic = N.MetabolicAllocation[i],
                    Storage = N.StorageAllocation[i],
                    Fixation = N.Fixation.Allocated[i],
                    Reallocation = N.ReAllocation.Allocated[i],
                    Retranslocation = N.ReTranslocation.Allocated[i],
                    Uptake = N.Uptake.Allocated[i]
                });
            }

            //Finally Check Mass balance adds up
            N.End = 0;
            for (int i = 0; i < Organs.Count; i++)
                N.End += Organs[i].Total.N;
            N.BalanceError = (N.End - (N.Start + N.TotalPlantSupply));
            if (N.BalanceError > 0.05)
                throw new Exception("N Mass balance violated!!!!.  Daily Plant N increment is greater than N supply");
            N.BalanceError = (N.End - (N.Start + N.TotalPlantDemand));
            if (N.BalanceError > 0.001)
                throw new Exception("N Mass balance violated!!!!  Daily Plant N increment is greater than N demand");

        }

        /// <summary>Relatives the allocation.</summary>
        /// <param name="AT">The Allocation process</param>
        /// <param name="BAS">The bat.</param>
        public void DoAllocation(AllocationType AT, BiomassArbitrationStates BAS)
        {
            if (AT.TotalSupply > 0.00000000001)
            {
                double TotalAllocated = 0;
                BiomassPoolType[] PriorityScalledDemands = new BiomassPoolType[Organs.Count];
                double TotalPlantPriorityScalledDemand = 0;
                for (int i = 0; i < Organs.Count; i++)
                {
                    PriorityScalledDemands[i] = new BiomassPoolType();
                    PriorityScalledDemands[i].Structural = BAS.StructuralDemand[i] * BAS.QStructural[i];
                    PriorityScalledDemands[i].Metabolic = BAS.MetabolicDemand[i] * BAS.QMetabolic[i];
                    PriorityScalledDemands[i].Storage = BAS.StorageDemand[i] * BAS.QStorage[i];
                    TotalPlantPriorityScalledDemand += PriorityScalledDemands[i].Total;
                }

                double NotAllocated = AT.TotalSupply;
                ////First time round allocate with priority factors applied so higher priority sinks get more allocation
                for (int i = 0; i < Organs.Count; i++)
                {
                    double StructuralRequirement = Math.Max(0, BAS.StructuralDemand[i] - BAS.StructuralAllocation[i]);
                    double MetabolicRequirement = Math.Max(0, BAS.MetabolicDemand[i] - BAS.MetabolicAllocation[i]);
                    double StorageRequirement = Math.Max(0, BAS.StorageDemand[i] - BAS.StorageAllocation[i]);
                    if ((StructuralRequirement + MetabolicRequirement + StorageRequirement) > 0.0)
                    {
                        double StructuralAllocation = Math.Min(StructuralRequirement, AT.TotalSupply * MathUtilities.Divide(PriorityScalledDemands[i].Structural, TotalPlantPriorityScalledDemand, 0));
                        double MetabolicAllocation = Math.Min(MetabolicRequirement, AT.TotalSupply * MathUtilities.Divide(PriorityScalledDemands[i].Metabolic, TotalPlantPriorityScalledDemand, 0));
                        double StorageAllocation = Math.Min(StorageRequirement, AT.TotalSupply * MathUtilities.Divide(PriorityScalledDemands[i].Storage, TotalPlantPriorityScalledDemand, 0));

                        BAS.StructuralAllocation[i] += StructuralAllocation;
                        BAS.MetabolicAllocation[i] += MetabolicAllocation;
                        BAS.StorageAllocation[i] += StorageAllocation;
                        NotAllocated -= (StructuralAllocation + MetabolicAllocation + StorageAllocation);
                        TotalAllocated += (StructuralAllocation + MetabolicAllocation + StorageAllocation);
                    }
                }
                double FirstPassNotallocated = NotAllocated;
                double RemainingDemand = BAS.TotalPlantDemand - BAS.TotalPlantAllocation;
                // Second time round if there is still biomass to allocate do it based on relative demands so lower priority organs have the change to be allocated full demand
                for (int i = 0; i < Organs.Count; i++)
                {
                    double StructuralRequirement = Math.Max(0, BAS.StructuralDemand[i] - BAS.StructuralAllocation[i]);
                    double MetabolicRequirement = Math.Max(0, BAS.MetabolicDemand[i] - BAS.MetabolicAllocation[i]);
                    double StorageRequirement = Math.Max(0, BAS.StorageDemand[i] - BAS.StorageAllocation[i]);
                    if ((StructuralRequirement + MetabolicRequirement + StorageRequirement) > 0.0)
                    {
                        double StructuralAllocation = Math.Min(StructuralRequirement, FirstPassNotallocated * MathUtilities.Divide(StructuralRequirement, RemainingDemand, 0));
                        double MetabolicAllocation = Math.Min(MetabolicRequirement, FirstPassNotallocated * MathUtilities.Divide(MetabolicRequirement, RemainingDemand, 0));
                        double StorageAllocation = Math.Min(StorageRequirement, FirstPassNotallocated * MathUtilities.Divide(StorageRequirement, RemainingDemand, 0));

                        BAS.StructuralAllocation[i] += StructuralAllocation;
                        BAS.MetabolicAllocation[i] += MetabolicAllocation;
                        BAS.StorageAllocation[i] += StorageAllocation;
                        NotAllocated -= (StructuralAllocation + MetabolicAllocation + StorageAllocation);
                        TotalAllocated += (StructuralAllocation + MetabolicAllocation + StorageAllocation);
                    }
                }

                // Attribute Allocation to supplying pools
                if (TotalAllocated > 0)
                    for (int i = 0; i < Organs.Count; i++)
                        if (AT.Supply[i] > 0)
                        {
                            double RelativeSupply = AT.Supply[i] / AT.TotalSupply;
                            AT.Allocated[i] += TotalAllocated * RelativeSupply;
                        }
            }
        }

        /// <summary>A list of organs or suborgans that have watardemands</summary>
        protected List<IHasWaterDemand> WaterDemands = new List<IHasWaterDemand>();

        /// <summary>Gets the water demand.</summary>
        /// <value>The water demand.</value>
        [JsonIgnore]
        public double WDemand { get; protected set; }

        /// <summary>Gets the water Supply.</summary>
        /// <value>The water supply.</value>
        [JsonIgnore]
        public double WSupply { get; protected set; }

        /// <summary>Gets the water allocated in the plant (taken up).</summary>
        /// <value>The water uptake.</value>
        [JsonIgnore]
        public double WAllocated { get; protected set; }



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
                    foreach (IWaterNitrogenUptake o in plant.FindAllChildren<IWaterNitrogenUptake>())
                        {
                            double[] organSupply = o.CalculateWaterSupply(zone);
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
                    waterDemand += WD.CalculateWaterDemand() * zone.Area;

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
                    ZWNs.Add(uptake);
                }
                return ZWNs;
            }
            return null;
        }

        /// <summary>
        /// Set the sw uptake for today
        /// </summary>
        public virtual void SetActualWaterUptake(List<ZoneWaterAndN> zones)
        {
            // Calculate the total water supply across all zones.
            double waterSupply = 0;   //NOTE: This is in L, not mm, to arbitrate water demands for spatial simulations.
            foreach (ZoneWaterAndN Z in zones)
            {
                waterSupply += MathUtilities.Sum(Z.Water) * Z.Zone.Area;
            }
            // Calculate total plant water demand.
            WDemand = 0.0; //NOTE: This is in L, not mm, to arbitrate water demands for spatial simulations.
            foreach (IWaterNitrogenUptake o in plant.FindAllChildren<IWaterNitrogenUptake>())
            {
                WDemand += (o as IHasWaterDemand).CalculateWaterDemand() * zone.Area;
            }

            // Calculate the fraction of water demand that has been given to us.
            double fraction = 1;
            if (WDemand > 0)
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
            //WDemand = waterUptakeMethod.WDemand;
            //WAllocated = waterUptakeMethod.WAllocated;

            // Give the water uptake for each zone to Root so that it can perform the uptake
            // i.e. Root will do pass the uptake to the soil water balance.
            foreach (ZoneWaterAndN Z in zones)
                plant.Root.DoWaterUptake(Z.Water, Z.Zone.Name);

        }

        /// <summary>
        /// Calculate the potential N uptake for today. Should return null if crop is not in the ground.
        /// </summary>
        public virtual List<Soils.Arbitrator.ZoneWaterAndN> GetNitrogenUptakeEstimates(SoilState soilstate)
        {
            if (plant.IsEmerged)
            {
                return GetUptakeEstimates(soilstate, Organs.ToArray());
            }
            return null;
        }

        /// <summary>Calculate the Actual Nitrogen Uptakes</summary>
        public void SetActualUptakes(List<ZoneWaterAndN> zones, ISubscribeToBiomassArbitration[] Organs)
        {
            // Calculate the total no3 and nh4 across all zones.
            double NSupply = 0;//NOTE: This is in kg, not kg/ha, to arbitrate N demands for spatial simulations.
            foreach (ZoneWaterAndN Z in zones)
                NSupply += (Z.NO3N.Sum() + Z.NH4N.Sum()) * Z.Zone.Area;

            //Reset actual uptakes to each organ based on uptake allocated by soil arbitrator and the organs proportion of potential uptake
            //NUptakeSupply units should be g/m^2
            for (int i = 0; i < Organs.Count(); i++)
                N.Uptake.Supply[i] = NSupply / zone.Area * MathUtilities.Divide(N.Uptake.Supply[i], N.Uptake.TotalSupply, 0) * kgha2gsm;

        }

        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantEnding")]
        virtual protected void OnPlantEnding(object sender, EventArgs e)
        {
            Clear();
        }

        /// Local methods for setting up supplies and demands
        /// <summary>Accumulate all of the Organ DM Supplies </summary>
        public virtual void DMSupplies()
        {
            // Setup DM supplies from each organ
            SetDMSupply?.Invoke(this, new EventArgs());
            BiomassSupplyType[] supplies = Organs.Select(organ => organ.Carbon.DMSupply).ToArray();

            double totalWt = Organs.Sum(o => o.Total.Wt);
            DM.GetSupplies(supplies, totalWt);

        }

        /// <summary>Calculate all of the Organ DM Demands </summary>
        public virtual void DMDemands()
        {
            // Setup DM demands for each organ  
            SetDMDemand?.Invoke(this, new EventArgs());
            BiomassPoolType[] demands = Organs.Select(organ => organ.Carbon.DMDemand).ToArray();
            DM.GetDemands(demands);
        }

        /// <summary>Calculate all of the Organ N Supplies </summary>
        public virtual void NSupplies()
        {
            // Setup N supplies from each organ
            SetNSupply?.Invoke(this, new EventArgs());
            BiomassSupplyType[] supplies = Organs.Select(organ => organ.Nutrients[0].NSupply).ToArray();
            double totalN = Organs.Sum(o => o.Total.N);
            N.GetSupplies(supplies, totalN);

        }

        /// <summary>Calculate all of the Organ N Demands </summary>
        public virtual void NDemands()
        {
            // Setup N demands
            SetNDemand?.Invoke(this, new EventArgs());
            BiomassPoolType[] demands = Organs.Select(organ => organ.Nutrients[0].NDemand).ToArray();
            N.GetDemands(demands);

        }

        /// <summary>Calculate Nitrogen UptakeEstimates</summary>
        public List<ZoneWaterAndN> GetUptakeEstimates(SoilState soilstate, ISubscribeToBiomassArbitration[] Organs)
        {
            double NSupply = 0;//NOTE: This is in kg, not kg/ha, to arbitrate N demands for spatial simulations.

            for (int i = 0; i < Organs.Count(); i++)
                N.Uptake.Supply[i] = 0;

            List<ZoneWaterAndN> zones = new List<ZoneWaterAndN>();
            foreach (ZoneWaterAndN zone in soilstate.Zones)
            {
                ZoneWaterAndN UptakeDemands = new ZoneWaterAndN(zone);

                UptakeDemands.NO3N = new double[zone.NO3N.Length];
                UptakeDemands.NH4N = new double[zone.NH4N.Length];
                UptakeDemands.Water = new double[UptakeDemands.NO3N.Length];

                //Get Nuptake supply from each organ and set the PotentialUptake parameters that are passed to the soil arbitrator
                for (int i = 0; i < Organs.Count(); i++)
                    if (Organs[i].WaterNitrogenUptakeObject != null)
                    {
                        double[] organNO3Supply = new double[zone.NO3N.Length];
                        double[] organNH4Supply = new double[zone.NH4N.Length];
                        Organs[i].WaterNitrogenUptakeObject.CalculateNitrogenSupply(zone, ref organNO3Supply, ref organNH4Supply);
                        UptakeDemands.NO3N = MathUtilities.Add(UptakeDemands.NO3N, organNO3Supply); //Add uptake supply from each organ to the plants total to tell the Soil arbitrator
                        UptakeDemands.NH4N = MathUtilities.Add(UptakeDemands.NH4N, organNH4Supply);
                        double organSupply = organNH4Supply.Sum() + organNO3Supply.Sum();
                        N.Uptake.Supply[i] += organSupply * kgha2gsm * zone.Zone.Area / this.zone.Area;
                        NSupply += organSupply * zone.Zone.Area;
                    }
                zones.Add(UptakeDemands);
            }

            double NDemand = (N.TotalPlantDemand - N.ReAllocation.TotalAllocated) / kgha2gsm * zone.Area; //NOTE: This is in kg, not kg/ha, to arbitrate N demands for spatial simulations.
            if (NDemand < 0) NDemand = 0;  //NSupply should be zero if Reallocation can meet all demand (including small rounding errors which can make this -ve)

            if (NSupply > NDemand)
            {
                //Reduce the PotentialUptakes that we pass to the soil arbitrator
                double ratio = Math.Min(1.0, NDemand / NSupply);
                foreach (ZoneWaterAndN UptakeDemands in zones)
                {
                    UptakeDemands.NO3N = MathUtilities.Multiply_Value(UptakeDemands.NO3N, ratio);
                    UptakeDemands.NH4N = MathUtilities.Multiply_Value(UptakeDemands.NH4N, ratio);
                }
            }
            return zones;
        }

        /// <summary>Clears this instance.</summary>
        virtual protected void Clear()
        {
            DM = new BiomassArbitrationStates("DM", Organs);
            N = new BiomassArbitrationStates("N", Organs);
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
                foreach (IModel child in this.FindAllChildren<Memo>())
                    AutoDocumentation.DocumentModel(child, tags, headingLevel + 1, indent);
            }
        }
    }
}
