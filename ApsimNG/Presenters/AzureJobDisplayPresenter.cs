using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ApsimNG.Cloud;
using Microsoft.WindowsAzure.Storage;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Common;
using UserInterface.Interfaces;
using System.IO;
using ApsimNG.Properties;
using System.ComponentModel;
using System.Timers;
using Models.Core.Runners;
using Models.Core;
using APSIM.Shared.Utilities;
using Microsoft.WindowsAzure.Storage.Blob;

namespace UserInterface.Presenters
{
    public class AzureJobDisplayPresenter : ExplorerPresenter
    {
        /// <summary>
        /// List of jobs which are currently being downloaded.        
        /// </summary>
        private List<Guid> currentlyDownloading;

        private MainPresenter mainPresenter;
        private IAzureJobDisplayView view;
        public MainPresenter MainPresenter { get; set; }

        /// <summary>
        /// Model associated with the context of the right click. Unused. 
        /// Remove this when this presenter is accessed directly from the home tab.
        /// </summary>
        private Models.Core.IModel model;

        /// <summary>
        /// 
        /// </summary>        
        private StorageCredentials storageCredentials;
        private BatchCredentials batchCredentials;
        private CloudStorageAccount storageAccount;
        private PoolSettings poolSettings;
        private BatchClient batchClient;
        private CloudBlobClient blobClient;

        private BackgroundWorker FetchJobs;
        //private Timer updateJobsTimer;
        
        private AzureResultsDownloader downloader;

        /// <summary>
        /// Semaphore controlling access to the section of code relating to the log file.        
        /// </summary>
        private Object logFileMutex;
        
        public AzureJobDisplayPresenter(MainPresenter mainPresenter) : base(mainPresenter)
        {
            MainPresenter = mainPresenter;
        }

        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {

        }

        public void Attach(object model, object view, MainPresenter main)
        {
            MainPresenter = main;
            this.view = (IAzureJobDisplayView)view;
            this.view.Presenter = this;
            //this.model = (IModel)model;
            currentlyDownloading = new List<Guid>();
            logFileMutex = new object();
            // read Azure credentials from a file. If credentials file doesn't exist, abort.
            string credentialsFileName = (string)Settings.Default["AzureLicenceFilePath"];

            // if the file name in Properties.Settings doesn't exist then prompt user for a new one
            if (credentialsFileName == "" || !File.Exists(credentialsFileName))
            {
                credentialsFileName = this.view.GetFile(new List<string> { "lic" }, "Azure Licence file");
            }
            if (SetCredentials(credentialsFileName))
            {
                // licence file is valid, remember this file for next time
                Settings.Default["AzureLicenceFilePath"] = credentialsFileName;
                Settings.Default.Save();
            }
            else
            {
                // licence file is invalid or non-existent. Show an error and remove the job submission form from the right hand panel.
                ShowError("Missing or invalid Azure Licence file: " + credentialsFileName);
                // TODO : find an equivalent of this (kill the tab)
                //explorerPresenter.HideRightHandPanel();
                return;
            }

            storageCredentials = StorageCredentials.FromConfiguration();
            batchCredentials = BatchCredentials.FromConfiguration();
            poolSettings = PoolSettings.FromConfiguration();            

            storageAccount = new CloudStorageAccount(new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials(storageCredentials.Account, storageCredentials.Key), true);
            var sharedCredentials = new Microsoft.Azure.Batch.Auth.BatchSharedKeyCredentials(batchCredentials.Url, batchCredentials.Account, batchCredentials.Key);
            batchClient = BatchClient.Open(sharedCredentials);
            blobClient = storageAccount.CreateCloudBlobClient();
            blobClient.DefaultRequestOptions.RetryPolicy = new Microsoft.WindowsAzure.Storage.RetryPolicies.LinearRetry(TimeSpan.FromSeconds(3), 10);
            
            FetchJobs = new BackgroundWorker();
            FetchJobs.DoWork += FetchJobs_DoWork;            
            // start downloading the list of jobs immediately
            if (FetchJobs.IsBusy)
            {
                FetchJobs.CancelAsync();
            }
            FetchJobs.RunWorkerAsync();

            /*
            // old timer-based code

            // timer to automatically update the list of jobs every 30 seconds
            updateJobsTimer = new Timer(30000);
            updateJobsTimer.Elapsed += TimerElapsed;
            updateJobsTimer.AutoReset = true;
            updateJobsTimer.Start();
            */
        }

        public void Detach()
        {
            Console.WriteLine("Closing cloud job viewer...");
        }

        private void TimerElapsed(object sender, EventArgs e)
        {
            if (!FetchJobs.IsBusy) FetchJobs.RunWorkerAsync();
        }

        private void FetchJobs_DoWork(object sender, DoWorkEventArgs args)
        {
            while (!FetchJobs.CancellationPending) // this check is performed regularly inside the ListJobs() function as well.
            {
                var jobs = ListJobs();
                if (jobs == null) return;
                try
                {
                    view.AddJobsToTableIfNecessary(jobs);
                }
                catch
                {
                    // the most likely error here occurs if the user has moved to a different right hand panel at an unfortunate time
                    // in which case this thread can no longer access the original view (or rather, the view is no longer attached to the right hand panel)
                }                
                System.Threading.Thread.Sleep(10000);
            }
            
        }


        /// <summary>
        /// Gets the list of jobs submitted to Azure.
        /// </summary>
        /// <returns>List of Jobs</returns>
        private List<JobDetails> ListJobs()
        {
            List<JobDetails> jobs = new List<JobDetails>();
            var pools = batchClient.PoolOperations.ListPools();
            var jobDetailLevel = new ODATADetailLevel { SelectClause = "id,displayName,state,executionInfo,stats", ExpandClause = "stats" };
            var cloudJobs = batchClient.JobOperations.ListJobs(jobDetailLevel);
            var length = cloudJobs.Count();
            int i = 1;            
            var updateProgressMutex = new object();

            foreach (var cloudJob in cloudJobs)
            {
                if (FetchJobs.CancellationPending)
                {
                    return null;
                }

                
                string owner = GetAzureMetaData("job-" + cloudJob.Id, "Owner");
                //double jobProgress = GetProgress(Guid.Parse(cloudJob.Id));
                double jobProgress = -1;
                var job = new JobDetails
                {
                    Id = cloudJob.Id,
                    DisplayName = cloudJob.DisplayName,
                    State = cloudJob.State.ToString(),
                    Owner = owner,
                    Progress = jobProgress
                };

                if (cloudJob.ExecutionInformation != null)
                {
                    job.StartTime = cloudJob.ExecutionInformation.StartTime;
                    job.EndTime = cloudJob.ExecutionInformation.EndTime;

                    if (cloudJob.ExecutionInformation.PoolId != null)
                    {
                        var pool = pools.FirstOrDefault(p => string.Equals(cloudJob.ExecutionInformation.PoolId, p.Id));

                        if (pool != null)
                        {
                            job.PoolSettings = new PoolSettings
                            {
                                MaxTasksPerVM = pool.MaxTasksPerComputeNode.GetValueOrDefault(1),
                                State = pool.AllocationState.GetValueOrDefault(AllocationState.Resizing).ToString(),
                                VMCount = pool.CurrentDedicated.GetValueOrDefault(0),
                                VMSize = pool.VirtualMachineSize
                            };
                        }
                    }
                }                
                jobs.Add(job);
                i++;

                
                lock(updateProgressMutex)
                {
                    view.UpdateJobLoadStatus(100.0 * i / length);
                }                
            }            
            return jobs;
        }

        /// <summary>
        /// Gets a value of particular metadata associated with a job.
        /// </summary>
        /// <param name="containerName">Container the job is stored in.</param>
        /// <param name="key">Metadata key (e.g. owner).</param>
        /// <returns></returns>
        private string GetAzureMetaData(string containerName, string key)
        {
            try
            {                
                var containerRef = storageAccount.CreateCloudBlobClient().GetContainerReference(containerName);
                if (containerRef.Exists())
                {
                    containerRef.FetchAttributes();
                    if (containerRef.Metadata.ContainsKey(key))
                    {
                        return containerRef.Metadata[key];
                    }
                }
            }
            catch (Exception e)
            {                
                MainPresenter.ShowMessage(e.ToString(), Simulation.ErrorLevel.Error);
            }
            return "";
        }

        /// <summary>
        /// Read Azure credentials from the file ApsimX\AzureAgR.lic
        /// This is a temporary measure - will probably need to allow user to specify a file.
        /// </summary>
        /// <returns>True if the credentials file exists, false otherwise.</returns>
        private bool SetCredentials(string path)
        {
            if (File.Exists(path))
            {
                string line;
                StreamReader file = new StreamReader(path);
                while ((line = file.ReadLine()) != null)
                {
                    int separatorIndex = line.IndexOf("=");
                    if (separatorIndex > -1)
                    {
                        string key = line.Substring(0, separatorIndex);
                        string val = line.Substring(separatorIndex + 1);

                        Settings.Default[key] = val;
                    }
                    else
                    {
                        return false;
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }


        /// <summary>
        /// Gets the job progress as a percentage.
        /// </summary>
        /// <param name="jobId">ID of the job.</param>
        /// <returns>Double between 0 and 100.</returns>
        private double GetProgress(Guid jobId)
        {
            int tasksComplete = 0;            
            var tasks = ListTasks(jobId);
            foreach (var task in tasks)
            {
                if (task.State == "Completed")
                {
                    tasksComplete++;
                }
            }
            return 100.0 * tasksComplete / tasks.Count;
        }

        /// <summary>
        /// Gets the Azure tasks in a job.
        /// </summary>
        /// <param name="jobId">ID of the job.</param>
        /// <returns>List of TaskDetail objects.</returns>
        private List<TaskDetails> ListTasks(Guid jobId)
        {
            List<TaskDetails> tasks = new List<TaskDetails>();
            CloudJob job = GetJob(jobId);
            if (job != null)
            {
                ODATADetailLevel detailLevel = new ODATADetailLevel { SelectClause = "id,displayName,state,executionInfo" };
                foreach (var cloudTask in batchClient.JobOperations.ListTasks(jobId.ToString(), detailLevel))
                {
                    tasks.Add(new TaskDetails
                    {
                        Id = cloudTask.Id,
                        DisplayName = cloudTask.DisplayName,
                        State = cloudTask.State.ToString(),
                        StartTime = cloudTask.ExecutionInformation == null ? null : cloudTask.ExecutionInformation.StartTime,
                        EndTime = cloudTask.ExecutionInformation == null ? null : cloudTask.ExecutionInformation.EndTime
                    });
                }
            }
            return tasks;
        }

        /// <summary>
        /// Gets a job stored on Azure with a given ID.
        /// </summary>
        /// <param name="jobId">ID of the job.</param>
        /// <returns>Azure CloudJob object representing the Apsim Job.</returns>
        private CloudJob GetJob(Guid jobId)
        {
            ODATADetailLevel detailLevel = new ODATADetailLevel { SelectClause = "id" };
            CloudJob job = batchClient.JobOperations.ListJobs(detailLevel).FirstOrDefault(j => string.Equals(jobId.ToString(), j.Id));
            if (job == null) return null;
            return batchClient.JobOperations.GetJob(jobId.ToString());
        }

        public bool OngoingDownload()
        {
            return currentlyDownloading.Count() > 0;
        }

        public void DownloadResults(string jobId, string jobName, bool saveToCsv)
        {
            currentlyDownloading.Add(Guid.Parse(jobId));            
            downloader = new AzureResultsDownloader(Guid.Parse(jobId), jobName, (string)Settings.Default["OutputDir"], this, saveToCsv);
            downloader.DownloadResults();
        }

        /// <summary>
        /// Removes a job from the list of currently downloading jobs.
        /// </summary>
        /// <param name="jobId">ID of the job.</param>
        public void DownloadComplete(Guid jobId)
        {
            currentlyDownloading.Remove(jobId);
        }

        /// <summary>
        /// Displays an error message.
        /// </summary>
        /// <param name="msg"></param>
        public void ShowError(string msg)
        {
            MainPresenter.ShowMessage(msg, Simulation.ErrorLevel.Error);            
        }

        /// <summary>
        /// Displays a warning to the user.
        /// </summary>
        /// <param name="msg"></param>
        /// <returns>True if the user wants to continue, false otherwise.</returns>
        public bool ShowWarning(string msg)
        {
            int x = MainPresenter.ShowMsgDialog(msg, "Sanity Check Failed - High-Grade Insanity Detected!", Gtk.MessageType.Warning, Gtk.ButtonsType.OkCancel);
            return x == -5;
        }

        /// <summary>
        /// Writes to a log file and asks the view to display an error message if download was unsuccessful.
        /// </summary>
        /// <param name="code"></param>
        public void DisplayFinishedDownloadStatus(string name, int code, string path)
        {
            if (code == 0) return;
            string msg = name + ": ";
            switch (code)
            {
                case 1:
                    msg += "Unable to generate a .csv file.";
                    break;
                default:
                    msg += "Download unsuccessful.";
                    break;
            }
            string logFile = path + "\\downloadError.log";
            view.UpdateDownloadStatus("One or more downloads encountered an error. See " + logFile + " for more details.");
            lock (logFileMutex)
            {
                try
                {
                    if (!File.Exists(logFile)) File.Create(logFile);
                    using (StreamWriter sw = File.AppendText(logFile))
                    {
                        sw.WriteLine(msg);
                    }
                } catch
                {

                }
            }
            
            
                        
        }

        /// <summary>
        /// Creates a dialog box asking if the user wishes to continue.
        /// </summary>
        /// <param name="msg">Message to be displayed</param>
        /// <param name="title">Title of dialog box</param>
        /// <returns>True if the user wishes to continue. False otherwise.</returns>
        public bool AskToContinue(string msg, string title)
        {
            int x = MainPresenter.ShowMsgDialog(msg, title, Gtk.MessageType.Question, Gtk.ButtonsType.OkCancel);
            return x == -5;
        }

        /// <summary>
        /// Deletes a job (and all associated files (I think)) from Azure cloud storage.
        /// </summary>
        /// <param name="id">id of the job</param>
        public void DeleteJob(Guid id)
        {
            // ask the user if they want to delete the job
            if (AskToContinue("Are you sure you wish to delete this job?", "Delete job confirmation"))
            {
                CloudBlobContainer containerRef;

                // this is done 3 times in MARS - not sure why
                for (int i = 0; i < 2; i++)
                {
                    containerRef = blobClient.GetContainerReference(StorageConstants.GetJobOutputContainer(id));
                    if (containerRef.Exists())
                    {
                        containerRef.Delete();
                    }
                }
                var job = GetJob(id);
                if (job != null) batchClient.JobOperations.DeleteJob(id.ToString());

                view.RemoveJobFromJobList(id);
                view.UpdateTreeView();
            }
        }
    }
}
