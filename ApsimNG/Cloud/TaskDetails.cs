using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApsimNG.Cloud
{
    class TaskDetails
    {
        /// <summary>
        /// Unique identifier for the task.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Task name/description.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Status of the task.
        /// </summary>
        public string State { get; set; }

        /// <summary>
        /// Start time of the task.
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// Finish time of the task.
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Duration of the task.
        /// </summary>
        public TimeSpan? Duration
        {
            get
            {
                if (StartTime == null)
                {
                    return null;
                }

                return (EndTime != null ? EndTime.Value : DateTime.UtcNow) - StartTime.Value;
            }
        }
    }
}
