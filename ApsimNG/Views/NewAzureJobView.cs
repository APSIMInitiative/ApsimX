using System;
using Gtk;
using System.IO;
using System.ComponentModel;
using ApsimNG.Cloud;

namespace UserInterface.Views
{   
    public class NewAzureJobView : ViewBase
    {
        // There are a lot of commented-out controls in this view which may be used at some point in the future.
        private Entry entryName;
        private RadioButton radioApsimDir;
        //private RadioButton radioBob;
        private RadioButton radioApsimZip;
        //private Entry entryVersion;
        //private Entry entryRevision;
        private Entry entryApsimDir;
        private Entry entryApsimZip;
        private Button btnApsimDir;
        private Button btnApsimZip;
        //private Button btnBob;
        private Entry entryOutputDir;
        private ComboBox comboCoreCount;
        private CheckButton chkEmail;
        private Entry entryEmail;
        private CheckButton chkDownload;
        private CheckButton chkSummarise;
        private CheckButton chkSaveModels;
        private Entry entryModelPath;
        private Button btnModelPath;
        private Label lblStatus;

        public NewAzureJobView(ViewBase owner) : base(owner)
        {
            SubmitJob = new BackgroundWorker();
            // this vbox holds both alignment objects (which in turn hold the frames)
            VBox vboxPrimary = new VBox(false, 10);

            // this is the alignment object which holds the azure job frame
            Alignment primaryContainer = new Alignment(0f, 0f, 0f, 0f);
            primaryContainer.LeftPadding = primaryContainer.RightPadding = primaryContainer.TopPadding = primaryContainer.BottomPadding = 5;

            // Azure Job Frame
            Frame frmAzure = new Frame("Azure Job");

            Alignment alignTblAzure = new Alignment(0.5f, 0.5f, 1f, 1f);
            alignTblAzure.LeftPadding = alignTblAzure.RightPadding = alignTblAzure.TopPadding = alignTblAzure.BottomPadding = 5;

            // Azure table - contains all fields in the azure job frame
            Table tblAzure = new Table(4, 2, false);
            tblAzure.RowSpacing = 5;
            // Job Name
            Label lblName = new Label("Job Description/Name:");
            lblName.Xalign = 0;
            lblName.Yalign = 0.5f;

            entryName = new Entry();

            tblAzure.Attach(lblName, 0, 1, 0, 1, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
            tblAzure.Attach(entryName, 1, 2, 0, 1, (AttachOptions.Fill | AttachOptions.Expand), AttachOptions.Fill, 20, 0);

            // Number of cores
            Label lblCores = new Label("Number of CPU cores to use:");
            lblCores.Xalign = 0;
            lblCores.Yalign = 0.5f;

            // use the same core count options as in MARS (16, 32, 48, 64, ... , 128, 256)
            comboCoreCount = ComboBox.NewText();
            for (int i = 16; i <= 128; i += 16) comboCoreCount.AppendText(i.ToString());
            comboCoreCount.AppendText("256");

            comboCoreCount.Active = 0;

            // combo boxes cannot be aligned, so it is placed in an alignment object, which can be aligned
            Alignment comboAlign = new Alignment(0f, 0.5f, 0.25f, 1f);
            comboAlign.Add(comboCoreCount);

            tblAzure.Attach(lblCores, 0, 1, 1, 2, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
            tblAzure.Attach(comboAlign, 1, 2, 1, 2, (AttachOptions.Fill | AttachOptions.Expand), AttachOptions.Fill, 20, 0);



            // User doesn't get to choose a model via the form anymore. It comes from the context of the right click
            
            // Model selection frame
            Frame frmModelSelect = new Frame("Model Selection");

            // Alignment to ensure a 5px border around the inside of the frame
            Alignment alignModel = new Alignment(0f, 0f, 1f, 1f);
            alignModel.LeftPadding = alignModel.RightPadding = alignModel.TopPadding = alignModel.BottomPadding = 5;
            Table tblModel = new Table(2, 3, false);
            tblModel.ColumnSpacing = 5;
            tblModel.RowSpacing = 10;

            chkSaveModels = new CheckButton("Save model files");
            chkSaveModels.Toggled += ChkSaveModels_Toggled;
            entryModelPath = new Entry();
            btnModelPath = new Button("...");
            btnModelPath.Clicked += BtnModelPath_Click;
            
            chkSaveModels.Active = true;
            chkSaveModels.Active = false;
            

            HBox hboxModelpath = new HBox();
            hboxModelpath.PackStart(entryModelPath, true, true, 0);
            hboxModelpath.PackStart(btnModelPath, false, false, 5);
            
            tblAzure.Attach(chkSaveModels, 0, 1, 2, 3, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
            tblAzure.Attach(hboxModelpath, 1, 2, 2, 3, (AttachOptions.Fill | AttachOptions.Expand), AttachOptions.Fill, 0, 0);

            //Apsim Version Selection frame/table		
            Frame frmVersion = new Frame("APSIM Next Generation Version Selection");
            Table tblVersion = new Table(2, 3, false);            
            tblVersion.ColumnSpacing = 5;
            tblVersion.RowSpacing = 10;

            // Alignment to ensure a 5px border on the inside of the frame
            Alignment alignVersion = new Alignment(0f, 0f, 1f, 1f);
            alignVersion.LeftPadding = alignVersion.RightPadding = alignVersion.TopPadding = alignVersion.BottomPadding = 5;

            /*
            // use from online source
            // TODO: find/implement a Bob equivalent
            HBox hbxBob = new HBox();
            radioBob = new RadioButton("Use APSIM Next Generation from an online source (Bob?)");
            radioBob.Toggled += new EventHandler(radioBob_Changed);
            Label lblVersion = new Label("Version:");
            entryVersion = new Entry();
            Label lblRevision = new Label("Revision:");
            entryRevision = new Entry();

            hbxBob.Add(radioBob);
            hbxBob.Add(lblVersion);
            hbxBob.Add(entryVersion);
            hbxBob.Add(lblRevision);
            hbxBob.Add(entryRevision);

            btnBob = new Button("...");
            btnBob.Clicked += new EventHandler(btnBob_Click);

            tblVersion.Attach(hbxBob, 0, 2, 0, 1, (AttachOptions.Fill | AttachOptions.Expand), AttachOptions.Fill, 0, 0);
            tblVersion.Attach(btnBob, 2, 3, 0, 1, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
            */
            // use Apsim from a directory

            radioApsimDir = new RadioButton("Use APSIM Next Generation from a directory");
            radioApsimDir.Toggled += new EventHandler(RadioApsimDir_Changed);
            // populate this input field with the directory containing this executable		
            entryApsimDir = new Entry(Directory.GetParent(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)).ToString());
            btnApsimDir = new Button("...");
            btnApsimDir.Clicked += new EventHandler(BtnApsimDir_Click);
            tblVersion.Attach(radioApsimDir, 0, 1, 0, 1, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
            tblVersion.Attach(entryApsimDir, 1, 2, 0, 1, (AttachOptions.Fill | AttachOptions.Expand), AttachOptions.Fill, 0, 0);
            tblVersion.Attach(btnApsimDir, 2, 3, 0, 1, AttachOptions.Fill, AttachOptions.Fill, 0, 0);

            // use a zipped version of Apsim

            radioApsimZip = new RadioButton(radioApsimDir, "Use a zipped version of APSIM Next Generation");
            radioApsimZip.Toggled += new EventHandler(RadioApsimZip_Changed);
            entryApsimZip = new Entry();
            btnApsimZip = new Button("...");
            btnApsimZip.Clicked += new EventHandler(BtnApsimZip_Click);

            tblVersion.Attach(radioApsimZip, 0, 1, 1, 2, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
            tblVersion.Attach(entryApsimZip, 1, 2, 1, 2, (AttachOptions.Fill | AttachOptions.Expand), AttachOptions.Fill, 0, 0);
            tblVersion.Attach(btnApsimZip, 2, 3, 1, 2, AttachOptions.Fill, AttachOptions.Fill, 0, 0);

            




            alignVersion.Add(tblVersion);
            frmVersion.Add(alignVersion);

            tblAzure.Attach(frmVersion, 0, 2, 3, 4, AttachOptions.Fill, AttachOptions.Fill, 0, 0);

            // toggle the default radio button to ensure appropriate entries/buttons are greyed out by default
            radioApsimDir.Active = true;
            radioApsimZip.Active = true;
            radioApsimDir.Active = true;

            // add azure job table to azure alignment, and add that to the azure job frame
            alignTblAzure.Add(tblAzure);
            frmAzure.Add(alignTblAzure);

            // Results frame
            Frame frameResults = new Frame("Results");
            // Alignment object to ensure a 10px border around the inside of the results frame		
            Alignment alignFrameResults = new Alignment(0f, 0f, 1f, 1f);
            alignFrameResults.LeftPadding = alignFrameResults.RightPadding = alignFrameResults.TopPadding = alignFrameResults.BottomPadding = 10;
            Table tblResults = new Table(4, 3, false);
            tblResults.ColumnSpacing = 5;
            tblResults.RowSpacing = 5;

            // Auto send email
            chkEmail = new CheckButton("Send email  upon completion to:");
            entryEmail = new Entry();




            tblResults.Attach(chkEmail, 0, 1, 0, 1, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
            tblResults.Attach(entryEmail, 1, 2, 0, 1, (AttachOptions.Expand | AttachOptions.Fill), AttachOptions.Fill, 0, 0);


            // Auto download results
            chkDownload = new CheckButton("Automatically download results once complete");
            chkSummarise = new CheckButton("Summarise Results");

            tblResults.Attach(chkDownload, 0, 1, 1, 2, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
            tblResults.Attach(chkSummarise, 1, 3, 1, 2, (AttachOptions.Fill | AttachOptions.Expand), AttachOptions.Fill, 0, 0);

            // Output dir

            Label lblOutputDir = new Label("Output Directory:");
            lblOutputDir.Xalign = 0;
            entryOutputDir = new Entry((string)AzureSettings.Default["OutputDir"]);

            Button btnOutputDir = new Button("...");
            btnOutputDir.Clicked += new EventHandler(BtnOutputDir_Click);

            tblResults.Attach(lblOutputDir, 0, 1, 2, 3, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
            tblResults.Attach(entryOutputDir, 1, 2, 2, 3, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
            tblResults.Attach(btnOutputDir, 2, 3, 2, 3, AttachOptions.Fill, AttachOptions.Fill, 0, 0);


            Alignment alignNameTip = new Alignment(0f, 0f, 1f, 1f);
            Label lblNameTip = new Label("(note: if you close Apsim before the job completes, the results will not be automatically downloaded)");
            alignNameTip.Add(lblNameTip);

            tblResults.Attach(alignNameTip, 0, 3, 3, 4, AttachOptions.Fill, AttachOptions.Fill, 0, 0);



            alignFrameResults.Add(tblResults);
            frameResults.Add(alignFrameResults);



            // OK/Cancel buttons
            
            BtnOK = new Button("OK");
            BtnOK.Clicked += new EventHandler(BtnOK_Click);
            Button btnCancel = new Button("Cancel");
            btnCancel.Clicked += new EventHandler(BtnCancel_Click);
            HBox hbxButtons = new HBox(true, 0);
            hbxButtons.PackEnd(btnCancel, false, true, 0);
            hbxButtons.PackEnd(BtnOK, false, true, 0);
            Alignment alignButtons = new Alignment(1f, 0f, 0.2f, 0f);            
            alignButtons.Add(hbxButtons);
            lblStatus = new Label("");
            lblStatus.Xalign = 0f;

            // Add Azure frame to primary vbox
            vboxPrimary.PackStart(frmAzure, false, true, 0);
            // add results frame to primary vbox
            vboxPrimary.PackStart(frameResults, false, true, 0);
            vboxPrimary.PackStart(alignButtons, false, true, 0);
            vboxPrimary.PackStart(lblStatus, false, true, 0);
            // Add primary vbox to alignment
            primaryContainer.Add(vboxPrimary);
            _mainWidget = primaryContainer;            
        }

        public Presenters.INewCloudJobPresenter Presenter { get; set; }
        public BackgroundWorker SubmitJob { get; set; }
        public JobParameters JobParams { get; set; }
        public Button BtnOK;

        public string JobName
        {
            get
            {
                return entryName.Text;
            }
            set
            {
                entryName.Text = value;
            }
        }

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
        /// Updates the status label.
        /// </summary>
        /// <param name="status">Status to be displayed.</param>
        public void DisplayStatus(string status)
        {
            // run IO on Gtk main loop thread
            Application.Invoke(delegate
            {
                lblStatus.Text = status;
            });
        }

        /// <summary>
        /// Closes the job submission panel.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnCancel_Click(object sender, EventArgs e)
        {
            Presenter.CancelJobSubmission();
        }

        /// <summary>
        /// Bundles up the user's settings and sends the data to the presenter to submit the job.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnOK_Click(object sender, EventArgs e)
        {
            string apsimPath = radioApsimDir.Active ? entryApsimDir.Text : entryApsimZip.Text;

            JobParams = new JobParameters
            {
                PoolVMCount = Int32.Parse(comboCoreCount.ActiveText) / 16,
                JobDisplayName = entryName.Text,
                Recipient = chkEmail.Active ? entryEmail.Text : "",
                ModelPath = chkSaveModels.Active ? entryModelPath.Text : Path.GetTempPath() + Guid.NewGuid(),
                SaveModelFiles = chkSaveModels.Active,
                ApsimFromDir = radioApsimDir.Active,
                OutputDir = entryOutputDir.Text,
                AutoDownload = chkDownload.Active,
                Summarise = chkSummarise.Active,
                ApplicationPackageVersion = Path.GetFileName(apsimPath).Substring(Path.GetFileName(apsimPath).IndexOf('-') + 1),
                ApplicationPackagePath = apsimPath
            };

            Presenter.SubmitJob(JobParams);
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

        /*
        /// <summary>
        /// Toggle Event handler for online version of ApsimX radio button.
        /// Greys out the input fields/buttons associated with the other radio buttons in this group.
        /// </summary>
        private void radioBob_Changed(object sender, EventArgs e)
        {
            if (radioBob.Active)
            {
                entryApsimDir.IsEditable = false;
                entryApsimDir.Sensitive = false;
                btnApsimDir.Sensitive = false;

                entryApsimZip.IsEditable = false;
                entryApsimZip.Sensitive = false;
                btnApsimZip.Sensitive = false;

                entryVersion.IsEditable = true;
                entryVersion.Sensitive = true;

                entryRevision.IsEditable = true;
                entryRevision.Sensitive = true;

                btnBob.Sensitive = true;
            }
        }
        */

        /// <summary>
        /// Toggle Event handler for run ApsimX from a directory radio button.
        /// Greys out the input fields/buttons associated with the other radio buttons in this group.
        /// </summary>
        private void RadioApsimDir_Changed(object sender, EventArgs e)
        {
            if (radioApsimDir.Active)
            {
                /*
                entryVersion.IsEditable = false;
                entryVersion.Sensitive = false;

                entryRevision.IsEditable = false;
                entryRevision.Sensitive = false;

                btnBob.Sensitive = false;
                */
                entryApsimZip.IsEditable = false;
                entryApsimZip.Sensitive = false;
                btnApsimZip.Sensitive = false;

                entryApsimDir.IsEditable = true;
                entryApsimDir.Sensitive = true;
                btnApsimDir.Sensitive = true;
            }
        }

        /// <summary>
        /// Toggle Event handler for run ApsimX from a zip file radio button.
        /// Greys out the input fields/buttons associated with the other radio buttons in this group.
        /// </summary>
        private void RadioApsimZip_Changed(object sender, EventArgs e)
        {
            if (radioApsimZip.Active)
            {
                /*
                entryVersion.IsEditable = false;
                entryVersion.Sensitive = false;

                entryRevision.IsEditable = false;
                entryRevision.Sensitive = false;
                entryRevision.Text = "";

                btnBob.Sensitive = false;
                */
                entryApsimDir.IsEditable = false;
                entryApsimDir.Sensitive = false;
                btnApsimDir.Sensitive = false;

                entryApsimZip.IsEditable = true;
                entryApsimZip.Sensitive = true;
                btnApsimZip.Sensitive = true;
            }
        }

        private void BtnBob_Click(object sender, EventArgs e)
        {
            // just leaving this here in case we ever end up implementing the online source functionality
        }

        private void BtnApsimDir_Click(object sender, EventArgs e)
        {
            entryApsimDir.Text = AskUserForFileName("Select the ApsimX folder", Utility.FileDialog.FileActionType.SelectFolder, string.Empty);
        }

        private void BtnApsimZip_Click(object sender, EventArgs e)
        {
            entryApsimZip.Text = AskUserForFileName("Please select a zipped file", Utility.FileDialog.FileActionType.Open, "Zip file (*.zip) | *.zip");
        }

        private void BtnOutputDir_Click(object sender, EventArgs e)
        {
            entryOutputDir.Text = AskUserForFileName("Select an output folder", Utility.FileDialog.FileActionType.SelectFolder, string.Empty);
        }        

        private void ChkSaveModels_Toggled(object sender, EventArgs e)
        {
            entryModelPath.IsEditable = chkSaveModels.Active;
            entryModelPath.Sensitive = chkSaveModels.Active;
            btnModelPath.Sensitive = chkSaveModels.Active;
        }

        private void BtnModelPath_Click(object sender, EventArgs e)
        {
            entryModelPath.Text = AskUserForFileName("Select an output folder", Utility.FileDialog.FileActionType.SelectFolder, string.Empty);
        }
    }
}
