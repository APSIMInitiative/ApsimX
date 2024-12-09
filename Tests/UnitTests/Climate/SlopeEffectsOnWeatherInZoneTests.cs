using APSIM.Shared.Utilities;
using Models.Core;
using Models.Core.ApsimFile;
using NUnit.Framework;
using System.IO;
using Models;
using Models.Storage;
using NUnit.Framework.Constraints;
using Models.GrazPlan;
using System.Collections.Generic;
using System.Data;
using Models.Climate;
using System.Linq;

namespace UnitTests.Climate;

[TestFixture]
class SlopeEffectsOnWeatherInZoneTests
{


        /// <summary>
        /// A simulations instance
        /// </summary>
        private Simulations simulations;
        
        /// <summary>
        /// A simulation instance
        /// </summary>
        private Simulation simulation;

        /// <summary>
        /// Start up code for all tests.
        /// </summary>
        [SetUp]
        public void Initialise()
        {
            string tempFolder = Path.Combine(Path.GetTempPath(), "UnitTests");
            Directory.CreateDirectory(tempFolder);
            Directory.SetCurrentDirectory(tempFolder);

            string str = ReflectionUtilities.GetResourceAsString("UnitTests.Climate.Resources.slopeInZone.apsimx");
            simulations = FileFormat.ReadFromString<Simulations>(str, e => throw e, false).NewModel as Simulations;
            simulation = simulations.FindChild<Simulation>();
        }

        [Test]
        public void ApsimXFileIsPresentTest()
        {
            Assert.That(simulations.Name, Is.EqualTo("Simulations"));
        }

        /// <summary>
        /// Ensures that locator links to the correct zone in a multi-zone simulation.
        /// </summary>
        [Test]
        public void CorrectZoneIsUsedInMultizoneSimulationTest()
        {
            // Utilities.RunModels(simulations, "");
            Utilities.ResolveLinks(simulations);
            SlopeEffectsOnWeather oneHundredAndEightySlope = simulation.FindChild<Zone>("Site4_30_180").FindChild<SlopeEffectsOnWeather>();
            SlopeEffectsOnWeather zeroSlope = simulation.FindChild<Zone>("Site4_30_0").FindChild<SlopeEffectsOnWeather>();
            Assert.That(oneHundredAndEightySlope.GetZoneName(), Is.EqualTo("Site4_30_180"));
            Assert.That(zeroSlope.GetZoneName(), Is.EqualTo("Site4_30_0"));
        }

        /// <summary>
        /// Makes sure that Zones with different aspect have differing values on the same datetime.
        /// </summary>
        [Test]
        public void EnsureDataIsDifferentBetweenDifferentZonesTest()
        {
            Utilities.RunModels(simulations,"--csv");
            string reportSearchPattern = $"{Path.GetFileNameWithoutExtension(simulations.FileName)}.SiteReport.csv";
            string[] outputFile = Directory.GetFiles(Path.GetDirectoryName(simulations.FileName), reportSearchPattern);
            DataTable siteReportDT = DataTableUtilities.FromCSV(reportSearchPattern, File.ReadAllText(outputFile.First()));
            double oneEightySlopeFirstRadnDValue = (double)siteReportDT.Rows[0]["RadnD"];
            DataRow row = siteReportDT.Rows[100];
            double zeroSlopeFirstRadnDValue = (double)siteReportDT.Rows[101]["RadnD"];
            Assert.That(oneEightySlopeFirstRadnDValue, Is.Not.SameAs(zeroSlopeFirstRadnDValue));
        }
}