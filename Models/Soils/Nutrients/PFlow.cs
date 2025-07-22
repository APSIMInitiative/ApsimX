using System;
using APSIM.Core;
using Models.Core;
using Models.Functions;

namespace Models.Soils.Nutrients
{

    /// <summary>
    /// Encapsulates a nitrogen flow between mineral N pools.
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Solute))]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ViewName("UserInterface.Views.PropertyView")]
    public class PFlow : Model
    {
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction rate = null;

        [NonSerialized]
        private ISolute sourceSolute = null;
        [NonSerialized]
        private ISolute destinationSolute = null;

        /// <summary>
        /// Value of total P flow into destination
        /// </summary>
        public double[] Value { get; set; }

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
        /// Start of simulation - find source and destination solute.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            sourceSolute = FindInScope<ISolute>(sourceName);
            destinationSolute = FindInScope<ISolute>(destinationName);
        }

        /// <summary>
        /// Calculate Flows
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoSoilOrganicMatter")]
        private void OnDoSoilOrganicMatter(object sender, EventArgs e)
        {
            if (sourceSolute != null)
            {
                double[] source = sourceSolute.kgha;
                int numLayers = source.Length;
                if (Value == null)
                    Value = new double[source.Length];

                double[] destination = null;
                if (destinationName != null)
                    destination = destinationSolute.kgha;

                for (int i = 0; i < numLayers; i++)
                {
                    double phosphorusFlow = 0;
                    if (source[i] > 0)
                        phosphorusFlow = rate.Value(i) * source[i];

                    source[i] -= phosphorusFlow;
                    Value[i] = phosphorusFlow;
                    destination[i] += phosphorusFlow;
                }
                sourceSolute.SetKgHa(SoluteSetterType.Soil, source);
                destinationSolute.SetKgHa(SoluteSetterType.Soil, destination);
            }
        }
    }
}
