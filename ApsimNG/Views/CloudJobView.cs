using System;
using System.Collections.Generic;
using System.Linq;
using ApsimNG.Cloud;
using ApsimNG.EventArguments;
using ApsimNG.Interfaces;
using Gtk;

namespace UserInterface.Views
{
    /// <summary>
    /// This is a view for displaying cloud jobs - that is, simulations
    /// which have been run on a cloud platform (e.g. Microsoft Azure).
    /// </summary>
    public class CloudJobView : ViewBase, ICloudJobView
    {
        /// <summary>
        /// TreeView to display the data.
        /// </summary>
        private Gtk.TreeView tree;

        /// <summary>
        /// ListStore to hold the raw data being displayed.
        /// </summary>
        private ListStore store;

        /// <summary>
        /// Filters results based on whether or not they were submitted by the user.
        /// </summary>
        private TreeModelFilter filterOwner;

        /// <summary>
        /// Sorts the results when the user clicks the column headings.
        /// </summary>
        private TreeModelSort sort;

        /// <summary>
        /// Container for the job load progress bar.
        /// Only visible when the job list is being updated.
        /// </summary>
        private HBox jobLoadProgressContainer;

        /// <summary>
        /// Container for the download progress bar.
        /// </summary>
        private HBox downloadProgressContainer;

        /// <summary>
        /// Allows user to choose whether or not display other people's jobs.
        /// </summary>
        private CheckButton chkMyJobsOnly;

        /// <summary>
        /// Progress bar for updating the job list.
        /// </summary>
        private ProgressBar loadingProgress;

        /// <summary>
        /// Progress bar for downloading job results.
        /// </summary>
        private ProgressBar downloadProgress;

        /// <summary>
        /// Allows the user to change the download directory.        
        /// </summary>
        private Button btnChangeDownloadDir;

        /// <summary>
        /// Button to download the currently selected jobs.
        /// </summary>
        private Button btnDownload;

        /// <summary>
        /// Button to delete the currently selected jobs.
        /// </summary>
        private Button btnDelete;

        /// <summary>
        /// Button to terminate execution of the currently selected jobs.
        /// </summary>
        private Button btnStop;

        /// <summary>
        /// Button to modify the cloud credentials.
        /// </summary>
        private Button btnSetup;

        /// <summary>
        /// Indices of the column headers. If columns are added or removed, change this.
        /// Name, ID, State, NumSims, Progress, StartTime, EndTime
        /// </summary>
        private readonly string[] columnTitles = { "Name/Description", "Job ID", "State", "#Sims", "Progress", "Start Time", "End Time", "Duration", "CPU Time" };
        private enum Columns { Name, ID, State, NumSims, Progress, StartTime, EndTime, Duration, CpuTime };

        /// <summary>
        /// Defines the format that the two TimeSpan fields (duration and CPU time) are to be displayed in.
        /// </summary>
        private const string TimespanFormat = @"dddd\d\ hh\h\ mm\m\ ss\s";

        private CloudDownloadView dl;

        /// <summary>
        /// Constructor. Initialises the jobs TreeView and the controls associated with it.
        /// </summary>
        /// <param name="owner"></param>
        public CloudJobView(ViewBase owner) : base(owner)
        {
            dl = new CloudDownloadView(this);
            dl.Download += OnDoDownload;
            dl.Visible = false;

            // Give the ListStore 1 more column than the tree view.
            // This last column displays the job owner but is not shown by the TreeView in its own column.
            store = new ListStore(Enumerable.Repeat(typeof(string), Enum.GetValues(typeof(Columns)).Length + 1).ToArray());
            tree = new Gtk.TreeView() { CanFocus = true, RubberBanding = true };
            tree.Selection.Mode = SelectionMode.Multiple;

            for (int i = 0; i < columnTitles.Length; i++)
            {
                TreeViewColumn col = new TreeViewColumn
                {
                    Title = columnTitles[i],
                    SortColumnId = i,
                    Resizable = true,
                    Sizing = TreeViewColumnSizing.GrowOnly
                };
                CellRendererText cell = new CellRendererText();
                col.PackStart(cell, false);
                col.AddAttribute(cell, "text", i);
                col.SetCellDataFunc(cell, OnSetCellData);
                tree.AppendColumn(col);
            }

            // this filter holds the model (data) and is used to filter jobs based on whether 
            // they were submitted by the user
            filterOwner = new TreeModelFilter(store, null) { VisibleFunc = FilterOwnerFunc };
            filterOwner.Refilter();

            // the filter then goes into this TreeModelSort, which is used to sort results when
            // the user clicks on a column header
            sort = new TreeModelSort(filterOwner)
            {
                // By default, sort by start time descending.
                DefaultSortFunc = (model, a, b) => -1 * SortData(model, a, b, (int)Columns.StartTime)
            };
            for (int i = 0; i < columnTitles.Length; i++)
                sort.SetSortFunc(i, (model, a, b) => SortData(model, a, b, i));

            // the tree holds the sorted, filtered data
            tree.Model = sort;

            // the tree goes into this ScrolledWindow, allowing users to scroll down
            // to view more jobs
            ScrolledWindow scroll = new ScrolledWindow();
            scroll.Add(tree);

            // never allow horizontal scrolling, and only allow vertical scrolling when needed
            scroll.HscrollbarPolicy = PolicyType.Automatic;
            scroll.VscrollbarPolicy = PolicyType.Automatic;

            // The scrolled window goes into this frame to distinguish the job view 
            // from the controls beside it.
            Frame treeContainer = new Frame("Cloud Jobs");
            treeContainer.Add(scroll);

            chkMyJobsOnly = new CheckButton("Display my jobs only");
            chkMyJobsOnly.Toggled += OnToggleFilter;
            // Display only the user's jobs by default.
            chkMyJobsOnly.Active = true;
            chkMyJobsOnly.Yalign = 0;

            downloadProgress = new ProgressBar(new Adjustment(0, 0, 1, 0.01, 0.01, 1));
            downloadProgressContainer = new HBox();
            downloadProgressContainer.PackStart(new Label("Downloading: "), false, false, 0);
            downloadProgressContainer.PackStart(downloadProgress, false, false, 0);

            loadingProgress = new ProgressBar(new Adjustment(0, 0, 100, 0.01, 0.01, 100));
            loadingProgress.Adjustment.Lower = 0;
            loadingProgress.Adjustment.Upper = 100;

            btnChangeDownloadDir = new Button("Change Download Directory");
            btnChangeDownloadDir.Clicked += OnChangeDownloadPath;

            Table tblButtonContainer = new Table(1, 1, false);
            tblButtonContainer.Attach(btnChangeDownloadDir, 0, 1, 0, 1, AttachOptions.Shrink, AttachOptions.Shrink, 0, 0);

            btnDownload = new Button("Download");
            btnDownload.Clicked += OnDownloadJobs;
            HBox downloadButtonContainer = new HBox();
            downloadButtonContainer.PackStart(btnDownload, false, true, 0);

            btnDelete = new Button("Delete Job(s)");
            btnDelete.Clicked += OnDeleteJobs;
            HBox deleteButtonContainer = new HBox();
            deleteButtonContainer.PackStart(btnDelete, false, true, 0);

            btnStop = new Button("Stop Job(s)");
            btnStop.Clicked += OnStopJobs;
            HBox stopButtonContainer = new HBox();
            stopButtonContainer.PackStart(btnStop, false, true, 0);

            btnSetup = new Button("Credentials");
            btnSetup.Clicked += OnSetupClicked;
            HBox setupButtonContainer = new HBox();
            setupButtonContainer.PackStart(btnSetup, false, true, 0);

            jobLoadProgressContainer = new HBox();
            jobLoadProgressContainer.PackStart(new Label("Loading Jobs: "), false, false, 0);
            jobLoadProgressContainer.PackStart(loadingProgress, false, false, 0);

            VBox controlsContainer = new VBox();
            controlsContainer.PackStart(chkMyJobsOnly, false, false, 0);
            controlsContainer.PackStart(downloadButtonContainer, false, false, 0);
            controlsContainer.PackStart(stopButtonContainer, false, false, 0);
            controlsContainer.PackStart(deleteButtonContainer, false, false, 0);
            controlsContainer.PackStart(setupButtonContainer, false, false, 0);
            controlsContainer.PackEnd(tblButtonContainer, false, false, 0);

            HBox hboxPrimary = new HBox();
            hboxPrimary.PackStart(treeContainer, true, true, 0);
            hboxPrimary.PackStart(controlsContainer, false, true, 0);


            VBox vboxPrimary = new VBox();
            vboxPrimary.PackStart(hboxPrimary);
            vboxPrimary.PackEnd(jobLoadProgressContainer, false, false, 0);
            vboxPrimary.PackEnd(downloadProgressContainer, false, false, 0);

            mainWidget = vboxPrimary;
            mainWidget.Destroyed += OnDestroyed;
            vboxPrimary.ShowAll();

            downloadProgressContainer.HideAll();
            HideLoadingProgressBar();
        }

        /// <summary>
        /// Close the view.
        /// </summary>
        public void Destroy()
        {
            mainWidget.Destroy();
        }

        /// <summary>
        /// Invoked when the user clicks the setup button to provide an API key/credentials.
        /// </summary>
        public event EventHandler SetupClicked;

        /// <summary>
        /// Invoked when the user wants to change the results output path.
        /// </summary>
        public event EventHandler ChangeOutputPath;

        /// <summary>
        /// Invoked when the user wants to stop a job.
        /// </summary>
        public event AsyncEventHandler StopJobs;

        /// <summary>
        /// Invoked when the user wants to delete a job.
        /// </summary>
        public event AsyncEventHandler DeleteJobs;

        /// <summary>
        /// Invoked when the user wants to download the results of a job.
        /// </summary>
        public event AsyncEventHandler DownloadJobs;

        /// <summary>
        /// Gets or sets the value of the job load progress bar.
        /// </summary>
        public double JobLoadProgress
        {
            get
            {
                return loadingProgress.Adjustment.Value;
            }
            set
            {
                Application.Invoke(delegate
                {
                    loadingProgress.Adjustment.Value = Math.Min(Math.Round(value, 2), loadingProgress.Adjustment.Upper);
                });
            }
        }

        /// <summary>
        /// Gets or sets the value of the download progress bar.
        /// </summary>
        public double DownloadProgress
        {
            get
            {
                return downloadProgress.Adjustment.Value;
            }
            set
            {
                // Set progresss bar to whichever is smaller - the value being passed in, or the maximum value the progress bar can take.
                Application.Invoke(delegate { downloadProgress.Adjustment.Value = Math.Min(Math.Round(value, 2), downloadProgress.Adjustment.Upper); });
            }
        }

        /// <summary>
        /// Output directory as specified by user.
        /// </summary>
        public string DownloadPath
        {
            get
            {
                return dl.Path;
            }
            set
            {
                dl.Path = value;
            }
        }

        /// <summary>
        /// Should results be extracted?
        /// </summary>
        public bool ExtractResults
        {
            get
            {
                return dl.ExtractResults;
            }
        }

        /// <summary>
        /// Should results be exported to .csv format?
        /// </summary>
        public bool ExportCsv
        {
            get
            {
                return dl.ExportCsv;
            }
        }

        /// <summary>
        /// Should debug files be downloaded?
        /// </summary>
        public bool DownloadDebugFiles
        {
            get
            {
                return dl.DownloadDebugFiles;
            }
        }

        /// <summary>
        /// Makes the download progress bar invisible.
        /// </summary>
        public void HideDownloadProgressBar()
        {
            Application.Invoke(delegate { downloadProgressContainer.HideAll(); });
        }

        /// <summary>
        /// Makes the download progress bar visible.
        /// </summary>
        public void ShowDownloadProgressBar()
        {
            Application.Invoke(delegate { downloadProgressContainer.ShowAll(); });
        }

        /// <summary>
        /// Makes the job load progress bar invisible.
        /// </summary>
        public void HideLoadingProgressBar()
        {
            Application.Invoke(delegate { jobLoadProgressContainer.HideAll(); });
        }

        /// <summary>
        /// Makes the job load progress bar visible.
        /// </summary>
        public void ShowLoadingProgressBar()
        {
            Application.Invoke(delegate { jobLoadProgressContainer.ShowAll(); });
        }

        /// <summary>
        /// Empties the TreeView and refills it with the contents of jobList.
        /// Current sorting/filtering remains unchanged.
        /// </summary>
        public void UpdateJobTable(List<JobDetails> jobs)
        {
            if (jobs == null)
                return;

            // This entire function is run on the Gtk main loop thread.
            // This may cause problems if another thread wants to modify a view at the same time,
            // but is probably better than the alternative, which is concurrent modification of live Gtk elements.
            Application.Invoke(delegate
            {
                // Remember which column is being sorted. If the results are not sorted at all, order by start time ascending.
                bool needToReSort = sort.GetSortColumnId(out int sortIndex, out SortType order);

                store.Clear();
                foreach (JobDetails job in jobs)
                {
                    string startTimeString = job.StartTime == null ? DateTime.UtcNow.ToLocalTime().ToString() : ((DateTime)job.StartTime).ToLocalTime().ToString();
                    string endTimeString = job.EndTime == null ? "" : ((DateTime)job.EndTime).ToLocalTime().ToString();
                    string progressString = job.Progress < 0 ? "Work in progress" : Math.Round(job.Progress, 2).ToString() + "%";
                    string timeStr = job.CpuTime == TimeSpan.Zero ? "" : job.CpuTime.ToString(TimespanFormat);
                    string durationStr = job.Duration() == TimeSpan.Zero ? "" : job.Duration().ToString(TimespanFormat);
                    store.AppendValues(job.DisplayName, job.Id, job.State, job.NumSims.ToString(), progressString, startTimeString, endTimeString, durationStr, timeStr, job.Owner);
                }
            });
        }

        /// <summary>
        /// Gets the IDs of all currently selected jobs.
        /// </summary>
        public string[] GetSelectedJobIDs()
        {
            TreePath[] selectedRows = tree.Selection.GetSelectedRows();
            List<string> jobIds = new List<string>();
            TreeIter iter;
            for (int i = 0; i < selectedRows.Count(); i++)
            {
                tree.Model.GetIter(out iter, selectedRows[i]);
                jobIds.Add((string)tree.Model.GetValue(iter, 1));
            }
            return jobIds.ToArray();
        }

        /// <summary>
        /// Invoked when the user toggles the "my jobs only" checkbox.
        /// Refreshes the TreeView.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnToggleFilter(object sender, EventArgs args)
        {
            try
            {
                filterOwner.Refilter();
                tree.QueueDraw();
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Sets the contents of a cell being display on a grid.
        /// Appends owner to display name if showing other people's jobs.
        /// </summary>
        /// <param name="col">The column.</param>
        /// <param name="cell">The cell.</param>
        /// <param name="model">The tree model.</param>
        /// <param name="iter">The tree iterator.</param>
        private void OnSetCellData(TreeViewColumn column, CellRenderer cell, TreeModel model, TreeIter iter)
        {
            try
            {
                if (Array.IndexOf(tree.Columns, column) == 0 && cell is CellRendererText textCell)
                {
                    string jobName = (string)model.GetValue(iter, 0);
                    string owner = (string)model.GetValue(iter, columnTitles.Length);

                    // First column.
                    if (chkMyJobsOnly.Active)
                        textCell.Text = jobName;
                    else
                        textCell.Text = $"{jobName} ({owner})";
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Comapres 2 elements from the ListStore and returns an indication of their relative values. 
        /// </summary>
        /// <param name="model">Model of the ListStore.</param>
        /// <param name="a">Path to the first row.</param>
        /// <param name="b">Path to the second row.</param>
        /// <param name="i">Column to take values from.</param>
        /// <returns></returns>
        private int SortData(TreeModel model, TreeIter a, TreeIter b, int i)
        {
            if (i == (int)Columns.Name || i == (int)Columns.ID || i == (int)Columns.State)
                return SortStrings(model, a, b, i);
            else if (i == (int)Columns.StartTime || i == (int)Columns.EndTime)
                return SortDateStrings(model, a, b, i);
            else if (i == (int)Columns.NumSims)
                return SortInts(model, a, b, i);
            else if (i == (int)Columns.Progress)
                return SortProgress(model, a, b);
            else if (i == (int)Columns.CpuTime || i == (int)Columns.Duration)
                return SortCpuTime(model, a, b);
            else
                return SortData(model, a, b, Math.Abs(i % columnTitles.Length));
        }

        /// <summary>
        /// Sorts strings from two successive rows in the ListStore.
        /// </summary>
        /// <param name="model">Model of the ListStore.</param>
        /// <param name="a">First row</param>
        /// <param name="b">Second row</param>
        /// <param name="x">Column number (0-indexed)</param>
        /// <returns>-1 if the first string is lexographically less than the second. 1 otherwise.</returns>
        private int SortStrings(TreeModel model, TreeIter a, TreeIter b, int x)
        {
            string s1 = (string)model.GetValue(a, x);
            string s2 = (string)model.GetValue(b, x);
            return string.Compare(s1, s2);
        }

        /// <summary>
        /// Sorts 2 integers and returns an indication of their relative values.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="n"></param>
        private int SortInts(TreeModel model, TreeIter a, TreeIter b, int n)
        {
            int x, y;
            if (!int.TryParse((string)model.GetValue(a, n), out x) || !Int32.TryParse((string)model.GetValue(b, n), out y))
                return -1;
            return x.CompareTo(y);
        }

        /// <summary>
        /// Sorts 2 progress strings (an integer followed by a % sign).
        /// </summary>
        /// <param name="model"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        private int SortProgress(TreeModel model, TreeIter a, TreeIter b)
        {
            int columnIndex = (int)Columns.Progress;
            if (!int.TryParse(((string)model.GetValue(a, columnIndex)).Replace("%", ""), out int x))
                return -1;
            if (!int.TryParse(((string)model.GetValue(b, columnIndex)).Replace("%", ""), out int y))
                return 1;

            return x.CompareTo(y);
        }

        /// <summary>
        /// Compare two date/time strings stored in a ListStore.
        /// </summary>
        /// <param name="model">The tree model containing the data.</param>
        /// <param name="a">TreeIter pointing to a row in the tree.</param>
        /// <param name="b">TreeIter pointing to a row in the tree.</param>
        /// <param name="n">The column (4 for start time, 5 for end time).</param>
        private int SortDateStrings(TreeModel model, TreeIter a, TreeIter b, int n)
        {
            if (!(n == (int)Columns.StartTime || n == (int)Columns.EndTime)) return -1;
            string str1 = (string)model.GetValue(a, n);
            string str2 = (string)model.GetValue(b, n);
            
            // if either of these strings is empty, the job is still running
            if (string.IsNullOrEmpty(str1))
            {
                if (string.IsNullOrEmpty(str2))
                    // Neither job has finished.
                    return 0;
                else
                    // First job is still running, second is finished.
                    return 1;
            }
            else if (string.IsNullOrEmpty(str2))
                // First job is finished, second job still running.
                return -1;

            // otherwise, both jobs are still running
            DateTime t1 = DateTime.Parse(str1, System.Globalization.CultureInfo.CurrentCulture);
            DateTime t2 = DateTime.Parse(str2, System.Globalization.CultureInfo.CurrentCulture);

            return DateTime.Compare(t1, t2);
        }

        /// <summary>
        /// Sorts two CPU time TimeSpans in the ListStore.
        /// </summary>
        private int SortCpuTime(TreeModel model, TreeIter a, TreeIter b)
        {
            int index = (int)Columns.CpuTime;
            string str1 = (string)model.GetValue(a, index);
            string str2 = (string)model.GetValue(b, index);

            if (string.IsNullOrEmpty(str1))
                return -1;

            if (string.IsNullOrEmpty(str2))
                return 1;

            TimeSpan t1, t2;
            if (!TimeSpan.TryParseExact(str1, TimespanFormat, null, out t1))
                return -1;
            if (!TimeSpan.TryParseExact(str2, TimespanFormat, null, out t2))
                return 1;
            return TimeSpan.Compare(t1, t2);
        }

        /// <summary>
        /// Tests whether a job should be displayed or not.
        /// Returning true means the row will be displayed.
        /// Returning false means the row will be displayed.
        /// </summary>
        /// <param name="model">The tree model.</param>
        /// <param name="iter">The tree iter.</param>
        private bool FilterOwnerFunc(TreeModel model, TreeIter iter)
        {
            try
            {
                // Always return true if the user has not checked the "show my jobs only" checkbox.
                if (!chkMyJobsOnly.Active)
                    return true;
                string owner = (string)model.GetValue(iter, columnTitles.Length);
                return string.Equals(owner, Environment.UserName, StringComparison.InvariantCultureIgnoreCase);
            }
            catch (Exception err)
            {
                ShowError(err);
                return true;
            }
        }

        /// <summary>
        /// Unbinds the event handlers.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnDestroyed(object sender, EventArgs args)
        {
            try
            {
                mainWidget.Destroyed -= OnDestroyed;
                chkMyJobsOnly.Toggled -= OnToggleFilter;
                btnChangeDownloadDir.Clicked -= OnChangeDownloadPath;
                btnDownload.Clicked -= OnDownloadJobs;
                btnDelete.Clicked -= OnDeleteJobs;
                btnSetup.Clicked -= OnSetupClicked;
                btnStop.Clicked -= OnStopJobs;

                dl.Download -= OnDoDownload;
                dl.Destroy();
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Event handler for the stop job button.
        /// Asks the user for confirmation and halts the execution of any 
        /// selected jobs which have not already finished.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private async void OnStopJobs(object sender, EventArgs e)
        {
            try
            {
                await StopJobs?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Event handler for the click event on the delete job button.
        /// Asks user for confirmation, then deletes each job the user has selected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnDeleteJobs(object sender, EventArgs e)
        {
            try
            {
                await DeleteJobs?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Opens a window allowing the user to edit cloud account credentials.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnSetupClicked(object sender, EventArgs e)
        {            
            try
            {
                SetupClicked?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Event handler for the click event on the download job button. 
        /// Creates a dialog box to prompt the user for download options.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnDownloadJobs(object sender, EventArgs e)
        {
            try
            {
                dl.Visible = true;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Invoked when the user clicks download on the "download options"
        /// dialog. Fires off an event for the presenter.
        /// </summary>
        /// <param name="sender">Event arguments.</param>
        /// <param name="e">Sender object.</param>
        private async void OnDoDownload(object sender, EventArgs e)
        {
            try
            {
                dl.Visible = false;
                await DownloadJobs?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Asks the user to select a directory from via a GUI, then sets this to be the 
        /// default download directory (stored in ApsimNG.Properties.Settings.Default).        
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnChangeDownloadPath(object sender, EventArgs e)
        {
            try
            {
                ChangeOutputPath?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }
    }
}
