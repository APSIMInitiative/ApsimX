using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gtk;

namespace ApsimNG.Cloud
{
    class DownloadWindow : Window
    {
        /// <summary>
        /// Whether or not the result files should be unzipped.
        /// </summary>
        private bool unzipResultFiles;
        private bool downloadResults;
        private bool collateResults;

        private CheckButton includeDebugFiles;
        private CheckButton unzipResults;
        private CheckButton keepRawOutputs;
        private CheckButton generateCsv;
        private CheckButton runAsync;
        private CheckButton chkDownloadResults;
        private Button btnDownload;
        private Button btnChangeOutputDir;

        private ProgressBar currentFileProgress;
        private ProgressBar overallProgress;

        private Entry entryOutputDir;

        private VBox vboxPrimary;

        private List<string> jobs;

        private UserInterface.Interfaces.ICloudJobPresenter presenter;
        /// <summary>
        /// Default constructor. Unused.
        /// </summary>
        public DownloadWindow() : base("Download cloud jobs")
        {
            InitialiseWindow();
        }

        /// <summary>
        /// The only constructor used (at present).
        /// </summary>
        /// <param name="jobIds">Ids of the jobs to be downloaded.</param>
        public DownloadWindow(UserInterface.Interfaces.ICloudJobPresenter parent, List<string> jobIds) : base("Download cloud jobs")
        {
            presenter = parent;
            jobs = jobIds;
            InitialiseWindow();            
        }

        /// <summary>
        /// Adds the controls (buttons, checkbuttons, progress bars etc.) to the window.
        /// </summary>
        private void InitialiseWindow()
        {
            vboxPrimary = new VBox();
            HBox downloadDirectoryContainer = new HBox();

            // Checkbox initialisation
            includeDebugFiles = new CheckButton("Include Debugging Files");

            runAsync = new CheckButton("Download asynchronously")
            {
                TooltipText = "If this is disabled, the UI will be unresponsive for the duration of the download. On the other hand, this functionality has not been thoroughly tested. Use at your own risk.",
                Active = false
            };

            chkDownloadResults = new CheckButton("Download results")
            {
                Active = true,
                TooltipText = "Results will be downloaded if and only if this option is enabled."
            };
            chkDownloadResults.Toggled += DownloadResultsToggle;
            downloadResults = true;

            unzipResults = new CheckButton("Unzip results")
            {
                Active = true,
                TooltipText = "Check this option to automatically unzip the results."
            };
            unzipResults.Toggled += UnzipToggle;
            unzipResultFiles = true;

            generateCsv = new CheckButton("Collate Results")
            {
                Active = true,
                TooltipText = "Check this option to automatically combine results into a CSV file."
            };
            collateResults = true;
            generateCsv.Toggled += GenerateCsvToggle;

            keepRawOutputs = new CheckButton("Keep raw output files")
            {
                Active = true,
                TooltipText = "By default, the raw output files are deleted after being combined into a CSV. Check this option to keep the raw outputs."
            };

            unzipResults.Active = false;

            // Button initialisation
            btnDownload = new Button("Download");
            btnDownload.Clicked += Download;

            btnChangeOutputDir = new Button("...");
            btnChangeOutputDir.Clicked += ChangeOutputDir;

            entryOutputDir = new Entry((string)Properties.Settings.Default["OutputDir"]) { Sensitive = false };
            entryOutputDir.WidthChars = entryOutputDir.Text.Length;

            downloadDirectoryContainer.PackStart(new Label("Output Directory: "), false, false, 0);
            downloadDirectoryContainer.PackStart(entryOutputDir, true, true, 0);
            downloadDirectoryContainer.PackStart(btnChangeOutputDir, false, false, 0);

            currentFileProgress = new ProgressBar(new Adjustment(0, 0, 1, 0.01, 0.01, 0.01));            
            overallProgress = new ProgressBar(new Adjustment(0, 0, 1, 0.01, 0.01, 0.01));
            
            // Put all form controls into the primary vbox
            vboxPrimary.PackStart(includeDebugFiles);
            vboxPrimary.PackStart(runAsync);
            vboxPrimary.PackStart(chkDownloadResults);
            vboxPrimary.PackStart(unzipResults);
            vboxPrimary.PackStart(generateCsv);
            vboxPrimary.PackStart(keepRawOutputs);
            vboxPrimary.PackStart(downloadDirectoryContainer);            
            vboxPrimary.PackStart(new Label(""));

            vboxPrimary.PackEnd(new Label(""));
            vboxPrimary.PackEnd(btnDownload, false, false, 0);

            Frame primaryContainer = new Frame("Download Settings");
            primaryContainer.Add(vboxPrimary);
            Add(primaryContainer);
            ShowAll();
            
            currentFileProgress.HideAll();
            overallProgress.HideAll();
        }

        /// <summary>
        /// Downloads the currently selected jobs, taking into account the settings.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Download(object sender, EventArgs e)
        {
            btnDownload.Clicked -= Download;
            btnChangeOutputDir.Clicked -= ChangeOutputDir;
            Destroy();
            presenter.DownloadResults(jobs, generateCsv.Active, includeDebugFiles.Active, keepRawOutputs.Active, unzipResults.Active, runAsync.Active);
        }

        /// <summary>
        /// Opens a GUI asking the user for a default download directory, and saves their choice.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChangeOutputDir(object sender, EventArgs e)
        {
            string dir = GetDirectory();
            if (dir != "")
            {
                entryOutputDir.Text = dir;
                AzureSettings.Default["OutputDir"] = dir;                
                AzureSettings.Default.Save();                
            }
        }

        /// <summary>Opens a file chooser dialog for the user to choose a directory.</summary>	
        /// <return>
        /// The path of the chosen directory, or an empty string if the user pressed cancel or 
        /// selected a nonexistent directory.
        /// </return>
        private string GetDirectory()
        {
            // In theory this method should work cross-platform. In practice it may be less buggy to 
            // check the OS and use a reliable method for each OS (as in ViewBase.cs)

            FileChooserDialog fc = new FileChooserDialog(
                                        "Choose the file to open",
                                        null,
                                        FileChooserAction.SelectFolder,
                                        "Cancel", ResponseType.Cancel,
                                        "Select Folder", ResponseType.Accept);
            string path = "";

            try
            {
                if (fc.Run() == (int)ResponseType.Accept)
                {
                    path = fc.Filename;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            fc.Destroy();
            return path;
        }

        /// <summary>
        /// Event handler for toggling the generate CSV checkbox. Disables the keep raw outputs checkbox.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GenerateCsvToggle(object sender, EventArgs e)
        {
            collateResults = !collateResults;

            keepRawOutputs.Active = false;
            keepRawOutputs.Sensitive = collateResults;
        }

        /// <summary>
        /// Event handler for toggling the unzip results checkbox. Disables the generate CSV checkbox.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UnzipToggle(object sender, EventArgs e)
        {
            unzipResultFiles = !unzipResultFiles;
            generateCsv.Active = false;
            generateCsv.Sensitive = unzipResultFiles;            
        }

        /// <summary>
        /// Event handler for toggling the download results checkbox. Disables the unzip results checkbox.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DownloadResultsToggle(object sender, EventArgs e)
        {
            downloadResults = !downloadResults;

            unzipResults.Active = false;
            unzipResults.Sensitive = downloadResults;
        }
    }
}
