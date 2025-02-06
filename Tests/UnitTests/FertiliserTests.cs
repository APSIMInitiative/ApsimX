using APSIM.Shared.Utilities;
using Models;
using Models.Core;
using Models.Soils;
using NUnit.Framework;
using System;


namespace UnitTests
{
    [TestFixture]
    class FertiliserTests
    {
        class MockSoilSolute : Model, ISolute
        {
            public MockSoilSolute(string name = "NO3")
            {
                Name = name;
            }
            public double[] kgha { get; set; }

            public double[] ppm => throw new NotImplementedException();
            public double AmountLostInRunoff { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public double DepthConstant { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public double MaxDepthSoluteAccessible { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public double RunoffEffectivenessAtMovingSolute { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public double MaxEffectiveRunoff { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public double[] AmountInSolution { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public double[] ConcAdsorpSolute { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            double[] ISolute.AmountLostInRunoff { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public void SetKgHa(SoluteSetterType callingModelType, double[] value)
            {
                kgha = value;
            }

            public void AddKgHaDelta(SoluteSetterType callingModelType, double[] delta)
            {
                kgha = MathUtilities.Add(kgha, delta);
            }
        }


        /// <summary>Ensure the the apply method works with non zero depth.</summary>
        [Test]
        public void SimpleFertiliseApply()
        {
            // Create a simulation
            var simulation = new Simulation()
            {
                Children =
                [
                    new MockSummary(),
                    new MockSoilSolute("NO3") { kgha = [1, 2, 3]},
                    new Physical() { Thickness = [100, 100, 100 ]},
                    new Fertiliser()
                    {
                        Children = [
                            new FertiliserType()
                            {
                                Name = "NO3N",
                                Solute1Name = "NO3",
                                Solute1Fraction = 1,
                                Children = [
                                    new Models.Functions.Constant()
                                    {
                                        Name = "Release",
                                        FixedValue = 1
                                    }
                                ]
                            }
                        ]
                    }
                ]
            };
            // set up the simulation and all models.
            simulation.ParentAllDescendants();
            var links = new Links();
            links.Resolve(simulation, true);

            // get instances.
            var summary = simulation.FindDescendant<MockSummary>();
            var fertiliser = simulation.FindDescendant<Fertiliser>();
            var no3 = simulation.FindDescendant<ISolute>();

            // apply fertiliser
            fertiliser.Apply(amount:100, "NO3N", depth: 200);

            // Run day 1 and check solute levels. The fertiliser should have been put into top layer.
            Utilities.CallMethod(fertiliser, "OnDoFertiliserApplications");

            Assert.That(no3.kgha, Is.EqualTo(new double[] { 1, 102, 3 }));
            Assert.That(summary.messages[0], Is.EqualTo("100.0 kg/ha of NO3N added at depth 200 layer 2"));
        }

        /// <summary>Ensure the the apply method works over a depth range.</summary>
        [Test]
        public void FertiliseApplyOverDepthRange()
        {
            // Create a simulation
            var simulation = new Simulation()
            {
                Children =
                [
                    new MockSummary(),
                    new MockSoilSolute("NO3") { kgha = [1, 2, 3, 4]},
                    new Physical() { Thickness = [100, 100, 200, 200 ]},
                    new Fertiliser()
                    {
                        Children = [
                            new FertiliserType()
                            {
                                Name = "NO3N",
                                Solute1Name = "NO3",
                                Solute1Fraction = 1,
                                Children = [
                                    new Models.Functions.Constant()
                                    {
                                        Name = "Release",
                                        FixedValue = 1
                                    }
                                ]
                            }
                        ]
                    }
                ]
            };

            // set up the simulation and all models.
            simulation.ParentAllDescendants();
            var links = new Links();
            links.Resolve(simulation, true);

            // get instances.
            var fertiliser = simulation.FindDescendant<Fertiliser>();
            var no3 = simulation.FindDescendant<ISolute>();

            // apply fertiliser
            fertiliser.Apply(amount:50, "NO3N", depth: 75, depthBottom: 300);

            // Run day 1 and check solute levels. The fertiliser should have been put into top 3 layers.
            Utilities.CallMethod(fertiliser, "OnDoFertiliserApplications");

            Assert.That(no3.kgha[0], Is.EqualTo(6.56).Within(0.01));
            Assert.That(no3.kgha[1], Is.EqualTo(24.2).Within(0.1));
            Assert.That(no3.kgha[2], Is.EqualTo(25.2).Within(0.1));
        }

        // <summary>Ensure the application of a slow release fertiliser works over a number of days.</summary>
        [Test]
        public void FertiliseApplySlowRelease()
        {
            // Create a simulation
            var simulation = new Simulation()
            {
                Children =
                [
                    new MockSummary(),
                    new MockSoilSolute("NO3") { kgha = [0, 0, 0, 0]},
                    new MockSoilSolute("NH4") { kgha = [0, 0, 0, 0]},
                    new Physical() { Thickness = [100, 100, 200, 200 ]},
                    new Fertiliser()
                    {
                        Children = [
                            new FertiliserType()
                            {
                                Name = "SlowRelease",
                                Solute1Name = "NO3",
                                Solute1Fraction = 0.6,
                                Solute2Name = "NH4",
                                Solute2Fraction = 0.4,
                                FractionWhenRemainderReleased = 0.8,
                                Children = [
                                    new Models.Functions.Constant()
                                    {
                                        Name = "Release",
                                        FixedValue = 0.5
                                    }
                                ]
                            }
                        ]
                    }
                ]
            };

            // set up the simulation and all models.
            simulation.ParentAllDescendants();
            var links = new Links();
            links.Resolve(simulation, true);

            // get instances.
            var fertiliser = simulation.FindDescendant<Fertiliser>();
            var no3 = simulation.FindDescendant<ISolute>("NO3");
            var nh4 = simulation.FindDescendant<ISolute>("NH4");

            // apply fertiliser
            fertiliser.Apply(amount:10, "SlowRelease", depth: 0);

            // Run day 1 and check solute levels. Half the fertiliser should have been put into top layer.
            Utilities.CallMethod(fertiliser, "OnDoDailyInitialisation");
            Utilities.CallMethod(fertiliser, "OnDoFertiliserApplications");
            Assert.That(fertiliser.NitrogenApplied, Is.EqualTo(5));
            Assert.That(no3.kgha[0], Is.EqualTo(3).Within(0.1));    // 3 kg/ha added here.
            Assert.That(no3.kgha[1], Is.EqualTo(0).Within(0.1));
            Assert.That(no3.kgha[2], Is.EqualTo(0).Within(0.1));
            Assert.That(no3.kgha[3], Is.EqualTo(0).Within(0.1));
            Assert.That(nh4.kgha[0], Is.EqualTo(2).Within(0.1));    // 2 kg/ha added here.
            Assert.That(nh4.kgha[1], Is.EqualTo(0).Within(0.1));
            Assert.That(nh4.kgha[2], Is.EqualTo(0).Within(0.1));
            Assert.That(nh4.kgha[3], Is.EqualTo(0).Within(0.1));

            // Run day 2 and check solute levels. Half of the remaining 5 kg/ha should have been put into top layer.
            Utilities.CallMethod(fertiliser, "OnDoDailyInitialisation");
            Utilities.CallMethod(fertiliser, "OnDoFertiliserApplications");
            Assert.That(fertiliser.NitrogenApplied, Is.EqualTo(2.5));
            Assert.That(no3.kgha[0], Is.EqualTo(4.5).Within(0.1));  // 1.5 kg/ha added here
            Assert.That(no3.kgha[1], Is.EqualTo(0).Within(0.1));
            Assert.That(no3.kgha[2], Is.EqualTo(0).Within(0.1));
            Assert.That(no3.kgha[3], Is.EqualTo(0).Within(0.1));
            Assert.That(nh4.kgha[0], Is.EqualTo(3).Within(0.1));    // 1 kg/ha added here
            Assert.That(nh4.kgha[1], Is.EqualTo(0).Within(0.1));
            Assert.That(nh4.kgha[2], Is.EqualTo(0).Within(0.1));
            Assert.That(nh4.kgha[3], Is.EqualTo(0).Within(0.1));

            // Run day 3 and check solute levels. Remaining fertiliser (2.5kg/ha) should have been applied
            // because the amount after another release would have seen the remaining amount fall below minimum of 2 kg/ha
            Utilities.CallMethod(fertiliser, "OnDoDailyInitialisation");
            Utilities.CallMethod(fertiliser, "OnDoFertiliserApplications");
            Assert.That(fertiliser.NitrogenApplied, Is.EqualTo(2.5));
            Assert.That(no3.kgha[0], Is.EqualTo(6).Within(0.1));    // 1.5 kg/ha added here
            Assert.That(no3.kgha[1], Is.EqualTo(0).Within(0.1));
            Assert.That(no3.kgha[2], Is.EqualTo(0).Within(0.1));
            Assert.That(no3.kgha[3], Is.EqualTo(0).Within(0.1));
            Assert.That(nh4.kgha[0], Is.EqualTo(4).Within(0.1));    // 1 kg/ha added here
            Assert.That(nh4.kgha[1], Is.EqualTo(0).Within(0.1));
            Assert.That(nh4.kgha[2], Is.EqualTo(0).Within(0.1));
            Assert.That(nh4.kgha[3], Is.EqualTo(0).Within(0.1));

            // Run day 4 and check solute levels. No fertiliser left to apply.
            Utilities.CallMethod(fertiliser, "OnDoDailyInitialisation");
            Utilities.CallMethod(fertiliser, "OnDoFertiliserApplications");
            Assert.That(fertiliser.NitrogenApplied, Is.EqualTo(0));
            Assert.That(no3.kgha[0], Is.EqualTo(6).Within(0.1));
            Assert.That(no3.kgha[1], Is.EqualTo(0).Within(0.1));
            Assert.That(no3.kgha[2], Is.EqualTo(0).Within(0.1));
            Assert.That(no3.kgha[3], Is.EqualTo(0).Within(0.1));
            Assert.That(nh4.kgha[0], Is.EqualTo(4).Within(0.1));
            Assert.That(nh4.kgha[1], Is.EqualTo(0).Within(0.1));
            Assert.That(nh4.kgha[2], Is.EqualTo(0).Within(0.1));
            Assert.That(nh4.kgha[3], Is.EqualTo(0).Within(0.1));
        }

        // <summary>Ensure two applications of slow release fertiliser works over a number of days.</summary>
        [Test]
        public void Fertilise2ApplicationsSlowRelease()
        {
            // Create a simulation
            var simulation = new Simulation()
            {
                Children =
                [
                    new MockSummary(),
                    new MockSoilSolute("NO3") { kgha = [0, 0]},
                    new Physical() { Thickness = [100, 100 ]},
                    new Fertiliser()
                    {
                        Children = [
                            new FertiliserType()
                            {
                                Name = "SlowRelease",
                                Solute1Name = "NO3",
                                Solute1Fraction = 1.0,
                                FractionWhenRemainderReleased = 0.8,
                                Children = [
                                    new Models.Functions.Constant()
                                    {
                                        Name = "Release",
                                        FixedValue = 0.5
                                    }
                                ]
                            }
                        ]
                    }
                ]
            };

            // set up the simulation and all models.
            simulation.ParentAllDescendants();
            var links = new Links();
            links.Resolve(simulation, true);

            // get instances.
            var fertiliser = simulation.FindDescendant<Fertiliser>();
            var no3 = simulation.FindDescendant<ISolute>("NO3");

            // apply fertiliser
            fertiliser.Apply(amount:10, "SlowRelease", depth: 0);

            // Run day 1 and check solute levels. Half the fertiliser should have been put into top layer.
            Utilities.CallMethod(fertiliser, "OnDoDailyInitialisation");
            Utilities.CallMethod(fertiliser, "OnDoFertiliserApplications");
            Assert.That(fertiliser.NitrogenApplied, Is.EqualTo(5));
            Assert.That(no3.kgha[0], Is.EqualTo(5).Within(0.1));        // 5 kg/ha added here from pool 1
            Assert.That(no3.kgha[1], Is.EqualTo(0).Within(0.1));

            // apply more fertiliser
            fertiliser.Apply(amount:10, "SlowRelease", depth: 0);

            // Run day 2 and check solute levels.
            Utilities.CallMethod(fertiliser, "OnDoDailyInitialisation");
            Utilities.CallMethod(fertiliser, "OnDoFertiliserApplications");
            Assert.That(fertiliser.NitrogenApplied, Is.EqualTo(7.5));
            Assert.That(no3.kgha[0], Is.EqualTo(12.5).Within(0.1));      // 2.5 kg/ha added (pool1) + 5 kg/ha (pool2)
            Assert.That(no3.kgha[1], Is.EqualTo(0).Within(0.1));

            // Run day 3 and check solute levels.
            Utilities.CallMethod(fertiliser, "OnDoDailyInitialisation");
            Utilities.CallMethod(fertiliser, "OnDoFertiliserApplications");
            Assert.That(fertiliser.NitrogenApplied, Is.EqualTo(5.0));
            Assert.That(no3.kgha[0], Is.EqualTo(17.5).Within(0.1));    // 2.5 kg/ha added (pool1) + 2.5 kg/ha (pool2)
            Assert.That(no3.kgha[1], Is.EqualTo(0).Within(0.1));

            // Run day 4 and check solute levels.
            Utilities.CallMethod(fertiliser, "OnDoDailyInitialisation");
            Utilities.CallMethod(fertiliser, "OnDoFertiliserApplications");
            Assert.That(fertiliser.NitrogenApplied, Is.EqualTo(2.5));
            Assert.That(no3.kgha[0], Is.EqualTo(20).Within(0.1));       // 0 kg/ha added (pool1) + 2.5 kg/ha (pool2)
            Assert.That(no3.kgha[1], Is.EqualTo(0).Within(0.1));

            // Run day 5 and check solute levels.
            Utilities.CallMethod(fertiliser, "OnDoDailyInitialisation");
            Utilities.CallMethod(fertiliser, "OnDoFertiliserApplications");
            Assert.That(fertiliser.NitrogenApplied, Is.EqualTo(0));
            Assert.That(no3.kgha[0], Is.EqualTo(20).Within(0.1));       // 0 kg/ha added (pool1) + 0 kg/ha (pool2)
            Assert.That(no3.kgha[1], Is.EqualTo(0).Within(0.1));
        }
    }
}