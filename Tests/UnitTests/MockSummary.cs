using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnitTests
{
    [Serializable]
    class MockSummary : Model,ISummary
    {
        public static List<string> messages = new List<string>();

        public MockSummary()
        {
            messages.Clear();
        }

        /// <summary>Performs the initialisation procedures for this species (set DM, N, LAI, etc.).</summary>
        /// <param name="sender">The sender model</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            messages.Clear();
        }

        public void WriteMessage(IModel model, string message)
        {
            messages.Add(message);
        }

        public void WriteMessage(object model, string message)
        {
            messages.Add(message);
        }

        public void WriteWarning(IModel model, string message)
        {
            messages.Add("WARNING: " + message);
        }

        public void WriteWarning(object model, string message)
        {
            messages.Add("WARNING: " + message);
        }
        public void WriteError(IModel model, string message)
        {
            messages.Add("ERROR: " + message);
        }

        public void WriteMessage(IModel model, string message, MessageType messageType)
        {
            switch (messageType)
            {
                case MessageType.Error:
                    WriteError(model, message);
                    break;
                case MessageType.Warning:
                    WriteWarning(model, message);
                    break;
                case MessageType.Information:
                    WriteMessage(model, message);
                    break;
                default:
                    messages.Add(message);
                    break;
            }
        }
    }
}
