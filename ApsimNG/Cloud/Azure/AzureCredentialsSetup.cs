using System;
using UserInterface.Views;
using System.Linq;
using Gtk;
using Utility;
using UserInterface.Interfaces;
using System.IO;
using UserInterface.Extensions;

namespace ApsimNG.Cloud.Azure
{
    /// <summary>
    /// A popup window which prompts the user to specify an azure licence file
    /// containing API keys/credentials required to run simulations on Azure.
    /// </summary>
    /// <remarks>
    /// Should this be in the Views directory?
    /// </remarks>
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
            batchAccountInput = new Entry();
            batchUrlInput = new Entry();
            batchKeyInput = new Entry();
            storageAccountInput = new Entry();
            storageKeyInput = new Entry();
            emailSenderInput = new Entry();
            emailPWInput = new Entry();

            try
            {
                PopulateInputs();
            }
            catch
            {
            }

            btnLoad = new Button("Load from File");
            btnLoad.Clicked += LoadCredentialsFromFile;

            btnSave = new Button("Save");
            btnSave.Clicked += SaveCredentials;

            btnHelp = new Button("Help");
            btnHelp.Clicked += ProvideHelp;

            Box buttonContainer = new Box(Orientation.Horizontal, 0);
            buttonContainer.PackStart(btnLoad, false, false, 0);
            buttonContainer.PackStart(btnHelp, false, false, 0);
            buttonContainer.PackEnd(btnSave, false, false, 0);


            Grid primaryContainer = new Grid();

            primaryContainer.Attach(new Label("Batch Account:") { Xalign = 0 }, 0, 0, 1, 1);
            primaryContainer.Attach(batchAccountInput, 1, 0, 1, 1);

            primaryContainer.Attach(new Label("Batch URL:") { Xalign = 0 }, 0, 1, 1, 1);
            primaryContainer.Attach(batchUrlInput, 1, 1, 1, 1);

            primaryContainer.Attach(new Label("Batch Key:") { Xalign = 0 }, 0, 2, 1, 1);
            primaryContainer.Attach(batchKeyInput, 1, 2, 1, 1);

            primaryContainer.Attach(new Label("Storage Account:") { Xalign = 0 }, 0, 3, 1, 1);
            primaryContainer.Attach(storageAccountInput, 1, 3, 1, 1);

            primaryContainer.Attach(new Label("Storage Key:") { Xalign = 0 }, 0, 4, 1, 1);
            primaryContainer.Attach(storageKeyInput, 1, 4, 1, 1);

            primaryContainer.Attach(new Label(""), 0, 5, 2, 1);

            primaryContainer.Attach(new Label("Email Address:") { Xalign = 0 }, 0, 6, 1, 1);
            primaryContainer.Attach(emailSenderInput, 1, 6, 1, 1);

            primaryContainer.Attach(new Label("Email Password:") { Xalign = 0 }, 0, 7, 1, 1);
            primaryContainer.Attach(emailPWInput, 1, 7, 1, 1);

            primaryContainer.Attach(new Label(""), 0, 8, 2, 1);

            primaryContainer.Attach(buttonContainer, 0, 9, 2, 1);

            primaryContainer.Margin = 15;
            Add(primaryContainer);

            WindowPosition = WindowPosition.Center;

            ShowAll();
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
            if (!File.Exists(AzureSettings.Default.LicenceFilePath))
                return false;

            try
            {
                Licence licence = new Licence(AzureSettings.Default.LicenceFilePath);
                if (string.IsNullOrEmpty(licence.BatchAccount))
                    return false;
                if (string.IsNullOrEmpty(licence.BatchUrl))
                    return false;
                if (string.IsNullOrEmpty(licence.BatchKey))
                    return false;
                if (string.IsNullOrEmpty(licence.StorageAccount))
                    return false;
                if (string.IsNullOrEmpty(licence.StorageKey))
                    return false;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void GetCredentialsIfNotExist(System.Action runAfter)
        {
            if (!CredentialsExist())
            {
                var popup = new AzureCredentialsSetup();
                popup.Finished += (_, __) => runAfter();
            }
            else
                runAfter();
        }

        /// <summary>
        /// Populate the text inputs with the values from the licence file.
        /// </summary>
        private void PopulateInputs()
        {
            if (File.Exists(AzureSettings.Default.LicenceFilePath))
            {
                Licence licence = new Licence(AzureSettings.Default.LicenceFilePath);

                batchAccountInput.Text = licence.BatchAccount;
                batchUrlInput.Text = licence.BatchUrl;
                batchKeyInput.Text = licence.BatchKey;
                storageAccountInput.Text = licence.StorageAccount;
                storageKeyInput.Text = licence.StorageKey;
                emailSenderInput.Text = licence.EmailSender;
                emailPWInput.Text = licence.EmailPW;
            }
        }

        /// <summary>
        /// Opens a file chooser dialog for the user to select a licence (.lic) file. 
        /// Populates the input fields with the appropriate values from this file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LoadCredentialsFromFile(object sender, EventArgs e)
        {
            try
            {
                IFileDialog fileChooser = new FileDialog()
                {
                    Prompt = "Select a licence file",
                    FileType = "Azure Licence file (*.lic) | *.lic | All Files (*.*) | *.*",
                    Action = FileDialog.FileActionType.Open,
                };

                AzureSettings.Default.LicenceFilePath = fileChooser.GetFile();
                AzureSettings.Default.Save();

                PopulateInputs();
            }
            catch// (Exception err) // fixme
            {

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
            try
            {
                Finished?.Invoke(this, e);
                Dispose();
            }
            catch// (Exception err) // fixme
            {

            }
        }

        /// <summary>
        /// Provides help for the user by opening the ApsimX documentation on getting started with Azure.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProvideHelp(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("http://apsimnextgeneration.netlify.app/usage/cloud/azure/gettingstarted/") { UseShellExecute = true });
            }
            catch// (Exception err) // fixme
            {

            }
        }
    }
}
