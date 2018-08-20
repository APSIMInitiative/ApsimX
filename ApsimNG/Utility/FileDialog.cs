namespace Utility
{
    using UserInterface.Interfaces;
    using System;
    using System.IO;
    using System.Linq;
    using MonoMac;
    using Gtk;
    using APSIM.Shared.Utilities;
    using Models.Core;
    using MonoMac.AppKit;

    /// <summary>
    /// All access to this class should be via <see cref="IFileDialog"/>.
    /// </summary>
    public class FileDialog : IFileDialog
    {
        private string initialDirectory = Configuration.Settings.PreviousFolder;

        /// <summary>
        /// Defines the type of action which the dialog should perform.
        /// </summary>
        public enum FileActionType
        {
            /// <summary>
            /// Prompt the user to choose an existing file.
            /// </summary>
            Open,

            /// <summary>
            /// Prompt the user to create a new file.
            /// </summary>
            Save,

            /// <summary>
            /// Prompt the user to select a directory.
            /// </summary>
            SelectFolder
        }

        /// <summary>
        /// Defines the type of action which the dialog should perform.
        /// </summary>
        public FileActionType Action { get; set; }

        /// <summary>
        /// File types which the user is allowed to choose.
        /// </summary>
        public string FileType { get; set; }

        /// <summary>
        /// Initial directory used when the file dialog runs.
        /// Defaults to the Apsim-wide previously selected folder.
        /// </summary>
        public string InitialDirectory
        {
            get
            {
                return Directory.Exists(initialDirectory) ? initialDirectory : Configuration.Settings.PreviousFolder;
            }
            set
            {
                initialDirectory = value;
            }
        }

        /// <summary>
        /// Prompt displayed in the title bar of the dialog.
        /// Defaults to "Choose a file."
        /// </summary>
        public string Prompt { get; set; } = "Choose a file.";

        /// <summary>
        /// Runs the dialog.
        /// Returns the chosen file/directory.
        /// </summary>
        /// <returns>The chosen file.</returns>
        public string GetFile()
        {
            throw new NotImplementedException("TODO");
        }

        /// <summary>
        /// Runs the dialog.
        /// Returns an array of chosen files/directories.
        /// </summary>
        /// <returns>Array of chosen files.</returns>
        public string[] GetFiles()
        {
            throw new NotImplementedException("TODO");
        }

        /// <summary>
        /// Ask user for a filename to open on Windows.
        /// </summary>
        /// <param name="prompt">String to use as dialog heading.</param>
        /// <param name="fileSpec">The file specification used to filter the files.</param>
        /// <param name="Action">Action to perform (currently either "Open" or "Save").</param>
        /// <param name="InitialPath">Optional Initial starting filename or directory.</param>      
        private string[] WindowsFileDialog(bool selectMultiple)
        {
            System.Windows.Forms.FileDialog dialog = null;
            if ((Action & FileActionType.Open) == FileActionType.Open)
            {
                dialog = new System.Windows.Forms.OpenFileDialog();
                (dialog as System.Windows.Forms.OpenFileDialog).Multiselect = selectMultiple;
            }
            else if ((Action & FileActionType.Save) == FileActionType.Save)
                dialog = new System.Windows.Forms.SaveFileDialog();
            else if ((Action & FileActionType.SelectFolder) == FileActionType.SelectFolder)
                return WindowsDirectoryDialog();

            dialog.Title = Prompt;
            if (!String.IsNullOrEmpty(FileType))
                dialog.Filter = FileType + "|All files (*.*)|*.*";

            // This almost works, but Windows is buggy.
            // If the file name is long, it doesn't display in a sensible way. ¯\_(ツ)_/¯
            dialog.InitialDirectory = InitialDirectory;
            dialog.FileName = null;

            string[] fileNames = null;
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                fileNames = dialog.FileNames;
            dialog = null;
            return fileNames;
        }

        /// <summary>Ask user for a directory on Windows.</summary>
        /// <param name="Prompt">String to use as dialog heading</param>        
        /// <param name="initialPath">Optional Initial starting filename or directory</param>
        /// <returns>string containing the path to the chosen directory.</returns>
        private string[] WindowsDirectoryDialog()
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog()
            {
                Description = Prompt,
                SelectedPath = InitialDirectory
            };
            
            string fileName = null;
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                fileName = dialog.SelectedPath;
            dialog = null;
            return null;
        }

        /// <summary>
        /// Ask user for a filename to open on Windows.
        /// </summary>
        /// <param name="prompt">String to use as dialog heading</param>
        /// <param name="fileSpec">The file specification used to filter the files.</param>
        /// <param name="action">Action to perform (currently either "Open" or "Save")</param>
        /// <param name="initialPath">Optional Initial starting filename or directory</param>      
        private string OSXFileDialog(string prompt, string fileSpec, FileChooserAction action, string initialPath)
        {
            string fileName = null;
            int result = 0;
            NSSavePanel panel;
            if (action == FileChooserAction.Open)
                panel = new NSOpenPanel();
            else
                panel = new NSSavePanel();
            panel.Title = prompt;

            if (!String.IsNullOrEmpty(fileSpec))
            {
                string[] specParts = fileSpec.Split(new Char[] { '|' });
                int nExts = 0;
                string[] allowed = new string[specParts.Length / 2];
                for (int i = 0; i < specParts.Length; i += 2)
                {
                    string pattern = Path.GetExtension(specParts[i + 1]);
                    if (!String.IsNullOrEmpty(pattern))
                    {
                        pattern = pattern.Substring(1); // Get rid of leading "."
                        if (!String.IsNullOrEmpty(pattern))
                            allowed[nExts++] = pattern;
                    }
                }
                if (nExts > 0)
                {
                    Array.Resize(ref allowed, nExts);
                    panel.AllowedFileTypes = allowed;
                }
            }
            panel.AllowsOtherFileTypes = true;

            if (File.Exists(initialPath))
            {
                panel.DirectoryUrl = new MonoMac.Foundation.NSUrl(Path.GetDirectoryName(initialPath));
                panel.NameFieldStringValue = Path.GetFileName(initialPath);
            }
            else if (Directory.Exists(initialPath))
                panel.DirectoryUrl = new MonoMac.Foundation.NSUrl(initialPath);
            else
                panel.DirectoryUrl = new MonoMac.Foundation.NSUrl(Utility.Configuration.Settings.PreviousFolder);

            result = panel.RunModal();
            if (result == 1 /*NSFileHandlingPanelOKButton*/)
            {
                fileName = panel.Url.Path;
            }
            return fileName;
        }
    }
}
