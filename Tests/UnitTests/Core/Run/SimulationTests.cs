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
            simulation.Prepare();
            simulation.Run();

            // Check that clock ticked.
            Assert.That((simulation.Children[0] as Clock).Today, Is.EqualTo(new DateTime(1980, 01, 02)));
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
            simulation.Prepare();
            Assert.Throws<SimulationException>(() => simulation.Run());

            // Make sure the error was sent to summary.
            var summary = simulation.FindDescendant<MockSummary>();
            Assert.That(summary.messages[0].Contains("Intentional exception"), Is.True);
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
            simulation.Prepare();
            simulation.Run();

            // Check that clock ticked.
            Assert.That((simulation.Children[0] as Clock).Today, Is.EqualTo(new DateTime(1980, 01, 02)));
        }

        [Serializable]
        class ModelThatDeletesAModel : Model
        {
            private string modelNameToRemove;

            public ModelThatDeletesAModel(string modelNameToDelete)
            {
                modelNameToRemove = modelNameToDelete;
            }

            public override void OnPreLink()
            {
                IModel modelToRemove = FindInScope(modelNameToRemove);
                modelToRemove.Parent.Children.Remove(modelToRemove);
            }
        }

        /// <summary>Ensures models receive a pre link call.</summary>
        [Test]
        public void EnsureOnPreLinkWorks()
        {
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
                        new MockModelThatThrows(),
                        new ModelThatDeletesAModel("MockModelThatThrows")
                    }
            };

            simulation.Run();

            // Should get to here and NOT throw in the call to Run above.
            Assert.That(true);
        }
    }
}