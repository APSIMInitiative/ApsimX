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
            listView1.Items.Clear();
            Version version = new Version(Application.ProductVersion);
            if (version.Major == 0)
                label1.Text = "You are currently using a custom build of APSIM. You cannot upgrade this to a newer version.";
            else
            {
                label1.Text = "You are currently using version " + version.ToString() + ". Newer versions are listed below.";
                PopulateUpgradeList();
            }

            firstNameBox.Text = Utility.Configuration.Settings.FirstName;
            lastNameBox.Text = Utility.Configuration.Settings.LastName;
            organisationBox.Text = Utility.Configuration.Settings.Organisation;
            address1Box.Text = Utility.Configuration.Settings.Address1;
            address2Box.Text = Utility.Configuration.Settings.Address2;
            cityBox.Text = Utility.Configuration.Settings.City;
            stateBox.Text = Utility.Configuration.Settings.State;
            postcodeBox.Text = Utility.Configuration.Settings.Postcode;
            countryBox.Text = Utility.Configuration.Settings.Country;
            emailBox.Text = Utility.Configuration.Settings.Email;

            WebClient web = new WebClient();

            string tempLicenseFileName = Path.Combine(Path.GetTempPath(), "APSIMLicense.rtf");
            if (File.Exists(tempLicenseFileName))
                File.Delete(tempLicenseFileName);

            try
            {
                web.DownloadFile(@"https://www.apsim.info/ProductRegistration/APSIMDisclaimer.rtf", tempLicenseFileName);
                htmlView1.SetContents(File.ReadAllText(tempLicenseFileName), false);
            }
            catch (Exception)
            {
                MessageBox.Show("Cannot download the license.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

        }

        /// <summary>
        /// Populate the upgrade list.
        /// </summary>
        private void PopulateUpgradeList()
        {
            Version version = new Version(Application.ProductVersion);
            using (BuildService.BuildProviderClient buildService = new BuildService.BuildProviderClient())
            {
                upgrades = buildService.GetUpgradesSinceIssue(version.Revision);
                foreach (Upgrade upgrade in upgrades)
                {
                    string versionNumber = upgrade.ReleaseDate.ToString("yyyy.MM.dd.") + upgrade.issueNumber;
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
                    if (!checkBox1.Checked)
                        throw new Exception("You must agree to the license terms before upgrading.");

                    if (firstNameBox.Text == null || lastNameBox.Text == null ||
                        emailBox.Text == null || countryBox.Text == null)
                        throw new Exception("The mandatory details at the bottom of the screen (denoted with an asterisk) must be completed.");

                    Upgrade upgrade = upgrades[listView1.SelectedIndices[0]];
                    string versionNumber = upgrade.ReleaseDate.ToString("yyyy.MM.dd.") + upgrade.issueNumber;

                    if (MessageBox.Show("Are you sure you want to upgrade to version " + versionNumber + "?",
                                        "Are you sure?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        Cursor.Current = Cursors.WaitCursor;

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

                        // Write to the registration database.
                        WriteUpgradeRegistration(versionNumber);

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
                            string newDirectory = Path.GetFullPath(Path.Combine(ourDirectory, "..", "APSIM" + versionNumber));
                            string arguments = StringUtilities.DQuote(ourDirectory) + " " + 
                                               StringUtilities.DQuote(newDirectory);

                            ProcessStartInfo info = new ProcessStartInfo();
                            info.FileName = upgraderFileName;
                            info.Arguments = arguments;
                            info.WorkingDirectory = Path.GetTempPath();
                            Process.Start(info);

                            Cursor.Current = Cursors.Default;

                            // Shutdown the user interface
                            Close();
                            explorerPresenter.Close();
                        }
                    }
                }
                catch (Exception err)
                {
                    Cursor.Current = Cursors.Default;
                    MessageBox.Show(err.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// Write to the registration database.
        /// </summary>
        private void WriteUpgradeRegistration(string version)
        {
            using (BuildService.BuildProviderClient buildService = new BuildService.BuildProviderClient())
            {
                buildService.RegisterUpgrade(firstNameBox.Text, lastNameBox.Text, organisationBox.Text,
                                             address1Box.Text, address2Box.Text, cityBox.Text,
                                             stateBox.Text, postcodeBox.Text, countryBox.Text,
                                             emailBox.Text, version);
            }
        }


        /// <summary>
        /// Form is closing - save personal details.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            Utility.Configuration.Settings.FirstName = firstNameBox.Text;
            Utility.Configuration.Settings.LastName = lastNameBox.Text;
            Utility.Configuration.Settings.Organisation = organisationBox.Text;
            Utility.Configuration.Settings.Address1 = address1Box.Text;
            Utility.Configuration.Settings.Address2 = address2Box.Text;
            Utility.Configuration.Settings.City = cityBox.Text;
            Utility.Configuration.Settings.State = stateBox.Text;
            Utility.Configuration.Settings.Postcode = postcodeBox.Text;
            Utility.Configuration.Settings.Country = countryBox.Text;
            Utility.Configuration.Settings.Email = emailBox.Text;
        }
    }
}
