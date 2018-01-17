using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserInterface.Interfaces;
using ApsimNG.Cloud;
using Gtk;
using GLib;
using System.IO;

namespace UserInterface.Views
{
    public class AzureJobDisplayView : ViewBase
    {
        public Presenters.AzureJobDisplayPresenter Presenter { get; set; }

        /// <summary>
        /// Whether or not only the jobs submitted by the user should be displayed.
        /// </summary>
        private bool myJobsOnly;

        /// <summary>
        /// Whether or not a csv file should be generated when results are downloaded.
        /// </summary>
        private bool exportToCsv;

        /// <summary>
        /// TreeView to display the data.
        /// </summary>
        private TreeView tree;

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

        /** columns **/
        private TreeViewColumn columnName;
        private TreeViewColumn columnId;
        private TreeViewColumn columnState;
        private TreeViewColumn columnNumSims;
        private TreeViewColumn columnProgress;
        private TreeViewColumn columnStartTime;
        private TreeViewColumn columnEndTime;
        private TreeViewColumn columnCpuTime;

        /** cells **/
        private CellRendererText cellName;
        private CellRendererText cellId;
        private CellRendererText cellState;
        private CellRendererText cellNumSims;
        private CellRendererText cellProgress;
        private CellRendererText cellStartTime;
        private CellRendererText cellEndTime;
        private CellRendererText cellCpuTime;

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

        private HBox downloadProgressContainer;


        /** controls **/

        /// <summary>
        /// Allows user to choose whether or not to save results to a csv file
        /// when downloading them.
        /// </summary>
        private CheckButton chkSaveToCsv;

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
        /// Mutual Exclusion semaphore to ensure only 1 thread can update the view at a time.
        /// </summary>
        private object updateMutex;
        
        /// <summary>
        /// Indices of the column headers. If columns are added or removed, change this.
        /// Name, ID, State, NumSims, Progress, StartTime, EndTime
        /// </summary>
        private readonly string[] columnIndices = { "Name", "ID", "State", "NumSims", "Progress", "StartTime", "EndTime", "CpuTime" };
        private enum columns { Name, ID, State, NumSims, Progress, StartTime, EndTime, CpuTime };

        private const string TIMESPAN_FORMAT = @"dddd\d\ hh\h\ mm\m\ ss\s";
        public AzureJobDisplayView(ViewBase owner) : base(owner)
        {
            updateMutex = new object();
            store = new ListStore(typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string));

            // perhaps these should be stored as a list? then just iterate over them            

            // initialise colummns
            columnName = new TreeViewColumn
            {
                Title = "Name/Description",
                SortColumnId = (int)columns.Name
            };

            columnId = new TreeViewColumn
            {
                Title = "Azure ID",
                SortColumnId = (int)columns.ID
            };

            columnState = new TreeViewColumn
            {
                Title = "Status",
                SortColumnId = (int)columns.State
            };

            columnNumSims = new TreeViewColumn
            {
                Title = "#Sims",
                SortColumnId = (int)columns.NumSims
            };

            columnProgress = new TreeViewColumn
            {
                Title = "Progress",
                SortColumnId = (int)columns.Progress
            };

            columnStartTime = new TreeViewColumn
            {
                Title = "Start Time",
                SortColumnId = (int)columns.StartTime
            };

            columnEndTime = new TreeViewColumn
            {
                Title = "End Time",
                SortColumnId = (int)columns.EndTime
            };

            columnCpuTime = new TreeViewColumn
            {
                Title = "CPU Time",
                SortColumnId = (int)columns.CpuTime
            };

            
            // create cells for each column
            cellName = new CellRendererText();
            cellId = new CellRendererText();
            cellState = new CellRendererText();
            cellNumSims = new CellRendererText();
            cellProgress = new CellRendererText();
            cellStartTime = new CellRendererText();
            cellEndTime = new CellRendererText();
            cellCpuTime = new CellRendererText();

            // bind cells to column
            columnName.PackStart(cellName, false);
            columnId.PackStart(cellId, false);
            columnState.PackStart(cellState, false);
            columnNumSims.PackStart(cellNumSims, false);
            columnProgress.PackStart(cellProgress, false);
            columnStartTime.PackStart(cellStartTime, false);
            columnEndTime.PackStart(cellEndTime, false);
            columnCpuTime.PackStart(cellCpuTime, false);
            
            columnName.AddAttribute(cellName, "text", (int)columns.Name);
            columnId.AddAttribute(cellId, "text", (int)columns.ID);
            columnState.AddAttribute(cellState, "text", (int)columns.State);
            columnNumSims.AddAttribute(cellNumSims, "text", (int)columns.NumSims);
            columnProgress.AddAttribute(cellProgress, "text", (int)columns.Progress);
            columnStartTime.AddAttribute(cellStartTime, "text", (int)columns.StartTime);
            columnEndTime.AddAttribute(cellEndTime, "text", (int)columns.EndTime);
            columnCpuTime.AddAttribute(cellCpuTime, "text", (int)columns.CpuTime);

            // bind columns to tree view
            tree = new TreeView();            
            tree.AppendColumn(columnName);
            tree.AppendColumn(columnId);
            tree.AppendColumn(columnState);
            tree.AppendColumn(columnNumSims);
            tree.AppendColumn(columnProgress);
            tree.AppendColumn(columnStartTime);
            tree.AppendColumn(columnEndTime);
            tree.AppendColumn(columnCpuTime);
            // allow user to select multiple jobs simultaneously
            tree.Selection.Mode = SelectionMode.Multiple;
            tree.CanFocus = true;
            tree.RubberBanding = true;

            // this filter holds the model (data) and is used to filter jobs based on whether 
            // they were submitted by the user
            filterOwner = new TreeModelFilter(store, null);
            filterOwner.VisibleFunc = FilterOwnerFunc;
            filterOwner.Refilter();

            // the filter then goes into this TreeModelSort, which is used to sort results when
            // the user clicks on a column header
            sort = new TreeModelSort(filterOwner);                        
            sort.SetSortFunc(Array.IndexOf(columnIndices, "Name"), SortName);
            sort.SetSortFunc(Array.IndexOf(columnIndices, "ID"), SortId);
            sort.SetSortFunc(Array.IndexOf(columnIndices, "State"), SortState);            
            sort.SetSortFunc(Array.IndexOf(columnIndices, "NumSims"), SortNumSims);
            sort.SetSortFunc(Array.IndexOf(columnIndices, "Progress"), SortProgress);
            sort.SetSortFunc(Array.IndexOf(columnIndices, "StartTime"), SortStartDate);
            sort.SetSortFunc(Array.IndexOf(columnIndices, "EndTime"), SortEndDate);
            sort.SetSortFunc((int)columns.CpuTime, SortCpuTime);

            // the tree holds the sorted, filtered data
            tree.Model = sort;

            // the tree goes into this ScrolledWindow, allowing users to scroll down
            // to view more jobs
            ScrolledWindow scroll = new ScrolledWindow();
            scroll.Add(tree);
            // never allow horizontal scrolling, and only allow vertical scrolling when needed
            scroll.HscrollbarPolicy = PolicyType.Never;
            scroll.VscrollbarPolicy = PolicyType.Automatic;

            // the scrolled window goes into this frame to distinguish the job view 
            // from the controls beside it
            Frame treeContainer = new Frame("Azure Jobs");
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
            btnChangeDownloadDir.Clicked += btnChangeDownloadDir_Click;

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

            btnSetup = new Button("Setup");
            btnSetup.Clicked += BtnSetup_Click;
            HBox setupButtonContainer = new HBox();
            setupButtonContainer.PackStart(btnSetup, false, true, 0);

            chkSaveToCsv = new CheckButton("Export results to .csv file");
            chkSaveToCsv.Active = false;
            exportToCsv = false;
            chkSaveToCsv.Toggled += ChkSaveToCsv_Toggled;

            progress = new HBox();
            progress.PackStart(new Label("Loading Jobs: "), false, false, 0);
            progress.PackStart(loadingProgress, false, false, 0);

            vboxDownloadStatuses = new VBox();

            /*
            vboxPrimary = new VBox();
            vboxPrimary.PackStart(treeContainer, true, true, 0);
            vboxPrimary.PackStart(chkFilterOwner, false, true, 0);
            vboxPrimary.PackStart(tempHbox, false, false, 0);
            vboxPrimary.PackStart(tempHbox2, false, false, 0);
            vboxPrimary.PackStart(tempHBox3, false, false, 0);
            vboxPrimary.PackStart(chkSaveToCsv, false, true, 0);

            vboxPrimary.PackStart(tblButtonContainer, false, false, 0);
            vboxPrimary.PackStart(lblDownloadStatus, false, false, 0);
            vboxPrimary.PackStart(vboxDownloadStatuses, false, true, 0);
            vboxPrimary.PackEnd(progress, false, false, 0);
            */

            // to force a button to not horizontally fill a container, the button is placed in its own container
            HBox tempHBox = new HBox();

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

            _mainWidget = vboxPrimary;
            vboxPrimary.ShowAll();

            downloadProgressContainer.HideAll();
        }

        /// <summary>
        /// Creates a dialog box asking if the user wishes to continue.
        /// </summary>
        /// <param name="msg">Message to be displayed</param>
        /// <param name="title">Title of dialog box</param>
        /// <returns>True if the user wishes to continue. False otherwise.</returns>
        public bool AskToContinue(string msg, string title)
        {
            int x = Presenter.MainPresenter.ShowMsgDialog(msg, title, MessageType.Question, ButtonsType.OkCancel);
            return x == -5;
        }

        /// <summary>
        /// Displays a warning to the user.
        /// </summary>
        /// <param name="msg"></param>
        /// <returns>True if the user wants to continue, false otherwise.</returns>
        public bool ShowWarning(string msg)
        {
            int x = Presenter.MainPresenter.ShowMsgDialog(msg, "Sanity Check Failed - High-Grade Insanity Detected!", MessageType.Warning, ButtonsType.OkCancel);
            return x == -5;
        }

        // TODO : combine functionality in these sorting methods - this is a horrible solution
        private int SortName(TreeModel model, TreeIter a, TreeIter b)
        {
            return SortStrings(model, a, b, (int)columns.Name);
        }

        private int SortId(TreeModel model, TreeIter a, TreeIter b)
        {
            return SortStrings(model, a, b, (int)columns.ID);
        }

        private int SortNumSims(TreeModel model, TreeIter a, TreeIter b)
        {
            long x, y;
            int columnIndex = (int)columns.NumSims;
            Int64.TryParse((string)model.GetValue(a, columnIndex), out x);
            Int64.TryParse((string)model.GetValue(b, columnIndex), out y);

            if (x < y) return -1;
            if (x == y) return 0;
            return 1;
        }


        private int SortState(TreeModel model, TreeIter a, TreeIter b)
        {
            return SortStrings(model, a, b, (int)columns.State);
        }

        private int SortProgress(TreeModel model, TreeIter a, TreeIter b)
        {
            int x, y;
            int columnIndex = (int)columns.Progress;
            Int32.TryParse(((string)model.GetValue(a, columnIndex)).Replace("%", ""), out x);
            Int32.TryParse(((string)model.GetValue(b, columnIndex)).Replace("%", ""), out y);

            if (x < y) return -1;
            if (x == y) return 0;
            return 1;            
        }

        /// <summary>
        /// Event Handler for the "view my jobs only" checkbutton. Re-applies the job owner filter.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ApplyFilter(object sender, EventArgs e)
        {
            myJobsOnly = !myJobsOnly;
            //filterOwner.Refilter();
            TreeIter iter;            
            store.GetIterFirst(out iter);
            for (int i = 0; i < store.IterNChildren(); i++)
            {                
                string id = (string)store.GetValue(iter, 1);
                string name = Presenter.GetJobName(id, !myJobsOnly);                
                store.SetValue(iter, 0, name);
                store.IterNext(ref iter);
            }            
        }

        /// <summary>
        /// Event Handler for toggling the "Export to csv" checkbutton.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChkSaveToCsv_Toggled(object sender, EventArgs e)
        {
            exportToCsv = !exportToCsv;
        }

        /// <summary>
        /// Tests whether a job should be displayed or not.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="iter"></param>
        /// <returns>True if the display my jobs only checkbox is inactive, or if the job's owner is the same as the user's username. False otherwise.</returns>
        private bool FilterOwnerFunc(TreeModel model, TreeIter iter)
        {            
            string owner = Presenter.GetLocalJob((string)model.GetValue(iter, 1)).Owner;
            return !myJobsOnly || owner.ToLower() == Environment.UserName.ToLower();            
        }

        /// <summary>
        /// Sorts two date strings in the ListStore.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>-1 if the first date is before the second. 1 otherwise.</returns>
        private int SortStartDate(TreeModel model, TreeIter a, TreeIter b)
        {
            return SortDateStrings(model, a, b, (int)columns.StartTime);
        }

        /// <summary>
        /// Sorts two date strings in the ListStore.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns>-1 if the first date is before the second. 1 otherwise.</returns>
        private int SortEndDate(TreeModel model, TreeIter a, TreeIter b)
        {
            return SortDateStrings(model, a, b, (int)columns.EndTime);
        }

        private int SortCpuTime(TreeModel model, TreeIter a, TreeIter b)
        {
            int index = (int)columns.CpuTime;            
            string str1 = (string)model.GetValue(a, index);
            string str2 = (string)model.GetValue(b, index);
            if (str1 == "" || str1 == null) return -1;
            if (str2 == "" || str2 == null) return 1;

            TimeSpan t1, t2;
            if (!TimeSpan.TryParseExact(str1, TIMESPAN_FORMAT, null, out t1)) return -1;
            if (!TimeSpan.TryParseExact(str2, TIMESPAN_FORMAT, null, out t2)) return 1;
            return TimeSpan.Compare(t1, t2);
        }

        public void HideDownloadProgressBar()
        {
            Application.Invoke(delegate { downloadProgressContainer.HideAll(); });
        }

        public void ShowDownloadProgressBar()
        {
            Application.Invoke(delegate { downloadProgressContainer.ShowAll(); });
        }

        /// <summary>
        /// Makes the job load progress bar invisible.
        /// </summary>
        public void HideLoadingProgressBar()
        {
            Application.Invoke(delegate { progress.HideAll(); });
        }

        public void ShowLoadingProgressBar()
        {
            Application.Invoke(delegate { progress.ShowAll(); });
        }

        /// <summary>
        /// Displays the progress of downloading the job details from the cloud.
        /// </summary>
        /// <param name="proportion"></param>
        public void UpdateJobLoadStatus(double proportion)
        {
            Application.Invoke(delegate 
            {
                if (proportion > loadingProgress.Adjustment.Upper)
                {
                    loadingProgress.Adjustment.Value = loadingProgress.Adjustment.Upper;
                }
                else
                {
                    loadingProgress.Adjustment.Value = proportion;
                }
            });
            
        }

        /// <summary>
        /// Sorts two date/time strings in the ListStore.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="n">The column - 4 for start time, 5 for end time</param>
        /// <returns>Less than zero if the first date is earlier than the second, zero if they are equal, or greater than zero if the first date is later than the second.</returns>
        private int SortDateStrings(TreeModel model, TreeIter a, TreeIter b, int n)
        {            
            if (!(n == (int)columns.StartTime || n == (int)columns.EndTime)) return -1;
            string str1 = (string)model.GetValue(a, n);
            string str2 = (string)model.GetValue(b, n);
            // if either of these strings is empty, the job is still running
            if (str1 == "")
            {
                if (str2 == "") // neither job has finished
                {                    
                    return 0;
                } else // first job is still running, second is finished
                {
                    return 1;
                }
            } else if (str2 == "")
            {
                // first job is finished, second job still running
                return -1;
            }
            // otherwise, both jobs are still running
            DateTime t1 = GetDateTimeFromString(str1);
            DateTime t2 = GetDateTimeFromString(str2);
            int x = DateTime.Compare(t1, t2);
            return DateTime.Compare(t1, t2);
        }

        /// <summary>
        /// Generates a DateTime object from a string.
        /// </summary>
        /// <param name="st">Date time string. MUST be in the format dd/mm/yyyy hh:mm:ss</param>
        /// <returns>A DateTime object representing this string.</returns>
        private DateTime GetDateTimeFromString(string st)
        {
            try
            {
                string[] separated = st.Split(' ');
                string[] date = separated[0].Split('/');
                string[] time = separated[1].Split(':');
                int year, month, day, hour, minute, second;            
                day = Int32.Parse(date[0]);
                month = Int32.Parse(date[1]);
                year = Int32.Parse(date[2]);

                hour = Int32.Parse(time[0]);
                minute = Int32.Parse(time[1]);
                second = Int32.Parse(time[2]);

                return new DateTime(year, month, day, hour, minute, second);
            }
            catch (Exception e)
            {
                ShowError(e.ToString());
            }
            return new DateTime();
        }

        /// <summary>
        /// Sorts strings from two successive rows in the ListStore. 
        /// </summary>
        /// <param name="model">The ListStore containing the data.</param>
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
        /// Empties the TreeView and refills it with the contents of jobList.
        /// Current sorting/filtering remains unchanged.
        /// </summary>
        public void UpdateTreeView(List<JobDetails> jobs)
        {
            // this function may be a critical section
            Application.Invoke(delegate
            {
                // remember which column is being sorted. If the results are not sorted at all, order by start time ascending
                int sortIndex;
                SortType order;
                if (!sort.GetSortColumnId(out sortIndex, out order))
                {
                    sortIndex = Array.IndexOf(columnIndices, "StartTime");
                    order = SortType.Ascending;
                }

                store = new ListStore(typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string), typeof(string));
                foreach (JobDetails job in jobs)
                {
                    string startTimeString = job.StartTime == null ? DateTime.UtcNow.ToLocalTime().ToString() : ((DateTime)job.StartTime).ToLocalTime().ToString();
                    string endTimeString = job.EndTime == null ? "" : ((DateTime)job.EndTime).ToLocalTime().ToString();
                    string dispName = myJobsOnly ? job.DisplayName : job.DisplayName + " (" + job.Owner + ")";
                    string progressString = job.Progress < 0 ? "Work in progress" : Math.Round(job.Progress, 2).ToString() + "%";
                    string timeStr = job.CpuTime.ToString(TIMESPAN_FORMAT);
                    store.AppendValues(dispName, job.Id, job.State, job.NumSims.ToString(), progressString, startTimeString, endTimeString, timeStr);
                }

                filterOwner = new TreeModelFilter(store, null);
                filterOwner.VisibleFunc = FilterOwnerFunc;
                filterOwner.Refilter();

                sort = new TreeModelSort(filterOwner);
                sort.SetSortFunc((int)columns.Name, SortName);
                sort.SetSortFunc((int)columns.ID, SortId);
                sort.SetSortFunc((int)columns.State, SortState);
                sort.SetSortFunc((int)columns.NumSims, SortNumSims);
                sort.SetSortFunc((int)columns.Progress, SortProgress);
                sort.SetSortFunc((int)columns.StartTime, SortStartDate);
                sort.SetSortFunc((int)columns.EndTime, SortEndDate);
                sort.SetSortFunc((int)columns.CpuTime, SortCpuTime);
                sort.SetSortColumnId(sortIndex, order);

                tree.Model = sort;
            });
                 
        }

        /// <summary>
        /// Displays an error message in a pop-up box.
        /// </summary>
        /// <param name="msg">Message to be displayed.</param>
        public void ShowError(string msg)
        {
            Presenter.ShowError(msg);
        }

        /// <summary>
        /// Tests if a string starts with a vowel.
        /// </summary>
        /// <param name="st"></param>
        /// <returns>True if st starts with a vowel, false otherwise.</returns>
        private bool StartsWithVowel(string st)
        {
            return "aeiou".IndexOf(st[0]) >= 0;
        }

        /// <summary>
        /// Opens a file chooser dialog so the user can choose a file with a specific extension.
        /// </summary>
        /// <param name="extensions">List of allowed file extensions. Extensions should not have a . in them, e.g. zip or tar or cs are valid but .cpp is not</param>
        /// <param name="extName">Name of the file type</param>
        /// <returns></returns>
        public string GetFile(List<string> extensions, string extName = "")
        {
            string path = "";
            string indefiniteArticle = StartsWithVowel(extName) ? "an" : "a";
            FileChooserDialog f = new FileChooserDialog("Choose " + indefiniteArticle + " " + extName + " file",
                                                         null,
                                                         FileChooserAction.Open,
                                                         "Cancel", ResponseType.Cancel,
                                                         "Select", ResponseType.Accept);
            FileFilter filter = new FileFilter();
            filter.Name = extName;
            foreach (string extension in extensions)
            {
                filter.AddPattern("*." + extension);
            }
            f.AddFilter(filter);

            try
            {
                if (f.Run() == (int)ResponseType.Accept)
                {
                    path = f.Filename;
                }
            }
            catch (Exception e)
            {
                Presenter.ShowError(e.ToString());
            }
            f.Destroy();
            return path;
        }

        /// <summary>
        /// Updates the text shown in the download status label.
        /// </summary>
        /// <param name="message">Message to be displayed.</param>
        public void UpdateDownloadStatus(string message)
        {
            Application.Invoke(delegate 
            { 
                lblDownloadStatus.Text = message;
            });            
        }

        /// <summary>
        /// Updates the download progress bar.
        /// </summary>
        /// <param name="progress">Progress of the download in the range [0, 1]</param>
        /// <param name="jobName">Name of the job being downloaded.</param>
        public void UpdateDownloadProgress(double progress, string jobName)
        {
            Application.Invoke(delegate
            {
                downloadProgress.Adjustment.Value = Math.Round(progress, 2);
            });
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
            TreePath[] selectedRows = tree.Selection.GetSelectedRows();

            // get the grammar right when asking for confirmation
            bool stopMultiple = selectedRows.Count() > 1;
            string msg = "Are you sure you want to stop " + (stopMultiple ? "these " + selectedRows.Count() + " jobs?" : "this job?") + " There is no way to resume their execution!";
            string label = stopMultiple ? "Stop these jobs?" : "Stop this job?";

            int response = Presenter.MainPresenter.ShowMsgDialog(msg, label, MessageType.Question, ButtonsType.YesNo);
            if (response == -8) return;

            TreeIter iter;            
            for (int i = 0; i < selectedRows.Count(); i++)
            {
                tree.Model.GetIter(out iter, selectedRows[i]);                                
                //Presenter.StopJob(tree.Model.GetValue(iter, 1));
            }
        }

        /// <summary>
        /// Event handler for the click event on the delete job button.
        /// Asks user for confirmation, then deletes each job the user has selected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnDelete_Click(object sender, EventArgs e)
        {
            TreePath[] selectedRows = tree.Selection.GetSelectedRows();

            // get the grammar right when asking for confirmation
            bool deletingMultiple = selectedRows.Count() > 1;
            string msg = "Are you sure you want to delete " + (deletingMultiple ? "these " + selectedRows.Count() + " jobs?" : "this job?");
            string label = deletingMultiple ? "Delete these jobs?" : "Delete this job?";

            // if user says no to the popup, no further action required
            int response = Presenter.MainPresenter.ShowMsgDialog(msg, label, MessageType.Question, ButtonsType.YesNo);
            if (response != -8) return;
            
            TreeIter iter;            
            for (int i = 0; i < selectedRows.Count(); i++)
            {
                tree.Model.GetIter(out iter, selectedRows[i]);                               
                Presenter.DeleteJob((string)tree.Model.GetValue(iter, 1));
            }
        }

        /// <summary>
        /// Opens a window allowing the user to edit cloud account credentials.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSetup_Click(object sender, EventArgs e)
        {
            Presenter.SetupCredentials();
        }        

        /// <summary>
        /// Detaches all event handlers from view controls.
        /// </summary>
        public void RemoveEventHandlers()
        {
            chkFilterOwner.Toggled -= ApplyFilter;
            btnChangeDownloadDir.Clicked -= btnChangeDownloadDir_Click;
            btnDownload.Clicked -= BtnDownload_Click;
            btnDelete.Clicked -= BtnDelete_Click;
            btnSetup.Clicked -= BtnSetup_Click;
            btnStop.Clicked -= BtnStop_Click;
            chkSaveToCsv.Toggled -= ChkSaveToCsv_Toggled;
        }

        /// <summary>
        /// Event handler for the click event on the download job button. 
        /// Asks the user for confirmation, then downloads the results for each
        /// job the user has selected.
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnDownload_Click(object sender, EventArgs e)
        {            
            lblDownloadStatus.Text = "";
            vboxDownloadStatuses = new VBox();
            Presenter.MainPresenter.ShowMessage("", Models.Core.Simulation.ErrorLevel.Information);
            TreePath[] selectedRows = tree.Selection.GetSelectedRows();            
            List<string> jobIds = new List<string>();
            foreach (TreePath row in selectedRows)
            {
                jobIds.Add(GetId(row));
            }            
            DownloadWindow dl = new DownloadWindow(Presenter, jobIds);            
        }
        

        /// <summary>
        /// Gets the ID of a specific job.
        /// </summary>
        /// <param name="row">Path of the row containing the job.</param>
        /// <returns></returns>
        private string GetId(TreePath row)
        {
            TreeIter iter;
            tree.Model.GetIter(out iter, row);
            return (string)tree.Model.GetValue(iter, (int)columns.ID);
        }

        /// <summary>        
        /// Asks the user to select a directory from via a GUI, then sets this to be the 
        /// default download directory (stored in ApsimNG.Properties.Settings.Default).        
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnChangeDownloadDir_Click(object sender, EventArgs e)
        {
            string dir = GetDirectory();
            // if user presed cancel, do nothing
            if (dir == "") return;

            if (Directory.Exists(dir))
            {
                ApsimNG.Properties.Settings.Default["OutputDir"] = dir;
                ApsimNG.Properties.Settings.Default.Save();
            } else
            {
                ShowError("Directory " + dir + " does not exist.");
            }
        }

        /// <summary>Opens a file chooser dialog for the user to choose a directory.</summary>	
        /// <return>
        /// The path of the chosen directory, or an empty string if the user pressed cancel or 
        /// selected a nonexistent directory.
        /// </return>
        private string GetDirectory()
        {
            var dc = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Choose the file to open"
            };
            dc.ShowDialog();
            return (Directory.Exists(dc.SelectedPath)) ? dc.SelectedPath : "";
            /*
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
            */
        }
    }
}
