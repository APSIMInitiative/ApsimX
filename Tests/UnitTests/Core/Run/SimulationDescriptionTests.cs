namespace UnitTests.Core.Run
{
    using Models.Core;
    using Models.Factorial;
    using Models.Core.Run;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using UnitTests.Weather;

    /// <summary>This is a test class for the SimulationDescription class</summary>
    [TestFixture]
    public class SimulationDescriptionTests
    {
        /// <summary>Ensure a property set overrides work.</summary>
        [Test]
        public void EnsurePropertyReplacementsWork()
        {
            var sim = new Simulation()
            {
                Name = "BaseSimulation",
                Children = new List<Model>()
                {
                    new MockWeather()
                    {
                        Name = "Weather",
                        MaxT = 1,
                        StartDate = DateTime.MinValue
                    },
                }
            };
            Apsim.ParentAllChildren(sim);

            var simulationDescription = new SimulationDescription(sim, "CustomName");
            simulationDescription.AddOverride(new PropertyReplacement("Weather.MaxT", 2));

            var newSim = simulationDescription.ToSimulation();

            var weather = newSim.Children[0] as MockWeather;
            Assert.AreEqual(weather.MaxT, 2);
        }

        /// <summary>Ensure a model override work.</summary>
        [Test]
        public void EnsureModelOverrideWork()
        {
            var sim = new Simulation()
            {
                Name = "BaseSimulation",
                Children = new List<Model>()
                {
                    new MockWeather()
                    {
                        Name = "Weather",
                        MaxT = 1,
                        StartDate = DateTime.MinValue
                    },
                }
            };
            Apsim.ParentAllChildren(sim);

            var replacementWeather = new MockWeather()
            {
                Name = "Weather2",
                MaxT = 2,
                StartDate = DateTime.MinValue
            };
            
            var simulationDescription = new SimulationDescription(sim, "CustomName");
            simulationDescription.AddOverride(new ModelReplacement("Weather", replacementWeather));

            var newSim = simulationDescription.ToSimulation();
            Assert.AreEqual(newSim.Name, "CustomName");

            var weather = newSim.Children[0] as MockWeather;
            Assert.AreEqual(weather.MaxT, 2);

            // The name of the new model should be the same as the original model.
            Assert.AreEqual(weather.Name, "Weather");
        }

        /// <summary>Ensure a model replacement override work.</summary>
        [Test]
        public void EnsureReplacementsNodeWorks()
        {
            var simulations = new Simulations()
            {
                Children = new List<Model>()
                {
                    new Folder()
                    {
                        Name = "Replacements",
                        Children = new List<Model>()
                        {
                            new MockWeather()
                            {
                                Name = "Weather",
                                MaxT = 2,
                                StartDate = DateTime.MinValue
                            }
                        }
                    },

                    new Simulation()
                    {
                        Name = "BaseSimulation",
                        Children = new List<Model>()
                        {
                            new MockWeather()
                            {
                                Name = "Weather",
                                MaxT = 1,
                                StartDate = DateTime.MinValue
                            },
                        }
                    }
                }
            };
            Apsim.ParentAllChildren(simulations);

            var sim = simulations.Children[1] as Simulation;
            var simulationDescription = new SimulationDescription(sim);

            var newSim = simulationDescription.ToSimulation(simulations);
            var weather = newSim.Children[0] as MockWeather;
            Assert.AreEqual(weather.MaxT, 2);

            // Make sure any property overrides happens after a model replacement.
            simulationDescription.AddOverride(new PropertyReplacement("Weather.MaxT", 3));
            newSim = simulationDescription.ToSimulation(simulations);
            weather = newSim.Children[0] as MockWeather;
            Assert.AreEqual(weather.MaxT, 3);

        }

        /// <summary>Ensure a model replacement that has a name that doesn't match won't replace anything.</summary>
        [Test]
        public void EnsureReplacementWithInvalidNameDoesntMatch()
        {
            var simulations = new Simulations()
            {
                Children = new List<Model>()
                {
                    new Folder()
                    {
                        Name = "Replacements",
                        Children = new List<Model>()
                        {
                            new MockWeather()
                            {
                                Name = "Dummy name",
                                MaxT = 2,
                                StartDate = DateTime.MinValue
                            }
                        }
                    },

                    new Simulation()
                    {
                        Name = "BaseSimulation",
                        Children = new List<Model>()
                        {
                            new MockWeather()
                            {
                                Name = "Weather",
                                MaxT = 1,
                                StartDate = DateTime.MinValue
                            },
                        }
                    }
                }
            };
            Apsim.ParentAllChildren(simulations);

            var sim = simulations.Children[1] as Simulation;
            var simulationDescription = new SimulationDescription(sim);

            var newSim = simulationDescription.ToSimulation(simulations);
            var weather = newSim.Children[0] as MockWeather;

            // Name ('Dummy name') didn't match so property should still be 1.
            Assert.AreEqual(weather.MaxT, 1);
        }

    }
}
