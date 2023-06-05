using System;
using System.Collections.Generic;
using System.Globalization;
using Models.Core;

namespace Models.Functions
{

    /// <summary>
    /// A function that accumulates values from child functions
    /// </summary>
    [Serializable]
    [Description("Keeps track of a variable")]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class TrackerFunction : Model, IFunction
    {
        /// <summary>Values we have kept</summary>
        private List<double> variableValues = new List<double>();

        /// <summary>Reference values we have kept</summary>
        private List<double> referenceValues = new List<double>();

        /// <summary>Should we be keeping track of the variable?</summary>
        private bool inTrackingWindow = false;

        /// <summary>Link to an event service.</summary>
        [Link]
        private IEvent events = null;

        /// <summary>The variable to track</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction variable = null;

        /// <summary>The variable to track</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction referenceVariable = null;

        /// <summary>The statistic to return e.g. value back 300</summary>
        [Description("The statistic to return e.g. value back 300")]
        public string Statistic { get; set; }

        /// <summary>Event name to start accumulation</summary>
        [Description("Event name to start accumulation")]
        public string StartEventName { get; set; }

        /// <summary>Event name to stop accumulation</summary>
        [Description("Event name to stop accumulation")]
        public string EndEventName { get; set; }

        /// <summary>Gets the value.</summary>
        public double Value(int arrayIndex = -1)
        {
            if (referenceValues.Count == 0)
                return 0;
            if (Statistic.StartsWith("value back "))
            {
                double accumulationTarget = Convert.ToDouble(Statistic.Replace("value back ", ""), CultureInfo.InvariantCulture);

                // Go backwards through referenceValues until we reach our accumulation target.
                double accumulationValue = 0;
                for (int i = referenceValues.Count - 1; i >= 0; i--)
                {
                    accumulationValue += referenceValues[i];
                    if (accumulationValue >= accumulationTarget)
                        return variableValues[i];
                }
            }
            else
                throw new Exception("Invalid statistic found in TrackerFunction: " + Statistic);

            return 0;
        }

        /// <summary>
        /// Connect event handlers.
        /// </summary>
        /// <param name="sender">Sender object..</param>
        /// <param name="args">Event data.</param>
        [EventSubscribe("SubscribeToEvents")]
        private void OnConnectToEvents(object sender, EventArgs args)
        {
            events.Subscribe(StartEventName, OnStartEvent);
            events.Subscribe(EndEventName, OnEndEvent);
        }

        /// <summary>
        /// Invoked when simulation has completed.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        [EventSubscribe("DoManagementCalculations")]
        private void OnDoDailyTracking(object sender, EventArgs e)
        {
            if (inTrackingWindow)
            {
                variableValues.Add(variable.Value());
                referenceValues.Add(referenceVariable.Value());
            }
        }

        /// <summary>
        /// Called to begin keeping track of variable
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnStartEvent(object sender, EventArgs e)
        {
            variableValues.Clear();
            referenceValues.Clear();
            inTrackingWindow = true;
        }

        /// <summary>
        /// Called to end keeping track of variable
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void OnEndEvent(object sender, EventArgs e)
        {
            inTrackingWindow = false;
        }


    }
}
