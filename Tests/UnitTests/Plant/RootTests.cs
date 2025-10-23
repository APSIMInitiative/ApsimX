using APSIM.Core;
using APSIM.Shared.Utilities;
using Models;
using Models.Core;
using Models.Soils;
using Models.Storage;
using NUnit.Framework;
using System;
using System.Globalization;
using System.Linq;

namespace UnitTests.Core
{
    [TestFixture]
    public class RootTests
    {

        /// <summary>Test that the CalcFSW() function in Root class works as expected.</summary>
        [Test]
        public void TestRootCalcFASW()
        {
            Simulations simulations = Utilities.GetPlantTestingSimulation(true);
            Simulation sim = simulations.Node.FindChild<Simulation>(recurse: true);

            // Setup the report.
            Models.Report report = sim.Node.FindChild<Models.Report>(recurse: true);
            report.VariableNames = [
                "[Clock].Today",
                "[Wheat].Root.FASW as FASW",
                "[Wheat].Root.CalcFASW(100000) as FASW100000",
                "[Wheat].Root.CalcFASW(600) as FASW600"
            ];
            report.EventNames = [ "[Clock].DoManagement" ];

            // Initialise water start values
            Water water = sim.Node.FindChild<Water>(recurse: true);
            water.InitialValues = [0.521, 0.349, 0.280, 0.280, 0.280, 0.280, 0.280];
            Node.Create(simulations);

            // Configure the sowing rules
            Manager sowingRuleManager = sim.Node.FindChild<Manager>(name: "SowingRule", recurse: true);
            IPlant wheat = sim.Node.FindChild<IPlant>(name: "Wheat", recurse: true);
            sowingRuleManager.SetProperty("StartDate", "01-jan");
            sowingRuleManager.SetProperty("EndDate", "02-jan");
            sowingRuleManager.SetProperty("Crop", wheat);

            sim.Prepare();
            sim.Run();

            // Get the results.
            DataStore storage = simulations.Node.FindChild<DataStore>(recurse: true);
            storage.Writer.Stop();
            storage.Reader.Refresh();
            var dataTable = storage.Reader.GetData("Report");

            var fasw = DataTableUtilities.GetColumnAsDoubles(dataTable, "FASW", CultureInfo.InvariantCulture);
            var fasw100000 = DataTableUtilities.GetColumnAsDoubles(dataTable, "FASW100000", CultureInfo.InvariantCulture);
            var fasw600 = DataTableUtilities.GetColumnAsDoubles(dataTable, "FASW600", CultureInfo.InvariantCulture);

            // Assertions
            Assert.That(fasw.Length, Is.EqualTo(2));
            Assert.That(Math.Round(fasw.First(), 3), Is.EqualTo(0.220));
            Assert.That(Math.Round(fasw100000.First(), 3), Is.EqualTo(0.220));
            Assert.That(Math.Round(fasw600.First(), 3), Is.EqualTo(0.390));
        }
    }
}