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
    /// Unit tests for the BBCHCanola class.
    /// </summary>
    [TestFixture]
    public class BBCHCanolaTests
    {
        /// <summary>
        /// Test that runs BBCHStageCanola.apsimx and validates the StageDAS values in the Report table.
        /// Checks the final DAS values for key BBCH stages: 1, 10, 31, 60, 69, 79, 87, and 90.
        /// </summary>
        [Test]
        public void TestBBCHStageReport()
        {
            // Load and run the BBCHStageCanola.apsimx simulation
            string path = Path.Combine("%root%", "Tests", "Simulation", "Zadoks", "BBCHStageCanola.apsimx");
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
                "[Canola].Phenology.Stage",
                "[Canola].Phenology.BBCH.Stage",
                "[Canola].Phenology.BBCH.StageDAS(1)",
                "[Canola].Phenology.BBCH.StageDAS(10)",
                "[Canola].Phenology.BBCH.StageDAS(31)",
                "[Canola].Phenology.BBCH.StageDAS(60)",
                "[Canola].Phenology.BBCH.StageDAS(69)",
                "[Canola].Phenology.BBCH.StageDAS(79)",
                "[Canola].Phenology.BBCH.StageDAS(87)",
                "[Canola].Phenology.BBCH.StageDAS(90)"
            ];

            // Run the simulation
            sim.Prepare();
            sim.Run();
            storage.Writer.Stop();
            storage.Reader.Refresh();

            // Read the Report table
            var dataTable = storage.Reader.GetData("Report");
            
            // Get the values for each stage from the report
            var stage1DAS = DataTableUtilities.GetColumnAsDoubles(dataTable, "Canola.Phenology.BBCH.StageDAS(1)", CultureInfo.InvariantCulture);
            var stage10DAS = DataTableUtilities.GetColumnAsDoubles(dataTable, "Canola.Phenology.BBCH.StageDAS(10)", CultureInfo.InvariantCulture);
            var stage31DAS = DataTableUtilities.GetColumnAsDoubles(dataTable, "Canola.Phenology.BBCH.StageDAS(31)", CultureInfo.InvariantCulture);
            var stage60DAS = DataTableUtilities.GetColumnAsDoubles(dataTable, "Canola.Phenology.BBCH.StageDAS(60)", CultureInfo.InvariantCulture);
            var stage69DAS = DataTableUtilities.GetColumnAsDoubles(dataTable, "Canola.Phenology.BBCH.StageDAS(69)", CultureInfo.InvariantCulture);
            var stage79DAS = DataTableUtilities.GetColumnAsDoubles(dataTable, "Canola.Phenology.BBCH.StageDAS(79)", CultureInfo.InvariantCulture);
            var stage87DAS = DataTableUtilities.GetColumnAsDoubles(dataTable, "Canola.Phenology.BBCH.StageDAS(87)", CultureInfo.InvariantCulture);
            var stage90DAS = DataTableUtilities.GetColumnAsDoubles(dataTable, "Canola.Phenology.BBCH.StageDAS(90)", CultureInfo.InvariantCulture);

            // Get the final values (last element in each array)
            double finalStage1 = stage1DAS.Last();
            double finalStage10 = stage10DAS.Last();
            double finalStage31 = stage31DAS.Last();
            double finalStage60 = stage60DAS.Last();
            double finalStage69 = stage69DAS.Last();
            double finalStage79 = stage79DAS.Last();
            double finalStage87 = stage87DAS.Last();
            double finalStage90 = stage90DAS.Last();

            // Expected values
            double expectedStage1 = 1.0;   
            double expectedStage10 = 4.0;  
            double expectedStage31 = 30.0;  
            double expectedStage60 = 105.0;  
            double expectedStage69 = 146.0;  
            double expectedStage79 = 177.0;  
            double expectedStage87 = 201.0;  
            double expectedStage90 = 202.0;  

            // Assertions - check each stage DAS value
            Assert.That(finalStage1, Is.EqualTo(expectedStage1), 
                $"BBCH Stage 1 DAS should be {expectedStage1}, but was {finalStage1}");
            
            Assert.That(finalStage10, Is.EqualTo(expectedStage10), 
                $"BBCH Stage 10 DAS should be {expectedStage10}, but was {finalStage10}");
            
            Assert.That(finalStage31, Is.EqualTo(expectedStage31), 
                $"BBCH Stage 31 DAS should be {expectedStage31}, but was {finalStage31}");
            
            Assert.That(finalStage60, Is.EqualTo(expectedStage60), 
                $"BBCH Stage 60 DAS should be {expectedStage60}, but was {finalStage60}");
            
            Assert.That(finalStage69, Is.EqualTo(expectedStage69), 
                $"BBCH Stage 69 DAS should be {expectedStage69}, but was {finalStage69}");
            
            Assert.That(finalStage79, Is.EqualTo(expectedStage79), 
                $"BBCH Stage 79 DAS should be {expectedStage79}, but was {finalStage79}");
            
            Assert.That(finalStage87, Is.EqualTo(expectedStage87), 
                $"BBCH Stage 87 DAS should be {expectedStage87}, but was {finalStage87}");
            
            Assert.That(finalStage90, Is.EqualTo(expectedStage90), 
                $"BBCH Stage 90 DAS should be {expectedStage90}, but was {finalStage90}");
        }

        /// <summary>
        /// Test that StageDAS throws an exception for invalid index 0 (out of valid range 1-90).
        /// </summary>
        [Test]
        public void TestStageDAS_InvalidIndex0()
        {
            // This test checks the behavior of StageDAS(0) to ensure it throws an exception
            string path = Path.Combine("%root%", "Tests", "Simulation", "Zadoks", "BBCHStageCanola.apsimx");
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
                "[Canola].Phenology.BBCH.StageDAS(0)"
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
            string path = Path.Combine("%root%", "Tests", "Simulation", "Zadoks", "BBCHStageCanola.apsimx");
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
                "[Canola].Phenology.BBCH.StageDAS(91)"
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
            string path = Path.Combine("%root%", "Tests", "Simulation", "Zadoks", "BBCHStageCanola.apsimx");
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
                "[Canola].Phenology.BBCH.StageDAS(-1)"
            ];

            sim.Prepare();

            // The exception should be thrown when trying to run the simulation with an invalid stage index
            Assert.Throws<SimulationException>(() => sim.Run(), 
                "StageDAS(-1) should throw an exception as negative indices are invalid");
        }
    }
}
