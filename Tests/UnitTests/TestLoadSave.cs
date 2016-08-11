

namespace UnitTests
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Models.Core;
    using Models;
    using APSIM.Shared.Utilities;
    using System.Xml;
    using System.IO;
    using System.Xml.Serialization;

    /// <summary>
    /// Test the writer's load/save .apsimx capability 
    /// </summary>
    [TestFixture]
    public class TestWriter
    {

        [Test]
        public void TestReadWrite()
        {
            Clock clock = new Clock();

            Simulation simulation = new Simulation();
            simulation.Children = new List<Model>();
            simulation.Parent = null;
            simulation.Children.Add(clock);
            simulation.Children.Add(new Zone());

            string xml = XmlUtilities.Serialise(simulation, withNamespace: true);

            string goodXML =
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
                "<Simulation xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">\r\n" +
                "  <Children>\r\n" +
                "    <Clock>\r\n" +
                "      <StartDate>0001-01-01T00:00:00</StartDate>\r\n" +
                "      <EndDate>0001-01-01T00:00:00</EndDate>\r\n" +
                "    </Clock>\r\n" +
                "    <Zone>\r\n" +
                "      <Name>Zone</Name>\r\n" +
                "      <Children />\r\n" +
                "      <Area>0</Area>\r\n" +
                "      <Slope>0</Slope>\r\n" +
                "    </Zone>\r\n" +
                "  </Children>\r\n" +
                "</Simulation>";

            Assert.AreEqual(xml, goodXML);

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            Simulation simulation2 = XmlUtilities.Deserialise(doc.DocumentElement, typeof(Simulation)) as Simulation;

            Assert.AreEqual(simulation2.Children.Count, 2);
        }
    }
}
