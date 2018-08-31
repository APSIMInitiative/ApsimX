
namespace Models.Soils.Nutrient
{
    using Core;
    using Models.Functions;
    using System;
    using System.Reflection;

    /// <summary>
    /// # [Name]
    /// Encapsulates a nitrogen flow between mineral N pools.
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Solute))]
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

        /// <summary>
        /// Value of total N flow into destination
        /// </summary>
        public double[] Value { get; set; }
        /// <summary>
        /// Value of total loss
        /// </summary>
        public double[] Loss { get; set; }

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
            double[] source = solutes.GetSolute(Parent.Name);
            if (Value == null)
                Value = new double[source.Length];
            if (Loss == null)
                Loss = new double[source.Length];

            double[] destination = null;
            if (destinationName !=null)
                destination = solutes.GetSolute(destinationName);

            for (int i= 0; i < source.Length; i++)
            {
                double nitrogenFlow = rate.Value(i) * source[i];
                Loss[i]= nitrogenFlow * NLoss.Value(i);  // keep value of loss for use in output
                double nitrogenFlowToDestination = nitrogenFlow - Loss[i];

                if (destination == null && NLoss.Value(i) != 1)
                    throw new Exception("N loss fraction for N flow must be 1 if no destination is specified.");

                source[i] -= nitrogenFlow;
                Value[i] = nitrogenFlowToDestination;  // keep value of flow for use in output
                if (destination != null)
                    destination[i] += nitrogenFlowToDestination;
            }
            solutes.SetSolute(sourceName, SoluteManager.SoluteSetterType.Soil, source);
            if (destination != null)
                solutes.SetSolute(destinationName, SoluteManager.SoluteSetterType.Soil, destination);
        }


    }
}
