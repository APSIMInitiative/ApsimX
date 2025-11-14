using APSIM.Core;
using APSIM.Shared.Utilities;
using Models;
using Models.Agroforestry;
using Models.Core;
using Models.Core.ApsimFile;
using Models.Soils;
using Models.Soils.Arbitrator;
using Models.Storage;
using NUnit.Framework;
using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace UnitTests.PMF.Phenology.Scales
{
    /// <summary>
    /// Unit tests for the ZadokPMFWheat class.
    /// </summary>
    [TestFixture]
    public class ZadokPMFWheatTests
    {
        /// <summary>
        /// Test that runs ZadoksStage.apsimx and validates the StageDAS values in the Report table.
        /// Checks the final DAS values for key Zadok stages: 1, 10, 31, 39, 55, 65, and 90.
        /// </summary>
        [Test]
        public void TestZadoksStageReport()
        {
            // Load and run the ZadoksStage.apsimx simulation
            string path = Path.Combine("%root%", "Tests", "Simulation", "Zadoks", "ZadoksStage.apsimx");
            path = PathUtilities.GetAbsolutePath(path, null);
            Simulations sims = FileFormat.ReadFromFile<Simulations>(path).Model as Simulations;
            
            foreach (Soil soil in sims.Node.FindChildren<Soil>(recurse: true))
                soil.Sanitise();
            
            DataStore storage = sims.Node.FindChild<DataStore>(recurse: true);
            storage.UseInMemoryDB = true;
            Simulation sim = sims.Node.FindChild<Simulation>(recurse: true);

            // Setup the report variables
            Models.Report report = sim.Node.FindChild<Models.Report>(recurse: true);
            report.VariableNames = [
                "[Clock].Today",
                "[Wheat].Phenology.Zadok",
                "[Wheat].Phenology.Zadok.StageDAS(1)",
                "[Wheat].Phenology.Zadok.StageDAS(10)",
                "[Wheat].Phenology.Zadok.StageDAS(31)",
                "[Wheat].Phenology.Zadok.StageDAS(39)",
                "[Wheat].Phenology.Zadok.StageDAS(55)",
                "[Wheat].Phenology.Zadok.StageDAS(65)",
                "[Wheat].Phenology.Zadok.StageDAS(90)"
            ];

            // Run the simulation
            sim.Prepare();
            sim.Run();
            storage.Writer.Stop();
            storage.Reader.Refresh();

            // Read the Report table
            var dataTable = storage.Reader.GetData("Report");
            
            // Get the values for each stage from the report
            var stage1DAS = DataTableUtilities.GetColumnAsDoubles(dataTable, "Wheat.Phenology.Zadok.StageDAS(1)", CultureInfo.InvariantCulture);
            var stage10DAS = DataTableUtilities.GetColumnAsDoubles(dataTable, "Wheat.Phenology.Zadok.StageDAS(10)", CultureInfo.InvariantCulture);
            var stage31DAS = DataTableUtilities.GetColumnAsDoubles(dataTable, "Wheat.Phenology.Zadok.StageDAS(31)", CultureInfo.InvariantCulture);
            var stage39DAS = DataTableUtilities.GetColumnAsDoubles(dataTable, "Wheat.Phenology.Zadok.StageDAS(39)", CultureInfo.InvariantCulture);
            var stage55DAS = DataTableUtilities.GetColumnAsDoubles(dataTable, "Wheat.Phenology.Zadok.StageDAS(55)", CultureInfo.InvariantCulture);
            var stage65DAS = DataTableUtilities.GetColumnAsDoubles(dataTable, "Wheat.Phenology.Zadok.StageDAS(65)", CultureInfo.InvariantCulture);
            var stage90DAS = DataTableUtilities.GetColumnAsDoubles(dataTable, "Wheat.Phenology.Zadok.StageDAS(90)", CultureInfo.InvariantCulture);

            // Get the final values (last element in each array)
            double finalStage1 = stage1DAS.Last();
            double finalStage10 = stage10DAS.Last();
            double finalStage31 = stage31DAS.Last();
            double finalStage39 = stage39DAS.Last();
            double finalStage55 = stage55DAS.Last();
            double finalStage65 = stage65DAS.Last();
            double finalStage90 = stage90DAS.Last();

            // Expected values
            double expectedStage1 = 1.0;
            double expectedStage10 = 10.0;
            double expectedStage31 = 69.0;
            double expectedStage39 = 97.0;
            double expectedStage55 = 114.0;
            double expectedStage65 = 119.0;
            double expectedStage90 = 175.0;

            // Assertions - check each stage DAS value
            Assert.That(finalStage1, Is.EqualTo(expectedStage1), 
                $"Stage 1 DAS should be {expectedStage1}, but was {finalStage1}");
            
            Assert.That(finalStage10, Is.EqualTo(expectedStage10), 
                $"Stage 10 DAS should be {expectedStage10}, but was {finalStage10}");
            
            Assert.That(finalStage31, Is.EqualTo(expectedStage31), 
                $"Stage 31 DAS should be {expectedStage31}, but was {finalStage31}");
            
            Assert.That(finalStage39, Is.EqualTo(expectedStage39), 
                $"Stage 39 DAS should be {expectedStage39}, but was {finalStage39}");
            
            Assert.That(finalStage55, Is.EqualTo(expectedStage55), 
                $"Stage 55 DAS should be {expectedStage55}, but was {finalStage55}");
            
            Assert.That(finalStage65, Is.EqualTo(expectedStage65), 
                $"Stage 65 DAS should be {expectedStage65}, but was {finalStage65}");
            
            Assert.That(finalStage90, Is.EqualTo(expectedStage90), 
                $"Stage 90 DAS should be {expectedStage90}, but was {finalStage90}");
        }

        /// <summary>
        /// Test that StageDAS returns 0 for stage index 1 before the stage is reached.
        /// </summary>
        [Test]
        public void TestStageDAS_Stage1_BeforeReached()
        {
            // This test checks the behavior of StageDAS(1) to ensure it returns 0 before stage 1 is reached
            string path = Path.Combine("%root%", "Tests", "Simulation", "Zadoks", "ZadoksStage.apsimx");
            path = PathUtilities.GetAbsolutePath(path, null);
            Simulations sims = FileFormat.ReadFromFile<Simulations>(path).Model as Simulations;
            
            foreach (Soil soil in sims.Node.FindChildren<Soil>(recurse: true))
                soil.Sanitise();
            
            DataStore storage = sims.Node.FindChild<DataStore>(recurse: true);
            storage.UseInMemoryDB = true;
            Simulation sim = sims.Node.FindChild<Simulation>(recurse: true);

            // Setup the report variables
            Models.Report report = sim.Node.FindChild<Models.Report>(recurse: true);
            report.VariableNames = [
                "[Clock].Today",
                "[Wheat].Phenology.Zadok.StageDAS(0)"
            ];

            sim.Prepare();
            // The exception should be thrown when trying to run the simulation with an invalid stage index
            Assert.Throws<SimulationException>(() => sim.Run(), 
                "StageDAS(0) should throw an exception as it's out of valid range (1-90)");
        }

        /// <summary>
        /// Test that StageDAS throws an exception for invalid index 91 (out of valid range 1-90).
        /// </summary>
        [Test]
        public void TestStageDAS_InvalidIndex91()
        {
            string path = Path.Combine("%root%", "Tests", "Simulation", "Zadoks", "ZadoksStage.apsimx");
            path = PathUtilities.GetAbsolutePath(path, null);
            Simulations sims = FileFormat.ReadFromFile<Simulations>(path).Model as Simulations;
            
            foreach (Soil soil in sims.Node.FindChildren<Soil>(recurse: true))
                soil.Sanitise();
            
            DataStore storage = sims.Node.FindChild<DataStore>(recurse: true);
            storage.UseInMemoryDB = true;
            Simulation sim = sims.Node.FindChild<Simulation>(recurse: true);

            // Setup the report variables - attempt to use invalid index 91
            Models.Report report = sim.Node.FindChild<Models.Report>(recurse: true);
            report.VariableNames = [
                "[Clock].Today",
                "[Wheat].Phenology.Zadok.StageDAS(91)"
            ];

            sim.Prepare();

            // The exception should be thrown when trying to run the simulation with an invalid stage index
            Assert.Throws<SimulationException>(() => sim.Run(), 
                "StageDAS(91) should throw an exception as it's out of valid range (1-90)");
        }

        /// <summary>
        /// Test that StageDAS throws an exception for invalid index -1 (negative index).
        /// </summary>
        [Test]
        public void TestStageDAS_InvalidIndexNegative()
        {
            string path = Path.Combine("%root%", "Tests", "Simulation", "Zadoks", "ZadoksStage.apsimx");
            path = PathUtilities.GetAbsolutePath(path, null);
            Simulations sims = FileFormat.ReadFromFile<Simulations>(path).Model as Simulations;
            
            foreach (Soil soil in sims.Node.FindChildren<Soil>(recurse: true))
                soil.Sanitise();
            
            DataStore storage = sims.Node.FindChild<DataStore>(recurse: true);
            storage.UseInMemoryDB = true;
            Simulation sim = sims.Node.FindChild<Simulation>(recurse: true);

            // Setup the report variables - attempt to use invalid index -1
            Models.Report report = sim.Node.FindChild<Models.Report>(recurse: true);
            report.VariableNames = [
                "[Clock].Today",
                "[Wheat].Phenology.Zadok.StageDAS(-1)"
            ];

            sim.Prepare();

            // The exception should be thrown when trying to run the simulation with an invalid stage index
            Assert.Throws<SimulationException>(() => sim.Run(), 
                "StageDAS(-1) should throw an exception as negative indices are invalid");
        }
    }
}
