﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using APSIM.Shared.Utilities;
using Models;
using Models.Core;
using Models.Core.ApsimFile;
using Models.Factorial;
using Models.Soils;
using Models.Storage;
using NUnit.Framework;

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

            IClock clock = file.FindInScope<Clock>();
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

            IClock clock = sims.FindInScope<Clock>();
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
            IClock clock2 = sims.FindByPath(".Simulations.SimulationVariant35.Clock", LocatorFlags.PropertiesOnly | LocatorFlags.IncludeDisabled)?.Value as Clock;

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
        [Order(2)]// If not ordered causes test failure when ran in conjunction with TestDeleteSimulationsCommandThrowsException().
        public void TestSimNameRegex()
        {
            string models = typeof(IModel).Assembly.Location;
            IModel sim1 = Utilities.GetRunnableSim().Children[1];
            sim1.Name = "sim1";

            IModel sim2 = Utilities.GetRunnableSim().Children[1];
            sim2.Name = "originalSimAfterAdd";

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
            Assert.False(stdout.Contains("originalSimAfterAdd"));
            Assert.False(stdout.Contains("simulation3"));
            Assert.False(stdout.Contains("Base"));

            args = @"/Verbose /SimulationNameRegexPattern:sim1";
            stdout = Utilities.RunModels(sims, args);

            Assert.True(stdout.Contains("sim1"));
            Assert.False(stdout.Contains("originalSimAfterAdd"));
            Assert.False(stdout.Contains("simulation3"));
            Assert.False(stdout.Contains("Base"));

            args = @"/Verbose /SimulationNameRegexPattern:(simulation3)|(Base)";
            stdout = Utilities.RunModels(sims, args);

            Assert.False(stdout.Contains("sim1"));
            Assert.False(stdout.Contains("originalSimAfterAdd"));
            Assert.True(stdout.Contains("simulation3"));
            Assert.True(stdout.Contains("Base"));
        }

        [Test]
        [Order(1)] // If not ordered causes test failure when ran in conjunction with TestDeleteSimulationsCommandThrowsException().
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

        [Test]
        public void TestApplySwitchAddWithModelName()
        {
            Simulations file = Utilities.GetRunnableSim();

            Zone fieldNode = file.FindInScope<Zone>();

            // Get path string for the config file that changes the date.
            string savingFilePath = Path.Combine(Path.GetTempPath(), "savingFile.apsimx");
            string newFileString = $"add [Zone] Report\nsave savingFile.apsimx";
            string newTempConfigFile = Path.Combine(Path.GetTempPath(), "config1.txt");
            File.WriteAllText(newTempConfigFile, newFileString);

            bool fileExists = File.Exists(newTempConfigFile);
            Assert.True(File.Exists(newTempConfigFile));

            Utilities.RunModels(file, $"--apply {newTempConfigFile}");

            string text = File.ReadAllText(savingFilePath);
            // Reload simulation from file text. Needed to see changes made.
            Simulations sim2 = FileFormat.ReadFromString<Simulations>(text, e => throw e, false).NewModel as Simulations;

            // Get new values from changed simulation.
            Zone fieldNodeAfterChange = sim2.FindInScope<Zone>();
            // See if the report shows up as a second child of Field with a specific name.
            Models.Report newReportNode = fieldNodeAfterChange.FindChild<Models.Report>("Report1");
            Assert.IsNotNull(newReportNode);
        }

        [Test]
        public void TestApplySwitchAddFromAnotherApsimxFile()
        {
            Simulations file = Utilities.GetRunnableSim();
            Simulations file2 = Utilities.GetRunnableSim();
            Simulations file3 = Utilities.GetRunnableSim();

            // TODO: Save these files in a Path.GetTempPath() place.
            // Then when its directory is used in RunInstructionOnApsimxFile it will be located correctly.

            Zone fieldNode = file.FindInScope<Zone>();

            // Get path string for the config file that changes the date.
            string newApsimFile = file2.FileName;
            string savingApsimFileName = file3.FileName;
            int indexOfNameStart = savingApsimFileName.LastIndexOf(Path.DirectorySeparatorChar) + 1;
            string savingApsimFileNameShort = savingApsimFileName.Substring(indexOfNameStart);
            string newFileString = $"add [Zone] {newApsimFile};[Report]\nsave {savingApsimFileNameShort} ";
            string newTempConfigFile = Path.Combine(Path.GetTempPath(), "config2.txt");
            //string newTempConfigFile = "config2.txt";
            File.WriteAllText(newTempConfigFile, newFileString);

            bool fileExists = File.Exists(newTempConfigFile);
            bool apsimFileExists = File.Exists(newApsimFile);
            Assert.True(File.Exists(newTempConfigFile));
            Assert.True(File.Exists(newApsimFile));

            Utilities.RunModels(file, $"--apply {newTempConfigFile}");

            string text = File.ReadAllText(savingApsimFileName);
            // Reload simulation from file text. Needed to see changes made.
            Simulations originalSimAfterAdd = FileFormat.ReadFromString<Simulations>(text, e => throw e, false).NewModel as Simulations;

            // Get new values from changed simulation.
            Zone fieldNodeAfterChange = originalSimAfterAdd.FindInScope<Zone>();
            // See if the report shows up as a second child of Field with a specific name.
            Models.Report newReportNode = fieldNodeAfterChange.FindChild<Models.Report>("Report1");
            Assert.IsNotNull(newReportNode);

        }

        [Test]
        public void TestApplySwitchDeleteCommand()
        {
            Simulations file = Utilities.GetRunnableSim();

            Zone fieldNode = file.FindInScope<Zone>();

            // Get path string for the config file that changes the date.
            string savingFilePath = Path.Combine(Path.GetTempPath(), "savingFile.apsimx");
            string newFileString = "delete [Zone].Report\nsave savingFile.apsimx";
            string newTempConfigFile = Path.Combine(Path.GetTempPath(), "config3.txt");
            File.WriteAllText(newTempConfigFile, newFileString);

            bool fileExists = File.Exists(newTempConfigFile);
            Assert.True(File.Exists(newTempConfigFile));

            Utilities.RunModels(file, $"--apply {newTempConfigFile}");

            string text = File.ReadAllText(savingFilePath);
            // Reload simulation from file text. Needed to see changes made.
            Simulations sim2 = FileFormat.ReadFromString<Simulations>(text, e => throw e, false).NewModel as Simulations;

            // Get new values from changed simulation.
            Zone fieldNodeAfterChange = sim2.FindInScope<Zone>();
            // See if the report shows up as a second child of Field with a specific name.
            Models.Report ReportNodeThatShouldHaveBeenDeleted = fieldNodeAfterChange.FindChild<Models.Report>("Report");
            Assert.IsNull(ReportNodeThatShouldHaveBeenDeleted);
        }

        [Test]
        public void TestApplySwitchDuplicateCommand()
        {
            Simulations file = Utilities.GetRunnableSim();

            Simulation simulationNode = file.FindInScope<Simulation>();

            // Get path string for the config file that changes the date.
            string savingFilePath = Path.Combine(Path.GetTempPath(), "savingFile.apsimx");
            string newFileString = "duplicate [Simulation] SimulationCopy\nsave savingFile.apsimx";
            string newTempConfigFile = Path.Combine(Path.GetTempPath(), "config4.txt");
            File.WriteAllText(newTempConfigFile, newFileString);

            bool fileExists = File.Exists(newTempConfigFile);
            Assert.True(File.Exists(newTempConfigFile));

            Utilities.RunModels(file, $"--apply {newTempConfigFile}");

            string text = File.ReadAllText(savingFilePath);
            // Reload simulation from file text. Needed to see changes made.
            Simulations sim2 = FileFormat.ReadFromString<Simulations>(text, e => throw e, false).NewModel as Simulations;

            // Get new values from changed ApsimX file.
            Simulation simulationCopyNodeAfterChange = sim2.FindInScope<Simulation>("SimulationCopy");
            Simulation originalSimulationAfterChange = sim2.FindInScope<Simulation>("Simulation");

            Assert.AreNotEqual(simulationCopyNodeAfterChange.Name, originalSimulationAfterChange.Name);
            Assert.IsNotNull(simulationCopyNodeAfterChange);
            Assert.IsNotNull(originalSimulationAfterChange);
        }

        [Test]
        public void TestApplySwitchWithFileDoesNotChangeOriginal()
        {
            Simulations file = Utilities.GetRunnableSim();

            Zone fieldNode = file.FindInScope<Zone>();

            // Get path string for the config file that changes the date.
            string newFileString = "add [Zone] Report\nsave modifiedSim.apsimx";
            string newTempConfigFile = Path.Combine(Path.GetTempPath(), "configFileDoesNotChangeOriginal.txt");
            File.WriteAllText(newTempConfigFile, newFileString);

            bool fileExists = File.Exists(newTempConfigFile);
            Assert.True(File.Exists(newTempConfigFile));

            Utilities.RunModels(file, $"--apply {newTempConfigFile}");

            string text = File.ReadAllText(file.FileName);
            // Reload simulation from file text. Needed to see changes made.
            Simulations sim2 = FileFormat.ReadFromString<Simulations>(text, e => throw e, false).NewModel as Simulations;
            List<Models.Report> reportList = sim2.FindAllInScope<Models.Report>().ToList();
            Assert.Less(reportList.Count, 2);
        }

        [Test]
        public void TestApplySwitchSaveFromConfigFile()
        {
            Simulations file = Utilities.GetRunnableSim();

            Simulation simulationNode = file.FindInScope<Simulation>();

            string newSaveFileName = file.FileName.Insert(file.FileName.LastIndexOf("."), "2").Split('/', '\\').ToList().Last();
            string simpleFileName = file.FileName.Split('/', '\\').ToList().Last();
            // Get path string for the config file that changes the date.
            string newFileString = $"load {simpleFileName}\nadd [Zone] Report\nsave {newSaveFileName}";
            string newTempConfigFile = Path.Combine(Path.GetTempPath(), "config5.txt");
            File.WriteAllText(newTempConfigFile, newFileString);

            bool fileExists = File.Exists(newTempConfigFile);
            Assert.True(File.Exists(newTempConfigFile));

            Utilities.RunModels($"--apply {newTempConfigFile}");

            string text = File.ReadAllText(Path.GetDirectoryName(newTempConfigFile) + Path.DirectorySeparatorChar + newSaveFileName);
            // Reload simulation from file text. Needed to see changes made.
            Simulations sim2 = FileFormat.ReadFromString<Simulations>(text, e => throw e, false).NewModel as Simulations;

            // Get new values from changed simulation.
            Zone fieldNodeAfterChange = sim2.FindInScope<Zone>();

            // See if the report shows up as a second child of Field with a specific name.
            Models.Report secondReportNodeThatShouldBePresent = fieldNodeAfterChange.FindChild<Models.Report>("Report1");
            Assert.IsNotNull(secondReportNodeThatShouldBePresent);

            // Make sure first sim was not modified.
            string firstSimText = File.ReadAllText(Path.GetDirectoryName(newTempConfigFile) + Path.DirectorySeparatorChar + simpleFileName);
            Simulations sim1 = FileFormat.ReadFromString<Simulations>(firstSimText, e => throw e, false).NewModel as Simulations;
            Zone fieldNodeFromOriginalSim = sim1.FindInScope<Zone>();
            Models.Report ReportNodeThatShouldNotBePresent = fieldNodeFromOriginalSim.FindChild<Models.Report>("Report1");
            Assert.IsNull(ReportNodeThatShouldNotBePresent);
        }

        [Test]
        public void TestApplySwitchLoadFromConfigFile()
        {
            Simulations file = Utilities.GetRunnableSim();

            Simulation simulationNode = file.FindInScope<Simulation>();

            string apsimxFileName = file.FileName.Split('\\', '/').ToList().Last();

            //File.Move(file.FileName, $"C:/unit-test-temp/{apsimxFileName}");

            string newFileString = $"load {apsimxFileName}\nadd [Zone] Report\nsave {apsimxFileName}";
            //string newTempConfigFile = Path.Combine("C:/unit-test-temp/", "config6.txt");
            string newTempConfigFile = Path.Combine(Path.GetTempPath(), "config6.txt");
            File.WriteAllText(newTempConfigFile, newFileString);

            bool fileExists = File.Exists(newTempConfigFile);
            Assert.True(File.Exists(newTempConfigFile));

            Utilities.RunModels($"--apply {newTempConfigFile}");

            string text = File.ReadAllText(file.FileName);
            // Reload simulation from file text. Needed to see changes made.
            Simulations sim2 = FileFormat.ReadFromString<Simulations>(text, e => throw e, false).NewModel as Simulations;

            // Get new values from changed simulation.
            Zone fieldNodeAfterChange = sim2.FindInScope<Zone>();

            // See if the report shows up as a second child of Field with a specific name.
            Models.Report secondReportNodeThatShouldBePresent = fieldNodeAfterChange.FindChild<Models.Report>("Report1");
            Assert.IsNotNull(secondReportNodeThatShouldBePresent);
        }

        [Test]
        public void TestApplySwitchCreateFromConfigFile()
        {
            string newApsimxFileName = "newSim.apsimx";
            string newApsimxFilePath = Path.Combine(Path.GetTempPath(), newApsimxFileName);

            string newConfigFilePath = Path.Combine(Path.GetTempPath(), "configFile.txt");
            string newCommandString = $"save {newApsimxFileName}";
            File.WriteAllText(newConfigFilePath, newCommandString);

            bool fileExists = File.Exists(newConfigFilePath);
            Assert.True(File.Exists(newConfigFilePath));

            Utilities.RunModels($"--apply {newConfigFilePath}");

            string text = File.ReadAllText(newApsimxFilePath);
            // Reload simulation from file text. Needed to see changes made.
            Simulations sim = FileFormat.ReadFromString<Simulations>(text, e => throw e, false).NewModel as Simulations;

            // Ensure that the sim was created.
            Assert.IsNotNull(sim);
        }

        [Test]
        public void TestApplySwitchRunFromConfigFile()
        {
            string newSimName = "newSim1.apsimx";
            string newApsimxFilePath = Path.Combine(Path.GetTempPath(), newSimName);
            string newConfigFilePath = Path.Combine(Path.GetTempPath(), "configFile1.txt");

            string newCommands = $@"save {newSimName}
load {newSimName}
add [Simulations] Simulation
add [Simulation] Summary
add [Simulation] Clock
add [Simulation] Weather
[Weather].FileName=dalby.met
[Clock].Start=1900/01/01
[Clock].End=1900/01/02
save {newSimName}
run";

            File.WriteAllText(newConfigFilePath, newCommands);
            bool configFileExists = File.Exists(newConfigFilePath);
            Assert.True(configFileExists);

            string newDalbyMetFilePath = Path.Combine(Path.GetTempPath(), "dalby.met");

            string dalbyMetFileText =
@"[weather.met.weather]
!station number = 041023
!station name = DALBY POST OFFICE
latitude = -27.18(DECIMAL DEGREES)
longitude = 151.26(DECIMAL DEGREES)
tav = 19.09(oC)! annual average ambient temperature
amp = 14.63(oC)! annual amplitude in mean monthly temperature
!Data extracted from Silo (by odin) on 20140902
!As evaporation is read at 9am, it has been shifted to day before
!ie The evaporation measured on 20 April is in row for 19 April
!The 6 digit code indicates the source of the 6 data columns
!0 actual observation, 1 actual observation composite station
!2 daily raster, 7 long term average raster
!more detailed two digit codes are available
!
!For further information see the documentation on the datadrill
!  http://www.dnr.qld.gov.au/silo/datadril.html
!
year  day radn  maxt   mint  rain  pan    vp      code
 ()   () (MJ/m^2) (oC) (oC)  (mm)  (mm)  (hPa)      ()
1900   1   24.0  29.4  18.6   0.0   8.2  20.3 300070
1900   2   25.0  31.6  17.2   0.0   8.2  16.5 300070
";

            File.WriteAllText(newDalbyMetFilePath, dalbyMetFileText);
            bool dalbyMetFileExists = File.Exists(newDalbyMetFilePath);
            Assert.True(dalbyMetFileExists);

            Utilities.RunModels($"--apply {newConfigFilePath}");

            string text = File.ReadAllText(newApsimxFilePath);

            // Reload simulation from file text. Needed to see changes made.
            Simulations sim = FileFormat.ReadFromString<Simulations>(text, e => throw e, false).NewModel as Simulations;
            Summary summaryNode = sim.FindInScope<Summary>();
            var summaryFileText = summaryNode.GetMessages(sim.Name);
            // Ensure that the sim was created.
            Assert.IsNotNull(summaryFileText);
        }

        [Test]
        public void TestDeleteSimulationsCommandThrowsException()
        {
            Simulations file = Utilities.GetRunnableSim();

            Simulation simulationNode = file.FindInScope<Simulation>();

            string apsimxFileName = file.FileName.Split('\\', '/').ToList().Last();

            string newFileString = $"load {apsimxFileName}\ndelete [Simulations]";
            string newTempConfigFile = Path.Combine(Path.GetTempPath(), "config6.txt");
            File.WriteAllText(newTempConfigFile, newFileString);

            bool fileExists = File.Exists(newTempConfigFile);
            Assert.True(File.Exists(newTempConfigFile));

            Exception ex = Assert.Throws<Exception>(delegate { Utilities.RunModels($"--apply {newTempConfigFile}"); });
            Assert.IsTrue(ex.Message.Contains("System.InvalidOperationException: Command 'delete [Simulations]' is an invalid command. [Simulations] node is the top-level node and cannot be deleted. Remove the command from your config file."));

        }

        [Test]
        public void TestApplySwitchUsingCopyCommand()
        {
            Simulations file = Utilities.GetRunnableSim();


            Simulation simulationNode = file.FindInScope<Simulation>();

            string apsimxFileName = file.FileName.Split('\\', '/').ToList().Last();
            string newApsimxFilePath = Path.Combine(Path.GetTempPath(), apsimxFileName);

            string newFileString = @$"load {apsimxFileName}
add [Simulations] Experiment
copy [Simulation] [Experiment]
save {apsimxFileName}";

            string newTempConfigFile = Path.Combine(Path.GetTempPath(), "configCopyCommand.txt");
            File.WriteAllText(newTempConfigFile, newFileString);

            bool fileExists = File.Exists(newTempConfigFile);
            Assert.True(File.Exists(newTempConfigFile));

            Utilities.RunModels($"--apply {newTempConfigFile}");

            string text = File.ReadAllText(newApsimxFilePath);

            Simulations simAfterSave = FileFormat.ReadFromString<Simulations>(text, e => throw e, false).NewModel as Simulations;
            Experiment experimentNode = simAfterSave.FindInScope<Experiment>();
            Assert.NotNull(experimentNode);
            Simulation simulation = experimentNode.FindInScope<Simulation>();
            Assert.NotNull(simulation);
        }

        [Test]
        public void TestInvalidCopyCommandThrowsException()
        {
            Simulations file = Utilities.GetRunnableSim();

            Simulation simulationNode = file.FindInScope<Simulation>();

            string apsimxFileName = file.FileName.Split('\\', '/').ToList().Last();

            string newFileString = @$"load {apsimxFileName}
copy [Simulation] [Experiment]
save {apsimxFileName}";

            string newTempConfigFile = Path.Combine(Path.GetTempPath(), "configCopyCommand.txt");
            File.WriteAllText(newTempConfigFile, newFileString);

            bool fileExists = File.Exists(newTempConfigFile);
            Assert.True(File.Exists(newTempConfigFile));

            Exception ex = Assert.Throws<Exception>(delegate { Utilities.RunModels($"--apply {newTempConfigFile}"); });
            //Assert.IsTrue(ex.Message.Contains("System.InvalidOperationException: Command 'delete [Simulations]' is an invalid command. [Simulations] node is the top-level node and cannot be deleted. Remove the command from your config file."));
        }
    }
}
