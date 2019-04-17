using System;
using System.Collections.Generic;
using System.Linq;
using ApsimNG.Cloud;
using Gtk;

namespace UserInterface.Views
{
    public class CloudJobDisplayView : ViewBase
    {
        /// <summary>
        /// Whether or not only the jobs submitted by the user should be displayed.
        /// </summary>
        private bool myJobsOnly;

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

        /** containers **/
        private VBox vboxPrimary;
        private VBox vboxDownloadStatuses;
        private HBox hboxPrimary;
        private VBox controlsContainer;

        /// <summary>
        /// Container for the job load progress bar.
        /// Only visible when the job list is being updated.
        /// </summary>
        private HBox progress;

        /// <summary>
        /// Container for the download progress bar.
        /// </summary>
        private HBox downloadProgressContainer;


        /** controls **/

        /// <summary>
        /// Allows user to choose whether or not display other people's jobs.
        /// </summary>
        private CheckButton chkFilterOwner;

        /// <summary>
        /// Shows the status of downloading a job.
        /// This will probably need to be reworked when the download controls 
        /// are moved into a popup/another view.
        /// </summary>
        private Label lblDownloadStatus;

        /// <summary>
        /// Progress bar for updating the job list.
        /// </summary>
        private ProgressBar loadingProgress;

        /// <summary>
        /// Progress bar for downloading job results.
        /// </summary>
        private ProgressBar downloadProgress;

        /// <summary>
        /// Label to display info about download in progress.
        /// </summary>
        private Label lblDownloadProgress;

        /// <summary>
        /// Allows the user to change the download directory.        
        /// </summary>
        private Button btnChangeDownloadDir;

        /// <summary>
        /// Contains the change download directory button. 
        /// This should probably be removed, and the change download directory button moved into
        /// hboxCHangeDownloadDir
        /// </summary>
        private Table tblButtonContainer;

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

        /// <summary>
        /// Constructor. Initialises the jobs TreeView and the controls associated with it.
        /// </summary>
        /// <param name="owner"></param>
        public CloudJobDisplayView(ViewBase owner) : base(owner)
        {
            store = new ListStore(typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string));

            Type[] types = new Type[columnTitles.Length];
            tree = new Gtk.TreeView() { CanFocus = true, RubberBanding = true, Name = "custom_treeview" };
            tree.Selection.Mode = SelectionMode.Multiple;

            for (int i = 0; i < columnTitles.Length; i++)
            {
                types[i] = typeof(string);
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
                tree.AppendColumn(col);
            }
            store = new ListStore(types);

            // this filter holds the model (data) and is used to filter jobs based on whether 
            // they were submitted by the user
            filterOwner = new TreeModelFilter(store, null) { VisibleFunc = FilterOwnerFunc };
            filterOwner.Refilter();

            // the filter then goes into this TreeModelSort, which is used to sort results when
            // the user clicks on a column header
            sort = new TreeModelSort(filterOwner)
            {
                DefaultSortFunc = (model, a, b) => SortData(model, a, b, Array.IndexOf(columnTitles, "Start Time"))
            };
            for (int i = 0; i < columnTitles.Length; i++)
            {
                int count = i;
                sort.SetSortFunc(i, (model, a, b) => SortData(model, a, b, count));
            }


            // change the colour of every other row - makes it easier to read
            string style = "style \"custom-treestyle\"{ GtkTreeView::odd-row-color = \"#ECF2FD\" GtkTreeView::even-row-color = \"#FFFFFF\" GtkTreeView::allow-rules = 1 } widget \"*custom_treeview*\" style \"custom-treestyle\"";
            //tree.CanFocus = true;
            tree.RulesHint = true;
            Rc.ParseString(style);


            // the tree holds the sorted, filtered data
            tree.Model = sort;

            // the tree goes into this ScrolledWindow, allowing users to scroll down
            // to view more jobs
            ScrolledWindow scroll = new ScrolledWindow();
            scroll.Add(tree);
            // never allow horizontal scrolling, and only allow vertical scrolling when needed
            scroll.HscrollbarPolicy = PolicyType.Automatic;
            scroll.VscrollbarPolicy = PolicyType.Automatic;

            // the scrolled window goes into this frame to distinguish the job view 
            // from the controls beside it
            Frame treeContainer = new Frame("Cloud Jobs");
            treeContainer.Add(scroll);

            chkFilterOwner = new CheckButton("Display my jobs only");
            // display the user's jobs only by default
            myJobsOnly = true;
            chkFilterOwner.Active = true;
            chkFilterOwner.Toggled += ApplyFilter;
            chkFilterOwner.Yalign = 0;

            downloadProgress = new ProgressBar(new Adjustment(0, 0, 1, 0.01, 0.01, 1));
            lblDownloadProgress = new Label("Downloading: ");

            downloadProgressContainer = new HBox();
            downloadProgressContainer.PackStart(lblDownloadProgress, false, false, 0);
            downloadProgressContainer.PackStart(downloadProgress, false, false, 0);

            loadingProgress = new ProgressBar(new Adjustment(0, 0, 100, 0.01, 0.01, 100));
            loadingProgress.Adjustment.Lower = 0;
            loadingProgress.Adjustment.Upper = 100;



            lblDownloadStatus = new Label("");
            lblDownloadStatus.Xalign = 0;

            btnChangeDownloadDir = new Button("Change Download Directory");
            btnChangeDownloadDir.Clicked += BtnChangeDownloadDir_Click;

            tblButtonContainer = new Table(1, 1, false);
            tblButtonContainer.Attach(btnChangeDownloadDir, 0, 1, 0, 1, AttachOptions.Shrink, AttachOptions.Shrink, 0, 0);

            btnDownload = new Button("Download");
            btnDownload.Clicked += BtnDownload_Click;
            HBox downloadButtonContainer = new HBox();
            downloadButtonContainer.PackStart(btnDownload, false, true, 0);

            btnDelete = new Button("Delete Job(s)");
            btnDelete.Clicked += BtnDelete_Click;
            HBox deleteButtonContainer = new HBox();
            deleteButtonContainer.PackStart(btnDelete, false, true, 0);

            btnStop = new Button("Stop Job(s)");
            btnStop.Clicked += BtnStop_Click;
            HBox stopButtonContainer = new HBox();
            stopButtonContainer.PackStart(btnStop, false, true, 0);

            btnSetup = new Button("Credentials");
            btnSetup.Clicked += BtnSetup_Click;
            HBox setupButtonContainer = new HBox();
            setupButtonContainer.PackStart(btnSetup, false, true, 0);

            progress = new HBox();
            progress.PackStart(new Label("Loading Jobs: "), false, false, 0);
            progress.PackStart(loadingProgress, false, false, 0);

            vboxDownloadStatuses = new VBox();

            controlsContainer = new VBox();
            controlsContainer.PackStart(chkFilterOwner, false, false, 0);
            controlsContainer.PackStart(downloadButtonContainer, false, false, 0);
            controlsContainer.PackStart(stopButtonContainer, false, false, 0);
            controlsContainer.PackStart(deleteButtonContainer, false, false, 0);
            controlsContainer.PackStart(setupButtonContainer, false, false, 0);
            controlsContainer.PackEnd(tblButtonContainer, false, false, 0);

            hboxPrimary = new HBox();
            hboxPrimary.PackStart(treeContainer, true, true, 0);
            hboxPrimary.PackStart(controlsContainer, false, true, 0);


            vboxPrimary = new VBox();
            vboxPrimary.PackStart(hboxPrimary);
            vboxPrimary.PackStart(lblDownloadStatus, false, false, 0);
            vboxPrimary.PackEnd(progress, false, false, 0);
            vboxPrimary.PackEnd(downloadProgressContainer, false, false, 0);

            mainWidget = vboxPrimary;
            vboxPrimary.ShowAll();

            downloadProgressContainer.HideAll();
            HideLoadingProgressBar();
        }

        public Interfaces.ICloudJobPresenter Presenter { get; set; }

        /// <summary>
        /// Gets or sets the value of the job download status.
        /// </summary>
        public string DownloadStatus
        {
            get { return lblDownloadStatus.Text; }
            set
            {
                Invoke(delegate
                {
                    lblDownloadStatus.Text = value;
                });
            }
        }

        /// <summary>
        /// Gets or sets the value of the job load progress bar.
        /// </summary>
        public double JobLoadProgress
        {
            get { return loadingProgress.Adjustment.Value; }
            set
            {
                // If the job load progress bar starts causing issues (e.g. not (dis)appearing correctly), 
                // try using this class' Invoke() method rather than Gtk.Application.Invoke here.
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
            get { return downloadProgress.Adjustment.Value; }
            set
            {
                // Set progresss bar to whichever is smaller - the value being passed in, or the maximum value the progress bar can take.
                Invoke(delegate { downloadProgress.Adjustment.Value = Math.Min(Math.Round(value, 2), downloadProgress.Adjustment.Upper); });
            }
        }

        /// <summary>
        /// Unbinds the event handlers.
        /// </summary>
        public void Detach()
        {
            RemoveEventHandlers();
            MainWidget.Destroy();
        }

        /// <summary>
        /// Displays a warning to the user, and asks if they want to continue.
        /// </summary>
        /// <param name="msg"></param>
        /// <returns>True if the user wants to continue, false otherwise.</returns>
        public bool AskQuestion(string msg)
        {
            return (Owner as MainView).AskQuestion(msg) == QuestionResponseEnum.Yes;
        }

        /// <summary>
        /// Makes the download progress bar invisible.
        /// </summary>
        public void HideDownloadProgressBar()
        {
            Invoke(delegate { downloadProgressContainer.HideAll(); });
        }

        /// <summary>
        /// Makes the download progress bar visible.
        /// </summary>
        public void ShowDownloadProgressBar()
        {
            Invoke(() => downloadProgressContainer.ShowAll());
        }

        /// <summary>
        /// Makes the job load progress bar invisible.
        /// </summary>
        public void HideLoadingProgressBar()
        {
            Application.Invoke(delegate { progress.HideAll(); });
        }

        /// <summary>
        /// Makes the job load progress bar visible.
        /// </summary>
        public void ShowLoadingProgressBar()
        {
            Application.Invoke(delegate { progress.ShowAll(); });
        }

        /// <summary>
        /// Empties the TreeView and refills it with the contents of jobList.
        /// Current sorting/filtering remains unchanged.
        /// </summary>
        public void UpdateJobTable(List<JobDetails> jobs)
        {
            // This entire function is run on the Gtk main loop thread.
            // This may cause problems if another thread wants to modify a view at the same time,
            // but is probably better than the alternative, which is concurrent modification of live Gtk elements.
            Application.Invoke(delegate
            {
                // remember which column is being sorted. If the results are not sorted at all, order by start time ascending
                int sortIndex;
                SortType order;
                bool needToResort = sort.GetSortColumnId(out sortIndex, out order);

                store.Clear();
                foreach (JobDetails job in jobs)
                {
                    string startTimeString = job.StartTime == null ? DateTime.UtcNow.ToLocalTime().ToString() : ((DateTime)job.StartTime).ToLocalTime().ToString();
                    string endTimeString = job.EndTime == null ? "" : ((DateTime)job.EndTime).ToLocalTime().ToString();
                    string dispName = myJobsOnly ? job.DisplayName : job.DisplayName + " (" + job.Owner + ")";
                    string progressString = job.Progress < 0 ? "Work in progress" : Math.Round(job.Progress, 2).ToString() + "%";
                    string timeStr = job.CpuTime == TimeSpan.Zero ? "" : job.CpuTime.ToString(TimespanFormat);
                    string durationStr = job.Duration() == TimeSpan.Zero ? "" : job.Duration().ToString(TimespanFormat);
                    store.AppendValues(dispName, job.Id, job.State, job.NumSims.ToString(), progressString, startTimeString, endTimeString, durationStr, timeStr);
                }
            });
        }

        /// <summary>
        /// Detaches all event handlers from view controls.
        /// </summary>
        private void RemoveEventHandlers()
        {
            chkFilterOwner.Toggled -= ApplyFilter;
            btnChangeDownloadDir.Clicked -= BtnChangeDownloadDir_Click;
            btnDownload.Clicked -= BtnDownload_Click;
            btnDelete.Clicked -= BtnDelete_Click;
            btnSetup.Clicked -= BtnSetup_Click;
            btnStop.Clicked -= BtnStop_Click;
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
            {
                return SortStrings(model, a, b, i);
            }
            else if (i == (int)Columns.StartTime || i == (int)Columns.EndTime)
            {
                return SortDateStrings(model, a, b, i);
            }
            else if (i == (int)Columns.NumSims)
            {
                return SortInts(model, a, b, i);
            }
            else if (i == (int)Columns.Progress)
            {
                return SortProgress(model, a, b);
            }
            else if (i == (int)Columns.CpuTime || i == (int)Columns.Duration)
            {
                return SortCpuTime(model, a, b);
            }
            else
            {
                return SortData(model, a, b, Math.Abs(i % columnTitles.Length));
            }
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
            return String.Compare(s1, s2);
        }

        /// <summary>
        /// Sorts 2 integers and returns an indication of their relative values.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="n"></param>
        /// <returns></returns>
        private int SortInts(TreeModel model, TreeIter a, TreeIter b, int n)
        {
            int x, y;
            if (!Int32.TryParse((string)model.GetValue(a, n), out x) || !Int32.TryParse((string)model.GetValue(b, n), out y)) return -1;
            return x.CompareTo(y);
        }

        /// <summary>
        /// Sorts 2 progress strings (an integer followed by a % sign).
        /// </summary>
        /// <param name="model"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private int SortProgress(TreeModel model, TreeIter a, TreeIter b)
        {
            int x, y;
            int columnIndex = (int)Columns.Progress;
            if (!Int32.TryParse(((string)model.GetValue(a, columnIndex)).Replace("%", ""), out x))
                return -1;
            if (!Int32.TryParse(((string)model.GetValue(b, columnIndex)).Replace("%", ""), out y))
                return 1;

            if (x < y) return -1;
            if (x == y) return 0;
            return 1;
        }

        /// <summary>
        /// Sorts two date/time strings in the ListStore. Unused.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="n">The column - 4 for start time, 5 for end time</param>
        /// <returns>Less than zero if the first date is earlier than the second, zero if they are equal, or greater than zero if the first date is later than the second.</returns>
        private int SortDateStrings(TreeModel model, TreeIter a, TreeIter b, int n)
        {
            if (!(n == (int)Columns.StartTime || n == (int)Columns.EndTime)) return -1;
            string str1 = (string)model.GetValue(a, n);
            string str2 = (string)model.GetValue(b, n);
            return Presenter.CompareDateTimeStrings(str1, str2);
        }

        /// <summary>
        /// Sorts two CPU time TimeSpans in the ListStore. Unused.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private int SortCpuTime(TreeModel model, TreeIter a, TreeIter b)
        {
            int index = (int)Columns.CpuTime;
            string str1 = (string)model.GetValue(a, index);
            string str2 = (string)model.GetValue(b, index);
            if (str1 == "" || str1 == null) return -1;
            if (str2 == "" || str2 == null) return 1;
            TimeSpan t1, t2;
            if (!TimeSpan.TryParseExact(str1, TimespanFormat, null, out t1))
                return -1;
            if (!TimeSpan.TryParseExact(str2, TimespanFormat, null, out t2))
                return 1;
            return TimeSpan.Compare(t1, t2);
        }

        /// <summary>
        /// Event Handler for toggling the "view my jobs only" checkbutton. Re-applies the job owner filter and modifies the jobs' display names.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ApplyFilter(object sender, EventArgs e)
        {            
            myJobsOnly = !myJobsOnly;
            TreeIter iter;
            store.GetIterFirst(out iter);
            for (int i = 0; i < store.IterNChildren(); i++)
            {
                string id = (string)store.GetValue(iter, 1);
                string name = Presenter.GetFormattedJobName(id, !myJobsOnly);
                store.SetValue(iter, 0, name);
                store.IterNext(ref iter);
            }
        }

        /// <summary>
        /// Tests whether a job should be displayed or not.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="iter"></param>
        /// <returns>True if the display my jobs only checkbox is inactive, or if the job's owner is the same as the user's username. False otherwise.</returns>
        private bool FilterOwnerFunc(TreeModel model, TreeIter iter)
        {
            object rawId = model.GetValue(iter, 1);
            if (rawId is string)
            {
                return !myJobsOnly || Presenter.UserOwnsJob((string)rawId);
            }
            return true;
        }

        /// <summary>
        /// Tests if a string starts with a vowel.
        /// </summary>
        /// <param name="st"></param>
        /// <returns>True if st starts with a vowel, false otherwise.</returns>
        private bool StartsWithVowel(string st)
        {
            return "aeiou".Contains(st[0]);
        }

        /// <summary>
        /// Gets the IDs of all currently selected jobs.
        /// </summary>
        /// <returns></returns>
        private List<string> GetSelectedJobIds()
        {
            TreePath[] selectedRows = tree.Selection.GetSelectedRows();
            List<string> jobIds = new List<string>();
            TreeIter iter;
            for (int i = 0; i < selectedRows.Count(); i++)
            {                
                tree.Model.GetIter(out iter, selectedRows[i]);
                jobIds.Add((string)tree.Model.GetValue(iter, 1));
            }
            return jobIds;
        }

        /// <summary>
        /// Event handler for the stop job button.
        /// Asks the user for confirmation and halts the execution of any 
        /// selected jobs which have not already finished.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnStop_Click(object sender, EventArgs e)
        {
            Presenter.StopJobs(GetSelectedJobIds());
        }

        /// <summary>
        /// Event handler for the click event on the delete job button.
        /// Asks user for confirmation, then deletes each job the user has selected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnDelete_Click(object sender, EventArgs e)
        {
            Presenter.DeleteJobs(GetSelectedJobIds());
        }

        /// <summary>
        /// Opens a window allowing the user to edit cloud account credentials.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSetup_Click(object sender, EventArgs e)
        {            
            var setup = new AzureCredentialsSetup();
            setup.Finished += delegate { Presenter.GetCredentials(); }; // this ensures that the changes actually have an effect
        }

        /// <summary>
        /// Event handler for the click event on the download job button. 
        /// Asks the user for confirmation, then downloads the results for each
        /// job the user has selected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnDownload_Click(object sender, EventArgs e)
        {

            List<string> jobIds = GetSelectedJobIds();
            // Normally (when stopping or deleting) the presenter would check to make sure there is at least 1 job selected. 
            // In this case though, we check here to prevent the download pop-up from appearing if nothing is selected.
            if (jobIds.Count < 1)
            {
                Presenter.ShowMessage("Unable to download jobs: no jobs are selected", Models.Core.Simulation.MessageType.Information);
                return;
            }
            DownloadWindow dl = new DownloadWindow(Presenter, jobIds);
        }

        /// <summary>
        /// Gets the ID of a specific job.
        /// </summary>
        /// <param name="row">Path in the TreeView to the row containing the job.</param>
        /// <returns></returns>
        private string GetId(TreePath row)
        {
            TreeIter iter;
            tree.Model.GetIter(out iter, row);
            return (string)tree.Model.GetValue(iter, (int)Columns.ID);
        }

        /// <summary>
        /// Asks the user to select a directory from via a GUI, then sets this to be the 
        /// default download directory (stored in ApsimNG.Properties.Settings.Default).        
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnChangeDownloadDir_Click(object sender, EventArgs e)
        {
            Presenter.SetDownloadDirectory(GetDirectory());
        }

        /// <summary>Opens a file chooser dialog for the user to choose a directory.</summary>	
        /// <return>
        /// The path of the chosen directory, or an empty string if the user pressed cancel or 
        /// selected a nonexistent directory.
        /// </return>
        private string GetDirectory()
        {
            string path = "";
            Application.Invoke(delegate
            {
                FileChooserDialog fc = new FileChooserDialog(
                                                        "Choose the file to open",
                                                        null,
                                                        FileChooserAction.SelectFolder,
                                                        "Cancel", ResponseType.Cancel,
                                                        "Select Folder", ResponseType.Accept);
                try
                {
                    if (fc.Run() == (int)ResponseType.Accept)
                    {
                        path = fc.Filename;
                    }
                }
                catch (Exception ex)
                {
                    (Owner as MainView).ShowMessage(ex.ToString(), Models.Core.Simulation.ErrorLevel.Error);
                }
                finally
                {
                    fc.Destroy();
                }
            });

            return path;
        }

        /// <summary>
        /// Waits until all events in the main event queue are processed and then runs an action on the main UI thread.
        /// </summary>
        /// <param name="action"></param>
        private void Invoke(System.Action action)
        {
            while (GLib.MainContext.Iteration()) ;
            Application.Invoke(delegate { action(); });
        }
    }
}
