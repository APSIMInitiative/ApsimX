using System;
using System.Collections.Generic;

namespace APSIM.Shared.JobRunning
{
    /// <summary>
    /// A class for managing jobs that are to be run with the JobRunner. A JobManager
    /// does NOT need to be thread safe.
    /// </summary>
    public interface IJobManager
    {
        /// <summary>Return an enumeration of jobs that need running.</summary>
        IEnumerable<IRunnable> GetJobs();

        /// <summary>A job has completed running.</summary>
        void JobHasCompleted(JobCompleteArguments args);
    }
}