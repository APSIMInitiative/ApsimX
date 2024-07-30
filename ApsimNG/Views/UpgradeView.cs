using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using APSIM.Shared.Utilities;
using Gtk;
using UserInterface.Extensions;
using UserInterface.Interfaces;

namespace UserInterface.Views
{

    /// <summary>
    /// An upgrade form.
    /// </summary>
    public class UpgradeView : ViewBase
    {
        public class Upgrade
        {
            public DateTime ReleaseDate { get; set; }
            public uint Issue { get; set; }
            public string Title { get; set; }
            public string DownloadLinkDebian { get; set; }
            public string DownloadLinkWindows { get; set; }
            public string DownloadLinkMacOS { get; set; }
            public string InfoUrl { get; set; }
            public string Version { get; set; }
            public uint Revision { get; set; }
        }

        /// <summary>
        /// A list of potential upgrades available.
        /// </summary>
        private Upgrade[] upgrades = new Upgrade[0];

        /// <summary>
        /// A list of all possible upgrades and downgrades.
        /// </summary>
        private Upgrade[] allUpgrades = new Upgrade[0];

        /// <summary>
        /// Version number that indicates custom build (normally 0; set to -1 to test upgrade during development)
        /// </summary>

        private int customBuildVersion = 0;

        private bool loadFailure = false;

        /// <summary>
        /// Our explorer presenter.
        /// </summary>
        private IMainView tabbedExplorerView;

        // Glade widgets
        private Window window1 = null;
        private Button button1 = null;
        private Button button2 = null;
        private ScrolledWindow scrolledWindow1 = null;
        private Grid grid1 = null;
        private Grid grid2 = null;
        private Entry firstNameBox = null;
        private Entry lastNameBox = null;
        private Entry organisationBox = null;
        private Entry emailBox = null;
        private ComboBox countryBox = null;
        private Label label1 = null;
        private Container licenseContainer = null;
        private CheckButton checkbutton1 = null;
        private Gtk.TreeView listview1 = null;
        private CheckButton oldVersions = null;
        private ListStore listmodel = new ListStore(typeof(string), typeof(string), typeof(string));
        private MarkdownView licenseView;

        /// <summary>
        /// Constructor
        /// </summary>
        public UpgradeView(ViewBase owner) : base(owner)
        {
            Builder builder = BuilderFromResource("ApsimNG.Resources.Glade.UpgradeView.glade");
            window1 = (Window)builder.GetObject("window1");
            button1 = (Button)builder.GetObject("button1");
            button2 = (Button)builder.GetObject("button2");
            grid1 = (Grid)builder.GetObject("grid1");
            grid2 = (Grid)builder.GetObject("grid2");
            firstNameBox = (Entry)builder.GetObject("firstNameBox");
            lastNameBox = (Entry)builder.GetObject("lastNameBox");
            organisationBox = (Entry)builder.GetObject("organisationBox");
            emailBox = (Entry)builder.GetObject("emailBox");
            countryBox = (ComboBox)builder.GetObject("countryBox");
            label1 = (Label)builder.GetObject("label1");
            licenseContainer = (Container)builder.GetObject("licenseContainer");
            checkbutton1 = (CheckButton)builder.GetObject("checkbutton1");
            listview1 = (Gtk.TreeView)builder.GetObject("listview1");
            scrolledWindow1 = (ScrolledWindow)builder.GetObject("scrolledwindow1");
            oldVersions = (CheckButton)builder.GetObject("checkbutton2");
            listview1.Model = listmodel;

            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            if (version.Build == customBuildVersion)
            {
                button1.Sensitive = false;
                grid2.Hide();
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
            grid1.FocusChain = new Widget[] { scrolledWindow1, button1, button2 };
            grid2.FocusChain = new Widget[] { firstNameBox, lastNameBox, emailBox, organisationBox, countryBox };

            licenseView = new MarkdownView(owner);
            licenseContainer.Add(licenseView.MainWidget);
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
                window1.Window.Cursor = new Gdk.Cursor(Gdk.Display.Default, Gdk.CursorType.Watch);
                while (Gtk.Application.EventsPending())
                    Gtk.Application.RunIteration();
                PopulateForm();
                window1.Window.Cursor = null;
                if (loadFailure)
                    window1.Dispose();
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

            string tempLicenseFileName = Path.Combine(Path.GetTempPath(), "APSIM_NonCommercial_RD_licence.htm");
            if (File.Exists(tempLicenseFileName))
                File.Delete(tempLicenseFileName);

            if (version.Build == customBuildVersion)
            {
                button1.Sensitive = false;
                grid2.Hide();
                checkbutton1.Hide();
                licenseView.Text = "You are currently using a custom build - **Upgrade is not available!**";
            }
            else
                licenseView.Text = ReflectionUtilities.GetResourceAsString("ApsimNG.LICENSE.md");
        }

        /// <summary>
        /// Populate the upgrade list.
        /// </summary>
        private void PopulateUpgradeList()
        {
            if (oldVersions.Active && allUpgrades.Length < 1)
                allUpgrades = GetUpgrades(-1).ToArray();
            else if (!oldVersions.Active && upgrades.Length < 1)
            {
                Version version = Assembly.GetExecutingAssembly().GetName().Version;
                upgrades = GetUpgrades(version.Build).ToArray();
            }

            foreach (Upgrade upgrade in oldVersions.Active ? allUpgrades : upgrades)
            {
                string versionNumber = $"{upgrade.ReleaseDate:yyyy.MM}.{upgrade.Revision}";
                listmodel.AppendValues(versionNumber, upgrade.Title, "");
            }
            if (listmodel.IterNChildren() > 0)
                listview1.SetCursor(new TreePath("0"), null, false);
        }

        /// <summary>
        /// Retrieve list of available upgrades from the upgrade server which
        /// are more recent than the specified revision number.
        /// </summary>
        /// <param name="minRevision">
        /// Retrieve all upgrades which are more recent than this revision
        /// number. Set to -1 for all upgrades.
        /// </param>
        private IReadOnlyList<Upgrade> GetUpgrades(int minRevision)
        {
            return WebUtilities.PostRestService<List<Upgrade>>($"https://builds.apsim.info/api/nextgen/list?min={minRevision}");
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
                    ProcessUtilities.ProcessStart(upgradeList[selIndex].InfoUrl);
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
                    versionNumber = upgrade.ReleaseDate.ToString("yyyy.MM.dd.") + upgrade.Issue;

                    if ((Gtk.ResponseType)ViewBase.MasterView.ShowMsgDialog($"Are you sure you want to upgrade to version {upgrade.Version}?",
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

                        window1.Window.Cursor = new Gdk.Cursor(Gdk.Display.Default, Gdk.CursorType.Watch);

                        tempSetupFileName = Path.Combine(Path.GetTempPath(), "APSIMSetup.exe");

                        string sourceURL;

                        if (ProcessUtilities.CurrentOS.IsMac)
                        {
                            sourceURL = upgrade.DownloadLinkMacOS;
                            tempSetupFileName = Path.ChangeExtension(tempSetupFileName, "dmg");
                        }
                        else if (ProcessUtilities.CurrentOS.IsUnix)
                        {
                            sourceURL = upgrade.DownloadLinkDebian;
                            tempSetupFileName = System.IO.Path.ChangeExtension(tempSetupFileName, "deb");
                        }
                        else
                            sourceURL = upgrade.DownloadLinkWindows;

                        if (File.Exists(tempSetupFileName))
                            File.Delete(tempSetupFileName);

                        try
                        {
                            waitDlg = new Gtk.MessageDialog(window1, Gtk.DialogFlags.Modal,
                                Gtk.MessageType.Info, Gtk.ButtonsType.Cancel, "Downloading file. Please wait...");
                            waitDlg.Title = "APSIM Upgrade";
                            var progress = new Progress<double>();
                            progress.ProgressChanged += Download_ProgressChanged;

                            var cancellationToken = new System.Threading.CancellationTokenSource();
                            FileStream file = new FileStream(tempSetupFileName, FileMode.Create, System.IO.FileAccess.Write);
                            _ = WebUtilities.GetAsyncWithProgress(sourceURL, file, progress, cancellationToken.Token, "*/*");
                            if (waitDlg.Run() == (int)ResponseType.Cancel)
                                cancellationToken.Cancel();

                        }
                        catch (Exception err)
                        {
                            ViewBase.MasterView.ShowMsgDialog("Cannot download this release. Error message is: \r\n" + err.Message, "Error", MessageType.Error, ButtonsType.Ok, window1);
                        }
                        finally
                        {
                            if (waitDlg != null)
                            {
                                waitDlg.Dispose();
                                waitDlg = null;
                            }
                            if (window1 != null && window1.Window != null)
                                window1.Window.Cursor = null;
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
        /// Invoked when the download progress changes.
        /// Updates the progress bar.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Fraction (0-1) of download which has completed</param>
        private void Download_ProgressChanged(object sender, double e)
        {
            try
            {
                Gtk.Application.Invoke(delegate
                {
                    try
                    {
                        double progress = 100.0 * e;
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
            if (e == 1.0) // Should be true only iff the file has been completely downloaded
            {
                Web_DownloadFileCompleted();
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
                string.IsNullOrWhiteSpace(countryBox.GetActiveText()))
                throw new Exception("The mandatory details at the bottom of the screen (denoted with an asterisk) must be completed.");
        }

        private void Web_DownloadFileCompleted()
        {
            try
            {
                Application.Invoke((_, __) =>
                {
                    if (waitDlg != null)
                    {
                        waitDlg.Dispose();
                        waitDlg = null;
                    }
                });
                if (!string.IsNullOrEmpty(tempSetupFileName) && versionNumber != null)
                {
                    int attemptCount = 0;
                    while (attemptCount < 2)
                    {
                        try
                        {


                            if (File.Exists(tempSetupFileName))
                            {

                                if (ProcessUtilities.CurrentOS.IsWindows)
                                {
                                    // The InnoSetup installer can be run with the /upgradefrom:xxx parameter
                                    // and will handle the removal of the previous version.
                                    string oldVersion = Models.Core.Simulations.ApsimVersion;
                                    var startInfo = new ProcessStartInfo()
                                    {
                                        FileName = tempSetupFileName,
                                        Arguments = $"/upgradefrom={oldVersion}",
                                        WorkingDirectory = Path.GetTempPath()
                                    };
                                    Process.Start(startInfo);
                                }
                                else if (ProcessUtilities.CurrentOS.IsMac)
                                {
                                    string script = Path.Combine(Path.GetTempPath(), $"apsim-upgrade-mac-{Guid.NewGuid()}.sh");
                                    ReflectionUtilities.WriteResourceToFile(GetType().Assembly, "ApsimNG.Resources.Scripts.upgrade-mac.sh", script);
                                    string apsimxDir = PathUtilities.GetAbsolutePath("%root%", null);
                                    Process.Start("/bin/sh", $"{script} {tempSetupFileName} {apsimxDir}");
                                }
                                else
                                {
                                    // Assume (Debian) Linux and hope for the best.
                                    string script = Path.Combine(Path.GetTempPath(), $"apsim-upgrade-debian-{Guid.NewGuid()}.sh");
                                    ReflectionUtilities.WriteResourceToFile(GetType().Assembly, "ApsimNG.Resources.Scripts.upgrade-debian.sh", script);
                                    Process.Start("/bin/sh", $"{script} {tempSetupFileName}");
                                }

                                attemptCount = 99;

                                Application.Invoke((_, __) =>
                                {
                                    window1.Window.Cursor = null;

                                    // Shutdown the user interface
                                    window1.Dispose();
                                    tabbedExplorerView.Close();
                                });
                            }
                        }
                        catch (Exception err)
                        {
                            // Possible that the install file is being used by another process (eg. antivirus scanner)
                            // Make one further attempt to start it after pausing for a short period, rather than failing

                            attemptCount += 1;

                            if (attemptCount < 2)
                            {

                                System.Threading.Thread.Sleep(2000);

                            }
                            else
                            {
                                Application.Invoke(delegate
                                {
                                    window1.Window.Cursor = null;
                                    ViewBase.MasterView.ShowMsgDialog(err.Message, "Installation Error", MessageType.Error, ButtonsType.Ok, window1);
                                });
                            }

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
        /// Write to the registration database.
        /// </summary>
        private void WriteUpgradeRegistration(string version)
        {
            string url = $"https://registration.apsim.info/api/upgrade?email={emailBox.Text}&version={version}&platform={GetPlatform()}";

            try
            {
                WebUtilities.PostRestService<object>(url);
            }
            catch
            {
                // Retry once.
                WebUtilities.CallRESTService<object>(url);
            }
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
            throw new PlatformNotSupportedException($"No upgrade is available for this operating system.");
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
                Utility.Configuration.Settings.Country = countryBox.GetActiveText();
                Utility.Configuration.Settings.Save();
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }
    }
}
