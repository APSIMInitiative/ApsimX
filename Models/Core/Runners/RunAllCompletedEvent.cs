namespace Models.Core.Runners
{
    using APSIM.Shared.Utilities;
    using System;
    using System.ComponentModel;

    /// <summary>
    /// This runnable class runs an external process.
    /// </summary>
    public class RunAllCompletedEvent : JobManager.IRunnable
    {
        private Simulations simulations;

        /// <summary>Initializes a new instance of the <see cref="RunAllCompletedEvent"/> class.</summary>
        /// <param name="simulations">Top level simulations object.</param>
        public RunAllCompletedEvent(Simulations simulations)
        {
            this.simulations = simulations;
        }

        /// <summary>Called to start the job.</summary>
        /// <param name="jobManager">The job manager running this job.</param>
        /// <param name="workerThread">The thread this job is running on.</param>
        public void Run(JobManager jobManager, BackgroundWorker workerThread)
        {
            ILocator locator = simulations.GetLocatorService(simulations);
            IStorageReader store = locator.Get(typeof(IStorageReader)) as IStorageReader;
            store.EndWriting();

            // Call the all completed event in all models
            object[] args = new object[] { this, new EventArgs() };

            Events events = new Events(simulations);
            events.Publish("AllCompleted", args);
        }
    }
}
