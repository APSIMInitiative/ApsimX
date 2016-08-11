using Models.Core;
using Models;
using APSIM.Shared.Utilities;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.PMF.Functions;

namespace UnitTests
{

    [Serializable]
    class ModelWithLinks
    {
        [Link]
        public Zone[] zones = null;

    }

    [Serializable]
    class ModelWithIFunctions
    {
        [Link]
        public IFunction model2 = null;

    }

    [Serializable]
    class IFunctionProxy : IFunction
    {
        public double value;

        public double Value
        {
            get
            {
                return value;
            }
        }
    }

    [Serializable]
    class ModelWithModelNodeLink
    {
        [Link]
        public ModelWrapper model = null;
    }

    [Serializable]
    class ModelWithDynamicProperty
    {
        [Link(AssociatedProperty = "xname")]
        public Property x = null;

        public string xname { get; set; }

    }

    [TestFixture]
    class TestLinks
    {

        [Test]
        public void Link()
        {
            // Create a tree with a root node for our models.
            ModelWrapper models = new ModelWrapper();

            // Create some models.
            ModelWrapper simulations = models.Add(new Simulations());

            ModelWrapper simulation = simulations.Add(new Simulation());

            Clock clock = new Clock();
            clock.StartDate = new DateTime(2015, 1, 1);
            clock.EndDate = new DateTime(2015, 12, 31);
            simulation.Add(clock);

            MockSummary summary = new MockSummary();
            simulation.Add(summary);
            simulation.Add(new Zone());
            simulation.Add(new Zone());

            ModelWithLinks links = new ModelWithLinks();
            simulation.Add(links);

            Links.Resolve(simulations);

            Assert.AreEqual(links.zones.Length, 3);
            Assert.NotNull(links.zones[0]);
            Assert.NotNull(links.zones[1]);
        }

        [Test]
        public void EnsureIFunctionLinksCorrectly()
        {
            // Create a tree with a root node for our models.
            ModelWrapper models = new ModelWrapper();

            // Create some models.
            ModelWrapper simulations = models.Add(new Simulations());

            ModelWrapper simulation = simulations.Add(new Simulation());

            Clock clock = new Clock();
            clock.StartDate = new DateTime(2015, 1, 1);
            clock.EndDate = new DateTime(2015, 12, 31);
            simulation.Add(clock);

            MockSummary summary = new MockSummary();
            simulation.Add(summary);
            simulation.Add(new Zone());
            simulation.Add(new Zone());

            ModelWithIFunctions links = new ModelWithIFunctions();
            simulation.Add(links);

            simulation.Add(new IFunctionProxy() { value = 1 }).Name = "model1";
            simulation.Add(new IFunctionProxy() { value = 2 }).Name = "model2";
            simulation.Add(new IFunctionProxy() { value = 3 }).Name = "model3";

            Links.Resolve(simulations);

            Assert.AreEqual(links.model2.Value, 2);
        }

        [Test]
        public void EnsureModelNodeLinksCorrectly()
        {
            // Create a tree with a root node for our models.
            ModelWrapper models = new ModelWrapper();

            // Create some models.
            ModelWrapper simulations = models.Add(new Simulations());

            ModelWrapper simulation = simulations.Add(new Simulation());

            Clock clock = new Clock();
            clock.StartDate = new DateTime(2015, 1, 1);
            clock.EndDate = new DateTime(2015, 12, 31);
            simulation.Add(clock);

            MockSummary summary = new MockSummary();
            simulation.Add(summary);
            simulation.Add(new Zone());
            simulation.Add(new Zone());

            ModelWithModelNodeLink modelWithModelNode = new ModelWithModelNodeLink();
            simulation.Add(modelWithModelNode);

            Links.Resolve(simulations);

            Assert.IsNotNull(modelWithModelNode.model);
        }

        [Test]
        public void EnsureDynamicPropertyLinksCorrectly()
        {
            // Create a tree with a root node for our models.
            ModelWrapper models = new ModelWrapper();

            // Create some models.
            ModelWrapper simulations = models.Add(new Simulations());

            ModelWrapper simulation = simulations.Add(new Simulation());

            Clock clock = new Clock();
            clock.StartDate = new DateTime(2015, 1, 1);
            clock.EndDate = new DateTime(2015, 12, 31);
            simulation.Add(clock);

            MockSummary summary = new MockSummary();
            simulation.Add(summary);
            simulation.Add(new Zone());
            simulation.Add(new Zone());

            ModelWithDynamicProperty modelWithDynamicProperty = new ModelWithDynamicProperty();
            modelWithDynamicProperty.xname = "Clock.StartDate";
            simulation.Add(modelWithDynamicProperty);

            Links.Resolve(simulation);

            Assert.IsNotNull(modelWithDynamicProperty);
            Assert.AreEqual(modelWithDynamicProperty.x.Get(), clock.StartDate);
        }

    }
}
