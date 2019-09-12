namespace APSIM.Shared.JobRunning
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Manages a collection of jobs.
    /// </summary>
    public class JobManager : IJobManager
    {
        private Queue<IRunnable> jobs = new Queue<IRunnable>();
        private int numJobsToRun;
        private bool initialised;

        /// <summary>Invoked when this job manager has finished everything.</summary>
        public event EventHandler Completed;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="job"></param>
        public void Add(IRunnable job)
        {
            jobs.Enqueue(job);
            Interlocked.Increment(ref numJobsToRun);
        }

        /// <summary>Return an enumeration of jobs that need running.</summary>
        public IEnumerable<IRunnable> GetJobs()
        {
            if (!initialised)
            {
                PreRun();
                initialised = true;
            }

            while (jobs.Count > 0)
                yield return jobs.Dequeue();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        public virtual void JobHasCompleted(JobCompleteArguments args)
        {
            PostRun(args);
            Interlocked.Decrement(ref numJobsToRun);
            if (numJobsToRun == 0)
                PostAllRuns();
            Completed?.Invoke(this, new EventArgs());
        }

        /// <summary>Called once to do initialisation before any jobs are run. Should throw on error.</summary>
        protected virtual void PreRun() { }

        /// <summary>Called once when all jobs have completed running. Should throw on error.</summary>
        protected virtual void PostRun(JobCompleteArguments args) { }

        /// <summary>Called once when all jobs have completed running. Should throw on error.</summary>
        protected virtual void PostAllRuns() { }
    }
}
