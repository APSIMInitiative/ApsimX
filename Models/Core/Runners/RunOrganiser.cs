namespace Models.Core.Runners
{
    using APSIM.Shared.Utilities;
    using Factorial;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Text;
    using System.Threading;

    /// <summary>Organises the running of a collection of jobs.</summary>
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

        /// <summary>Number of sims running.</summary>
        public int numRunning { get; private set; }

        /// <summary>Run the organiser. Will throw on error.</summary>
        /// <param name="jobManager">The job manager</param>
        /// <param name="workerThread">The thread this job is running on</param>
        public void Run(JobManager jobManager, BackgroundWorker workerThread)
        {
            List<JobManager.IRunnable> jobs = new List<JobManager.IRunnable>();

            Simulation[] simulationsToRun = FindAllSimulationsToRun(this.model);

            // IF we are going to run all simulations, we can delete all tables in the DataStore. This
            // will clean up order of columns in the tables and removed unused ones.
            // Otherwise just remove the unwanted simulations from the DataStore.
            DataStore store = Apsim.Child(simulations, typeof(DataStore)) as DataStore;
            if (model is Simulations)
                store.DeleteAllTables();
            else
                store.RemoveUnwantedSimulations(simulations);
            store.Disconnect();

            foreach (Simulation simulation in simulationsToRun)
            {
                jobs.Add(simulation);
                jobManager.AddJob(simulation);
            }

            // Wait until all our jobs are all finished.
            while (AreSomeJobsRunning(jobs, jobManager))
                Thread.Sleep(200);

            // Collect all error messages.
            string ErrorMessage = null;
            foreach (Simulation job in simulationsToRun)
            {
                if (job.ErrorMessage != null)
                    ErrorMessage += job.ErrorMessage + Environment.NewLine;
            }

            // <summary>Call the all completed event in all models.</summary>
            object[] args = new object[] { this, new EventArgs() };
            foreach (Model childModel in Apsim.ChildrenRecursively(simulations))
            {
                try
                {
                    Apsim.CallEventHandler(childModel, "AllCompleted", args);
                }
                catch (Exception err)
                {
                    ErrorMessage += "Error in file: " + simulations.FileName + Environment.NewLine;
                    ErrorMessage += err.ToString() + Environment.NewLine + Environment.NewLine;
                }
            }

            // Optionally run the test nodes.
            if (runTests)
            {
                foreach (Tests tests in Apsim.ChildrenRecursively(simulations, typeof(Tests)))
                {
                    try
                    {
                        tests.Test();
                    }
                    catch (Exception err)
                    {
                        ErrorMessage += "Error in file: " + simulations.FileName + Environment.NewLine;
                        ErrorMessage += err.ToString() + Environment.NewLine + Environment.NewLine;
                    }
                }
            }

            if (ErrorMessage != null)
                throw new Exception(ErrorMessage);
        }


        /// <summary>Find all simulations under the specified parent model.</summary>
        /// <param name="model">The parent.</param>
        /// <returns></returns>
        private Simulation[] FindAllSimulationsToRun(Model model)
        {
            List<Simulation> simulations = new List<Simulation>();

            if (model is Experiment)
                simulations.AddRange((model as Experiment).Create());
            else if (model is Simulation)
            {
                Simulation clonedSim;
                if (model.Parent == null)
                {
                    // model is already a cloned simulation, probably from user running a single 
                    // simulation from an experiment.
                    clonedSim = model as Simulation;
                }
                else
                    clonedSim = Apsim.Clone(model) as Simulation;

                Simulations.MakeSubstitutions(this.simulations, new List<Simulation> { clonedSim });

                Simulations.CallOnLoaded(clonedSim);
                simulations.Add(clonedSim);
            }
            else
            {
                // Look for simulations.
                foreach (Model child in Apsim.ChildrenRecursively(model))
                {
                    if (child is Experiment)
                        simulations.AddRange((child as Experiment).Create());
                    else if (child is Simulation && !(child.Parent is Experiment))
                        simulations.AddRange(FindAllSimulationsToRun(child));
                }
            }

            // Make sure each simulation has it's filename set correctly.
            foreach (Simulation simulation in simulations)
            {
                if (simulation.FileName == null)
                    simulation.FileName = RootSimulations(model).FileName;
            }

            return simulations.ToArray();
        }

        /// <summary>Are some jobs still running?</summary>
        /// <param name="jobs">The jobs to check.</param>
        /// <param name="jobManager">The job manager</param>
        /// <returns>True if all completed.</returns>
        private static bool AreSomeJobsRunning(List<JobManager.IRunnable> jobs, JobManager jobManager)
        {
            foreach (JobManager.IRunnable job in jobs)
            {
                if (!jobManager.IsJobCompleted(job))
                    return true;
            }
            return false;
        }


        /// <summary>Roots the simulations.</summary>
        /// <param name="model">The model.</param>
        /// <returns></returns>
        private static Simulations RootSimulations(Model model)
        {
            Model m = model;
            while (m != null && m.Parent != null && !(m is Simulations))
                m = m.Parent as Model;

            return m as Simulations;
        }
    }
}
