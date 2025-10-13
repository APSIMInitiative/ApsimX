﻿namespace UnitTests.Soils
{
    using APSIM.Core;
    using Models.Core;
    using Models.Soils;
    using Models.Soils.Nutrients;
    using Models.Soils.SoilTemp;
    using Models.WaterModel;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    [TestFixture]
    public class StandardiserTests
    {
        /// <summary>Ensure layer mapping works.</summary>
        [Test]
        public void LayerMappingWorks()
        {
            var soil = new Soil
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
                    new CERESSoilTemperature(),
                    new Organic
                    {
                        Thickness = new double[] { 100, 300 },
                        Carbon = new double[] { 2, 1 }
                    },
                    new Solute
                    {
                        Name = "NO3",
                        Thickness = new double[] { 100, 200 },
                        InitialValues = new double[] { 27, 10 },
                        InitialValuesUnits = Solute.UnitsEnum.kgha
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
                        InitialValues = new double[] { 0.261 },
                    }
                }
            };
            var tree = Node.Create(soil);

            soil.Sanitise();

            var physical = soil.Node.FindChild<Physical>();
            var soilOrganicMatter = soil.Node.FindChild<Organic>();
            var water = soil.Node.FindChild<Water>();

            // Make sure layer structures have been standardised.
            var targetThickness = new double[] { 100, 300, 300 };
            Assert.That(physical.Thickness, Is.EqualTo(targetThickness));
            Assert.That(soilOrganicMatter.Thickness, Is.EqualTo(targetThickness));
            Assert.That(water.Thickness, Is.EqualTo(targetThickness));
        }

        /// <summary>Ensure a LayerStructure is used for mapping.</summary>
        [Test]
        public void LayerStructureIsUsedForMapping()
        {
            var soil = new Soil
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
                                Name = "WheatSoil",
                                KL = new double[] { 0.06, 0.060, 0.060 },
                                LL = new double[] { 0.27, 0.267, 0.261 }
                            }
                        }
                    },
                    new Models.WaterModel.WaterBalance(),
                    new CERESSoilTemperature(),
                    new Organic
                    {
                        Thickness = new double[] { 100, 300 },
                        Carbon = new double[] { 2, 1 }
                    },
                    new Solute
                    {
                        Name = "NO3",
                        Thickness = new double[] { 100, 200 },
                        InitialValues = new double[] { 27, 6 },
                        InitialValuesUnits = Solute.UnitsEnum.kgha
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
                        InitialValues = new double[] { 0.261 }
                    },
                    new LayerStructure
                    {
                        Thickness = new double[] { 100, 300 }
                    }
                }
            };
            var tree = Node.Create(soil);

            soil.Sanitise();

            var physical = soil.Node.FindChild<Physical>();
            var soilOrganicMatter = soil.Node.FindChild<Organic>();
            var water = soil.Node.FindChild<Water>();

            // Make sure layer structures have been standardised.
            var targetThickness = new double[] { 100, 300 };
            Assert.That(physical.Thickness, Is.EqualTo(targetThickness));
            Assert.That(soilOrganicMatter.Thickness, Is.EqualTo(targetThickness));
            Assert.That(water.Thickness, Is.EqualTo(targetThickness));
        }

        /// <summary>Ensure a single initial conditions sample is created.</summary>
        [Test]
        public void InitialConditionsIsCreated()
        {
            Soil soil = CreateSimpleSoil();
            var tree = Node.Create(soil);

            soil.Sanitise();

            var chemical = soil.Node.FindChild<Chemical>();
            var organic = soil.Node.FindChild<Organic>();
            var water = soil.Node.FindChild<Water>();
            var solutes = soil.Node.FindChildren<Solute>().ToArray();

            Assert.That(soil.Node.FindChildren<Water>().Count(), Is.EqualTo(1));
            Assert.That(water.Name, Is.EqualTo("Water"));
            Assert.That(water.Volumetric, Is.EqualTo(new double[] { 0.1, 0.2 }));
            Assert.That(organic.Carbon, Is.EqualTo(new double[] { 2.0, 0.9 }));
            Assert.That(chemical.PH, Is.EqualTo(new double[] { 6.65, 7.0 }));
            Assert.That(chemical.EC, Is.EqualTo(new double[] { 150, 200 }));

            Assert.That(solutes[0].InitialValues, Is.EqualTo(new double[] { 21.5, 0.0 }));  // NO3 kg/ha
            Assert.That(solutes[1].InitialValues, Is.EqualTo(new double[] { 1.0, 0.0 })); // NH4 kg/ha
        }

        [Test]
        public void DontStandardiseDisabledSoils()
        {
            Soil soil = CreateSimpleSoil();
            Physical phys = soil.Node.FindChild<Physical>();

            // Remove a layer from BD - this will cause standardisation to fail.
            phys.BD = new double[phys.BD.Length - 1];

            // Now disable the soil so it doesn't get standardised.
            soil.Enabled = false;

            // Chuck the soil in a simulation.
            Simulations sims = Utilities.GetRunnableSim();
            Zone paddock = sims.Node.FindChild<Zone>(recurse: true);
            paddock.Node.AddChild(soil);
            soil.Parent = paddock;

            var tree = Node.Create(soil);

            // Run the simulation - this shouldn't fail, because the soil is disabled.
            var runner = new Models.Core.Run.Runner(tree.Model as IModel);
            List<Exception> errors = runner.Run();
            Assert.That(errors.Count, Is.EqualTo(0), "There should be no errors - the faulty soil is disabled");
        }

        [Test]
        public void EnsureSoilStandardiserAddsMissingNodes()
        {
            Soil soil = new()
            {
                Children =
                [
                    new Physical()
                    {
                        Thickness = [100, 200],
                        BD = [1.36, 1.216],
                        AirDry = [0.135, 0.214],
                        LL15 = [0.27, 0.267],
                        DUL = [0.365, 0.461],
                        SAT = [0.400, 0.481],
                    },
                ]
            };
            Node.Create(soil);
            SoilSanitise.InitialiseSoil(soil);

            var waterBalance = soil.Node.FindChild<WaterBalance>();
            var nutrient = soil.Node.FindChild<Nutrient>();
            Assert.That(soil.Node.FindChild<SoilTemperature>(), Is.Not.Null);
            Assert.That(soil.Node.FindChild<Solute>("NO3"), Is.Not.Null);
            Assert.That(soil.Node.FindChild<Solute>("NH4"), Is.Not.Null);
            Assert.That(soil.Node.FindChild<Solute>("Urea"), Is.Not.Null);
            Assert.That(soil.Node.FindChild<Water>(), Is.Not.Null);
            Assert.That(waterBalance, Is.Not.Null);
            Assert.That(soil.Node.FindChild<Organic>(), Is.Not.Null);
            Assert.That(soil.Node.FindChild<Chemical>(), Is.Not.Null);
            Assert.That(nutrient, Is.Not.Null);

            // Ensure that waterbalance and nutrient models have their child models from resource.
            Assert.That(waterBalance.Children.Count, Is.GreaterThan(0));
            Assert.That(nutrient.Children.Count, Is.GreaterThan(0));
        }

        /// <summary>
        /// When the user does "Add model" and adds an empty physical, organic, chemical, solute model
        /// make sure the soil sanitiser doesn't throw - https://github.com/APSIMInitiative/ApsimX/issues/10355
        /// </summary>
        [Test]
        public void EnsureSoilsWithoutThicknessDontThrowWhenStandardised()
        {
            Soil soil = CreateSimpleSoil();
            soil.Node.FindChild<Physical>().Thickness = null;
            soil.Node.FindChild<Organic>().Thickness = null;
            soil.Node.FindChild<Chemical>().Thickness = null;
            soil.Node.FindChild<Solute>().Thickness = null;

            Assert.DoesNotThrow(() => soil.Sanitise());
        }


        [Test]
        public void EnsureSoilInitialiserDoesntOverwriteExistingNodes()
        {
            Soil soil = new()
            {
                Children =
                [
                    new Physical()
                    {
                        Thickness = [100, 200],
                        ParticleSizeClay = [ 1, 2 ],
                        ParticleSizeSand = [ 3, 4 ],
                        ParticleSizeSilt = [ 5, 6 ],
                        BD = [1.36, 1.216],
                        AirDry = [0.135, 0.214],
                        LL15 = [0.27, 0.267],
                        DUL = [0.365, 0.461],
                        SAT = [0.400, 0.481],
                    },
                    new Organic()
                    {
                        Thickness = [100, 200],
                        Carbon = [100, 200]
                    },
                    new Chemical()
                    {
                        Thickness = [100, 200],
                        CEC = [10, 11]
                    },
                    new WaterBalance()
                    {
                        Thickness = [100, 200],
                        SWCON = [200, 200]
                    }
                ]
            };
            Node.Create(soil);
            SoilSanitise.InitialiseSoil(soil);

            Assert.That(soil.Node.FindChild<Physical>().ParticleSizeClay, Is.EqualTo([1, 2]));
            Assert.That(soil.Node.FindChild<Physical>().ParticleSizeSand, Is.EqualTo([3, 4]));
            Assert.That(soil.Node.FindChild<Physical>().ParticleSizeSilt, Is.EqualTo([5, 6]));
            Assert.That(soil.Node.FindChild<Organic>().Carbon, Is.EqualTo([100, 200]));
            Assert.That(soil.Node.FindChild<Chemical>().CEC, Is.EqualTo([10, 11]));
            Assert.That(soil.Node.FindChild<WaterBalance>().SWCON, Is.EqualTo([200, 200]));
        }

        private Soil CreateSimpleSoil()
        {
            var soil = new Soil
            {
                Children = new List<IModel>()
                {
                    new Physical()
                    {
                        Thickness = new double[] { 100, 200 },
                        BD = new double[] { 1.36, 1.216 },
                        AirDry = new double[] { 0.135, 0.214 },
                        LL15 = new double[] { 0.27, 0.267 },
                        DUL = new double[] { 0.365, 0.461 },
                        SAT = new double[] { 0.400, 0.481 },
                    },
                    new Models.WaterModel.WaterBalance(),
                    new CERESSoilTemperature(),
                    new Organic
                    {
                        Thickness = new double[] { 100, 200 },
                        Carbon = new double[] { 2, 0.9 },
                        FBiom = new double[] { 1, 2 }
                    },
                    new Chemical
                    {
                        Thickness = new double[] { 50, 50 },
                        PH = new double[] { 6.4, 6.9 },
                        EC = new double[] { 100, 200 }
                    },
                    new Solute
                    {
                        Name = "NO3",
                        Thickness = new double[] { 50, 50 },
                        InitialValues = new double[] { 27, 16 },
                        InitialValuesUnits = Solute.UnitsEnum.ppm
                    },
                    new Solute
                    {
                        Name = "NH4",
                        Thickness = new double[] { 50, 50 },
                        InitialValues = new double[] { 2, double.NaN },
                        InitialValuesUnits = Solute.UnitsEnum.ppm
                    },
                    new Water
                    {
                        Thickness = new double[] { 100, 200 },
                        InitialValues = new double[] { 0.1, 0.2 },
                    }
                }
            };
            Node.Create(soil);
            return soil;
        }

        [Test]
        public void EnsureBadKS()
        {
            Soil soil = new()
            {
                Children =
                [
                    new Physical()
                    {
                        Thickness = [100, 200],
                        ParticleSizeClay = [ 1, 2 ],
                        ParticleSizeSand = [ 3, 4 ],
                        ParticleSizeSilt = [ 5, 6 ],
                        BD = [1.36, 1.216],
                        AirDry = [0.135, 0.214],
                        LL15 = [0.27, 0.267],
                        DUL = [0.365, 0.461],
                        SAT = [0.400, 0.481],
                        KS = [0, 0]
                    }
                ]
            };
            Node.Create(soil);

            Exception exception = null;
            try
            {
                soil.Check(new MockSummary());
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            Assert.That(exception.Message, Does.Contain("KS in layer 1 must be > 0"));
        }

        [Test]
        public void EnsureZeroKSInBottomLayerOk()
        {
            Soil soil = new()
            {
                Children =
                [
                    new Physical()
                    {
                        Thickness = [100, 200],
                        ParticleSizeClay = [ 1, 2 ],
                        ParticleSizeSand = [ 3, 4 ],
                        ParticleSizeSilt = [ 5, 6 ],
                        BD = [1.36, 1.216],
                        AirDry = [0.135, 0.214],
                        LL15 = [0.27, 0.267],
                        DUL = [0.365, 0.461],
                        SAT = [0.400, 0.481],
                        KS = [10, 0]
                    }
                ]
            };
            Node.Create(soil);

            Exception exception = null;
            try
            {
                soil.Check(new MockSummary());
            }
            catch (Exception ex)
            {
                exception = ex;
            }
            Assert.That(exception.Message, Does.Not.Contain("KS in layer"));
        }
    }
}
