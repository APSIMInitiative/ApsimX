using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gtk;
using System.IO;
using System.ComponentModel;
using UserInterface.Interfaces;
using ApsimNG.Cloud;

namespace UserInterface.Views
{   
    public class NewAzureJobView : ViewBase, INewAzureJobView
    {
        public BackgroundWorker SubmitJob { get; set; }
        public JobParameters jobParams { get; set; }             
        public Button btnOK;
        public Label lblStatus;

        private Entry entryName;
        private RadioButton radioApsimDir;
        private RadioButton radioBob;
        private RadioButton radioApsimZip;
        private Entry entryVersion;
        private Entry entryRevision;
        private Entry entryApsimDir;
        private Entry entryApsimZip;
        private Button btnApsimDir;
        private Button btnApsimZip;
        private Button btnBob;
        private Entry entryOutputDir;
        private ComboBox comboCoreCount;
        private CheckButton chkEmail;
        private Entry entryEmail;
        private CheckButton chkSummarise;
        private CheckButton chkSaveModels;
        private Entry entryModelPath;
        private Button btnModelPath;



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
            Table tblAzure = new Table(5, 2, false);
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
            chkSaveModels.Toggled += chkSaveModels_Toggled;
            entryModelPath = new Entry((string)ApsimNG.Properties.Settings.Default["ModelPath"]);
            btnModelPath = new Button("...");
            btnModelPath.Clicked += btnModelPath_Click;

            chkSaveModels.Active = true;
            chkSaveModels.Active = false;
            

            HBox hboxModelpath = new HBox();
            hboxModelpath.PackStart(entryModelPath, true, true, 0);
            hboxModelpath.PackStart(btnModelPath, false, false, 5);
            
            tblAzure.Attach(chkSaveModels, 0, 1, 2, 3, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
            tblAzure.Attach(hboxModelpath, 1, 2, 2, 3, (AttachOptions.Fill | AttachOptions.Expand), AttachOptions.Fill, 0, 0);

            //Apsim Version Selection frame/table		
            Frame frmVersion = new Frame("APSIM Next Generation Version Selection");
            Table tblVersion = new Table(3, 3, false);
            tblVersion.ColumnSpacing = 5;
            tblVersion.RowSpacing = 10;

            // Alignment to ensure a 5px border on the inside of the frame
            Alignment alignVersion = new Alignment(0f, 0f, 1f, 1f);
            alignVersion.LeftPadding = alignVersion.RightPadding = alignVersion.TopPadding = alignVersion.BottomPadding = 5;

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

            // use Apsim from a directory

            radioApsimDir = new RadioButton(radioBob, "Use APSIM Next Generation from a directory");
            radioApsimDir.Toggled += new EventHandler(radioApsimDir_Changed);
            // populate this input field with the directory containing this executable		
            entryApsimDir = new Entry(Directory.GetParent(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)).ToString());
            btnApsimDir = new Button("...");
            btnApsimDir.Clicked += new EventHandler(btnApsimDir_Click);
            tblVersion.Attach(radioApsimDir, 0, 1, 1, 2, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
            tblVersion.Attach(entryApsimDir, 1, 2, 1, 2, (AttachOptions.Fill | AttachOptions.Expand), AttachOptions.Fill, 0, 0);
            tblVersion.Attach(btnApsimDir, 2, 3, 1, 2, AttachOptions.Fill, AttachOptions.Fill, 0, 0);

            // use a zipped version of Apsim

            radioApsimZip = new RadioButton(radioBob, "Use a zipped version of APSIM Next Generation");
            radioApsimZip.Toggled += new EventHandler(radioApsimZip_Changed);
            entryApsimZip = new Entry((string)ApsimNG.Properties.Settings.Default["ApsimZipPath"]);
            btnApsimZip = new Button("...");
            btnApsimZip.Clicked += new EventHandler(btnApsimZip_Click);

            tblVersion.Attach(radioApsimZip, 0, 1, 2, 3, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
            tblVersion.Attach(entryApsimZip, 1, 2, 2, 3, (AttachOptions.Fill | AttachOptions.Expand), AttachOptions.Fill, 0, 0);
            tblVersion.Attach(btnApsimZip, 2, 3, 2, 3, AttachOptions.Fill, AttachOptions.Fill, 0, 0);

            




            alignVersion.Add(tblVersion);
            frmVersion.Add(alignVersion);

            tblAzure.Attach(frmVersion, 0, 2, 3, 4, AttachOptions.Fill, AttachOptions.Fill, 0, 0);

            // toggle the default radio button to ensure appropriate entries/buttons are greyed out by default
            radioApsimDir.Active = true;
            radioApsimZip.Active = true;
            radioApsimDir.Active = (bool)ApsimNG.Properties.Settings.Default["ApsimFromDir"];

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
            CheckButton chkDownload = new CheckButton("Automatically download results once complete");
            chkSummarise = new CheckButton("Summarise Results");

            tblResults.Attach(chkDownload, 0, 1, 1, 2, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
            tblResults.Attach(chkSummarise, 1, 3, 1, 2, (AttachOptions.Fill | AttachOptions.Expand), AttachOptions.Fill, 0, 0);

            // Output dir

            Label lblOutputDir = new Label("Output Directory:");
            lblOutputDir.Xalign = 0;
            entryOutputDir = new Entry((string)ApsimNG.Properties.Settings.Default["OutputDir"]);

            Button btnOutputDir = new Button("...");
            btnOutputDir.Clicked += new EventHandler(btnOutputDir_Click);

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
            
            btnOK = new Button("OK");
            btnOK.Clicked += new EventHandler(btnOK_Click);
            Button btnCancel = new Button("Cancel");
            HBox hbxButtons = new HBox(true, 0);
            hbxButtons.PackEnd(btnCancel, false, true, 0);
            hbxButtons.PackEnd(btnOK, false, true, 0);
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

        /// <summary>
        /// Displays a warning message and asks the user if they want to continue
        /// </summary>
        /// <param name="msg">Message to be displayed</param>
        /// <returns>true if the user wants to continue, false otherwise</returns>
        public bool ShowWarning(string msg)
        {
            MessageDialog md = new MessageDialog(null, DialogFlags.DestroyWithParent, MessageType.Warning, ButtonsType.YesNo, "WARNING: " + msg + " - Continue?");
            md.Title = "Sanity Check Failed - High-Grade Insanity Detected!!!";
            ResponseType res = (ResponseType)md.Run();
            md.Destroy();
            return res == ResponseType.Yes;
        }

        /// <summary>
        /// Updates the status label. This is a temporary measure.
        /// </summary>
        /// <param name="status">Status to be displayed.</param>
        public void DisplayStatus(string status)
        {
            lblStatus.Text = status;
        }

        public void ShowError(string msg)
        {
            MessageDialog md = new MessageDialog(null, DialogFlags.DestroyWithParent, MessageType.Error, ButtonsType.Ok, msg);
            md.Title = "Sanity Check Failed - High-Grade Insanity Detected!!!";
            md.Run();
            md.Destroy();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (entryName.Text.Length < 1)
            {
                ShowError("A description is required");
                return;
            }
            
            if (!Directory.Exists(entryApsimDir.Text) && radioApsimDir.Active)
            {
                ShowError("Directory not found: " + entryApsimDir.Text);
                return;
            }

            if (!File.Exists(entryApsimZip.Text) && radioApsimZip.Active)
            {
                ShowError("File not found: " + entryApsimZip.Text);
                return;
            }

            if (comboCoreCount.ActiveText == null)
            {
                ShowError("Number of cores per CPU is a required field");
                return;
            }

            // save user's choices to ApsimNG.Properties.Settings            
            ApsimNG.Properties.Settings.Default["ApsimFromDir"] = radioApsimDir.Active;
            ApsimNG.Properties.Settings.Default["OutputDir"] = entryOutputDir.Text;
            if (entryApsimDir.Text.Length > 0) ApsimNG.Properties.Settings.Default["ApsimDirPath"] = entryApsimDir.Text;
            if (entryApsimZip.Text.Length > 0) ApsimNG.Properties.Settings.Default["ApsimZipPath"] = entryApsimZip.Text;
            if (entryModelPath.Text.Length > 0) ApsimNG.Properties.Settings.Default["ModelPath"] = entryModelPath.Text;
            ApsimNG.Properties.Settings.Default.Save();


            // TODO : test validity of online source input boxes

            jobParams = new JobParameters();
            jobParams.PoolVMCount = Int32.Parse(comboCoreCount.ActiveText) / 16;
            jobParams.JobDisplayName = entryName.Text;
            jobParams.Recipient = chkEmail.Active ? entryEmail.Text : "";
            jobParams.ModelPath = chkSaveModels.Active ? entryModelPath.Text : Path.GetTempPath() + Guid.NewGuid();
            jobParams.SaveModelFiles = chkSaveModels.Active;
            // TODO : test for running from online source
            string apsimPath = radioApsimDir.Active ? entryApsimDir.Text : entryApsimZip.Text;

            jobParams.ApplicationPackageVersion = Path.GetFileName(apsimPath).Substring(Path.GetFileName(apsimPath).IndexOf('-') + 1);
            jobParams.ApplicationPackagePath = apsimPath;

            jobParams.OutputDir = entryOutputDir.Text;
            jobParams.Summarise = chkSummarise.Active;

            SubmitJob.RunWorkerAsync(jobParams);
        }

        private void backgroundWorkerNewJob_DoWork(object o, DoWorkEventArgs e)
        {

        }

        /// <summary>Opens a file chooser dialog for the user to choose a .zip file.</summary>	
        /// <return>The path of the chosen zip file</return>
        private string GetZipFile()
        {
            // this could easily be modified to accomodate for other file types - just pass them in as arguments		
            // and zipFilter.AddPattern("*." + extension); for each extension. 
            
            string path = "";

            FileChooserDialog f = new FileChooserDialog("Choose a zip file to open",
                                                        null,
                                                        FileChooserAction.Open,
                                                        "Cancel", ResponseType.Cancel,
                                                        "Select", ResponseType.Accept);
            FileFilter zipFilter = new FileFilter();
            zipFilter.Name = "ZIP File";
            zipFilter.AddPattern("*.zip");
            f.AddFilter(zipFilter);



            try
            {
                if (f.Run() == (int)ResponseType.Accept)
                {
                    path = f.Filename;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
            f.Destroy();
            return path;
            
        }

        /// <summary>Opens a file chooser dialog for the user to choose a directory.</summary>	
        /// <return>The path of the chosen directory</return>
        private string GetDirectory()
        {
            
            Gtk.FileChooserDialog fc =
            new Gtk.FileChooserDialog("Choose the file to open",
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

        /// <summary>
        /// Toggle Event handler for run ApsimX from a directory radio button.
        /// Greys out the input fields/buttons associated with the other radio buttons in this group.
        /// </summary>
        private void radioApsimDir_Changed(object sender, EventArgs e)
        {
            if (radioApsimDir.Active)
            {
                entryVersion.IsEditable = false;
                entryVersion.Sensitive = false;

                entryRevision.IsEditable = false;
                entryRevision.Sensitive = false;

                btnBob.Sensitive = false;

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
        private void radioApsimZip_Changed(object sender, EventArgs e)
        {
            if (radioApsimZip.Active)
            {
                entryVersion.IsEditable = false;
                entryVersion.Sensitive = false;

                entryRevision.IsEditable = false;
                entryRevision.Sensitive = false;
                entryRevision.Text = "";

                btnBob.Sensitive = false;

                entryApsimDir.IsEditable = false;
                entryApsimDir.Sensitive = false;
                btnApsimDir.Sensitive = false;

                entryApsimZip.IsEditable = true;
                entryApsimZip.Sensitive = true;
                btnApsimZip.Sensitive = true;
            }
        }

        private void btnBob_Click(object sender, EventArgs e)
        {
            Console.WriteLine("Bob stuff");
        }

        private void btnApsimDir_Click(object sender, EventArgs e)
        {
            entryApsimDir.Text = GetDirectory();
        }

        private void btnApsimZip_Click(object sender, EventArgs e)
        {
            entryApsimZip.Text = GetZipFile();
        }

        private void btnOutputDir_Click(object sender, EventArgs e)
        {
            entryOutputDir.Text = GetDirectory();
        }        

        private void chkSaveModels_Toggled(object sender, EventArgs e)
        {
            entryModelPath.IsEditable = chkSaveModels.Active;
            entryModelPath.Sensitive = chkSaveModels.Active;
            btnModelPath.Sensitive = chkSaveModels.Active;
        }

        private void btnModelPath_Click(object sender, EventArgs e)
        {
            entryModelPath.Text = GetDirectory();
        }

        public void SetDefaultJobName(string st)
        {
            entryName.Text = st;
        }
    }
}
