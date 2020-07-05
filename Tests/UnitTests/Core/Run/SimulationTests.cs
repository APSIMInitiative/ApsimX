namespace UnitTests.Core
{
    using Models;
    using Models.Core;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>This is a test class for the RunnableSimulationList class</summary>
    [TestFixture]
    public class SimulationTests
    {
        /// <summary>Ensure a single simulation runs.</summary>
        [Test]
        public void EnsureSimulationRuns()
        {
            // Create a simulation and add a datastore.
            var simulation = new Simulation()
            {
                Name = "Sim",
                FileName = Path.GetTempFileName(),
                Children = new List<IModel>()
                {
                    new Clock()
                    {
                        StartDate = new DateTime(1980, 1, 1),
                        EndDate = new DateTime(1980, 1, 2)
                    },
                    new MockSummary(),
  
                }
            };

            // Run simulation
            simulation.Run();

            // Check that clock ticked.
            Assert.AreEqual((simulation.Children[0] as Clock).Today, new DateTime(1980, 01, 02));
        }

        /// <summary>Ensures a simulation with exceptions throws.</summary>
        [Test]
        public void EnsureRunErrorsAreThrown()
        {
            // Create a simulation and add a datastore.
            var simulation = new Simulation()
            {
                Name = "Sim",
                FileName = Path.GetTempFileName(),
                Children = new List<IModel>()
                {
                    new Clock()
                    {
                        StartDate = new DateTime(1980, 1, 1),
                        EndDate = new DateTime(1980, 1, 2)
                    },
                    new MockSummary(),
                    new MockModelThatThrows()
                }
            };

            // Run simulation making sure it throws.
            Assert.Throws<Exception>(() => simulation.Run());

            // Make sure the error was sent to summary.
            Assert.IsTrue(MockSummary.messages[0].Contains("Intentional exception"));
        }

        /// <summary>Ensures a disable model does NOT participate in the simulation run.</summary>
        [Test]
        public void EnsureDisabledModelsAreNotRun()
        {
            // Create a simulation and add a datastore.
            var simulation = new Simulation()
            {
                Name = "Sim",
                FileName = Path.GetTempFileName(),
                Children = new List<IModel>()
                {
                    new Clock()
                    {
                        StartDate = new DateTime(1980, 1, 1),
                        EndDate = new DateTime(1980, 1, 2)
                    },
                    new MockSummary(),
                    new MockModelThatThrows()
                    {
                        Enabled = false
                    }
                }
            };

            // Run simulation making sure it throws.
            simulation.Run();

            // Check that clock ticked.
            Assert.AreEqual((simulation.Children[0] as Clock).Today, new DateTime(1980, 01, 02));
        }
    }
}
