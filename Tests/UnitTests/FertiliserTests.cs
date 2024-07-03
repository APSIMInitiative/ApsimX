namespace UnitTests
{
    using APSIM.Shared.Utilities;
    using Models;
    using Models.Core;
    using Models.Core.ApsimFile;
    using Models.Interfaces;
    using Models.Soils;
    using Models.Soils.Nutrients;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;

    [TestFixture]
    class FertiliserTests
    {
        /// <summary>Test setup routine. Returns a soil properties that can be used for testing.</summary>
        [Serializable]
        public class MockSoil : Model
        {
            public double[] NO3 { get; set; }
        }

        class MockSoilSolute : Model, ISolute
        {
            public MockSoilSolute(string name = "NO3")
            {
                Name = name;
            }
            public double[] kgha
            {
                get
                {
                    return (Parent as MockSoil).NO3;
                }
                set
                {
                    (Parent as MockSoil).NO3 = value;
                }
            }

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
        public void Fertiliser_EnsureApplyWorks()
        {
            // Create a tree with a root node for our models.
            var simulation = new Simulation()
            {
                Children = new List<IModel>()
                {
                    new Clock()
                    {
                        StartDate = new DateTime(2015, 1, 1),
                        EndDate = new DateTime(2015, 1, 1)
                    },
                    new MockSummary(),
                    new MockSoil()
                    {
                        NO3 = new double[] { 1, 2, 3 },
                        Children = new List<IModel>()
                        {
                            new MockSoilSolute("NO3"),
                            new MockSoilSolute("NH4"),
                            new MockSoilSolute("Urea"),
                            new Physical() { Thickness = new double[] { 100, 100, 100 }}
                        }
                    },
                    new Fertiliser() { Name = "Fertilise", ResourceName = "Fertiliser" },
                    new Operations()
                    {
                        Operation = new List<Operation>()
                        {
                            new Operation()
                            {
                                Date = "1-jan",
                                Action = "[Fertilise].Apply(Amount: 100, Type:Fertiliser.Types.NO3N, Depth:300)"
                            }
                        }
                    }
                }
            };
            Resource.Instance.Replace(simulation);
            FileFormat.InitialiseModel(simulation, (e) => throw e);
            simulation.Prepare();
            simulation.Run();

            var soil = simulation.Children[2] as MockSoil;
            var summary = simulation.FindDescendant<MockSummary>();
            Assert.That(soil.NO3, Is.EqualTo(new double[] { 1, 2, 103 }));
            Assert.That(summary.messages[0], Is.EqualTo("100.0 kg/ha of NO3N added at depth 300 layer 3"));
        }

        /// <summary>Ensure the the apply method works over a depth range.</summary>
        [Test]
        public void Fertiliser_EnsureApplyOverDepthRangeWorks()
        {
            // Create a tree with a root node for our models.
            var simulation = new Simulation()
            {
                Children = new List<IModel>()
                {
                    new Clock()
                    {
                        StartDate = new DateTime(2015, 1, 1),
                        EndDate = new DateTime(2015, 1, 1)
                    },
                    new MockSummary(),
                    new MockSoil()
                    {
                        NO3 = new double[] { 1, 2, 3, 4 },
                        Children = new List<IModel>()
                        {
                            new MockSoilSolute("NO3"),
                            new MockSoilSolute("NH4"),
                            new MockSoilSolute("Urea"),
                            new Physical() { Thickness = new double[] { 100, 100, 200, 200 }}
                        }
                    },
                    new Fertiliser() { Name = "Fertilise", ResourceName = "Fertiliser" },
                    new Operations()
                    {
                        Operation = new List<Operation>()
                        {
                            new Operation()
                            {
                                Date = "1-jan",
                                Action = "[Fertilise].Apply(amount: 50, type:Fertiliser.Types.NO3N, depthTop:75, depthBottom: 300)"
                            }
                        }
                    }
                }
            };
            Resource.Instance.Replace(simulation);
            FileFormat.InitialiseModel(simulation, (e) => throw e);
            simulation.Prepare();
            simulation.Run();

            var soil = simulation.Children[2] as MockSoil;
            Assert.That(soil.NO3[0], Is.EqualTo(6.56).Within(0.01));
            Assert.That(soil.NO3[1], Is.EqualTo(24.2).Within(0.1));
            Assert.That(soil.NO3[2], Is.EqualTo(25.2).Within(0.1));
        }
    }
}
