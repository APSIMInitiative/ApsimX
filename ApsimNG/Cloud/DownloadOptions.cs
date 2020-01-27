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

        /// <summary>Extract output .db files?</summary>
        public bool ExtractResults { get; set; }

        /// <summary>Combine results into a .csv file?</summary>
        public bool ExportToCsv { get; set; }

        /// <summary>Download debugging (.stdout) files?</summary>
        public bool DownloadDebugFiles { get; set; }
    }
}