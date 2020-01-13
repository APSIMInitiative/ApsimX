using ApsimNG.Cloud;
using ApsimNG.EventArguments;
using System;
using System.Collections.Generic;

namespace ApsimNG.Interfaces
{
    interface ICloudJobView
    {
        /// <summary>
        /// Invoked when the user wants to change the results output path.
        /// </summary>
        event EventHandler ChangeOutputPath;

        /// <summary>
        /// Invoked when the user clicks the setup button to provide an API key/credentials.
        /// </summary>
        event EventHandler SetupClicked;

        /// <summary>
        /// Invoked when the user wants to stop a job.
        /// </summary>
        event AsyncEventHandler StopJobs;

        /// <summary>
        /// Invoked when the user wants to delete a job.
        /// </summary>
        event AsyncEventHandler DeleteJobs;

        /// <summary>
        /// Invoked when the user wants to download the results of a job.
        /// </summary>
        event AsyncEventHandler DownloadJobs;

        /// <summary>
        /// Get the IDs of all currently selected jobs.
        /// </summary>
        string[] GetSelectedJobIDs();

        /// <summary>
        /// Populate the view with data.
        /// </summary>
        void UpdateJobTable(List<JobDetails> jobs);

        /// <summary>
        /// Makes the download progress bar invisible.
        /// </summary>
        void HideDownloadProgressBar();

        /// <summary>
        /// Makes the download progress bar visible.
        /// </summary>
        void ShowDownloadProgressBar();

        /// <summary>
        /// Makes the job load progress bar invisible.
        /// </summary>
        void HideLoadingProgressBar();

        /// <summary>
        /// Makes the job load progress bar visible.
        /// </summary>
        void ShowLoadingProgressBar();

        /// <summary>
        /// Close the view.
        /// </summary>
        void Destroy();

        /// <summary>
        /// Gets or sets the value of the job load progress bar.
        /// </summary>
        double JobLoadProgress { get; set; }

        /// <summary>
        /// Gets or sets the value of the download progress bar.
        /// </summary>
        double DownloadProgress { get; set; }

        /// <summary>
        /// Output directory as specified by user.
        /// </summary>
        string DownloadPath { get; set; }

        /// <summary>
        /// Should results be extracted?
        /// </summary>
        bool ExtractResults { get; }

        /// <summary>
        /// Should results be exported to .csv format?
        /// </summary>
        bool ExportCsv { get; }

        /// <summary>
        /// Should debug files be downloaded?
        /// </summary>
        bool DownloadDebugFiles { get; }
    }
}
