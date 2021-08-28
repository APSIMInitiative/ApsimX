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
using Models.PMF.Organs;

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
    public class BiomassArbitrator : Model, ICustomDocumentation, ITotalDMFixationSupply
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

        ///3. The Constructor
        /// -------------------------------------------------------------------------------------------------
        BiomassArbitrator()
        {

        }

        ///4. Public Events And Enums
        /// -------------------------------------------------------------------------------------------------


        ///5. Public Properties
        /// --------------------------------------------------------------------------------------------------

        /// <summary>The list of organs</summary>
        [JsonIgnore]
        public List<Organ> Organs = new List<Organ>();

        /// <summary>The variables for DM</summary>
        [JsonIgnore]
        public PlantResourceDeltas Carbon { get; private set; }

        /// <summary>The variables for N</summary>
        [JsonIgnore]
        public PlantResourceDeltas Nitrogen { get; private set; }

        /// <summary>Gets the dry mass supply relative to dry mass demand.</summary>
        [JsonIgnore]
        public double FDM { get { return Carbon == null ? 0 : MathUtilities.Divide(Carbon.TotalPlantSupply, Carbon.TotalPlantDemand, 0); } }

        /// <summary>Gets the delta wt.</summary>
        public double DeltaWt { get { return Carbon == null ? 0 : (Carbon.End - Carbon.Start); } }

        /// <summary>Total DM supply from photosynthesis needed for partitioning fraction function</summary>
        public double TotalDMFixationSupply { get { return Carbon.TotalFixationSupply; } }

        ///6. Public methods
        /// -----------------------------------------------------------------------------------------------------------

        /// <summary>Things the plant model does when the simulation starts</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        virtual protected void OnSimulationCommencing(object sender, EventArgs e) 
        {
            List<OrganResourceDeltas> organsToArbitrateC = new List<OrganResourceDeltas>();
            List<OrganResourceDeltas> organsToArbitrateN = new List<OrganResourceDeltas>();

            foreach (Organ organ in plant.FindAllChildren<Organ>())
            {
                organsToArbitrateC.Add(organ.Carbon.Deltas);
                organsToArbitrateN.Add(organ.Nitrogen.Deltas);
            }

            Carbon = new PlantResourceDeltas(organsToArbitrateC);
            Nitrogen = new PlantResourceDeltas(organsToArbitrateN);
        }
        

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
                // Calculate potential DM allocaiton without nutrient limitation

                double CTotalReAllocationAllocated = DoAllocation(Carbon.TotalReAllocationSupply, Carbon);
                foreach (OrganResourceDeltas o in Carbon.organs)
                    if (o.Supplies.ReAllocation > 0)
                    {
                        double RelativeSupply = o.Supplies.ReAllocation / Carbon.TotalReAllocationSupply;
                        o.SuppliesAllocated.ReAllocation = CTotalReAllocationAllocated * RelativeSupply;
                    }
                
                double CTotalFixationAllocated = DoAllocation(Carbon.TotalFixationSupply, Carbon);
                foreach (OrganResourceDeltas o in Carbon.organs)
                    if (o.Supplies.Fixation > 0)
                    {
                        double RelativeSupply = o.Supplies.Fixation / Carbon.TotalFixationSupply;
                        o.SuppliesAllocated.Fixation = CTotalFixationAllocated * RelativeSupply;
                    }

                double CTotalReTranslocationAllocated = DoAllocation(Carbon.TotalReTranslocationSupply, Carbon);
                foreach (OrganResourceDeltas o in Carbon.organs)
                    if (o.Supplies.ReTranslocation > 0)
                    {
                        double RelativeSupply = o.Supplies.ReTranslocation / Carbon.TotalReTranslocationSupply;
                        o.SuppliesAllocated.ReTranslocation = CTotalReTranslocationAllocated * RelativeSupply;
                    }

                foreach (Organ o in Organs)
                    o.Nitrogen.SetSuppliesAndDemands();

                // Calculate N Reallocation
                double NTotalReTranslocationAllocated = DoAllocation(Nitrogen.TotalReAllocationSupply, Nitrogen);
                foreach (OrganResourceDeltas o in Nitrogen.organs)
                    if (o.Supplies.ReAllocation > 0)
                    {
                        double RelativeSupply = o.Supplies.ReAllocation / Carbon.TotalReAllocationSupply;
                        o.SuppliesAllocated.ReAllocation = NTotalReTranslocationAllocated * RelativeSupply;
                    }
            }
        }

        /// <summary>Takes N Allocation from Soil arbitration and partitions it within the plant</summary>
        public void AllocateNUptake(double TotalPlantUptake)
        {
            //Let uptake organs know what theire uptake
            int count = 0;
            foreach (Organ o in Organs)
            {
                if (o.WaterNitrogenUptakeObject != null)
                {
                    if (count > 0)
                        throw new Exception("Two organs have IWaterNitrogenUptake");
                    double relativeSupply = MathUtilities.Divide(o.Nitrogen.Deltas.Supplies.Uptake,  Nitrogen.TotalUptakeSupply, 0);
                    o.Nitrogen.Deltas.SuppliesAllocated.Uptake = TotalPlantUptake / zone.Area * relativeSupply;
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
                double NTotalFixationAllocated = DoAllocation(Nitrogen.TotalFixationSupply, Nitrogen);
                foreach (OrganResourceDeltas o in Carbon.organs)
                    if (o.Supplies.ReTranslocation > 0)
                    {
                        double RelativeSupply = o.Supplies.ReTranslocation / Carbon.TotalReTranslocationSupply;
                        o.SuppliesAllocated.ReTranslocation = NTotalFixationAllocated * RelativeSupply;
                    }

                double NTotalReTranslocationAllocated = DoAllocation(Nitrogen.TotalReTranslocationSupply, Nitrogen);
                
                foreach (OrganResourceDeltas o in Nitrogen.organs)
                    if (o.Supplies.ReAllocation > 0)
                    {
                        double RelativeSupply = o.Supplies.ReAllocation / Carbon.TotalReAllocationSupply;
                        o.SuppliesAllocated.ReAllocation = NTotalReTranslocationAllocated * RelativeSupply;
                    }

                NutrientConstrainedDMAllocation();
            }
        }
        
        /// <summary>Determines Nutrient limitations to DM allocations</summary>
        public void NutrientConstrainedDMAllocation()
        {
            double PreNStressDMAllocation = Carbon.TotalPlantDemandsAllocated;

            //To introduce functionality for other nutrients we need to repeat this for loop for each new nutrient type
            // Calculate posible growth based on Minimum N requirement of organs
            foreach (Organ o in Organs)
            {
                var N = o.Nitrogen.Deltas;
                if (N.DemandsAllocated.Total > N.Demands.Total || MathUtilities.FloatsAreEqual(N.DemandsAllocated.Total, N.Demands.Total))
                    N.MaxCDelta = 100000000; //given high value so where there is no N deficit in organ and N limitation to growth  
                else
                    if (N.DemandsAllocated.Total == 0 | N.MinimumConcentration == 0)
                    N.MaxCDelta = 0;
                else
                    N.MaxCDelta = N.DemandsAllocated.Total / N.MinimumConcentration;

                var C = o.Carbon.Deltas;
                if ((C.DemandsAllocated.Metabolic + C.DemandsAllocated.Structural) != 0)
                {
                    double MetabolicProportion = C.DemandsAllocated.Metabolic / C.DemandsAllocated.Total;
                    double StructuralProportion = C.DemandsAllocated.Metabolic / C.DemandsAllocated.Total;
                    double StorageProportion = C.DemandsAllocated.Metabolic / C.DemandsAllocated.Total; ;
                    C.DemandsAllocated.Metabolic = Math.Min(C.DemandsAllocated.Metabolic, C.MaxCDelta * MetabolicProportion);
                    C.DemandsAllocated.Structural = Math.Min(C.DemandsAllocated.Structural, C.MaxCDelta * StructuralProportion);  //To introduce effects of other nutrients Need to include Plimited and Klimited growth in this min function
                    C.DemandsAllocated.Storage = Math.Min(C.DemandsAllocated.Storage, C.MaxCDelta * StorageProportion);  //To introduce effects of other nutrients Need to include Plimited and Klimited growth in this min function
                }
            }
        }


        /// <summary>Relatives the allocation.</summary>
        /// <param name="TotalSupply">The Allocation process</param>
        /// <param name="PRS">The bat.</param>
        public double DoAllocation(double TotalSupply, PlantResourceDeltas PRS)
        {
            double TotalAllocated = 0;
            if (TotalSupply > 0.00000000001)
            {
                double NotAllocated = TotalSupply;
                
                ////First time round allocate with priority factors applied so higher priority sinks get more allocation
                foreach (OrganResourceDeltas o in PRS.organs)
                {
                    if (o.OutstandingDemands.Total > 0.0)
                    {
                        double StructuralAllocation = Math.Min(o.OutstandingDemands.Structural, TotalSupply * MathUtilities.Divide(o.PriorityScaledDemand.Structural, PRS.TotalPlantPriorityScalledDemand, 0));
                        double MetabolicAllocation = Math.Min(o.OutstandingDemands.Metabolic, TotalSupply * MathUtilities.Divide(o.PriorityScaledDemand.Metabolic, PRS.TotalPlantPriorityScalledDemand, 0));
                        double StorageAllocation = Math.Min(o.OutstandingDemands.Storage, TotalSupply * MathUtilities.Divide(o.PriorityScaledDemand.Storage, PRS.TotalPlantPriorityScalledDemand, 0));

                        o.DemandsAllocated.Structural += StructuralAllocation;
                        o.DemandsAllocated.Metabolic += MetabolicAllocation;
                        o.DemandsAllocated.Storage += StorageAllocation;
                        NotAllocated -= (StructuralAllocation + MetabolicAllocation + StorageAllocation);
                        TotalAllocated += (StructuralAllocation + MetabolicAllocation + StorageAllocation);
                    }
                }
                double FirstPassNotallocated = NotAllocated;
                double RemainingDemand = PRS.TotalPlantDemand - PRS.TotalPlantDemandsAllocated;
                // Second time round if there is still biomass to allocate do it based on relative demands so lower priority organs have the change to be allocated full demand
                foreach (OrganResourceDeltas o in PRS.organs)
                {
                    if (o.OutstandingDemands.Total > 0.0)
                    {
                        double StructuralAllocation = Math.Min(o.OutstandingDemands.Structural, FirstPassNotallocated * MathUtilities.Divide(o.OutstandingDemands.Structural, RemainingDemand, 0));
                        double MetabolicAllocation = Math.Min(o.OutstandingDemands.Metabolic, FirstPassNotallocated * MathUtilities.Divide(o.OutstandingDemands.Metabolic, RemainingDemand, 0));
                        double StorageAllocation = Math.Min(o.OutstandingDemands.Storage, FirstPassNotallocated * MathUtilities.Divide(o.OutstandingDemands.Storage, RemainingDemand, 0));

                        o.DemandsAllocated.Structural += StructuralAllocation;
                        o.DemandsAllocated.Metabolic += MetabolicAllocation;
                        o.DemandsAllocated.Storage += StorageAllocation;
                        NotAllocated -= (StructuralAllocation + MetabolicAllocation + StorageAllocation);
                        TotalAllocated += (StructuralAllocation + MetabolicAllocation + StorageAllocation);
                    }
                }
            }
            return TotalAllocated;
        }

        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantEnding")]
        virtual protected void OnPlantEnding(object sender, EventArgs e)
        {
            Clear();
        }

        /// <summary>Clears this instance.</summary>
        virtual protected void Clear()
        {
            Carbon.Clear();
            Nitrogen.Clear();
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
