using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;
using System.IO;

namespace Models
{
    /// <summary>
    /// A low level log component that writes state / parameter variables to a text file.
    /// </summary>
    [Serializable]
    public class Log : Model
    {
        [Link]
        Clock Clock = null;

        [Link]
        Simulation Simulation = null;

        private StreamWriter Writer;

        /// <summary>
        /// Initialise the model.
        /// </summary>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            if (Simulation.FileName != null)
            {
                string fileName = Path.ChangeExtension(Simulation.FileName, ".log");
                Writer = new StreamWriter(fileName);
            }
        }

        /// <summary>
        /// Simulation has completed.
        /// </summary>
        [EventSubscribe("Completed")]
        private void OnSimulationCompleted(object sender, EventArgs e)
        {
            Writer.Close();
        }

        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            Writer.WriteLine("Date: " + Clock.Today.ToString());
        }

    }
}
