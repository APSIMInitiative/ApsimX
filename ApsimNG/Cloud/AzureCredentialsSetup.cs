using System;
using Gtk;

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

        public EventHandler Finished { get; set; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public AzureCredentialsSetup() : base("Azure Batch and Storage Account Information")
        {
            WidthRequest = 500;
            // initialise input fields with the last values used
            batchAccountInput = new Entry((string)Properties.Settings.Default["BatchAccount"]);
            batchUrlInput = new Entry((string)Properties.Settings.Default["BatchUrl"]);
            batchKeyInput = new Entry((string)Properties.Settings.Default["BatchKey"]);
            storageAccountInput = new Entry((string)Properties.Settings.Default["StorageAccount"]);
            storageKeyInput = new Entry((string)Properties.Settings.Default["StorageKey"]);

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

            Table primaryContainer = new Table(7, 2, false);

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

            primaryContainer.Attach(buttonContainer, 0, 2, 6, 7, (AttachOptions.Fill | AttachOptions.Expand), AttachOptions.Shrink, 0, 0);
                        
            Alignment adj = new Alignment(0f, 0f, 1f, 0f); // 3rd argument is 1 to make the controls to scale (horizontally) with viewport size
            adj.LeftPadding = adj.RightPadding = adj.TopPadding = adj.BottomPadding = 15;
            adj.Add(primaryContainer);
            Add(adj);
            adj.ShowAll();
            Show();
        }

        /// <summary>
        /// Opens a file chooser dialog for the user to select a licence (.lic) file. 
        /// Populates the input fields with the appropriate values from this file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoadCredentialsFromFile(object sender, EventArgs e)
        {
            string path = UserInterface.ViewBase.AskUserForFileName("Select a licence file", "Azure Licence file (*.lic) | *.lic", FileChooserAction.Open);            
            if (path != "" && path != null)
            {
                Properties.Settings.Default["AzureLicenceFilepath"] = path;
                Properties.Settings.Default.Save();
                string line = "";
                System.IO.StreamReader file = new System.IO.StreamReader(path);
                while ((line= file.ReadLine()) != null)
                {
                    if (line.IndexOf('=') > -1)
                    {
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
                            case "BatchAccounts":
                                batchAccountInput.Text = value;
                                break;
                            case "BatchKey":
                                batchKeyInput.Text = value;
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
            Properties.Settings.Default["StorageAccount"] = storageAccountInput.Text;
            Properties.Settings.Default["StorageKey"] = storageKeyInput.Text;
            Properties.Settings.Default["BatchUrl"] = batchUrlInput.Text;
            Properties.Settings.Default["BatchAccount"] = batchAccountInput.Text;
            Properties.Settings.Default["BatchKey"] = batchKeyInput.Text;            
            Properties.Settings.Default.Save();
            Finished?.Invoke(this, e);
            this.Destroy();
        }

        private void ProvideHelp(object sender, EventArgs e)
        {
            // System.Diagnostics.Process.Start("http://apsimnextgeneration.netlify.com/usage/cloud/azure/gettingstarted/"); // this page will not exist until this branch is merged
            System.Diagnostics.Process.Start("https://azure.microsoft.com/free/");
            System.Diagnostics.Process.Start("https://docs.microsoft.com/en-us/azure/batch/batch-account-create-portal");
            System.Diagnostics.Process.Start("https://docs.microsoft.com/en-us/azure/storage/common/storage-create-storage-account");
        }
    }
}
