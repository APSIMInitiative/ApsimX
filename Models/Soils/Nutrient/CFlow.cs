
namespace Models.Soils.Nutrient
{
    using Core;
    using Models.PMF.Functions;
    using System;
    using APSIM.Shared.Utilities;
    using System.Collections.Generic;

    /// <summary>
    /// Encapsulates a carbon flow between pools.
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(NutrientPool))]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ViewName("UserInterface.Views.GridView")]
    public class CarbonFlow : Model
    {
        private List<NutrientPool> destinations = new List<NutrientPool>();

        [ChildLinkByName]
        private IFunction Rate = null;

        [ChildLinkByName]
        private IFunction CO2Efficiency = null;

        [Link]
        private SoluteManager solutes = null;

        /// <summary>
        /// Name of destination pool
        /// </summary>
        [Description("Names of destination pools (CSV)")]
        public string[] destinationNames { get; set; }
        /// <summary>
        /// Fractions for each destination pool
        /// </summary>
        [Description("Fractions of flow to each pool (CSV)")]
        public double[] destinationFraction { get; set; }

        /// <summary>Performs the initial checks and setup</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            foreach (string destinationName in destinationNames)
            {
                NutrientPool destination = Apsim.Find(this, destinationName) as NutrientPool;
                if (destination == null)
                    throw new Exception("Cannot find destination pool with name: " + destinationName);
                destinations.Add(destination);
            }
        }

        /// <summary>
        /// Get the information on potential residue decomposition - perform daily calculations as part of this.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoSoilOrganicMatter")]
        private void OnDoSoilOrganicMatter(object sender, EventArgs e)
        {
            NutrientPool source = Parent as NutrientPool;
            double[] NH4 = solutes.GetSolute("NH4");
            double[] NO3 = solutes.GetSolute("NO3");

            for (int i = 0; i < source.C.Length; i++)
            {
                double carbonFlow = Rate.Value(i) * source.C[i];
                double nitrogenFlow = MathUtilities.Divide(carbonFlow, source.CNRatio[i], 0);

                double[] carbonFlowToDestination = new double [destinations.Count];
                double[] nitrogenFlowToDestination = new double[destinations.Count];

                for (int j = 0; j < destinations.Count; j++)
                {
                    carbonFlowToDestination[j] = carbonFlow * CO2Efficiency.Value(i) * destinationFraction[j];
                    nitrogenFlowToDestination[j] = carbonFlowToDestination[j] / destinations[j].CNRatio[i];
                }

                double TotalNitrogenFlowToDestinations = MathUtilities.Sum(nitrogenFlowToDestination);
                double NSupply = nitrogenFlow + NO3[i] + NH4[i];
                double NSupplyFactor = 1.0;
                if (TotalNitrogenFlowToDestinations > NSupply)
                   NSupplyFactor = MathUtilities.Bound(MathUtilities.Divide(NO3[i] + NH4[i], TotalNitrogenFlowToDestinations - nitrogenFlow, 1.0),0.0,1.0);

                for (int j = 0; j < destinations.Count; j++)
                {
                    carbonFlowToDestination[j] *= NSupplyFactor;
                    nitrogenFlowToDestination[j] *= NSupplyFactor;
                }

                source.C[i] -= carbonFlow * NSupplyFactor;
                source.N[i] -= nitrogenFlow * NSupplyFactor;

                for (int j = 0; j < destinations.Count; j++)
                {
                    destinations[j].C[i] += carbonFlowToDestination[j] * NSupplyFactor;
                    destinations[j].N[i] += nitrogenFlowToDestination[j] * NSupplyFactor;
                }

                if (TotalNitrogenFlowToDestinations * NSupplyFactor <= nitrogenFlow)
                    NH4[i] += nitrogenFlow - TotalNitrogenFlowToDestinations * NSupplyFactor;
                else
                {
                    double NDeficit = TotalNitrogenFlowToDestinations * NSupplyFactor - nitrogenFlow;
                    double NH4Immobilisation = Math.Min(NH4[i], NDeficit);
                    NH4[i] -= NH4Immobilisation;
                    NDeficit -= NH4Immobilisation;

                    double NO3Immobilisation = Math.Min(NO3[i], NDeficit);
                    NO3[i] -= NO3Immobilisation;
                    NDeficit -= NO3Immobilisation;

                    if (MathUtilities.IsGreaterThan(NDeficit, 0.0))
                        throw new Exception("Insufficient mineral N for immobilisation demand for C flow " + Name);
                }

            }
            solutes.SetSolute("NH4", NH4);
            solutes.SetSolute("NO3", NO3);
        }


    }
}
