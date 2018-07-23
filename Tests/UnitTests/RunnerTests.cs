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
using Models.Factorial;
using System.Linq;

namespace UnitTests
{
    [TestFixture]
    class RunnerTests
    {
        /// <summary>Create a bunch of simulations</summary>
        [Test]
        public void Runner_CreateSimulations()
        {
            // Create a tree with a root node for our models.
            Simulation simulation = new Simulation();

            Clock clock = new Clock();
            clock.StartDate = new DateTime(2015, 1, 1);
            clock.EndDate = new DateTime(2015, 1, 1);
            simulation.Children.Add(clock);

            simulation.Children.Add(new MockSummary());
            simulation.Children.Add(new MockStorage());

            Experiment experiment = new Experiment();
            Factors factors = new Factors();
            Factor factor1 = new Factor();
            factor1.Specifications = new List<string>();
            factor1.Specifications.Add("[Clock].StartDate = 2003-11-01, 2003-12-20");
            experiment.Children.Add(simulation);
            factors.Children.Add(factor1);
            experiment.Children.Add(factors);

            Simulations topLevelSimulationsModel = Simulations.Create(new IModel[] { experiment,
                                                                      Apsim.Clone(simulation) });

            Runner.SimulationCreator simulationCreator = Runner.AllSimulations(topLevelSimulationsModel);

            string[] simulationNames = simulationCreator.Select(sim => sim.Name).ToArray();
            Assert.AreEqual(simulationNames, new string[] { "ExperimentFactor2003-11-01",
                                                            "ExperimentFactor2003-12-20",
                                                            "Simulation" });

            Assert.AreEqual(simulationCreator.SimulationNamesBeingRun, 
                            new string[] { "ExperimentFactor2003-11-01",
                                           "ExperimentFactor2003-12-20",
                                           "Simulation" });
        }



    }
}
