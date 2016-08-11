
namespace Models.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using APSIM.Shared.Utilities;
    using System.Xml;
    using System.IO;
    using System.Reflection;
    using System.Xml.Serialization;
    using System.Xml.Schema;
    using Models;


    /// <summary>
    /// A class for reading and writing the .apsimx file format.
    /// </summary>
    public class FileFormat
    {
        /// <summary>All known model types</summary>
        private List<Type> modelTypes = null;

        /// <summary>Constructor</summary>
        public FileFormat(List<Type> types)
        {
            modelTypes = new List<Type>();
            types.AddRange(Assembly.GetExecutingAssembly().GetTypes());
            foreach (Type t in types)
            {
                if (t.IsPublic && !t.IsInterface &&  t.FullName.StartsWith("Models.") && t.BaseType == typeof(Model) &&
                    t.FullName != "Models.Script")
                    modelTypes.Add(t);
            }
        }

        /// <summary>Convert the specified model to XML</summary>
        /// <param name="rootNode">The root model to serialise.</param>
        /// <returns>The XML</returns>
        public string WriteXML(ModelWrapper rootNode)
        {
            StringWriter s = new StringWriter();
            APSIMFileWriter writer = new APSIMFileWriter(s);
            writer.Formatting = Formatting.Indented;
            XmlUtilities.SerialiseWithOptions(rootNode, false, null, modelTypes.ToArray(), writer);
            return s.ToString();
        }

        /// <summary>Write the specified simulation set to the specified filename</summary>
        /// <param name="model">The model to write.</param>
        /// <param name="fileName">Name of the file.</param>
        public void WriteFile(Simulations model, string fileName)
        {
            //string tempFileName = Path.Combine(Path.GetTempPath(), Path.GetFileName(fileName));
            //StreamWriter writer = new StreamWriter(tempFileName);
            //writer.Write(WriteXML(model));
            //writer.Close();

            //// If we get this far without an exception then copy the tempfilename over our filename,
            //// creating a backup (.bak) in the process.
            //string bakFileName = fileName + ".bak";
            //File.Delete(bakFileName);
            //if (File.Exists(fileName))
            //    File.Move(fileName, bakFileName);
            //File.Move(tempFileName, fileName);
        }

        /// <summary>Convert XML to an object model.</summary>
        /// <param name="xml">The XML</param>
        /// <returns>The newly deserialised model.</returns>
        public ModelWrapper ReadXML(string xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            APSIMFileConverter.ConvertToLatestVersion(doc.DocumentElement);

            XmlReader reader = new APSIMFileReader(doc.DocumentElement);
            XmlSerializer serial = new XmlSerializer(typeof(ModelWrapper), modelTypes.ToArray());
            return serial.Deserialize(reader) as ModelWrapper;
        }

        /// <summary>Create a simulations object by reading the specified filename</summary>
        /// <param name="fileName">Name of the file.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Simulations.Read() failed. Invalid simulation file.\n</exception>
        public ModelWrapper ReadFile(string fileName)
        {
            return ReadXML(File.ReadAllText(fileName));
        }

    }
}
