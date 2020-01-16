using System;
using System.Collections.Generic;
using System.Linq;
using ApsimNG.Cloud;
using UserInterface.Interfaces;
using System.IO;
using System.ComponentModel;
using UserInterface.Views;
using System.Threading.Tasks;
using System.Threading;
using ApsimNG.Interfaces;
using Models.Core;

namespace UserInterface.Presenters
{
    public class CloudJobPresenter : IPresenter
    {
        /// <summary>
        /// The view displaying the list of cloud jobs.
        /// </summary>
        private ICloudJobView view;

        /// <summary>
        /// This object will handle the cloud platform-specific tasks
        /// such as job enumeration, termination, etc.
        /// </summary>
        private ICloudInterface cloudInterface;

        /// <summary>
        /// This worker repeatedly fetches information about all Azure jobs on the batch account.
        /// </summary>
        private BackgroundWorker fetchJobs;

        /// <summary>
        /// List of all Azure jobs.
        /// </summary>
        private List<JobDetails> jobList;

        /// <summary>
        /// The parent presenter.
        /// </summary>
        private MainPresenter presenter;

        /// <summary>
        /// Used to distribute cancellation tokens to async processes.
        /// These cancellation tokens are never cancelled (for now).
        /// </summary>
        private CancellationTokenSource cancelToken;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="primaryPresenter"></param>
        public CloudJobPresenter(MainPresenter primaryPresenter)
        {
            cloudInterface = new AzureInterface();
            cancelToken = new CancellationTokenSource();

            presenter = primaryPresenter;
            jobList = new List<JobDetails>();

            fetchJobs = new BackgroundWorker()
            {
                WorkerSupportsCancellation = true
            };
            fetchJobs.DoWork += ListJobs;
        }

        /// <summary>
        /// Attach the view to this presenter.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="view"></param>
        /// <param name="explorerPresenter"></param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.view = (CloudJobView)view;

            // fixme
            this.view.DownloadPath = ApsimNG.Cloud.Azure.AzureSettings.Default.OutputDir;

            this.view.ChangeOutputPath += OnChangeOutputPath;
            this.view.SetupClicked += OnSetup;
            this.view.StopJobs += OnStopJobs;
            this.view.DeleteJobs += OnDeleteJobs;
            this.view.DownloadJobs += OnDownloadJobs;

            fetchJobs.RunWorkerAsync();
        }

        /// <summary>
        /// Detach the view from this presenter.
        /// </summary>
        public void Detach()
        {
            view.ChangeOutputPath -= OnChangeOutputPath;
            view.SetupClicked -= OnSetup;
            view.StopJobs -= OnStopJobs;
            view.DeleteJobs -= OnDeleteJobs;
            view.DownloadJobs -= OnDownloadJobs;

            cancelToken.Cancel();
            fetchJobs.CancelAsync();

            view.Destroy();
        }

        /// <summary>
        /// Called when the user wants to halt the execution of a job.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private async Task OnStopJobs(object sender, EventArgs args)
        {
            string[] jobIDs = view.GetSelectedJobIDs();
            if (jobIDs.Length < 1)
                throw new Exception("Unable to stop jobs: no jobs are selected");

            // Get the grammar right when asking for confirmation.
            string msg = "Are you sure you want to stop " + (jobIDs.Length > 1 ? "these " + jobIDs.Length + " jobs" : "this job") + "? There is no way to resume execution!";
            if (presenter.AskQuestion(msg) != QuestionResponseEnum.Yes)
                return;

            foreach (string id in jobIDs)
                await cloudInterface.StopJobAsync(id, cancelToken.Token);
        }

        /// <summary>
        /// Called when the user wants to delete a job.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private async Task OnDeleteJobs(object sender, EventArgs args)
        {
            string[] jobIDs = view.GetSelectedJobIDs();
            if (jobIDs.Length < 1)
                throw new Exception("Unable to delete jobs: no jobs are selected");

            // Get the grammar right when asking for confirmation.
            string msg = "Are you sure you want to delete " + (jobIDs.Length > 1 ? "these " + jobIDs.Length + " jobs?" : "this job?");
            if (presenter.AskQuestion(msg) != QuestionResponseEnum.Yes)
                return;

            foreach (string id in jobIDs)
            {
                await cloudInterface.DeleteJobAsync(id, cancelToken.Token);

                // Remove the job from the locally stored list of jobs.
                jobList.RemoveAll(j => j.Id == id);
            }

            // Refresh the view.
            view.UpdateJobTable(jobList);
        }

        /// <summary>
        /// Called when the user wants to download results of a job.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        /// <returns></returns>
        private async Task OnDownloadJobs(object sender, EventArgs args)
        {
            DownloadOptions options = new DownloadOptions();
            
            options.DownloadDebugFiles = view.DownloadDebugFiles;
            options.ExportToCsv = view.ExportCsv;
            options.ExtractResults = view.ExtractResults;

            string basePath = view.DownloadPath;

            view.DownloadProgress = 0;
            view.ShowDownloadProgressBar();

            try
            {
                foreach (string id in view.GetSelectedJobIDs())
                {
                    JobDetails job = jobList.Find(j => j.Id == id);

                    options.Path = Path.Combine(basePath, job.DisplayName + "_results");
                    options.JobID = Guid.Parse(id);

                    await cloudInterface.DownloadResultsAsync(options, cancelToken.Token, p => view.DownloadProgress = p);

                    if (cancelToken.IsCancellationRequested)
                        return;
                }
                presenter.ShowMessage($"Results were successfully downloaded to {options.Path}", Simulation.MessageType.Information);
            }
            finally
            {
                view.HideDownloadProgressBar();
                view.DownloadProgress = 0;
            }
        }

        /// <summary>
        /// Called from a background worker thread. Updates the list of cloud jobs
        /// by interrogating the cloud platform.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private async void ListJobs(object sender, DoWorkEventArgs args)
        {
            try
            {
                while (!fetchJobs.CancellationPending) // fixme
                {
                    // Update the list of jobs. This will take some time.
                    view.JobLoadProgress = 0;
                    view.ShowLoadingProgressBar();
                    var newJobs = await cloudInterface.ListJobsAsync(cancelToken.Token, p => view.JobLoadProgress = p);

                    if (fetchJobs.CancellationPending)
                        return;

                    if (Different(newJobs, jobList))
                        view.UpdateJobTable(newJobs);

                    jobList = newJobs;

                    view.HideLoadingProgressBar();

                    // Refresh job list every 10 seconds
                    Thread.Sleep(10000);
                }
            }
            catch (Exception err)
            {
                presenter.ShowError(err);
            }
            finally
            {
                view?.HideLoadingProgressBar();
            }
        }

        /// <summary>
        /// Called when the user wants to input credentials/API key.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnSetup(object sender, EventArgs e)
        {
            // fixme - should probably move this functionality into ICloudInterface
            new ApsimNG.Cloud.Azure.AzureCredentialsSetup();
        }

        private void OnChangeOutputPath(object sender, EventArgs e)
        {
            // fixme - should probably move this functionality into ICloudInterface
            string path = ViewBase.AskUserForFileName("Choose a folder", Utility.FileDialog.FileActionType.SelectFolder, "");
            ApsimNG.Cloud.Azure.AzureSettings.Default.OutputDir = path;
            ApsimNG.Cloud.Azure.AzureSettings.Default.Save();
            view.DownloadPath = path;
        }

        /// <summary>
        /// Checks if two lists of JobDetails objects are different.
        /// </summary>
        /// <param name="list1">The first list.</param>
        /// <param name="list2">The second list.</param>
        private bool Different(List<JobDetails> list1, List<JobDetails> list2)
        {
            if (list1 == null && list2 == null)
                return false;

            if (list1 == null || list2 == null)
                return true;

            if (list1.Count != list2.Count)
                return true;

            for (int i = 0; i < list1.Count; i++)
                if (!list1[i].IsEqualTo(list2[i]))
                    return true;

            return false;
        }
    }
}
