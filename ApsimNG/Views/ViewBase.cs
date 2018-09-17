using Gtk;
using System;
using UserInterface.Interfaces;
using Utility;

namespace UserInterface.Views
{
    public class ViewBase
    {
        /// <summary>
        /// A reference to the main view.
        /// </summary>
        public static IMainView MasterView = null;

        /// <summary>
        /// The parent view.
        /// </summary>
        protected ViewBase _owner = null;

        /// <summary>
        /// The main widget in this view.
        /// </summary>
        protected Widget _mainWidget = null;

        /// <summary>
        /// Displays an error message to the user.
        /// </summary>
        /// <param name="err"></param>
        protected void ShowError(Exception err)
        {
            MasterView.ShowError(err);
        }

        /// <summary>
        /// Asks the user for a file or directory. If you need more specialised behaviour 
        /// (e.g. select multiple files), you will need to instantiate and use an 
        /// implementation of <see cref="IFileDialog"/>.
        /// </summary>
        /// <param name="prompt">Prompt to be displayed in the title bar of the dialog.</param>
        /// <param name="actionType">Type of action the dialog should perform.</param>
        /// /// <param name="fileType">File types the user is allowed to choose.</param>
        /// <param name="initialDirectory">Initial directory. Defaults to the previously used directory.</param>
        /// <returns>Path to the chosen file or directory.</returns>
        protected static string AskUserForFileName(string prompt, FileDialog.FileActionType actionType, string fileType, string initialDirectory = "")
        {
            IFileDialog dialog = new FileDialog()
            {
                Prompt = prompt,
                Action = actionType,
                FileType = fileType,
                InitialDirectory = initialDirectory
            };
            return dialog.GetFile();
        }

        /// <summary>
        /// The parent view.
        /// </summary>
        public ViewBase Owner
        {
            get
            {
                return _owner;
            }
        }

        /// <summary>
        /// The main widget in this view.
        /// </summary>
        public Widget MainWidget
        {
            get
            {
                return _mainWidget;
            }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="owner">The parent view.</param>
        public ViewBase(ViewBase owner)
        {
            _owner = owner;
        }
    }
}