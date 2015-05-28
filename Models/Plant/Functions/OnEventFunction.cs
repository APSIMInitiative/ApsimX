using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Phen;
using System.Reflection;
namespace Models.PMF.Functions
{
    /// <summary>
    /// Returns the value of PreEventValue child function from Initialisation to SetEvent, PostEventValue from ReSetEvent and PreEventValue again from ReSetEvent to the next SetEvent
    /// </summary>
    [Serializable]
    [Description("Returns the value of PreEventValue child function from Initialisation to SetEvent, PostEventValue from ReSetEvent and PreEventValue again from ReSetEvent to the next SetEvent")]
    [ViewName("UserInterface.Views.GenericPMFView")]
    [PresenterName("UserInterface.Presenters.GenericPMFPresenter")]
    public class OnEventFunction : Model, IFunction
    {
        /// <summary>The _ value</summary>
        private double _Value = 0;

        /// <summary>The set event</summary>
        public string SetEvent = "";
        /// <summary>The re set event</summary>
        public string ReSetEvent = "";


        /// <summary>The pre event value</summary>
        [Link]
        IFunction PreEventValue = null;
        /// <summary>The post event value</summary>
        [Link]
        IFunction PostEventValue = null;

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            _Value = PreEventValue.Value;
        }

        /// <summary>Called when [phase changed].</summary>
        /// <param name="PhaseChange">The phase change.</param>
        [EventSubscribe("PhaseChanged")]
        private void OnPhaseChanged(PhaseChangedType PhaseChange)
        {
            if (PhaseChange.EventStageName == SetEvent)
                OnSetEvent();

            if (PhaseChange.EventStageName == ReSetEvent)
                OnReSetEvent();
        }

        /// <summary>Called when [re set event].</summary>
        public void OnReSetEvent()
        {
            _Value = PreEventValue.Value;
        }

        /// <summary>Called when [set event].</summary>
        public void OnSetEvent()
        {
            _Value = PostEventValue.Value;
        }


        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value
        {
            get
            {
                return _Value;
            }
        }

    }

}