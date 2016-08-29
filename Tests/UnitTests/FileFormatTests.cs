

namespace UnitTests
{
    using Models.Core;
    using Models;
    using APSIM.Shared.Utilities;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.IO;
    using System.Xml;
    using System.Text;

    /// <summary>
    /// Test the writer's load/save .apsimx capability 
    /// </summary>
    [TestFixture]
    public class FileFormatTests
    {

        /// <summary>Test basic read/write capability</summary>
        [Test]
        public void FileFormat_EnsureWriteReadRoundTripWorks()
        {
            // Create a simulations object with child model wrappers.
            ModelWrapper rootNode1 = new ModelWrapper();
            ModelWrapper simulations = rootNode1.Add(new Simulations());
            ModelWrapper simulation = simulations.Add(new Simulation());

            Clock clock = new Clock();
            clock.StartDate = new DateTime(2015, 1, 1);
            clock.EndDate = new DateTime(2015, 12, 31);
            simulation.Add(clock);

            ModelWrapper zone = simulation.Add(new Zone());

            // Write the above simulations object to an xml string.
            FileFormat fileFormat = new FileFormat();
            string xml = fileFormat.WriteXML(rootNode1);

            // Read XML back in.
            ModelWrapper rootNode2 = fileFormat.ReadXML(xml);

            // Make sure the two root nodes are the same.
            Assert.IsTrue(rootNode2.Model is Simulations);
            Assert.AreEqual(rootNode2.Children.Count, 1);
            Assert.IsTrue((rootNode2.Children[0] as ModelWrapper).Model is Simulation);
            Assert.AreEqual((rootNode2.Children[0] as ModelWrapper).Children.Count, 2);
        }


        /// <summary>Make sure we can read a whole Simulations object.</summary>
        [Test]
        public void FileFormat_SimulationsRead()
        {
            Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("UnitTests.Resources.Continuous_Wheat.apsimx");
            XmlDocument doc = new XmlDocument();
            doc.Load(s);

            Simulations simulations = Simulations.Read(doc.DocumentElement);
         }

        [Test]
        public void FileFormat_Deserialise()
        {
            // Get our test file.
            Stream s2 = Assembly.GetExecutingAssembly().GetManifestResourceStream
                ("UnitTests.Resources.APSIMFileReaderTests2.xml");

            XmlDocument doc = new XmlDocument();
            doc.Load(s2);
            XmlReader reader = new XmlNodeReader(doc.DocumentElement);
            reader.Read();

            Assembly modelsAssembly = null;
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
                if (Path.GetFileName(a.Location) == "Models.exe")
                    modelsAssembly = a;

            ModelWrapper root = XmlUtilities.Deserialise(reader, modelsAssembly) as ModelWrapper;
        }


    }
}
