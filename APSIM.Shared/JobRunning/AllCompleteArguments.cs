namespace APSIM.Shared.JobRunning
{
    using System;

    /// <summary>Arguments for AllJobComplete event</summary>
    public class AllCompleteArguments
    {
        /// <summary>The exception thrown by the job. Can be null for no exception.</summary>
        public Exception ExceptionThrowByRunner { get; set; }

        /// <summary>The amount of time the job took to run.</summary>
        public TimeSpan ElapsedTime { get; set; }
    }
}
