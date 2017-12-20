using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApsimNG.Cloud
{
    class TaskDetails
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }

        public string State { get; set; }

        public DateTime? StartTime { get; set; }

        public DateTime? EndTime { get; set; }

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
