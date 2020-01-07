using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Auth;
using Microsoft.Azure.Batch.Common;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.RetryPolicies;
using Models;
using Models.Core;
using Models.Core.Run;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ApsimNG.Cloud.Azure;
using APSIM.Shared.Utilities;

namespace ApsimNG.Cloud
{
    public class AzureInterface : ICloudInterface
    {
        private BatchClient batchClient;
        private CloudBlobClient storageClient;

        /// <summary>The results are compressed into a file with this name.</summary>
        private const string resultsFileName = "Results.zip";

        /// <summary>Array of all valid debug file formats.</summary>
        private static readonly string[] debugFileFormats = { ".stdout", ".sum" };

        public AzureInterface()
        {
            AzureCredentialsSetup.GetCredentialsIfNotExist();

            // Setup Azure batch/storage clients using the given credentials.
            SetupClient();
        }

        /// <summary>
        /// Submit a job to be run on Azure.
        /// </summary>
        /// <param name="job">Job parameters.</param>
        /// <param name="UpdateStatus">Action which will display job submission status to the user.</param>
        public async Task SubmitJobAsync(JobParameters job, Action<string> UpdateStatus)
        {
            // Initialise a working directory.
            UpdateStatus("Initialising job environment...");
            string workingDirectory = Path.Combine(Path.GetTempPath(), job.ID.ToString());
            Directory.CreateDirectory(workingDirectory);

            // Set job owner.
            string owner = Environment.UserName.ToLower();
            await SetAzureMetaDataAsync("job-" + job.ID, "Owner", owner);

            // If the ApsimX path is a directory it will need to be compressed.
            if (Directory.Exists(job.ApsimXPath))
            {
                UpdateStatus("Compressing APSIM Next Generation...");

                string zipFile = Path.Combine(workingDirectory, $"Apsim-tmp-X-{owner}.zip");
                if (File.Exists(zipFile))
                    File.Delete(zipFile);

                CreateApsimXZip(job.ApsimXPath, zipFile);

                job.ApsimXPath = zipFile;
                job.ApsimXVersion = Path.GetFileName(zipFile).Substring(Path.GetFileName(zipFile).IndexOf('-') + 1);
            }

            // Upload tools such as 7zip, AzCopy, CMail, etc.
            UpdateStatus("Uploading tools...");
            string executableDirectory = Path.GetDirectoryName(GetType().Assembly.Location);
            string toolsDir = Path.Combine(executableDirectory, "tools");
            if (!Directory.Exists(toolsDir))
                throw new Exception("Tools Directory not found: " + toolsDir);

            foreach (string filePath in Directory.EnumerateFiles(toolsDir))
                await UploadFileIfNeededAsync("tools", filePath);

            // Upload email config file.
            if (job.SendEmail)
            {
                StringBuilder config = new StringBuilder();
                config.AppendLine($"EmailRecipient={job.EmailRecipient}");
                config.AppendLine($"EmailSender={AzureSettings.Default.EmailSender}");
                config.AppendLine($"EmailPW={AzureSettings.Default.EmailPW}");

                // Write these settings to a temporary config file.
                string configFile = Path.Combine(workingDirectory, "settings.txt");
                File.WriteAllText(configFile, config.ToString());

                await UploadFileIfNeededAsync("job-" + job.ID, configFile);
                File.Delete(configFile);
            }

            // Upload job manager.
            UpdateStatus("Uploading job manager...");
            await UploadFileIfNeededAsync("jobmanager", Path.Combine(executableDirectory, "azure-apsim.exe"));

            // Upload apsim.
            UpdateStatus("Uploading APSIM Next Generation...");
            await UploadFileIfNeededAsync("apsim", job.ApsimXPath);

            // Generate model files.
            UpdateStatus("Generating model files...");
            if (!Directory.Exists(job.ModelPath))
                Directory.CreateDirectory(job.ModelPath);

            // Copy weather files to models directory to be compressed.
            Simulations sims = Apsim.Parent(job.Model, typeof(Simulations)) as Simulations;
            foreach (Weather child in Apsim.ChildrenRecursively(job.Model, typeof(Weather)))
            {
                if (Path.GetDirectoryName(child.FullFileName) != Path.GetDirectoryName(sims.FileName))
                    throw new Exception("Weather file must be in the same directory as .apsimx file: " + child.FullFileName);

                string sourceFile = child.FullFileName;
                string destFile = Path.Combine(job.ModelPath, child.FileName);
                if (!File.Exists(destFile))
                    File.Copy(sourceFile, destFile);
            }

            // Generate .apsimx file for each simulation to be run.
            Runner run = new Runner(job.Model);
            GenerateApsimXFiles.Generate(run, job.ModelPath, p => { /* Don't bother with progress reporting */ });

            // Compress model (.apsimx file) directory.
            UpdateStatus("Compressing model files...");
            string tmpZip = Path.Combine(workingDirectory, $"Model-{Guid.NewGuid()}.zip");
            ZipFile.CreateFromDirectory(job.ModelPath, tmpZip, CompressionLevel.Fastest, false);
            job.ModelPath = tmpZip;

            // Upload models.
            UpdateStatus("Uploading model files...");
            string modelZipFileSas = await UploadFileIfNeededAsync(job.ID.ToString(), job.ModelPath);

            // Clean up temp files.
            UpdateStatus("Deleting temp files...");
            Directory.Delete(workingDirectory, true);

            // Submit job.
            UpdateStatus("Submitting Job");
            CloudJob cloudJob = batchClient.JobOperations.CreateJob(job.ID.ToString(), GetPoolInfo(job));
            cloudJob.DisplayName = job.DisplayName;
            cloudJob.JobPreparationTask = ToJobPreparationTask(job, modelZipFileSas);
            cloudJob.JobReleaseTask = ToJobReleaseTask(job, modelZipFileSas);
            cloudJob.JobManagerTask = ToJobManagerTask(job);

            await cloudJob.CommitAsync();
            UpdateStatus("Job Successfully submitted");
        }

        /// <summary>
        /// List all Azure jobs.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        /// <param name="ShowProgress">Function to report progress as percentage in range [0, 100].</param>
        public async Task<List<JobDetails>> ListJobsAsync(CancellationToken ct, Action<double> ShowProgress)
        {
            ShowProgress(0);

            IEnumerable<CloudPool> pools = batchClient.PoolOperations.ListPools();
            ODATADetailLevel jobDetailLevel = new ODATADetailLevel { SelectClause = "id,displayName,state,executionInfo,stats", ExpandClause = "stats" };

            // Download raw job list via the Azure API.
            List<CloudJob> cloudJobs = batchClient.JobOperations.ListJobs(jobDetailLevel).ToList();

            // Parse jobs into a list of JobDetails objects.
            List<JobDetails> jobs = new List<JobDetails>();
            for (int i = 0; i < cloudJobs.Count; i++)
            {
                if (ct.IsCancellationRequested)
                    return jobs;

                ShowProgress(100.0 * i / cloudJobs.Count);
                jobs.Add(await GetJobDetails(cloudJobs[i]));
            }

            ShowProgress(100);
            return jobs;
        }

        /// <summary>
        /// Halt the execution of a job.
        /// </summary>
        /// <param name="jobID">ID of the job.</param>
        public async Task StopJobAsync(string jobID)
        {
            await batchClient.JobOperations.TerminateJobAsync(jobID);
        }

        /// <summary>
        /// Delete a job and all cloud storage associated with the job.
        /// </summary>
        /// <param name="jobID">ID of the job.</param>
        public async Task DeleteJobAsync(string jobID)
        {
            Guid parsedID = Guid.Parse(jobID);

            // Delete cloud storage associated with the job.
            await storageClient.GetContainerReference(StorageConstants.GetJobOutputContainer(parsedID)).DeleteIfExistsAsync();
            await storageClient.GetContainerReference(StorageConstants.GetJobContainer(parsedID)).DeleteIfExistsAsync();
            await storageClient.GetContainerReference(jobID).DeleteIfExistsAsync();

            // Delete the job.
            await batchClient.JobOperations.DeleteJobAsync(jobID);
        }

        /// <summary>Download the results of a job.</summary>
        /// <param name="options">Download options.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task DownloadResultsAsync(DownloadOptions options, CancellationToken ct)
        {
            if (!Directory.Exists(options.Path))
                Directory.CreateDirectory(options.Path);

            List<CloudBlockBlob> outputs = await GetJobOutputs(options.JobID);
            List<CloudBlockBlob> toDownload = new List<CloudBlockBlob>();

            // Build up a list of files to download.
            CloudBlockBlob results = outputs.Find(b => string.Equals(b.Name, resultsFileName, StringComparison.InvariantCultureIgnoreCase));
            if (results != null)
                toDownload.Add(results);
            else
                // Always download debug files if no results archive can be found.
                options.DownloadDebugFiles = true;

            if (options.DownloadDebugFiles)
                toDownload.AddRange(outputs.Where(blob => debugFileFormats.Contains(Path.GetExtension(blob.Name.ToLower()))));

            // Now download the necessary files.
            foreach (CloudBlockBlob blob in toDownload)
            {
                if (ct.IsCancellationRequested)
                    return;
                else
                    // todo: Download in parallel?
                    await blob.DownloadToFileAsync(Path.Combine(options.Path, blob.Name), FileMode.Create, ct);
            }

            if (options.ExtractResults)
            {
                string archive = Path.Combine(options.Path, resultsFileName);
                string resultsDir = Path.Combine(options.Path, "results");
                if (File.Exists(archive))
                {
                    // Extract the result files.
                    using (ZipArchive zip = ZipFile.Open(archive, ZipArchiveMode.Read, Encoding.UTF8))
                        zip.ExtractToDirectory(resultsDir);

                    // Merge results into a single .db file.
                    DBMerger.MergeFiles(Path.Combine(resultsDir, "*.db"), "combined.db");

                    // TBI: merge into csv file.
                    if (options.ExportToCsv)
                        throw new NotImplementedException();
                }
            }
        }

        /// <summary>Lists all output files of a given job.</summary>
        /// <param name="jobID">Job ID.</param>
        private async Task<List<CloudBlockBlob>> GetJobOutputs(Guid jobID)
        {
            CloudBlobContainer containerRef = storageClient.GetContainerReference(StorageConstants.GetJobOutputContainer(jobID));
            if (!await containerRef.ExistsAsync())
                return null;

            return await containerRef.ListBlobsAsync();
        }

        /// <summary>
        /// Initialise the batch/storage clients used to call the Azure API.
        /// </summary>
        private void SetupClient()
        {
            var credentials = new Microsoft.Azure.Storage.Auth.StorageCredentials(AzureSettings.Default.StorageAccount, AzureSettings.Default.StorageKey);
            var storageAccount = new CloudStorageAccount(credentials, true);
            storageClient = storageAccount.CreateCloudBlobClient();

            string url = AzureSettings.Default.BatchUrl;
            string account = AzureSettings.Default.BatchAccount;
            string key = AzureSettings.Default.BatchKey;
            var sharedCredentials = new BatchSharedKeyCredentials(url, account, key);
            batchClient = BatchClient.Open(sharedCredentials);
        }

        /// <summary>
        /// Sets metadata for a particular container in Azure's cloud storage.
        /// </summary>
        /// <param name="containerName">Name of the container</param>
        /// <param name="key">Metadata key/name</param>
        /// <param name="val">Data to write</param>
        private async Task SetAzureMetaDataAsync(string containerName, string key, string val)
        {
            var containerRef = storageClient.GetContainerReference(containerName);
            await containerRef.CreateIfNotExistsAsync();
            containerRef.Metadata.Add(key, val);
            await containerRef.SetMetadataAsync();
        }

        /// <summary>Gets metadata value for a container in Azure cloud storage.</summary>
        /// <param name="containerName">Name of the container.</param>
        /// <param name="key">Metadata key.</param>
        private async Task<string> GetContainerMetaDataAsync(string containerName, string key)
        {
            CloudBlobContainer containerRef = storageClient.GetContainerReference(containerName);
            if (!await containerRef.ExistsAsync())
                throw new Exception($"Failed to fetch metadata '{key}' for container '{containerName}' - container does not exist");

            await containerRef.FetchAttributesAsync();
            if (containerRef.Metadata.ContainsKey(key))
                return containerRef.Metadata[key];

            throw new Exception($"Failed to fetch metadata '{key}' for container '{containerName}' - key does not exist");
        }

        /// <summary>
        /// Upload a file to Azure's cloud storage if it does not already exist.
        /// Return the URI and shared access signature of the uploaded blob.
        /// </summary>
        /// <param name="containerName">Name of the container to upload the file to</param>
        /// <param name="filePath">Path to the file on disk</param>
        private async Task<string> UploadFileIfNeededAsync(string containerName, string filePath)
        {
            CloudBlobContainer container = storageClient.GetContainerReference(containerName);
            await container.CreateIfNotExistsAsync();
            CloudBlockBlob blob = container.GetBlockBlobReference(Path.GetFileName(filePath));

            string md5 = GetFileMd5(filePath);
            
            // If blob already exists and md5 matches, there is no need to upload the file.
            if (await blob.ExistsAsync() && string.Equals(md5, blob.Properties.ContentMD5, StringComparison.InvariantCultureIgnoreCase))
                return null;

            blob.Properties.ContentMD5 = md5;
            await blob.UploadFromFileAsync(filePath);

            var policy = new SharedAccessBlobPolicy
            {
                Permissions = SharedAccessBlobPermissions.Read,
                SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-15),
                SharedAccessExpiryTime = DateTime.UtcNow.AddMonths(12) // Hopefully this job won't take more than a year!
            };
            return blob.Uri.AbsoluteUri + blob.GetSharedAccessSignature(policy);
        }

        /// <summary>
        /// Translates an Azure-specific CloudDetails object into a
        /// generic JobDetails object which can be passed back to the
        /// presenter.
        /// </summary>
        /// <param name="cloudJob"></param>
        private async Task<JobDetails> GetJobDetails(CloudJob cloudJob)
        {
            if (!await storageClient.GetContainerReference($"job-{cloudJob.Id}").ExistsAsync())
            {
                await DeleteJobAsync(cloudJob.Id);
                return null;
            }
            string owner = await GetContainerMetaDataAsync($"job-{cloudJob.Id}", "Owner");

            TaskCounts tasks = await batchClient.JobOperations.GetJobTaskCountsAsync(cloudJob.Id);
            int numTasks = tasks.Active + tasks.Running + tasks.Completed;

            // If there are no tasks, set progress to 100%.
            double jobProgress = numTasks == 0 ? 100 : 100.0 * tasks.Completed / numTasks;

            // If cpu time is unavailable, set this field to 0.
            TimeSpan cpu = cloudJob.Statistics == null ? TimeSpan.Zero : cloudJob.Statistics.KernelCpuTime + cloudJob.Statistics.UserCpuTime;
            JobDetails job = new JobDetails
            {
                Id = cloudJob.Id,
                DisplayName = cloudJob.DisplayName,
                State = cloudJob.State.ToString(),
                Owner = owner,
                NumSims = numTasks - 1, // subtract one because one of these is the job manager
                Progress = jobProgress,
                CpuTime = cpu
            };

            if (cloudJob.ExecutionInformation != null)
            {
                job.StartTime = cloudJob.ExecutionInformation.StartTime;
                job.EndTime = cloudJob.ExecutionInformation.EndTime;
            }

            return job;
        }

        private JobPreparationTask ToJobPreparationTask(JobParameters job, string sas)
        {
            return new JobPreparationTask
            {
                CommandLine = "cmd.exe /c jobprep.cmd",
                ResourceFiles = GetJobPrepResourceFiles(sas, job.ApsimXVersion).ToList(),
                WaitForSuccess = true
            };
        }

        private JobReleaseTask ToJobReleaseTask(JobParameters job, string sas)
        {
            return new JobReleaseTask
            {
                CommandLine = "cmd.exe /c jobrelease.cmd",
                ResourceFiles = GetJobPrepResourceFiles(sas, job.ApsimXVersion).ToList(),
                EnvironmentSettings = new[]
                {
                    new EnvironmentSetting("APSIM_STORAGE_ACCOUNT", AzureSettings.Default.StorageAccount/*job.StorageAuth.Account*/),
                    new EnvironmentSetting("APSIM_STORAGE_KEY", AzureSettings.Default.StorageKey/*job.StorageAuth.Key*/),
                    new EnvironmentSetting("JOBNAME", job.DisplayName),
                    new EnvironmentSetting("RECIPIENT", job.EmailRecipient)
                }
            };
        }

        private JobManagerTask ToJobManagerTask(JobParameters job)
        {
            var cmd = string.Format("cmd.exe /c {0} job-manager {1} {2} {3} {4} {5} {6} {7} {8} {9}",
                BatchConstants.GetJobManagerPath(job.ID),
                AzureSettings.Default.BatchUrl,//job.BatchAuth.Url,
                AzureSettings.Default.BatchAccount,//job.BatchAuth.Account,
                AzureSettings.Default.BatchKey,//job.BatchAuth.Key,
                AzureSettings.Default.StorageAccount,//job.StorageAuth.Account,
                AzureSettings.Default.StorageKey,//job.StorageAuth.Key,
                job.ID,
                BatchConstants.GetModelPath(job.ID),
                job.JobManagerShouldSubmitTasks,
                job.AutoScale
            );

            return new JobManagerTask
            {
                CommandLine = cmd,
                DisplayName = "Job manager task",
                KillJobOnCompletion = true,
                Id = BatchConstants.JobManagerName,
                RunExclusive = false,
                ResourceFiles = GetJobManagerResourceFiles().ToList()
            };
        }

        private IEnumerable<ResourceFile> GetJobManagerResourceFiles()
        {
            var toolsRef = storageClient.GetContainerReference("jobmanager");
            foreach (CloudBlockBlob listBlobItem in toolsRef.ListBlobs())
            {
                var sas = listBlobItem.GetSharedAccessSignature(new SharedAccessBlobPolicy
                {
                    SharedAccessStartTime = DateTime.UtcNow.AddHours(-1),
                    SharedAccessExpiryTime = DateTime.UtcNow.AddMonths(2),
                    Permissions = SharedAccessBlobPermissions.Read,
                });
                yield return ResourceFile.FromUrl(listBlobItem.Uri.AbsoluteUri + sas, listBlobItem.Name);
            }
        }

        /// <summary>
        /// Returns the zipped Apsim file and helpers like AzCopy and 7zip
        /// </summary>
        /// <param name="job"></param>
        /// <param name="blobClient"></param>
        /// <returns></returns>
        private IEnumerable<ResourceFile> GetJobPrepResourceFiles(string modelZipFileSas, string version)
        {
            yield return ResourceFile.FromUrl(modelZipFileSas, BatchConstants.ModelZipFileName);

            var toolsRef = storageClient.GetContainerReference("tools");
            foreach (CloudBlockBlob listBlobItem in toolsRef.ListBlobs())
            {
                var sas = listBlobItem.GetSharedAccessSignature(new SharedAccessBlobPolicy
                {
                    SharedAccessStartTime = DateTime.UtcNow.AddHours(-1),
                    SharedAccessExpiryTime = DateTime.UtcNow.AddMonths(2),
                    Permissions = SharedAccessBlobPermissions.Read,
                });
                yield return ResourceFile.FromUrl(listBlobItem.Uri.AbsoluteUri + sas, listBlobItem.Name);
            }

            var apsimRef = storageClient.GetContainerReference("apsim");
            foreach (CloudBlockBlob listBlobItem in apsimRef.ListBlobs())
            {
                if (listBlobItem.Name.ToLower().Contains(version.ToLower()))
                {
                    var sas = listBlobItem.GetSharedAccessSignature(new SharedAccessBlobPolicy
                    {
                        SharedAccessStartTime = DateTime.UtcNow.AddHours(-1),
                        SharedAccessExpiryTime = DateTime.UtcNow.AddMonths(2),
                        Permissions = SharedAccessBlobPermissions.Read
                    });
                    yield return ResourceFile.FromUrl(listBlobItem.Uri.AbsoluteUri + sas, listBlobItem.Name);
                }
            }
        }

        /// <summary>
        /// Compress all .exe and .dll files in the ApsimX/Bin directory
        /// into a single .zip archive at a given path.
        /// </summary>
        /// <param name="srcPath">Path of the ApsimX directory.</param>
        /// <param name="zipPath">Path to which the zip file will be saved.</param>
        private void CreateApsimXZip(string srcPath, string zipPath)
        {
            try
            {
                string bin = Path.Combine(srcPath, "Bin");
                string[] extensions = new string[] { "*.dll", "*.exe" };

                using (FileStream zipToOpen = new FileStream(zipPath, FileMode.Create))
                using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
                    foreach (string extension in extensions)
                        foreach (string fileName in Directory.EnumerateFiles(bin, extension))
                            archive.CreateEntryFromFile(fileName, Path.GetFileName(fileName));
            }
            catch (Exception err)
            {
                throw new Exception("Error compressing APSIM", err);
            }
        }

        /// <summary>
        /// Get the md5 hash of a file
        /// </summary>
        /// <param name="filePath">Path of the file on disk</param>        
        private string GetFileMd5(string filePath)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    var hashBytes = md5.ComputeHash(stream);
                    return Convert.ToBase64String(hashBytes);
                }
            }
        }

        private PoolInformation GetPoolInfo(JobParameters job)
        {
            if (string.IsNullOrEmpty(AzureSettings.Default.PoolName))
            {
                return new PoolInformation
                {
                    AutoPoolSpecification = new AutoPoolSpecification
                    {
                        PoolLifetimeOption = PoolLifetimeOption.Job,
                        PoolSpecification = new PoolSpecification
                        {
                            ResizeTimeout = TimeSpan.FromMinutes(15),

                            // todo: look into using ComputeNodeFillType.Pack
                            TaskSchedulingPolicy = new TaskSchedulingPolicy(ComputeNodeFillType.Spread),

                            // This specifies the OS that our VM will be running.
                            // OS Family 5 means .NET 4.6 will be installed.
                            // For more info see:
                            // https://docs.microsoft.com/en-us/azure/cloud-services/cloud-services-guestos-update-matrix#releases
                            CloudServiceConfiguration = new CloudServiceConfiguration("5"),

                            // For now, always use standard_d5_v2 VM type.
                            // This VM has 16 vCPUs, 56 GiB of memory and 800 GiB temp (SSD) storage.
                            // todo: should make this user-controllable
                            // For other VM sizes, see:
                            // https://docs.microsoft.com/azure/batch/batch-pool-vm-sizes
                            // https://docs.microsoft.com/azure/virtual-machines/windows/sizes-general
                            VirtualMachineSize = "standard_d5_v2",

                            // Each task needs only one vCPU. Therefore number of tasks per VM will be number of vCPUs per VM.
                            MaxTasksPerComputeNode = 16,

                            // We only use one pool, so number of nodes per pool will be total number of vCPUs (as specified by the user)
                            // divided by number of vCPUs per VM. We've hardcoded VM size to standard_d5_v2, which has 16 vCPUs.
                            TargetDedicatedComputeNodes = job.CpuCount / 16,
                        }
                    }
                };
            }
            return new PoolInformation
            {
                // Should never be true - we never modify AzureSettings.Default.PoolName
                PoolId = AzureSettings.Default.PoolName
            };
        }
    }
}
