namespace APSIM.Shared.JobRunning
{
    using System.Threading;

    /// <summary>A simple runnable sleep job.</summary>
    public class JobRunnerSleepJob : IRunnable
    {
        // Duration of sleep in milliseconds.
        private int durationOfSleep;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="duration">Duration of sleep in milliseconds.</param>
        public JobRunnerSleepJob(int duration = 200)
        {
            durationOfSleep = duration;
        }

        /// <summary>
        /// Returns the job's progress as a real number in range [0, 1].
        /// </summary>
        public double Progress { get { return 0; } }

        /// <summary>
        /// Name of the job.
        /// </summary>
        public string Name { get { return $"Sleep job ({durationOfSleep}ms)"; } }

        /// <summary>
        /// Cleanup the job after running it.
        /// </summary>
        public void Cleanup()
        {
            // Do nothing.
        }

        /// <summary>
        /// Prepare the job for running.
        /// </summary>
        public void Prepare()
        {
            // Do nothing.
        }

        /// <summary>Called to start the job. Can throw on error.</summary>
        /// <param name="cancelToken">Is cancellation pending?</param>
        public void Run(CancellationTokenSource cancelToken)
        {
            if (!cancelToken.IsCancellationRequested)
                Thread.Sleep(durationOfSleep);
        }
    }
}
