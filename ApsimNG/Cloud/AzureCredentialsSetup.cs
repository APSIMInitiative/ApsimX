using System;
using UserInterface.Views;
using System.Linq;
using Gtk;
using Utility;
using UserInterface.Interfaces;

namespace ApsimNG.Cloud
{
    class AzureCredentialsSetup : Window
    {
        /// <summary>
        /// Input field for the batch account name.
        /// </summary>
        private Entry batchAccountInput;

        /// <summary>
        /// Input field for the batch URL.
        /// </summary>
        private Entry batchUrlInput;

        /// <summary>
        /// Input field for the batch account key.
        /// </summary>
        private Entry batchKeyInput;

        /// <summary>
        /// Input field for the storage account name.
        /// </summary>
        private Entry storageAccountInput;

        /// <summary>
        /// Input field for the storage account key.
        /// </summary>
        private Entry storageKeyInput;

        /// <summary>
        /// Input field for the email address to send the automatic message from.
        /// </summary>
        private Entry emailSenderInput;

        /// <summary>
        /// Password for the email account to send the auto message from.
        /// </summary>
        private Entry emailPWInput;

        /// <summary>
        /// Button to load the credentials data from a file.
        /// </summary>
        private Button btnLoad;

        /// <summary>
        /// Button to save the settings.
        /// </summary>
        private Button btnSave;

        /// <summary>
        /// Button to provide some information to first-time users.
        /// </summary>
        private Button btnHelp;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public AzureCredentialsSetup() : base("Azure Batch and Storage Account Information")
        {
            WidthRequest = 500;
            // initialise input fields with the last values used
            batchAccountInput = new Entry((string)AzureSettings.Default["BatchAccount"]);
            batchUrlInput = new Entry((string)AzureSettings.Default["BatchUrl"]);
            batchKeyInput = new Entry((string)AzureSettings.Default["BatchKey"]);
            storageAccountInput = new Entry((string)AzureSettings.Default["StorageAccount"]);
            storageKeyInput = new Entry((string)AzureSettings.Default["StorageKey"]);
            emailSenderInput = new Entry((string)AzureSettings.Default["EmailSender"]);
            emailPWInput = new Entry((string)AzureSettings.Default["EmailPW"]);

            btnLoad = new Button("Load from File");
            btnLoad.Clicked += LoadCredentialsFromFile;

            btnSave = new Button("Save");
            btnSave.Clicked += SaveCredentials;

            btnHelp = new Button("Help");
            btnHelp.Clicked += ProvideHelp;

            HBox buttonContainer = new HBox();
            buttonContainer.PackStart(btnLoad, false, false, 0);
            buttonContainer.PackStart(btnHelp, false, false, 0);
            buttonContainer.PackEnd(btnSave, false, false, 0);

            Table primaryContainer = new Table(10, 2, false);

            primaryContainer.Attach(new Label("Batch Account:") { Xalign = 0 }, 0, 1, 0, 1, AttachOptions.Fill, AttachOptions.Shrink, 0, 0);
            primaryContainer.Attach(batchAccountInput, 1, 2, 0, 1, (AttachOptions.Fill | AttachOptions.Expand), AttachOptions.Shrink, 0, 0);

            primaryContainer.Attach(new Label("Batch URL:") { Xalign = 0 }, 0, 1, 1, 2, AttachOptions.Fill, AttachOptions.Shrink, 0, 0);
            primaryContainer.Attach(batchUrlInput, 1, 2, 1, 2, (AttachOptions.Fill | AttachOptions.Expand), AttachOptions.Shrink, 0, 0);

            primaryContainer.Attach(new Label("Batch Key:") { Xalign = 0 }, 0, 1, 2, 3, AttachOptions.Fill, AttachOptions.Shrink, 0, 0);
            primaryContainer.Attach(batchKeyInput, 1, 2, 2, 3, (AttachOptions.Fill | AttachOptions.Expand), AttachOptions.Shrink, 0, 0);

            primaryContainer.Attach(new Label("Storage Account:") { Xalign = 0 }, 0, 1, 3, 4, AttachOptions.Fill, AttachOptions.Shrink, 0, 0);
            primaryContainer.Attach(storageAccountInput, 1, 2, 3, 4, (AttachOptions.Fill | AttachOptions.Expand), AttachOptions.Shrink, 0, 0);

            primaryContainer.Attach(new Label("Storage Key:") { Xalign = 0 }, 0, 1, 4, 5, AttachOptions.Fill, AttachOptions.Shrink, 0, 0);
            primaryContainer.Attach(storageKeyInput, 1, 2, 4, 5, (AttachOptions.Fill | AttachOptions.Expand), AttachOptions.Shrink, 0, 0);

            primaryContainer.Attach(new Label(""), 0, 2, 5, 6, (AttachOptions.Fill | AttachOptions.Expand), AttachOptions.Shrink, 0, 0);

            primaryContainer.Attach(new Label("Email Address:") { Xalign = 0 }, 0, 1, 6, 7, AttachOptions.Fill, AttachOptions.Shrink, 0, 0);
            primaryContainer.Attach(emailSenderInput, 1, 2, 6, 7, (AttachOptions.Fill | AttachOptions.Expand), AttachOptions.Shrink, 0, 0);

            primaryContainer.Attach(new Label("Email Password:") { Xalign = 0 }, 0, 1, 7, 8, AttachOptions.Fill, AttachOptions.Shrink, 0, 0);
            primaryContainer.Attach(emailPWInput, 1, 2, 7, 8, (AttachOptions.Fill | AttachOptions.Expand), AttachOptions.Shrink, 0, 0);

            primaryContainer.Attach(new Label(""), 0, 2, 8, 9, (AttachOptions.Fill | AttachOptions.Expand), AttachOptions.Shrink, 0, 0);

            primaryContainer.Attach(buttonContainer, 0, 2, 9, 10, (AttachOptions.Fill | AttachOptions.Expand), AttachOptions.Shrink, 0, 0);
                        
            Alignment adj = new Alignment(0f, 0f, 1f, 0f); // 3rd argument is 1 to make the controls to scale (horizontally) with viewport size
            adj.LeftPadding = adj.RightPadding = adj.TopPadding = adj.BottomPadding = 15;
            adj.Add(primaryContainer);
            Add(adj);
            adj.ShowAll();
            Show();
        }
        
        /// <summary>
        /// Event handler for when the user has finished entering credentials (when they press save).
        /// </summary>
        public EventHandler Finished { get; set; }

        /// <summary>
        /// Checks if Azure credentials exist in AzureSettings.Default. This method does not check their validity.
        /// It also does not check to see if the path to the Azure licence file exists there.
        /// </summary>
        /// <returns>True if credentials exist, false otherwise.</returns>
        public static bool CredentialsExist()
        {
            string[] credentials = new string[] { "BatchAccount", "BatchUrl", "BatchKey", "StorageAccount", "StorageKey", "EmailSender", "EmailPW" };
            // not very scalable, I know. the better solution would be to split the properties into 2 files: credentials, and misc settings. then just iterate over the following:
            //List<string> properties = AzureSettings.Default.Properties.Cast<System.Configuration.SettingsProperty>().Select(p => p.Name).ToList();

            // Could turn this method into a two-liner (including the above line initialising credentials):
            // return credentials.All(c => !string.IsNullOrEmpty((string)AzureSettings.Default[c]));
            // But I don't have the means to test this right now - DH 8/18.
            foreach (string key in credentials)
            {
                string value = (string)AzureSettings.Default[key];
                if (string.IsNullOrEmpty(value))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Opens a file chooser dialog for the user to select a licence (.lic) file. 
        /// Populates the input fields with the appropriate values from this file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoadCredentialsFromFile(object sender, EventArgs e)
        {
            IFileDialog fileChooser = new FileDialog()
            {
                Prompt = "Select a licence file",
                FileType = "Azure Licence file (*.lic) | *.lic",
                Action = FileDialog.FileActionType.Open,
                InitialDirectory = (string)AzureSettings.Default["AzureLicenceFilePath"]
            };
            string credentialsFile = fileChooser.GetFile();
            if (!string.IsNullOrEmpty(credentialsFile))
            {
                AzureSettings.Default["AzureLicenceFilePath"] = credentialsFile;
                AzureSettings.Default.Save();
                string line = "";
                System.IO.StreamReader file = new System.IO.StreamReader(credentialsFile);
                while ((line= file.ReadLine()) != null)
                {
                    if (line.IndexOf('=') > -1)
                    {
                        // Not a very scalable solution - see comment in this.CredentialsExist()
                        string key = line.Substring(0, line.IndexOf('='));
                        string value = line.Substring(line.IndexOf('=') + 1);
                        switch (key)
                        {
                            case "StorageAccount":
                                storageAccountInput.Text = value;
                                break;
                            case "StorageKey":
                                storageKeyInput.Text = value;
                                break;
                            case "BatchUrl":
                                batchUrlInput.Text = value;
                                break;
                            case "BatchAccount":
                                batchAccountInput.Text = value;
                                break;
                            case "BatchKey":
                                batchKeyInput.Text = value;
                                break;
                            case "GmailAccount":
                                emailSenderInput.Text = value;
                                break;
                            case "GmailPassword":
                                emailPWInput.Text = value;
                                break;
                            default:
                                 // TODO : show error message, because if flow reaches here, the file is not a valid Azure Licence file
                                break;                                
                        }
                    }
                }
                file.Close();
            }
        }

        /// <summary>
        /// Saves the values stored in the input fields to ApsimNG.Properties.Settings.Default, then
        /// closes this window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveCredentials(object sender, EventArgs e)
        {
            AzureSettings.Default["StorageAccount"] = storageAccountInput.Text;
            AzureSettings.Default["StorageKey"] = storageKeyInput.Text;
            AzureSettings.Default["BatchUrl"] = batchUrlInput.Text;
            AzureSettings.Default["BatchAccount"] = batchAccountInput.Text;
            AzureSettings.Default["BatchKey"] = batchKeyInput.Text;
            AzureSettings.Default["EmailSender"] = emailSenderInput.Text;
            AzureSettings.Default["EmailPW"] = emailPWInput.Text;
            AzureSettings.Default.Save();
            Finished?.Invoke(this, e);
            this.Destroy();
        }

        /// <summary>
        /// Provides help for the user by opening the ApsimX documentation on getting started with Azure.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProvideHelp(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://apsimnextgeneration.netlify.com/usage/cloud/azure/gettingstarted/");            
        }
    }
}
