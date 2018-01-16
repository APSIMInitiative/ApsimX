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
using UserInterface.Views;
using System.Net;

namespace UserInterface.Presenters
{
    public class AzureJobDisplayPresenter : IPresenter, ICloudJobPresenter
    {
        /// <summary>
        /// List of jobs which are currently being downloaded.        
        /// </summary>
        private List<Guid> currentlyDownloading;
        
        private AzureJobDisplayView view;
        public MainPresenter MainPresenter { get; set; }

        private StorageCredentials storageCredentials;
        private BatchCredentials batchCredentials;
        private CloudStorageAccount storageAccount;
        private PoolSettings poolSettings;
        private BatchClient batchClient;
        private CloudBlobClient blobClient;

        private BackgroundWorker FetchJobs;
        //private Timer updateJobsTimer;

        /// <summary>
        /// Mutual exclusion semaphore controlling access to the section of code relating to the log file.        
        /// </summary>
        private Object logFileMutex;

        /// <summary>
        /// List of all Azure jobs.
        /// </summary>
        private List<JobDetails> jobList;

        public AzureJobDisplayPresenter(MainPresenter mainPresenter)
        {
            MainPresenter = mainPresenter;
            jobList = new List<JobDetails>();
            logFileMutex = new object();            
            currentlyDownloading = new List<Guid>();

            FetchJobs = new BackgroundWorker();
            FetchJobs.WorkerSupportsCancellation = true;
            FetchJobs.DoWork += FetchJobs_DoWork;
        }

        /// <summary>
        /// Attach the view to this presenter.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="view"></param>
        /// <param name="explorerPresenter"></param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.view = (AzureJobDisplayView)view;
            this.view.Presenter = this;

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
            
            // start downloading the list of jobs immediately
            FetchJobs.RunWorkerAsync();
        }


        public void Detach()
        {
            FetchJobs.CancelAsync();
            view.RemoveEventHandlers();
            view.MainWidget.Destroy();
        }

        private void TimerElapsed(object sender, EventArgs e)
        {
            if (!FetchJobs.IsBusy) FetchJobs.RunWorkerAsync();
        }

        private void FetchJobs_DoWork(object sender, DoWorkEventArgs args)
        {
            while (!FetchJobs.CancellationPending) // this check is performed regularly inside the ListJobs() function as well.
            {
                // TODO : find a way to detect when this tab has been closed. If this occurs, this thread needs to stop                

                // update the list of jobs. this will take a bit of time                
                var newJobs = ListJobs();
                
                if (FetchJobs.CancellationPending) return;
                if (newJobs != null)
                {
                    // if the new job list is different, update the tree view
                    if (newJobs.Count() != jobList.Count())
                    {                        
                        jobList = newJobs;
                        if (UpdateDisplay() == 1) return;
                        
                        
                    }
                    for (int i = 0; i < newJobs.Count(); i++)
                    {
                        if (!IsEqual(newJobs[i], jobList[i]))
                        {                            
                            jobList = newJobs;
                            if (UpdateDisplay() == 1) return;
                            break;
                        }
                    }
                    jobList = newJobs;
                } else
                {                    
                    // ListJobs() will only return null if the thread is asked to cancel or if unable to talk to
                    // the view (due to a null ref.)
                    return;
                }
                // refresh every 10 seconds
                System.Threading.Thread.Sleep(10000);
            }            
        }

        /// <summary>
        /// Gets the formatted display name of a job.
        /// </summary>
        /// <param name="id">ID of the job.</param>
        /// <param name="withOwner">If true, the return value will include the job owner's name in brackets.</param>
        /// <returns></returns>
        public string GetJobName(string id, bool withOwner)
        {
            JobDetails job = GetLocalJob(id);
            return withOwner ? job.DisplayName + " (" + job.Owner + ")" : job.DisplayName;
        }

        /// <summary>
        /// Asks the view to update the tree view.
        /// </summary>
        /// <returns>0 if the operation is successful, 1 if a NullRefEx. occurs, 2 if another exception is generated.</returns>
        private int UpdateDisplay()
        {
            try
            {
                view.UpdateTreeView(jobList);
            }
            catch (NullReferenceException)
            {                
                return 1;
            }
            catch (Exception e)
            {
                ShowError(e.ToString());
                return 2;
            }
            return 0;
        }

        /// <summary>
        /// Gets the list of jobs submitted to Azure.
        /// </summary>
        /// <returns>List of Jobs. Null if the thread is asked to cancel, or if unable to update the progress bar.</returns>
        private List<JobDetails> ListJobs()
        {
            try
            {
                view.ShowLoadingProgressBar();
                view.UpdateJobLoadStatus(0);
            } catch (NullReferenceException)
            {
                return null;
            } catch (Exception e)
            {
                ShowError(e.ToString());
            }
            
            List<JobDetails> jobs = new List<JobDetails>();
            var pools = batchClient.PoolOperations.ListPools();
            var jobDetailLevel = new ODATADetailLevel { SelectClause = "id,displayName,state,executionInfo,stats", ExpandClause = "stats" };            
            var cloudJobs = batchClient.JobOperations.ListJobs(jobDetailLevel);            
            var length = cloudJobs.Count();
            int i = 0;                        

            foreach (var cloudJob in cloudJobs)
            {
                if (FetchJobs.CancellationPending)
                {
                    return null;
                }

                try
                {                        
                    view.UpdateJobLoadStatus(Math.Min(100.0 * i / length, 100));
                } catch (NullReferenceException)
                {
                    return null;
                } catch (Exception e)
                {
                    ShowError(e.ToString());
                }

                          
                string owner = GetAzureMetaData("job-" + cloudJob.Id, "Owner");

                //var tasks = ListTasks(Guid.Parse(cloudJob.Id));
                // for some reason the succeeded task count is always exactly double the actual number of tasks
                // and the number of tasks is the number of sims + 1 (the job manager?)

                /*
                long taskCount = batchClient.JobOperations.GetTaskCount();
                long numSims = taskCount.;
                long numCompleteSims = (cloudJob.Statistics.SucceededTaskCount + cloudJob.Statistics.FailedTaskCount);
                */
                //long numCompletedTasks = cloudJob.Statistics.SucceededTaskCount + cloudJob.Statistics.FailedTaskCount;
                //double progress = cloudJob.State.ToString() == "Completed" ? 100.0 : 50.0;

                // move the comment to be around 100 if you want to see the accurate task count

                long numCompletedTasks = (cloudJob.Statistics.FailedTaskCount + cloudJob.Statistics.SucceededTaskCount) / 2 - 1;
                double jobProgress = cloudJob.State.ToString().ToLower() == "completed" ? 100 : 0;
                var job = new JobDetails
                {
                    Id = cloudJob.Id,
                    DisplayName = cloudJob.DisplayName,
                    State = cloudJob.State.ToString(),
                    Owner = owner,
                    NumSims = numCompletedTasks,
                    Progress = jobProgress,
                    CpuTime = cloudJob.Statistics.KernelCpuTime + cloudJob.Statistics.UserCpuTime
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
            }
            view.HideLoadingProgressBar();
            if (jobs == null) return new List<JobDetails>();            
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
            return 100.0 * tasksComplete / tasks.Length;            
        }

        private long CountCompleteTasks(Guid jobId)
        {
            ODATADetailLevel detailLevel = new ODATADetailLevel { SelectClause = "state" };
            var taskList = batchClient.JobOperations.ListTasks(jobId.ToString(), detailLevel).ToArray();
            int completeTasks = 0;
            for (int i = 0; i < taskList.Length; i++)
            {
                if (taskList[i].State.ToString() == "Complete") completeTasks++;
            }
            return completeTasks;
        }

        /// <summary>
        /// Tests if two jobs are equal.
        /// </summary>
        /// <param name="a">The first job.</param>
        /// <param name="b">The second job.</param>
        /// <returns>True if the jobs have the same ID and they are in the same state.</returns>
        private bool IsEqual(JobDetails a, JobDetails b)
        {
            return (a.Id == b.Id && a.State == b.State);
        }

        /// <summary>
        /// Gets the Azure tasks in a job.
        /// </summary>
        /// <param name="jobId">ID of the job.</param>
        /// <returns>List of TaskDetail objects.</returns>
        private TaskDetails[] ListTasks(Guid jobId)
        {
            ODATADetailLevel detailLevel = new ODATADetailLevel { SelectClause = "id,displayName,state,executionInfo" };
            var taskList = batchClient.JobOperations.ListTasks(jobId.ToString(), detailLevel).ToArray();

            /*
            foreach (var cloudTask in )
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
            */

            int n = taskList.Count();
            TaskDetails[] tasks = new TaskDetails[n];
            for (int i = 0; i < n; i++) {
                CloudTask task = taskList[i];                
                TaskDetails test = new TaskDetails
                {
                    Id = task.Id,
                    DisplayName = task.DisplayName,
                    State = task.State.ToString(),
                    StartTime = task.ExecutionInformation == null ? null : taskList.ElementAt(i).ExecutionInformation.StartTime,
                    EndTime = task.ExecutionInformation == null ? null : taskList.ElementAt(i).ExecutionInformation.EndTime
                };
                tasks[i] = test;
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

        /// <summary>
        /// Gets a job with a given ID.
        /// </summary>
        /// <param name="id">ID of the job.</param>
        /// <returns>A job from the local job list. The job is not live but should have been recently updated.</returns>
        public JobDetails GetLocalJob(string id)
        {
            return jobList.FirstOrDefault(x => x.Id == id);
        }

        public void UpdateDownloadProgress(double progress, string jobName)
        {
            view.UpdateDownloadProgress(progress, jobName);
        }

        public bool OngoingDownload()
        {
            return currentlyDownloading.Count() > 0;
        }

        /// <summary>
        /// Downloads the results of a list of jobs.
        /// </summary>
        /// <param name="jobsToDownload">List of IDs of the jobs.</param>
        /// <param name="saveToCsv">If true, results will be combined into a csv file.</param>
        /// <param name="includeDebugFiles">If true, debug files will be downloaded.</param>
        /// <param name="keepOutputFiles">If true, the raw .db output files will be saved.</param>
        public void DownloadResults(List<string> jobsToDownload, bool saveToCsv, bool includeDebugFiles, bool keepOutputFiles)
        {
            // TODO : make jobs download serially
            if (OngoingDownload())
            {
                ShowError("Unable to start a new batch of downloads - one or more downloads are already ongoing.");
                return;
            }

            if (Directory.GetFiles((string)Settings.Default["OutputDir"]).Length > 0 && !view.ShowWarning("Files detected in output directory. Results will be generated from ALL files in this directory. Are you certain you wish to continue?"))
                return;
            view.ShowDownloadProgressBar();            
            string path = (string)Settings.Default["OutputDir"];
            AzureResultsDownloader dl;
            Guid jobId;
            foreach (string id in jobsToDownload)
            {                
                // if the job id is invalid, skip downloading this job                
                if (!Guid.TryParse(id, out jobId)) continue;
                view.UpdateDownloadProgress(0, GetJob(jobId).DisplayName);                
                dl = new AzureResultsDownloader(jobId, GetJob(jobId).DisplayName, path, this, saveToCsv, includeDebugFiles, keepOutputFiles);                
                dl.DownloadResults();
            }            
        }

        public void SetupCredentials()
        {
            AzureCredentialsSetup setup = new AzureCredentialsSetup();
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
        /// Writes to a log file and asks the view to display an error message if download was unsuccessful.
        /// </summary>
        /// <param name="code"></param>
        public void DisplayFinishedDownloadStatus(string name, int code, string path, DateTime timeStamp)
        {
            view.HideDownloadProgressBar();
            if (code == 0) return;
            string msg = timeStamp.ToLongTimeString().Split(' ')[0] + ": " +  name + ": ";
            switch (code)
            {
                case 1:
                    msg += "Unable to generate a .csv file.";
                    break;
                default:
                    msg += "Download unsuccessful.";
                    break;
            }
            string logFile = path + "\\download.log";
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
        /// Halts the execution of a job. 
        /// </summary>
        /// <param name="id">ID of the job.</param>
        public void StopJob(Guid id)
        {
            try
            {
                batchClient.JobOperations.TerminateJob(id.ToString());
            } catch (Exception e)
            {
                MainPresenter.ShowMessage(e.Message, Simulation.ErrorLevel.Error);
            }
        }

        /// <summary>
        /// Deletes a job (and all associated files (I think)) from Azure cloud storage.
        /// </summary>
        /// <param name="id">ID of the job.</param>
        public void DeleteJob(string id)
        {
            // cancel the fetch jobs worker
            FetchJobs.CancelAsync();
            view.HideLoadingProgressBar();

            // try to parse the id. if it is not a valid Guid, return
            Guid jobId;
            if (!Guid.TryParse(id, out jobId)) return;

            // delete the job from Azure
            CloudBlobContainer containerRef;

            containerRef = blobClient.GetContainerReference(StorageConstants.GetJobOutputContainer(jobId));
            if (containerRef.Exists()) containerRef.Delete();
            
            containerRef = blobClient.GetContainerReference(StorageConstants.GetJobOutputContainer(jobId));
            if (containerRef.Exists()) containerRef.Delete();

            containerRef = blobClient.GetContainerReference(jobId.ToString());
            if (containerRef.Exists()) containerRef.Delete();

            var job = GetJob(jobId);
            if (job != null) batchClient.JobOperations.DeleteJob(id);

            // remove the job from the locally stored list of jobs
            jobList.RemoveAt(jobList.IndexOf(GetLocalJob(id)));            
            
            // refresh the tree view
            view.UpdateTreeView(jobList);

            // restart the fetch jobs worker
            FetchJobs.RunWorkerAsync();
        }
    }
}
