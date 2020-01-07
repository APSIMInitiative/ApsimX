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

namespace ApsimNG.Cloud.Azure
{
    public class AzureInterface : ICloudInterface
    {
        private BatchClient batchClient;

        private CloudBlobClient blobClient;

        public AzureInterface()
        {
            AzureCredentialsSetup.GetCredentialsIfNotExist();

            // Setup Azure batch/storage clients using the given credentials.
            SetupClient();
        }

        /// <summary>
        /// List all Azure jobs.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        public async Task<JobDetails> ListJobs(CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Submit a job to be run on Azure.
        /// </summary>
        /// <param name="job">Job parameters.</param>
        /// <param name="UpdateStatus">Action which will display job submission status to the user.</param>
        public async Task SubmitJob(JobParameters job, Action<string> UpdateStatus)
        {
            // Initialise a working directory.
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

                // Save a config file in the job directory that has the email settings.
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
            CloudJob cloudJob = batchClient.JobOperations.CreateJob(job.ID.ToString(), GetPoolInfo(PoolSettings.FromConfiguration()));
            cloudJob.DisplayName = job.DisplayName;
            cloudJob.JobPreparationTask = ToJobPreparationTask(job, modelZipFileSas);
            cloudJob.JobReleaseTask = ToJobReleaseTask(job, modelZipFileSas);
            cloudJob.JobManagerTask = ToJobManagerTask(job);

            await cloudJob.CommitAsync();
            UpdateStatus("Job Successfully submitted");
        }

        private void SetupClient()
        {
            var credentials = new Microsoft.Azure.Storage.Auth.StorageCredentials(AzureSettings.Default.StorageAccount, AzureSettings.Default.StorageKey);
            var storageAccount = new CloudStorageAccount(credentials, true);
            blobClient = storageAccount.CreateCloudBlobClient();

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
            var containerRef = blobClient.GetContainerReference(containerName);
            await containerRef.CreateIfNotExistsAsync();
            containerRef.Metadata.Add(key, val);
            await containerRef.SetMetadataAsync();
        }

        /// <summary>
        /// Upload a file to Azure's cloud storage if it does not already exist.
        /// Return the URI and shared access signature of the uploaded blob.
        /// </summary>
        /// <param name="containerName">Name of the container to upload the file to</param>
        /// <param name="filePath">Path to the file on disk</param>
        private async Task<string> UploadFileIfNeededAsync(string containerName, string filePath)
        {
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
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
            var toolsRef = blobClient.GetContainerReference("jobmanager");
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

            var toolsRef = blobClient.GetContainerReference("tools");
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

            var apsimRef = blobClient.GetContainerReference("apsim");
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

        private PoolInformation GetPoolInfo(PoolSettings settings)
        {
            if (string.IsNullOrEmpty(settings.PoolName))
            {
                return new PoolInformation
                {
                    AutoPoolSpecification = new AutoPoolSpecification
                    {
                        PoolLifetimeOption = PoolLifetimeOption.Job,
                        PoolSpecification = new PoolSpecification
                        {
                            MaxTasksPerComputeNode = settings.MaxTasksPerVM,

                            // This specifies the OS that our VM will be running.
                            // OS Family 5 means .NET 4.6 will be installed.
                            // For more info see https://docs.microsoft.com/en-us/azure/cloud-services/cloud-services-guestos-update-matrix#releases
                            CloudServiceConfiguration = new CloudServiceConfiguration("5"),
                            ResizeTimeout = TimeSpan.FromMinutes(15),
                            TargetDedicatedComputeNodes = settings.VMCount,
                            VirtualMachineSize = settings.VMSize,
                            TaskSchedulingPolicy = new TaskSchedulingPolicy(ComputeNodeFillType.Spread)
                        }
                    }
                };
            }
            return new PoolInformation
            {
                PoolId = settings.PoolName
            };
        }
    }
}
