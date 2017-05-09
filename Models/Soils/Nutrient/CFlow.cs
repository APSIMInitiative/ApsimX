
namespace Models.Soils.Nutrient
{
    using Core;
    using Models.PMF.Functions;
    using System;
    using APSIM.Shared.Utilities;

    /// <summary>
    /// Encapsulates a carbon flow between pools.
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(NutrientPool))]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ViewName("UserInterface.Views.GridView")]
    public class CarbonFlow : Model
    {
        private NutrientPool destination = null;

        [ChildLinkByName]
        private IFunction Rate = null;

        [ChildLinkByName]
        private IFunction CO2Efficiency = null;

        [Link]
        private SoluteManager solutes = null;

        /// <summary>
        /// Name of destination pool
        /// </summary>
        [Description("Name of destination pool")]
        public string destinationName { get; set; }

        /// <summary>Performs the initial checks and setup</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            destination = Apsim.Find(this, destinationName) as NutrientPool;
            if (destination == null)
                throw new Exception("Cannot find destination pool with name: " + destinationName);
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
            for (int i= 0; i < source.C.Length; i++)
            {
                double carbonFlow = Rate.Value(i) * source.C[i];
                double nitrogenFlow = MathUtilities.Divide(carbonFlow, source.CNRatio[i],0);
                double carbonFlowToDestination = carbonFlow * CO2Efficiency.Value(i);
                double nitrogenFlowToDestination = carbonFlowToDestination / destination.CNRatio[i];
                source.C[i] -= carbonFlow;
                source.N[i] -= nitrogenFlow;
                destination.C[i] += carbonFlowToDestination;
                destination.N[i] += nitrogenFlowToDestination;
                if (nitrogenFlowToDestination <= nitrogenFlow)
                    solutes.AddToLayer(i, "NH4", nitrogenFlow - nitrogenFlowToDestination);
                else
                    throw new NotImplementedException();
            }
        }


    }
}
