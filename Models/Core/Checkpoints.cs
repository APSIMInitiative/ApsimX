
namespace Models.Core
{
    using APSIM.Shared.Utilities;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using System.Linq;

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

        /// <summary>
        /// Write a message line into checkpoint file
        /// </summary>
        /// <param name="message"></param>
        /// <param name="name"></param>
        public void WriteMessageLine(string name, string message)
        {
            if (CheckPointFile == null)
                MakeCheckPointFile(name);
            CheckPointFile.WriteLine(message);
        }

        
        private StreamWriter CheckPointFile = null;

        /// <summary>
        /// Adds the status of the model to the CheckPointFile
        /// </summary>
        /// <param name="name"></param>
        /// <param name="o"></param>
        public void AddToCheckpointFile(string name, object o)
        {
            if (CheckPointFile == null)
                MakeCheckPointFile(name);
            AppendToFile(o);
        }

        ///<summary> Makes a checkpoint file instance to write to </summary>
        private void MakeCheckPointFile(string name)
        {
            string fileName = Path.Combine(Path.GetDirectoryName(simulations.FileName), name);
            fileName = Path.ChangeExtension(fileName, ".checkpoint.json");
            CheckPointFile = new StreamWriter(fileName);
        }

        ///<summary> Appends current checkpoint to checkpoint file </summary>
        private void AppendToFile(object o)
        {
            CheckPointFile.WriteLine(JsonConvert.SerializeObject(o, Formatting.Indented,
                    new JsonSerializerSettings
                    {
                        ContractResolver = new DynamicContractResolver(),
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                    }));
        }


        ///<summary> Custom Contract resolver to stop deseralization of Parent properties </summary>
        private class DynamicContractResolver : DefaultContractResolver
        {
            public DynamicContractResolver()
            {
            }

            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                IList<JsonProperty> properties = base.CreateProperties(type, memberSerialization);

                // only serializer properties that start with the specified character
                properties =
                    properties.Where(p => p.PropertyName != "Parent").ToList();

                return properties;
            }

        }
    }
}
