using System;
using System.Collections.Generic;
using UserInterface.Interfaces;
using Utility;
using Gtk;

namespace ApsimNG.Cloud
{
    /// <summary>
    /// View to ask user for some options regarding the download of files from a cloud platform.
    /// Once the user has chosen their options and pressed download, this class will pass the user's
    /// preferences into its presenter's DownloadResults method.
    /// </summary>
    class DownloadWindow : Window
    {
        /// <summary>
        /// Whether or not the result files should be unzipped.
        /// </summary>
        private bool unzipResultFiles;

        /// <summary>
        /// Whether the results should be downloaded.
        /// </summary>
        private bool downloadResults;

        /// <summary>
        /// Whether the results should be combined into a .csv file.
        /// </summary>
        private bool collateResults;

        /// <summary>
        /// Whether 'debug' (.stdout) files should be downloaded.
        /// </summary>
        private CheckButton includeDebugFiles;

        /// <summary>
        /// Wether results should be unzipped.
        /// </summary>
        private CheckButton unzipResults;

        /// <summary>
        /// Whether results should be saved after being combined into a .csv file.
        /// </summary>
        private CheckButton keepRawOutputs;

        /// <summary>
        /// Whether the results should be combined into a .csv file.
        /// </summary>
        private CheckButton generateCsv;

        /// <summary>
        /// Whether the download should occur in a separate thread.
        /// </summary>
        private CheckButton runAsync;

        /// <summary>
        /// Whether results should be downloaded.
        /// </summary>
        private CheckButton chkDownloadResults;

        /// <summary>
        /// Button to initiate the download.
        /// </summary>
        private Button btnDownload;

        /// <summary>
        /// Button to change the download directory.
        /// </summary>
        private Button btnChangeOutputDir;

        /// <summary>
        /// Input field to show/edit the download directory.
        /// </summary>
        private Entry entryOutputDir;

        /// <summary>
        /// Primary container, which holds all other controls in the window.
        /// </summary>
        private VBox vboxPrimary;

        /// <summary>
        /// List of jobs to be downloaded.
        /// </summary>
        private List<string> jobs;

        /// <summary>
        /// Reference to the presenter, which will have a method to download results.
        /// </summary>
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

            entryOutputDir = new Entry((string)AzureSettings.Default["OutputDir"]) { Sensitive = false };
            entryOutputDir.WidthChars = entryOutputDir.Text.Length;

            downloadDirectoryContainer.PackStart(new Label("Output Directory: "), false, false, 0);
            downloadDirectoryContainer.PackStart(entryOutputDir, true, true, 0);
            downloadDirectoryContainer.PackStart(btnChangeOutputDir, false, false, 0);
            
            // Put all form controls into the primary vbox
            vboxPrimary.PackStart(includeDebugFiles);
            vboxPrimary.PackStart(runAsync);
            vboxPrimary.PackStart(chkDownloadResults);
            vboxPrimary.PackStart(unzipResults);
            vboxPrimary.PackStart(generateCsv);
            vboxPrimary.PackStart(keepRawOutputs);
            vboxPrimary.PackStart(downloadDirectoryContainer);            

            // This empty label will put a gap between the controls above it and below it.
            vboxPrimary.PackStart(new Label(""));

            vboxPrimary.PackEnd(btnDownload, false, false, 0);

            Frame primaryContainer = new Frame("Download Settings");
            primaryContainer.Add(vboxPrimary);
            Add(primaryContainer);
            ShowAll();
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
            presenter.DownloadResults(jobs, chkDownloadResults.Active, generateCsv.Active, includeDebugFiles.Active, keepRawOutputs.Active, unzipResults.Active, runAsync.Active);
        }

        /// <summary>
        /// Opens a GUI asking the user for a default download directory, and saves their choice.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChangeOutputDir(object sender, EventArgs e)
        {
            IFileDialog fileChooser = new FileDialog()
            {
                Action = FileDialog.FileActionType.SelectFolder,
                Prompt = "Choose a download folder"
            };
            string downloadDirectory = fileChooser.GetFile();
            if (!string.IsNullOrEmpty(downloadDirectory))
            {
                entryOutputDir.Text = downloadDirectory;
                AzureSettings.Default["OutputDir"] = downloadDirectory;                
                AzureSettings.Default.Save();                
            }
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
