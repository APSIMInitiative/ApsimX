using System;

namespace ApsimNG.Cloud
{
    /// <summary>
    /// Options exposed to the user for downloading results of a job run on the cloud.
    /// </summary>
    public class DownloadOptions
    {
        /// <summary>ID of the job to be downloaded.</summary>
        public Guid JobID { get; set; }

        /// <summary>Path to which results will be downloaded.</summary>
        public string Path { get; set; }
    }
}