

namespace UnitTests
{
    using Models.Core;
    using Models;
    using APSIM.Shared.Utilities;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    /// <summary>
    /// Test the writer's load/save .apsimx capability 
    /// </summary>
    [TestFixture]
    public class TestFileFormat
    {
        /// <summary>Test basic read/write capability</summary>
        [Test]
        public void TestReadWrite()
        {
            // Create a tree with a root node for our models.
            ModelWrapper models = new ModelWrapper();

            // Create some models.
            ModelWrapper simulations = models.Add(new Simulations());

            ModelWrapper simulation = simulations.Add(new Simulation());

            Clock clock = new Clock();
            clock.StartDate = new DateTime(2015, 1, 1);
            clock.EndDate = new DateTime(2015, 12, 31);
            simulation.Add(clock);

            ModelWrapper zone = simulation.Add(new Zone());

            // Write simulation to XML.
            List<Type> modelTypes = new List<Type>();
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
                foreach (Type t in a.GetTypes())
                    modelTypes.Add(t);

            FileFormat fileFormat = new FileFormat(modelTypes);
            string xml = fileFormat.WriteXML(models);

            // Compare XML against known good
            string goodXML =
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n" +
                "<Simulations Version=\"1\">\r\n" +
                "  <Name>Simulations</Name>\r\n" +
                "  <ExplorerWidth>0</ExplorerWidth>\r\n" +
                "  <Simulation>\r\n" +
                "    <Name>Simulation</Name>\r\n" +
                "    <Area>0</Area>\r\n" +
                "    <Slope>0</Slope>\r\n" +
                "    <Clock>\r\n" +
                "      <Name>Clock</Name>\r\n" +
                "      <StartDate>2015-01-01T00:00:00</StartDate>\r\n" +
                "      <EndDate>2015-12-31T00:00:00</EndDate>\r\n" +
                "    </Clock>\r\n" +
                "    <Zone>\r\n" +
                "      <Name>Zone</Name>\r\n" +
                "      <Area>0</Area>\r\n" +
                "      <Slope>0</Slope>\r\n" +
                "    </Zone>\r\n" +
                "  </Simulation>\r\n" +
                "</Simulations>";
            Assert.AreEqual(xml, goodXML);

            // Read XML back in.

            ModelWrapper rootNode = fileFormat.ReadXML(goodXML);
            Assert.IsTrue(rootNode.Model is Simulations);
            Assert.AreEqual(rootNode.Children.Count, 1);
            Assert.IsTrue((rootNode.Children[0] as ModelWrapper).Model is Simulation);
            Assert.AreEqual((rootNode.Children[0] as ModelWrapper).Children.Count, 2);
        }


    }
}
