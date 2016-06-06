using System;
using Gtk;
using Glade;
using UserInterface.Interfaces;

namespace UserInterface.Views
{
    interface IInputView
    {
        /// <summary>
        /// Invoked when a browse button is clicked.
        /// </summary>
        event EventHandler<OpenDialogArgs> BrowseButtonClicked;

        /// <summary>
        /// Property to provide access to the filename label.
        /// </summary>
        string FileName { get; set; }

        /// <summary>
        /// Property to provide access to the warning text label.
        /// </summary>
        string WarningText { get; set; }
        
        /// <summary>
        /// Property to provide access to the grid.
        /// </summary>
        IGridView GridView { get; }
    }

    public class InputView : ViewBase, Views.IInputView
    {
        /// <summary>
        /// Invoked when a browse button is clicked.
        /// </summary>
        public event EventHandler<OpenDialogArgs> BrowseButtonClicked;

        [Widget]
        private VBox vbox1 = null;
        [Widget]
        private Button button1 = null;
        [Widget]
        private Label label1 = null;
        [Widget]
        private Label label2 = null;
        private GridView Grid;

        /// <summary>
        /// Constructor
        /// </summary>
        public InputView(ViewBase owner) : base(owner)
        {
            Glade.XML gxml = new Glade.XML("ApsimNG.Resources.Glade.InputView.glade", "vbox1");
            gxml.Autoconnect(this);
            _mainWidget = vbox1;

            Grid = new GridView(this);
            vbox1.PackStart(Grid.MainWidget, true, true, 0);
            button1.Clicked += OnBrowseButtonClick;
            label2.ModifyFg(StateType.Normal, new Gdk.Color(0xFF, 0x0, 0x0));
        }

        /// <summary>
        /// Property to provide access to the grid.
        /// </summary>
        public IGridView GridView { get { return Grid; } }

        public override void Destroy()
        {
            if (Grid != null)
            {
                Grid.Destroy();
                Grid = null;
            }
            base.Destroy();
        }

        /// <summary>
        /// Property to provide access to the filename label.
        /// </summary>
        public string FileName
        {
            get
            {
                // FileNameLabel.Text = Path.GetFullPath(FileNameLabel.Text);
                return label1.Text;
            }
            set
            {
                label1.Text = value;
            }
        }

        /// <summary>
        /// Property to provide access to the warning text label.
        /// </summary>
        public string WarningText
        {
            get
            {
                return label2.Text;
            }
            set
            {
                label2.Text = value;
                label2.Visible = !string.IsNullOrWhiteSpace(value);
            }
        }

        /// <summary>
        /// Browse button was clicked - send event to presenter.
        /// </summary>
        private void OnBrowseButtonClick(object sender, EventArgs e)
        {
            if (BrowseButtonClicked != null )
            {
                FileChooserDialog fileChooser = new FileChooserDialog("Select a file to open", null, FileChooserAction.Open, "Cancel", ResponseType.Cancel, "Select", ResponseType.Accept);
                FileFilter allFilter = new FileFilter();
                allFilter.AddPattern("*");
                allFilter.Name = "All files";
                fileChooser.AddFilter(allFilter);
                if (fileChooser.Run() == (int)ResponseType.Accept)
                {
                    OpenDialogArgs args = new OpenDialogArgs();
                    args.FileNames = fileChooser.Filenames;
                    BrowseButtonClicked.Invoke(this, args);
                }
                fileChooser.Destroy();
            }
        }
    }

    /// <summary>
    /// A class for holding info about a begin drag event.
    /// </summary>
    public class OpenDialogArgs : EventArgs
    {
        public string[] FileNames;
    }
}
