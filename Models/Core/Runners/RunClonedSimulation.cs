namespace Models.Core.Runners
{
    using APSIM.Shared.Utilities;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;


    /// <summary>
    /// This runnable class clones a simulation and then runs it.
    /// </summary>
    class RunClonedSimulation : JobManager.IRunnable, JobManager.IComputationalyTimeConsuming
    {
        /// <summary>The simulation to clone and run.</summary>
        private Simulation simulation;


        /// <summary>Initializes a new instance of the <see cref="RunExternal"/> class.</summary>
        /// <param name="simulation">The simulation to clone and run.</param>
        public RunClonedSimulation(Simulation simulation)
        {
            this.simulation = simulation;
        }

        /// <summary>Called to start the job.</summary>
        /// <param name="jobManager">The job manager running this job.</param>
        /// <param name="workerThread">The thread this job is running on.</param>
        public void Run(JobManager jobManager, BackgroundWorker workerThread)
        {
            Simulation clonedSim = Apsim.Clone(simulation) as Simulation;

            Simulations simulations = Apsim.Parent(simulation, typeof(Simulations)) as Simulations;
            simulation.FileName = simulations.FileName;

            Runner.MakeSubstitutions(simulations, new List<Simulation> { clonedSim });

            Simulations.CallOnLoaded(clonedSim);
            jobManager.AddChildJob(this, clonedSim);
        }
    }
}