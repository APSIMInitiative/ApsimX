using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Models.PMF.Interfaces;
using Models.Soils.Arbitrator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

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
    /// ![Alt Text](ArbitrationDiagram.PNG)
    /// 
    /// **Figure [FigureNumber]:**  Schematic showing the procedure for arbitration of biomass partitioning.  Pink boxes represent events that occur every day and their numbering shows the order of calculations. Blue boxes represent the methods that are called when these events occur.  Orange boxes contain properties that make up the organ/arbitrator interface.  Green boxes are organ specific properties.
    /// </summary>

    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Plant))]
    public class OrganArbitrator : BaseArbitrator
    {
        #region Links and Input parameters

        #endregion

        #region Main outputs

        #endregion

        #region IUptake interface

        #endregion

        #region Plant interface methods
        /// <summary>Accumulate all of the Organ DM Supplies </summary>
        public override void DMSupplies()
        {
            base.DMSupplies();

            double maintenanceRespiration = Organs.Sum(o => o.MaintenanceRespiration);
            if (maintenanceRespiration > 0)
            {
                SubtractMaintenanceRespiration(maintenanceRespiration);
            }

        }

        /// <summary>Subtract maintenance respiration from daily fixation</summary>
        /// <param name="respiration">The toal maintenance respiration</param>
        public void SubtractMaintenanceRespiration(double respiration)
        {
            double total = DM.TotalFixationSupply;
            // First: from daily fixation 
            double respirationFixation = respiration <= total ? respiration : total;
            double ratio = (total - respirationFixation) / total;
            for (int i = 0; i < DM.FixationSupply.Length; i++)
            {
                DM.FixationSupply[i] *= ratio;
            }

            // Second: from live component if there are not enough fixation
            if (respiration > total)
            {
                double remainRespiration = respiration - total;
                for (int i = 0; i < Organs.ToArray().Length; i++)
                {
                    if ((Organs[i].Live.StorageWt + Organs[i].Live.MetabolicWt) > 0)
                    {
                        double organRespiration = remainRespiration * Organs[i].MaintenanceRespiration / respiration;
                        Organs[i].RemoveMaintenanceRespiration(organRespiration);
                    }
                }
            }
        }
        /// <summary>Does the nutrient allocations.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoActualPlantPartioning")]
        override protected void OnDoActualPlantPartioning(object sender, EventArgs e)
        {
            if (Plant.IsEmerged)
            {
                AllocateFixation(Organs.ToArray(), N, NArbitrator);               //Allocate supply of fixable Nitrogen to each organ
                Retranslocation(Organs.ToArray(), N, NArbitrator);      //Allocate supply of retranslocatable N to each organ
                CalculatedNutrientConstrainedDMAllocation(Organs.ToArray());               //Work out how much DM can be assimilated by each organ based on allocated nutrients
                SetDryMatterAllocations(Organs.ToArray());                               //Tell each organ how DM they are getting folling allocation
                SetNitrogenAllocations(Organs.ToArray());                         //Tell each organ how much nutrient they are getting following allocaition
            }
        }
        /// <summary>Determines Nutrient limitations to DM allocations</summary>
        /// <param name="Organs">The organs.</param>
        virtual public void CalculatedNutrientConstrainedDMAllocation(IArbitration[] Organs)
        {
            double PreNStressDMAllocation = DM.Allocated;
            for (int i = 0; i < Organs.Length; i++)
                N.TotalAllocation[i] = N.StructuralAllocation[i] + N.MetabolicAllocation[i] + N.StorageAllocation[i];

            N.Allocated = MathUtilities.Sum(N.TotalAllocation);

            //To introduce functionality for other nutrients we need to repeat this for loop for each new nutrient type
            // Calculate posible growth based on Minimum N requirement of organs
            for (int i = 0; i < Organs.Length; i++)
            {
                double TotalNDemand = N.StructuralDemand[i] + N.MetabolicDemand[i] + N.StorageDemand[i];
                if (N.TotalAllocation[i] >= TotalNDemand)
                    N.ConstrainedGrowth[i] = 100000000; //given high value so where there is no N deficit in organ and N limitation to growth  
                else
                    if (N.TotalAllocation[i] == 0 | Organs[i].MinNconc == 0)
                    N.ConstrainedGrowth[i] = 0;
                else
                    N.ConstrainedGrowth[i] = N.TotalAllocation[i] / Organs[i].MinNconc;
            }

            // Reduce DM allocation below potential if insufficient N to reach Min n Conc or if DM was allocated to fixation
            for (int i = 0; i < Organs.Length; i++)
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
        #endregion

        #region Arbitration step functions

        /// <summary>Does the fixation.</summary>
        /// <param name="Organs">The organs.</param>
        /// <param name="BAT">The bat.</param>
        /// <param name="arbitrator">The option.</param>
        /// <exception cref="System.Exception">Crop is trying to Fix excessive amounts of BAT.  Check partitioning coefficients are giving realistic nodule size and that FixationRatePotential is realistic</exception>
        override public void AllocateFixation(IArbitration[] Organs, BiomassArbitrationType BAT, IArbitrationMethod arbitrator)
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
                            double Respiration = BiomassFixed * RelativeSupply * Organs[i].NFixationCost;  //Calculalte how much respirtion is associated with fixation
                            DM.Respiration[i] = Respiration; // allocate it to the organ
                        }
                        DM.TotalRespiration = MathUtilities.Sum(DM.Respiration);
                    }

                // Work out the amount of biomass (if any) lost due to the cost of N fixation
                if (DM.TotalRespiration <= DM.SinkLimitation)
                { } //Cost of N fixation can be met by DM supply that was not allocated
                else
                {//claw back todays StorageDM allocation to cover the cost
                    double UnallocatedRespirationCost = DM.TotalRespiration - DM.SinkLimitation;
                    if (MathUtilities.IsGreaterThan(DM.TotalStorageAllocation, 0))
                    {
                        double Costmet = 0;
                        for (int i = 0; i < Organs.Length; i++)
                        {
                            double proportion = DM.StorageAllocation[i] / DM.TotalStorageAllocation;
                            double Clawback = Math.Min(UnallocatedRespirationCost * proportion, DM.StorageAllocation[i]);
                            DM.StorageAllocation[i] -= Clawback;
                            Costmet += Clawback;
                        }
                        UnallocatedRespirationCost -= Costmet;
                    }
                    if (UnallocatedRespirationCost == 0)
                    { }//All cost accounted for
                    else
                    {//Remobilise more Non-structural DM to cover the cost
                        if (DM.TotalRetranslocationSupply > 0)
                        {
                            double Costmet = 0;
                            for (int i = 0; i < Organs.Length; i++)
                            {
                                double proportion = DM.RetranslocationSupply[i] / DM.TotalRetranslocationSupply;
                                double DMRetranslocated = Math.Min(UnallocatedRespirationCost * proportion, DM.RetranslocationSupply[i]);
                                DM.Retranslocation[i] += DMRetranslocated;
                                Costmet += DMRetranslocated;
                            }
                            UnallocatedRespirationCost -= Costmet;
                        }
                        if (UnallocatedRespirationCost == 0)
                        { }//All cost accounted for
                        else
                        {//Start cutting into Structural and Metabolic Allocations
                            if ((DM.TotalStructuralAllocation + DM.TotalMetabolicAllocation) > 0)
                            {
                                double Costmet = 0;
                                for (int i = 0; i < Organs.Length; i++)
                                    if ((DM.StructuralAllocation[i] + DM.MetabolicAllocation[i]) > 0)
                                    {
                                        double proportion = (DM.StructuralAllocation[i] + DM.MetabolicAllocation[i]) / (DM.TotalStructuralAllocation + DM.TotalMetabolicAllocation);
                                        double StructualFraction = DM.StructuralAllocation[i] / (DM.StructuralAllocation[i] + DM.MetabolicAllocation[i]);
                                        double StructuralClawback = Math.Min(UnallocatedRespirationCost * proportion * StructualFraction, DM.StructuralAllocation[i]);
                                        double MetabolicClawback = Math.Min(UnallocatedRespirationCost * proportion * (1 - StructualFraction), DM.MetabolicAllocation[i]);
                                        DM.StructuralAllocation[i] -= StructuralClawback;
                                        DM.MetabolicAllocation[i] -= MetabolicClawback;
                                        Costmet += (StructuralClawback + MetabolicClawback);
                                    }
                                UnallocatedRespirationCost -= Costmet;
                            }
                        }
                        if (UnallocatedRespirationCost > 0.0000000001)
                            throw new Exception("Crop is trying to Fix excessive amounts of " + BAT.BiomassType + " Check partitioning coefficients are giving realistic nodule size and that FixationRatePotential is realistic");
                    }
                }
            }
        }
        #endregion
        /// <summary>Writes documentation for this function by adding to the list of documentation tags.</summary>
        /// <param name="tags">The list of tags to add to.</param>
        /// <param name="headingLevel">The level (e.g. H2) of the headings.</param>
        /// <param name="indent">The level of indentation 1, 2, 3 etc.</param>
        public override void Document(List<AutoDocumentation.ITag> tags, int headingLevel, int indent)
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