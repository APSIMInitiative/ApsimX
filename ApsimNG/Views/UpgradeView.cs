namespace UserInterface.Views
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using APSIM.Shared.Utilities;
    using Gtk;
    using Interfaces;

    /// <summary>
    /// An upgrade form.
    /// </summary>
    public class UpgradeView : ViewBase
    {
        public class Upgrade
        {
            public DateTime ReleaseDate { get; set; }
            public int IssueNumber { get; set; }
            public string IssueTitle { get; set; }
            public string IssueURL { get; set; }
            public string ReleaseURL { get; set; }
        }

        /// <summary>
        /// A list of potential upgrades available.
        /// </summary>
        private Upgrade[] upgrades = new Upgrade[0];

        /// <summary>
        /// A list of all possible upgrades and downgrades.
        /// </summary>
        private Upgrade[] allUpgrades = new Upgrade[0];

        private bool loadFailure = false;

        /// <summary>
        /// Our explorer presenter.
        /// </summary>
        private IMainView tabbedExplorerView;

        // Glade widgets
        private Window window1 = null;
        private Button button1 = null;
        private Button button2 = null;
        private Table table1 = null;
        private Table table2 = null;
        private Entry firstNameBox = null;
        private Entry lastNameBox = null;
        private Entry organisationBox = null;
        private Entry emailBox = null;
        private ComboBox countryBox = null;
        private Label label1 = null;
        private Alignment htmlAlign = null;
        private CheckButton checkbutton1 = null;
        private Gtk.TreeView listview1 = null;
        private Alignment alignment7 = null;
        private CheckButton oldVersions = null;
        private ListStore listmodel = new ListStore(typeof(string), typeof(string), typeof(string));
        private HTMLView htmlView;

        /// <summary>
        /// Constructor
        /// </summary>
        public UpgradeView(ViewBase owner) : base(owner)
        {
            Builder builder = BuilderFromResource("ApsimNG.Resources.Glade.UpgradeView.glade");
            window1 = (Window)builder.GetObject("window1");
            button1 = (Button)builder.GetObject("button1");
            button2 = (Button)builder.GetObject("button2");
            table1 = (Table)builder.GetObject("table1");
            table2 = (Table)builder.GetObject("table2");
            firstNameBox = (Entry)builder.GetObject("firstNameBox");
            lastNameBox = (Entry)builder.GetObject("lastNameBox");
            organisationBox = (Entry)builder.GetObject("organisationBox");
            emailBox = (Entry)builder.GetObject("emailBox");
            countryBox = (ComboBox)builder.GetObject("countryBox");
            label1 = (Label)builder.GetObject("label1");
            htmlAlign = (Alignment)builder.GetObject("HTMLalign");
            checkbutton1 = (CheckButton)builder.GetObject("checkbutton1");
            listview1 = (Gtk.TreeView)builder.GetObject("listview1");
            alignment7 = (Alignment)builder.GetObject("alignment7");
            oldVersions = (CheckButton)builder.GetObject("checkbutton2");
            listview1.Model = listmodel;

            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            if (version.Revision == 0)
            {
                button1.Sensitive = false;
                table2.Hide();
                checkbutton1.Hide();
            }

            CellRendererText textRender = new Gtk.CellRendererText();
            textRender.Editable = false;

            TreeViewColumn column0 = new TreeViewColumn("Version", textRender, "text", 0);
            listview1.AppendColumn(column0);
            column0.Sizing = TreeViewColumnSizing.Autosize;
            column0.Resizable = true;

            TreeViewColumn column1 = new TreeViewColumn("Description", textRender, "text", 1);
            listview1.AppendColumn(column1);
            column1.Sizing = TreeViewColumnSizing.Autosize;
            column1.Resizable = true;

            // Populate the combo box with a list of valid country names.
            ListStore countries = new ListStore(typeof(string));
            foreach (string country in Constants.Countries)
                countries.AppendValues(country);
            countryBox.Model = countries;

            // Add a cell renderer to the combo box.
            CellRendererText cell = new CellRendererText();
            countryBox.PackStart(cell, false);
            countryBox.AddAttribute(cell, "text", 0);

            // Make the tab order a little more sensible than the defaults
            table1.FocusChain = new Widget[] { alignment7, button1, button2 };
            table2.FocusChain = new Widget[] { firstNameBox, lastNameBox, emailBox, organisationBox, countryBox };

            htmlView = new HTMLView(new ViewBase(null));
            htmlAlign.Add(htmlView.MainWidget);
            tabbedExplorerView = owner as IMainView;

            window1.TransientFor = owner.MainWidget.Toplevel as Window;
            window1.Modal = true;
            oldVersions.Toggled += OnShowOldVersionsToggled;
            button1.Clicked += OnUpgrade;
            button2.Clicked += OnViewMoreDetail;
            window1.Destroyed += OnFormClosing;
            window1.MapEvent += OnShown;
        }

        public void Show()
        {
            window1.ShowAll();
        }

        /// <summary>
        /// Form has loaded. Populate the form.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnShown(object sender, EventArgs e)
        {
            try
            {
                window1.GdkWindow.Cursor = new Gdk.Cursor(Gdk.CursorType.Watch);
                while (Gtk.Application.EventsPending())
                    Gtk.Application.RunIteration();
                PopulateForm();
                window1.GdkWindow.Cursor = null;
                if (loadFailure)
                    window1.Destroy();
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Populates all controls on the form.
        /// </summary>
        private void PopulateForm()
        {
            listmodel.Clear();
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            try
            {
                PopulateUpgradeList();
            }
            catch (Exception)
            {
                MasterView.ShowMsgDialog("Cannot download the upgrade list.\nEither the server is down or your network connection is broken.", "Error", MessageType.Error, ButtonsType.Ok, window1);
                loadFailure = true;
                return;
            }
            if (upgrades.Length > 0)
            {
                label1.Text = "You are currently using version " + version.ToString() + ". Newer versions are listed below.";
                label1.Text = label1.Text + Environment.NewLine + "Select an upgrade below.";
            }
            else
                label1.Text = "You are currently using version " + version.ToString() + ". You are using the latest version.";


            firstNameBox.Text = Utility.Configuration.Settings.FirstName;
            lastNameBox.Text = Utility.Configuration.Settings.LastName;
            emailBox.Text = Utility.Configuration.Settings.Email;
            organisationBox.Text = Utility.Configuration.Settings.Organisation;
            countryBox.Active = Constants.Countries.ToList().IndexOf(Utility.Configuration.Settings.Country);

            WebClient web = new WebClient();

            string tempLicenseFileName = Path.Combine(Path.GetTempPath(), "APSIM_NonCommercial_RD_licence.htm");
            if (File.Exists(tempLicenseFileName))
                File.Delete(tempLicenseFileName);

            if (version.Revision == 0)
            {
                button1.Sensitive = false;
                table2.Hide();
                checkbutton1.Hide();
                htmlView.SetContents("<center><span style=\"color:red\"><b>WARNING!</b></span><br/>You are currently using a custom build<br/><b>Upgrade is not available!</b></center>", false, false);
            }
            else
            {
                try
                {
                    // web.DownloadFile(@"https://apsimdev.apsim.info/APSIM.Registration.Portal/APSIM_NonCommercial_RD_licence.htm", tempLicenseFileName);
                    // HTMLview.SetContents(File.ReadAllText(tempLicenseFileName), false, true);
                    htmlView.SetContents(@"https://apsimdev.apsim.info/APSIM.Registration.Portal/APSIM_NonCommercial_RD_licence.htm", false, true);
                }
                catch (Exception)
                {
                    ViewBase.MasterView.ShowMsgDialog("Cannot download the license.", "Error", MessageType.Error, ButtonsType.Ok, window1);
                    loadFailure = true;
                }
            }

        }

        /// <summary>
        /// Populate the upgrade list.
        /// </summary>
        private void PopulateUpgradeList()
        {
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            // version = new Version(0, 0, 0, 652);  
            if (oldVersions.Active && allUpgrades.Length < 1)
                allUpgrades = WebUtilities.CallRESTService<Upgrade[]>("https://apsimdev.apsim.info/APSIM.Builds.Service/Builds.svc/GetUpgradesSinceIssue?issueID=-1");
            else if (!oldVersions.Active && upgrades.Length < 1)
                upgrades = WebUtilities.CallRESTService<Upgrade[]>("https://apsimdev.apsim.info/APSIM.Builds.Service/Builds.svc/GetUpgradesSinceIssue?issueID=" + version.Revision);
            foreach (Upgrade upgrade in oldVersions.Active ? allUpgrades : upgrades)
            {
                string versionNumber = upgrade.ReleaseDate.ToString("yyyy.MM.dd.") + upgrade.IssueNumber;
                listmodel.AppendValues(versionNumber, upgrade.IssueTitle, "");
            }
            if (listmodel.IterNChildren() > 0)
                listview1.SetCursor(new TreePath("0"), null, false);
        }

        private int GetSelIndex()
        {
            TreePath selPath;
            TreeViewColumn selCol;
            listview1.GetCursor(out selPath, out selCol);
            return selPath != null ? selPath.Indices[0] : -1;
        }

        /// <summary>
        /// User is requesting more detail about a release.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnViewMoreDetail(object sender, EventArgs e)
        {
            try
            {
                int selIndex = GetSelIndex();
                if (selIndex >= 0)
                {
                    Upgrade[] upgradeList = oldVersions.Active ? allUpgrades : upgrades;
                    Process.Start(upgradeList[selIndex].IssueURL);
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        Gtk.MessageDialog waitDlg = null;
        string tempSetupFileName = null;
        string versionNumber = null;

        private void OnShowOldVersionsToggled(object sender, EventArgs args)
        {
            try
            {
                listmodel.Clear();
                PopulateUpgradeList();
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// User has requested an upgrade.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnUpgrade(object sender, EventArgs e)
        {
            try
            {
                int selIndex = GetSelIndex();
                if (selIndex >= 0)
                {
                    if (!checkbutton1.Active)
                        throw new Exception("You must agree to the license terms before upgrading.");

                    AssertInputsAreValid();

                    Upgrade[] upgradeList = oldVersions.Active ? allUpgrades : upgrades;
                    Upgrade upgrade = upgradeList[selIndex];
                    versionNumber = upgrade.ReleaseDate.ToString("yyyy.MM.dd.") + upgrade.IssueNumber;

                    if ((Gtk.ResponseType)ViewBase.MasterView.ShowMsgDialog("Are you sure you want to upgrade to version " + versionNumber + "?",
                                            "Are you sure?", MessageType.Question, ButtonsType.YesNo, window1) == Gtk.ResponseType.Yes)
                    {
                        // Write to the registration database.
                        AssertInputsAreValid();
                        try
                        {
                            WriteUpgradeRegistration(versionNumber);
                        }
                        catch (Exception err)
                        {
                            throw new Exception("Encountered an error while updating registration information. Please try again later.", err);
                        }

                        window1.GdkWindow.Cursor = new Gdk.Cursor(Gdk.CursorType.Watch);

                        WebClient web = new WebClient();

                        tempSetupFileName = Path.Combine(Path.GetTempPath(), "APSIMSetup.exe");

                        string sourceURL;
                        if (ProcessUtilities.CurrentOS.IsMac)
                        {
                            sourceURL = Path.ChangeExtension(upgrade.ReleaseURL, "dmg");
                            tempSetupFileName = Path.ChangeExtension(tempSetupFileName, "dmg");
                        }
                        else if (ProcessUtilities.CurrentOS.IsUnix)
                        {
                            sourceURL = System.IO.Path.ChangeExtension(upgrade.ReleaseURL, "deb");
                            tempSetupFileName = System.IO.Path.ChangeExtension(tempSetupFileName, "deb");
                        }
                        else
                            sourceURL = upgrade.ReleaseURL;

                        if (File.Exists(tempSetupFileName))
                            File.Delete(tempSetupFileName);

                        try
                        {
                            waitDlg = new Gtk.MessageDialog(window1, Gtk.DialogFlags.Modal,
                                Gtk.MessageType.Info, Gtk.ButtonsType.Cancel, "Downloading file. Please wait...");
                            waitDlg.Title = "APSIM Upgrade";
                            web.DownloadFileCompleted += Web_DownloadFileCompleted;
                            web.DownloadProgressChanged += OnDownloadProgressChanged;
                            web.DownloadFileAsync(new Uri(sourceURL), tempSetupFileName);
                            if (waitDlg.Run() == (int)ResponseType.Cancel)
                                web.CancelAsync();
                        }
                        catch (Exception err)
                        {
                            ViewBase.MasterView.ShowMsgDialog("Cannot download this release. Error message is: \r\n" + err.Message, "Error", MessageType.Error, ButtonsType.Ok, window1);
                        }
                        finally
                        {
                            if (waitDlg != null)
                            {
                                web.DownloadProgressChanged -= OnDownloadProgressChanged;
                                waitDlg.Destroy();
                                waitDlg = null;
                            }
                            if (window1 != null && window1.GdkWindow != null)
                                window1.GdkWindow.Cursor = null;
                        }

                    }
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Throws if user has not provided info in a mandatory field.
        /// </summary>
        private void AssertInputsAreValid()
        {
            if (string.IsNullOrWhiteSpace(firstNameBox.Text) || 
                string.IsNullOrWhiteSpace(lastNameBox.Text) ||
                string.IsNullOrWhiteSpace(emailBox.Text) || 
                string.IsNullOrWhiteSpace(countryBox.ActiveText))
                throw new Exception("The mandatory details at the bottom of the screen (denoted with an asterisk) must be completed.");
        }

        /// <summary>
        /// Invoked when the download progress changes.
        /// Updates the progress bar.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            try
            {
                Gtk.Application.Invoke(delegate
                {
                    try
                    {
                        double progress = 100.0 * e.BytesReceived / e.TotalBytesToReceive;
                        waitDlg.Text = string.Format("Downloading file: {0:0.}%. Please wait...", progress);
                    }
                    catch (Exception err)
                    {
                        ShowError(err);
                    }
                });
            }
            catch (Exception err)
            {
                err = new Exception("Error updating download progress", err);
                ShowError(err);
            }
        }

        private void Web_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            try
            {
                if (waitDlg != null)
                {
                    waitDlg.Destroy();
                    waitDlg = null;
                }
                if (!e.Cancelled && !string.IsNullOrEmpty(tempSetupFileName) && versionNumber != null)
                {
                    try
                    {
                        if (e.Error != null) // On Linux, we get to this point even when errors have occurred
                            throw e.Error;

                        if (File.Exists(tempSetupFileName))
                        {
                            // Copy the separate upgrader executable to the temp directory.
                            string sourceUpgraderFileName = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Updater.exe");
                            string upgraderFileName = Path.Combine(Path.GetTempPath(), "Updater.exe");

                            // Check to see if upgrader is already running for whatever reason.
                            // Kill them if found.
                            foreach (Process process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(upgraderFileName)))
                                process.Kill();

                            // Delete the old upgrader.
                            if (File.Exists(upgraderFileName))
                                File.Delete(upgraderFileName);
                            // Copy in the new upgrader.
                            File.Copy(sourceUpgraderFileName, upgraderFileName, true);

                            // Run the upgrader.
                            string binDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                            string ourDirectory = Path.GetFullPath(Path.Combine(binDirectory, ".."));
                            string newDirectory = Path.GetFullPath(Path.Combine(ourDirectory, "..", "APSIM" + versionNumber));
                            string arguments = StringUtilities.DQuote(ourDirectory) + " " +
                                               StringUtilities.DQuote(newDirectory);

                            ProcessStartInfo info = new ProcessStartInfo();
                            if (ProcessUtilities.CurrentOS.IsMac)
                            {
                                info.FileName = "mono";
                                info.Arguments = upgraderFileName + " " + arguments;
                            }
                            else
                            {
                                info.FileName = upgraderFileName;
                                info.Arguments = arguments;
                            }
                            info.WorkingDirectory = Path.GetTempPath();
                            Process.Start(info);
                            window1.GdkWindow.Cursor = null;

                            // Shutdown the user interface
                            window1.Destroy();
                            tabbedExplorerView.Close();
                        }
                    }
                    catch (Exception err)
                    {
                        window1.GdkWindow.Cursor = null;
                        Application.Invoke(delegate
                        {
                            ViewBase.MasterView.ShowMsgDialog(err.Message, "Installation Error", MessageType.Error, ButtonsType.Ok, window1);
                        });
                    }
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Write to the registration database.
        /// </summary>
        private void WriteUpgradeRegistration(string version)
        {
            string url = "https://apsimdev.apsim.info/APSIM.Registration.Service/Registration.svc/AddRegistration";
            url += "?firstName=" + firstNameBox.Text;

            url = AddToURL(url, "lastName", lastNameBox.Text);
            url = AddToURL(url, "organisation", organisationBox.Text);
            url = AddToURL(url, "country", countryBox.ActiveText);
            url = AddToURL(url, "email", emailBox.Text);
            url = AddToURL(url, "product", "APSIM Next Generation");
            url = AddToURL(url, "version", version);
            url = AddToURL(url, "platform", GetPlatform());
            url = AddToURL(url, "type", "Upgrade");

            try
            {
                WebUtilities.CallRESTService<object>(url);
            }
            catch
            {
                // Retry once.
                WebUtilities.CallRESTService<object>(url);
            }
        }

        /// <summary>Add a key / value pair to url if not empty</summary>
        private string AddToURL(string url, string key, string value)
        {
            if (value == null || value == string.Empty)
                value = "-";
            return url + "&" + key + "=" + value;
        }

        /// <summary>
        /// Gets the platform name used when writing to registration database.
        /// </summary>
        private string GetPlatform()
        {
            if (ProcessUtilities.CurrentOS.IsWindows)
                return "Windows";
            else if (ProcessUtilities.CurrentOS.IsMac)
                return "Mac";
            else if (ProcessUtilities.CurrentOS.IsLinux)
                return "Linux";
            return "?";
        }

        /// <summary>
        /// Form is closing - save personal details.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnFormClosing(object sender, EventArgs e)
        {
            try
            {
                Utility.Configuration.Settings.FirstName = firstNameBox.Text;
                Utility.Configuration.Settings.LastName = lastNameBox.Text;
                Utility.Configuration.Settings.Email = emailBox.Text;
                Utility.Configuration.Settings.Organisation = organisationBox.Text;
                Utility.Configuration.Settings.Country = countryBox.ActiveText;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }
    }
}
