using System;
using System.Text;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using UserInterface.Views;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Storage;
using System.Security.Cryptography;
using ApsimNG.Cloud;
using Microsoft.Azure.Batch.Common;
using Models.Core;
using System.Linq;

namespace UserInterface.Presenters
{
    public class NewAzureJobPresenter : IPresenter, INewCloudJobPresenter
    {
        /// <summary>The new azure job view</summary>
        private NewAzureJobView view;
        
        /// <summary>The explorer presenter</summary>
        private ExplorerPresenter presenter;

        /// <summary>
        /// The node which we want to run on Azure.
        /// </summary>
        private IModel model;

        /// <summary>
        /// The file uploader client.
        /// </summary>
        private FileUploader uploader;

        /// <summary>
        /// The Azure Batch client.
        /// </summary>
        private BatchClient batchCli;

        /// <summary>
        /// The Azure Storage account.
        /// </summary>
        private CloudStorageAccount storageAccount;

        /// <summary>
        /// The Azure Storage credentials (account name + key).
        /// </summary>
        private StorageCredentials storageAuth;

        /// <summary>
        /// The Azure Batch credentials (account name + key).
        /// </summary>
        private BatchCredentials batchAuth;

        /// <summary>
        /// The worker which will submit the job.
        /// </summary>
        private BackgroundWorker submissionWorker;

        /// <summary>
        /// The settings file name. This is uploaded to Azure, and stores some information
        /// used by the Azure APSIM job manager (azure-apsim.exe).
        /// </summary>
        private const string settingsFileName = "settings.txt";

        /// <summary>
        /// Default constructor.
        /// </summary>
        public NewAzureJobPresenter()
        {
        }

        /// <summary>
        /// Attaches this presenter to a view.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="view"></param>
        /// <param name="parentPresenter"></param>
        public void Attach(object model, object view, ExplorerPresenter parentPresenter)
        {
            this.presenter = parentPresenter;
            this.view = (NewAzureJobView)view;
            
            this.view.Presenter = this;
            
            GetCredentials(null, null);
            this.model = (IModel)model;
            this.view.JobName = this.model.Name;
            

            submissionWorker = new BackgroundWorker();
            submissionWorker.DoWork += SubmitJob_DoWork;
            submissionWorker.WorkerSupportsCancellation = true;
        }

        /// <summary>
        /// Validates user input, saves their choices and starts the job submission in a separate thread.
        /// </summary>
        /// <param name="jp">Job Parameters.</param>
        public void SubmitJob(JobParameters jp)
        {
            if (jp.JobDisplayName.Length < 1)
            {
                ShowErrorMessage("A description is required");
                return;
            }

            if (jp.ApplicationPackagePath.Length < 1)
            {
                string msg = jp.ApsimFromDir ? "Invalid Apsim Directory" : "Invalid Apsim zip file";
                return;
            }

            if (! (Directory.Exists(jp.ApplicationPackagePath) || File.Exists(jp.ApplicationPackagePath)) )
            {
                ShowErrorMessage("File or Directory not found: " + jp.ApplicationPackagePath);
                return;
            }

            if (jp.CoresPerProcess.ToString().Length < 1)
            {
                ShowErrorMessage("Number of cores per CPU is a required field");
                return;
            }

            if (jp.SaveModelFiles && jp.ModelPath.Length < 0)
            {
                ShowErrorMessage("Invalid model output directory: " + jp.ModelPath);
                return;
            }
            if (!Directory.Exists(jp.ModelPath))
            {
                try
                {
                    Directory.CreateDirectory(jp.ModelPath);
                }
                catch (Exception err)
                {
                    ShowError(err);
                    return;
                }                
            }

            if (jp.OutputDir.Length < 1)
            {
                ShowErrorMessage("Invalid output directory.");
                return;
            }

            if (!Directory.Exists(jp.OutputDir))
            {
                try
                {
                    Directory.CreateDirectory(jp.OutputDir);
                }
                catch (Exception err)
                {
                    ShowError(err);
                    return;
                }
            }

            // save user's choices to ApsimNG.Properties.Settings            
            
            AzureSettings.Default["OutputDir"] = jp.OutputDir;
            AzureSettings.Default.Save();
            if (batchCli == null)
                GetCredentials(null, null);
            else
                submissionWorker.RunWorkerAsync(jp);
        }

        /// <summary>
        /// Cancels submission of a job and hides the right hand panel (which holds the new job view).
        /// </summary>
        public void CancelJobSubmission()
        {
            if (submissionWorker != null)
                submissionWorker.CancelAsync();
            presenter.HideRightHandPanel();
        }

        /// <summary>
        /// Displays an error message to the user.
        /// </summary>
        /// <param name="msg">Message to be displayed.</param>
        public void ShowError(Exception err)
        {
            presenter.MainPresenter.ShowError(err);
        }

        /// <summary>
        /// Displays an error message to the user.
        /// </summary>
        /// <param name="msg">Message to be displayed.</param>
        public void ShowErrorMessage(string msg)
        {
            presenter.MainPresenter.ShowError(msg);
        }

        public void Detach()
        {
        }

        /// <summary>
        /// Handles the bulk of the work for submitting the job to the cloud. 
        /// Zips up ApsimX (if necessary), uploads tools and ApsimX, 
        /// </summary>
        /// <param name="e">Event arg, containing the job parameters</param>
        private void SubmitJob_DoWork(object o, DoWorkEventArgs e)
        {   
            JobParameters jp = (JobParameters)e.Argument;
            jp.JobId = Guid.NewGuid();
            jp.CoresPerProcess = 1;
            jp.JobManagerShouldSubmitTasks = true;
            jp.AutoScale = true;
            jp.PoolMaxTasksPerVM = 16;
            string tmpZip = "";

            SetAzureMetaData("job-" + jp.JobId, "Owner", Environment.UserName.ToLower());

            // if jp.ApplicationPackagePath is a directory it will need to be zipped up
            if (Directory.Exists(jp.ApplicationPackagePath))
            {                
                view.Status = "Zipping APSIM";
                
                tmpZip = Path.Combine(Path.GetTempPath(), "Apsim-tmp-X-" + Environment.UserName.ToLower() + ".zip");
                if (File.Exists(tmpZip)) File.Delete(tmpZip);

                if (CreateApsimXZip(jp.ApplicationPackagePath, tmpZip) > 0)
                {
                    view.Status = "Cancelled";
                    return;
                }

                jp.ApplicationPackagePath = tmpZip;
                jp.ApplicationPackageVersion = Path.GetFileName(tmpZip).Substring(Path.GetFileName(tmpZip).IndexOf('-') + 1);
            }

            // add current job to the list of jobs                        

            // TODO : do we actually need/use the APSIMJob class?
            APSIMJob job = new APSIMJob(jp.JobDisplayName, "", jp.ApplicationPackagePath, jp.ApplicationPackageVersion, jp.Recipient, batchAuth, storageAuth, PoolSettings.FromConfiguration());
            job.PoolInfo.MaxTasksPerVM = jp.PoolMaxTasksPerVM;
            job.PoolInfo.VMCount = jp.PoolVMCount;


            // upload tools such as 7zip, AzCopy, CMail, etc.

            view.Status = "Checking tools";

            string executableDirectory = GetExecutableDirectory();
            string toolsDir = Path.Combine(executableDirectory, "tools");
            if (!Directory.Exists(toolsDir))
            {
                ShowErrorMessage("Tools Directory not found: " + toolsDir);
            }
            
            foreach (string filePath in Directory.EnumerateFiles(toolsDir))
            {
                UploadFileIfNeeded("tools", filePath);
            }

            if (jp.Recipient.Length > 0)
            {
                try
                {
                    // Store a config file into the job directory that has the e-mail config

                    string tmpConfig = Path.Combine(Path.GetTempPath(), settingsFileName);
                    using (StreamWriter file = new StreamWriter(tmpConfig))
                    {
                        file.WriteLine("EmailRecipient=" + jp.Recipient);
                        file.WriteLine("EmailSender=" + AzureSettings.Default["EmailSender"]);
                        file.WriteLine("EmailPW=" + AzureSettings.Default["EmailPW"]);
                    }

                    UploadFileIfNeeded("job-" + jp.JobId, tmpConfig);
                    File.Delete(tmpConfig);
                }
                catch (Exception err)
                {
                    ShowError(new Exception("Error writing to settings file; you may not receive an email upon job completion: ", err));
                }
            }

            // upload job manager            
            UploadFileIfNeeded("jobmanager", Path.Combine(executableDirectory, "azure-apsim.exe"));



            // upload apsim
            view.Status = "Uploading APSIM Next Generation";

            UploadFileIfNeeded("apsim", jp.ApplicationPackagePath);
            

            // generate model files

            view.Status = "Generating model files";
            if (!Directory.Exists(jp.ModelPath)) Directory.CreateDirectory(jp.ModelPath);

            try
            {
                // copy weather files to models directory to be zipped up
                foreach (Models.Weather child in Apsim.ChildrenRecursively(model).OfType<Models.Weather>())
                {
                    if (Path.GetDirectoryName(child.FullFileName) != Path.GetDirectoryName(presenter.ApsimXFile.FileName))
                    {
                        presenter.MainPresenter.ShowError("Weather file must be in the same directory as .apsimx file: " + child.FullFileName);
                        view.Status = "Cancelled";
                        return;
                    }
                    string sourceFile = child.FullFileName;
                    string destFile = Path.Combine(jp.ModelPath, child.FileName);
                    if (!File.Exists(destFile))
                        File.Copy(sourceFile, destFile); ;
                }
                // Generate .apsimx files, and if any errors are encountered, abort the job submission process.
                if (!presenter.GenerateApsimXFiles(model, jp.ModelPath))
                {
                    view.Status = "Cancelled";
                    return;
                }
            }
            catch (Exception err)
            {
                presenter.MainPresenter.ShowError(err);
                return;
            }

            tmpZip = "";

            // zip up models directory
            if (Directory.Exists(jp.ModelPath)) // this test may be unnecessary
            {
                tmpZip = GetTempFileName("Model-", ".zip", true);
                ZipFile.CreateFromDirectory(jp.ModelPath, tmpZip, CompressionLevel.Fastest, false);                
                jp.ModelPath = tmpZip;
            }

            // upload models

            view.Status = "Uploading models";
            job.ModelZipFileSas = uploader.UploadFile(jp.ModelPath, jp.JobId.ToString(), Path.GetFileName(jp.ModelPath));

            // clean up temporary model files
            if (File.Exists(tmpZip)) File.Delete(tmpZip);
            if (!jp.SaveModelFiles)
            {                
                if (Directory.Exists(jp.ModelPath)) Directory.Delete(jp.ModelPath);
            }
            
            view.Status = "Submitting Job";






            // submit job
            try
            {
                CloudJob cloudJob = batchCli.JobOperations.CreateJob(jp.JobId.ToString(), GetPoolInfo(job.PoolInfo));
                cloudJob.DisplayName = job.DisplayName;
                cloudJob.JobPreparationTask = job.ToJobPreparationTask(jp.JobId, Microsoft.Azure.Storage.Blob.BlobAccountExtensions.CreateCloudBlobClient(storageAccount));
                cloudJob.JobReleaseTask = job.ToJobReleaseTask(jp.JobId, Microsoft.Azure.Storage.Blob.BlobAccountExtensions.CreateCloudBlobClient(storageAccount));
                cloudJob.JobManagerTask = job.ToJobManagerTask(jp.JobId, Microsoft.Azure.Storage.Blob.BlobAccountExtensions.CreateCloudBlobClient(storageAccount), jp.JobManagerShouldSubmitTasks, jp.AutoScale);

                cloudJob.Commit();
            }
            catch (Exception err)
            {
                ShowError(err);
            }

            view.Status = "Job Successfully submitted";
            
            if (jp.AutoDownload)
            {
                AzureResultsDownloader dl = new AzureResultsDownloader(jp.JobId, jp.JobDisplayName, jp.OutputDir, null, true, jp.Summarise, true, true, true);
                dl.DownloadResults(true);
            }
        }

        /// <summary>
        /// Returns the path to the directory of the running executable.
        /// </summary>        
        private string GetExecutableDirectory()
        {
            string codeBase = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path);            
        }

        /// <summary>
        /// Checks the model output directory. If it already exists and contains files, user will receive a warning.
        /// </summary>
        /// <param name="path">Path to the model output directory.</param>
        /// <returns>1 if user chooses to abort the experiment, 0 otherwise.</returns>
        private int CheckSims(string path)
        {
            if (Directory.Exists(path))
            {
                if (Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly).Length > 0)
                {
                    ShowErrorMessage("There are already files in the output directory");                    
                }
            }
            return 0;
        }

        private int CreateSimOutputDir(string path)
        {
            try
            {
                Directory.CreateDirectory(path);
            }
            catch (Exception err)
            {
                ShowError(new Exception("Error: creation of simulation directory " + path + " failed: ", err));
                return 1;
            }
            return 0;
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
                    {
                       ShowError(new Exception("No unique temporary file name is available: ", err));
                    }
                }
            }
        }

        /// <summary>
        /// Sets metadata for a particular container in Azure's cloud storage.
        /// </summary>
        /// <param name="containerName">Name of the container</param>
        /// <param name="key">Metadata key/name</param>
        /// <param name="val">Data to write</param>
        private void SetAzureMetaData(string containerName, string key, string val)
        {
            try
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
            catch (Exception err)
            {                
                ShowError(err);
            }
        }

        /// <summary>
        /// Read Azure credentials from the file ApsimX\AzureAgR.lic
        /// This is a temporary measure - will probably need to allow user to specify a file.
        /// </summary>
        /// <returns>True if credentials file exists and is correctly formatted, false otherwise.</returns>
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
                        try
                        {
                            AzureSettings.Default[key] = val;
                        }
                        catch // key does not exist in AzureSettings
                        {
                            return false;
                        }                        
                    } else
                    {
                        return false;
                    }
                }
                return true;
            } else
            {                
                return false;
            }
        }

        /// <summary>
        /// Upload a file to Azure's cloud storage if it does not already exist.
        /// </summary>
        /// <param name="containerName">Name of the container to upload the file to</param>
        /// <param name="filePath">Path to the file on disk</param>
        private void UploadFileIfNeeded(string containerName, string filePath)
        {
            var credentials = new Microsoft.Azure.Storage.Auth.StorageCredentials(
                (string)AzureSettings.Default["StorageAccount"],
                (string)AzureSettings.Default["StorageKey"]);

            var storageAccount = new CloudStorageAccount(credentials, true);
            var blobClient = Microsoft.Azure.Storage.Blob.BlobAccountExtensions.CreateCloudBlobClient(storageAccount);
            var containerRef = blobClient.GetContainerReference(containerName);
            containerRef.CreateIfNotExists();
            var blobRef = containerRef.GetBlockBlobReference(Path.GetFileName(filePath));

            var md5 = GetFileMd5(filePath);
            // if blob exists and md5 matches, there is no need to upload the file
            if (blobRef.Exists() && string.Equals(md5, blobRef.Properties.ContentMD5, StringComparison.InvariantCultureIgnoreCase)) return;
            blobRef.Properties.ContentMD5 = md5;
            blobRef.UploadFromFileAsync(filePath);
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
        /// Zips up a directory containing ApsimX
        /// </summary>
        /// <param name="srcPath">Path of the ApsimX directory</param>
        /// <param name="zipPath">Path to which the zip file will be saved</param>
        /// <returns>0 if successful, 1 otherwise</returns>
        private int CreateApsimXZip(string srcPath, string zipPath)
        {
            try
            {
                using (FileStream zipToOpen = new FileStream(zipPath, FileMode.Create))
                {
                    using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
                    {
                        ZipArchiveEntry f;
                        f = archive.CreateEntryFromFile(Path.Combine(srcPath, "Bin", "APSIM.Shared.dll"), "APSIM.Shared.dll");
                        f = archive.CreateEntryFromFile(Path.Combine(srcPath, "Bin", "Models.exe"), "Models.exe");
                        f = archive.CreateEntryFromFile(Path.Combine(srcPath, "Bin", "sqlite3.dll"), "sqlite3.dll");
                    }
                }
                return 0;
            }
            catch (Exception err)
            {
                ShowError(new Exception("Error zipping up APSIM: ", err));
                return 1;
            }
        }

        /// <summary>
        /// This function should probably not be used. Try using CreateApsimXZip() instead!
        /// Creates a zip file of a directory containing Apsim.
        /// </summary>
        /// <param name="srcPath">Path of the Apsim Directory</param>
        /// <param name="zipPath">Path of the zip file to be created</param>
        /// <returns>0 if successful, 1 otherwise</returns>
        private int CreateApsimZip(string srcPath, string zipPath)
        {
            try
            {
                using (FileStream zipToOpen = new FileStream(zipPath, FileMode.Create))
                {
                    using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
                    {
                        ZipArchiveEntry f;

                        f = archive.CreateEntryFromFile(Path.Combine(srcPath, "Apsim.xml"), "Apsim.xml");
                        ZipAddDir(Path.Combine(srcPath, "Model"), srcPath, archive);
                        ZipAddDir(Path.Combine(srcPath, "UserInterface"), srcPath, archive);
                    }
                }
                return 0;
            }
            catch (Exception err)
            {
                ShowError(err);
                return 1;
            }
        }

        /// <summary>
        /// Adds a directory to a zip file?
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="baseDir"></param>
        /// <param name="za"></param>
        private void ZipAddDir(string dir, string baseDir, ZipArchive za)
        {
            // TODO : figure out what this does and improve variable names
            try
            {

                foreach (string file in Directory.GetFiles(dir))
                {

                    string path = file.Substring(baseDir.Length);

                    ZipArchiveEntry fe;
                    fe = za.CreateEntryFromFile(file, path);

                    //Console.WriteLine(f);
                }

                foreach (string d in Directory.GetDirectories(dir))
                {
                    ZipAddDir(d, baseDir, za);
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        } 

        /// <summary>
        /// Initialises the uploader and batch client. Asks user for an Azure licence file and saves the credentials
        /// if the credentials have not previously been set.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GetCredentials(object sender, EventArgs e)
        {
            if (AzureCredentialsSetup.CredentialsExist())
            {
                // store credentials
                storageAuth = StorageCredentials.FromConfiguration();
                batchAuth = BatchCredentials.FromConfiguration();

                storageAccount = new CloudStorageAccount(new Microsoft.Azure.Storage.Auth.StorageCredentials(storageAuth.Account, storageAuth.Key), true);
                uploader = new FileUploader(storageAccount);
                var sharedCredentials = new Microsoft.Azure.Batch.Auth.BatchSharedKeyCredentials(batchAuth.Url, batchAuth.Account, batchAuth.Key);
                try
                {
                    batchCli = BatchClient.Open(sharedCredentials);
                }
                catch (UriFormatException)
                {
                    ShowErrorMessage("Error opening Azure Batch client: credentials are invalid.");
                    AzureCredentialsSetup cred = new AzureCredentialsSetup();
                    cred.Finished += GetCredentials;
                }
                catch (Exception ex)
                {
                    ShowError(ex);
                }
                
            } else
            {
                // ask user for a credentials file
                AzureCredentialsSetup cred = new AzureCredentialsSetup();
                cred.Finished += GetCredentials;
            }
        }
    }
}
