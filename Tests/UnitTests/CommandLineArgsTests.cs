using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using APSIM.Shared.Documentation.Extensions;
using APSIM.Shared.Utilities;
using Models;
using Models.Core;
using Models.Core.ApsimFile;
using Models.Factorial;
using Models.PostSimulationTools;
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
            Assert.That(File.Exists(csvFile), Is.True, "Models.exe failed to create a csv file when passed the /Csv command line argument. Output of Models.exe: " + output);

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
            Assert.That(csvData, Is.EqualTo(expected));
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
            Assert.That(clock.StartDate.Year, Is.EqualTo(start.Year));
            Assert.That(clock.StartDate.DayOfYear, Is.EqualTo(start.DayOfYear));

            Assert.That(sim1.Name, Is.EqualTo("Sim1"));
            Assert.That(sim2.Enabled, Is.True);
            Assert.That(physical.Thickness[0], Is.EqualTo(150));
            Assert.That(physical.Thickness[1], Is.EqualTo(150));

            // Run Models.exe with /Edit command.
            var overrides = Overrides.ParseStrings(File.ReadAllLines(configFileName));
            Overrides.Apply(sims, overrides);

            // Get references to the changed models.
            clock = sims.FindInScope<Clock>();
            IClock clock2 = sims.FindByPath(".Simulations.SimulationVariant35.Clock", LocatorFlags.PropertiesOnly | LocatorFlags.IncludeDisabled)?.Value as Clock;

            // Sims should have at least 3 children - data store and the 2 sims.
            Assert.That(sims.Children.Count, Is.GreaterThan(2));
            sim1 = sims.Children.OfType<Simulation>().First();
            sim2 = sims.Children.OfType<Simulation>().Last();
            physical = sims.FindByPath(".Simulations.Sim1.Field.Soil.Physical")?.Value as IPhysical;

            start = new DateTime(2019, 1, 20);
            DateTime end = new DateTime(2019, 3, 20);

            // Check clock.
            Assert.That(clock.StartDate.Year, Is.EqualTo(start.Year));
            Assert.That(clock.StartDate.DayOfYear, Is.EqualTo(start.DayOfYear));
            Assert.That(clock.EndDate.Year, Is.EqualTo(end.Year));
            Assert.That(clock.EndDate.DayOfYear, Is.EqualTo(end.DayOfYear));

            // Clock 2 should have been changed as well.
            Assert.That(clock2.StartDate.Year, Is.EqualTo(start.Year));
            Assert.That(clock2.StartDate.DayOfYear, Is.EqualTo(start.DayOfYear));
            Assert.That(clock2.EndDate.Year, Is.EqualTo(2003));
            Assert.That(clock2.EndDate.DayOfYear, Is.EqualTo(319));

            // Sim2 should have been renamed to SimulationVariant35
            Assert.That(sim2.Name, Is.EqualTo("SimulationVariant35"));

            // Sim1's name should be unchanged.
            Assert.That(sim1.Name, Is.EqualTo("Sim1"));

            // Sim2 should have been disabled. This should not affect sim1.
            Assert.That(sim1.Enabled, Is.True);
            Assert.That(!sim2.Enabled, Is.True);

            // First 2 soil thicknesses have been changed to 500 and 2500 respectively.
            Assert.That(physical.Thickness[0], Is.EqualTo(500).Within(1e-8));
            Assert.That(physical.Thickness[1], Is.EqualTo(2500).Within(1e-8));
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

            Assert.That(stdout.Contains("sim1"), Is.True);
            Assert.That(stdout.Contains("originalSimAfterAdd"), Is.False);
            Assert.That(stdout.Contains("simulation3"), Is.False);
            Assert.That(stdout.Contains("Base"), Is.False);

            args = @"/Verbose /SimulationNameRegexPattern:sim1";
            stdout = Utilities.RunModels(sims, args);

            Assert.That(stdout.Contains("sim1"), Is.True);
            Assert.That(stdout.Contains("originalSimAfterAdd"), Is.False);
            Assert.That(stdout.Contains("simulation3"), Is.False);
            Assert.That(stdout.Contains("Base"), Is.False);

            args = @"/Verbose /SimulationNameRegexPattern:(simulation3)|(Base)";
            stdout = Utilities.RunModels(sims, args);

            Assert.That(stdout.Contains("sim1"), Is.False);
            Assert.That(stdout.Contains("originalSimAfterAdd"), Is.False);
            Assert.That(stdout.Contains("simulation3"), Is.True);
            Assert.That(stdout.Contains("Base"), Is.True);
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
            Assert.That(output, Is.EqualTo(expected));

            output = Utilities.RunModels(simpleExperiment, $"/ListSimulations /SimulationNameRegexPattern:.*Y1");
            expected = @"ExperimentX1Y1
ExperimentX2Y1
";
            Assert.That(output, Is.EqualTo(expected));

            // Disable the x factor. The disabled factor should not generate any simulations,
            // so the output should only contain the 2 simulations which modify y.
            simpleExperiment.Children[1].Children[0].Children[0].Children[0].Enabled = false;
            output = Utilities.RunModels(simpleExperiment, $"/ListSimulations");
            expected = @"ExperimentY1
ExperimentY2
";
            Assert.That(output, Is.EqualTo(expected));
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
            Assert.That(File.Exists(newTempConfigFile), Is.True);

            Utilities.RunModels(file, $"--apply {newTempConfigFile}");

            string text = File.ReadAllText(savingFilePath);
            // Reload simulation from file text. Needed to see changes made.
            Simulations sim2 = FileFormat.ReadFromString<Simulations>(text, e => throw e, false).NewModel as Simulations;

            // Get new values from changed simulation.
            Zone fieldNodeAfterChange = sim2.FindInScope<Zone>();
            // See if the report shows up as a second child of Field with a specific name.
            Models.Report newReportNode = fieldNodeAfterChange.FindChild<Models.Report>("Report1");
            Assert.That(newReportNode, Is.Not.Null);
        }

        [Test]
        public void TestApplySwitchAddFromAnotherApsimxFile()
        {
            Simulations file = Utilities.GetRunnableSim();
            Simulations file2 = Utilities.GetRunnableSim();
            Simulations file3 = Utilities.GetRunnableSim();

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
            Assert.That(File.Exists(newTempConfigFile), Is.True);
            Assert.That(File.Exists(newApsimFile), Is.True);

            Utilities.RunModels(file, $"--apply {newTempConfigFile}");

            string text = File.ReadAllText(savingApsimFileName);
            // Reload simulation from file text. Needed to see changes made.
            Simulations originalSimAfterAdd = FileFormat.ReadFromString<Simulations>(text, e => throw e, false).NewModel as Simulations;

            // Get new values from changed simulation.
            Zone fieldNodeAfterChange = originalSimAfterAdd.FindInScope<Zone>();
            // See if the report shows up as a second child of Field with a specific name.
            Models.Report newReportNode = fieldNodeAfterChange.FindChild<Models.Report>("Report1");
            Assert.That(newReportNode, Is.Not.Null);

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
            Assert.That(File.Exists(newTempConfigFile), Is.True);

            Utilities.RunModels(file, $"--apply {newTempConfigFile}");

            string text = File.ReadAllText(savingFilePath);
            // Reload simulation from file text. Needed to see changes made.
            Simulations sim2 = FileFormat.ReadFromString<Simulations>(text, e => throw e, false).NewModel as Simulations;

            // Get new values from changed simulation.
            Zone fieldNodeAfterChange = sim2.FindInScope<Zone>();
            // See if the report shows up as a second child of Field with a specific name.
            Models.Report ReportNodeThatShouldHaveBeenDeleted = fieldNodeAfterChange.FindChild<Models.Report>("Report");
            Assert.That(ReportNodeThatShouldHaveBeenDeleted, Is.Null);
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
            Assert.That(File.Exists(newTempConfigFile), Is.True);

            Utilities.RunModels(file, $"--apply {newTempConfigFile}");

            string text = File.ReadAllText(savingFilePath);
            // Reload simulation from file text. Needed to see changes made.
            Simulations sim2 = FileFormat.ReadFromString<Simulations>(text, e => throw e, false).NewModel as Simulations;

            // Get new values from changed ApsimX file.
            Simulation simulationCopyNodeAfterChange = sim2.FindInScope<Simulation>("SimulationCopy");
            Simulation originalSimulationAfterChange = sim2.FindInScope<Simulation>("Simulation");

            Assert.That(simulationCopyNodeAfterChange.Name, Is.Not.EqualTo(originalSimulationAfterChange.Name));
            Assert.That(simulationCopyNodeAfterChange, Is.Not.Null);
            Assert.That(originalSimulationAfterChange, Is.Not.Null);
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
            Assert.That(File.Exists(newTempConfigFile), Is.True);

            Utilities.RunModels(file, $"--apply {newTempConfigFile}");

            string text = File.ReadAllText(file.FileName);
            // Reload simulation from file text. Needed to see changes made.
            Simulations sim2 = FileFormat.ReadFromString<Simulations>(text, e => throw e, false).NewModel as Simulations;
            List<Models.Report> reportList = sim2.FindAllInScope<Models.Report>().ToList();
            Assert.That(reportList.Count, Is.LessThan(2));
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
            Assert.That(File.Exists(newTempConfigFile), Is.True);

            Utilities.RunModels($"--apply {newTempConfigFile}");

            string text = File.ReadAllText(Path.GetDirectoryName(newTempConfigFile) + Path.DirectorySeparatorChar + newSaveFileName);
            // Reload simulation from file text. Needed to see changes made.
            Simulations sim2 = FileFormat.ReadFromString<Simulations>(text, e => throw e, false).NewModel as Simulations;

            // Get new values from changed simulation.
            Zone fieldNodeAfterChange = sim2.FindInScope<Zone>();

            // See if the report shows up as a second child of Field with a specific name.
            Models.Report secondReportNodeThatShouldBePresent = fieldNodeAfterChange.FindChild<Models.Report>("Report1");
            Assert.That(secondReportNodeThatShouldBePresent, Is.Not.Null);

            // Make sure first sim was not modified.
            string firstSimText = File.ReadAllText(Path.GetDirectoryName(newTempConfigFile) + Path.DirectorySeparatorChar + simpleFileName);
            Simulations sim1 = FileFormat.ReadFromString<Simulations>(firstSimText, e => throw e, false).NewModel as Simulations;
            Zone fieldNodeFromOriginalSim = sim1.FindInScope<Zone>();
            Models.Report ReportNodeThatShouldNotBePresent = fieldNodeFromOriginalSim.FindChild<Models.Report>("Report1");
            Assert.That(ReportNodeThatShouldNotBePresent, Is.Null);
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
            Assert.That(File.Exists(newTempConfigFile), Is.True);

            Utilities.RunModels($"--apply {newTempConfigFile}");

            string text = File.ReadAllText(file.FileName);
            // Reload simulation from file text. Needed to see changes made.
            Simulations sim2 = FileFormat.ReadFromString<Simulations>(text, e => throw e, false).NewModel as Simulations;

            // Get new values from changed simulation.
            Zone fieldNodeAfterChange = sim2.FindInScope<Zone>();

            // See if the report shows up as a second child of Field with a specific name.
            Models.Report secondReportNodeThatShouldBePresent = fieldNodeAfterChange.FindChild<Models.Report>("Report1");
            Assert.That(secondReportNodeThatShouldBePresent, Is.Not.Null);
        }

        [Test]
        public void TestApplySwitchCreateFromConfigFile()
        {
            string newApsimxFileName = "newSim.apsimx";
            string newApsimxFilePath = Path.Combine(Path.GetTempPath(), newApsimxFileName);

            string newConfigFilePath = Path.Combine(Path.GetTempPath(), "configFile8.txt");
            string newCommandString = $"save {newApsimxFileName}";
            File.WriteAllText(newConfigFilePath, newCommandString);

            bool fileExists = File.Exists(newConfigFilePath);
            Assert.That(File.Exists(newConfigFilePath), Is.True);

            Utilities.RunModels($"--apply {newConfigFilePath}");

            string text = File.ReadAllText(newApsimxFilePath);
            // Reload simulation from file text. Needed to see changes made.
            Simulations sim = FileFormat.ReadFromString<Simulations>(text, e => throw e, false).NewModel as Simulations;

            // Ensure that the sim was created.
            Assert.That(sim, Is.Not.Null);
        }

        [Test]
        public void TestApplySwitchRunFromConfigFile()
        {
            string newSimName = "newSim1.apsimx";
            string newApsimxFilePath = Path.Combine(Path.GetTempPath(), newSimName);
            string newConfigFilePath = Path.Combine(Path.GetTempPath(), "configFile9.txt");

            string newDalbyMetFilePath = Path.Combine(Path.GetTempPath(), "dalby.met");

            string newCommands = $@"save {newSimName}
load {newSimName}
add [Simulations] Simulation
add [Simulation] Summary
add [Simulation] Clock
add [Simulation] Weather
[Weather].FileName={newDalbyMetFilePath}
[Clock].Start=1900/01/01
[Clock].End=1900/01/02
save {newSimName}
run";

            File.WriteAllText(newConfigFilePath, newCommands);
            bool configFileExists = File.Exists(newConfigFilePath);
            Assert.That(configFileExists, Is.True);


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
            Assert.That(dalbyMetFileExists, Is.True);

            Utilities.RunModels($"--apply {newConfigFilePath}");

            string text = File.ReadAllText(newApsimxFilePath);

            // Reload simulation from file text. Needed to see changes made.
            Simulations sim = FileFormat.ReadFromString<Simulations>(text, e => throw e, false).NewModel as Simulations;
            Summary summaryNode = sim.FindInScope<Summary>();
            var summaryFileText = summaryNode.GetMessages(sim.Name);
            // Ensure that the sim was created.
            Assert.That(summaryFileText, Is.Not.Null);
        }

        [Test]
        public void TestDeleteSimulationsCommandThrowsException()
        {
            Simulations file = Utilities.GetRunnableSim();

            Simulation simulationNode = file.FindInScope<Simulation>();

            string apsimxFileName = file.FileName.Split('\\', '/').ToList().Last();

            string newFileString = $"load {apsimxFileName}\ndelete [Simulations]";
            string newTempConfigFile = Path.Combine(Path.GetTempPath(), "config10.txt");
            File.WriteAllText(newTempConfigFile, newFileString);

            bool fileExists = File.Exists(newTempConfigFile);
            Assert.That(File.Exists(newTempConfigFile), Is.True);

            Exception ex = Assert.Throws<Exception>(delegate { Utilities.RunModels($"--apply {newTempConfigFile}"); });
            Assert.That(ex.Message.Contains("System.InvalidOperationException: Command 'delete [Simulations]' is an invalid command. [Simulations] node is the top-level node and cannot be deleted. Remove the command from your config file."), Is.True);

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

            string newTempConfigFile = Path.Combine(Path.GetTempPath(), "config11.txt");
            File.WriteAllText(newTempConfigFile, newFileString);

            bool fileExists = File.Exists(newTempConfigFile);
            Assert.That(File.Exists(newTempConfigFile), Is.True);

            Utilities.RunModels($"--apply {newTempConfigFile}");

            string text = File.ReadAllText(newApsimxFilePath);

            Simulations simAfterSave = FileFormat.ReadFromString<Simulations>(text, e => throw e, false).NewModel as Simulations;
            Experiment experimentNode = simAfterSave.FindInScope<Experiment>();
            Assert.That(experimentNode, Is.Not.Null);
            Simulation simulation = experimentNode.FindInScope<Simulation>();
            Assert.That(simulation, Is.Not.Null);
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

            string newTempConfigFile = Path.Combine(Path.GetTempPath(), "config12.txt");
            File.WriteAllText(newTempConfigFile, newFileString);

            bool fileExists = File.Exists(newTempConfigFile);
            Assert.That(File.Exists(newTempConfigFile), Is.True);

            Exception ex = Assert.Throws<Exception>(delegate { Utilities.RunModels($"--apply {newTempConfigFile}"); });
        }

        [Test]
        [Order(3)]
        public void TestSubsequentCommandDoesNotOverwriteTempSim()
        {
            Simulations file = Utilities.GetRunnableSim();

            string apsimxFileName = file.FileName.Split('\\', '/').ToList().Last();

            string newFileString = @$"load {apsimxFileName}
add [Simulations] Simulation
add [Simulations] Simulation
save {apsimxFileName}
";

            string newTempConfigFile = Path.Combine(Path.GetTempPath(), "config13.txt");
            File.WriteAllText(newTempConfigFile, newFileString);

            Utilities.RunModels($"--apply {newTempConfigFile}");
            string newApsimxFilePath = Path.Combine(Path.GetTempPath(), apsimxFileName);

            string text = File.ReadAllText(newApsimxFilePath);

            Simulations simAfterCommands = FileFormat.ReadFromString<Simulations>(text, e => throw e, false).NewModel as Simulations;
            Simulation thirdSim = simAfterCommands.FindInScope<Simulation>("Simulation2");
            Assert.That(thirdSim, Is.Not.Null);

        }

        [Test]
        [Order(4)]
        public void TestFactorOverrideIsApplied()
        {
            Simulations file = Utilities.GetRunnableSim();

            string apsimxFileName = file.FileName.Split('\\', '/').ToList().Last();

            string newFileString = @$"load {apsimxFileName}
add [Simulations] Experiment
add [Experiment] Factors
add [Factors] Factor
[Factor].Specification = [Fertilise at sowing].Script.Amount = 0 to 200 step 20
save {apsimxFileName}
";

            string newTempConfigFile = Path.Combine(Path.GetTempPath(), "config14.txt");
            File.WriteAllText(newTempConfigFile, newFileString);

            Utilities.RunModels($"--apply {newTempConfigFile}");
            string newApsimxFilePath = Path.Combine(Path.GetTempPath(), apsimxFileName);

            string text = File.ReadAllText(newApsimxFilePath);

            Simulations simAfterCommands = FileFormat.ReadFromString<Simulations>(text, e => throw e, false).NewModel as Simulations;
            Factor modifiedFactor = simAfterCommands.FindInScope<Factor>();
            Assert.That(new List<string>() { "[Fertilise at sowing].Script.Amount = 0 to 200 step 20" }, Does.Contain(modifiedFactor.Specification));
        }

        [Test]
        public void TestListReferencedFileNamesUnmodified()
        {
            Simulations file = Utilities.GetRunnableSim();

            // Add an excel file so that a file path is given when calling command.
            Simulation sim = file.FindAllChildren<Simulation>().First();

            string[] fileNames = { "example.xlsx" };

            ExcelInput excelInputNode = new ExcelInput()
            {
                FileNames = fileNames
            };

            sim.Children.Add(excelInputNode);

            string apsimxFileName = file.FileName.Split('\\', '/').ToList().Last();

            string outputText = Utilities.RunModels(file, "--list-referenced-filenames-unmodified");

            Assert.That(outputText.Contains("example.xlsx"), Is.True);

        }

        /// <summary>
        /// Test to make sure playlist switch works.
        /// </summary>
        [Test]
        public void TestPlaylistSwitch()
        {
            Simulations sims = Utilities.GetRunnableSim();
            string firstSimName = (sims.FindChild<Simulation>()).Name;
            Playlist newplaylist = new Playlist()
            {
                Name = "playlist",
                Text = firstSimName
            };
            sims.Children.Add(newplaylist);
            sims.Write(sims.FileName);
            string newTempConfigFile = Path.Combine(Path.GetTempPath(), "configCopyCommand.txt");
            string apsimxFileName = sims.FileName.Split('\\', '/').ToList().Last();
            string newFileString = @$"load {apsimxFileName}
        duplicate [Simulation] Simulation1
        save {apsimxFileName}
        run";

            File.WriteAllText(newTempConfigFile, newFileString);
            Utilities.RunModels($"--apply {newTempConfigFile} -p playlist");

            Simulations simsAfterRun = FileFormat.ReadFromFile<Simulations>(sims.FileName, e => throw e, false).NewModel as Simulations;
            DataStore datastore = simsAfterRun.FindChild<DataStore>();
            List<String> dataStoreNames = datastore.Reader.SimulationNames;
            Assert.That(dataStoreNames.Count, Is.EqualTo(1));
            Assert.That(dataStoreNames.First(), Is.EqualTo(firstSimName));
        }

        /// <summary>
        /// Tests that an exception is thrown when a playlist is specified that does not exist.
        /// </summary>
        [Test]
        public void TestPlaylistSwitchFailsGracefully()
        {
            Simulations sims = Utilities.GetRunnableSim();
            Assert.Throws<Exception>(() => Utilities.RunModels($"{sims.FileName} --playlist playlistNameThatDoesntExist --verbose"));
        }

        /// <summary>
        /// Tests to make sure Playlist continues to run if there is a difference in case in the specified Playlist name.
        /// </summary>
        [Test]
        public void TestPlaylistCaseInsensitivity()
        {
            Simulations sims = Utilities.GetRunnableSim();
            string firstSimName = sims.FindChild<Simulation>().Name;
            Playlist newplaylist = new Playlist()
            {
                Name = "playlist",
                Text = firstSimName
            };
            sims.Children.Add(newplaylist);
            sims.Write(sims.FileName);
            string newTempConfigFile = Path.Combine(Path.GetTempPath(), "configCopyCommand.txt");
            string apsimxFileName = sims.FileName.Split('\\', '/').ToList().Last();
            string newFileString = @$"load {apsimxFileName}
        duplicate [Simulation] Simulation1
        save {apsimxFileName}
        run";

            File.WriteAllText(newTempConfigFile, newFileString);
            Utilities.RunModels($"--apply {newTempConfigFile} -p Playlist");

            Simulations simsAfterRun = FileFormat.ReadFromFile<Simulations>(sims.FileName, e => throw e, false).NewModel as Simulations;
            DataStore datastore = simsAfterRun.FindChild<DataStore>();
            List<String> dataStoreNames = datastore.Reader.SimulationNames;
            Assert.That(dataStoreNames.Count, Is.EqualTo(1));
            Assert.That(dataStoreNames.First(), Is.EqualTo(firstSimName));
        }


        [Test]
        public void TestPlaylistDoesNotRunWhenDisabled()
        {
            Simulations sims = Utilities.GetRunnableSim();
            string firstSimName = (sims.FindChild<Simulation>()).Name;
            Playlist newplaylist = new Playlist()
            {
                Name = "playlist",
                Text = firstSimName,
                Enabled = false
            };
            sims.Children.Add(newplaylist);
            sims.Write(sims.FileName);
            string newTempConfigFile = Path.Combine(Path.GetTempPath(), "configCopyCommand.txt");
            string apsimxFileName = sims.FileName.Split('\\', '/').ToList().Last();
            string newFileString = @$"load {apsimxFileName}
        duplicate [Simulation] Simulation1
        save {apsimxFileName}
        run";

            File.WriteAllText(newTempConfigFile, newFileString);
            Assert.Throws<Exception>(() => Utilities.RunModels($"--apply {newTempConfigFile} -p playlist"));

        }

        [Test]
        public void TestApplySwitch_ConfigFileWithTwoRunStatements_RunsAppropriateFiles()
        {
            Simulations sims = Utilities.GetRunnableSim();
            string firstSimName = (sims.FindChild<Simulation>()).Name;

            string newTempConfigFile = Path.Combine(Path.GetTempPath(), "configCopyCommand.txt");
            string firstApsimxFileName = sims.FileName.Split('\\', '/').ToList().Last();
            string firstApsimxFileNameWithoutExtension = firstApsimxFileName.Split('.')[0];
            string newFileString =
@$"load {firstApsimxFileName}
copy [Simulation] [Simulations]
save {firstApsimxFileNameWithoutExtension + "1" + ".apsimx"}
run
load {firstApsimxFileName}
copy [Simulation] [Simulations]
save {firstApsimxFileNameWithoutExtension + "2" + ".apsimx"}
run";
            File.WriteAllText(newTempConfigFile, newFileString);
            Utilities.RunModels($"--apply {newTempConfigFile}");
            // Check that original file is unmodified.
            Simulations originalSims = FileFormat.ReadFromFile<Simulations>(sims.FileName, e => throw e, false).NewModel as Simulations;
            List<Simulation> simulations = originalSims.FindAllChildren<Simulation>().ToList();
            Assert.That(simulations.Count(), Is.EqualTo(1));
            // Check that 'Simulation1' has a duplicate simulation called 'Simulation1'.
            Simulations firstModdedSims = FileFormat.ReadFromFile<Simulations>(Path.GetTempPath() + firstApsimxFileNameWithoutExtension + "1" + ".apsimx", e => throw e, false).NewModel as Simulations;
            // Check that 'Simulation2' has a duplicate simulation called 'Simulation2'.
            Simulations secondModdedSims = FileFormat.ReadFromFile<Simulations>(Path.GetTempPath() + firstApsimxFileNameWithoutExtension + "2" + ".apsimx", e => throw e, false).NewModel as Simulations;


        }

        [Test]
        public void TestApplySwitch_WithConfigFileWithManagerOverride_ModifiesManager()
        {
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Resources.test-wheat.apsimx");
            Simulations sims = FileFormat.ReadFromString<IModel>(json, e => throw e, false).NewModel as Simulations;
            var originalPair = KeyValuePair.Create("StartDate", "1-may");
            Assert.That(sims.FindDescendant<Manager>("Sowing").Parameters.Contains(originalPair), Is.True);
            sims.FileName = "test-wheat.apsimx";
            string tempSimsFilePath = Path.Combine(Path.GetTempPath(), "test-wheat.apsimx");
            File.WriteAllText(tempSimsFilePath, json);

            string newTempConfigFile = Path.Combine(Path.GetTempPath(), "configCopyCommand.txt");
            string newFileString =
                $"load test-wheat.apsimx{Environment.NewLine}" +
                $"[Sowing].Script.StartDate = 2-May{Environment.NewLine}" +
                $"save test-wheat1.apsimx{Environment.NewLine}";

            File.WriteAllText(newTempConfigFile, newFileString);
            Utilities.RunModels($"--apply {newTempConfigFile}");

            //Check that StartDateParameter got modified to 2-May.
            Simulations moddedSim = FileFormat.ReadFromFile<Simulations>($"{Path.Combine(Path.GetTempPath(), "test-wheat1.apsimx")}", e => throw e, false).NewModel as Simulations;
            Manager manager = moddedSim.FindDescendant<Manager>("Sowing");
            var modifiedPair = KeyValuePair.Create("StartDate", "2-May");
            Assert.That(manager.Parameters.Contains(modifiedPair), Is.True);
        }
          
        /// <summary>
        /// Test log switch works as expected.
        /// </summary>
        [Test]
        public void LogSwitch_ChangesVerbosity_ToError()
        {
            Simulations sims = Utilities.GetRunnableSim();
            string simName = sims.FileName;
            string tempFilePath = Path.GetTempPath();
            Utilities.RunModels($"{simName} --log error");
            Simulations simAfterVerbosityChange = FileFormat.ReadFromFile<Simulations>(simName, e => throw e, true).NewModel as Simulations;
            Summary summary = simAfterVerbosityChange.FindDescendant<Summary>();
            Assert.That(summary.Verbosity == MessageType.Error, Is.True);
        }

        /// <summary>
        /// Test log switch throws an exception when a bad string is included.
        /// </summary>
        [Test]
        public void LogSwitch_ThrowsException_When_NonMatchingVerbosityType_IsUsed()
        {
            Simulations sims = Utilities.GetRunnableSim();
            string simName = sims.FileName;
            string tempFilePath = Path.GetTempPath();
            Assert.Throws<Exception>(() => Utilities.RunModels($"{simName} --log xyz"));
        }

        [Test]
        public void InMemoryDBSwitch_DoesNotFillDB()
        {
            Simulations sims = Utilities.GetRunnableSim();
            string simFileName = Path.GetFileNameWithoutExtension(sims.FileName);
            string dbFilePath = Path.GetTempPath() + simFileName + ".db";
            Utilities.RunModels($"{sims.FileName} --in-memory-db");
            var fileInfo = new FileInfo(dbFilePath);
            long fileLength = fileInfo.Length;
            Assert.That(fileLength, Is.EqualTo(4096));
        }

        [Test]
        public void InMemoryDBSwitch_WorksWithApplySwitch()
        {
            Simulations sims = Utilities.GetRunnableSim();
            string simFileNameWithoutExt = Path.GetFileNameWithoutExtension(sims.FileName);
            string simsFileName = Path.GetFileName(sims.FileName);
            string dbFilePath = Path.GetTempPath() + simFileNameWithoutExt + ".db";
            string commandsFilePath = Path.Combine(Path.GetTempPath(),"commands.txt");
            string newFileString = 
                $"load {simsFileName}{Environment.NewLine}" +
                $"duplicate [Simulation] Simulation1{Environment.NewLine}" +
                $"save {simFileNameWithoutExt + "-new.apsimx"}{Environment.NewLine}"+
                $"run{Environment.NewLine}";
            File.WriteAllText(commandsFilePath,newFileString);
            Utilities.RunModels($"--apply {commandsFilePath} --in-memory-db");
            var fileInfo = new FileInfo(dbFilePath);
            long fileLength = fileInfo.Length;
            Assert.That(fileLength, Is.EqualTo(4096));            
        }

        [Test]
        public void InMemoryDBSwitch_WorksWithApplySwitch_WithFile()
        {
            Simulations sims = Utilities.GetRunnableSim();
            string simFileNameWithoutExt = Path.GetFileNameWithoutExtension(sims.FileName);
            string simsFileName = Path.GetFileName(sims.FileName);
            string dbFilePath = Path.GetTempPath() + simFileNameWithoutExt + ".db";
            string commandsFilePath = Path.Combine(Path.GetTempPath(),"commands.txt");
            string newFileString = 
                $"duplicate [Simulation] Simulation1{Environment.NewLine}" +
                $"save {simFileNameWithoutExt + "-new.apsimx"}{Environment.NewLine}"+
                $"run{Environment.NewLine}";
            File.WriteAllText(commandsFilePath,newFileString);
            Utilities.RunModels($"{sims.FileName} --apply {commandsFilePath} --in-memory-db");
            var fileInfo = new FileInfo(dbFilePath);
            long fileLength = fileInfo.Length;
            Assert.That(fileLength, Is.EqualTo(4096));            
        }

        [Test]
        public void BatchSwitch_WorksWithApplySwitch_WithFile()
        {
            //Create simulation
            Simulations sims = Utilities.GetRunnableSim();
            string simFileNameWithoutExt = Path.GetFileNameWithoutExtension(sims.FileName);
            string simsFileName = Path.GetFileName(sims.FileName);
            string simsFilePath = Path.Combine(Path.GetTempPath(), simsFileName);

            // Create config file.
            string commandsFilePath = Path.Combine(Path.GetTempPath(),"commands.txt");
            string newFileString = 
                $"[Simulation].Name=$sim-name{Environment.NewLine}" +
                $"save {simFileNameWithoutExt + "-new.apsimx"}{Environment.NewLine}"+
                $"run{Environment.NewLine}";
            File.WriteAllText(commandsFilePath,newFileString);

            // Create a batch file
            string batchFilePath = Path.Combine(Path.GetTempPath(), "batch.csv");
            string batchContents =
                $"sim-name,{Environment.NewLine}" +
                $"SpecialSimulation,{Environment.NewLine}";
            File.WriteAllText(batchFilePath, batchContents);

            Utilities.RunModels($"{sims.FileName} --apply {commandsFilePath} --batch {batchFilePath}");
            Simulation originalSim = (FileFormat.ReadFromFile<Simulations>(simsFilePath, e => throw e, true).NewModel as Simulations).FindChild<Simulation>();
            // Makes sure the originals' Name is not modified.
            Assert.That(originalSim.Name, Is.EqualTo("Simulation"));
            // Makes sure the new files' Simulation name is modified.
            string newSimFilePath = Path.Combine(Path.GetTempPath(), simFileNameWithoutExt + "-new.apsimx");
            Simulation newSim = (FileFormat.ReadFromFile<Simulations>(newSimFilePath, e => throw e, true).NewModel as Simulations).FindChild<Simulation>();
            Assert.That(newSim.Name, Is.EqualTo("SpecialSimulation"));
        }

        [Test]
        public void BatchSwitch_WorksWithApplySwitch()
        {
            //Create simulation
            Simulations sims = Utilities.GetRunnableSim();
            string simFileNameWithoutExt = Path.GetFileNameWithoutExtension(sims.FileName);
            string simsFileName = Path.GetFileName(sims.FileName);
            string simsFilePath = Path.Combine(Path.GetTempPath(), simsFileName);

            // Create config file.
            string commandsFilePath = Path.Combine(Path.GetTempPath(),"commands.txt");
            string newFileString = 
                $"load {simsFileName}{Environment.NewLine}" +
                $"[Simulation].Name=$sim-name{Environment.NewLine}" +
                $"save {simFileNameWithoutExt + "-new.apsimx"}{Environment.NewLine}"+
                $"run{Environment.NewLine}";
            File.WriteAllText(commandsFilePath,newFileString);

            // Create a batch file
            string batchFilePath = Path.Combine(Path.GetTempPath(), "batch.csv");
            string batchContents =
                $"sim-name,{Environment.NewLine}" +
                $"SpecialSimulation,{Environment.NewLine}";
            File.WriteAllText(batchFilePath, batchContents);

            Utilities.RunModels($"--apply {commandsFilePath} --batch {batchFilePath}");
            Simulation originalSim = (FileFormat.ReadFromFile<Simulations>(simsFilePath, e => throw e, true).NewModel as Simulations).FindChild<Simulation>();
            // Makes sure the originals' Name is not modified.
            Assert.That(originalSim.Name, Is.EqualTo("Simulation"));
            // Makes sure the new files' Simulation name is modified.
            string newSimFilePath = Path.Combine(Path.GetTempPath(), simFileNameWithoutExt + "-new.apsimx");
            Simulation newSim = (FileFormat.ReadFromFile<Simulations>(newSimFilePath, e => throw e, true).NewModel as Simulations).FindChild<Simulation>();
            Assert.That(newSim.Name, Is.EqualTo("SpecialSimulation"));
        }

        [Test]
        public void Test_ListEnabledSimulationNames_OnlyShowsEnabledSimulations()
        {
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Resources.TwodisabledSimsOneEnabled.apsimx");
            Simulations sims = FileFormat.ReadFromString<IModel>(json, e => throw e, false).NewModel as Simulations;
            sims.FileName = "TwodisabledSimsOneEnabled.apsimx";
            string tempSimsFilePath = Path.Combine(Path.GetTempPath(), sims.FileName);
            File.WriteAllText(tempSimsFilePath, json);
            var actual = Utilities.RunModels($"{tempSimsFilePath} -e");
            string expected = $"Simulation{Environment.NewLine}";
            Assert.That(actual, Is.EqualTo(expected));
        }

    }
}
