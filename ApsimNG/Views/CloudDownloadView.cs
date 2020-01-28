using System;
using System.Collections.Generic;
using UserInterface.Interfaces;
using Utility;
using Gtk;
using ApsimNG.Cloud.Azure;

namespace UserInterface.Views
{
    /// <summary>
    /// This view creates a popup window which allows the user to
    /// choose some options for downloading results of a cloud job.
    /// Once the user has chosen their options and pressed download,
    /// an event will be fired off to initiate the download.
    /// </summary>
    class CloudDownloadView : ViewBase
    {
        /// <summary>
        /// Whether 'debug' (.stdout) files should be downloaded.
        /// </summary>
        private CheckButton includeDebugFiles;

        /// <summary>
        /// Wether results should be unzipped.
        /// </summary>
        private CheckButton extractResults;

        /// <summary>
        /// Whether results should be saved after being combined into a .csv file.
        /// </summary>
        private CheckButton keepRawOutputs;

        /// <summary>
        /// Whether the results should be combined into a .csv file.
        /// </summary>
        private CheckButton generateCsv;

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
        /// Window which holds <see cref="vboxPrimary"/>.
        /// </summary>
        private Window window;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public CloudDownloadView(ViewBase owner) : base(owner)
        {
            window = new Window("Download cloud jobs");
            window.TransientFor = (MasterView as ViewBase).MainWidget.Toplevel as Window;
            window.WindowPosition = WindowPosition.CenterOnParent;
            vboxPrimary = new VBox();
            HBox outputPathContainer = new HBox();

            // Checkbox initialisation
            includeDebugFiles = new CheckButton("Include Debugging Files");

            chkDownloadResults = new CheckButton("Download results")
            {
                Active = true,
                TooltipText = "Results will be downloaded if and only if this option is enabled."
            };
            chkDownloadResults.Toggled += OnToggleDownloadResults;

            extractResults = new CheckButton("Unzip results")
            {
                Active = true,
                TooltipText = "Check this option to automatically unzip the results."
            };
            extractResults.Toggled += OnToggleExtractResults;

            generateCsv = new CheckButton("Collate Results")
            {
                Active = true,
                TooltipText = "Check this option to automatically combine results into a CSV file."
            };
            generateCsv.Toggled += OnToggleGenerateCsv;

            keepRawOutputs = new CheckButton("Keep raw output files")
            {
                Active = true,
                TooltipText = "By default, the raw output files are deleted after being combined into a CSV. Check this option to keep the raw outputs."
            };

            extractResults.Active = false;

            // Button initialisation
            btnDownload = new Button("Download");
            btnDownload.Clicked += OnDownload;

            btnChangeOutputDir = new Button("...");
            btnChangeOutputDir.Clicked += OnChangeOutputDir;

            entryOutputDir = new Entry();
            entryOutputDir.Sensitive = false;
            entryOutputDir.WidthChars = entryOutputDir.Text.Length;

            outputPathContainer.PackStart(new Label("Output Directory: "), false, false, 0);
            outputPathContainer.PackStart(entryOutputDir, true, true, 0);
            outputPathContainer.PackStart(btnChangeOutputDir, false, false, 0);

            // Put all form controls into the primary vbox
            vboxPrimary.PackStart(includeDebugFiles);
            vboxPrimary.PackStart(chkDownloadResults);
            vboxPrimary.PackStart(extractResults);
            vboxPrimary.PackStart(generateCsv);
            vboxPrimary.PackStart(keepRawOutputs);
            vboxPrimary.PackStart(outputPathContainer);

            // This empty label will put a gap between the controls above it and below it.
            vboxPrimary.PackStart(new Label(""));

            vboxPrimary.PackEnd(btnDownload, false, false, 0);

            Frame primaryContainer = new Frame("Download Settings");
            primaryContainer.Add(vboxPrimary);
            window.Add(primaryContainer);
            window.HideAll();

            window.Destroyed += OnDestroyed;
            window.DeleteEvent += OnDelete;
        }

        /// <summary>
        /// Invoked when the user clicks the download button.
        /// </summary>
        public event EventHandler Download;

        /// <summary>
        /// Output directory as specified by user.
        /// </summary>
        public string Path
        {
            get
            {
                return entryOutputDir.Text;
            }
            set
            {
                entryOutputDir.Text = value;
            }
        }

        /// <summary>
        /// Should results be extracted?
        /// </summary>
        public bool ExtractResults
        {
            get
            {
                return extractResults.Active;
            }
        }

        /// <summary>
        /// Should results be exported to .csv format?
        /// </summary>
        public bool ExportCsv
        {
            get
            {
                return generateCsv.Active;
            }
        }

        /// <summary>
        /// Should debug files be downloaded?
        /// </summary>
        public bool DownloadDebugFiles
        {
            get
            {
                return includeDebugFiles.Active;
            }
        }

        /// <summary>
        /// Controls visibility of the download window.
        /// </summary>
        public bool Visible
        {
            get
            {
                return window.Visible;
            }
            set
            {
                if (value)
                    window.ShowAll();
                else
                    window.HideAll();
            }
        }

        /// <summary>
        /// Destroys the download window.
        /// </summary>
        public void Destroy()
        {
            window.Destroy();
        }

        /// <summary>
        /// Downloads the currently selected jobs, taking into account
        /// the settings.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDownload(object sender, EventArgs e)
        {
            try
            {
                Download?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Opens a GUI asking the user for a default download
        /// directory, and saves their choice.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnChangeOutputDir(object sender, EventArgs e)
        {
            try
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
                    // todo: fire off an event here??
                    AzureSettings.Default.OutputDir = downloadDirectory;
                    AzureSettings.Default.Save();
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }
        
        /// <summary>
        /// Event handler for toggling the generate CSV checkbox.
        /// Disables the keep raw outputs checkbox.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnToggleGenerateCsv(object sender, EventArgs e)
        {
            try
            {
                keepRawOutputs.Active = false;
                keepRawOutputs.Sensitive = generateCsv.Active;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Event handler for toggling the unzip results checkbox.
        /// Disables the generate csv checkbox.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnToggleExtractResults(object sender, EventArgs e)
        {
            try
            {
                generateCsv.Active = false;
                generateCsv.Sensitive = extractResults.Active;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Event handler for toggling the download results checkbox.
        /// Disables the unzip results checkbox.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnToggleDownloadResults(object sender, EventArgs e)
        {
            try
            {
                extractResults.Active = false;
                extractResults.Sensitive = chkDownloadResults.Active;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Invoked when the window is closed for good, when apsim
        /// closes.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnDestroyed(object sender, EventArgs e)
        {
            try
            {
                btnDownload.Clicked -= OnDownload;
                btnChangeOutputDir.Clicked -= OnChangeOutputDir;

                chkDownloadResults.Toggled -= OnToggleDownloadResults;
                extractResults.Toggled -= OnToggleExtractResults;
                generateCsv.Toggled -= OnToggleGenerateCsv;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Invoked when the user closes the window.
        /// This prevents the window from closing, but still hides
        /// the window. This means we don't have to re-initialise
        /// the window each time the user opens it.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnDelete(object sender, DeleteEventArgs args)
        {
            try
            {
                Visible = false;
                args.RetVal = true;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }
    }
}
