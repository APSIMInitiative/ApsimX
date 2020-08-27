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
using System.Data;
using UserInterface.Extensions;

namespace UserInterface.Presenters
{
    public sealed class CloudJobPresenter : IPresenter, IDisposable
    {
        private const string timespanFormat = @"dddd\d\ hh\h\ mm\m\ ss\s";
        private const string dateFormat = "yyyy-MM-dd HH.mm";

        /// <summary>The view.</summary>
        private ViewBase view;

        /// <summary>The list view control.</summary>
        private ListView jobListView;

        /// <summary>The refresh button.</summary>
        private ButtonView refreshButton;

        /// <summary>The download button.</summary>
        private ButtonView downloadButton;

        /// <summary>The stop button.</summary>
        private ButtonView stopButton;

        /// <summary>The delete button.</summary>
        private ButtonView deleteButton;

        /// <summary>The credentials button.</summary>
        private ButtonView credentialsButton;

        /// <summary>The show my jobs only checkbox.</summary>
        private CheckBoxView showMyJobsOnlyCheckbox;

        /// <summary>Progress bar.</summary>
        private ProgressBarView progressBar;

        /// <summary>
        /// This object will handle the cloud platform-specific tasks
        /// such as job enumeration, termination, etc.
        /// </summary>
        private ICloudInterface cloudInterface;

        /// <summary>
        /// List of all Azure jobs.
        /// </summary>
        private List<JobDetails> jobList = new List<JobDetails>();

        /// <summary>
        /// Filtered job list that the view works from.
        /// </summary>
        private List<JobDetails> filteredJobList = new List<JobDetails>();

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
        }

        /// <summary>
        /// Attach the view to this presenter.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="viewBase"></param>
        /// <param name="explorerPresenter"></param>
        public void Attach(object model, object viewBase, ExplorerPresenter explorerPresenter)
        {
            view = (ViewBase)viewBase;
            jobListView = view.GetControl<ListView>("jobListView");
            refreshButton = view.GetControl<ButtonView>("refreshButton");
            downloadButton = view.GetControl<ButtonView>("downloadButton");
            stopButton = view.GetControl<ButtonView>("stopButton");
            deleteButton = view.GetControl<ButtonView>("deleteButton");
            credentialsButton = view.GetControl<ButtonView>("credentialsButton");
            showMyJobsOnlyCheckbox = view.GetControl<CheckBoxView>("showMyJobsCheckbox");
            progressBar = view.GetControl<ProgressBarView>("progressBar");

            jobListView.SortColumn = "Start time";
            jobListView.SortAscending = false;

            jobListView.AddColumn("Name");
            jobListView.AddColumn("Owner");
            jobListView.AddColumn("State");
            jobListView.AddColumn("# Sims");
            jobListView.AddColumn("Progress");
            jobListView.AddColumn("Start time");
            jobListView.AddColumn("End time");
            jobListView.AddColumn("Duration");
            jobListView.AddColumn("CPU time");

            refreshButton.Clicked += OnRefreshClicked;
            downloadButton.Clicked += OnDownloadClicked;
            stopButton.Clicked += OnStopClicked;
            deleteButton.Clicked += OnDeleteClicked;
            credentialsButton.Clicked += OnCredentialsClicked;
            showMyJobsOnlyCheckbox.Changed += OnShowMyJobsChanged;
        }

        /// <summary>
        /// Detach the view from this presenter.
        /// </summary>
        public void Detach()
        {
            cancelToken.Cancel();

            refreshButton.Clicked -= OnRefreshClicked;
            downloadButton.Clicked -= OnDownloadClicked;
            stopButton.Clicked -= OnStopClicked;
            deleteButton.Clicked -= OnDeleteClicked;
            credentialsButton.Clicked -= OnCredentialsClicked;
            showMyJobsOnlyCheckbox.Changed -= OnShowMyJobsChanged;

            view.MainWidget.Cleanup();
        }

        /// <summary>Dispose of object.</summary>
        public void Dispose()
        {
            cancelToken.Dispose();
        }

        /// <summary>
        /// Refresh button has been clicked.
        /// </summary>
        /// <param name="sender">Sender of event.</param>
        /// <param name="e">Event arguments.</param>
        private void OnRefreshClicked(object sender, EventArgs e)
        {
            refreshButton.IsEnabled = false;
            jobList.Clear();
            jobListView.ClearRows();

            Task.Run(() => cloudInterface.ListJobsAsync(cancelToken.Token,
                                                        p => UpdateProgressBar(p),
                                                        job => AddJobToView(job)));
        }
                
        /// <summary>
        /// User has clicked download.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private async void OnDownloadClicked(object sender, EventArgs e)
        {
            // Ask user for download path.
            string path = ViewBase.AskUserForFileName("Choose a download folder",
                                                      Utility.FileDialog.FileActionType.SelectFolder,
                                                      "",
                                                      ApsimNG.Cloud.Azure.AzureSettings.Default.OutputDir);
            if (!string.IsNullOrEmpty(path))
            {
                ApsimNG.Cloud.Azure.AzureSettings.Default.OutputDir = path;
                ApsimNG.Cloud.Azure.AzureSettings.Default.Save();

                presenter.ShowWaitCursor(true);

                try
                {
                    foreach (int listViewIndex in jobListView.SelectedIndicies)
                    {
                        var jobListIndex = ConvertListViewIndexToJobIndex(listViewIndex);

                        DownloadOptions options = new DownloadOptions()
                        {
                            Name = jobList[jobListIndex].DisplayName,
                            Path = ApsimNG.Cloud.Azure.AzureSettings.Default.OutputDir,
                            JobID = Guid.Parse(jobList[jobListIndex].Id)
                        };

                        await cloudInterface.DownloadResultsAsync(options, cancelToken.Token, p => { });

                        if (cancelToken.IsCancellationRequested)
                            return;
                    }
                    presenter.ShowMessage($"Results were successfully downloaded to {ApsimNG.Cloud.Azure.AzureSettings.Default.OutputDir}", Simulation.MessageType.Information);
                }
                catch (Exception err)
                {
                    presenter.ShowError(err);
                }
                finally
                {
                    presenter.ShowWaitCursor(false);
                }
            }
        }

        /// <summary>
        /// User has clicked the stop button.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event argument.</param>
        private async void OnStopClicked(object sender, EventArgs e)
        {
            int numJobs = jobListView.SelectedIndicies.Length;
            if (numJobs < 1)
                throw new Exception("Unable to stop jobs: no jobs are selected");

            // Get the grammar right when asking for confirmation.
            string msg = "Are you sure you want to stop " + (numJobs > 1 ? "these " + numJobs + " jobs" : "this job") + "? There is no way to resume execution!";
            if (presenter.AskQuestion(msg) == QuestionResponseEnum.Yes)
            {
                foreach (int listViewIndex in jobListView.SelectedIndicies)
                {
                    var jobListIndex = ConvertListViewIndexToJobIndex(listViewIndex);
                    await cloudInterface.StopJobAsync(jobList[jobListIndex].Id, cancelToken.Token);
                }
            }
        }

        /// <summary>
        /// User has clicked the delete button.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event argument.</param>
        private async void OnDeleteClicked(object sender, EventArgs e)
        {
            int numJobs = jobListView.SelectedIndicies.Length;
            if (numJobs < 1)
                throw new Exception("Unable to delete jobs: no jobs are selected");

            // Get the grammar right when asking for confirmation.
            string msg = "Are you sure you want to delete " + (numJobs > 1 ? "these " + numJobs + " jobs?" : "this job?");
            if (presenter.AskQuestion(msg) == QuestionResponseEnum.Yes)
            {
                var selectedIndicies = jobListView.SelectedIndicies;

                // Delete the jobs from 'bottom up' so that the indicies remain valid.
                Array.Sort(selectedIndicies);
                Array.Reverse(selectedIndicies);
                foreach (int listViewIndex in selectedIndicies)
                {
                    var jobListIndex = ConvertListViewIndexToJobIndex(listViewIndex);
                    await cloudInterface.DeleteJobAsync(jobList[jobListIndex].Id, cancelToken.Token);

                    // Remove the job from the locally stored list of jobs.
                    jobList.RemoveAll(j => j.Id == jobList[jobListIndex].Id);

                    // Update the view.
                    jobListView.RemoveRow(listViewIndex);
                }
            }
        }

        /// <summary>
        /// User has clicked the credentials button.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnCredentialsClicked(object sender, EventArgs e)
        {
            // fixme - should probably move this functionality into ICloudInterface
            using (var dialog = new ApsimNG.Cloud.Azure.AzureCredentialsSetup())
            {
            }
        }

        /// <summary>
        /// User has clicked show my jobs only.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnShowMyJobsChanged(object sender, EventArgs e)
        {
            jobListView.ClearRows();
            filteredJobList.Clear();
            foreach (var job in jobList)
                AddRowToJobListView(job);
        }

        /// <summary>
        /// Update view's progress bar.
        /// </summary>
        /// <param name="progress">The progress 0-100.</param>
        private void UpdateProgressBar(double progress)
        {
            view.InvokeOnMainThread(delegate { progressBar.Position = progress; });
            if (progress == 100)
            {
                refreshButton.IsEnabled = true;
                view.InvokeOnMainThread(delegate { progressBar.Visible = false; });
            }
        }

        /// <summary>
        /// Update the list of jobs shown to user.
        /// </summary>
        /// <param name="job">The job to added to the view.</param>
        private void AddJobToView(JobDetails job)
        {
            if (job != null)
            {
                // Because this method is called on a worker thead we need to update the 
                // view on the main thread.
                view.InvokeOnMainThread(delegate
                {
                    jobList.Add(job);
                    AddRowToJobListView(job);
                });
            }
        }

        /// <summary>Add a row to the ListView.</summary>
        /// <param name="job">The job to use to populate the row.</param>
        private void AddRowToJobListView(JobDetails job)
        {
            if (!showMyJobsOnlyCheckbox.Checked ||
                       string.Equals(job.Owner, Environment.UserName, StringComparison.InvariantCultureIgnoreCase))
            {
                filteredJobList.Add(job);
                string startTime = null;
                string endTime = null;
                if (job.StartTime != null)
                    startTime = ((DateTime)job.StartTime).ToLocalTime().ToString(dateFormat);
                if (job.EndTime != null)
                    endTime = ((DateTime)job.EndTime).ToLocalTime().ToString(dateFormat);

                jobListView.AddRow(new object[]
                {
                    job.DisplayName,
                    job.Owner,
                    job.State,
                    job.NumSims.ToString(),
                    job.Progress.ToString("F0"),
                    startTime,
                    endTime,
                    job.Duration().ToString(timespanFormat),
                    job.CpuTime.ToString(timespanFormat)
                });
            }
        }

        /// <summary>
        /// Convert an ListView row index to an index into our JobList.
        /// </summary>
        /// <param name="listViewRowIndex">ListView row index</param>
        /// <returns></returns>
        private int ConvertListViewIndexToJobIndex(int listViewRowIndex)
        {
            var values = jobListView.GetRow(listViewRowIndex);
            return jobList.FindIndex(job => job.DisplayName == (string)values[0] &&
                                            job.Owner == (string)values[1] &&
                                            job.State == (string)values[2] &&
                                            job.NumSims.ToString() == (string)values[3] &&
                                            ((DateTime)job.StartTime).ToLocalTime().ToString(dateFormat) == (string)values[5] &&
                                            ((DateTime)job.EndTime).ToLocalTime().ToString(dateFormat) == (string)values[6]);
        }
    }
}
