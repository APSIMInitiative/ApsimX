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
    public class SorghumArbitrator: BaseArbitrator
    {
        #region Links and Input parameters

        ///// <summary>The method used to arbitrate N allocations</summary>
        //[ChildLinkByName]
        //private IArbitrationMethod NArbitrator = null;

        ///// <summary>The method used to arbitrate N allocations</summary>
        //[ChildLinkByName]
        //private IArbitrationMethod DMArbitrator = null;

        ///// <summary>The kgha2gsm</summary>
        //private const double kgha2gsm = 0.1;

        ///// <summary>The list of organs</summary>
        //private List<IArbitration> Organs = new List<IArbitration>();

        #endregion

        #region Main outputs
        /// <summary>Gets the water Supply.</summary>
        /// <value>The water supply.</value>
        [XmlIgnore]
        public double WatSupply { get; set; }

        #endregion
        private List<IModel> uptakeModels = null;
        private List<IModel> zones = null;

        /// <summary>Called at the start of the simulation.</summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">Dummy event data.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            uptakeModels = Apsim.ChildrenRecursively(Parent, typeof(IUptake));
            zones = Apsim.ChildrenRecursively(this.Parent, typeof(Zone));
        }

        #region IUptake interface
        [EventSubscribe("DoWaterArbitration")]
        private void OnDoWaterArbitration(object sender, EventArgs e)
        {
            //SoilState InitialSoilState = new SoilState(this.Parent);
            //InitialSoilState.Initialise(zones);
            //GetWaterUptake(InitialSoilState);
        }


        ///// <summary>Called at water arbitration.</summary>
        ///// <summary>WaterSupply is being calculated as the same as what is uptaken.</summary>
        ///// <param name="soilstate">The sender of the event</param>
        //public void GetWaterUptake(SoilState soilstate)
        //{
        //    if (Plant.IsAlive)
        //    {
        //        // Get all water supplies.
        //        double waterSupply = 0;  //NOTE: This is in L, not mm, to arbitrate water demands for spatial simulations.

        //        List<double[]> supplies = new List<double[]>();
        //        List<Zone> zones = new List<Zone>();
        //        foreach (ZoneWaterAndN zone in soilstate.Zones)
        //            foreach (IOrgan o in Organs)
        //                if (o is IWaterNitrogenUptake)
        //                {
        //                    double[] organSupply = (o as IWaterNitrogenUptake).CalculateWaterSupply(zone);
        //                    if (organSupply != null)
        //                    {
        //                        supplies.Add(organSupply);
        //                        zones.Add(zone.Zone);
        //                        waterSupply += MathUtilities.Sum(organSupply) * zone.Zone.Area;
        //                    }
        //                }

        //        // Calculate total water demand.
        //        double waterDemand = 0; //NOTE: This is in L, not mm, to arbitrate water demands for spatial simulations.
        //        foreach (IArbitration o in Organs)
        //            if (o is IHasWaterDemand)
        //                waterDemand += (o as IHasWaterDemand).CalculateWaterDemand() * Plant.Zone.Area;

        //        // Calculate demand / supply ratio.
        //        double fractionUsed = 0;
        //        if (waterSupply > 0)
        //            fractionUsed = Math.Min(1.0, waterDemand / waterSupply);

        //        WatSupply = waterSupply;
        //    }
        //}
        #endregion

        #region Plant interface methods

        /// <summary>Does the retranslocation.</summary>
        /// <param name="Organs">The organs.</param>
        /// <param name="BAT">The bat.</param>
        /// <param name="arbitrator">The option.</param>
        override public void Retranslocation(IArbitration[] Organs, BiomassArbitrationType BAT, IArbitrationMethod arbitrator)
        {
            double BiomassRetranslocated = 0;
            if (BAT.TotalRetranslocationSupply > 0.00000000001)
            {
                (arbitrator as SorghumArbitratorN).DoRetranslocation(Organs, BAT.TotalRetranslocationSupply, ref BiomassRetranslocated, BAT);
                // Then calculate how much N (and associated biomass) is retranslocated from each supplying organ based on relative retranslocation supply
                for (int i = 0; i < Organs.Length; i++)
                    if (BAT.RetranslocationSupply[i] > 0.00000000001)
                    {
                        double RelativeSupply = BAT.RetranslocationSupply[i] / BAT.TotalRetranslocationSupply;
                        BAT.Retranslocation[i] += BiomassRetranslocated * RelativeSupply;
                    }
            }
        }

        #endregion

        #region Arbitration step functions
        /// <summary>Calculate all of the Organ DM Demands </summary>
        public override void PotentialDMAllocation()
        {
            base.PotentialDMAllocation();
            
            //need to recalc Actual area after being allocated potential DM.
            //Initial dltLAI is calculated assuming enough biomass is available
            //need to adjust dltLAI to maximum availabel with given biomass.
            //need to do this before N is calculated.
            

            //also need to calculate senescence before calculating NDemand - which is definied within PotentialArbitration
            

        }


        #endregion

    }
}