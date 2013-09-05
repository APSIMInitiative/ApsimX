using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Model.Core;

namespace Model.Components
{
    public class Summary : ISummary
    {
        // Links
        [Link] private DataStore DataStore = null;
        [Link] private ISimulation Simulation = null;
        [Link] private Clock Clock = null;

        /// <summary>
        /// Write a message to the summary
        /// </summary>
        public void WriteMessage(string Message)
        {
            DataStore.WriteMessage(Simulation.Name, Clock.Today, Message);
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
