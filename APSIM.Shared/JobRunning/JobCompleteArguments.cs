namespace APSIM.Shared.JobRunning
{
    using System;

    /// <summary>Arguments for JobComplete event</summary>
    public class JobCompleteArguments
    {
        /// <summary>The job that was completed</summary>
        public IRunnable Job { get; set; }

        /// <summary>The exception thrown by the job. Can be null for no exception.</summary>
        public Exception ExceptionThrowByJob { get; set; }

        /// <summary>The amount of time the job took to run.</summary>
        public TimeSpan ElapsedTime { get; set; }
    }
}
