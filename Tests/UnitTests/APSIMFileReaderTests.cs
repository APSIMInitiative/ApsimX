

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
        /// <summary>Ensure that APSIMFileReader can read and convert simple XML</summary>
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

        /// <summary>
        /// Another test to ensure that an APSIMFileReader behaves the same as an
        /// XmlNodeReader
        /// </summary>
        [Test]
        public void APSIMFileReader_EnsureSameAsXmlNodeReader()
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

            // Create 1st instance of reader which is an APSIMFileReader
            XmlReader reader1 = new APSIMFileReader(toolboxdoc.DocumentElement);

            // Create 2nd instance of reader based on XmlNodeReader where an XmlDocument
            // has already done a complete read from APSIMFileReader.
            XmlDocument doc = new XmlDocument();
            XmlReader tempReader = new APSIMFileReader(toolboxdoc.DocumentElement);
            tempReader.Read();
            doc.Load(tempReader);
            XmlReader reader2 = new XmlNodeReader(doc.DocumentElement);

            // The two readers should be the same.
            CompareReaders(reader1, reader2);
            while (reader1.Read() && reader2.Read())
                CompareReaders(reader1, reader2);
            
            bool ok = reader2.Read();
            Assert.IsFalse(reader2.Read());
        }

        /// <summary>
        /// Test to ensure that APSIMFileReader can read and convert standard toolbox XML. 
        /// This is a more complex test than the one above.
        /// </summary>
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
            XmlReader reader1 = new APSIMFileReader(toolboxdoc.DocumentElement);
            reader1.Read();

            Assembly modelsAssembly = null;
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
                if (!a.IsDynamic && Path.GetFileName(a.Location) == "Models.exe")
                    modelsAssembly = a;

            ModelWrapper wrapper = XmlUtilities.Deserialise(reader1, modelsAssembly) as ModelWrapper;
            Assert.AreEqual(wrapper.Children.Count, 8);
        }

        /// <summary>
        /// Compare two readers.
        /// </summary>
        /// <param name="reader1">Reader 1</param>
        /// <param name="reader2">Reader 2</param>
        private void CompareReaders(XmlReader reader1, XmlReader reader2)
        {
            Assert.AreEqual(reader1.BaseURI, reader2.BaseURI);
            Assert.AreEqual(reader1.NamespaceURI, reader2.NamespaceURI);
            Assert.AreEqual(reader1.Prefix, reader2.Prefix);
            Assert.AreEqual(reader1.Depth, reader2.Depth);
            Assert.AreEqual(reader1.EOF, reader2.EOF);
            Assert.AreEqual(reader1.NodeType, reader2.NodeType);
            Assert.AreEqual(reader1.Name, reader2.Name);
            Assert.AreEqual(reader1.Value, reader2.Value);
            Assert.AreEqual(reader1.AttributeCount, reader2.AttributeCount);
            for (int i = 0; i < reader1.AttributeCount; i++)
            {
                Assert.AreEqual(reader1.GetAttribute(i), reader2.GetAttribute(i));
            }
        }
    }
}
