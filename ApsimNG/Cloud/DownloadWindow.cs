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
        private CheckButton includeDebugFiles;
        private CheckButton keepRawOutputs;
        private CheckButton generateCsv;

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

            includeDebugFiles = new CheckButton("Include Debugging Files");            
            keepRawOutputs = new CheckButton("Keep raw output files");
            keepRawOutputs.Active = true;
            generateCsv = new CheckButton("Collate Results");
            generateCsv.Active = true;

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
            

            vboxPrimary.PackStart(includeDebugFiles);
            vboxPrimary.PackStart(keepRawOutputs);
            vboxPrimary.PackStart(generateCsv);
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
            presenter.DownloadResults(jobs, generateCsv.Active, includeDebugFiles.Active, keepRawOutputs.Active);
        }

        private void ChangeOutputDir(object sender, EventArgs e)
        {
            string dir = GetDirectory();
            if (dir != "")
            {
                entryOutputDir.Text = dir;
                Properties.Settings.Default["OutputDir"] = dir;                
                Properties.Settings.Default.Save();                
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
    }
}
