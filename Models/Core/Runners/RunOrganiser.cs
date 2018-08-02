namespace Models.Core.Runners
{
    using APSIM.Shared.Utilities;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>A job manager that looks after running all simulations</summary>
    public class RunOrganiser : IJobManager
    {
        private Simulations simulations;
        private IModel modelSelectedByUser;
        private bool runTests;
        private IEnumerator<Simulation> simulationEnumerator;

        /// <summary>Simulation names being run</summary>
        public List<string> SimulationNamesBeingRun { get; private set; }

        /// <summary>
        /// Clocks of simulations that have begun running
        /// </summary>
        public List<IClock> SimClocks
        {
            get
            {
                if (simulationEnumerator as Runner.SimulationEnumerator != null)
                    return (simulationEnumerator as Runner.SimulationEnumerator).simClocks;
                else
                    return null;
            }
        }

        /// <summary>All known simulation names</summary>
        public List<string> AllSimulationNames
        {
            get
            {
                List<string> AllSimulationNames = new List<string>();
                List<ISimulationGenerator> allModels = Apsim.ChildrenRecursively(simulations, typeof(ISimulationGenerator)).Cast<ISimulationGenerator>().ToList();
                allModels.ForEach(model => AllSimulationNames.AddRange(model.GetSimulationNames(false)));
                return AllSimulationNames;
            }
        }

        /// <summary>Constructor</summary>
        /// <param name="model">The model to run.</param>
        /// <param name="simulations">simulations object.</param>
        /// <param name="runTests">Run the test nodes?</param>
        public RunOrganiser(Simulations simulations, IModel model, bool runTests)
        {
            this.simulations = simulations;
            this.modelSelectedByUser = model;
            this.runTests = runTests;
        }

        /// <summary>Return the index of next job to run or -1 if nothing to run.</summary>
        /// <returns>Job to run or null if no more jobs to run</returns>
        public IRunnable GetNextJobToRun()
        {
            // First time through there. Get a list of things to run.
            if (simulationEnumerator == null)
            {
                // Send event telling all models that we're about to begin running.
                Events events = new Events(simulations);
                events.Publish("BeginRun", null);

                Runner.SimulationEnumerator enumerator= new Runner.SimulationEnumerator(modelSelectedByUser);
                simulationEnumerator = enumerator;
                SimulationNamesBeingRun = enumerator.SimulationNamesBeingRun;

                // Send event telling all models that we're about to begin running.
                events.Publish("RunCommencing", new object[] { AllSimulationNames, SimulationNamesBeingRun });
            }

            // If we didn't find anything to run then return null to tell job runner to exit.
            if (simulationEnumerator.MoveNext())
                return new RunSimulation(simulations, simulationEnumerator.Current, false);
            else
                return null;
        }

        /// <summary>Called by the job runner when all jobs completed</summary>
        public void Completed()
        {
            Events events = new Events(simulations);
            events.Publish("EndRun", new object[] {this, new EventArgs() });

            // Optionally run the tests
            if (runTests)
            {
                foreach (Tests test in Apsim.ChildrenRecursively(simulations, typeof(Tests)))
                    test.Test();
            }
        }
    }
}
