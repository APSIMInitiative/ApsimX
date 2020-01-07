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
using ApsimNG.Cloud.Azure;

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

        private ICloudInterface cloudInterface;
        /// <summary>
        /// Default constructor.
        /// </summary>
        public NewAzureJobPresenter()
        {
            cloudInterface = new AzureInterface();
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
        public async void SubmitJob(JobParameters jp)
        {
            if (string.IsNullOrWhiteSpace(jp.DisplayName))
                throw new Exception("A description is required");

            if (string.IsNullOrWhiteSpace(jp.ApsimXPath))
                throw new Exception("Invalid path to apsim");

            if (!Directory.Exists(jp.ApsimXPath) && !File.Exists(jp.ApsimXPath))
                throw new Exception($"File or Directory not found: '{jp.ApsimXPath}'");

            if (jp.CoresPerProcess <= 0)
                jp.CoresPerProcess = 1;

            if (jp.SaveModelFiles && string.IsNullOrWhiteSpace(jp.ModelPath))
                throw new Exception($"Invalid model output directory: '{jp.ModelPath}'");

            if (!Directory.Exists(jp.ModelPath))
                Directory.CreateDirectory(jp.ModelPath);

            jp.Model = Apsim.Get(presenter.ApsimXFile, presenter.CurrentNodePath) as IModel;

            // todo: Save settings to ApsimNG.Properties.Settings.
            //AzureSettings.Default["OutputDir"] = jp.OutputDir;
            //AzureSettings.Default.Save();

            //if (batchCli == null)
            //    GetCredentials(null, null);
            //else
            //    submissionWorker.RunWorkerAsync(jp);
            try
            {
                await cloudInterface.SubmitJob(jp, s => view.Status = s);
            }
            catch (Exception err)
            {
                view.Status = "Cancelled";
                presenter.MainPresenter.ShowError(err);
            }
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
