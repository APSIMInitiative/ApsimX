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
        private List<IJobGenerator> modelsToRun;

        /// <summary>Simulation names being run</summary>
        public List<string> SimulationNamesBeingRun { get; private set; }

        /// <summary>All known simulation names</summary>
        public List<string> AllSimulationNames { get; private set; }

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
            if (modelsToRun == null)
            {
                GetListOfModelsToRun(modelSelectedByUser);

                // Send event telling all models that we're about to begin running.
                Events events = new Events(simulations);
                events.Publish("BeginRun", new object[] { SimulationNamesBeingRun, AllSimulationNames });

                modelsToRun.ForEach(model => simulations.Links.Resolve(model));
            }

            // If we didn't find anything to run then return null to tell job runner to exit.
            if (modelsToRun == null || modelsToRun.Count == 0)
                return null;

            else
            {
                // Iterate through all jobs and return the next one.
                IRunnable job = modelsToRun[0].NextJobToRun();
                while (job == null && modelsToRun.Count > 0)
                {
                    modelsToRun.RemoveAt(0);
                    if (modelsToRun.Count > 0)
                        job = modelsToRun[0].NextJobToRun();
                }

                // If we didn't find a job to run then send event telling all models that we're about to end running.
                if (job != null)
                    simulations.Links.Resolve(job);
                return job;
            }
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

        /// <summary>Determine the list of jobs to run</summary>
        /// <param name="relativeTo">Model to use to find jobs to run</param>
        private void GetListOfModelsToRun(IModel relativeTo)
        {
            AllSimulationNames = new List<string>();
            SimulationNamesBeingRun = new List<string>();

            // get a list of all simulation names.
            List<IJobGenerator> allModels = Apsim.ChildrenRecursively(simulations, typeof(IJobGenerator)).Cast<IJobGenerator>().ToList();
            allModels.ForEach(model => AllSimulationNames.AddRange(model.GetSimulationNames()));

            // Get a list of all models we're going to run.
            modelsToRun = Apsim.ChildrenRecursively(relativeTo, typeof(IJobGenerator)).Cast<IJobGenerator>().ToList();

            // For each model, get a list of simulation names.
            modelsToRun.ForEach(model => SimulationNamesBeingRun.AddRange(model.GetSimulationNames()));
        }
    }
}
