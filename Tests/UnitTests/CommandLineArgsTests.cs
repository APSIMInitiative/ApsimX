using APSIM.Shared.Utilities;
using Models;
using Models.Core;
using Models.Core.ApsimFile;
using Models.Soils;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UnitTests
{
    [TestFixture]
    public class CommandLineArgsTests
    {
        [Test]
        public void TestCsvSwitch()
        {
            Simulations file = Utilities.GetRunnableSim();

            string reportName = "Report";

            Models.Report report = Apsim.Find(file, typeof(Models.Report)) as Models.Report;
            report.VariableNames = new string[] { "[Clock].Today.DayOfYear as n", "2 * [Clock].Today.DayOfYear as 2n" };
            report.EventNames = new string[] { "[Clock].DoReport" };
            report.Name = reportName;

            Clock clock = Apsim.Find(file, typeof(Clock)) as Clock;
            clock.StartDate = new DateTime(2019, 1, 1);
            clock.EndDate = new DateTime(2019, 1, 10);

            string output = Utilities.RunModels(file, "/Csv");

            string csvFile = Path.ChangeExtension(file.FileName, ".Report.csv");
            Assert.True(File.Exists(csvFile), "Models.exe failed to create a csv file when passed the /Csv command line argument. Output of Models.exe: " + output);

            // Verify that the .csv file contains correct data.
            string csvData = File.ReadAllText(csvFile);
            string expected = @"SimulationName,SimulationID,    2n,CheckpointID,CheckpointName, n,Zone
    Simulation,           1, 2.000,           1,       Current, 1,Zone
    Simulation,           1, 4.000,           1,       Current, 2,Zone
    Simulation,           1, 6.000,           1,       Current, 3,Zone
    Simulation,           1, 8.000,           1,       Current, 4,Zone
    Simulation,           1,10.000,           1,       Current, 5,Zone
    Simulation,           1,12.000,           1,       Current, 6,Zone
    Simulation,           1,14.000,           1,       Current, 7,Zone
    Simulation,           1,16.000,           1,       Current, 8,Zone
    Simulation,           1,18.000,           1,       Current, 9,Zone
    Simulation,           1,20.000,           1,       Current,10,Zone
";
            Assert.AreEqual(expected, csvData);
        }

        /// <summary>
        /// Tests the edit file option.
        /// </summary>
        [Test]
        public void TestEditOption()
        {
            string[] changes = new string[]
            {
                "[Clock].StartDate = 2019-1-20",
                ".Simulations.Sim1.Clock.EndDate = 3/20/2019",
                ".Simulations.Sim2.Enabled = false",
                ".Simulations.Sim1.Field.Soil.Thickness[1] = 500",
                ".Simulations.Sim1.Field.Soil.Thickness[2] = 2500",
                ".Simulations.Sim2.Name = SimulationVariant35",
            };
            string configFileName = Path.GetTempFileName();
            File.WriteAllLines(configFileName, changes);

            string apsimxFileName = Path.ChangeExtension(Path.GetTempFileName(), ".apsimx");
            string text = ReflectionUtilities.GetResourceAsString("UnitTests.BasicFile.apsimx");

            // Check property values at this point.
            Simulations sims = FileFormat.ReadFromString<Simulations>(text, out List<Exception> errors);
            if (errors != null && errors.Count > 0)
                throw errors[0];

            Clock clock = Apsim.Find(sims, typeof(Clock)) as Clock;
            Simulation sim1 = Apsim.Find(sims, typeof(Simulation)) as Simulation;
            Simulation sim2 = Apsim.Find(sims, "Sim2") as Simulation;
            Soil soil = Apsim.Get(sims, ".Simulations.Sim1.Field.Soil") as Soil;

            // Check property values - they should be unchanged at this point.
            DateTime start = new DateTime(2003, 11, 15);
            Assert.AreEqual(start.Year, clock.StartDate.Year);
            Assert.AreEqual(start.DayOfYear, clock.StartDate.DayOfYear);

            Assert.AreEqual(sim1.Name, "Sim1");
            Assert.AreEqual(sim2.Enabled, true);
            Assert.AreEqual(soil.Thickness[0], 150);
            Assert.AreEqual(soil.Thickness[1], 150);

            // Run Models.exe with /Edit command.
            sims.Write(apsimxFileName);
            Utilities.RunModels($"{apsimxFileName} /Edit {configFileName}");
            sims = FileFormat.ReadFromFile<Simulations>(apsimxFileName, out errors);
            if (errors != null && errors.Count > 0)
                throw errors[0];

            // Get references to the changed models.
            clock = Apsim.Find(sims, typeof(Clock)) as Clock;
            Clock clock2 = Apsim.Get(sims, ".Simulations.SimulationVariant35.Clock") as Clock;

            // Sims should have at least 3 children - data store and the 2 sims.
            Assert.That(sims.Children.Count > 2);
            sim1 = sims.Children.OfType<Simulation>().First();
            sim2 = sims.Children.OfType<Simulation>().Last();
            soil = Apsim.Get(sims, ".Simulations.Sim1.Field.Soil") as Soil;

            start = new DateTime(2019, 1, 20);
            DateTime end = new DateTime(2019, 3, 20);

            // Check clock.
            Assert.AreEqual(clock.StartDate.Year, start.Year);
            Assert.AreEqual(clock.StartDate.DayOfYear, start.DayOfYear);
            Assert.AreEqual(clock.EndDate.Year, end.Year);
            Assert.AreEqual(clock.EndDate.DayOfYear, end.DayOfYear);

            // These changes should not affect the clock in simulation 2.
            start = new DateTime(2003, 11, 15);
            end = new DateTime(2003, 11, 15);
            Assert.AreEqual(clock2.StartDate.Year, start.Year);
            Assert.AreEqual(clock2.StartDate.DayOfYear, start.DayOfYear);
            Assert.AreEqual(clock2.EndDate.Year, end.Year);
            Assert.AreEqual(clock2.EndDate.DayOfYear, end.DayOfYear);

            // Sim2 should have been renamed to SimulationVariant35
            Assert.AreEqual(sim2.Name, "SimulationVariant35");

            // Sim1's name should be unchanged.
            Assert.AreEqual(sim1.Name, "Sim1");

            // Sim2 should have been disabled. This should not affect sim1.
            Assert.That(sim1.Enabled);
            Assert.That(!sim2.Enabled);

            // First 2 soil thicknesses have been changed to 500 and 2500 respectively.
            Assert.AreEqual(soil.Thickness[0], 500, 1e-8);
            Assert.AreEqual(soil.Thickness[1], 2500, 1e-8);
        }
    }
}
