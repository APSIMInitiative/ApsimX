namespace UnitTests.Core.ApsimFile
{
    using APSIM.Shared.Utilities;
    using Models;
    using Models.Core;
    using Models.Core.ApsimFile;
    using Models.Soils;
    using NUnit.Framework;
    using System;

    /// <summary>This is a test class for the simulation structure manager.</summary>
    [TestFixture]
    public class StructureTests
    {
        [Test]
        public void EnsureAddXMLFromOldAPSIMWorks()
        {
            Simulation simulation = new Simulation();

            var xml = "<clock>" +
                      "  <start_date type=\"date\">01/01/1990</start_date>" +
                      "  <end_date type=\"date\">31/12/2000</end_date>" +
                      "</clock>";

            Structure.Add(xml, simulation);
            Assert.AreEqual(simulation.Children.Count, 1);
            var clock = simulation.Children[0] as Clock;
            Assert.IsNotNull(clock);
            Assert.AreEqual(clock.StartDate, new DateTime(1990, 1, 1));
            Assert.AreEqual(clock.EndDate, new DateTime(2000, 12, 31));
        }

        [Test]
        public void StructureTests_EnsureAddNewJSONWorks()
        {
            Simulation simulation = new Simulation();

            string json =
                "{" +
                "  \"$type\": \"Models.Clock, Models\"," +
                "  \"StartDate\": \"1900-01-01T00:00:00\"," +
                "  \"EndDate\": \"2000-12-31T00:00:00\"," +
                "  \"Name\": \"Clock\"," +
                "  \"Children\": []," +
                "  \"IncludeInDocumentation\": true," +
                "  \"Enabled\": true," +
                "  \"ReadOnly\": false" +
                "}";

            Structure.Add(json, simulation);
            Assert.AreEqual(simulation.Children.Count, 1);
            Clock clock = simulation.Children[0] as Clock;
            Assert.AreEqual(clock.Name, "Clock");
        }

        [Test]
        public void StructureTests_EnsureAddAvoidsDuplicateNames()
        {
            Simulation simulation = new Simulation();

            string xml =
            "<Memo>" +
            "  <Name>TitlePage</Name>" +
            "  <IncludeInDocumentation>true</IncludeInDocumentation>" +
            "  <MemoText>Some text</MemoText>" +
            "</Memo>";

            Structure.Add(xml, simulation);
            Structure.Add(xml, simulation);
            Assert.AreEqual(simulation.Children.Count, 2);
            Memo memo1 = simulation.Children[0] as Memo;
            Memo memo2 = simulation.Children[1] as Memo;
            Assert.AreEqual(memo1.Name, "TitlePage");
            Assert.AreEqual(memo2.Name, "TitlePage1");
        }

        /// <summary>When a soil is copied from APSoil make sure an InitWater and Sample is added.</summary>
        [Test]
        public void StructureTests_EnsureAPSOILSoilHasInitWaterAndSampleAdded()
        {
            Simulation simulation = new Simulation();

            string soilXml = ReflectionUtilities.GetResourceAsString("UnitTests.Core.ApsimFile.StructureTestsAPSoilSoil.xml");
            Structure.Add(soilXml, simulation);
            Assert.AreEqual(simulation.Children.Count, 1);
            Soil soil = simulation.Children[0] as Soil;
            Assert.AreEqual(soil.Children.Count, 6);
            Assert.IsTrue(soil.Children[4] is InitialWater);
            Assert.IsTrue(soil.Children[5] is Sample);
        }

        [Test]
        public void StructureTests_EnsureCannotAddModelToReadonlyParent()
        {
            Simulation simulation = new Simulation()
            {
                ReadOnly = true
            };

            string xml =
            "<Memo>" +
            "  <Name>TitlePage</Name>" +
            "  <IncludeInDocumentation>true</IncludeInDocumentation>" +
            "  <MemoText>Some text</MemoText>" +
            "</Memo>";

            Exception err = Assert.Throws<Exception>(() => Structure.Add(xml, simulation));
            Assert.AreEqual(err.Message, "Unable to modify Simulation - it is read-only.");
        }

        [Test]
        public void StructureTests_HandleBadStringGracefully()
        {
            Simulation simulation = new Simulation()
            {
                ReadOnly = true
            };

            string json = "INVALID STRING";

            Exception err = Assert.Throws<Exception>(() => Structure.Add(json, simulation));
            Assert.AreEqual(err.Message, "Unknown string encountered. Not JSON or XML. String: INVALID STRING");
        }
    }
}