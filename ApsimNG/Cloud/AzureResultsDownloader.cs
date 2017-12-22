using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Common;
using System.IO;
using System.ComponentModel;
using UserInterface.Presenters;

namespace ApsimNG.Cloud
{
    class AzureResultsDownloader
    {
        private Guid jobId;
        private string outputPath;
        private CloudStorageAccount storageAccount;
        private BatchClient batchClient;
        private CloudBlobClient blobClient;
        private AzureJobDisplayPresenter presenter;
        private CloudJob job;
        private BackgroundWorker downloader;

        public AzureResultsDownloader(Guid id, string path, AzureJobDisplayPresenter explorer)
        {
            jobId = id;
            outputPath = path;
            presenter = explorer;

            StorageCredentials storageCredentials = StorageCredentials.FromConfiguration();
            BatchCredentials batchCredentials = BatchCredentials.FromConfiguration();
            storageAccount = new CloudStorageAccount(new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials(storageCredentials.Account, storageCredentials.Key), true);
            var sharedCredentials = new Microsoft.Azure.Batch.Auth.BatchSharedKeyCredentials(batchCredentials.Url, batchCredentials.Account, batchCredentials.Key);
            batchClient = BatchClient.Open(sharedCredentials);
            blobClient = storageAccount.CreateCloudBlobClient();
            blobClient.DefaultRequestOptions.RetryPolicy = new Microsoft.WindowsAzure.Storage.RetryPolicies.LinearRetry(TimeSpan.FromSeconds(3), 10);

            ODATADetailLevel detailLevel = new ODATADetailLevel { SelectClause = "id" };
            CloudJob tmpJob = batchClient.JobOperations.ListJobs(detailLevel).FirstOrDefault(j => string.Equals(jobId.ToString(), j.Id));
            job = tmpJob == null ? tmpJob : batchClient.JobOperations.GetJob(jobId.ToString());
        }

        public void DownloadResults()
        {
            // TODO : add a checkbox to include debugging files
            int numJobs = CountBlobs(false);

            // progress bar. maximum is numJobs

            if (!Directory.Exists(outputPath))
            {
                try
                {
                    Directory.CreateDirectory(outputPath);
                }
                catch (Exception e)
                {
                    presenter.ShowError(e.ToString());
                }
            }
            downloader = new BackgroundWorker();
            downloader.DoWork += Downloader_DoWork;
            downloader.RunWorkerAsync(outputPath);
        }

        private void Downloader_DoWork(object sender, DoWorkEventArgs e)
        {
            CancellationToken ct;

            var outputHashLock = new object();
            HashSet<string> downloadedOutputs = GetDownloadedOutputFiles();

            while (true)
            {
                try
                {
                    if (downloader.CancellationPending || ct.IsCancellationRequested) return;

                    bool complete = IsJobComplete();
                    var outputs = ListJobOutputsFromStorage();
                    Parallel.ForEach(outputs,
                                     new ParallelOptions { CancellationToken = ct, MaxDegreeOfParallelism = 8 },
                                     blob => 
                                     {
                                         bool skip = false;
                                         string extension = Path.GetExtension(blob.Name.ToLower());
                                         //if (extension == ".stdout" || extension == ".sum") skip = true;
                                         if (!skip && !downloadedOutputs.Contains(blob.Name))
                                         {
                                             blob.DownloadToFile(Path.Combine(outputPath, blob.Name), FileMode.Create);
                                             lock (outputHashLock)
                                             {
                                                 downloadedOutputs.Add(blob.Name);
                                             }
                                         }
                                         Console.WriteLine(DateTime.Now.ToLongTimeString() + ": Downloaded " + Path.Combine(outputPath, blob.Name));                                         
                                         // report progress?
                                     });
                    presenter.DisplayFinishedDownloadStatus(outputPath, true);
                    if (complete) break;
                }
                catch (AggregateException ae)
                {
                    Console.WriteLine(ae.InnerException.ToString());
                }
            }
            Console.WriteLine("Finished Downloading Results!");
        }

        /// <summary>
        /// Cancels a download in progress.
        /// </summary>
        /// <param name="block">Whether or not the function should block until the download thread exits.</param>
        private void CancelDownload(bool block = false)
        {
            downloader.CancelAsync();
            while (block && downloader.IsBusy);
        }
        
        /// <summary>
        /// Counts the number of blobs in a cloud storage container.
        /// </summary>
        /// <param name="jobId">Id of the container.</param>
        /// <param name="includeDebugFiles">Whether or not to count debug file blobs.</param>
        /// <returns></returns>
        private int CountBlobs(bool includeDebugFiles)
        {
            try
            {
                CloudBlobContainer container = blobClient.GetContainerReference(StorageConstants.GetJobOutputContainer(jobId));
                container.FetchAttributes();
                int count = 0;
                var blobs = container.ListBlobs();

                foreach (var blob in blobs)
                {
                    string extension = Path.GetExtension(blob.Uri.LocalPath.ToLower());
                    if (includeDebugFiles || !(extension == ".stdout" || extension == ".sum")) count++;
                }
                return count;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Lists all files in the output directory.
        /// </summary>
        /// <returns></returns>
        private HashSet<string> GetDownloadedOutputFiles()
        {
            return new HashSet<string>(Directory.EnumerateFiles(outputPath).Select(f => Path.GetFileName(f)));
        }

        private IEnumerable<CloudBlockBlob> ListJobOutputsFromStorage()
        {
            var containerRef = blobClient.GetContainerReference(StorageConstants.GetJobOutputContainer(jobId));
            if (!containerRef.Exists())
            {
                return Enumerable.Empty<CloudBlockBlob>();
            }
            return containerRef.ListBlobs().Select(b => ((CloudBlockBlob)b));
        }

        /// <summary>
        /// Tests if a job has been completed.
        /// </summary>
        /// <returns>True if the job was completed or disabled, otherwise false.</returns>
        private bool IsJobComplete()
        {
            if (job == null) return true;
            return job.State == JobState.Completed || job.State == JobState.Disabled;
        }

        private void ReportFinished(bool successful)
        {
            presenter.DisplayFinishedDownloadStatus(outputPath, successful);
        }
    }
}
