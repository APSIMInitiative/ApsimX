
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
                    destinations[j].C[i] += carbonFlowToDestination[j];
                    destinations[j].N[i] += nitrogenFlowToDestination[j];
                }

                source.C[i] -= carbonFlow;
                source.N[i] -= nitrogenFlow;
                double TotalNitrogenFlowToDestinations = MathUtilities.Sum(nitrogenFlowToDestination);
                if (TotalNitrogenFlowToDestinations <= nitrogenFlow)
                        solutes.AddToLayer(i, "NH4", nitrogenFlow - TotalNitrogenFlowToDestinations);
                    else
                        throw new NotImplementedException();
                
            }
        }


    }
}
