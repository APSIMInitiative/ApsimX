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
        /// <param name="UpdateStatus">Action which will display job submission status to the user.</param>
        Task SubmitJobAsync(JobParameters job, Action<string> UpdateStatus);

        /// <summary>
        /// List all apsim jobs on a cloud platform.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        Task<List<JobDetails>> ListJobsAsync(CancellationToken ct, Action<double> ShowProgress);

        /// <summary>
        /// Halt the execution of a job.
        /// </summary>
        /// <param name="jobID">ID of the job.</param>
        Task StopJobAsync(string jobID);

        /// <summary>
        /// Delete a job and all cloud storage associated with the job.
        /// </summary>
        /// <param name="jobID">ID of the job.</param>
        Task DeleteJobAsync(string jobID);

        /// <summary>Download the results of a job.</summary>
        /// <param name="options">Download options.</param>
        /// <param name="ct">Cancellation token.</param>
        Task DownloadResultsAsync(DownloadOptions options, CancellationToken ct);
    }
}
