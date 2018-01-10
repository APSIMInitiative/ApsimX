using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserInterface.Interfaces;
using ApsimNG.Cloud;
using Gtk;
using GLib;
namespace UserInterface.Views
{
    public class AzureJobDisplayView : ViewBase, IAzureJobDisplayView
    {
        public Presenters.AzureJobDisplayPresenter Presenter { get; set; }

        private List<JobDetails> jobList;
        private bool myJobsOnly;
        private bool exportToCsv;

        private TreeView tree;
        private ListStore store;
        private TreeViewColumn columnName;
        //private TreeViewColumn columnId;
        private TreeViewColumn columnState;
        private TreeViewColumn columnNumSims;
        private TreeViewColumn columnProgress;
        private TreeViewColumn columnStartTime;
        private TreeViewColumn columnEndTime;

        private CellRendererText cellName;
        //private CellRendererText cellId;
        private CellRendererText cellState;
        private CellRendererText cellNumSims;
        private CellRendererText cellProgress;
        private CellRendererText cellStartTime;
        private CellRendererText cellEndTime;        

        private TreeModelFilter filterOwner;
        private TreeModelSort sort;
        private VBox vboxPrimary;
        private CheckButton chkFilterOwner;
        private CheckButton chkSaveToCsv;
        private Label lblProgress;
        private Label lblDownloadStatus;
        private ProgressBar loadingProgress;
        private HBox progress;
        private Button btnChangeDownloadDir;
        private Entry entryChangeDownloadDir;
        private Button btnSave;
        private HBox hboxChangeDownloadDir;
        private Table tblButtonContainer;

        private Button btnDirSelect;
        private Button btnDownload;
        private Button btnDelete;
        private Button btnStop;
        private VBox vboxDownloadStatuses;

        private readonly string[] columnIndices = { "Name", "State", "NumSims", "Progress", "StartTime", "EndTime" };        
        public AzureJobDisplayView(ViewBase owner) : base(owner)
        {
            jobList = new List<JobDetails>();
            store = new ListStore(typeof(string), typeof(string), typeof(string), typeof(string), typeof(string));

            // perhaps these should be stored as a list? then just iterate over them
            // initialise colummns
            columnName = new TreeViewColumn
            {
                Title = "Name/Description",
                SortColumnId = Array.IndexOf(columnIndices, "Name")
            };

            columnState = new TreeViewColumn
            {
                Title = "Status",
                SortColumnId = Array.IndexOf(columnIndices, "State")
            };

            columnNumSims = new TreeViewColumn
            {
                Title = "#Sims",
                SortColumnId = Array.IndexOf(columnIndices, "NumSims")
            };

            columnProgress = new TreeViewColumn
            {
                Title = "Progress",
                SortColumnId = Array.IndexOf(columnIndices, "Progress")
            };

            columnStartTime = new TreeViewColumn
            {
                Title = "Start Time",
                SortColumnId = Array.IndexOf(columnIndices, "StartTime")
            };

            columnEndTime = new TreeViewColumn
            {
                Title = "End Time",
                SortColumnId = Array.IndexOf(columnIndices, "EndTime")
            };

            // create cells for each column
            cellName = new CellRendererText();            
            cellState = new CellRendererText();
            cellNumSims = new CellRendererText();
            cellProgress = new CellRendererText();
            cellStartTime = new CellRendererText();
            cellEndTime = new CellRendererText();            

            // bind cells to column
            columnName.PackStart(cellName, false);            
            columnState.PackStart(cellState, false);
            columnNumSims.PackStart(cellNumSims, false);
            columnProgress.PackStart(cellProgress, false);
            columnStartTime.PackStart(cellStartTime, false);
            columnEndTime.PackStart(cellEndTime, false);            
            
            columnName.AddAttribute(cellName, "text", Array.IndexOf(columnIndices, "Name"));            
            columnState.AddAttribute(cellState, "text", Array.IndexOf(columnIndices, "State"));
            columnNumSims.AddAttribute(cellNumSims, "text", Array.IndexOf(columnIndices, "NumSims"));
            columnProgress.AddAttribute(cellProgress, "text", Array.IndexOf(columnIndices, "Progress"));
            columnStartTime.AddAttribute(cellStartTime, "text", Array.IndexOf(columnIndices, "StartTime"));
            columnEndTime.AddAttribute(cellEndTime, "text", Array.IndexOf(columnIndices, "EndTIme"));

            tree = new TreeView();
            //tree.ButtonPressEvent += TreeClickEvent;
            //tree.RowActivated += TreeRowActivated;
            
            tree.AppendColumn(columnName);            
            tree.AppendColumn(columnState);
            tree.AppendColumn(columnNumSims);
            tree.AppendColumn(columnProgress);
            tree.AppendColumn(columnStartTime);
            tree.AppendColumn(columnEndTime);            

            tree.Selection.Mode = SelectionMode.Multiple;
            tree.CanFocus = true;
            tree.RubberBanding = true;

            chkFilterOwner = new CheckButton("Display my jobs only");
            myJobsOnly = true;
            chkFilterOwner.Active = true;
            chkFilterOwner.Toggled += ApplyFilter;
            chkFilterOwner.Yalign = 0;

            filterOwner = new TreeModelFilter(store, null);
            filterOwner.VisibleFunc = FilterOwnerFunc;

            filterOwner.Refilter();
            sort = new TreeModelSort(filterOwner);
            //sort.SetDefaultSortFunc(SortStrings, null, null);
            sort.SetSortFunc(0, SortName);
            //sort.SetSortFunc(1, SortId);
            sort.SetSortFunc(1, SortState);
            sort.SetSortFunc(2, SortProgress); // can't sort a progress bar - need to prevent this from being sorted by the default sort func
            sort.SetSortFunc(3, SortStartDate);
            sort.SetSortFunc(4, SortStartDate);
            

            // sort by start time ascending by default
            //sort.SetSortColumnId(3, SortType.Ascending);
            tree.Model = sort;

            lblProgress = new Label("Loading: 0.00%");
            lblProgress.Xalign = 0f;

            loadingProgress = new ProgressBar(new Adjustment(0, 0, 100, 0.01, 0.01, 100));
            
            loadingProgress.Adjustment.Lower = 0;
            loadingProgress.Adjustment.Upper = 100;

            ScrolledWindow scroll = new ScrolledWindow();
            scroll.Add(tree);
            scroll.HscrollbarPolicy = PolicyType.Never;
            scroll.VscrollbarPolicy = PolicyType.Automatic;

            lblDownloadStatus = new Label("");
            lblDownloadStatus.Xalign = 0;

            btnChangeDownloadDir = new Button("Change Download Directory");
            btnChangeDownloadDir.Clicked += btnChangeDownloadDir_Click;

            tblButtonContainer = new Table(1, 1, false);
            tblButtonContainer.Attach(btnChangeDownloadDir, 0, 1, 0, 1, AttachOptions.Shrink, AttachOptions.Shrink, 0, 0);

            btnDownload = new Button("Download");
            btnDownload.Clicked += BtnDownload_Click;
            HBox tempHbox = new HBox();
            tempHbox.PackStart(btnDownload, false, true, 0);

            btnDelete = new Button("Delete Job(s)");
            btnDelete.Clicked += BtnDelete_Click;
            HBox tempHbox2 = new HBox();
            tempHbox2.PackStart(btnDelete, false, true, 0);

            btnStop = new Button("Stop Job(s)");
            btnStop.Clicked += BtnStop_Click;
            HBox tempHBox3 = new HBox();
            tempHBox3.PackStart(btnStop, false, true, 0);


            chkSaveToCsv = new CheckButton("Export results to .csv file");
            chkSaveToCsv.Active = false;
            exportToCsv = false;
            chkSaveToCsv.Toggled += ChkSaveToCsv_Toggled;

            progress = new HBox();
            progress.PackStart(new Label("Loading Jobs: "), false, false, 0);
            progress.PackStart(loadingProgress, false, false, 0);

            vboxDownloadStatuses = new VBox();

            vboxPrimary = new VBox();
            vboxPrimary.PackStart(scroll, true, true, 0);
            vboxPrimary.PackStart(chkFilterOwner, false, true, 0);
            vboxPrimary.PackStart(tempHbox, false, false, 0);
            vboxPrimary.PackStart(tempHbox2, false, false, 0);
            vboxPrimary.PackStart(tempHBox3, false, false, 0);
            vboxPrimary.PackStart(chkSaveToCsv, false, true, 0);

            vboxPrimary.PackStart(tblButtonContainer, false, false, 0);
            vboxPrimary.PackStart(lblDownloadStatus, false, false, 0);
            vboxPrimary.PackStart(vboxDownloadStatuses, false, true, 0);
            vboxPrimary.PackEnd(progress, false, false, 0);

            _mainWidget = vboxPrimary;
            vboxPrimary.ShowAll();
        }
        /*
         * deprecated
        /// <summary>
        /// Event handler for a click event on the TreeView. 
        /// If the click is in the download or delete column, it will perform the appropriate action on the selected job. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [GLib.ConnectBefore]
        private void TreeClickEvent(object sender, ButtonPressEventArgs e)
        {
            lblDownloadStatus.Text = "";            
            int downloadLeftDist = 0;
            int downloadRightDist = 0;
            for (int i = 0; i < tree.Columns.Length; i++)
            {
                if (tree.Columns[i].Title == "Download")
                {
                    downloadRightDist = downloadLeftDist + tree.Columns[i].Width;
                    break;
                }
                downloadLeftDist += tree.Columns[i].Width;
            }
            

            int x = Int32.Parse(((Gdk.EventButton)e.Args[0]).X.ToString());
            if (x < downloadLeftDist) return;            

            int y = Int32.Parse(((Gdk.EventButton)e.Args[0]).Y.ToString());
            
            TreePath path;
            tree.GetPathAtPos(x, y, out path);
            TreeIter iter;
            tree.Model.GetIter(out iter, path);
            int[] arr = new int[] { 1, 2 };
            string jobName = (string)tree.Model.GetValue(iter, 0);
            string id = GetIdFromName(jobName);
            if (x < downloadRightDist)
            {
                Presenter.DownloadResults(id, jobName, exportToCsv);
            } else
            {
                // delete the job                
                if (GetJobOwner(id).ToLower() != Environment.UserName.ToLower())
                {
                    Presenter.ShowError("Deleting other people's jobs is not allowed.");
                    return;
                }
                Presenter.DeleteJob(Guid.Parse(id));
            }
        }
        */

        private string GetIdFromName(string name)
        {
            if (!myJobsOnly)
            {
                string[] split = name.Split(' ');
                split = split.Take(split.Count() - 1).ToArray();
                name = String.Join(" ", split);
            }
            foreach (JobDetails x in jobList)
            {
                if (x.DisplayName == name) return x.Id.ToString();                
            }
            return "";
        }
        private int SortName(TreeModel model, TreeIter a, TreeIter b)
        {
            return SortStrings(model, a, b, 0);
        }

        private int SortId(TreeModel model, TreeIter a, TreeIter b)
        {
            return SortStrings(model, a, b, 1);
        }

        private int SortState(TreeModel model, TreeIter a, TreeIter b)
        {
            return SortStrings(model, a, b, 2);
        }

        private int SortProgress(TreeModel model, TreeIter a, TreeIter b)
        {
            return SortStrings(model, a, b, 3);
        }

        /// <summary>
        /// Event Handler for the "view my jobs only" checkbutton. Re-applies the job owner filter.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ApplyFilter(object sender, EventArgs e)
        {
            myJobsOnly = !myJobsOnly;
            filterOwner.Refilter();
            UpdateTreeView();
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
            string owner = GetJobOwner(GetIdFromName((string)model.GetValue(iter, 0)));
            return !myJobsOnly || owner.ToLower() == Environment.UserName.ToLower();            
        }

        private int SortStartDate(TreeModel model, TreeIter a, TreeIter b)
        {
            return SortDateStrings(model, a, b, 4);
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
            return SortDateStrings(model, a, b, 5);
        }


        /// <summary>
        /// Displays the progress of downloading the job details from the cloud.
        /// </summary>
        /// <param name="proportion"></param>
        public void UpdateJobLoadStatus(double proportion)
        {
            if (jobList.Count != 0) return;
            lblProgress.Text = "Loading: " + Math.Round(proportion, 2).ToString() + "%";
            loadingProgress.Adjustment.Value = proportion;
            //loadingProgress.Text = Math.Round(proportion, 2).ToString() + "%";
            if (Math.Abs(proportion - 100) < Math.Pow(10, -6))
            {
                // sometimes the UI crashes upon getting to this point
                vboxPrimary.Remove(progress);                
            }
            else if (proportion < Math.Pow(10, -6))
            {                
                vboxPrimary.PackStart(progress, false, false, 0);                                
            }
        }

        /// <summary>
        /// Sorts two date/time strings in the ListStore.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="n">The column - 4 for start time, 5 for end time</param>
        /// <returns></returns>
        private int SortDateStrings(TreeModel model, TreeIter a, TreeIter b, int n)
        {
            if (!(n == 4 || n == 5)) return -1;
            DateTime t1 = GetDateTimeFromString((string)model.GetValue(a, n));
            DateTime t2 = GetDateTimeFromString((string)model.GetValue(b, n));

            return DateTime.Compare(t1, t2);
        }

        /// <summary>
        /// Generates a DateTime object from a string.
        /// </summary>
        /// <param name="st">Date time string. MUST be in the format dd/mm/yyyy hh:mm:ss</param>
        /// <returns>A DateTime object representing this string.</returns>
        private DateTime GetDateTimeFromString(string st)
        {
            string[] separated = st.Split(' ');
            string[] date = separated[0].Split('/');
            string[] time = separated[1].Split(':');
            int year, month, day, hour, minute, second;
            try
            {
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
                ShowError(e.Message);
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
        /// Gets the owner of the job with a given id.
        /// </summary>
        /// <param name="id">ID of the job.</param>
        /// <returns>Owner of the job</returns>
        private string GetJobOwner(string id)
        {
            foreach (var job in jobList)
            {
                if (job.Id == id) return job.Owner;
            }
            return "";
        }

        /// <summary>
        /// Redraws the TreeView if and only if the list of jobs passed in is different to the list of jobs already displayed.
        /// </summary>
        /// <param name="jobs"></param>
        public void AddJobsToTableIfNecessary(List<JobDetails> jobs)
        {
            if (jobList.Count == 0)
            {
                jobList = jobs;
                UpdateTreeView();
            }
            for (int i = 0; i < jobList.Count; i++)
            {
                if (!((jobList[i].Id == jobs[i].Id) && (jobList[i].State == jobs[i].State)))
                {
                    jobList = jobs;
                    UpdateTreeView();
                }
            }

            return;
        }

        public void UpdateTreeView()
        {
            // remember which column is being sorted. If the results are not sorted at all, order by start time ascending
            int sortIndex;
            SortType order;
            if (!sort.GetSortColumnId(out sortIndex, out order))
            {
                sortIndex = 3;
                order = SortType.Ascending;
            }

            store = new ListStore(typeof(string), typeof(string), typeof(string), typeof(string), typeof(string));
            foreach (JobDetails job in jobList)
            {
                string startTimeString = job.StartTime == null ? DateTime.UtcNow.ToLocalTime().ToString() : ((DateTime)job.StartTime).ToLocalTime().ToString();
                string endTimeString = job.EndTime == null ? "" : ((DateTime)job.EndTime).ToLocalTime().ToString();
                string dispName = myJobsOnly ? job.DisplayName : job.DisplayName + " (" + job.Owner + ")";
                string progressString = job.Progress < 0 ? "Work in progress" : Math.Round(job.Progress, 2).ToString() + "%";
                store.AppendValues(dispName, job.State, progressString, startTimeString, endTimeString);
            }

            filterOwner = new TreeModelFilter(store, null);
            filterOwner.VisibleFunc = FilterOwnerFunc;
            filterOwner.Refilter();

            sort = new TreeModelSort(filterOwner);
            sort.SetSortFunc(0, SortName);
            //sort.SetSortFunc(1, SortId);
            sort.SetSortFunc(1, SortState);
            sort.SetSortFunc(2, SortProgress);
            sort.SetSortFunc(3, SortStartDate);
            sort.SetSortFunc(4, SortStartDate);

            tree.Model = sort;
            sort.SetSortColumnId(sortIndex, order);
        }

        public void RemoveJobFromJobList(Guid jobId)
        {
            foreach (JobDetails job in jobList)
            {
                if (job.Id == jobId.ToString())
                {
                    jobList.Remove(job);
                    return;
                }
            }
        }

        /// <summary>
        /// Displays an error message in a pop-up box.
        /// </summary>
        /// <param name="msg">Message to be displayed.</param>
        public void ShowError(string msg)
        {
            MessageDialog md = new MessageDialog(null, DialogFlags.DestroyWithParent, MessageType.Error, ButtonsType.Ok, msg);
            md.Title = "Sanity Check Failed - High-Grade Insanity Detected!!!";
            md.Run();
            md.Destroy();
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

        public void UpdateDownloadStatus(string message)
        {
            lblDownloadStatus.Text = message;
            vboxDownloadStatuses.PackStart(new Label(message), false, true, 0);
        }

        private void btnChangeDownloadDir_Click(object sender, EventArgs e)
        {            
            vboxPrimary.Remove(tblButtonContainer);

            entryChangeDownloadDir = new Entry((string)ApsimNG.Properties.Settings.Default["OutputDir"]);

            btnSave = new Button("Save");
            btnSave.Clicked += btnSave_Click;

            btnDirSelect = new Button("...");
            btnDirSelect.Clicked += btnDirSelect_Click;

            hboxChangeDownloadDir = new HBox();
            hboxChangeDownloadDir.PackStart(entryChangeDownloadDir, false, true, 0);
            hboxChangeDownloadDir.PackStart(btnDirSelect, false, true, 0);
            hboxChangeDownloadDir.PackStart(btnSave, false, true, 0);

            vboxPrimary.PackStart(hboxChangeDownloadDir, false, true, 0);            
            hboxChangeDownloadDir.ShowAll();
        }

        /// <summary>
        /// Event handler for the stop job button.
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
            string jobName, jobId;
            for (int i = 0; i < selectedRows.Count(); i++)
            {
                tree.Model.GetIter(out iter, selectedRows[i]);
                jobName = (string)tree.Model.GetValue(iter, 0);
                jobId = GetIdFromName(jobName);
                Presenter.StopJob(Guid.Parse(jobId));
            }

        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            TreePath[] selectedRows = tree.Selection.GetSelectedRows();

            // get the grammar right when asking for confirmation
            bool deletingMultiple = selectedRows.Count() > 1;
            string msg = "Are you sure you want to delete " + (deletingMultiple ? "these " + selectedRows.Count() + " jobs?" : "this job?");
            string label = deletingMultiple ? "Delete these jobs?" : "Delete this job?";

            // if user says no to the popup, no further action required
            if (Presenter.MainPresenter.ShowMsgDialog(msg, label, MessageType.Question, ButtonsType.YesNo) == -8) return;
            
            TreeIter iter;
            string jobName, jobId;
            for (int i = 0; i < selectedRows.Count(); i++)
            {
                tree.Model.GetIter(out iter, selectedRows[i]);
                jobName = (string)tree.Model.GetValue(iter, 0);
                jobId = GetIdFromName(jobName);                
                Presenter.DeleteJob(Guid.Parse(jobId));
            }
        }

        private void BtnDownload_Click(object sender, EventArgs e)
        {
            lblDownloadStatus.Text = "";
            vboxDownloadStatuses = new VBox();
            if (Presenter.OngoingDownload())
            {
                Presenter.ShowError("Unable to start a new batch of downloads - a download is already ongoing!");
                return;
            }

            //if there are already files in the output directory, ask the user if they want to continue
            if (System.IO.Directory.GetFiles((string)ApsimNG.Properties.Settings.Default["OutputDir"]).Length > 0 && !Presenter.ShowWarning("Files detected in output directory. Results will be generated from ALL files in this directory. Are you certain you wish to continue?"))
                return;

            TreePath[] selectedRows = tree.Selection.GetSelectedRows();
            TreeIter x;
            string jobName, jobId;
            for (int i = 0; i < selectedRows.Count(); i++)
            {                
                tree.Model.GetIter(out x, selectedRows[i]);
                jobName = (string)tree.Model.GetValue(x, 0);
                jobId = GetIdFromName(jobName);
                Presenter.DownloadResults(jobId, jobName, exportToCsv);
            }
            //lblDownloadStatus.Text = "Finished Downloading Results";
        }
        private void btnSave_Click(object sender, EventArgs e)
        {
            ApsimNG.Properties.Settings.Default["OutputDir"] = entryChangeDownloadDir.Text;
            ApsimNG.Properties.Settings.Default.Save();
            vboxPrimary.Remove(hboxChangeDownloadDir);
            vboxPrimary.PackStart(tblButtonContainer, false, false, 0);
        }
        
        private void btnDirSelect_Click(object sender, EventArgs e)
        {
            entryChangeDownloadDir.Text = GetDirectory();
        }

        /// <summary>Opens a file chooser dialog for the user to choose a directory.</summary>	
        /// <return>The path of the chosen directory</return>
        private string GetDirectory()
        {
            
            FileChooserDialog fc =
            new FileChooserDialog("Choose the file to open",
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
