using APSIM.Shared.Utilities;
using ApsimNG.Cloud;
using Models.Core;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UserInterface.Views;
using static UserInterface.Views.UpgradeView;

namespace UserInterface.Presenters
{
    /// <summary>
    /// Presenter which allows user to send a job to run on a cloud platform.
    /// </summary>
    /// <remarks>
    /// Currently, the only supported/implemented cloud platform is azure.
    /// If this class is extended (ie if you remove the sealed modifier)
    /// please remember to update the IDisposable implementation.
    /// </remarks>
    public sealed class RunOnCloudPresenter : IPresenter
    {
        /// <summary>The view.</summary>
        private ViewBase view;

        /// <summary>The explorer presenter.</summary>
        private ExplorerPresenter presenter;

        /// <summary>List of all APSIM releases.</summary>
        private Upgrade[] upgrades;

        /// <summary>The name of the job edit box.</summary>
        private EditView nameOfJobEdit;

        /// <summary>The combobox for number of cpus.</summary>
        private DropDownView numberCPUCombobox;

        /// <summary>The control showing the type of APSIM to run on cloud.</summary>
        private DropDownView apsimTypeToRunCombobox;

        /// <summary>The label with text 'Directory'.</summary>
        private LabelView directoryLabel;

        /// <summary>The directory edit box.</summary>
        private EditView directoryEdit;

        /// <summary>The browse button.</summary>
        private ButtonView browseButton;

        /// <summary>The version label.</summary>
        private LabelView versionLabel;

        /// <summary>The version combobox.</summary>
        private DropDownView versionCombobox;

        /// <summary>The submit button.</summary>
        private ButtonView submitButton;

        /// <summary>The status label.</summary>
        private LabelView statusLabel;

        /// <summary>Low priority checkbox.</summary>
        private CheckBoxView lowPriorityCheckBox;

        /// <summary>The model which we want to run on Azure.</summary>
        private IModel modelToRun;

        /// <summary>Cloud interface responsible for job submission.</summary>
        private ICloudInterface cloudInterface;

        /// <summary>Allows job submission to be cancelled.</summary>
        private CancellationTokenSource cancellation;

        /// <summary>Default constructor.</summary>
        public RunOnCloudPresenter()
        {
            cloudInterface = new AzureInterface();
            cancellation = new CancellationTokenSource();
        }

        /// <summary>Attaches this presenter to a view.</summary>
        /// <param name="model"></param>
        /// <param name="viewBase"></param>
        /// <param name="parentPresenter"></param>
        public void Attach(object model, object viewBase, ExplorerPresenter parentPresenter)
        {
            presenter = parentPresenter;
            view = (ViewBase)viewBase;
            modelToRun = (IModel)model;

            nameOfJobEdit = view.GetControl<EditView>("nameOfJobEdit");
            numberCPUCombobox = view.GetControl<DropDownView>("numberCPUCombobox");
            apsimTypeToRunCombobox = view.GetControl<DropDownView>("apsimTypeToRunCombobox");
            directoryLabel = view.GetControl<LabelView>("directoryLabel");
            directoryEdit = view.GetControl<EditView>("directoryEdit");
            browseButton = view.GetControl<ButtonView>("browseButton");
            versionLabel = view.GetControl<LabelView>("versionLabel");
            versionCombobox = view.GetControl<DropDownView>("versionCombobox");
            submitButton = view.GetControl<ButtonView>("submitButton");
            statusLabel = view.GetControl<LabelView>("statusLabel");
            lowPriorityCheckBox = view.GetControl<CheckBoxView>("lowPriorityCheckBox");

            nameOfJobEdit.Text = modelToRun.Name + DateTime.Now.ToString("yyyy-MM-dd HH.mm");
            numberCPUCombobox.Values = new string[] { "16", "32", "48", "64", "80", "96", "112", "128", "256" };
            numberCPUCombobox.SelectedValue = ApsimNG.Cloud.Azure.AzureSettings.Default.NumCPUCores;

            apsimTypeToRunCombobox.Values = new string[] { "A released version", "A directory", "A zip file" };
            if (!string.IsNullOrEmpty(ApsimNG.Cloud.Azure.AzureSettings.Default.APSIMVersion))
                apsimTypeToRunCombobox.SelectedValue = "A released version";
            else if (!string.IsNullOrEmpty(ApsimNG.Cloud.Azure.AzureSettings.Default.APSIMDirectory))
                apsimTypeToRunCombobox.SelectedValue = "A directory";
            else if (!string.IsNullOrEmpty(ApsimNG.Cloud.Azure.AzureSettings.Default.APSIMZipFile))
                apsimTypeToRunCombobox.SelectedValue = "A zip file";
            else
                apsimTypeToRunCombobox.SelectedValue = "A released version";

            SetupWidgets();

            if (!string.IsNullOrEmpty(ApsimNG.Cloud.Azure.AzureSettings.Default.APSIMVersion))
                versionCombobox.SelectedValue = ApsimNG.Cloud.Azure.AzureSettings.Default.APSIMVersion;
            else if (!string.IsNullOrEmpty(ApsimNG.Cloud.Azure.AzureSettings.Default.APSIMDirectory))
                directoryEdit.Text = ApsimNG.Cloud.Azure.AzureSettings.Default.APSIMDirectory;
            else if (!string.IsNullOrEmpty(ApsimNG.Cloud.Azure.AzureSettings.Default.APSIMZipFile))
                directoryEdit.Text = ApsimNG.Cloud.Azure.AzureSettings.Default.APSIMZipFile;
            lowPriorityCheckBox.Checked = ApsimNG.Cloud.Azure.AzureSettings.Default.LowPriority;

            apsimTypeToRunCombobox.Changed += OnVersionComboboxChanged;
            browseButton.Clicked += OnBrowseButtonClicked;
            submitButton.Clicked += OnSubmitJobClicked;
        }

        /// <summary>This instance has been detached.</summary>
        public void Detach()
        {
            ApsimNG.Cloud.Azure.AzureSettings.Default.APSIMVersion = null;
            ApsimNG.Cloud.Azure.AzureSettings.Default.APSIMDirectory = null;
            ApsimNG.Cloud.Azure.AzureSettings.Default.APSIMZipFile = null;
            ApsimNG.Cloud.Azure.AzureSettings.Default.LowPriority = lowPriorityCheckBox.Checked;

            if (apsimTypeToRunCombobox.SelectedValue == "A released version")
                ApsimNG.Cloud.Azure.AzureSettings.Default.APSIMVersion = versionCombobox.SelectedValue;
            else if (apsimTypeToRunCombobox.SelectedValue == "A directory")
                ApsimNG.Cloud.Azure.AzureSettings.Default.APSIMDirectory = directoryEdit.Text;
            else
                ApsimNG.Cloud.Azure.AzureSettings.Default.APSIMZipFile = directoryEdit.Text;
            ApsimNG.Cloud.Azure.AzureSettings.Default.Save();
            cancellation.Dispose();
            apsimTypeToRunCombobox.Changed -= OnVersionComboboxChanged;
            browseButton.Clicked -= OnBrowseButtonClicked;
            submitButton.Clicked -= OnSubmitJobClicked;
        }

        /// <summary>User has changed the selection in the version combobox.</summary>
        private void OnVersionComboboxChanged(object sender, EventArgs e)
        {
            SetupWidgets();
        }

        /// <summary>User has clicked the browse button.</summary>
        private void OnBrowseButtonClicked(object sender, EventArgs e)
        {
            string newValue;
            if (apsimTypeToRunCombobox.SelectedValue == "A directory")
                newValue = ViewBase.AskUserForFileName("Select the APSIM directory", 
                                                       Utility.FileDialog.FileActionType.SelectFolder, 
                                                       directoryEdit.Text);
            else
                newValue = ViewBase.AskUserForFileName("Please select a zipped file", 
                                                       Utility.FileDialog.FileActionType.Open, 
                                                       "Zip file (*.zip) | *.zip");
            if (!string.IsNullOrEmpty(newValue))
                directoryEdit.Text = newValue;
        }

        /// <summary>User has clicked the submit button.</summary>
        private async void OnSubmitJobClicked(object sender, EventArgs e)
        {
            try
            {
                if (submitButton.Text == "Submit")
                {
                    submitButton.Text = "Cancel submit";
                    await SubmitJob(this, EventArgs.Empty);
                }
                else
                {
                    submitButton.Text = "Submit";
                    cancellation.Cancel();
                    
                }

            }
            catch (Exception err)
            {
                presenter.MainPresenter.ShowError(err);
            }
        }

        /// <summary>Cancels submission of a job.</summary>
        public void CancelJobSubmission(object sender, EventArgs args)
        {
            cancellation.Cancel();
        }

        /// <summary>/hide the directory and version controls based on the value in the version dropdown.</summary>
        private void SetupWidgets()
        {
            versionLabel.Visible = apsimTypeToRunCombobox.SelectedValue == "A released version";
            versionCombobox.Visible = versionLabel.Visible;
            directoryLabel.Visible = !versionLabel.Visible;
            directoryEdit.Visible = !versionLabel.Visible;
            browseButton.Visible = !versionLabel.Visible;

            if (versionLabel.Visible)
            {
                // Populate the version drop down.
                if (versionCombobox.Values.Length == 0)
                {
                    presenter.MainPresenter.ShowWaitCursor(true);
                    try
                    {
                        upgrades = WebUtilities.PostRestService<Upgrade[]>("https://builds.apsim.info/api/nextgen/list");
                        var upgradeNames = upgrades.Select(upgrade =>
                        {
                            var name = upgrade.ReleaseDate.ToString("yyyy.MM.dd.") + upgrade.Issue + " " + upgrade.Title;
                            if (name.Length > 50)
                                name = name.Remove(50);
                            return name;
                        });
                        versionCombobox.Values = upgradeNames.ToArray();
                    }
                    finally
                    {
                        presenter.MainPresenter.ShowWaitCursor(false);
                    }
                }
            }

            if (apsimTypeToRunCombobox.SelectedValue == "A directory")
                directoryLabel.Text = "Directory:";
            else
                directoryLabel.Text = "Zip file:";
        }
        
        /// <summary>Perform the actual submission of a job to the cloud.</summary>
        private async Task SubmitJob(object sender, EventArgs args)
        {
            string path;
            string version;
            if (versionCombobox.SelectedValue == null)
            {
                path = directoryEdit.Text;
                version = Path.GetFileName(directoryEdit.Text);
            }
            else
            {
                path = await DownloadReleasedVersion();
                version = versionCombobox.SelectedValue;
                if (path == null)
                    return;
            }
            JobParameters job = new JobParameters
            {
                ID = Guid.NewGuid(),
                DisplayName = nameOfJobEdit.Text,
                Model = modelToRun,
                ApsimXPath = path,
                CpuCount = Convert.ToInt32(numberCPUCombobox.SelectedValue),
                ModelPath = Path.GetTempPath() + Guid.NewGuid(),
                CoresPerProcess = 1,
                ApsimXVersion = version,
                JobManagerShouldSubmitTasks = true,
                AutoScale = true,
                MaxTasksPerVM = 16,
                LowPriority = lowPriorityCheckBox.Checked
            };

            if (cancellation.IsCancellationRequested)
                view.InvokeOnMainThread(delegate { statusLabel.Text = "Cancelled"; });

            if (string.IsNullOrWhiteSpace(job.DisplayName))
                throw new Exception("A description is required");

            if (string.IsNullOrWhiteSpace(job.ApsimXPath))
                throw new Exception("Invalid path to apsim");

            if (!Directory.Exists(job.ApsimXPath) && !File.Exists(job.ApsimXPath))
                throw new Exception($"File or Directory not found: '{job.ApsimXPath}'");

            if (job.CoresPerProcess <= 0)
                job.CoresPerProcess = 1;

            if (job.SaveModelFiles && string.IsNullOrWhiteSpace(job.ModelPath))
                throw new Exception($"Invalid model output directory: '{job.ModelPath}'");

            if (!Directory.Exists(job.ModelPath))
                Directory.CreateDirectory(job.ModelPath);

            try
            {
                await cloudInterface.SubmitJobAsync(job, cancellation.Token, s =>
                {
                    view.InvokeOnMainThread(delegate
                    {
                        statusLabel.Text = s;
                    });
                });
            }
            catch (Exception err)
            {
                view.InvokeOnMainThread(delegate
                {
                    statusLabel.Text = "Cancelled";
                    submitButton.Text = "Cancel submit";
                    presenter.MainPresenter.ShowError(err);
                });
            }
            finally
            {
                view.InvokeOnMainThread(delegate
                {
                    submitButton.Text = "Submit";
                });
            }
        }

        /// <summary>
        /// Download a released version of APSIM.
        /// </summary>
        /// <returns>The path to the downloads.</returns>
        private async Task<string> DownloadReleasedVersion()
        {
            var apsimReleasesPath = Path.Combine(Path.GetTempPath(), "ApsimReleases");
            Directory.CreateDirectory(apsimReleasesPath);
            var versionNumber = versionCombobox.SelectedValue;
            StringUtilities.SplitOffAfterDelimiter(ref versionNumber, " ");
            var apsimReleaseDirectory = Path.Combine(apsimReleasesPath, versionNumber);
            if (!Directory.Exists(apsimReleaseDirectory))
            {
                Directory.CreateDirectory(apsimReleaseDirectory);

                var upgrade = upgrades.ToList().Find(u => versionNumber == u.ReleaseDate.ToString("yyyy.MM.dd.") + u.Issue);
                var releaseFileName = Path.Combine(apsimReleasesPath, versionNumber + ".exe");

                // Download the release.
                try
                {
                    view.InvokeOnMainThread(delegate { statusLabel.Text = "Downloading the APSIM release..."; });
                    Stream stream = await WebUtilities.AsyncGetStreamTask(upgrade.DownloadLinkWindows, "*/*");
                    stream.Position = 0;
                    using (FileStream file = new FileStream(releaseFileName, FileMode.Create, System.IO.FileAccess.Write))
                    {
                        stream.CopyTo(file);
                        file.Flush();
                    }

                    // Unpack the installer's bin directory.
                    var binDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                    var setupUnpacker = Path.Combine(binDirectory, "tools", "innounp.exe");
                    var binFileSpecToUnpack = $"*Bin{Path.DirectorySeparatorChar}*";

                    var startInfo = new ProcessStartInfo()
                    {
                        FileName = setupUnpacker,
                        Arguments = $"-x -y -q {releaseFileName} {binFileSpecToUnpack}",
                        WorkingDirectory = apsimReleaseDirectory,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    Process p = Process.Start(startInfo);
                    p.WaitForExit();

                    // Remove the installer.
                    File.Delete(releaseFileName);

                    // In earlier releases of APSIM both 32bit and 64bit versions of files were included in the 
                    // setup.exe file. We only want the 64bit versions. In the bin folder there will be files like:
                    //    atk-sharp,1.dll
                    //    atk-sharp,2.dll
                    //    atk-sharp,3.dll
                    // It seems that the ,1 and ,3 files are 64bit versions so just keep the ,1 files and delete the
                    // other two. Rename the ,1 files to get rid of the ',1' e.g.
                    //    atk-sharp,1.dll becomes atk-sharp.dll
                    var binFolder = Path.Combine(apsimReleaseDirectory, "{app}", "Bin");
                    foreach (string fileName in Directory.GetFiles(binFolder, "*,1.*"))
                    {
                        var newFileName = fileName.Replace(",1", "");
                        File.Move(fileName, newFileName);
                    }
                    foreach (string fileName in Directory.GetFiles(binFolder, "*,?.*"))
                        File.Delete(fileName);

                }
                catch (Exception err)
                {
                    view.InvokeOnMainThread(delegate
                    {
                        presenter.MainPresenter.ShowError(err);
                    });
                    return null;
                }
            }
            return Path.Combine(apsimReleaseDirectory, "{app}", "Bin");
        }
    }
}