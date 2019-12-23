using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ApsimNG.Cloud
{
    /// <summary>
    /// Interface for cloud functionality.
    /// </summary>
    public interface ICloudInterface
    {
        /// <summary>
        /// List all Azure jobs.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        Task<JobDetails> ListJobs(CancellationToken ct);

        /// <summary>
        /// Submit a job to be run on Azure.
        /// </summary>
        /// <param name="job">Job parameters.</param>
        /// <param name="UpdateStatus">Action which will display job submission status to the user.</param>
        Task SubmitJobs(JobParameters job, Action<string> UpdateStatus);
    }
}
