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
        public async Task SubmitJobs(JobParameters job, Action<string> UpdateStatus)
        {
            SetupClient();

            job.ID = Guid.NewGuid();
            job.CoresPerProcess = 1;
            job.JobManagerShouldSubmitTasks = true;
            job.AutoScale = true;

            // Our default VM size is the standard_d5_v2.
            // This VM has 16 vCPUs and 56 GiB of memory.
            // Therefore, default max tasks per VM is 16 (1 per core).
            job.PoolMaxTasksPerVM = 16;

            SetAzureMetaData("job-" + job.ID, "Owner", Environment.UserName.ToLower());

            // if jp.ApplicationPackagePath is a directory it will need to be zipped up
            if (Directory.Exists(job.ApsimXPath))
            {
                UpdateStatus("Compressing APSIM binaries...");

                string zipFile = Path.Combine(Path.GetTempPath(), $"Apsim-tmp-X-{Environment.UserName.ToLower()}.zip");
                if (File.Exists(zipFile))
                    File.Delete(zipFile);

                CreateApsimXZip(job.ApsimXPath, zipFile);

                job.ApsimXPath = zipFile;
                job.ApsimXVersion = Path.GetFileName(zipFile).Substring(Path.GetFileName(zipFile).IndexOf('-') + 1);
            }

            // TODO : do we actually need/use the APSIMJob class?
            APSIMJob apsimJob = new APSIMJob(job.DisplayName, "", job.ApsimXPath, job.ApsimXVersion, job.EmailRecipient, PoolSettings.FromConfiguration());
            apsimJob.PoolInfo.MaxTasksPerVM = job.PoolMaxTasksPerVM;
            apsimJob.PoolInfo.VMCount = job.PoolVMCount;

            // Upload tools such as 7zip, AzCopy, CMail, etc.
            UpdateStatus("Uploading tools...");
            string executableDirectory = Path.GetDirectoryName(GetType().Assembly.Location);
            string toolsDir = Path.Combine(executableDirectory, "tools");
            if (!Directory.Exists(toolsDir))
                throw new Exception("Tools Directory not found: " + toolsDir);

            foreach (string filePath in Directory.EnumerateFiles(toolsDir))
                await UploadFileIfNeeded("tools", filePath);

            if (job.SendEmail)
            {
                try
                {
                    // Save a config file in the job directory that has the email settings.
                    string tmpConfig = Path.Combine(Path.GetTempPath(), "settings.txt");
                    using (StreamWriter file = new StreamWriter(tmpConfig))
                    {
                        file.WriteLine("EmailRecipient=" + job.EmailRecipient);
                        file.WriteLine("EmailSender=" + AzureSettings.Default["EmailSender"]);
                        file.WriteLine("EmailPW=" + AzureSettings.Default["EmailPW"]);
                    }

                    await UploadFileIfNeeded("job-" + job.ID, tmpConfig);
                    File.Delete(tmpConfig);
                }
                catch (Exception err)
                {
                    throw new Exception("Error writing to settings file", err);
                }
            }

            // Upload job manager.
            UpdateStatus("Uploading job manager");
            await UploadFileIfNeeded("jobmanager", Path.Combine(executableDirectory, "azure-apsim.exe"));

            // Upload apsim.
            UpdateStatus("Uploading APSIM Next Generation");
            await UploadFileIfNeeded("apsim", job.ApsimXPath);

            // Generate model files.
            UpdateStatus("Generating model files");
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

            Runner run = new Runner(job.Model);
            GenerateApsimXFiles.Generate(run, job.ModelPath, p => { /* Don't bother with progress reporting */ });

            // zip up models directory
            if (!Directory.Exists(job.ModelPath)) // this test may be unnecessary
                throw new Exception($"Directory does not exist: {job.ModelPath}");

            string tmpZip = GetTempFileName("Model-", ".zip", true);
            ZipFile.CreateFromDirectory(job.ModelPath, tmpZip, CompressionLevel.Fastest, false);
            job.ModelPath = tmpZip;

            // Upload models.
            UpdateStatus("Uploading models");
            apsimJob.ModelZipFileSas = UploadFile(job.ModelPath, job.ID.ToString(), Path.GetFileName(job.ModelPath));

            // Clean up temporary model files.
            if (File.Exists(tmpZip))
                File.Delete(tmpZip);

            if (!job.SaveModelFiles)
            {
                if (Directory.Exists(job.ModelPath))
                    Directory.Delete(job.ModelPath, true);
                else if (File.Exists(job.ModelPath))
                    File.Delete(job.ModelPath);
            }

            // Submit job.
            UpdateStatus("Submitting Job");
            CloudJob cloudJob = batchClient.JobOperations.CreateJob(job.ID.ToString(), GetPoolInfo(apsimJob.PoolInfo));
            cloudJob.DisplayName = apsimJob.DisplayName;
            cloudJob.JobPreparationTask = apsimJob.ToJobPreparationTask(job.ID, blobClient);
            cloudJob.JobReleaseTask = apsimJob.ToJobReleaseTask(job.ID, blobClient);
            cloudJob.JobManagerTask = apsimJob.ToJobManagerTask(job.ID, blobClient, job.JobManagerShouldSubmitTasks, job.AutoScale);

            cloudJob.Commit();
            UpdateStatus("Job Successfully submitted");
        }

        private void SetupClient()
        {
            var credentials = new Microsoft.Azure.Storage.Auth.StorageCredentials(AzureSettings.Default.StorageAccount, AzureSettings.Default.StorageKey);
            var storageAccount = new CloudStorageAccount(credentials, true);
            blobClient = BlobAccountExtensions.CreateCloudBlobClient(storageAccount);

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
        private void SetAzureMetaData(string containerName, string key, string val)
        {
            var credentials = new Microsoft.Azure.Storage.Auth.StorageCredentials(
                (string)AzureSettings.Default["StorageAccount"],
                (string)AzureSettings.Default["StorageKey"]);

            var storageAccount = new CloudStorageAccount(credentials, true);
            var blobClient = Microsoft.Azure.Storage.Blob.BlobAccountExtensions.CreateCloudBlobClient(storageAccount);
            var containerRef = blobClient.GetContainerReference(containerName);
            containerRef.CreateIfNotExists();
            containerRef.Metadata.Add(key, val);
            containerRef.SetMetadata();
        }

        /// <summary>
        /// Zips up a directory containing ApsimX
        /// </summary>
        /// <param name="srcPath">Path of the ApsimX directory</param>
        /// <param name="zipPath">Path to which the zip file will be saved</param>
        /// <returns>0 if successful, 1 otherwise</returns>
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
        /// Upload a file to Azure's cloud storage if it does not already exist.
        /// </summary>
        /// <param name="containerName">Name of the container to upload the file to</param>
        /// <param name="filePath">Path to the file on disk</param>
        private async Task UploadFileIfNeeded(string containerName, string filePath)
        {
            CloudBlobContainer container = blobClient.GetContainerReference(containerName);
            await container.CreateIfNotExistsAsync();
            CloudBlockBlob blobRef = container.GetBlockBlobReference(Path.GetFileName(filePath));

            string md5 = GetFileMd5(filePath);
            
            // if blob exists and md5 matches, there is no need to upload the file
            if (blobRef.Exists() && string.Equals(md5, blobRef.Properties.ContentMD5, StringComparison.InvariantCultureIgnoreCase))
                return;

            blobRef.Properties.ContentMD5 = md5;
            await blobRef.UploadFromFileAsync(filePath);
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
        /// Gets a temp file with a given extension, checks it can be created, possibly removes it and returns the path
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="extension"></param>
        /// <param name="removeFile">Whether the file should be removed</param>
        /// <returns></returns>
        private string GetTempFileName(string prefix, string extension, bool removeFile)
        {
            int attempt = 0;
            while (true)
            {
                string fileName = Path.GetRandomFileName();
                fileName = Path.ChangeExtension(fileName, extension);
                fileName = Path.Combine(Path.GetTempPath(), prefix + fileName);

                try
                {
                    using (new FileStream(fileName, FileMode.CreateNew)) { }
                    if (removeFile) File.Delete(fileName);
                    return fileName;
                }
                catch (IOException err)
                {
                    if (++attempt == 10)
                        throw new Exception("No unique temporary file name is available", err);
                }
            }
        }

        /// <summary>
        /// Uploads a file to an Azure container.
        /// </summary>
        /// <param name="filePath">Path of the local file to be uploaded.</param>
        /// <param name="container">Container to upload the file to.</param>
        /// <param name="remoteFileName">Name of the remote file (once it has been uploaded). Name of the local file will not be changed.</param>
        /// <returns></returns>
        public string UploadFile(string filePath, string container, string remoteFileName)
        {
            // retry connection every 3 seconds
            //blobClient.DefaultRequestOptions.RetryPolicy = new LinearRetry(TimeSpan.FromSeconds(3), 10);

            CloudBlobContainer containerRef = blobClient.GetContainerReference(container);
            containerRef.CreateIfNotExists();

            CloudBlockBlob blob = containerRef.GetBlockBlobReference(remoteFileName);
            if (BlobNeedsUploading(blob, filePath))
                blob.UploadFromFileAsync(filePath, new AccessCondition(), new BlobRequestOptions { ParallelOperationThreadCount = 8, StoreBlobContentMD5 = true }, null).Wait();

            var policy = new SharedAccessBlobPolicy
            {
                Permissions = SharedAccessBlobPermissions.Read,
                SharedAccessStartTime = DateTime.UtcNow.AddMinutes(-15),
                SharedAccessExpiryTime = DateTime.UtcNow.AddMonths(12)
            };
            return blob.Uri.AbsoluteUri + blob.GetSharedAccessSignature(policy);
        }

        /// <summary>
        /// Checks if a blob actually needs to be uploaded to a path.
        /// Returns false if a blob with the same MD5 already exists at the given path.
        /// Returns true otherwise.
        /// </summary>
        /// <param name="blob">Blob to be uploaded.</param>
        /// <param name="filePath">Path for the blob to be uploaded to.</param>
        /// <returns></returns>
        private bool BlobNeedsUploading(CloudBlockBlob blob, string filePath)
        {
            if (blob.Exists())
            {
                blob.FetchAttributes();

                if (blob.Properties.ContentMD5 != null)
                {
                    string localMD5 = GetFileMd5(filePath);
                    if (blob.Properties.ContentMD5 == localMD5) return false;
                }
            }
            return true;
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
