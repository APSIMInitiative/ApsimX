using APSIM.Shared.Utilities;
using ApsimNG.Cloud.Azure;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Auth;
using Microsoft.Azure.Batch.Common;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
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

namespace ApsimNG.Cloud
{
    /// <summary>
    /// This class handles communications with Microsoft's Azure API.
    /// </summary>
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
            AzureCredentialsSetup.GetCredentialsIfNotExist(Initialise);
        }

        /// <summary>
        /// Initialise the batch and storage clients for communication with Microsoft's APIs.
        /// </summary>
        private void Initialise()
        {
            Licence licence = new Licence(AzureSettings.Default.LicenceFilePath);

            // Setup Azure batch/storage clients using the given credentials.
            var credentials = new Microsoft.Azure.Storage.Auth.StorageCredentials(licence.StorageAccount, licence.StorageKey);
            var storageAccount = new CloudStorageAccount(credentials, true);
            storageClient = storageAccount.CreateCloudBlobClient();

            var sharedCredentials = new BatchSharedKeyCredentials(licence.BatchUrl, licence.BatchAccount, licence.BatchKey);
            batchClient = BatchClient.Open(sharedCredentials);
        }

        /// <summary>
        /// Submit a job to be run on Azure.
        /// </summary>
        /// <param name="job">Job parameters.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <param name="UpdateStatus">Action which will display job submission status to the user.</param>
        public async Task SubmitJobAsync(JobParameters job, CancellationToken ct, Action<string> UpdateStatus)
        {
            if (batchClient == null || storageClient == null)
                throw new Exception("Unable to submit job to Azure: no credentials provided");

            // Initialise a working directory.
            UpdateStatus("Initialising job environment...");
            string workingDirectory = Path.Combine(Path.GetTempPath(), job.ID.ToString());
            Directory.CreateDirectory(workingDirectory);

            // Set job owner.
            string owner = Environment.UserName.ToLower();
            await SetAzureMetaDataAsync("job-" + job.ID, "Owner", owner, ct);

            if (ct.IsCancellationRequested)
            {
                UpdateStatus("Cancelled");
                return;
            }

            // If the ApsimX path is a directory it will need to be compressed.
            if (Directory.Exists(job.ApsimXPath))
            {
                UpdateStatus("Compressing APSIM Next Generation...");

                string zipFile = Path.Combine(workingDirectory, $"Apsim-tmp-X-{owner}.zip");
                if (File.Exists(zipFile))
                    File.Delete(zipFile);

                CreateApsimXZip(job.ApsimXPath, zipFile, ct);

                job.ApsimXPath = zipFile;
                job.ApsimXVersion = Path.GetFileName(zipFile).Substring(Path.GetFileName(zipFile).IndexOf('-') + 1);
            }

            if (ct.IsCancellationRequested)
            {
                UpdateStatus("Cancelled");
                return;
            }

            // Upload tools such as 7zip, AzCopy, CMail, etc.
            UpdateStatus("Uploading tools...");
            string executableDirectory = Path.GetDirectoryName(GetType().Assembly.Location);
            string toolsDir = Path.Combine(executableDirectory, "tools");
            if (!Directory.Exists(toolsDir))
                throw new Exception("Tools Directory not found: " + toolsDir);

            foreach (string filePath in Directory.EnumerateFiles(toolsDir))
            {
                await UploadFileIfNeededAsync("tools", filePath, ct);

                if (ct.IsCancellationRequested)
                {
                    UpdateStatus("Cancelled");
                    return;
                }
            }

            // Upload email config file.
            if (job.SendEmail)
            {
                Licence licence = new Licence(AzureSettings.Default.LicenceFilePath);
                StringBuilder config = new StringBuilder();
                config.AppendLine($"EmailRecipient={job.EmailRecipient}");
                config.AppendLine($"EmailSender={licence.EmailSender}");
                config.AppendLine($"EmailPW={licence.EmailPW}");

                // Write these settings to a temporary config file.
                string configFile = Path.Combine(workingDirectory, "settings.txt");
                File.WriteAllText(configFile, config.ToString());

                await UploadFileIfNeededAsync("job-" + job.ID, configFile, ct);
                File.Delete(configFile);
            }

            if (ct.IsCancellationRequested)
            {
                UpdateStatus("Cancelled");
                return;
            }

            // Upload job manager.
            UpdateStatus("Uploading job manager...");
            await UploadFileIfNeededAsync("jobmanager", Path.Combine(executableDirectory, "azure-apsim.exe"), ct);

            if (ct.IsCancellationRequested)
            {
                UpdateStatus("Cancelled");
                return;
            }

            // Upload apsim.
            UpdateStatus("Uploading APSIM Next Generation...");
            await UploadFileIfNeededAsync("apsim", job.ApsimXPath, ct);

            if (ct.IsCancellationRequested)
            {
                UpdateStatus("Cancelled");
                return;
            }

            // Generate model files.
            UpdateStatus("Generating model files...");
            if (!Directory.Exists(job.ModelPath))
                Directory.CreateDirectory(job.ModelPath);

            if (ct.IsCancellationRequested)
            {
                UpdateStatus("Cancelled");
                return;
            }

            // Generate .apsimx file for each simulation to be run.
            Runner run = new Runner(job.Model);
            GenerateApsimXFiles.Generate(run, job.ModelPath, p => { /* Don't bother with progress reporting */ }, collectExternalFiles:true);

            if (ct.IsCancellationRequested)
            {
                UpdateStatus("Cancelled");
                return;
            }

            // Compress model (.apsimx file) directory.
            UpdateStatus("Compressing model files...");
            string tmpZip = Path.Combine(workingDirectory, $"Model-{Guid.NewGuid()}.zip");
            ZipFile.CreateFromDirectory(job.ModelPath, tmpZip, CompressionLevel.Fastest, false);
            job.ModelPath = tmpZip;

            if (ct.IsCancellationRequested)
            {
                UpdateStatus("Cancelled");
                return;
            }

            // Upload models.
            UpdateStatus("Uploading model files...");
            string modelZipFileSas = await UploadFileIfNeededAsync(job.ID.ToString(), job.ModelPath, ct);

            if (ct.IsCancellationRequested)
            {
                UpdateStatus("Cancelled");
                return;
            }

            // Clean up temp files.
            UpdateStatus("Deleting temp files...");
            Directory.Delete(workingDirectory, true);

            // Submit job.
            UpdateStatus("Submitting Job...");
            CloudJob cloudJob = batchClient.JobOperations.CreateJob(job.ID.ToString(), GetPoolInfo(job));
            cloudJob.DisplayName = job.DisplayName;
            cloudJob.JobPreparationTask = CreateJobPreparationTask(job, modelZipFileSas);
            cloudJob.JobReleaseTask = CreateJobReleaseTask(job, modelZipFileSas);
            cloudJob.JobManagerTask = CreateJobManagerTask(job);

            await cloudJob.CommitAsync(cancellationToken: ct);
            UpdateStatus("Job Successfully submitted");
        }

        /// <summary>
        /// List all Azure jobs.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        /// <param name="ShowProgress">Function to report progress as percentage in range [0, 100].</param>
        /// <param name="AddJobHandler">Callback which will be run each time a job is loaded.</param>
        public async void ListJobsAsync(CancellationToken ct, Action<double> ShowProgress, Action<JobDetails> AddJobHandler)
        {
            try
            {
                if (batchClient == null || storageClient == null)
                    return;

                ShowProgress(0);

                ODATADetailLevel jobDetailLevel = new ODATADetailLevel { SelectClause = "id,displayName,state,executionInfo,stats", ExpandClause = "stats" };

                // Download raw job list via the Azure API.
                List<CloudJob> cloudJobs = await batchClient.JobOperations.ListJobs(jobDetailLevel).ToListAsync(ct);

                if (ct.IsCancellationRequested)
                    return;

                // Parse jobs into a list of JobDetails objects.
                for (int i = 0; i < cloudJobs.Count; i++)
                {
                    if (ct.IsCancellationRequested)
                        return;

                    ShowProgress(100.0 * i / cloudJobs.Count);
                    AddJobHandler(await GetJobDetails(cloudJobs[i], ct));
                }
            }
            finally
            {
                ShowProgress(100);
            }
        }

        /// <summary>
        /// Halt the execution of a job.
        /// </summary>
        /// <param name="jobID">ID of the job.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task StopJobAsync(string jobID, CancellationToken ct)
        {
            await batchClient.JobOperations.TerminateJobAsync(jobID, cancellationToken: ct);
        }

        /// <summary>
        /// Delete a job and all cloud storage associated with the job.
        /// </summary>
        /// <param name="jobID">ID of the job.</param>
        /// <param name="ct">Cancellation token.</param>
        public async Task DeleteJobAsync(string jobID, CancellationToken ct)
        {
            Guid parsedID = Guid.Parse(jobID);

            // Delete cloud storage associated with the job.
            await storageClient.GetContainerReference(StorageConstants.GetJobOutputContainer(parsedID)).DeleteIfExistsAsync(ct);
            if (ct.IsCancellationRequested)
                return;

            await storageClient.GetContainerReference(StorageConstants.GetJobContainer(parsedID)).DeleteIfExistsAsync(ct);
            if (ct.IsCancellationRequested)
                return;

            await storageClient.GetContainerReference(jobID).DeleteIfExistsAsync(ct);
            if (ct.IsCancellationRequested)
                return;

            // Delete the job.
            await batchClient.JobOperations.DeleteJobAsync(jobID, cancellationToken: ct);
        }

        /// <summary>
        /// Download the results of a job.
        /// </summary>
        /// <param name="options">Download options.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <param name="ShowProgress">Function which reports progress (in range [0, 1]) to the user.</param>
        public async Task DownloadResultsAsync(DownloadOptions options, CancellationToken ct, Action<double> ShowProgress)
        {
            // Determine what the resulting .zip file will be called.
            var resultsZip = Path.Combine(options.Path, options.Name + ".zip");
            resultsZip = DirectoryUtilities.EnsureFileNameIsUnique(resultsZip);

            // Create a temporary workarea which will be zipped later.
            var resultTempDirectory = Path.Combine(options.Path, Path.GetFileNameWithoutExtension(resultsZip));
            if (!Directory.Exists(resultTempDirectory))
                Directory.CreateDirectory(resultTempDirectory);

            // Get a list of outputs generated by cloud run.
            var outputs = await GetJobOutputs(options.JobID, ct);
            if (outputs != null && outputs.Count > 0)
            {
                // First try and get just the results.zip
                var resultsZipFile = outputs.FindAll(output => output.Name == "Results.zip");
                await DownloadAndProcessFiles(resultsZipFile, ShowProgress, resultTempDirectory, ct);

                var resultsDB = Path.Combine(resultTempDirectory, "Results.db");
                if (!File.Exists(resultsDB))
                {
                    // Cound't find results.db file so download everything to provide debug info to user.
                    await DownloadAndProcessFiles(outputs, ShowProgress, resultTempDirectory, ct);
                }

                // Zip up resulting temp folder.
                using (ZipArchive zip = ZipFile.Open(resultsZip, ZipArchiveMode.Create, Encoding.UTF8))
                {
                    foreach (string fileName in Directory.GetFiles(resultTempDirectory))
                        zip.CreateEntryFromFile(fileName, Path.GetFileName(fileName));
                }

                // Delete temp folder.
                Directory.Delete(resultTempDirectory, true);
            }
            else
                throw new Exception($"No results were found. Is this an APSIM 7.10 run?");
        }

        /// <summary>
        /// Download one or more files and process the .zip and .db files.
        /// </summary>
        /// <param name="outputs"></param>
        /// <param name="ShowProgress"></param>
        /// <param name="resultTempDirectory"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private static async Task DownloadAndProcessFiles(List<CloudBlockBlob> outputs, Action<double> ShowProgress, string resultTempDirectory, CancellationToken ct)
        {
            // Download all run outputs.
            for (int i = 0; i < outputs.Count; i++)
            {
                ShowProgress(i / outputs.Count);
                CloudBlockBlob blob = outputs[i];

                // todo: Download in parallel?
                var fullFileName = Path.Combine(resultTempDirectory, blob.Name);
                await blob.DownloadToFileAsync(fullFileName, FileMode.Create, ct);

                if (ct.IsCancellationRequested)
                    return;
            }
            ShowProgress(100);

            // Extract any zip files.
            foreach (var zipFileName in Directory.GetFiles(resultTempDirectory, "*.zip"))
            {
                if (Path.GetFileName(zipFileName) != "model.zip")
                {
                    // Extract the result files.
                    using (ZipArchive zip = ZipFile.Open(zipFileName, ZipArchiveMode.Read, Encoding.UTF8))
                        zip.ExtractToDirectory(resultTempDirectory);
                }
            }

            try
            {
                // Merge results into a single .db file.
                var dbFiles = Directory.GetFiles(resultTempDirectory, "*.db");
                var resultsDB = Path.Combine(resultTempDirectory, "Results.db");
                DBMerger.MergeFiles(Path.Combine(resultTempDirectory, "*.db"), false, resultsDB);
                if (File.Exists(resultsDB))
                {
                    // Remove the individual .db files.
                    foreach (string dbFileName in dbFiles)
                        File.Delete(dbFileName);

                    // Delete the zip file containing the .db files.
                    foreach (var zipFileName in Directory.GetFiles(resultTempDirectory, "*.zip"))
                        if (Path.GetFileName(zipFileName) != "model.zip")
                            File.Delete(zipFileName);
                }
            }
            catch (Exception err)
            {
                throw new Exception($"Results were successfully extracted to {resultTempDirectory} but an error wasn encountered while attempting to merge the individual .db files", err);
            }
        }

        /// <summary>
        /// Lists all output files of a given job.
        /// </summary>
        /// <param name="jobID">Job ID.</param>
        /// <param name="ct">Cancellation token.</param>
        private async Task<List<CloudBlockBlob>> GetJobOutputs(Guid jobID, CancellationToken ct)
        {
            CloudBlobContainer containerRef = storageClient.GetContainerReference(StorageConstants.GetJobOutputContainer(jobID));
            if (!await containerRef.ExistsAsync(ct) || ct.IsCancellationRequested)
                return null;

            return await containerRef.ListBlobsAsync(ct);
        }

        /// <summary>
        /// Sets metadata for a particular container in Azure's cloud storage.
        /// </summary>
        /// <param name="containerName">Name of the container</param>
        /// <param name="key">Metadata key/name</param>
        /// <param name="val">Data to write</param>
        /// <param name="ct">Cancellation token.</param>
        private async Task SetAzureMetaDataAsync(string containerName, string key, string val, CancellationToken ct)
        {
            var containerRef = storageClient.GetContainerReference(containerName);
            await containerRef.CreateIfNotExistsAsync(ct);
            if (ct.IsCancellationRequested)
                return;

            containerRef.Metadata.Add(key, val);
            await containerRef.SetMetadataAsync(ct);
        }

        /// <summary>Gets metadata value for a container in Azure cloud storage.</summary>
        /// <param name="containerName">Name of the container.</param>
        /// <param name="key">Metadata key.</param>
        /// <param name="ct">Cancellation token.</param>
        private async Task<string> GetContainerMetaDataAsync(string containerName, string key, CancellationToken ct)
        {
            CloudBlobContainer containerRef = storageClient.GetContainerReference(containerName);
            if (!await containerRef.ExistsAsync(ct))
                throw new Exception($"Failed to fetch metadata '{key}' for container '{containerName}' - container does not exist");

            if (ct.IsCancellationRequested)
                return null;

            await containerRef.FetchAttributesAsync(ct);
            if (containerRef.Metadata.ContainsKey(key))
                return containerRef.Metadata[key];

            if (ct.IsCancellationRequested)
                return null;

            throw new Exception($"Failed to fetch metadata '{key}' for container '{containerName}' - key does not exist");
        }

        /// <summary>
        /// Upload a file to Azure's cloud storage if it does not already exist.
        /// Return the URI and shared access signature of the uploaded blob.
        /// </summary>
        /// <param name="containerName">Name of the container to upload the file to</param>
        /// <param name="filePath">Path to the file on disk</param>
        /// <param name="ct">Allows for cancellation of the task.</param>
        private async Task<string> UploadFileIfNeededAsync(string containerName, string filePath, CancellationToken ct)
        {
            CloudBlobContainer container = storageClient.GetContainerReference(containerName);
            await container.CreateIfNotExistsAsync(ct);
            CloudBlockBlob blob = container.GetBlockBlobReference(Path.GetFileName(filePath));

            string md5 = GetFileMd5(filePath);
            
            // If blob already exists and md5 matches, there is no need to upload the file.
            if (await blob.ExistsAsync(ct) && string.Equals(md5, blob.Properties.ContentMD5, StringComparison.InvariantCultureIgnoreCase))
                return null;

            blob.Properties.ContentMD5 = md5;
            await blob.UploadFromFileAsync(filePath, ct);

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
        /// <param name="cloudJob">The Azure ClouJob object.</param>
        /// <param name="ct">Cancellation token.</param>
        private async Task<JobDetails> GetJobDetails(CloudJob cloudJob, CancellationToken ct)
        {
            try
            {
                if (!await storageClient.GetContainerReference($"job-{cloudJob.Id}").ExistsAsync(ct))
                {
                    await DeleteJobAsync(cloudJob.Id, ct);
                    return null;
                }
                string owner = await GetContainerMetaDataAsync($"job-{cloudJob.Id}", "Owner", ct);

                TaskCounts tasks = await batchClient.JobOperations.GetJobTaskCountsAsync(cloudJob.Id, cancellationToken: ct);
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
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Create a job preparation task for an azure job. The job preparation task
        /// will run on each compute node (VM) before any other tasks are run.
        /// </summary>
        /// <param name="job">Job parameters.</param>
        /// <param name="sas">Shared access signature for the model files.</param>
        private JobPreparationTask CreateJobPreparationTask(JobParameters job, string sas)
        {
            return new JobPreparationTask
            {
                CommandLine = "cmd.exe /c jobprep.cmd",
                ResourceFiles = GetJobPrepResourceFiles(sas, job.ApsimXVersion).ToList(),
                WaitForSuccess = true
            };
        }

        /// <summary>
        /// Create a job release task for an azure job. The job release task will run
        /// on each compute node after the job has finished running.
        /// </summary>
        /// <param name="job">Job parameters.</param>
        /// <param name="sas">Shared access signature for the model files.</param>
        private JobReleaseTask CreateJobReleaseTask(JobParameters job, string sas)
        {
            Licence licence = new Licence(AzureSettings.Default.LicenceFilePath);
            return new JobReleaseTask
            {
                CommandLine = "cmd.exe /c jobrelease.cmd",
                ResourceFiles = GetJobPrepResourceFiles(sas, job.ApsimXVersion).ToList(),
                EnvironmentSettings = new[]
                {
                    new EnvironmentSetting("APSIM_STORAGE_ACCOUNT", licence.StorageAccount),
                    new EnvironmentSetting("APSIM_STORAGE_KEY", licence.StorageKey),
                    new EnvironmentSetting("JOBNAME", job.DisplayName),
                    new EnvironmentSetting("RECIPIENT", job.EmailRecipient)
                }
            };
        }

        /// <summary>
        /// Create a job manager task for an azure job. The job manager task controls
        /// creation and distribution of tasks which get run on the compute nodes.
        /// </summary>
        /// <param name="job">Job parameters.</param>
        private JobManagerTask CreateJobManagerTask(JobParameters job)
        {
            Licence licence = new Licence(AzureSettings.Default.LicenceFilePath);
            var cmd = string.Format("cmd.exe /c {0} job-manager {1} {2} {3} {4} {5} {6} {7} {8} {9}",
                BatchConstants.GetJobManagerPath(job.ID),
                licence.BatchUrl,
                licence.BatchAccount,
                licence.BatchKey,
                licence.StorageAccount,
                licence.StorageKey,
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

        /// <summary>
        /// Get a list of resource files which will be required by the job manager.
        /// </summary>
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
        /// Get a list of resource files which will be required by job preparation
        /// and release tasks (e.g. AZCopy, 7zip, etc).
        /// </summary>
        /// <param name="modelZipFileSas">Shared access signature of the model files.</param>
        /// <param name="version">Version of apsim being run.</param>
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
        /// <param name="ct">Cancellation token.</param>
        private void CreateApsimXZip(string srcPath, string zipPath, CancellationToken ct)
        {
            try
            {
                string bin = srcPath;
                if (!bin.EndsWith("Bin"))
                    bin = Path.Combine(srcPath, "Bin");
                string[] extensions = new string[] { "*.dll", "*.exe" };

                using (FileStream zipToOpen = new FileStream(zipPath, FileMode.Create))
                {
                    using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update, true))
                    {
                        foreach (string extension in extensions)
                        {
                            foreach (string fileName in Directory.EnumerateFiles(bin, extension))
                            {
                                archive.CreateEntryFromFile(fileName, Path.GetFileName(fileName));
                                if (ct.IsCancellationRequested)
                                    return;
                            }
                        }
                    }
                }
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

        /// <summary>
        /// This function controls how the Azure pools/VMs are setup.
        /// This is not really controllable by the user but probably should be.
        /// </summary>
        /// <param name="job">Job parameters.</param>
        private PoolInformation GetPoolInfo(JobParameters job)
        {
            var autoPoolSpecification = new AutoPoolSpecification
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
                    MaxTasksPerComputeNode = 16
                }
            };

            // We only use one pool, so number of nodes per pool will be total number of vCPUs (as specified by the user)
            // divided by number of vCPUs per VM. We've hardcoded VM size to standard_d5_v2, which has 16 vCPUs.
            if (job.LowPriority)
                autoPoolSpecification.PoolSpecification.TargetLowPriorityComputeNodes = job.CpuCount / 16;
            else
                autoPoolSpecification.PoolSpecification.TargetDedicatedComputeNodes = job.CpuCount / 16;

            return new PoolInformation { AutoPoolSpecification = autoPoolSpecification};
        }
    }
}
