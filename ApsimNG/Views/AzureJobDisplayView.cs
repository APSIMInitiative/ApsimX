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

        private TreeView tree;
        private ListStore store;
        private TreeViewColumn columnName;
        //private TreeViewColumn columnId;
        private TreeViewColumn columnState;
        private TreeViewColumn columnProgress;
        private TreeViewColumn columnStartTime;
        private TreeViewColumn columnEndTime;
        private TreeViewColumn columnDownload;
        private TreeViewColumn columnDelete;

        private CellRendererText cellName;
        //private CellRendererText cellId;
        private CellRendererText cellState;
        private CellRendererText cellProgress;
        private CellRendererText cellStartTime;
        private CellRendererText cellEndTime;
        private CellRendererPixbuf cellDownload;
        private CellRendererPixbuf cellDelete;

        private TreeModelFilter filterOwner;
        private TreeModelSort sort;
        private VBox vboxPrimary;
        private CheckButton chkFilterOwner;
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
        
        public AzureJobDisplayView(ViewBase owner) : base(owner)
        {
            jobList = new List<JobDetails>();
            store = new ListStore(typeof(string), typeof(string), typeof(string), typeof(string), typeof(string));

            // create colummns
            columnName = new TreeViewColumn
            {
                Title = "Name/Description",
                SortColumnId = 0
            };

            /*
            columnId = new TreeViewColumn
            {
                Title = "Job ID",
                SortColumnId = 1
            };
            */

            columnState = new TreeViewColumn
            {
                Title = "Status",
                SortColumnId = 1
            };

            columnProgress = new TreeViewColumn
            {
                Title = "Progress",
                SortColumnId = 2
            };

            columnStartTime = new TreeViewColumn
            {
                Title = "Start Time",
                SortColumnId = 3
            };

            columnEndTime = new TreeViewColumn
            {
                Title = "End Time",
                SortColumnId = 4,
            };

            columnDownload = new TreeViewColumn
            {
                Title = "Download"
            };

            columnDelete = new TreeViewColumn
            {
                Title = "Delete"
            };

            // create cells for each column
            cellName = new CellRendererText();
            //cellId = new CellRendererText();
            cellState = new CellRendererText();
            cellProgress = new CellRendererText();
            cellStartTime = new CellRendererText();
            cellEndTime = new CellRendererText();
            cellDownload = new CellRendererPixbuf();
            cellDownload.Pixbuf = new Gdk.Pixbuf(null, "ApsimNG.Resources.Download.png");

            cellDelete = new CellRendererPixbuf();
            cellDelete.Pixbuf = new Gdk.Pixbuf(null, "ApsimNG.Resources.Delete16.png");

            // bind cells to column
            columnName.PackStart(cellName, false);
            //columnId.PackStart(cellId, false);
            columnState.PackStart(cellState, false);
            columnProgress.PackStart(cellProgress, false);
            columnStartTime.PackStart(cellStartTime, false);
            columnEndTime.PackStart(cellEndTime, false);
            columnDownload.PackStart(cellDownload, false);
            columnDelete.PackStart(cellDelete, false);
            
            columnName.AddAttribute(cellName, "text", 0);
            //columnId.AddAttribute(cellId, "text", 1);
            columnState.AddAttribute(cellState, "text", 1);
            columnProgress.AddAttribute(cellProgress, "text", 2);
            columnStartTime.AddAttribute(cellStartTime, "text", 3);
            columnEndTime.AddAttribute(cellEndTime, "text", 4);

            tree = new TreeView();
            tree.ButtonPressEvent += TreeClickEvent;
            //tree.RowActivated += TreeRowActivated;
            
            tree.AppendColumn(columnName);
            //tree.AppendColumn(columnId);
            tree.AppendColumn(columnState);
            tree.AppendColumn(columnProgress);
            tree.AppendColumn(columnStartTime);
            tree.AppendColumn(columnEndTime);
            tree.AppendColumn(columnDownload);
            tree.AppendColumn(columnDelete);

            chkFilterOwner = new CheckButton("Display my jobs only");
            myJobsOnly = false;
            chkFilterOwner.Toggled += ApplyFilter;
            chkFilterOwner.Yalign = 0;

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

            progress = new HBox();
            progress.PackStart(new Label("Loading Jobs: "), false, false, 0);
            progress.PackStart(loadingProgress, false, false, 0);            
                        
            vboxPrimary = new VBox();
            vboxPrimary.PackStart(scroll, true, true, 0);
            vboxPrimary.PackStart(chkFilterOwner, false, true, 0);
            vboxPrimary.PackStart(tblButtonContainer, false, false, 0);
            vboxPrimary.PackStart(lblDownloadStatus, false, false, 0);
            
            vboxPrimary.PackEnd(progress, false, false, 0);

            _mainWidget = vboxPrimary;
        }

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
            string id = GetIdFromName((string)tree.Model.GetValue(iter, 0));
            if (x < downloadRightDist)
            {
                Presenter.DownloadResults(id);
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
        /// Event Handler for the "view my jobs only" checkbox. Re-applies the job owner filter.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="e"></param>
        private void ApplyFilter(object o, EventArgs e)
        {
            myJobsOnly = !myJobsOnly;
            filterOwner.Refilter();
            UpdateTreeView();
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

        public void UpdateDownloadStatus(string path, bool successful)
        {
            lblDownloadStatus.Text = successful ? "Successfully downloaded results to " + path : "Failed to download results to " + path;                                    
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
