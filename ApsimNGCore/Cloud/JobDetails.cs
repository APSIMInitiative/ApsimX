using System;

namespace ApsimNG.Cloud
{
    /// <summary>
    /// This class holds the details about a cloud job which are displayed in the job viewer.
    /// </summary>
    public class JobDetails
    {
        /// <summary>
        /// Unique Identifier for the job.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Name/description for the job.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Status of the job (uploading, finished, etc).
        /// </summary>
        public string State { get; set; }

        /// <summary>
        /// Username of job owner.
        /// </summary>
        public string Owner { get; set; }

        /// <summary>
        /// Proportion of simulations completed as a percentage.
        /// </summary>
        public double Progress { get; set; }

        /// <summary>
        /// Total number of simulations.
        /// </summary>
        public long NumSims { get; set; }

        /// <summary>
        /// Start time of the job.
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// End time of the job.
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Duration of the job.
        /// </summary>
        public TimeSpan Duration()
        {
            if (StartTime == null || EndTime == null)
                return TimeSpan.Zero;
            return EndTime.Value - StartTime.Value;
        }
        
        /// <summary>
        /// Total CPU time of the job.
        /// </summary>
        public TimeSpan CpuTime { get; set; }

        /// <summary>
        /// Tests if two jobs are equal.
        /// </summary>
        /// <param name="a">The first job.</param>
        /// <param name="b">The second job.</param>
        /// <returns>True if the jobs have the same ID and they are in the same state.</returns>
        public bool IsEqualTo(JobDetails b)
        {
            return (Id == b.Id && State == b.State && Progress == b.Progress);
        }
    }
}
