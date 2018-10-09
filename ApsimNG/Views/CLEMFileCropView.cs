using System;
using Gtk;
using UserInterface.Interfaces;

namespace UserInterface.Views
{
    //duplicate of InputView because we want to place this at the top of our simulation not onto the Datastore
    interface ICLEMFileCropView
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

    public class CLEMFileCropView : ViewBase, Views.ICLEMFileCropView
    {
        /// <summary>
        /// Invoked when a browse button is clicked.
        /// </summary>
        public event EventHandler<OpenDialogArgs> BrowseButtonClicked;

        private VBox vbox1 = null;
        private Button button1 = null;
        private Label label1 = null;
        private Label label2 = null;
        private GridView Grid;

        /// <summary>
        /// Property to provide access to the grid.
        /// </summary>
        public IGridView GridView { get { return Grid; } }

        /// <summary>
        /// Constructor
        /// </summary>
        public CLEMFileCropView(ViewBase owner) : base(owner)
        {
            Builder builder = MasterView.BuilderFromResource("ApsimNG.Resources.Glade.InputView.glade");
            vbox1 = (VBox)builder.GetObject("vbox1");
            button1 = (Button)builder.GetObject("button1");
            label1 = (Label)builder.GetObject("label1");
            label2 = (Label)builder.GetObject("label2");
            _mainWidget = vbox1;

            Grid = new GridView(this);
            vbox1.PackStart(Grid.MainWidget, true, true, 0);
            button1.Clicked += OnBrowseButtonClick;
            label2.ModifyFg(StateType.Normal, new Gdk.Color(0xFF, 0x0, 0x0));
            _mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        private void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            button1.Clicked -= OnBrowseButtonClick;
            Grid.MainWidget.Destroy();
            Grid = null;
            _mainWidget.Destroyed -= _mainWidget_Destroyed;
            _owner = null;
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
            if (BrowseButtonClicked != null)
            {
                string fileName = AskUserForFileName("Select a file to open", Utility.FileDialog.FileActionType.Open, "*.*");
                if (!String.IsNullOrEmpty(fileName))
                {
                    OpenDialogArgs args = new OpenDialogArgs();
                    args.FileName = fileName;
                    BrowseButtonClicked.Invoke(this, args);
                }
            }
        }
    }

    //Because this is a duplicate of InputView we don't need to declare what is below again.

    ///// <summary>
    ///// A class for holding info about a begin drag event.
    ///// </summary>
    //public class OpenDialogArgs : EventArgs
    //{
    //    public string FileName;
    //}
}
