using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApsimNG.Cloud
{
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
        public TimeSpan? Duration
        {
            get
            {
                if (StartTime == null) return null;
                return (EndTime != null ? EndTime.Value : DateTime.UtcNow) - StartTime.Value;
            }
        }

        /// <summary>
        /// Pool settings of the job.
        /// </summary>
        public PoolSettings PoolSettings { get; set; }

        public TimeSpan CpuTime { get; set; }
    }
}
