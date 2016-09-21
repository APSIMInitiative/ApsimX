

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

        /// <summary>
        /// Test that a simulation can be written to a string and then
        /// converted back into a simulation i.e. round trip.
        /// </summary>
        // [Test]  // Temporarily disabled
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

        /// <summary>
        /// Make sure FileFormat to read an xml stream into a simulation.
        /// </summary>
        [Test]
        public void FileFormat_ReadFromStream()
        {
            // Get our test file.
            Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream
                ("UnitTests.Resources.APSIMFileReaderTests1.xml");

            FileFormat fileFormat = new FileFormat();
            ModelWrapper rootNode = fileFormat.Read(s);
            Assert.AreEqual(rootNode.Children.Count, 2);
        }


    }
}
