using UserInterface.Interfaces;
using System;
using System.IO;
using System.Linq;
using Gtk;
using APSIM.Shared.Utilities;
using Models.Core;
using UserInterface.Extensions;
using UserInterface.Views;

namespace Utility
{
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
            {
                Configuration.Settings.PreviousFolder = Path.GetDirectoryName(files.First(f => File.Exists(f)));
                Configuration.Settings.Save();
            }
            return files;
        }

        /// <summary>
        /// Runs the dialog. Returns the chosen files/directories.
        /// </summary>
        /// <param name="selectMultiple">Whether the user is allowed to select multiple files/directories.</param>
        /// <returns>Array containing the paths of the chosen files/directories.</returns>
        private string[] GetFiles(bool selectMultiple)
        {

            return GenericFileDialog(selectMultiple);

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


            Window window = (Window)((ViewBase)ViewBase.MasterView).MainWidget;
            FileChooserNative fileChooser = new FileChooserNative(Prompt, window, gtkActionType, buttonText, "Cancel");

            fileChooser.SelectMultiple = selectMultiple;

            string[] specParts = null;
            if (!string.IsNullOrEmpty(FileType))
            {
                specParts = FileType.Split(new Char[] { '|' });
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
            fileChooser.DoOverwriteConfirmation = true;

            bool tryAgain;
            string[] fileNames;
            do
            {
                fileNames = new string[0];
                if (fileChooser.Run() == (int)ResponseType.Accept)
                    fileNames = fileChooser.Filenames;

                // The Gtk FileChooser does NOT automatically append extensions based on the currently selected filter
                // We need to do this somewhat manually when saving files.
                //
                // Note that the following makes the assumption that specified filters have the form "*.ext".
                //
                // It is perhaps unclear whether, when the filename does not have the selected filter's extension,
                // an existing extension should be replaced, or added to. That is, if the selected filter
                // is "*.apsimx" and the user has provided the name "Wheat.json", should the returned name be 
                // "Wheat.apsimx" or "Wheat.json.apsimx"? I have elected to append, rather than replace.
                // There is also the question of whether case differences should be considered or not.

                
                tryAgain = false;
                if (ProcessUtilities.CurrentOS.IsWindows && Action == FileActionType.Save && fileChooser.Filter != allFilter && specParts != null)
                {
                    string filterName = fileChooser.Filter.Name;
                    for (int i = 0; i < specParts.Length; i += 2)
                    {
                        if (filterName == specParts[i])
                        {
                            string filterExt = Path.GetExtension(specParts[i + 1]);
                            for (int j = 0; j < fileNames.Length; j++)
                            {
                                if (!Path.GetExtension(fileNames[j]).Equals(filterExt, StringComparison.CurrentCultureIgnoreCase))
                                {
                                    fileNames[j] = fileNames[j] + filterExt;
                                    if (File.Exists(fileNames[j]))
                                    {
                                        tryAgain = true;
                                        fileChooser.SetFilename(fileChooser.Filename + filterExt);
                                    }
                                }
                            }
                            break; // We've applied one extension; let's not risk trying to apply another
                        }
                    }
                }
            } while (tryAgain);

            fileChooser.Dispose();

            return fileNames;
        }


    }
}
