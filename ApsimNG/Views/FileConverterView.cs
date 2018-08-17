namespace UserInterface.Views
{
    using System;
    using Gtk;
    using Interfaces;

    class FileConverterView : ViewBase, IFileConverterView
    {
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
            mainWindow = new Window("File Converter");
            mainWindow.TransientFor = owner.MainWidget.Toplevel as Window;

            pathInput = new Entry();
            Label pathLabel = new Label("File to conver: ");
            chooseFileButton = new Button("...");
            chooseFileButton.ButtonPressEvent += OnChooseFile;
            HBox pathContainer = new HBox();
            pathContainer.PackStart(pathLabel, false, true, 0);
            pathContainer.PackStart(pathInput, false, true, 0);
            pathContainer.PackEnd(chooseFileButton, false, false, 0);

            versionInput = new Entry();
            Label versionLabel = new Label("Upgrade to version: ");
            HBox versionContainer = new HBox();
            versionContainer.PackStart(versionLabel, false, true, 0);
            versionContainer.PackEnd(versionInput, false, true, 0);

            convertButton = new Button("Go");
            convertButton.ButtonPressEvent += OnConvert;

            VBox controlsContainer = new VBox();
            controlsContainer.PackStart(pathContainer, false, true, 5);
            controlsContainer.PackStart(versionContainer, false, true, 5);
            controlsContainer.PackEnd(convertButton, false, false, 5);
            mainWindow.Add(controlsContainer);

            _mainWidget = mainWindow;
        }

        /// <summary>
        /// Invoked when the user hits clicks convert button.
        /// </summary>
        public event EventHandler Convert;

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
        /// Invoked when the user presses the file chooser button.
        /// Opens a file chooser dialog and sets the contents of 
        /// the file path textbox to the chosen file.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="args">Event arguments.</param>
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
        private void OnConvert(object sender, ButtonPressEventArgs args)
        {
            Convert?.Invoke(this, EventArgs.Empty);
        }
    }
}
