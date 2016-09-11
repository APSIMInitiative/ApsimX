
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
        /// <summary>Constructor</summary>
        public FileFormat()
        {
        }

        /// <summary>Convert the specified model to XML</summary>
        /// <param name="rootNode">The root model to serialise.</param>
        /// <returns>The XML</returns>
        public string WriteXML(ModelWrapper rootNode)
        {
            StringWriter s = new StringWriter();
            APSIMFileWriter writer = new APSIMFileWriter(s);
            writer.Formatting = Formatting.Indented;
            XmlUtilities.SerialiseWithOptions(rootNode, false, null, null, writer);
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

            return Read(doc.DocumentElement);
        }

        /// <summary>Create a simulations object by reading the specified filename</summary>
        /// <param name="fileName">Name of the file.</param>
        public ModelWrapper ReadFile(string fileName)
        {
            return ReadXML(File.ReadAllText(fileName));
        }

        /// <summary>Create a simulations object by reading the specified filename</summary>
        /// <param name="s">The stream to read from.</param>
        public ModelWrapper Read(Stream s)
        {
            XmlReader reader = new APSIMFileReader(s);
            reader.Read();

            Assembly modelsAssembly = null;
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
                if (!a.IsDynamic && Path.GetFileName(a.Location) == "Models.exe")
                    modelsAssembly = a;

            return XmlUtilities.Deserialise(reader, modelsAssembly) as ModelWrapper;
        }

        /// <summary>Create a simulations object by reading the specified filename</summary>
        /// <param name="node">XML node to read from.</param>
        public ModelWrapper Read(XmlNode node)
        {
            APSIMFileConverter.ConvertToLatestVersion(node);
            
            XmlReader reader = new APSIMFileReader(node);
            reader.Read();

            Assembly modelsAssembly = null;
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
                if (!a.IsDynamic && Path.GetFileName(a.Location) == "Models.exe")
                    modelsAssembly = a;

            return XmlUtilities.Deserialise(reader, modelsAssembly) as ModelWrapper;
        }
    }
}
