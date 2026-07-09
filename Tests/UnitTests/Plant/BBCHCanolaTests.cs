using APSIM.Core;
using APSIM.Shared.Utilities;
using Models;
using Models.Agroforestry;
using Models.Core;
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
                "[Canola].Phenology.BBCH.StageDAS(30)",
                "[Canola].Phenology.BBCH.StageDAS(60)",
                "[Canola].Phenology.BBCH.StageDAS(65)",
                "[Canola].Phenology.BBCH.StageDAS(75)",
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
            var stage30DAS = DataTableUtilities.GetColumnAsDoubles(dataTable, "Canola.Phenology.BBCH.StageDAS(30)", CultureInfo.InvariantCulture);
            var stage60DAS = DataTableUtilities.GetColumnAsDoubles(dataTable, "Canola.Phenology.BBCH.StageDAS(60)", CultureInfo.InvariantCulture);
            var stage65DAS = DataTableUtilities.GetColumnAsDoubles(dataTable, "Canola.Phenology.BBCH.StageDAS(65)", CultureInfo.InvariantCulture);
            var stage75DAS = DataTableUtilities.GetColumnAsDoubles(dataTable, "Canola.Phenology.BBCH.StageDAS(75)", CultureInfo.InvariantCulture);
            var stage87DAS = DataTableUtilities.GetColumnAsDoubles(dataTable, "Canola.Phenology.BBCH.StageDAS(87)", CultureInfo.InvariantCulture);
            var stage90DAS = DataTableUtilities.GetColumnAsDoubles(dataTable, "Canola.Phenology.BBCH.StageDAS(90)", CultureInfo.InvariantCulture);

            // Get the final values (last element in each array)
            double finalStage1 = stage1DAS.Last();
            double finalStage10 = stage10DAS.Last();
            double finalStage30 = stage30DAS.Last();
            double finalStage60 = stage60DAS.Last();
            double finalStage65 = stage65DAS.Last();
            double finalStage75 = stage75DAS.Last();
            double finalStage87 = stage87DAS.Last();
            double finalStage90 = stage90DAS.Last();

            // Expected values
            double expectedStage1 = 1.0;   
            double expectedStage10 = 4.0;  
            double expectedStage30 = 30.0;  
            double expectedStage60 = 105.0;  
            double expectedStage65 = 123.0;  
            double expectedStage75 = 142.0;  
            double expectedStage87 = 201.0;  
            double expectedStage90 = 202.0;  

            // Assertions - check each stage DAS value
            Assert.That(finalStage1, Is.EqualTo(expectedStage1), 
                $"BBCH Stage 1 DAS should be {expectedStage1}, but was {finalStage1}");
            
            Assert.That(finalStage10, Is.EqualTo(expectedStage10), 
                $"BBCH Stage 10 DAS should be {expectedStage10}, but was {finalStage10}");
            
            Assert.That(finalStage30, Is.EqualTo(expectedStage30), 
                $"BBCH Stage 30 DAS should be {expectedStage30}, but was {finalStage30}");
            
            Assert.That(finalStage60, Is.EqualTo(expectedStage60), 
                $"BBCH Stage 60 DAS should be {expectedStage60}, but was {finalStage60}");
            
            Assert.That(finalStage65, Is.EqualTo(expectedStage65), 
                $"BBCH Stage 65 DAS should be {expectedStage65}, but was {finalStage65}");
            
            Assert.That(finalStage75, Is.EqualTo(expectedStage75), 
                $"BBCH Stage 75 DAS should be {expectedStage75}, but was {finalStage75}");
            
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
