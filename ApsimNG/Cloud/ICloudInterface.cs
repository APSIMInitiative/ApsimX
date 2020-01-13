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
        /// Submit a job to be run on a cloud platform.
        /// </summary>
        /// <param name="job">Job parameters.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <param name="UpdateStatus">Action which will display job submission status to the user.</param>
        Task SubmitJobAsync(JobParameters job, CancellationToken ct, Action<string> UpdateStatus);

        /// <summary>
        /// List all apsim jobs on a cloud platform.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        /// <param name="ShowProgress">Function which reports progress to the user.</param>
        Task<List<JobDetails>> ListJobsAsync(CancellationToken ct, Action<double> ShowProgress);

        /// <summary>
        /// Halt the execution of a job.
        /// </summary>
        /// <param name="jobID">ID of the job.</param>
        /// <param name="ct">Cancellation token.</param>
        Task StopJobAsync(string jobID, CancellationToken ct);

        /// <summary>
        /// Delete a job and all cloud storage associated with the job.
        /// </summary>
        /// <param name="jobID">ID of the job.</param>
        /// <param name="ct">Cancellation token.</param>
        Task DeleteJobAsync(string jobID, CancellationToken ct);

        /// <summary>Download the results of a job.</summary>
        /// <param name="options">Download options.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <param name="ShowProgress">Function which reports progress (in range [0, 1]) to the user.</param>
        Task DownloadResultsAsync(DownloadOptions options, CancellationToken ct, Action<double> ShowProgress);
    }
}
