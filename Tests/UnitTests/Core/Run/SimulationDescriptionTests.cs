﻿namespace UnitTests.Core.Run
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Core.ApsimFile;
    using Models.Core.Run;
    using Models.Soils;
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
                Children = new List<IModel>()
                {
                    new MockWeather()
                    {
                        Name = "Weather",
                        MaxT = 1,
                        StartDate = DateTime.MinValue
                    },
                }
            };
            sim.ParentAllDescendants();

            var simulationDescription = new SimulationDescription(sim, "CustomName");
            simulationDescription.AddOverride(new Overrides.Override("Weather.MaxT", 2, Overrides.Override.MatchTypeEnum.NameAndType));

            var newSim = simulationDescription.ToSimulation();

            var weather = newSim.Children[0] as MockWeather;
            Assert.That(weather.MaxT, Is.EqualTo(2));
        }

        /// <summary>Ensure a model override work.</summary>
        [Test]
        public void EnsureModelOverrideWork()
        {
            var sim = new Simulation()
            {
                Name = "BaseSimulation",
                Children = new List<IModel>()
                {
                    new MockWeather()
                    {
                        Name = "Weather",
                        MaxT = 1,
                        StartDate = DateTime.MinValue
                    },
                }
            };
            sim.ParentAllDescendants();

            var replacementWeather = new MockWeather()
            {
                Name = "Weather2",
                MaxT = 2,
                StartDate = DateTime.MinValue
            };
            
            var simulationDescription = new SimulationDescription(sim, "CustomName");
            simulationDescription.AddOverride(new Overrides.Override("Weather", replacementWeather, Overrides.Override.MatchTypeEnum.NameAndType));

            var newSim = simulationDescription.ToSimulation();
            Assert.That(newSim.Name, Is.EqualTo("CustomName"));

            var weather = newSim.Children[0] as MockWeather;
            Assert.That(weather.MaxT, Is.EqualTo(2));

            // The name of the new model should be the same as the original model.
            Assert.That(weather.Name, Is.EqualTo("Weather"));
        }

        /// <summary>Ensure a model replacement override work.</summary>
        [Test]
        public void EnsureReplacementsNodeWorks()
        {
            var simulations = new Simulations()
            {
                Children = new List<IModel>()
                {
                    new Folder()
                    {
                        Name = "Replacements",
                        Children = new List<IModel>()
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
                        Children = new List<IModel>()
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
            simulations.ParentAllDescendants();

            var sim = simulations.Children[1] as Simulation;
            var simulationDescription = new SimulationDescription(sim);

            var newSim = simulationDescription.ToSimulation();
            var weather = newSim.Children[0] as MockWeather;
            Assert.That(weather.MaxT, Is.EqualTo(2));

            // Make sure any property overrides happens after a model replacement.
            simulationDescription.AddOverride(new Overrides.Override("Weather.MaxT", 3, Overrides.Override.MatchTypeEnum.NameAndType));
            newSim = simulationDescription.ToSimulation();
            weather = newSim.Children[0] as MockWeather;
            Assert.That(weather.MaxT, Is.EqualTo(3));

        }

        /// <summary>Ensure a model replacement that has a name that doesn't match won't replace anything.</summary>
        [Test]
        public void EnsureReplacementWithInvalidNameDoesntMatch()
        {
            var simulations = new Simulations()
            {
                Children = new List<IModel>()
                {
                    new Folder()
                    {
                        Name = "Replacements",
                        Children = new List<IModel>()
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
                        Children = new List<IModel>()
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
            simulations.ParentAllDescendants();

            var sim = simulations.Children[1] as Simulation;
            var simulationDescription = new SimulationDescription(sim);

            var newSim = simulationDescription.ToSimulation();
            var weather = newSim.Children[0] as MockWeather;

            // Name ('Dummy name') didn't match so property should still be 1.
            Assert.That(weather.MaxT, Is.EqualTo(1));
        }

        /// <summary>
        /// Ensure the soil in a simulation is NOT standardised when ToSimulation is called.
        /// This cannot happen during ToSimulation(), as we need to wait until disabled
        /// models have been removed and other property/model replacements have been applied.
        /// </summary>
        [Test]
        public void EnsureSoilIsStandardised()
        {
            var sim = new Simulation()
            {
                Name = "Simulation",
                Children = new List<IModel>()
                {
                    new Soil
                    {
                        Children = new List<IModel>()
                        {
                            new Physical()
                            {
                                Thickness = new double[] { 100, 300, 300 },
                                BD = new double[] { 1.36, 1.216, 1.24 },
                                AirDry = new double[] { 0.135, 0.214, 0.261 },
                                LL15 = new double[] { 0.27, 0.267, 0.261 },
                                DUL = new double[] { 0.365, 0.461, 0.43 },
                                SAT = new double[] { 0.400, 0.481, 0.45 },

                                Children = new List<IModel>()
                                {
                                    new SoilCrop
                                    {
                                        Name = "Wheat",
                                        KL = new double[] { 0.06, 0.060, 0.060 },
                                        LL = new double[] { 0.27, 0.267, 0.261 }
                                    }
                                }
                            },
                            new Models.WaterModel.WaterBalance(),
                            new Organic
                            {
                                Thickness = new double[] { 100, 300 },
                                Carbon = new double[] { 2, 1 }
                            },
                            new Solute
                            {
                                Name = "CL",
                                Thickness = new double[] { 100, 200 },
                                InitialValues = new double[] { 38, double.NaN },
                                InitialValuesUnits = Solute.UnitsEnum.ppm
                            },
                            new Water
                            {
                                Thickness = new double[] { 500 },
                                InitialValues = new double[] { 0.103 },
                            },
                            new CERESSoilTemperature(),
                        }
                    }
                }
            };
            sim.ParentAllDescendants();

            var originalSoil = sim.Children[0] as Soil;
            var originalWater = originalSoil.Children[0] as Physical;
            var originalSoilOM = originalSoil.Children[2] as Organic;

            originalSoil.OnCreated();
            
            var simulationDescription = new SimulationDescription(sim);

            var newSim = simulationDescription.ToSimulation();

            var physical = newSim.Children[0].Children[0] as Physical;
            var soilOrganicMatter = newSim.Children[0].Children[2] as Organic;
            var water = newSim.Children[0].Children[4] as Water;

            // Make sure layer structures have been standardised.
            Assert.That(physical.Thickness, Is.EqualTo(originalWater.Thickness), "soilwat thickness is incorrect");
            Assert.That(soilOrganicMatter.Thickness, Is.EqualTo(originalSoilOM.Thickness), "soil OM thickness is incorrect");
        }

        /// <summary>
        /// This test attempts to run a simulation with multiple models under the replacements
        /// node. One replacement overrides wheat's cultivar folder, giving the axe cultivar
        /// an invalid parameter, which should cause the simulation to bomb.
        /// </summary>
        [Test]
        public void TestMultipleModelReplacements()
        {
            string json = ReflectionUtilities.GetResourceAsString("UnitTests.Core.Run.MultipleReplacements.apsimx");
            Simulations sims = FileFormat.ReadFromString<Simulations>(json, e => throw e, false).NewModel as Simulations;

            Runner runner = new Runner(sims);
            List<Exception> errors = runner.Run();

            // The above should throw. The simulations contains a replacements node which
            // replaces wheat's cultivars folder, giving axe an invalid parameter. We sow
            // axe in this sim, so we should get an error when the plant is sown.
            Assert.That(errors, Is.Not.Null);
            Assert.That(errors.Count, Is.EqualTo(1));
        }
    }
}
