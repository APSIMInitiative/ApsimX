using System;

namespace UserInterface.EventArguments
{
    /// <summary>
    /// Custom event arguments used to perform an action on a file.
    /// </summary>
    public class FileActionArgs : EventArgs
    {
        /// <summary>
        /// Path to the file.
        /// </summary>
        public string Path { get; set; }
    }
}
