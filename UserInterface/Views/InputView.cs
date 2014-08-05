using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;
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
        /// Property to provide access to the grid.
        /// </summary>
        IGridView GridView { get; }
    }

    public partial class InputView : UserControl, Views.IInputView
    {
        /// <summary>
        /// Invoked when a browse button is clicked.
        /// </summary>
        public event EventHandler<OpenDialogArgs> BrowseButtonClicked;

        /// <summary>
        /// Constructor
        /// </summary>
        public InputView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Property to provide access to the grid.
        /// </summary>
        public IGridView GridView { get { return Grid; } }

        /// <summary>
        /// Property to provide access to the filename label.
        /// </summary>
        public string FileName
        {
            get
            {
            //TODO: This won't work on Linux or Mac
                if (!FileNameLabel.Text.Contains(':')) // no drive designator, so it's a relative path
                    FileNameLabel.Text = Utility.PathUtils.GetAbsolutePath(FileNameLabel.Text); //remove bin

                return FileNameLabel.Text;
            }
            set
            {
                string curdir = Utility.PathUtils.GetAbsolutePath(String.Empty);
                FileNameLabel.Text = value;
                FileNameLabel.Text = FileNameLabel.Text.Replace(curdir, String.Empty);
            }
        }

        /// <summary>
        /// Browse button was clicked - send event to presenter.
        /// </summary>
        private void OnBrowseButtonClick(object sender, EventArgs e)
        {
            if (BrowseButtonClicked != null && OpenFileDialog.ShowDialog() == DialogResult.OK)
            {
                OpenDialogArgs args = new OpenDialogArgs();
                args.FileNames = OpenFileDialog.FileNames;

                string curdir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
                        .Substring(0, Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location).Length - 3);
                for (int i=0; i< args.FileNames.Length; i++)
                {
                    args.FileNames[i] = args.FileNames[i].Replace(curdir, "");
                }

                BrowseButtonClicked.Invoke(this, args);
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
