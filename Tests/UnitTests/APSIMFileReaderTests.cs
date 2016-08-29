

namespace UnitTests
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Xml;
    using System.Xml.Serialization;

    class APSIMFileReaderTests
    {
        /// <summary>Ensure that APSIMFileReader can read and convert XML</summary>
        [Test]
        public void APSIMFileReader_EnsureReadWorks()
        {
            // Get our test file.
            Stream s1 = Assembly.GetExecutingAssembly().GetManifestResourceStream
                ("UnitTests.Resources.APSIMFileReaderTests1.xml");
           
            // Create instance of reader.
            XmlReader reader = new APSIMFileReader(s1);
            reader.Read();

            // Get new XML from our reader
            StringWriter writer = new StringWriter();
            XmlDocument doc = new XmlDocument();
            doc.Load(reader);
            doc.Save(writer);
            string xml = writer.ToString().Replace("utf-16", "utf-8");  // TODO: Need to work out a better way of getting utf-8
 
            // Get the accepted XML
            Stream s2 = Assembly.GetExecutingAssembly().GetManifestResourceStream
                ("UnitTests.Resources.APSIMFileReaderTests2.xml");
            StreamReader s2Reader = new StreamReader(s2);
            string acceptedXML = s2Reader.ReadToEnd();

            // Compare to what it should be.
            Assert.AreEqual(xml, acceptedXML);
        }

        /// <summary>Another test to ensure that APSIMFileReader can read and convert standard toolbox XML</summary>
        [Test]
        public void APSIMFileReader_EnsureReadingStandardToolboxWorks()
        {
            string toolboxFileName = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                                                                        "..",
                                                                        "..",
                                                                        "..",
                                                                        "..",
                                                                        "UserInterface",
                                                                        "Resources",
                                                                        "Toolboxes",
                                                                        "StandardToolbox.apsimx");
            XmlDocument toolboxdoc = new XmlDocument();
            toolboxdoc.Load(toolboxFileName);

            // Create instance of reader.
            XmlReader reader = new APSIMFileReader(toolboxdoc.DocumentElement);
            reader.Read();

            // Get new XML from our reader
            StringWriter writer = new StringWriter();
            XmlDocument doc = new XmlDocument();
            doc.Load(reader);
            doc.Save(writer);
            string xml = writer.ToString();

            Assembly modelsAssembly = null;
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
                if (!a.IsDynamic && Path.GetFileName(a.Location) == "Models.exe")
                    modelsAssembly = a;

            ModelWrapper wrapper = XmlUtilities.Deserialise(doc.DocumentElement, modelsAssembly) as ModelWrapper;

            FileFormat fileFormat = new FileFormat();
            ModelWrapper rootNode = fileFormat.Read(toolboxdoc.DocumentElement);



            // If we get this far then the read worked.
        }
    }
}
