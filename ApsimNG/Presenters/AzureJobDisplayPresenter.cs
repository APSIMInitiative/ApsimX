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
            // in theory fetch jobs should never already be busy at this point, but it doesn't hurt to double check
            if (!FetchJobs.IsBusy) FetchJobs.RunWorkerAsync();
        }

        /// <summary>
        /// Asks the user a question, allowing them to choose yes or no. Returns true if they clicked yes, returns false if they clicked no.
        /// </summary>
        /// <param name="msg">Message/question to be displayed.</param>
        /// <returns></returns>
        private bool AskQuestion(string msg)
        {
            return MainPresenter.AskQuestion(msg) == QuestionResponseEnum.Yes;
        }
        
        public void Detach()
        {
            FetchJobs.CancelAsync();
            view.RemoveEventHandlers();
            view.MainWidget.Destroy();
        }

        public void UpdateDownloadProgress(double progress)
        {
            view.DownloadProgress = progress;
        }

        private void FetchJobs_DoWork(object sender, DoWorkEventArgs args)
        {
            while (!FetchJobs.CancellationPending) // this check is performed regularly inside the ListJobs() function as well.
            {
                // TODO : find a way to detect when this tab has been closed. If this occurs, this thread needs to stop                

                // update the list of jobs. this will take a bit of time                
                var newJobs = ListJobs();
                
                if (FetchJobs.CancellationPending) return;
                if (newJobs == null) return;

                if (newJobs.Count > 0)
                {
                    // if the new job list is different, update the tree view
                    if (newJobs.Count() != jobList.Count())
                    {
                        jobList = newJobs;
                        if (UpdateDisplay() == 1) return;
                    } else
                    {
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
                    }
                }
                // refresh every 10 seconds
                System.Threading.Thread.Sleep(10000);
            }            
        }

        /// <summary>
        /// Checks if the current user owns a job. 
        /// </summary>
        /// <param name="id">ID of the job.</param>
        /// <returns></returns>
        public bool UserOwnsJob(string id)
        {
            return GetJob(id).Owner.ToLower() == Environment.UserName.ToLower();
        }

        /// <summary>
        /// Gets the formatted display name of a job.
        /// </summary>
        /// <param name="id">ID of the job.</param>
        /// <param name="withOwner">If true, the return value will include the job owner's name in brackets.</param>
        /// <returns></returns>
        public string GetJobName(string id, bool withOwner)
        {
            JobDetails job = GetJob(id);
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
                view.JobLoadProgress = 0;
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

            IPagedEnumerable<CloudJob> cloudJobs = null;


            int numTries = 0;
            while (numTries < 4 && cloudJobs == null)
            {
                try
                {
                    cloudJobs = batchClient.JobOperations.ListJobs(jobDetailLevel);
                }
                catch (Exception e)
                {
                    if (numTries >= 3)
                    {
                        ShowError("Unable to retrieve job list: " + e.ToString());
                        return new List<JobDetails>();
                    }
                }
            }
            
            
            var length = cloudJobs.Count();
            int i = 0;

            foreach (var cloudJob in cloudJobs)
            {
                if (FetchJobs.CancellationPending) return null;
                try
                {
                    view.JobLoadProgress = 100.0 * i / length;
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
                TaskCounts tasks;
                long numTasks;
                double jobProgress;
                try
                {
                    tasks = batchClient.JobOperations.GetJobTaskCounts(cloudJob.Id);
                    numTasks = tasks.Active + tasks.Running + tasks.Completed;
                    // if there are no tasks, set progress to 100%
                    jobProgress = numTasks == 0 ? 100 : 100.0 * tasks.Completed / numTasks;
                } catch (Exception e)
                {
                    // sometimes an exception is thrown when retrieving the task counts
                    // could be due to the job not being submitted correctly
                    ShowError(e.ToString());

                    numTasks = -1;
                    jobProgress = 100;
                }
                
                

                
                // if cpu time is unavailable, set this field to 0
                TimeSpan cpu = cloudJob.Statistics == null ? TimeSpan.Zero : cloudJob.Statistics.KernelCpuTime + cloudJob.Statistics.UserCpuTime;
                var job = new JobDetails
                {
                    Id = cloudJob.Id,
                    DisplayName = cloudJob.DisplayName,
                    State = cloudJob.State.ToString(),
                    Owner = owner,
                    NumSims = numTasks,
                    Progress = jobProgress,
                    CpuTime = cpu
                };

                if (cloudJob.ExecutionInformation != null)
                {
                    job.StartTime = cloudJob.ExecutionInformation.StartTime;
                    job.EndTime = cloudJob.ExecutionInformation.EndTime;

                    if (cloudJob.ExecutionInformation.PoolId != null)
                    {
                        //var pool = pools.FirstOrDefault(p => string.Equals(cloudJob.ExecutionInformation.PoolId, p.Id));
                        string poolId = cloudJob.ExecutionInformation.PoolId;
                        CloudPool pool = null;
                        foreach (CloudPool currentPool in pools)
                        {
                            if (currentPool.Id == poolId)
                            {
                                pool = currentPool;
                                break;
                            }
                        }
                        if (pool != null)
                        {
                            job.PoolSettings = new PoolSettings
                            {
                                MaxTasksPerVM = pool.MaxTasksPerComputeNode.GetValueOrDefault(1),
                                State = pool.AllocationState.GetValueOrDefault(AllocationState.Resizing).ToString(),
                                VMCount = pool.CurrentDedicatedComputeNodes.GetValueOrDefault(0),
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
                ShowError(e.ToString());
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
        /// Tests if two jobs are equal.
        /// </summary>
        /// <param name="a">The first job.</param>
        /// <param name="b">The second job.</param>
        /// <returns>True if the jobs have the same ID and they are in the same state.</returns>
        private bool IsEqual(JobDetails a, JobDetails b)
        {
            return (a.Id == b.Id && a.State == b.State && a.Progress == b.Progress);
        }

        /// <summary>
        /// Gets a job with a given ID from the local job list.
        /// </summary>
        /// <param name="id">ID of the job.</param>
        /// <returns>JobDetails object.</returns>
        public JobDetails GetJob(string id)
        {
            return jobList.FirstOrDefault(x => x.Id == id);
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
            if (currentlyDownloading.Count() > 0)
            {
                ShowError("Unable to start a new batch of downloads - one or more downloads are already ongoing.");
                return;
            }

            if (jobsToDownload.Count < 1)
            {
                ShowMessage("Unable to download jobs - no jobs are selected.");
                return;
            }
            FetchJobs.CancelAsync();
            while (FetchJobs.IsBusy) ;

            view.HideLoadingProgressBar();
            view.ShowDownloadProgressBar();
            ShowMessage("");
            string path = (string)Settings.Default["OutputDir"];
            AzureResultsDownloader dl;

            // If a results directory (outputPath\jobName) already exists, the user will receive a warning asking them if they want to continue.
            // This message should only be displayed once. Once it's been displayed this boolean is set to true so they won't be asked again.
            bool ignoreWarning = false;

            foreach (string id in jobsToDownload)
            {                
                // if the job id is invalid, skip downloading this job                
                if (!Guid.TryParse(id, out Guid jobId)) continue;
                currentlyDownloading.Add(jobId);
                string jobName = GetJob(id).DisplayName;

                view.DownloadProgress = 0;

                // if output directory already exists and warning has not already been given, display a warning
                if (Directory.Exists((string)Settings.Default["OutputDir"] + "\\" + jobName) && !ignoreWarning)
                {
                    if (!view.ShowWarning("Files detected in output directory. Results will be generated from ALL files in this directory. Are you certain you wish to continue?"))
                    {
                        // if user has chosen to cancel the download
                        view.HideDownloadProgressBar();
                        return;
                    }
                    else ignoreWarning = true;
                }

                // if job has not finished, skip to the next job in the list
                if (GetJob(id).State.ToString().ToLower() != "completed")
                {
                    ShowError("Unable to download " + GetJob(id).DisplayName.ToString() + ": Job has not finished running");
                    continue;
                }

                dl = new AzureResultsDownloader(jobId, GetJob(id).DisplayName, path, this, saveToCsv, includeDebugFiles, keepOutputFiles);                
                dl.DownloadResults(false);
            }
            FetchJobs.RunWorkerAsync();
        }

        /// <summary>
        /// Opens a dialog box which asks the user for credentials.
        /// </summary>
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

        public void ShowMessage(string msg)
        {
            MainPresenter.ShowMessage(msg, Simulation.ErrorLevel.Information);
        }

        /// <summary>
        /// Sets the default downlaod directory.
        /// </summary>
        /// <param name="dir">Path to the directory.</param>
        public void SetDownloadDirectory(string dir)
        {            
            if (dir == "") return;

            if (Directory.Exists(dir))
            {
                Settings.Default["OutputDir"] = dir;
                Settings.Default.Save();
            }
            else
            {
                ShowError("Directory " + dir + " does not exist.");
            }
        }

        /// <summary>
        /// Parses and compares two DateTime objects stored as strings.
        /// </summary>
        /// <param name="str1">First DateTime.</param>
        /// <param name="str2">Second DateTime.</param>
        /// <returns></returns>
        public int CompareDateTimeStrings(string str1, string str2)
        {
            // if either of these strings is empty, the job is still running
            if (str1 == "" || str1 == null)
            {
                if (str2 == "" || str2 == null) // neither job has finished
                {
                    return 0;
                }
                else // first job is still running, second is finished
                {
                    return 1;
                }
            }
            else if (str2 == "" || str2 == null) // first job is finished, second job still running
            {                
                return -1;
            }
            // otherwise, both jobs are still running
            DateTime t1 = GetDateTimeFromString(str1);
            DateTime t2 = GetDateTimeFromString(str2);
            
            return DateTime.Compare(t1, t2);
        }

        /// <summary>
        /// Generates a DateTime object from a string.
        /// </summary>
        /// <param name="st">Date time string. MUST be in the format dd/mm/yyyy hh:mm:ss (A|P)M</param>
        /// <returns>A DateTime object representing this string.</returns>
        public DateTime GetDateTimeFromString(string st)
        {
            try
            {
                string[] separated = st.Split(' ');
                string[] date = separated[0].Split('/');
                string[] time = separated[1].Split(':');
                int year, month, day, hour, minute, second;
                day = Int32.Parse(date[0]);
                month = Int32.Parse(date[1]);
                year = Int32.Parse(date[2]);

                hour = Int32.Parse(time[0]);
                if (separated[separated.Length - 1].ToLower() == "pm" && hour < 12) hour += 12;
                minute = Int32.Parse(time[1]);
                second = Int32.Parse(time[2]);

                return new DateTime(year, month, day, hour, minute, second);
            }
            catch (Exception e)
            {
                ShowError(e.ToString());
            }
            return new DateTime();
        }

        /// <summary>
        /// Writes to a log file and asks the view to display an error message if download was unsuccessful.
        /// </summary>
        /// <param name="code"></param>
        public void DisplayFinishedDownloadStatus(string name, int code, string path, DateTime timeStamp)
        {
            view.HideDownloadProgressBar();
            if (code == 0)
            {
                ShowMessage("Download successful.");
                return;
            }
            string msg = timeStamp.ToLongTimeString().Split(' ')[0] + ": " +  name + ": ";
            switch (code)
            {
                case 1:
                    msg += "Unable to generate a .csv file: no result files were found.";
                    break;
                case 2:
                    msg += "Unable to generate a .csv file: one or more result files may be empty";
                    break;
                case 3:
                    msg += "Unable to generate a temporary directory.";
                    break;
                default:
                    msg += "Download unsuccessful.";
                    break;
            }
            string logFile = path + "\\download.log";
            view.DownloadStatus = "One or more downloads encountered an error. See " + logFile + " for more details.";
            lock (logFileMutex)
            {
                try
                {
                    if (!File.Exists(logFile)) File.Create(logFile);
                    using (StreamWriter sw = File.AppendText(logFile))
                    {
                        sw.WriteLine(msg);
                        sw.Close();
                    }
                } catch
                {

                }
            }
        }

        /// <summary>
        /// Asks the user for confirmation and then halts execution of a list of jobs.
        /// </summary>
        /// <param name="id">ID of the job.</param>
        public void StopJobs(List<string> jobIds)
        {
            // ask user once for confirmation

            // get the grammar right when asking for confirmation
            bool stopMultiple = jobIds.Count > 1;
            string msg = "Are you sure you want to stop " + (stopMultiple ? "these " + jobIds.Count + " jobs?" : "this job?") + " There is no way to resume their execution!";
            string label = stopMultiple ? "Stop these jobs?" : "Stop this job?";
            if (!view.ShowWarning(msg)) return;
            
            foreach (string id in jobIds)
            {
                // no need to stop a job that is already finished
                if (GetJob(id).State.ToLower() != "completed")
                {
                    StopJob(id);
                }
            }            
        }

        /// <summary>
        /// Halts the execution of a job.
        /// </summary>
        /// <param name="id"></param>
        public void StopJob(string id)
        {
            try
            {
                batchClient.JobOperations.TerminateJob(id);
            }
            catch (Exception e)
            {
                ShowError(e.Message);
            }
        }

        /// <summary>
        /// Asks the user for confirmation and then deletes a list of jobs.
        /// </summary>
        /// <param name="jobIds">ID of the job.</param>
        public void DeleteJobs(List<string> jobIds)
        {
            // cancel the fetch jobs worker
            FetchJobs.CancelAsync();
            while (FetchJobs.IsBusy);
            view.HideLoadingProgressBar();

            foreach (string id in jobIds)
            {
                try
                {
                    if (!Guid.TryParse(id, out Guid parsedId)) continue;
                    // delete the job from Azure
                    CloudBlobContainer containerRef;

                    containerRef = blobClient.GetContainerReference(StorageConstants.GetJobOutputContainer(parsedId));
                    if (containerRef.Exists()) containerRef.Delete();

                    containerRef = blobClient.GetContainerReference(StorageConstants.GetJobContainer(parsedId));
                    if (containerRef.Exists()) containerRef.Delete();

                    containerRef = blobClient.GetContainerReference(parsedId.ToString());
                    if (containerRef.Exists()) containerRef.Delete();

                    var job = GetJob(id);
                    if (job != null) batchClient.JobOperations.DeleteJob(id);

                    // remove the job from the locally stored list of jobs
                    jobList.RemoveAt(jobList.IndexOf(GetJob(id)));
                } catch (Exception e)
                {
                    ShowError(e.ToString());
                }                
            }
            // refresh the tree view
            view.UpdateTreeView(jobList);

            // restart the fetch jobs worker
            FetchJobs.RunWorkerAsync();
        }
    }
}
