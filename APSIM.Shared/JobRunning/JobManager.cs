namespace APSIM.Shared.JobRunning
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Linq;

    /// <summary>
    /// Manages a collection of jobs.
    /// </summary>
    public class JobManager : IJobManager
    {
        private Queue<IRunnable> jobs = new Queue<IRunnable>();
        /// <summary> number of jobs </summary>
        protected int numJobsToRun;
        private bool initialised;

        /// <summary>Invoked when this job manager has finished everything.</summary>
        public event EventHandler Completed;

        /// <summary>
        /// Returns total number of jobs. This includes jobs which
        /// have not yet run, and jobs which have already run.
        /// </summary>
        public int NumJobs { get; protected set; }

        /// <summary>Call JobHasCompleted when job is complete?</summary>
        public bool NotifyWhenJobComplete => true;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="job"></param>
        public void Add(IRunnable job)
        {
            jobs.Enqueue(job);
            NumJobs++;
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
            // Modulus arithmetic. This is a hack to allow the server job runner
            // to reuse the same job manager object across multiple runs. Ideally,
            // this should all be refactored out, possibly by making PostAllRuns()
            // get called from outside of this class, by something with enough
            // context to know when it needs to be called. What should really happen
            // is that the numJobsToRun variable gets set to NumJobs when a run
            // (of all jobs 'owned' by this job manager) first starts.
            // numJobsToRun should also probably be uint.
            if (numJobsToRun < 0)
                numJobsToRun = NumJobs - 1;
            if (numJobsToRun == 0)
            {
                PostAllRuns();
                Completed?.Invoke(this, new EventArgs());
            }
        }

        /// <summary>Called once to do initialisation before any jobs are run. Should throw on error.</summary>
        protected virtual void PreRun() { }

        /// <summary>Called once when all jobs have completed running. Should throw on error.</summary>
        protected virtual void PostRun(JobCompleteArguments args) { }

        /// <summary>Called once when all jobs have completed running. Should throw on error.</summary>
        protected virtual void PostAllRuns() { }
    }
}
