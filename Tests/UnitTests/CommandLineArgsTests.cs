using Models;
using Models.Core;
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

            Models.Report.Report report = Apsim.Find(file, typeof(Models.Report.Report)) as Models.Report.Report;
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
    }
}
