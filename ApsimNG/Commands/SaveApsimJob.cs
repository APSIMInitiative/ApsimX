namespace UserInterface.Commands
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using System.ComponentModel;

    /// <summary>
    /// A runnable job that looks at the model passed in and determines what is to be run.
    /// Will spawn other jobs to do the actual running.
    /// </summary>
    class SaveApsimJob : JobManager.IRunnable
    {
        /// <summary>The simulations object to save</summary>
        private Simulations simulations;

        /// <summary>Constructor</summary>
        /// <param name="model">The simulations object to save</param>
        public SaveApsimJob(Simulations simulations)
        {
            this.simulations = simulations;
        }

        /// <summary>Called to start the job.</summary>
        /// <param name="jobManager">The job manager running this job.</param>
        /// <param name="workerThread">The thread this job is running on.</param>
        public void Run(JobManager jobManager, BackgroundWorker workerThread)
        {
            simulations.Write(simulations.FileName);
        }
    }
}
