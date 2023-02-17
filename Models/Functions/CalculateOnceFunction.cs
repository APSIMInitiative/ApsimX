using System;
using Models.Core;

namespace Models.Functions
{
    /// <summary>
    /// Returns the a value calculated at the start of the simulation and then held constant
    /// </summary>
    [Serializable]
    [Description("Returns a value that is calculated at SimulationCommencing and then held constant")]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class CalculateOnceFunction : Model, IFunction
    {
        /// <summary>The _ value</summary>
        private double _Value = 0;


        /// <summary>The value to calculate and hold</summary>
        [Link(Type = LinkType.Child)]
        IFunction ValueToHold = null;


        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnCommencing(object sender, EventArgs e)
        {
            _Value = ValueToHold.Value();
        }

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            return _Value;
        }
    }
}