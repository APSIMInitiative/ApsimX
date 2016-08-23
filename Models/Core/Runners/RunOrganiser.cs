namespace Models.Core.Runners
{
    using APSIM.Shared.Utilities;
    using Factorial;
    using System.Collections.Generic;
    using System.ComponentModel;
    /// <summary>
    /// A runnable job that looks at the model passed in and determines what is to be run.
    /// Will spawn other jobs to do the actual running.
    /// </summary>
    class RunOrganiser : JobManager.IRunnable
    {
        private Simulations simulations;
        private Model model;
        private bool runTests;

        /// <summary>Constructor</summary>
        /// <param name="model">The model to run.</param>
        /// <param name="simulations">simulations object.</param>
        /// <param name="runTests">Run the test nodes?</param>
        public RunOrganiser(Simulations simulations, Model model, bool runTests)
        {
            this.simulations = simulations;
            this.model = model;
            this.runTests = runTests;
        }

        /// <summary>Called to start the job.</summary>
        /// <param name="jobManager">The job manager running this job.</param>
        /// <param name="workerThread">The thread this job is running on.</param>
        public void Run(JobManager jobManager, BackgroundWorker workerThread)
        {
            // IF we are going to run all simulations, we can delete all tables in the DataStore. This
            // will clean up order of columns in the tables and removed unused ones.
            // Otherwise just remove the unwanted simulations from the DataStore.
            DataStore store = Apsim.Child(simulations, typeof(DataStore)) as DataStore;
            if (model is Simulations)
                store.DeleteAllTables();
            else
                store.RemoveUnwantedSimulations(simulations);
            store.Disconnect();

            JobSequence parentJob = new JobSequence();
            JobParallel simulationJobs = new JobParallel();
            FindAllSimulationsToRun(model, simulationJobs);
            parentJob.Jobs.Add(simulationJobs);
            parentJob.Jobs.Add(new RunAllCompletedEvent(simulations));

            if (runTests)
            {
                foreach (Tests tests in Apsim.ChildrenRecursively(model, typeof(Tests)))
                    parentJob.Jobs.Add(tests);
            }
            jobManager.AddChildJob(this, parentJob);
        }

        /// <summary>Find simulations/experiments to run.</summary>
        /// <param name="model">The model and its children to search.</param>
        /// <param name="parentJob">The parent job to add the child jobs to.</param>
        private void FindAllSimulationsToRun(IModel model, JobParallel parentJob)
        { 
            // Get jobs to run and give them to job manager.
            List<JobManager.IRunnable> jobs = new List<JobManager.IRunnable>();

            if (model is Experiment)
                parentJob.Jobs.Add(model as Experiment);
            else if (model is Simulation)
            {
                if (model.Parent == null)
                    parentJob.Jobs.Add(model as Simulation);
                else
                    parentJob.Jobs.Add(new RunClonedSimulation(model as Simulation));
            }
            else
            {
                // Look for simulations.
                foreach (Model child in model.Children)
                    if (child is Experiment || child is Simulation || child is Folder)
                        FindAllSimulationsToRun(child, parentJob);
            }
        }
    }
}
