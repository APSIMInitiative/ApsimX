
namespace Models.Core
{
    using APSIM.Shared.Utilities;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Newtonsoft.Json;

    /// <summary>B
    /// Saves state of objects and has options to write to a file.
    /// </summary>
    [Serializable]
    public class Checkpoints
    {
        private Simulations simulations = null;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="sims"></param>
        public Checkpoints(Simulations sims)
        {
            simulations = sims;
        }

        /// <summary>
        /// Save the state of an object under the specified name.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="o"></param>
        public void SaveStateOfObject(string name, object o)
        {
            if (o is IModel)
            {
                var simulation = Apsim.Parent(o as IModel, typeof(Simulation));
                WriteToFile(name, simulation);
            }
            else
                WriteToFile(name, o);
        }

        private void WriteToFile(string name, object o)
        {
            string fileName = Path.Combine(Path.GetDirectoryName(simulations.FileName), name);
            fileName = Path.ChangeExtension(fileName, ".checkpoint.json");
            using (StreamWriter writer = new StreamWriter(fileName))
            {
                writer.WriteLine(JsonConvert.SerializeObject(o, Formatting.Indented,
                    new JsonSerializerSettings
                    {
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                    }));
            }
        }
    }
}
