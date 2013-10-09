using Models.Core;

namespace Models
{
    public class Summary : Model, ISummary
    {
        // Links
        [Link] private DataStore DataStore = null;
        [Link] private Simulation Simulation = null;
        [Link] private Clock Clock = null;

        /// <summary>
        /// Write a message to the summary
        /// </summary>
        public void WriteMessage(string Message)
        {
            DataStore.WriteMessage(Simulation.Name, Clock.Today, Message, Models.DataStore.ErrorLevel.Information);
        }

        /// <summary>
        /// Write a property to the summary.
        /// </summary>
        public void WriteProperty(string Name, string Value)
        {
            DataStore.WriteProperty(Simulation.Name, Name, Value);
        }

    }
}
