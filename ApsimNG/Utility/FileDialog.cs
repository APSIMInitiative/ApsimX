namespace Utility
{
    using UserInterface.Interfaces;
    using System;
    using System.IO;
    using System.Linq;
    using Gtk;
    using APSIM.Shared.Utilities;
    using Models.Core;
    using UserInterface.Extensions;
    using UserInterface.Views;
#if NETFRAMEWORK
    using MonoMac;
    using MonoMac.AppKit;
#endif

    /// <summary>
    /// All access to this class should be via <see cref="IFileDialog"/>.
    /// </summary>
    public class FileDialog : IFileDialog
    {
        /// <summary>
        /// Initial directory used when the file dialog runs.
        /// Defaults to the Apsim-wide previously selected folder.
        /// </summary>
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
        /// This doesn't need to be set if you intend to use directory chooser mode.
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
                return Directory.Exists(initialDirectory) ? initialDirectory : File.Exists(initialDirectory) ? Path.GetDirectoryName(initialDirectory) : Configuration.Settings.PreviousFolder;
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
            string[] files = GetFiles(false);
            if (files == null || !files.Any())
                return null;
            else
                return files.FirstOrDefault();
        }

        /// <summary>
        /// Runs the dialog.
        /// Returns an array of chosen files/directories.
        /// </summary>
        /// <returns>Array of chosen files.</returns>
        public string[] GetFiles()
        {
            string[] files = GetFiles(true);
            if (files != null && files.Any(f => File.Exists(f)))
                Configuration.Settings.PreviousFolder = Path.GetDirectoryName(files.First(f => File.Exists(f)));
            return files;
        }

        /// <summary>
        /// Runs the dialog. Returns the chosen files/directories.
        /// </summary>
        /// <param name="selectMultiple">Whether the user is allowed to select multiple files/directories.</param>
        /// <returns>Array containing the paths of the chosen files/directories.</returns>
        private string[] GetFiles(bool selectMultiple)
        {
#if NETCOREAPP
            return GenericFileDialog(selectMultiple);
#else
            if (ProcessUtilities.CurrentOS.IsWindows)
                return WindowsFileDialog(selectMultiple);
            else if (ProcessUtilities.CurrentOS.IsMac)
                return OSXFileDialog(selectMultiple);
            else
                return GenericFileDialog(selectMultiple);
#endif
        }

        /// <summary>
        /// Ask the user for a file name. Used on OSs which are not Windows or MacOS.
        /// </summary>
        /// <param name="selectMultiple">Whether or not the user is allowed to select multiple files.</param>
        /// <returns>Array of files selected by the user.</returns>
        public string[] GenericFileDialog(bool selectMultiple)
        {
            string buttonText = string.Empty;
            FileChooserAction gtkActionType;
            if (Action == FileActionType.Open)
            {
                buttonText = "Open";
                gtkActionType = FileChooserAction.Open;
            }
            else if (Action == FileActionType.Save)
            {
                buttonText = "Save";
                gtkActionType = FileChooserAction.Save;
            }
            else if (Action == FileActionType.SelectFolder)
            {
                buttonText = "Select Folder";
                gtkActionType = FileChooserAction.SelectFolder;
            }
            else
                throw new Exception("This file chooser dialog has specified more than one action type.");

#if NETFRAMEWORK
            FileChooserDialog fileChooser = new FileChooserDialog(Prompt, null, gtkActionType, "Cancel", ResponseType.Cancel, buttonText, ResponseType.Accept);
#else
            Window window = (Window)((ViewBase)ViewBase.MasterView).MainWidget;
            FileChooserNative fileChooser = new FileChooserNative(Prompt, window, gtkActionType, buttonText, "Cancel");
#endif
            fileChooser.SelectMultiple = selectMultiple;

            if (!string.IsNullOrEmpty(FileType))
            {
                string[] specParts = FileType.Split(new Char[] { '|' });
                for (int i = 0; i < specParts.Length; i += 2)
                {
                    FileFilter fileFilter = new FileFilter();
                    fileFilter.Name = specParts[i];
                    fileFilter.AddPattern(specParts[i + 1]);
                    fileChooser.AddFilter(fileFilter);
                }
            }

            FileFilter allFilter = new FileFilter();
            allFilter.AddPattern("*");
            allFilter.Name = "All files";
            fileChooser.AddFilter(allFilter);

            fileChooser.SetCurrentFolder(InitialDirectory);

            string[] fileNames = new string[0];
            if (fileChooser.Run() == (int)ResponseType.Accept)
                fileNames = fileChooser.Filenames;
#if NETFRAMEWORK
            fileChooser.Cleanup();
#else
            fileChooser.Dispose();
#endif
            return fileNames;
        }
#if NETFRAMEWORK
        /// <summary>
        /// Ask user for a filename to open on Windows.
        /// </summary>
        /// <param name="selectMultiple">Allow the user to select multiple files?</param>
        private string[] WindowsFileDialog(bool selectMultiple)
        {
            System.Windows.Forms.FileDialog dialog = null;
            if (Action == FileActionType.Open)
            {
                dialog = new System.Windows.Forms.OpenFileDialog();
                (dialog as System.Windows.Forms.OpenFileDialog).Multiselect = selectMultiple;
            }
            else if (Action == FileActionType.Save)
                dialog = new System.Windows.Forms.SaveFileDialog();
            else if (Action == FileActionType.SelectFolder)
                return WindowsDirectoryDialog(selectMultiple);

            dialog.Title = Prompt;

            if (string.IsNullOrEmpty(FileType))
                dialog.Filter = "All files (*.*)|*.*";
            else if (FileType.Contains("|"))
                dialog.Filter = FileType;
            else
                dialog.Filter = FileType + "|" + FileType;

            // This almost works, but Windows is buggy.
            // If the file name is long, it doesn't display in a sensible way. ¯\_(ツ)_/¯
            dialog.InitialDirectory = InitialDirectory;
            dialog.FileName = null;

            string[] fileNames = new string[0];
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                fileNames = dialog.FileNames;
            dialog = null;
            return fileNames;
        }

        /// <summary>
        /// Ask user for a filename to open on Windows.
        /// </summary>
        /// <param name="selectMultiple">Allow the user to select multiple files?</param>
        private string[] OSXFileDialog(bool selectMultiple)
        {
            int result = 0;
            NSSavePanel panel = null;
            if (Action  == FileActionType.Open)
            {
                panel = new NSOpenPanel();
                (panel as NSOpenPanel).AllowsMultipleSelection = selectMultiple;
                (panel as NSOpenPanel).CanChooseDirectories = false;
                (panel as NSOpenPanel).CanChooseFiles = true;
            }
            else if (Action == FileActionType.Save)
                panel = new NSSavePanel();
            else if (Action == FileActionType.SelectFolder)
            {
                panel = new NSOpenPanel();
                (panel as NSOpenPanel).AllowsMultipleSelection = selectMultiple;
                (panel as NSOpenPanel).CanChooseDirectories = true;
                (panel as NSOpenPanel).CanChooseFiles = false;
            }
            else
                throw new Exception("This file chooser dialog has specified more than one action type.");

            panel.Title = Prompt;

            if (!string.IsNullOrEmpty(FileType))
            {
                string[] specParts = FileType.Split(new Char[] { '|' });
                int nExts = 0;
                string[] allowed = new string[specParts.Length / 2];
                for (int i = 0; i < specParts.Length; i += 2)
                {
                    string pattern = Path.GetExtension(specParts[i + 1]);
                    if (!string.IsNullOrEmpty(pattern))
                    {
                        pattern = pattern.Substring(1); // Get rid of leading "."
                        if (!string.IsNullOrEmpty(pattern))
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
            panel.DirectoryUrl = new MonoMac.Foundation.NSUrl(InitialDirectory);
            
            result = panel.RunModal();
            string[] fileNames = new string[0];
            if (result == 1 /*NSFileHandlingPanelOKButton*/)
                fileNames = panel is NSOpenPanel ? (panel as NSOpenPanel).Urls.Select(u => u.Path).ToArray() : new string[] { panel.Url.Path };
            panel.Dispose();
            return fileNames;
        }

        /// <summary>Ask user for a directory on Windows.</summary>
        /// <param name="selectMultiple">Allow the user to select multiple directories?</param>
        /// <returns>string containing the path to the chosen directory.</returns>
        /// <remarks>
        /// For reasons which are unclear to me, the Windows file chooser dialog cannot not be 
        /// configured to select directories. Therefore, directory selection on Windows has 
        /// been moved to a separate method from file selection.
        /// </remarks>
        private string[] WindowsDirectoryDialog(bool selectMultiple)
        {
            // Windows directory choosers cannot allow multiple directory selection.
            // Windows file choosers cannot allow directory selection.
            // Therefore, if we want to select multiple directories, we will need to use the Gtk file chooser
            // dialog, which may be configured to allow selection of multiple directories.
            // Side note - this will have the gnome 'look and feel', rather than the Windows one which
            // you might be expecting.
            if (selectMultiple)
                return GenericFileDialog(true);

            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog()
            {
                Description = Prompt,
                SelectedPath = InitialDirectory
            };
            
            string fileName = string.Empty;
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                fileName = dialog.SelectedPath;
            dialog.Dispose();
            dialog = null;
            return new string[] { fileName };
        }
#endif
    }
}
