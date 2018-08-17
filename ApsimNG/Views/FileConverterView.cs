namespace UserInterface.Views
{
    using System;
    using Gtk;
    using Interfaces;

    class FileConverterView : ViewBase, IFileConverterView
    {
        /// <summary>
        /// Label which goes next to the checkbutton.
        /// Clicking on this label toggles the checkbutton.
        /// </summary>
        private Label autoVersionLabel;

        /// <summary>
        /// Check box which sets the version number to the latest version.
        /// </summary>
        private CheckButton latestVersion;

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
        /// Constructor. Initialises the components of the view, but does not show anything onscreen.
        /// </summary>
        /// <param name="owner">Owner view.</param>
        public FileConverterView(ViewBase owner) : base(owner)
        {
            pathInput = new Entry();
            Label pathLabel = new Label("File to convert: ");
            chooseFileButton = new Button("...");
            chooseFileButton.Clicked += OnChooseFile;
            HBox pathContainer = new HBox();
            pathContainer.PackStart(pathLabel, false, true, 0);
            pathContainer.PackStart(pathInput, false, true, 0);
            pathContainer.PackEnd(chooseFileButton, false, false, 0);

            latestVersion = new CheckButton();
            latestVersion.Toggled += OnAutoVersionToggle;
            autoVersionLabel = new Label("Upgrade to latest version: ");
            autoVersionLabel.Events = Gdk.EventMask.ButtonPressMask;
            autoVersionLabel.ButtonPressEvent += OnAutoVersionLabelClick;
            HBox autoVersionContainer = new HBox();
            autoVersionContainer.PackStart(autoVersionLabel, false, true, 0);
            autoVersionContainer.PackEnd(latestVersion, false, true, 0);
            
            versionInput = new Entry();
            Label versionLabel = new Label("Upgrade to version: ");
            HBox versionContainer = new HBox();
            versionContainer.PackStart(versionLabel, false, true, 0);
            versionContainer.PackEnd(versionInput, false, true, 0);

            // This will grey out the version input text box.
            latestVersion.Active = true;

            convertButton = new Button("Go");
            convertButton.Clicked += OnConvert;

            VBox controlsContainer = new VBox();
            controlsContainer.PackStart(pathContainer, false, true, 5);
            controlsContainer.PackStart(autoVersionContainer, false, true, 5);
            controlsContainer.PackStart(versionContainer, false, true, 5);
            controlsContainer.PackEnd(convertButton, false, false, 5);

            mainWindow = new Window("File Converter");
            mainWindow.TransientFor = owner.MainWidget.Toplevel as Window;
            mainWindow.Add(controlsContainer);
            mainWindow.DeleteEvent += OnDelete;
            mainWindow.Destroyed += OnClose;
            mainWindow.KeyPressEvent += OnKeyPress;

            _mainWidget = mainWindow;
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
                return int.Parse(versionInput.Text);
            }
            set
            {
                versionInput.Text = value.ToString();
            }
        }

        /// <summary>
        /// Path to the file.
        /// </summary>
        public string FilePath
        {
            get
            {
                return pathInput.Text;
            }
            set
            {
                pathInput.Text = value;
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
        /// Invoked when the user clicks on the auto version label.
        /// Toggles the auto version checkbox.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
        [GLib.ConnectBefore]
        private void OnAutoVersionLabelClick(object sender, EventArgs args)
        {
            LatestVersion = !LatestVersion;
        }

        [GLib.ConnectBefore]
        private void OnAutoVersionToggle(object sender, EventArgs args)
        {
            // We will need to access the active property of latestVersion.
            // In order to get an accurate value, we first need to let the
            // Gtk event loop do its thing.
            while (GLib.MainContext.Iteration()) ;

            versionInput.Sensitive = !LatestVersion;
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
            string fileName = MasterView.AskUserForOpenFileName("*.xml|*.xml", Utility.Configuration.Settings.PreviousFolder);
            if (!string.IsNullOrEmpty(fileName))
                FilePath = fileName;
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
            Convert?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Invoked when the user closes the window.
        /// This prevents the window from closing, but still hides
        /// the window. This means we don't have to re-initialise
        /// the window each time the user opens it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        [GLib.ConnectBefore]
        private void OnDelete(object sender, DeleteEventArgs args)
        {
            Visible = false;
            args.RetVal = true;
        }

        /// <summary>
        /// Invoked when the window is closed for good, when Apsim closes.
        /// </summary>
        /// <param name="sender">Event arguments.</param>
        /// <param name="args">Sender object.</param>
        [GLib.ConnectBefore]
        private void OnClose(object sender, EventArgs args)
        {
            mainWindow.DeleteEvent -= OnDelete;
            mainWindow.Destroyed -= OnClose;
            chooseFileButton.Clicked -= OnChooseFile;
            convertButton.Clicked -= OnConvert;
            latestVersion.Toggled -= OnAutoVersionToggle;
            autoVersionLabel.ButtonPressEvent -= OnAutoVersionLabelClick;
            mainWindow.Dispose();
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
            if (args.Event.Key == Gdk.Key.Escape)
                Visible = false;
        }
    }
}
