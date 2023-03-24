using APSIM.Shared.Utilities;
using DocumentFormat.OpenXml.InkML;
using Gtk;
using Models;
using Models.Core;
using Models.Core.ApsimFile;
using Models.Soils;
using Models.Storage;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace UnitTests
{
    [SetUpFixture]
    public class SetupTrace
    {
        [OneTimeSetUp]
        public void StartTest()
        {
            System.Diagnostics.Trace.Listeners.Add(new ConsoleTraceListener());
        }

        [OneTimeTearDown]
        public void EndTest()
        {
            System.Diagnostics.Trace.Flush();
        }
    }

    [TestFixture]
    public class CommandLineArgsTests
    {

        [Test]
        public void TestCsvSwitch()
        {
            Simulations file = Utilities.GetRunnableSim();

            string reportName = "Report";

            Models.Report report = file.FindInScope<Models.Report>();
            report.VariableNames = new string[] { "[Clock].Today.DayOfYear as n", "2 * [Clock].Today.DayOfYear as 2n" };
            report.EventNames = new string[] { "[Clock].DoReport" };
            report.Name = reportName;

            Clock clock = file.FindInScope<Clock>();
            clock.StartDate = new DateTime(2019, 1, 1);
            clock.EndDate = new DateTime(2019, 1, 10);

            string output = Utilities.RunModels(file, "/Csv");

            string csvFile = Path.ChangeExtension(file.FileName, ".Report.csv");
            Assert.True(File.Exists(csvFile), "Models.exe failed to create a csv file when passed the /Csv command line argument. Output of Models.exe: " + output);

            // Verify that the .csv file contains correct data.
            string csvData = File.ReadAllText(csvFile);
            string expected = @"SimulationName,SimulationID,2n,CheckpointID,CheckpointName,n,Zone
Simulation,1,2.000,1,Current,1,Zone
Simulation,1,4.000,1,Current,2,Zone
Simulation,1,6.000,1,Current,3,Zone
Simulation,1,8.000,1,Current,4,Zone
Simulation,1,10.000,1,Current,5,Zone
Simulation,1,12.000,1,Current,6,Zone
Simulation,1,14.000,1,Current,7,Zone
Simulation,1,16.000,1,Current,8,Zone
Simulation,1,18.000,1,Current,9,Zone
Simulation,1,20.000,1,Current,10,Zone
";
            Console.WriteLine(expected);
            Console.WriteLine(csvData);
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
                ".Simulations.Sim1.Field.Soil.Physical.Thickness[1] = 500",
                ".Simulations.Sim1.Field.Soil.Physical.Thickness[2] = 2500",
                ".Simulations.Sim2.Name = SimulationVariant35",
            };
            string configFileName = Path.GetTempFileName();
            File.WriteAllLines(configFileName, changes);

            string apsimxFileName = Path.ChangeExtension(Path.GetTempFileName(), ".apsimx");
            string text = ReflectionUtilities.GetResourceAsString("UnitTests.BasicFile.apsimx");

            // Check property values at this point.
            Simulations sims = FileFormat.ReadFromString<Simulations>(text, e => throw e, false).NewModel as Simulations;

            Clock clock = sims.FindInScope<Clock>();
            Simulation sim1 = sims.FindInScope<Simulation>();
            Simulation sim2 = sims.FindInScope("Sim2") as Simulation;
            IPhysical physical = sims.FindByPath(".Simulations.Sim1.Field.Soil.Physical")?.Value as IPhysical;

            // Check property values - they should be unchanged at this point.
            DateTime start = new DateTime(2003, 11, 15);
            Assert.AreEqual(start.Year, clock.StartDate.Year);
            Assert.AreEqual(start.DayOfYear, clock.StartDate.DayOfYear);

            Assert.AreEqual(sim1.Name, "Sim1");
            Assert.AreEqual(sim2.Enabled, true);
            Assert.AreEqual(physical.Thickness[0], 150);
            Assert.AreEqual(physical.Thickness[1], 150);

            // Run Models.exe with /Edit command.
            var overrides = Overrides.ParseStrings(File.ReadAllLines(configFileName));
            Overrides.Apply(sims, overrides);

            // Get references to the changed models.
            clock = sims.FindInScope<Clock>();
            Clock clock2 = sims.FindByPath(".Simulations.SimulationVariant35.Clock", LocatorFlags.PropertiesOnly | LocatorFlags.IncludeDisabled)?.Value as Clock;

            // Sims should have at least 3 children - data store and the 2 sims.
            Assert.That(sims.Children.Count > 2);
            sim1 = sims.Children.OfType<Simulation>().First();
            sim2 = sims.Children.OfType<Simulation>().Last();
            physical = sims.FindByPath(".Simulations.Sim1.Field.Soil.Physical")?.Value as IPhysical;

            start = new DateTime(2019, 1, 20);
            DateTime end = new DateTime(2019, 3, 20);

            // Check clock.
            Assert.AreEqual(clock.StartDate.Year, start.Year);
            Assert.AreEqual(clock.StartDate.DayOfYear, start.DayOfYear);
            Assert.AreEqual(clock.EndDate.Year, end.Year);
            Assert.AreEqual(clock.EndDate.DayOfYear, end.DayOfYear);

            // Clock 2 should have been changed as well.
            Assert.AreEqual(clock2.StartDate.Year, start.Year);
            Assert.AreEqual(clock2.StartDate.DayOfYear, start.DayOfYear);
            Assert.AreEqual(clock2.EndDate.Year, 2003);
            Assert.AreEqual(clock2.EndDate.DayOfYear, 319);

            // Sim2 should have been renamed to SimulationVariant35
            Assert.AreEqual(sim2.Name, "SimulationVariant35");

            // Sim1's name should be unchanged.
            Assert.AreEqual(sim1.Name, "Sim1");

            // Sim2 should have been disabled. This should not affect sim1.
            Assert.That(sim1.Enabled);
            Assert.That(!sim2.Enabled);

            // First 2 soil thicknesses have been changed to 500 and 2500 respectively.
            Assert.AreEqual(physical.Thickness[0], 500, 1e-8);
            Assert.AreEqual(physical.Thickness[1], 2500, 1e-8);
        }

        /// <summary>
        /// Test the /SimulationNameRegexPattern option (and the /Verbose option as well,
        /// technically. This isn't really ideal but it makes things simpler...).
        /// </summary>
        [Test]
        public void TestSimNameRegex()
        {
            string models = typeof(IModel).Assembly.Location;
            IModel sim1 = Utilities.GetRunnableSim().Children[1];
            sim1.Name = "sim1";

            IModel sim2 = Utilities.GetRunnableSim().Children[1];
            sim2.Name = "sim2";

            IModel sim3 = Utilities.GetRunnableSim().Children[1];
            sim3.Name = "simulation3";

            IModel sim4 = Utilities.GetRunnableSim().Children[1];
            sim4.Name = "Base";

            Simulations sims = Simulations.Create(new[] { sim1, sim2, sim3, sim4, new DataStore() });
            sims.ParentAllDescendants();

            string apsimxFileName = Path.ChangeExtension(Path.GetTempFileName(), ".apsimx");
            sims.Write(apsimxFileName);

            // Need to quote the regex on unix systems.
            string args;
            args = @"/Verbose /SimulationNameRegexPattern:sim\d";

            string stdout = Utilities.RunModels(sims, args);

            Assert.True(stdout.Contains("sim1"));
            Assert.True(stdout.Contains("sim2"));
            Assert.False(stdout.Contains("simulation3"));
            Assert.False(stdout.Contains("Base"));

            args = @"/Verbose /SimulationNameRegexPattern:sim1";
            stdout = Utilities.RunModels(sims, args);

            Assert.True(stdout.Contains("sim1"));
            Assert.False(stdout.Contains("sim2"));
            Assert.False(stdout.Contains("simulation3"));
            Assert.False(stdout.Contains("Base"));

            args = @"/Verbose /SimulationNameRegexPattern:(simulation3)|(Base)";
            stdout = Utilities.RunModels(sims, args);

            Assert.False(stdout.Contains("sim1"));
            Assert.False(stdout.Contains("sim2"));
            Assert.True(stdout.Contains("simulation3"));
            Assert.True(stdout.Contains("Base"));
        }

        [Test]
        public void TestListSimulationNames()
        {
            Simulations simpleExperiment = Utilities.GetSimpleExperiment();
            string output = Utilities.RunModels(simpleExperiment, $"/ListSimulations");
            string expected = @"ExperimentX1Y1
ExperimentX2Y1
ExperimentX1Y2
ExperimentX2Y2
";
            Assert.AreEqual(expected, output);

            output = Utilities.RunModels(simpleExperiment, $"/ListSimulations /SimulationNameRegexPattern:.*Y1");
            expected = @"ExperimentX1Y1
ExperimentX2Y1
";
            Assert.AreEqual(expected, output);

            // Disable the x factor. The disabled factor should not generate any simulations,
            // so the output should only contain the 2 simulations which modify y.
            simpleExperiment.Children[1].Children[0].Children[0].Children[0].Enabled = false;
            output = Utilities.RunModels(simpleExperiment, $"/ListSimulations");
            expected = @"ExperimentY1
ExperimentY2
";
            Assert.AreEqual(expected, output);
        }
    }
}
