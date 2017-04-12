
namespace Models.Soils.Nutrient
{
    using Core;
    using Models.PMF.Functions;
    using System;
    using System.Reflection;

    /// <summary>
    /// Encapsulates a nitrogen flow between mineral N pools.
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Nutrient))]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ViewName("UserInterface.Views.GridView")]
    public class NFlow : Model
    {
        [Link]
        private IFunction rate = null;

        [Link]
        private IFunction NLoss = null;

        [Link]
        private SoluteManager solutes = null;

        [Link]
        private Soil soil = null;

        /// <summary>
        /// Name of source pool
        /// </summary>
        [Description("Name of source pool")]
        public string sourceName { get; set; }

        /// <summary>
        /// Name of destination pool
        /// </summary>
        [Description("Name of destination pool")]
        public string destinationName { get; set; }

        /// <summary>
        /// Get the information on potential residue decomposition - perform daily calculations as part of this.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoSoilOrganicMatter")]
        private void OnDoSoilOrganicMatter(object sender, EventArgs e)
        {

            double[] source = solutes.GetSolute(sourceName);

            double[] destination = solutes.GetSolute(destinationName);

            for (int i= 0; i < soil.Thickness.Length; i++)
            {
                double nitrogenFlow = rate.Value(i) * source[i];
                double nitrogenFlowToDestination = nitrogenFlow * (1-NLoss.Value(i));

                source[i] -= nitrogenFlow;
                destination[i] += nitrogenFlowToDestination;
            }
            solutes.SetSolute(sourceName, source);
            solutes.SetSolute(destinationName, destination);
        }


    }
}
