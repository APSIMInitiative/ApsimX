namespace UserInterface.Presenters
{
    using System;
    using System.Text;
    using Views;
    using System.ComponentModel;
    using System.IO;
    using System.IO.Compression;
    using Microsoft.Azure.Batch;
    using Microsoft.WindowsAzure.Storage;
    using System.Security.Cryptography;
    using ApsimNG.Cloud;
    using Microsoft.Azure.Batch.Common;
    using ApsimNG.Properties;
    using Models.Core;

    public class NewAzureJobPresenter : IPresenter, INewCloudJobPresenter
    {        
        /// <summary>The new azure job view</summary>
        private NewAzureJobView view;
        
        /// <summary>The explorer presenter</summary>
        private ExplorerPresenter explorerPresenter;        
        private IModel model;
        private FileUploader uploader;
        private BatchClient batchClient;
        private CloudStorageAccount storageAccount;
        private StorageCredentials storageCredentials;
        private BatchCredentials batchCredentials;
        private PoolSettings poolSettings;
        private BackgroundWorker submissionWorker;

        private const string SETTINGS_FILENAME = "settings.txt";

        public NewAzureJobPresenter()
        {            
        }

        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.explorerPresenter = explorerPresenter;
            this.view = (NewAzureJobView)view;
            
            this.view.Presenter = this;
            
            GetCredentials(null, null);

            this.view.SetDefaultJobName( (model as IModel).Name );
            this.model = (IModel)model;

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
            if (!CredentialsExist()) return;
            if (jp.JobDisplayName.Length < 1)
            {
                ShowError("A description is required");
                return;
            }

            if (jp.ApplicationPackagePath.Length < 1)
            {
                string msg = jp.ApsimFromDir ? "Invalid Apsim Directory" : "Invalid Apsim zip file";
                return;
            }

            if (! (Directory.Exists(jp.ApplicationPackagePath) || File.Exists(jp.ApplicationPackagePath)) )
            {
                ShowError("File or Directory not found: " + jp.ApplicationPackagePath);
                return;
            }

            if (jp.CoresPerProcess.ToString().Length < 1)
            {
                ShowError("Number of cores per CPU is a required field");
                return;
            }

            if (jp.SaveModelFiles && jp.ModelPath.Length < 0)
            {
                ShowError("Invalid model output directory: " + jp.ModelPath);
                return;
            }
            if (!Directory.Exists(jp.ModelPath))
            {
                try
                {
                    Directory.CreateDirectory(jp.ModelPath);
                } catch (Exception e)
                {
                    ShowError(e.ToString());
                    return;
                }                
            }

            if (jp.OutputDir.Length < 1)
            {
                ShowError("Invalid output directory.");
                return;
            }

            if (!Directory.Exists(jp.OutputDir))
            {
                try
                {
                    Directory.CreateDirectory(jp.OutputDir);
                }
                catch (Exception e)
                {
                    ShowError(e.ToString());
                    return;
                }
            }

            // save user's choices to ApsimNG.Properties.Settings            
            
            Settings.Default["OutputDir"] = jp.OutputDir;
            Settings.Default.Save();
            
            submissionWorker.RunWorkerAsync(jp);
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

                if (createApsimXZip(jp.ApplicationPackagePath, tmpZip) > 0) ShowError("Error zipping up Apsim");

                jp.ApplicationPackagePath = tmpZip;
                jp.ApplicationPackageVersion = Path.GetFileName(tmpZip).Substring(Path.GetFileName(tmpZip).IndexOf('-') + 1);
            }

            // add current job to the list of jobs                        

            // TODO : do we actually need/use the APSIMJob class?
            APSIMJob job = new APSIMJob(jp.JobDisplayName, "", jp.ApplicationPackagePath, jp.ApplicationPackageVersion, jp.Recipient, batchCredentials, storageCredentials, PoolSettings.FromConfiguration());
            job.PoolInfo.MaxTasksPerVM = jp.PoolMaxTasksPerVM;
            job.PoolInfo.VMCount = jp.PoolVMCount;


            // upload tools such as 7zip, AzCopy, CMail, etc.

            view.Status = "Checking tools";

            string executableDirectory = GetExecutableDirectory();
            string toolsDir = Path.Combine(executableDirectory, "tools");
            if (!Directory.Exists(toolsDir))
            {
                ShowError("Tools Directory not found: " + toolsDir);
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

                    string tmpConfig = Path.Combine(Path.GetTempPath(), SETTINGS_FILENAME);
                    using (StreamWriter file = new StreamWriter(tmpConfig))
                    {
                        file.WriteLine("EmailRecipient=" + jp.Recipient);
                        file.WriteLine("EmailSender=agresearch.azure@gmail.com");
                        file.WriteLine("EmailPW=New-Tangent");
                    }

                    UploadFileIfNeeded("job-" + jp.JobId, tmpConfig);
                    File.Delete(tmpConfig);
                    
                }
                catch (Exception ex)
                {
                    ShowError("Error writing to settings file; you may not receive an email upon job completion: " + ex.ToString());
                }
            }

            // upload job manager            
            UploadFileIfNeeded("jobmanager", Path.Combine(executableDirectory, "azure-apsim.exe"));



            // upload apsim
            view.Status = "Uploading APSIM Next Generation";

            UploadFileIfNeeded("apsim", jp.ApplicationPackagePath);


            // create models

            // TODO : show error message if other files already exist in model output directory?
            //        may be necessary if using  a user-selected directory
                     
            string path = "";



            // generate model files

            view.Status = "Generating model files";
            if (!Directory.Exists(jp.ModelPath)) Directory.CreateDirectory(jp.ModelPath);

            // generate xml
            string xml = "";
            foreach (Simulation sim in Runner.AllSimulations(model))
            {
                path = jp.ModelPath + "\\" + sim.Name + ".apsimx";
                // if weather file is not in the same directory as the .apsimx file, display an error then abort
                foreach (var child in sim.Children)
                {
                    if (child is Models.Weather)
                    {
                        string childPath = ((Models.Weather)sim.Children[0]).FileName;                        
                        if (Path.GetDirectoryName(childPath) != "")
                        {
                            ShowError(childPath + " must be in the same directory as the .apsimx file" + sim.FileName != null ? " (" + Path.GetDirectoryName(sim.FileName) + ")" : "");
                            view.Status = "Cancelled";
                            return;
                        }
                        string sourcePath = ((Models.Weather)sim.Children[0]).FullFileName;
                        string destPath = Path.Combine(jp.ModelPath, ((Models.Weather)sim.Children[0]).FileName);
                        if (!File.Exists(destPath)) File.Copy(sourcePath, destPath);
                    }
                }                
                xml = Apsim.Serialise(sim);
                // delete model file if it already exists
                if (File.Exists(path)) File.Delete(path);
                string pattern = @"<\?xml[^<>]+>";
                System.Text.RegularExpressions.Regex rx = new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                var matches = rx.Matches(xml);
                xml = "<Simulations xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" Version=\"23\">\r\n<Name>" + sim.Name + "</Name>\r\n" + xml.Replace(matches[0].ToString(), "") + "<DataStore><Name>DataStore</Name><AutoExport>false</AutoExport><MaximumResultsPerPage>0</MaximumResultsPerPage></DataStore><ExplorerWidth>296</ExplorerWidth></Simulations>";
                // write xml to file
                using (FileStream fs = File.Create(path))
                {
                    Byte[] info = new UTF8Encoding(true).GetBytes(xml);
                    fs.Write(info, 0, info.Length);
                }
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
                CloudJob cloudJob = batchClient.JobOperations.CreateJob(jp.JobId.ToString(), GetPoolInfo(job.PoolInfo));
                cloudJob.DisplayName = job.DisplayName;
                cloudJob.JobPreparationTask = job.ToJobPreparationTask(jp.JobId, storageAccount.CreateCloudBlobClient());
                cloudJob.JobReleaseTask = job.ToJobReleaseTask(jp.JobId, storageAccount.CreateCloudBlobClient());
                cloudJob.JobManagerTask = job.ToJobManagerTask(jp.JobId, storageAccount.CreateCloudBlobClient(), jp.JobManagerShouldSubmitTasks, jp.AutoScale);

                cloudJob.Commit();
            } catch (Exception ex)
            {
                ShowError(ex.ToString());
            }

            view.Status = "Job Successfully submitted";

            // in theory, instantiating an AzureResultsDownloader here and passing in this job's ID and name, then calling its DownloadResults() method would allow 
            // for automatic job downloading as soon as the job is finished. The trouble is it needs an AzureJobDisplayPresenter (for progress bar updating, etc).

            // explorerPresenter.MainPresenter.ShowMessage("Job Successfully submitted", Simulation.ErrorLevel.Information);            
            // AzureResultsDownloader dl = new AzureResultsDownloader(jp.JobId, jp.JobDisplayName, jp.OutputDir, this.explorerPresenter, true, false, false);
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
                    ShowError("There are already files in the output directory");                    
                }
            }
            return 0;
        }

        private int CreateSimOutputDir(string path)
        {
            try
            {
                Directory.CreateDirectory(path);
            } catch (Exception e)
            {
                ShowError("Error: creation of simulation directory " + path + " failed: " + e.ToString());
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
                            CloudServiceConfiguration = new CloudServiceConfiguration("4"),
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
                catch (IOException e)
                {
                    if (++attempt == 10)
                    {
                       ShowError("No unique temporary file name is available: " + e.ToString());
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
                var credentials = new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials(
                    (string)Settings.Default["StorageAccount"],
                    (string)Settings.Default["StorageKey"]);

                var storageAccount = new CloudStorageAccount(credentials, true);
                var blobClient = storageAccount.CreateCloudBlobClient();
                var containerRef = blobClient.GetContainerReference(containerName);
                containerRef.CreateIfNotExists();
                containerRef.Metadata.Add(key, val);
                containerRef.SetMetadata();
            }
            catch (Exception e)
            {                
                ShowError(e.ToString());
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
                            Settings.Default[key] = val;
                        } catch // key does not exist in ApsimNG.Properties.Settings.Default
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
        /// Displays an error message to the user.
        /// </summary>
        /// <param name="msg">Message to be displayed.</param>
        public void ShowError(string msg)
        {
            explorerPresenter.MainPresenter.ShowMessage(msg, Simulation.ErrorLevel.Error);
        }

        /// <summary>
        /// Upload a file to Azure's cloud storage if it does not already exist.
        /// </summary>
        /// <param name="containerName">Name of the container to upload the file to</param>
        /// <param name="filePath">Path to the file on disk</param>
        private void UploadFileIfNeeded(string containerName, string filePath)
        {
            var credentials = new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials(
                (string)Settings.Default["StorageAccount"],
                (string)Settings.Default["StorageKey"]);

            var storageAccount = new CloudStorageAccount(credentials, true);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var containerRef = blobClient.GetContainerReference(containerName);
            containerRef.CreateIfNotExists();
            var blobRef = containerRef.GetBlockBlobReference(Path.GetFileName(filePath));

            var md5 = GetFileMd5(filePath);
            // if blob exists and md5 matches, there is no need to upload the file
            if (blobRef.Exists() && string.Equals(md5, blobRef.Properties.ContentMD5, StringComparison.InvariantCultureIgnoreCase)) return;
            blobRef.Properties.ContentMD5 = md5;
            blobRef.UploadFromFile(filePath, FileMode.Open);
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
        private int createApsimXZip(string srcPath, string zipPath)
        {
            try
            {
                using (FileStream zipToOpen = new FileStream(zipPath, FileMode.Create))
                {
                    using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
                    {
                        ZipArchiveEntry f;
                        f = archive.CreateEntryFromFile(srcPath + "\\Bin\\APSIM.Shared.dll", "APSIM.Shared.dll");
                        f = archive.CreateEntryFromFile(srcPath + "\\Bin\\Models.exe", "Models.exe");
                        f = archive.CreateEntryFromFile(srcPath + "\\Bin\\sqlite3.dll", "sqlite3.dll");
                    }
                }
                return 0;
            }
            catch (Exception e)
            {
                ShowError(e.ToString());
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

                        f = archive.CreateEntryFromFile(srcPath + "\\Apsim.xml", "Apsim.xml");
                        ZipAddDir(srcPath + "\\Model", srcPath + "\\", archive);  // note trailing \\
                        ZipAddDir(srcPath + "\\UserInterface", srcPath + "\\", archive);  // note trailing \\
                    }
                }
                return 0;
            }
            catch (Exception e)
            {
                ShowError(e.ToString());
                return 1;
            }
        }

        /// <summary>
        /// Adds a directory to a zip file?
        /// </summary>
        /// <param name="sDir"></param>
        /// <param name="baseDir"></param>
        /// <param name="za"></param>
        private void ZipAddDir(string sDir, string baseDir, ZipArchive za)
        {
            // TODO : figure out what this does and improve variable names
            try
            {

                foreach (string f in Directory.GetFiles(sDir))
                {

                    string fPath = f.Substring(baseDir.Length);

                    ZipArchiveEntry fe;
                    fe = za.CreateEntryFromFile(f, fPath);

                    //Console.WriteLine(f);
                }

                foreach (string d in Directory.GetDirectories(sDir))
                {
                    ZipAddDir(d, baseDir, za);
                }
            }
            catch (Exception e)
            {
                ShowError(e.ToString());
            }
        } 

        public void Detach()
        {
        }

        /// <summary>
        /// Initialises the uploader and batch client. Asks user for an Azure licence file and saves the credentials
        /// if the credentials have not previously been set.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GetCredentials(object sender, EventArgs e)
        {
            if (CredentialsExist())
            {
                // store credentials
                storageCredentials = StorageCredentials.FromConfiguration();
                batchCredentials = BatchCredentials.FromConfiguration();
                poolSettings = PoolSettings.FromConfiguration();

                storageAccount = new CloudStorageAccount(new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials(storageCredentials.Account, storageCredentials.Key), true);
                uploader = new FileUploader(storageAccount);
                var sharedCredentials = new Microsoft.Azure.Batch.Auth.BatchSharedKeyCredentials(batchCredentials.Url, batchCredentials.Account, batchCredentials.Key);
                batchClient = BatchClient.Open(sharedCredentials);
            } else
            {
                // ask user for a credentials file
                AzureCredentialsSetup cred = new AzureCredentialsSetup();
                cred.Finished += GetCredentials;
            }
        }

        /// <summary>
        /// Checks if Azure credentials exist in Settings.Default. This method does not check their validity.
        /// </summary>
        /// <returns>True if credentials exist, false otherwise.</returns>
        private bool CredentialsExist()
        {            
            string[] credentials = new string[] { "BatchAccount", "BatchUrl", "BatchKey", "StorageAccount", "StorageKey" };
            foreach (string key in credentials)
            {
                string value = (string)Settings.Default[key];
                if (value == null || value == "") return false;
            }
            return true;
        }
        
        /// <summary>
        /// Cancels submission of a job and hides the right hand panel (which holds the new job view).
        /// </summary>
        public void CancelJobSubmission()
        {
            submissionWorker.CancelAsync();
            explorerPresenter.HideRightHandPanel();
        }
    }
}
