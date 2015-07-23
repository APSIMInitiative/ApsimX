// -----------------------------------------------------------------------
// <copyright file="UpgradeForm.cs"  company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Forms
{
    using APSIM.Shared.Utilities;
    using global::UserInterface.BuildService;
    using global::UserInterface.Presenters;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Text;
    using System.Windows.Forms;

    /// <summary>
    /// An upgrade form.
    /// </summary>
    public partial class UpgradeForm : Form
    {
        /// <summary>
        /// A list of potential upgrades available.
        /// </summary>
        private Upgrade[] upgrades;

        /// <summary>
        /// Our explorer presenter.
        /// </summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>
        /// Constructor
        /// </summary>
        public UpgradeForm(ExplorerPresenter explorerPresenter)
        {
            InitializeComponent();
            this.explorerPresenter = explorerPresenter;
        }

        /// <summary>
        /// Form has loaded. Populate the form.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnShown(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            PopulateForm();
            Cursor.Current = Cursors.Default;
        }

        /// <summary>
        /// Populates all controls on the form.
        /// </summary>
        private void PopulateForm()
        {
            Version version = new Version(Application.ProductVersion);
            //if (version.Major == 0)
            //    label1.Text = "You are currently using a custom build of APSIM. You cannot upgrade this to a newer version.";
            //else
            {
                label1.Text = "You are currently using version " + version.ToString() + ". Newer versions are listed below. Right click on them to show more detail or to upgrade.";
                PopulateUpgradeList();
            }
        }

        /// <summary>
        /// Populate the upgrade list.
        /// </summary>
        private void PopulateUpgradeList()
        {
            listView1.Items.Clear();
            using (BuildService.BuildProviderClient buildService = new BuildService.BuildProviderClient())
            {
                upgrades = buildService.GetUpgradesSincePullRequest(0);
                foreach (Upgrade upgrade in upgrades)
                {
                    string versionNumber = upgrade.ReleaseDate.ToString("yyyy.MM.dd.") + upgrade.pullRequest;
                    ListViewItem newItem = new ListViewItem(versionNumber);
                    newItem.SubItems.Add(upgrade.IssueTitle);
                    listView1.Items.Add(newItem);
                }

            }
        }

        /// <summary>
        /// User is requesting more detail about a release.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnViewMoreDetail(object sender, EventArgs e)
        {
            if (listView1.SelectedIndices.Count == 1)
                Process.Start(upgrades[listView1.SelectedIndices[0]].IssueURL);
        }

        /// <summary>
        /// User has requested an upgrade.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnUpgrade(object sender, EventArgs e)
        {
            if (listView1.SelectedIndices.Count == 1)
            {
                try
                {
                    Upgrade upgrade = upgrades[listView1.SelectedIndices[0]];
                    string versionNumber = upgrade.ReleaseDate.ToString("yyyy.MM.dd.") + upgrade.pullRequest;

                    if (MessageBox.Show("Are you sure you want to upgrade to version " + versionNumber + "?",
                                        "Are you sure?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        explorerPresenter.Save();

                        WebClient web = new WebClient();

                        string tempSetupFileName = Path.Combine(Path.GetTempPath(), "APSIMSetup.exe");
                        if (File.Exists(tempSetupFileName))
                            File.Delete(tempSetupFileName);

                        try
                        {
                            web.DownloadFile(upgrade.ReleaseURL, tempSetupFileName);
                        }
                        catch (Exception err)
                        {
                            MessageBox.Show("Cannot download this release. Error message is: \r\n" + err.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }

                        if (File.Exists(tempSetupFileName))
                        {
                            // Copy the separate upgrader executable to the temp directory.
                            string sourceUpgraderFileName = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Updater.exe");
                            string upgraderFileName = Path.Combine(Path.GetTempPath(), "Updater.exe");

                            // Delete the old upgrader.
                            if (File.Exists(upgraderFileName))
                                File.Delete(upgraderFileName);

                            // Copy in the new upgrader.
                            File.Copy(sourceUpgraderFileName, upgraderFileName, true);

                            // Run the upgrader.
                            string binDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                            string ourDirectory = Path.GetFullPath(Path.Combine(binDirectory, ".."));
                            string newDirectory = Path.GetFullPath(Path.Combine(ourDirectory, "..", versionNumber));
                            string arguments = StringUtilities.DQuote(ourDirectory) + " " + 
                                               StringUtilities.DQuote(newDirectory);
                            Process.Start(upgraderFileName, arguments);

                            // Shutdown the user interface
                            Close();
                            explorerPresenter.Close();
                        }
                    }
                }
                catch (Exception err)
                {
                    MessageBox.Show(err.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

    }
}
