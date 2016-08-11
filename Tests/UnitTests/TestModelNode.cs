using Models.Core;
using Models;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace UnitTests
{
    [TestFixture]
    class TestModelNode
    {
        public class ModelWithProperty
        {

        }

        [Test]
        public void TestGet()
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

            simulation.Add(new MockSummary());
            ModelWrapper zone = simulation.Add(new Zone());

            // Check that it is case sensitive.
            Assert.IsNull(models.Get("simulation.clock.StartDate"));

            // Fix case this should work.
            object d = models.Get("Simulation.Clock.StartDate");
            DateTime startDate = (DateTime)d;
            Assert.AreEqual(startDate.Year, 2015);
            Assert.AreEqual(startDate.Month, 1);
            Assert.AreEqual(startDate.Day, 1);
        }

        [Test]
        public void TestSet()
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

            simulation.Add(new MockSummary());
            ModelWrapper zone = simulation.Add(new Zone());

            // Fix case this should work.
            Assert.IsTrue(models.Set("Simulation.Clock.EndDate", new DateTime(2016, 1, 1)));
            Assert.AreEqual(clock.EndDate.Year, 2016);
            Assert.AreEqual(clock.EndDate.Month, 1);
            Assert.AreEqual(clock.EndDate.Day, 1);
        }
    }
}
