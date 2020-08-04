using System;
using Gtk;
using System.IO;
using System.Threading.Tasks;
using ApsimNG.EventArguments;
using UserInterface.Extensions;
using System.Collections.Generic;

namespace UserInterface.Views
{
    /// <summary>
    /// A view for submitting a job to be run on a cloud platform.
    /// </summary>
    public class RunOnCloudView : ViewBase
    {
        private RadioButton radioApsimDir;
        private RadioButton radioApsimZip;
        private Entry entryApsimDir;
        private Entry entryApsimZip;
        private Button btnApsimDir;
        private Button btnApsimZip;
        private Button BtnOK;
        private ComboBox comboCoreCount;
        private Label lblStatus;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="owner">Owner view.</param>
        public RunOnCloudView(ViewBase owner) : base(owner)
        {
            // This vbox holds both alignment objects (which in turn hold the frames).
            VBox vboxPrimary = new VBox(false, 10);

            // This is the alignment object which holds the azure job frame.
            Alignment primaryContainer = new Alignment(0f, 0f, 0f, 0f);
            primaryContainer.LeftPadding = primaryContainer.RightPadding = primaryContainer.TopPadding = primaryContainer.BottomPadding = 5;

            // Azure Job Frame.
            Frame frmAzure = new Frame();

            Alignment alignTblAzure = new Alignment(0.5f, 0.5f, 1f, 1f);
            alignTblAzure.LeftPadding = alignTblAzure.RightPadding = alignTblAzure.TopPadding = alignTblAzure.BottomPadding = 5;

            // Azure table - contains all fields in the azure job frame.
#if NETFRAMEWORK
            Table tblAzure = new Table(4, 2, false);
#else
            Grid tblAzure = new Grid();
#endif
            tblAzure.RowSpacing = 5;

            // Number of cores
            Label lblCores = new Label("Number of CPU cores to use:");
            lblCores.Xalign = 0;
            lblCores.Yalign = 0.5f;

            // Use the same core count options as in MARS (16, 32, 48, 64, ... , 128, 256)
            List<string> coreCounts = new List<string>();
            for (int i = 16; i <= 128; i += 16)
                coreCounts.Add(i.ToString());
            coreCounts.Add("256");

#if NETFRAMEWORK
            comboCoreCount = ComboBox.NewText();
            foreach (string core in coreCounts)
                comboCoreCount.AppendText(core);
#else
            comboCoreCount = new ComboBox(coreCounts.ToArray());
#endif
            comboCoreCount.Active = 0;

            // Combo boxes cannot be aligned, so it is placed in an alignment object, which can be aligned.
            Alignment comboAlign = new Alignment(0f, 0.5f, 0.25f, 1f);
            comboAlign.Add(comboCoreCount);

            tblAzure.Attach(lblCores, 0, 1, 1, 2, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
            tblAzure.Attach(comboAlign, 1, 2, 1, 2, (AttachOptions.Fill | AttachOptions.Expand), AttachOptions.Fill, 20, 0);

            // Alignment to ensure a 5px border around the inside of the frame
            Alignment alignModel = new Alignment(0f, 0f, 1f, 1f);
            alignModel.LeftPadding = alignModel.RightPadding = alignModel.TopPadding = alignModel.BottomPadding = 5;
            Table tblModel = new Table(2, 3, false);
            tblModel.ColumnSpacing = 5;
            tblModel.RowSpacing = 10;

            // Apsim Version Selection frame/table.
            Frame frmVersion = new Frame("APSIM Next Generation Version Selection");
#if NETFRAMEWORK
            Table tblVersion = new Table(2, 3, false);
#else
            Grid tblVersion = new Grid();
#endif
            tblVersion.ColumnSpacing = 5;
            tblVersion.RowSpacing = 10;

            // Alignment to ensure a 5px border on the inside of the frame.
            Alignment alignVersion = new Alignment(0f, 0f, 1f, 1f);
            alignVersion.LeftPadding = alignVersion.RightPadding = alignVersion.TopPadding = alignVersion.BottomPadding = 5;

            // Options for running apsim from a directory.
            radioApsimDir = new RadioButton("Use APSIM Next Generation from a directory");
            radioApsimDir.Toggled += new EventHandler(RadioApsimDir_Changed);

            // Populate this input field with the directory containing this executable.
            entryApsimDir = new Entry(Directory.GetParent(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)).ToString());
            entryApsimDir.WidthChars = 50;
            btnApsimDir = new Button("...");
            btnApsimDir.Clicked += new EventHandler(OnChooseApsimDir);
            tblVersion.Attach(radioApsimDir, 0, 1, 0, 1, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
            tblVersion.Attach(entryApsimDir, 1, 2, 0, 1, (AttachOptions.Fill | AttachOptions.Expand), AttachOptions.Fill, 0, 0);
            tblVersion.Attach(btnApsimDir, 2, 3, 0, 1, AttachOptions.Fill, AttachOptions.Fill, 0, 0);

            // Options for running apsim from a .zip archive.
            radioApsimZip = new RadioButton(radioApsimDir, "Use a zipped version of APSIM Next Generation");
            radioApsimZip.Toggled += new EventHandler(OnChangeApsimSource);
            entryApsimZip = new Entry();
            btnApsimZip = new Button("...");
            btnApsimZip.Clicked += new EventHandler(OnChooseApsimZipFile);

            tblVersion.Attach(radioApsimZip, 0, 1, 1, 2, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
            tblVersion.Attach(entryApsimZip, 1, 2, 1, 2, (AttachOptions.Fill | AttachOptions.Expand), AttachOptions.Fill, 0, 0);
            tblVersion.Attach(btnApsimZip, 2, 3, 1, 2, AttachOptions.Fill, AttachOptions.Fill, 0, 0);

            alignVersion.Add(tblVersion);
            frmVersion.Add(alignVersion);

            tblAzure.Attach(frmVersion, 0, 2, 3, 4, AttachOptions.Fill, AttachOptions.Fill, 0, 0);

            // Toggle the default radio button to ensure appropriate entries/buttons are greyed out by default.
            radioApsimDir.Active = true;
            radioApsimZip.Active = true;
            radioApsimDir.Active = true;

            // Add azure job table to azure alignment, and add that to the azure job frame.
            alignTblAzure.Add(tblAzure);
            frmAzure.Add(alignTblAzure);

            // OK/Cancel buttons
            BtnOK = new Button("OK");
            BtnOK.Clicked += OnOKClicked;
            Button btnCancel = new Button("Cancel");
            btnCancel.Clicked += OnCancel;
            HBox hbxButtons = new HBox(true, 0);
            hbxButtons.PackEnd(btnCancel, false, true, 0);
            hbxButtons.PackEnd(BtnOK, false, true, 0);
            Alignment alignButtons = new Alignment(1f, 0f, 0.2f, 0f);
            alignButtons.Add(hbxButtons);
            lblStatus = new Label("");
            lblStatus.Xalign = 0f;

            // Add Azure frame to primary vbox
            vboxPrimary.PackStart(frmAzure, false, true, 0);

            // Add results frame to primary vbox.
            //vboxPrimary.PackStart(frameResults, false, true, 0);
            vboxPrimary.PackStart(alignButtons, false, true, 0);
            vboxPrimary.PackStart(lblStatus, false, true, 0);

            // Add primary vbox to alignment.
            primaryContainer.Add(vboxPrimary);
            mainWidget = primaryContainer;
        }

        /// <summary>
        /// Invoked when the user clicks on the OK button to submit the job.
        /// </summary>
        public event AsyncEventHandler SubmitJob;

        /// <summary>
        /// Invoked when the user clicks on the Cancel button to cancel job submission.
        /// </summary>
        public event EventHandler CancelSubmission;

        /// <summary>
        /// Path to ApsimX.
        /// </summary>
        public string ApsimXPath
        {
            get
            {
                return radioApsimDir.Active ? entryApsimDir.Text : entryApsimZip.Text;
            }
        }

        /// <summary>
        /// Number of vCPUs to use when running the job.
        /// </summary>
        public int CpuCount
        {
            get
            {
                return int.Parse(comboCoreCount.GetActiveText());
            }
        }

        /// <summary>
        /// Job submission status.
        /// </summary>
        public string Status
        {
            get
            {
                return lblStatus.Text;
            }
            set
            {
                Application.Invoke(delegate
                {
                    lblStatus.Text = value;
                });
            }
        }

        /// <summary>
        /// Closes the job submission panel.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnCancel(object sender, EventArgs e)
        {
            try
            {
                CancelSubmission?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Bundles up the user's settings and sends the data to the presenter to submit the job.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnOKClicked(object sender, EventArgs e)
        {
            try
            {
                await SubmitJob?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Toggle Event handler for run ApsimX from a directory radio button.
        /// Greys out the input fields/buttons associated with the other radio buttons in this group.
        /// </summary>
        private void RadioApsimDir_Changed(object sender, EventArgs e)
        {
            try
            {
                if (radioApsimDir.Active)
                {
                    entryApsimZip.IsEditable = false;
                    entryApsimZip.Sensitive = false;
                    btnApsimZip.Sensitive = false;

                    entryApsimDir.IsEditable = true;
                    entryApsimDir.Sensitive = true;
                    btnApsimDir.Sensitive = true;
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Toggle Event handler for run ApsimX from a zip file radio button.
        /// Greys out the input fields/buttons associated with the other radio buttons in this group.
        /// </summary>
        private void OnChangeApsimSource(object sender, EventArgs e)
        {
            try
            {
                if (radioApsimZip.Active)
                {
                    entryApsimDir.IsEditable = false;
                    entryApsimDir.Sensitive = false;
                    btnApsimDir.Sensitive = false;

                    entryApsimZip.IsEditable = true;
                    entryApsimZip.Sensitive = true;
                    btnApsimZip.Sensitive = true;
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        private void OnChooseApsimDir(object sender, EventArgs e)
        {
            try
            {
                entryApsimDir.Text = AskUserForFileName("Select the ApsimX folder", Utility.FileDialog.FileActionType.SelectFolder, string.Empty);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        private void OnChooseApsimZipFile(object sender, EventArgs e)
        {
            try
            {
                entryApsimZip.Text = AskUserForFileName("Please select a zipped file", Utility.FileDialog.FileActionType.Open, "Zip file (*.zip) | *.zip");
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }
       
    }
}
