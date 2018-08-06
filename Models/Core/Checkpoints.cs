
namespace Models.Core
{
    using APSIM.Shared.Utilities;
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>B
    /// Saves state of objects and has options to write to a file.
    /// </summary>
    [Serializable]
    public class Checkpoints
    {
        List<Tuple<string, string>> store = new List<Tuple<string, string>>();

        /// <summary>
        /// Save the state of an object under the specified name.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="o"></param>
        public void SaveStateOfObject(string name, object o)
        {
            store.Add(new Tuple<string, string>(name, XmlUtilities.Serialise(o, false)));
        }

        /// <summary>
        /// Write the store to a file.
        /// </summary>
        /// <param name="fileName">Name of file to write to</param>
        public void Write(string fileName)
        {
            using (StreamWriter writer = new StreamWriter(fileName))
            {
                foreach (Tuple<string, string> item in store)
                {
                    writer.WriteLine(item.Item1);
                    writer.WriteLine(item.Item2);
                }
            }
            store.Clear();
        }
    }
}
