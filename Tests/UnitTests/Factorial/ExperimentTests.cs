namespace UnitTests.Factorial
{
    using Models.Core;
    using Models.Factorial;
    using Models.Core.Run;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using UnitTests.Weather;

    /// <summary>This is a test class for the Experiment class</summary>
    [TestFixture]
    public class ExperimentTests
    {
        /// <summary>Ensure a property set overrides work.</summary>
        [Test]
        public void EnsurePropertySetsWork()
        {
            var experiment = new Experiment()
            {
                Name = "Exp1",
                Children = new List<Model>()
                {
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
                    },
                    new Factors()
                    {
                        Children = new List<Model>()
                        {
                            new Factor()
                            {
                                Name = "MaxT",
                                Specification = "[Weather].MaxT = 10, 20"
                            },
                            new Factor()
                            {
                                Name = "StartDate",
                                Specification = "[Weather].StartDate = 2003-11-01, 2003-12-01"
                            }
                        }
                    }
                }
            };
            Apsim.ParentAllChildren(experiment);

            var sims = experiment.GenerateSimulationDescriptions();
            Assert.AreEqual(sims[0].Descriptors.Find(d => d.Name == "Experiment").Value, "Exp1");
            Assert.AreEqual(sims[0].Descriptors.Find(d => d.Name == "MaxT").Value, "10");
            Assert.AreEqual(sims[0].Descriptors.Find(d => d.Name == "StartDate").Value, "2003-11-01");
            var weather = sims[0].ToSimulation().Children[0] as MockWeather;
            Assert.AreEqual(weather.MaxT, 10);
            Assert.AreEqual(weather.StartDate, new DateTime(2003, 11, 1));

            Assert.AreEqual(sims[1].Descriptors.Find(d => d.Name == "Experiment").Value, "Exp1");
            Assert.AreEqual(sims[1].Descriptors.Find(d => d.Name == "MaxT").Value, "20");
            Assert.AreEqual(sims[1].Descriptors.Find(d => d.Name == "StartDate").Value, "2003-11-01");
            weather = sims[1].ToSimulation().Children[0] as MockWeather;
            Assert.AreEqual(weather.MaxT, 20);
            Assert.AreEqual(weather.StartDate, new DateTime(2003, 11, 1));

            Assert.AreEqual(sims[2].Descriptors.Find(d => d.Name == "Experiment").Value, "Exp1");
            Assert.AreEqual(sims[2].Descriptors.Find(d => d.Name == "MaxT").Value, "10");
            Assert.AreEqual(sims[2].Descriptors.Find(d => d.Name == "StartDate").Value, "2003-12-01");
            weather = sims[2].ToSimulation().Children[0] as MockWeather;
            Assert.AreEqual(weather.MaxT, 10);
            Assert.AreEqual(weather.StartDate, new DateTime(2003, 12, 1));

            Assert.AreEqual(sims[3].Descriptors.Find(d => d.Name == "Experiment").Value, "Exp1");
            Assert.AreEqual(sims[3].Descriptors.Find(d => d.Name == "MaxT").Value, "20");
            Assert.AreEqual(sims[3].Descriptors.Find(d => d.Name == "StartDate").Value, "2003-12-01");
            weather = sims[3].ToSimulation().Children[0] as MockWeather;
            Assert.AreEqual(weather.MaxT, 20);
            Assert.AreEqual(weather.StartDate, new DateTime(2003, 12, 1));

            Assert.AreEqual(sims.Count, 4);
        }

        /// <summary>Ensure a property range override works.</summary>
        [Test]
        public void EnsurePropertyRangeWork()
        {
            var experiment = new Experiment()
            {
                Name = "Exp1",
                Children = new List<Model>()
                {
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
                    },
                    new Factors()
                    {
                        Children = new List<Model>()
                        {
                            new Factor()
                            {
                                Name = "MaxT",
                                Specification = "[Weather].MaxT = 10 to 20 step 5"
                            }
                        }
                    }
                }
            };
            Apsim.ParentAllChildren(experiment);

            var sims = experiment.GenerateSimulationDescriptions();
            Assert.AreEqual(sims[0].Descriptors.Find(d => d.Name == "Experiment").Value, "Exp1");
            Assert.AreEqual(sims[0].Descriptors.Find(d => d.Name == "MaxT").Value, "10");
            var weather = sims[0].ToSimulation().Children[0] as MockWeather;
            Assert.AreEqual(weather.MaxT, 10);

            Assert.AreEqual(sims[1].Descriptors.Find(d => d.Name == "Experiment").Value, "Exp1");
            Assert.AreEqual(sims[1].Descriptors.Find(d => d.Name == "MaxT").Value, "15");
            weather = sims[1].ToSimulation().Children[0] as MockWeather;
            Assert.AreEqual(weather.MaxT, 15);

            Assert.AreEqual(sims[2].Descriptors.Find(d => d.Name == "Experiment").Value, "Exp1");
            Assert.AreEqual(sims[2].Descriptors.Find(d => d.Name == "MaxT").Value, "20");
            weather = sims[2].ToSimulation().Children[0] as MockWeather;
            Assert.AreEqual(weather.MaxT, 20);

            Assert.AreEqual(sims.Count, 3);
        }

        /// <summary>Ensure model overrides work.</summary>
        [Test]
        public void EnsureModelOverrideWorks()
        {
            var experiment = new Experiment()
            {
                Name = "Exp1",
                Children = new List<Model>()
                {
                    new Simulation()
                    {
                        Name = "BaseSimulation",
                        Children = new List<Model>()
                        {
                            new MockWeather()
                            {
                                Name = "Weather",
                                MaxT = 1,
                                MinT = 0
                            },
                        }
                    },
                    new Factors()
                    {
                        Children = new List<Model>()
                        {
                            new Factor()
                            {
                                Name = "Factor",
                                Specification = "[Weather]",
                                Children = new List<Model>()
                                {
                                    new MockWeather()
                                    {
                                        Name = "Weather1",
                                        MaxT = 10,
                                        MinT = 10.2
                                    },
                                    new MockWeather()
                                    {
                                        Name = "Weather2",
                                        MaxT = 20,
                                        MinT = 10.4
                                    }
                                }
                            },
                        }
                    }
                }
            };
            Apsim.ParentAllChildren(experiment);

            var sims = experiment.GenerateSimulationDescriptions();
            Assert.AreEqual(sims[0].Descriptors.Find(d => d.Name == "Experiment").Value, "Exp1");
            Assert.AreEqual(sims[0].Descriptors.Find(d => d.Name == "Factor").Value, "Weather1");
            var weather = sims[0].ToSimulation().Children[0] as MockWeather;
            Assert.AreEqual(weather.MaxT, 10);
            Assert.AreEqual(weather.MinT, 10.2);

            Assert.AreEqual(sims[1].Descriptors.Find(d => d.Name == "Experiment").Value, "Exp1");
            Assert.AreEqual(sims[1].Descriptors.Find(d => d.Name == "Factor").Value, "Weather2");
            weather = sims[1].ToSimulation().Children[0] as MockWeather;
            Assert.AreEqual(weather.MaxT, 20);
            Assert.AreEqual(weather.MinT, 10.4);

            Assert.AreEqual(sims.Count, 2);
        }

        /// <summary>Ensure compound factors work.</summary>
        [Test]
        public void EnsureCompoundWorks()
        {
            var experiment = new Experiment()
            {
                Name = "Exp1",
                Children = new List<Model>()
                {
                    new Simulation()
                    {
                        Name = "BaseSimulation",
                        Children = new List<Model>()
                        {
                            new MockWeather()
                            {
                                Name = "Weather",
                                MaxT = 1,
                                MinT = 0
                            },
                            new MockClock()
                            {
                                Name = "Clock",
                                NumberOfTicks = 1,
                                Today = DateTime.MinValue 
                            },
                            new MockSummary()
                        }
                    },
                    new Factors()
                    {
                        Children = new List<Model>()
                        {
                            new Factor()
                            {
                                Name = "Factor",
                                Children = new List<Model>()
                                {
                                    new CompositeFactor()
                                    {
                                        Name = "1",
                                        Specifications = new List<string>() { "[Weather].MaxT = 10",
                                                                              "[Weather].MinT = 20",
                                                                              "[Clock].NumberOfTicks = 10"},
                                    },
                                    new CompositeFactor()
                                    {
                                        Name = "2",
                                        Specifications = new List<string>() { "[Weather].MaxT = 100",
                                                                              "[Weather].MinT = 200",
                                                                              "[Clock].NumberOfTicks = 100"},
                                    }
                                }
                            }
                        }
                    }
                }
            };
            Apsim.ParentAllChildren(experiment);

            var sims = experiment.GenerateSimulationDescriptions();
            Assert.AreEqual(sims[0].Name, "Exp1Factor1");
            Assert.AreEqual(sims[0].Descriptors.Find(d => d.Name == "Experiment").Value, "Exp1");
            Assert.AreEqual(sims[0].Descriptors.Find(d => d.Name == "Factor").Value, "1");
            var sim = sims[0].ToSimulation();
            var weather = sim.Children[0] as MockWeather;
            var clock = sim.Children[1] as MockClock;
            Assert.AreEqual(weather.MaxT, 10);
            Assert.AreEqual(weather.MinT, 20);
            Assert.AreEqual(clock.NumberOfTicks, 10);

            Assert.AreEqual(sims[1].Name, "Exp1Factor2");
            Assert.AreEqual(sims[1].Descriptors.Find(d => d.Name == "Experiment").Value, "Exp1");
            Assert.AreEqual(sims[1].Descriptors.Find(d => d.Name == "Factor").Value, "2");
            sim = sims[1].ToSimulation();
            weather = sim.Children[0] as MockWeather;
            clock = sim.Children[1] as MockClock;
            Assert.AreEqual(weather.MaxT, 100);
            Assert.AreEqual(weather.MinT, 200);
            Assert.AreEqual(clock.NumberOfTicks, 100);

            Assert.AreEqual(sims.Count, 2);
        }

        /// <summary>Ensure compound that has a model override works.</summary>
        [Test]
        public void EnsureCompoundWithModelOverrideWorks()
        {
            var experiment = new Experiment()
            {
                Name = "Exp1",
                Children = new List<Model>()
                {
                    new Simulation()
                    {
                        Name = "BaseSimulation",
                        Children = new List<Model>()
                        {
                            new MockWeather()
                            {
                                Name = "Weather",
                                MaxT = 1,
                                MinT = 0
                            },
                            new MockClock()
                            {
                                Name = "Clock",
                                NumberOfTicks = 1,
                                Today = DateTime.MinValue
                            },
                            new MockSummary()
                        }
                    },
                    new Factors()
                    {
                        Children = new List<Model>()
                        {
                            new Factor()
                            {
                                Name = "Site",
                                Children = new List<Model>()
                                {
                                    new CompositeFactor()
                                    {
                                        Name = "Goondiwindi",
                                        Specifications = new List<string>() { "[Weather].MaxT = 10",
                                                                              "[Weather].MinT = 20",
                                                                              "[Clock]"},
                                        Children = new List<Model>()
                                        {
                                            new MockClock()
                                            {
                                                Name = "Clock",
                                                NumberOfTicks = 10,
                                                Today = DateTime.MinValue
                                            }
                                        }
                                    },
                                    new CompositeFactor()
                                    {
                                        Name = "Toowoomba",
                                        Specifications = new List<string>() { "[Weather].MaxT = 100",
                                                                              "[Weather].MinT = 200",
                                                                              "[Clock]"},
                                        Children = new List<Model>()
                                        {
                                            new MockClock()
                                            {
                                                Name = "Clock",
                                                NumberOfTicks = 100,
                                                Today = DateTime.MinValue
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };
            Apsim.ParentAllChildren(experiment);

            var sims = experiment.GenerateSimulationDescriptions();
            Assert.AreEqual(sims[0].Name, "Exp1SiteGoondiwindi");
            Assert.AreEqual(sims[0].Descriptors.Find(d => d.Name == "Experiment").Value, "Exp1");
            Assert.AreEqual(sims[0].Descriptors.Find(d => d.Name == "Site").Value, "Goondiwindi");
            var sim = sims[0].ToSimulation();
            var weather = sim.Children[0] as MockWeather;
            var clock = sim.Children[1] as MockClock;
            Assert.AreEqual(weather.MaxT, 10);
            Assert.AreEqual(weather.MinT, 20);
            Assert.AreEqual(clock.NumberOfTicks, 10);

            Assert.AreEqual(sims[1].Name, "Exp1SiteToowoomba");
            Assert.AreEqual(sims[1].Descriptors.Find(d => d.Name == "Experiment").Value, "Exp1");
            Assert.AreEqual(sims[1].Descriptors.Find(d => d.Name == "Site").Value, "Toowoomba");
            sim = sims[1].ToSimulation();
            weather = sim.Children[0] as MockWeather;
            clock = sim.Children[1] as MockClock;
            Assert.AreEqual(weather.MaxT, 100);
            Assert.AreEqual(weather.MinT, 200);
            Assert.AreEqual(clock.NumberOfTicks, 100);

            Assert.AreEqual(sims.Count, 2);
        }


        /// <summary>Ensure compound that has a model override works.</summary>
        [Test]
        public void EnsureFactorWithTwoChildModelsOfSameTypeWorks()
        {
            var experiment = new Experiment()
            {
                Name = "Exp1",
                Children = new List<Model>()
                {
                    new Simulation()
                    {
                        Name = "BaseSimulation",
                        Children = new List<Model>()
                        {
                            new Models.Operations()
                            {
                                Name = "Sowing",
                                Operation = new List<Models.Operation>()
                                {
                                    new Models.Operation()
                                    {
                                        Action = "Sowing"
                                    }
                                }
                            },
                            new Models.Operations()
                            {
                                Name = "Cutting",
                                Operation = new List<Models.Operation>()
                                {
                                    new Models.Operation()
                                    {
                                        Action =   "Cutting"
                                    }
                                }

                            },
                        }
                    },
                    new Factors()
                    {
                        Children = new List<Model>()
                        {
                            new Factor()
                            {
                                Name = "Site",
                                Children = new List<Model>()
                                {
                                    new CompositeFactor()
                                    {
                                        Name = "1",
                                        Specifications = new List<string>() { "[Sowing]",
                                                                              "[Cutting]"},
                                        Children = new List<Model>()
                                        {
                                            new Models.Operations()
                                            {
                                                Name = "Sowing",
                                                Operation = new List<Models.Operation>()
                                                {
                                                    new Models.Operation()
                                                    {
                                                        Action = "Sowing1"
                                                    }
                                                }
                                            },
                                            new Models.Operations()
                                            {
                                                Name = "Cutting",
                                                Operation = new List<Models.Operation>()
                                                {
                                                    new Models.Operation()
                                                    {
                                                        Action =   "Cutting1"
                                                    }
                                                }

                                            },
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };
            Apsim.ParentAllChildren(experiment);

            var sims = experiment.GenerateSimulationDescriptions();
            Assert.AreEqual(sims[0].Name, "Exp1Site1");
            var sim = sims[0].ToSimulation();
            var sowing = sim.Children[0] as Models.Operations;
            var cutting = sim.Children[1] as Models.Operations;
            Assert.AreEqual(sowing.Operation[0].Action, "Sowing1");
            Assert.AreEqual(cutting.Operation[0].Action, "Cutting1");
        }

        /// <summary>Ensure disabled simulations aren't run.</summary>
        [Test]
        public void EnsureDisabledSimulationsArentRun()
        {
            var experiment = new Experiment()
            {
                Name = "Exp1",
                Children = new List<Model>()
                {
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
                    },
                    new Factors()
                    {
                        Children = new List<Model>()
                        {
                            new Factor()
                            {
                                Name = "MaxT",
                                Specification = "[Weather].MaxT = 10, 20"
                            },
                            new Factor()
                            {
                                Name = "StartDate",
                                Specification = "[Weather].StartDate = 2003-11-01, 2003-12-01"
                            }
                        }
                    }
                }
            };
            Apsim.ParentAllChildren(experiment);

            experiment.DisabledSimNames = new List<string>() { "Exp1MaxT10StartDate2003-11-01", "Exp1MaxT20StartDate2003-11-01" };

            var sims = experiment.GenerateSimulationDescriptions();
            Assert.AreEqual(sims[0].Descriptors.Find(d => d.Name == "Experiment").Value, "Exp1");
            Assert.AreEqual(sims[0].Descriptors.Find(d => d.Name == "MaxT").Value, "10");
            Assert.AreEqual(sims[0].Descriptors.Find(d => d.Name == "StartDate").Value, "2003-12-01");
            var weather = sims[0].ToSimulation().Children[0] as MockWeather;
            Assert.AreEqual(weather.MaxT, 10);
            Assert.AreEqual(weather.StartDate, new DateTime(2003, 12, 1));

            Assert.AreEqual(sims[1].Descriptors.Find(d => d.Name == "Experiment").Value, "Exp1");
            Assert.AreEqual(sims[1].Descriptors.Find(d => d.Name == "MaxT").Value, "20");
            Assert.AreEqual(sims[1].Descriptors.Find(d => d.Name == "StartDate").Value, "2003-12-01");
            weather = sims[1].ToSimulation().Children[0] as MockWeather;
            Assert.AreEqual(weather.MaxT, 20);
            Assert.AreEqual(weather.StartDate, new DateTime(2003, 12, 1));

            Assert.AreEqual(sims.Count, 2);
        }

    }
}
