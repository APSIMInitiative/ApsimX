namespace UserInterface.Views
{
    using System;
    using Gtk;
    using Interfaces;
    using Utility;
    using System.Linq;
    using APSIM.Shared.Utilities;
    using System.Collections.Specialized;

    class FileConverterView : ViewBase, IFileConverterView
    {
        /// <summary>
        /// Radio button which sets the version number to the latest version.
        /// </summary>
        private RadioButton latestVersion;

        /// <summary>
        /// Radio button which allows the user to upgrade to a specific version.
        /// </summary>
        private RadioButton specificVersion;

        /// <summary>
        /// Text box into which the user types the desired file version.
        /// </summary>
        private Entry versionInput;

        /// <summary>
        /// Text box into which the user types the file path.
        /// Can also be filled in via a file chooser dialog.
        /// </summary>
        private Entry pathInput;

        /// <summary>
        /// Button used to choose a file via a file chooser dialog.
        /// </summary>
        private Button chooseFileButton;

        /// <summary>
        /// Button used to initate the file conversion.
        /// </summary>
        private Button convertButton;

        /// <summary>
        /// The main window which holds all of the controls used in the view.
        /// </summary>
        private Window mainWindow;

        /// <summary>
        /// List of files to be converted.
        /// </summary>
        private string[] fileList;

        /// <summary>
        /// Constructor. Initialises the components of the view, but does not show anything onscreen.
        /// </summary>
        /// <param name="owner">Owner view.</param>
        public FileConverterView(ViewBase owner) : base(owner)
        {
            pathInput = new Entry();
            // pathInput.Changed += OnFilesChanged;
            pathInput.KeyPressEvent += OnFilesChanged;
            Label pathLabel = new Label("File to convert: ");
            chooseFileButton = new Button("...");
            chooseFileButton.Clicked += OnChooseFile;
            HBox pathContainer = new HBox();
            pathContainer.PackStart(pathLabel, false, true, 0);
            pathContainer.PackStart(pathInput, true, true, 0);
            pathContainer.PackEnd(chooseFileButton, false, false, 0);

            latestVersion = new RadioButton("Latest version");
            latestVersion.Toggled += OnUseLatestVersion;

            specificVersion = new RadioButton(latestVersion, "Specific version: ");
            specificVersion.Toggled += OnUseLatestVersion;
            versionInput = new Entry();
            HBox versionContainer = new HBox();
            versionContainer.PackStart(specificVersion, false, true, 0);
            versionContainer.PackEnd(versionInput, true, true, 0);

            // This will grey out the version input text box.
            specificVersion.Active = true;
            latestVersion.Active = true;

            convertButton = new Button("Go");
            convertButton.Clicked += OnConvert;

            VBox controlsContainer = new VBox();
            controlsContainer.PackStart(pathContainer, false, true, 5);
            controlsContainer.PackStart(latestVersion, false, true, 5);
            controlsContainer.PackStart(versionContainer, false, true, 5);
            controlsContainer.PackEnd(convertButton, false, false, 5);

            mainWindow = new Window("File Converter");
            mainWindow.TransientFor = owner.MainWidget.Toplevel as Window;
            mainWindow.WindowPosition = WindowPosition.Center;
            mainWindow.Add(controlsContainer);
            mainWindow.DeleteEvent += OnDelete;
            mainWindow.Destroyed += OnClose;
            mainWindow.KeyPressEvent += OnKeyPress;

            mainWidget = mainWindow;
        }

        /// <summary>
        /// Invoked when the user hits clicks convert button.
        /// </summary>
        public event EventHandler Convert;

        /// <summary>
        /// If true, we automatically upgrade to the latest version.
        /// </summary>
        public bool LatestVersion
        {
            get
            {
                return latestVersion.Active;
            }
            set
            {
                latestVersion.Active = value;
                latestVersion.Sensitive = !value;
            }
        }

        /// <summary>
        /// Version to which the user wants to upgrade the file.
        /// </summary>
        public int ToVersion
        {
            get
            {
                try
                {
                    return int.Parse(versionInput.Text);
                }
                catch (FormatException)
                {
                    throw new FormatException("Version number was not in the correct format. This should be an integer.");
                }
            }
            set
            {
                versionInput.Text = value.ToString();
            }
        }

        /// <summary>
        /// Path to the file.
        /// </summary>
        public string[] Files
        {
            get
            {
                return fileList;
            }
            set
            {
                fileList = value;
                pathInput.Text = value.Select(v => "\"" + v + "\"").Aggregate((a, b) => string.Format("{0} {1}", a, b));
            }
        }

        /// <summary>
        /// Controls the visibility of the view.
        /// Settings this to true displays the view.
        /// </summary>
        public bool Visible
        {
            get
            {
                return mainWindow.Visible;
            }
            set
            {
                if (value)
                    mainWindow.ShowAll();
                else
                    mainWindow.HideAll();
            }
        }

        /// <summary>
        /// Does some cleanup when the object is no longer needed.
        /// </summary>
        public void Destroy()
        {
            mainWindow.Destroy();
        }

        /// <summary>
        /// Invoked when the 'latest version' checkbox is toggled.
        /// When this occurs, we need to toggle the 'sensitive'
        /// property of the version input textbox to grey it out
        /// if it's not going to be used.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        [GLib.ConnectBefore]
        private void OnUseLatestVersion(object sender, EventArgs args)
        {
            try
            {
                versionInput.Sensitive = specificVersion.Active;
                versionInput.IsEditable = specificVersion.Active;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Invoked when the user presses the file chooser button.
        /// Opens a file chooser dialog and sets the contents of 
        /// the file path textbox to the chosen file.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        [GLib.ConnectBefore]
        private void OnChooseFile(object sender, EventArgs args)
        {
            try
            {
                //string fileName = MasterView.AskUserForOpenFileName("*.xml|*.xml", Utility.Configuration.Settings.PreviousFolder, false);
                IFileDialog dialog = new FileDialog();
                dialog.Action = FileDialog.FileActionType.Open;
                dialog.FileType = "JSON Files (*.json) | *.json|XML Files (*.xml) | *.xml";
                dialog.Prompt = "Choose files";
                string[] files = dialog.GetFiles();
                if (files != null && files.Any(f => !string.IsNullOrEmpty(f)))
                    Files = files;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Invoked when the user presses the go button.
        /// Initiates the file conversion process.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        [GLib.ConnectBefore]
        private void OnConvert(object sender, EventArgs args)
        {
            try
            {
                Convert?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Invoked when the user closes the window.
        /// This prevents the window from closing, but still hides
        /// the window. This means we don't have to re-initialise
        /// the window each time the user opens it.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        [GLib.ConnectBefore]
        private void OnDelete(object sender, DeleteEventArgs args)
        {
            try
            {
                Visible = false;
                args.RetVal = true;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Invoked when the window is closed for good, when Apsim closes.
        /// </summary>
        /// <param name="sender">Event arguments.</param>
        /// <param name="args">Sender object.</param>
        [GLib.ConnectBefore]
        private void OnClose(object sender, EventArgs args)
        {
            try
            {
                mainWindow.DeleteEvent -= OnDelete;
                mainWindow.Destroyed -= OnClose;
                mainWindow.KeyPressEvent -= OnKeyPress;
                chooseFileButton.Clicked -= OnChooseFile;
                convertButton.Clicked -= OnConvert;
                latestVersion.Toggled -= OnUseLatestVersion;
                specificVersion.Toggled -= OnUseLatestVersion;
                pathInput.Changed -= OnFilesChanged;
                mainWindow.Dispose();
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Invoked when a keyboard key is pressed.
        /// Closes the window if the pressed key was escape.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        [GLib.ConnectBefore]
        private void OnKeyPress(object sender, KeyPressEventArgs args)
        {
            try
            {
                if (args.Event.Key == Gdk.Key.Escape)
                    Visible = false;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }

        /// <summary>
        /// Invoked when the user changes the contents of the file paths textbox.
        /// Updates the array of files.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        private void OnFilesChanged(object sender, EventArgs args)
        {
            try
            {
                StringCollection paths = StringUtilities.SplitStringHonouringQuotes(pathInput.Text, " ");
                Files = new string[paths.Count];
                for (int i = 0; i < paths.Count; i++)
                    Files[i] = paths[i];   
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }
    }
}
