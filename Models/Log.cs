using System;
using System.IO;
using Models.Core;

namespace Models
{
    /// <summary>
    /// A low level log component that writes state / parameter variables to a text file.
    /// </summary>
    [Serializable]
    public class Log : Model
    {
        [Link]
        IClock Clock = null;

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
