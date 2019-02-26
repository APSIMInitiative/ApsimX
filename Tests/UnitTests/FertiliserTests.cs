using Models;
using Models.Core;
using Models.Functions;
using Models.Soils;
using NUnit.Framework;
using System;
using Models.Core.Runners;
using System.Collections.Generic;
using Models.Interfaces;
using Models.Core.Interfaces;
using APSIM.Shared.Utilities;

namespace UnitTests
{
    [TestFixture]
    class FertiliserTests
    {
        /// <summary>Test setup routine. Returns a soil properties that can be used for testing.</summary>
        [Serializable]
        public class MockSoil : Model, ISoil
        {
            public double[] Thickness { get; set; }

            public double[] NO3 { get; set; }
        }

        class MockSoilSolute : Model, ISolute
        {
            private MockSoil parentSoil;

            public MockSoilSolute(MockSoil parent)
            {
                parentSoil = parent;
                Name = "NO3";
            }
            public MockSoilSolute(MockSoil parent, string name)
            {
                parentSoil = parent;
                Name = name;
            }
            public double[] kgha { get { return parentSoil.NO3; } set { parentSoil.NO3 = value; } }

            public double[] ppm => throw new NotImplementedException();

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
            Simulation simulation = new Simulation();

            Clock clock = new Clock();
            clock.StartDate = new DateTime(2015, 1, 1);
            clock.EndDate = new DateTime(2015, 1, 1);
            simulation.Children.Add(clock);

            MockSummary summary = new MockSummary();
            simulation.Children.Add(summary);

            MockSoil soil = new MockSoil();
            soil.Thickness = new double[] { 100, 100, 100 };
            soil.NO3 = new double[] { 1, 2, 3 };
            simulation.Children.Add(soil);
            soil.Children.Add(new MockSoilSolute(soil, "NO3"));
            soil.Children.Add(new MockSoilSolute(soil, "NH4"));
            soil.Children.Add(new MockSoilSolute(soil, "Urea"));
            Fertiliser fertiliser = new Fertiliser();
            fertiliser.Name = "Fertilise";
            simulation.Children.Add(fertiliser);

            Operations operations = new Operations();
            Operation fertiliseOperation = new Operation();
            fertiliseOperation.Date = "1-jan";
            fertiliseOperation.Action = "[Fertilise].Apply(Amount: 100, Type:Fertiliser.Types.NO3N, Depth:300)";
            operations.Operation = new List<Operation>();
            operations.Operation.Add(fertiliseOperation);
            simulation.Children.Add(operations);

            ISimulationEngine simulationEngine = Simulations.Create(new Model[] { simulation });
            simulationEngine.Run(simulation, doClone:false);

            Assert.AreEqual(soil.NO3, new double[] { 1, 2, 103 });
            Assert.AreEqual(MockSummary.messages[0], "100 kg/ha of NO3N added at depth 300 layer 3");
        }



    }
}
