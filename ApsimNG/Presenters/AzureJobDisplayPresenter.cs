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

namespace UserInterface.Presenters
{
    public class AzureJobDisplayPresenter : IPresenter
    {

        private CloudStorageAccount storageAccount;
        private BatchClient batchClient;        
        private IAzureJobDisplayView view;
        private ExplorerPresenter explorerPresenter;
        private Models.Core.IModel model;
        private StorageCredentials storageCredentials;
        private BatchCredentials batchCredentials;
        private PoolSettings poolSettings;
        private BackgroundWorker FetchJobs;
        private Timer updateJobsTimer;

        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.explorerPresenter = explorerPresenter;
            this.view = (IAzureJobDisplayView)view;
            this.model = (IModel)model;

            // store credentials
            if (!SetCredentials())
            {                
                return;
            }
            storageCredentials = StorageCredentials.FromConfiguration();
            batchCredentials = BatchCredentials.FromConfiguration();
            poolSettings = PoolSettings.FromConfiguration();

            storageAccount = new CloudStorageAccount(new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials(storageCredentials.Account, storageCredentials.Key), true);
            var sharedCredentials = new Microsoft.Azure.Batch.Auth.BatchSharedKeyCredentials(batchCredentials.Url, batchCredentials.Account, batchCredentials.Key);
            batchClient = BatchClient.Open(sharedCredentials);
            
            FetchJobs = new BackgroundWorker();
            FetchJobs.DoWork += FetchJobs_DoWork;            
            // start downloading the list of jobs immediately
            if (FetchJobs.IsBusy)
            {
                FetchJobs.CancelAsync();
            }
            FetchJobs.RunWorkerAsync();

            // timer to automatically update the list of jobs every 30 seconds
            updateJobsTimer = new Timer(30000);
            updateJobsTimer.Elapsed += TimerElapsed;
            updateJobsTimer.AutoReset = true;
            updateJobsTimer.Start();
        }

        public void Detach()
        {

        }

        private void TimerElapsed(object sender, EventArgs e)
        {
            if (!FetchJobs.IsBusy) FetchJobs.RunWorkerAsync();
        }

        private void FetchJobs_DoWork(object sender, DoWorkEventArgs args)
        {
            var jobs = ListJobs();
            try
            {
                view.AddJobsToTableIfNecessary(jobs);
            }
            catch
            {
                // the most likely error here occurs if the user has moved to a different right hand panel at an unfortunate time
                // in which case this thread can no longer access the original view (or rather, the view is no longer attached to the right hand panel)
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
            foreach (var cloudJob in cloudJobs)
            {
                if (FetchJobs.CancellationPending)
                {
                    return jobs;
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

                // TODO : find a neater solution to the threading problem than this try/catch
                try
                {
                    view.UpdateJobLoadStatus(100.0 * i / length);
                }
                catch
                {
                    return jobs;
                }
            }            
            return jobs;
        }

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
                explorerPresenter.MainPresenter.ShowMessage(e.ToString(), Simulation.ErrorLevel.Error);
            }
            return "";
        }

        /// <summary>
        /// Read Azure credentials from the file ApsimX\AzureAgR.lic
        /// This is a temporary measure - will probably need to allow user to specify a file.
        /// </summary>
        /// <returns>True if the credentials file exists, false otherwise.</returns>
        private bool SetCredentials()
        {
            // TODO : allow user to specify a credentials file?
            string credentialsFile = Directory.GetParent(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)).ToString() + "\\AzureAgR.lic";
            if (File.Exists(credentialsFile))
            {
                string line;
                StreamReader file = new StreamReader(credentialsFile);
                while ((line = file.ReadLine()) != null)
                {
                    int separatorIndex = line.IndexOf("=");
                    if (separatorIndex > -1)
                    {
                        string key = line.Substring(0, separatorIndex);
                        string val = line.Substring(separatorIndex + 1);

                        Settings.Default[key] = val;
                    }
                }
                return true;
            } else
            {
                explorerPresenter.MainPresenter.ShowMessage("Azure Licence file " + credentialsFile + " not found.", Simulation.ErrorLevel.Error);
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

        private CloudJob GetJob(Guid jobId)
        {
            ODATADetailLevel detailLevel = new ODATADetailLevel { SelectClause = "id" };
            CloudJob job = batchClient.JobOperations.ListJobs(detailLevel).FirstOrDefault(j => string.Equals(jobId.ToString(), j.Id));
            if (job == null) return null;
            return batchClient.JobOperations.GetJob(jobId.ToString());
        }
    }
}
