using System;
using Gtk;
using UserInterface.Extensions;
using UserInterface.Interfaces;

namespace UserInterface.Views
{
    interface IInputView
    {
        /// <summary>
        /// Invoked when a browse button is clicked.
        /// </summary>
        event EventHandler BrowseButtonClicked;

        /// <summary>
        /// Property to provide access to the filename label.
        /// </summary>
        string FileName { get; set; }

        /// <summary>
        /// Property to provide access to the grid.
        /// </summary>
        IGridView GridView { get; }
    }

    public class InputView : ViewBase, IInputView
    {
        /// <summary>
        /// Invoked when a browse button is clicked.
        /// </summary>
        public event EventHandler BrowseButtonClicked;

        private VBox vbox1 = null;
        private Button button1 = null;
        private Label label1 = null;
        private GridView grid;

        /// <summary>
        /// Property to provide access to the grid.
        /// </summary>
        public IGridView GridView { get { return grid; } }

        /// <summary>
        /// Constructor
        /// </summary>
        public InputView(ViewBase owner) : base(owner)
        {
            Builder builder = BuilderFromResource("ApsimNG.Resources.Glade.InputView.glade");
            vbox1 = (VBox)builder.GetObject("vbox1");
            button1 = (Button)builder.GetObject("button1");
            label1 = (Label)builder.GetObject("label1");
            mainWidget = vbox1;

            grid = new GridView(this);
            vbox1.PackStart(grid.MainWidget, true, true, 0);
            button1.Clicked += OnBrowseButtonClick;
            mainWidget.Destroyed += _mainWidget_Destroyed;
        }

        private void _mainWidget_Destroyed(object sender, EventArgs e)
        {
            try
            {
                button1.Clicked -= OnBrowseButtonClick;
                grid.MainWidget.Cleanup();
                grid = null;
                mainWidget.Destroyed -= _mainWidget_Destroyed;
                owner = null;
            }
            catch (Exception err)
            {
                ShowError(err);
            }
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
        /// Browse button was clicked - send event to presenter.
        /// </summary>
        private void OnBrowseButtonClick(object sender, EventArgs e)
        {
            try
            {
                if (BrowseButtonClicked != null)
                {
                    BrowseButtonClicked.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception err)
            {
                ShowError(err);
            }
        }
    }

    /// <summary>
    /// A class for holding info about a begin drag event.
    /// </summary>
    public class OpenDialogArgs : EventArgs
    {
        public string FileName;
    }
}
