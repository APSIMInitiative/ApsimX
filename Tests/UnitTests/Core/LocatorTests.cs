namespace UnitTests.Core
{
    using Models;
    using Models.Core;
    using Models.Soils;
    using NUnit.Framework;
    using System;

    [TestFixture]
    public class LocatorTests
    {
        class ModelA : Model
        {
            public int A1 { get { return 1; } }
            public int A2 { get { return 2; } }
        }

        class ModelB : Model
        {
            public int B1 { get { return 3; } }
            public int B2 { get { return 4; } }
        }

        class ModelC : Model
        {
            public int C1 { get { return 5; } }
            public double[] C2 { get { return new double[] { 6.0, 6.1, 6.2 }; } }
        }

        class ModelD : Model
        {
            public int D1 { get { return 7; } }
            public DateTime D2 { get { return new DateTime(2000, 1, 1); } }
        }

        class ModelE : Model
        {
            public ModelF[] models = new ModelF[] { new ModelF(), new ModelF() };
            public ModelF[] E1 { get { return models; } }
        }

        class ModelF : Model
        {
            public int F { get; set; }
        }

        [Test]
        public void LocatorGetVariable()
        {
            Simulation sim = new Simulation();
            sim.Children.Add(new ModelA());
            sim.Children.Add(new ModelB());
            sim.Children.Add(new Zone());
            sim.Children[2].Children.Add(new ModelC());
            sim.Children[2].Children.Add(new ModelD());

            Simulations sims = Simulations.Create(new Model[] { sim } );

            // locator for modelC
            ILocator locatorForC = sims.GetLocatorService(sim.Children[2].Children[0]);
            Assert.AreEqual(locatorForC.Get("[ModelA].A1"), 1);

            // locator for modelD
            ILocator locatorForD = sims.GetLocatorService(sim.Children[2].Children[1]);
            Assert.AreEqual(locatorForD.Get("[ModelD].D2.Year"), 2000);
        }

        [Test]
        public void LocatorGetVariableWithArrayIndex()
        {
            Simulation sim = new Simulation();
            sim.Children.Add(new ModelA());
            sim.Children.Add(new ModelB());
            sim.Children.Add(new Zone());
            sim.Children[2].Children.Add(new ModelC());
            sim.Children[2].Children.Add(new ModelD());

            Simulations sims = Simulations.Create(new Model[] { sim });

            // locator for modelD
            ILocator locatorForD = sims.GetLocatorService(sim.Children[2].Children[1]);
            Assert.AreEqual(locatorForD.Get("[ModelC].C2[1]"), 6.0);
            Assert.AreEqual(locatorForD.Get("[ModelC].C2[2]"), 6.1);
            Assert.AreEqual(locatorForD.Get("[ModelC].C2[3]"), 6.2);
        }

        [Test]
        public void LocatorGetVariableWithAbsoluteAddress()
        {
            Simulation sim = new Simulation();
            sim.Children.Add(new ModelA());
            sim.Children.Add(new ModelB());
            sim.Children.Add(new Zone());
            sim.Children[2].Children.Add(new ModelC());
            sim.Children[2].Children.Add(new ModelD());

            Simulations sims = Simulations.Create(new Model[] { sim });

            // locator for modelC
            ILocator locatorForC = sims.GetLocatorService(sim.Children[2].Children[0]);
            Assert.AreEqual(locatorForC.Get(".Simulations.Simulation.ModelA.A1"), 1);

            // locator for modelD
            ILocator locatorForD = sims.GetLocatorService(sim.Children[2].Children[1]);
            Assert.AreEqual(locatorForD.Get(".Simulations.Simulation.Zone.ModelD.D2.Year"), 2000);
        }

        [Test]
        public void LocatorGetVariableWithRelativeAddress()
        {
            Simulation sim = new Simulation();
            sim.Children.Add(new ModelA());
            sim.Children.Add(new ModelB());
            sim.Children.Add(new Zone());
            sim.Children[2].Children.Add(new ModelC());
            sim.Children[2].Children.Add(new ModelD());

            Simulations sims = Simulations.Create(new Model[] { sim });

            // locator for zone
            ILocator locatorForZone = sims.GetLocatorService(sim.Children[2]);
            Assert.AreEqual(locatorForZone.Get("ModelC.C1"), 5);
        }

        [Test]
        public void LocatorGetExpression()
        {
            Simulation sim = new Simulation();
            sim.Children.Add(new ModelA());
            sim.Children.Add(new ModelB());
            sim.Children.Add(new Zone());
            sim.Children[2].Children.Add(new ModelC());
            sim.Children[2].Children.Add(new ModelD());

            Simulations sims = Simulations.Create(new Model[] { sim });

            // locator for modelC
            ILocator locatorForC = sims.GetLocatorService(sim.Children[2].Children[0]);
            Assert.AreEqual(locatorForC.Get("[ModelA].A1+[ModelD].D2.Year"), 2001);
        }

        [Test]
        public void LocatorGetModel()
        {
            Simulation sim = new Simulation();
            sim.Children.Add(new ModelA());
            sim.Children.Add(new ModelB());
            sim.Children.Add(new Zone());
            sim.Children[2].Children.Add(new ModelC());
            sim.Children[2].Children.Add(new ModelD());

            Simulations sims = Simulations.Create(new Model[] { sim });

            // locator for modelC
            ILocator locatorForC = sims.GetLocatorService(sim.Children[2].Children[0]);
            Assert.AreEqual(locatorForC.Get("[ModelA]"), sim.Children[0]);
        }

        [Test]
        public void LocatorGetPropertyOfModelAtSpecificArrayElement()
        {
            Simulation sim = new Simulation();
            sim.Children.Add(new ModelF());
            sim.Children.Add(new ModelB());
            sim.Children.Add(new Zone());
            sim.Children[2].Children.Add(new ModelC());
            ModelE e = new ModelE();
            e.models[0].F = 20;
            e.models[1].F = 21;
            sim.Children[2].Children.Add(e);

            Simulations sims = Simulations.Create(new Model[] { sim });

            // locator for modelC
            ILocator locatorForC = sims.GetLocatorService(sim.Children[2].Children[0]);
            Assert.AreEqual(locatorForC.Get("[ModelE].E1[1].F"), 20);
            Assert.AreEqual(locatorForC.Get("[ModelE].E1[2].F"), 21);
        }
    }
}