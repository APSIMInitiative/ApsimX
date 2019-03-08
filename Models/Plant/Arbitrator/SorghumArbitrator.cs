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
        public double WatSupply { get; set; }

        /// <summary>Gets the water demand.</summary>
        /// <value>The water demand.</value>
        public double NDemand { get; private set; }

        /// <summary>Gets the water demand.</summary>
        /// <value>The water demand.</value>
        public double NSupply { get; private set; }

        /// <summary>Gets the water demand.</summary>
        /// <value>The water demand.</value>
        public double NMassFlowSupply { get; private set; }

        /// <summary>Gets the water demand.</summary>
        /// <value>The water demand.</value>
        public double NDiffusionSupply { get; private set; }
        
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

        /// <summary>
        /// Calculate the potential N uptake for today. Should return null if crop is not in the ground.
        /// </summary>
        public override List<Soils.Arbitrator.ZoneWaterAndN> GetNitrogenUptakeEstimates(SoilState soilstate)
        {
            if (Plant.IsEmerged)
            {
                //this function is called 4 times as part of estimates
                //shouldn't set public variables in here
                var nSupply = 0.0;//NOTE: This is in kg, not kg/ha, to arbitrate N demands for spatial simulations.
                var grainDemand = N.StructuralDemand[0] + N.MetabolicDemand[0];
                var leafStructuralDemand = N.StructuralDemand[2];
                var structuralDemand = MathUtilities.Sum(N.StructuralDemand);
                var metabolicDemand = MathUtilities.Sum(N.MetabolicDemand);

                //double NDemand = (N.TotalPlantDemand - N.TotalReallocation) / kgha2gsm * Plant.Zone.Area; //NOTE: This is in kg, not kg/ha, to arbitrate N demands for spatial simulations.
                double nDemand = (structuralDemand + metabolicDemand - grainDemand - leafStructuralDemand - N.TotalReallocation) * Plant.Zone.Area; //NOTE: This is in kg, not kg/ha, to arbitrate N demands for spatial simulations.

                for (int i = 0; i < Organs.Count; i++)
                    N.UptakeSupply[i] = 0;

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
                    var root = Organs[1] as Root;
                    //Get Nuptake supply from each organ and set the PotentialUptake parameters that are passed to the soil arbitrator
                    double[] organNO3Supply = new double[zone.NO3N.Length];
                    double[] organNH4Supply = new double[zone.NH4N.Length];
                    root.CalculateNitrogenSupply(zone, ref organNO3Supply, ref organNH4Supply);

                    //new code
                    double[] diffnAvailable = new double[root.Diffusion.Length];
                    for(var i = 0; i < root.Diffusion.Length; ++i)
                    {
                        diffnAvailable[i] = root.Diffusion[i] - root.MassFlow[i];
                    }
                    var totalMassFlow = MathUtilities.Sum(root.MassFlow);
                    var totalDiffusion = MathUtilities.Sum(diffnAvailable);

                    var potentialSupply = totalMassFlow + totalDiffusion;
                    var dltt = root.DltThermalTime.Value();
                    var actualDiffusion = 0.0;
                    var actualMassFlow = dltt > 0 ? totalMassFlow : 0.0;
                    var maxDiffusionConst = root.MaxDiffusion.Value();

                    if (totalMassFlow < nDemand && dltt > 0.0)
                    {
                        actualDiffusion = MathUtilities.Bound(nDemand - totalMassFlow, 0.0, totalDiffusion);
                        actualDiffusion = MathUtilities.Divide(actualDiffusion, maxDiffusionConst, 0.0);

                        var nsupplyFraction = root.NSupplyFraction.Value();
                        var maxRate = root.MaxNUptakeRate.Value();

                        var maxUptakeRateFrac = Math.Min(1.0, (potentialSupply / root.NSupplyFraction.Value())) * root.MaxNUptakeRate.Value();
                        var maxUptake = maxUptakeRateFrac * dltt - actualMassFlow;
                        actualDiffusion = Math.Min(actualDiffusion, maxUptake);
                    }

                    nSupply = 0.0;
                    //adjust diffusion values proportionally
                    for (int layer = 0; layer < organNO3Supply.Length; layer++)
                    {
                        var massFlowLayerFraction = MathUtilities.Divide(root.MassFlow[layer], totalMassFlow, 0.0);
                        var diffusionLayerFraction = MathUtilities.Divide(diffnAvailable[layer], totalDiffusion, 0.0);
                        organNH4Supply[layer] = massFlowLayerFraction * root.MassFlow[layer];
                        organNO3Supply[layer] = massFlowLayerFraction * root.MassFlow[layer] +
                            diffusionLayerFraction * actualDiffusion;
                    }

                    //originalcode
                    UptakeDemands.NO3N = MathUtilities.Add(UptakeDemands.NO3N, organNO3Supply); //Add uptake supply from each organ to the plants total to tell the Soil arbitrator
                    UptakeDemands.NH4N = MathUtilities.Add(UptakeDemands.NH4N, organNH4Supply);
                    N.UptakeSupply[1] += MathUtilities.Sum(organNO3Supply) * kgha2gsm * zone.Zone.Area / Plant.Zone.Area;
                    nSupply += MathUtilities.Sum(organNO3Supply) * zone.Zone.Area;
                    zones.Add(UptakeDemands);
                }

                if (nSupply > nDemand)
                {
                    //Reduce the PotentialUptakes that we pass to the soil arbitrator
                    double ratio = Math.Min(1.0, nDemand / nSupply);
                    foreach (ZoneWaterAndN UptakeDemands in zones)
                    {
                        UptakeDemands.NO3N = MathUtilities.Multiply_Value(UptakeDemands.NO3N, ratio);
                        UptakeDemands.NH4N = MathUtilities.Multiply_Value(UptakeDemands.NH4N, ratio);
                    }
                }
                return zones;
            }
            return null;
        }

        /// <summary>
        /// Set the sw uptake for today
        /// </summary>
        public override void SetActualNitrogenUptakes(List<ZoneWaterAndN> zones)
        {
            if (Plant.IsEmerged)
            {
                // Calculate the total no3 and nh4 across all zones.
                NSupply = 0;//NOTE: This is in kg, not kg/ha, to arbitrate N demands for spatial simulations.
                NMassFlowSupply = 0.0;
                NDiffusionSupply = 0.0;
                var supply = 0.0;
                foreach (ZoneWaterAndN Z in zones)
                {
                    supply += MathUtilities.Sum(Z.NO3N);
                    NMassFlowSupply += MathUtilities.Sum(Z.NH4N);
                    NSupply += supply * Z.Zone.Area;

                    for(int i = 0; i < Z.NH4N.Length; ++i)
                        Z.NH4N[i] = 0;
                }
                NDiffusionSupply = supply - NMassFlowSupply;

                //Reset actual uptakes to each organ based on uptake allocated by soil arbitrator and the organs proportion of potential uptake
                for (int i = 0; i < Organs.Count; i++)
                    N.UptakeSupply[i] = NSupply / Plant.Zone.Area * N.UptakeSupply[i] / N.TotalUptakeSupply / kgha2gsm;

                //Allocate N that the SoilArbitrator has allocated the plant to each organ
                AllocateUptake(Organs.ToArray(), N, NArbitrator);
                Plant.Root.DoNitrogenUptake(zones);
            }
        }

        #endregion

        #region Plant interface methods

        /// <summary>Does the retranslocation.</summary>
        /// <param name="Organs">The organs.</param>
        /// <param name="BAT">The bat.</param>
        /// <param name="arbitrator">The option.</param>
        override public void Retranslocation(IArbitration[] Organs, BiomassArbitrationType BAT, IArbitrationMethod arbitrator)
        {
            if (BAT.TotalRetranslocationSupply > 0.00000000001)
            {
                var nArbitrator = arbitrator as SorghumArbitratorN;
                if (nArbitrator != null)
                {
                    nArbitrator.DoRetranslocation(Organs, BAT);
                }
                else
                {
                    double BiomassRetranslocated = 0;
                    if (BAT.TotalRetranslocationSupply > 0.00000000001)
                    {
                        arbitrator.DoAllocation(Organs, BAT.TotalRetranslocationSupply, ref BiomassRetranslocated, BAT);
                        // Then calculate how much DM (and associated biomass) is retranslocated from each supplying organ based on relative retranslocation supply
                        for (int i = 0; i < Organs.Length; i++)
                            if (BAT.RetranslocationSupply[i] > 0.00000000001)
                            {
                                double RelativeSupply = BAT.RetranslocationSupply[i] / BAT.TotalRetranslocationSupply;
                                BAT.Retranslocation[i] += BiomassRetranslocated * RelativeSupply;
                            }
                    }
                }
            }
        }

        #endregion

        #region Arbitration step functions
       

        #endregion

    }
}