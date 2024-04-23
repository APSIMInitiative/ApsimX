using System;
using System.IO;
using APSIM.Shared.Utilities;
using Models.Core;

namespace Models.Utilities
{

    /// <summary>
    /// A class used when trying to compare two models. It allows the caller to write objects
    /// to a text file that can then be easily diffed.
    /// </summary>
    /// <remarks>
    /// Use case: I am moving the patching code from SoilNitrogen to a PatchManager. I then want
    /// to compare two different simulations, one that uses the patching in SoilNitrogen and
    /// another that uses a PatchManager with the Nutrient model. This class lets me write
    /// variables to a text file in both simulations that I can then quickly diff using an
    /// external diff tool.
    /// </remarks>
    [Serializable]
    [ValidParent(ParentType = typeof(Simulation))]
    public class DebugLogger : Model
    {
        private string fileName;

        [Link]
        Simulation simulation = null;

        [Link]
        IClock clock = null;

        /// <summary>At the start of the simulation set up LifeCyclePhases</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            fileName = Path.ChangeExtension(simulation.FileName, $"{simulation.Name}.log");
            if (File.Exists(fileName))
                File.Delete(fileName);
        }

        /// <summary>At the start of the simulation set up LifeCyclePhases</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            WriteObject("Date", clock.Today);
        }

        /// <summary>Write an object to a file.</summary>
        /// <param name="name">The name of the object.</param>
        /// <param name="o">The object to write.</param>
        /// <param name="includePrivates">Include privates when writing an object?</param>
        public void WriteObject(string name, object o, bool includePrivates = false)
        {
            // Open and close the file every time we write because when debugging simulations
            // they can crash with an exception which then means the writer isn't closed.
            using (StreamWriter writer = new StreamWriter(fileName, append: true))
            {
                writer.WriteLine(name);
                writer.WriteLine(ReflectionUtilities.JsonSerialise(o, includePrivates));
            }
        }
    }
}
