namespace UserInterface.Interfaces
{
    using Utility;

    /// <summary>
    /// Contract to provide options for asking the user for file names.
    /// </summary>
    /// <remarks>
    /// 
    /// </remarks>
    interface IFileDialog
    {
        /// <summary>
        /// Defines the type of action which the dialog should perform.
        /// </summary>
        FileDialog.FileActionType Action { get; set; }

        /// <summary>
        /// File types which the user is allowed to choose.
        /// </summary>
        string FileType { get; set; }

        /// <summary>
        /// Initial directory used when the file dialog runs.
        /// If left empty or null, the Apsim-wide default will be used.
        /// </summary>
        string InitialDirectory { get; set; }

        /// <summary>
        /// Prompt displayed in the title bar of the dialog.
        /// Defaults to "Choose a file."
        /// </summary>
        string Prompt { get; set; }

        /// <summary>
        /// Runs the dialog.
        /// Returns the chosen file/directory.
        /// </summary>
        /// <returns>The chosen file.</returns>
        string GetFile();

        /// <summary>
        /// Runs the dialog.
        /// Returns an array of chosen files/directories.
        /// </summary>
        /// <returns>Array of chosen files.</returns>
        string[] GetFiles();
    }
}
