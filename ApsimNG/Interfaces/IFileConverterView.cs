namespace UserInterface.Interfaces
{
    using System;

    /// <summary>
    /// Interface for a view which gets information from the user used to 
    /// convert an xml file to a different version.
    /// </summary>
    interface IFileConverterView
    {
        /// <summary>
        /// Version to which the user wants to upgrade the file.
        /// </summary>
        int ToVersion { get; }

        /// <summary>
        /// Path to the file.
        /// </summary>
        string[] Files { get; }

        /// <summary>
        /// If true, we automatically upgrade to the latest version.
        /// </summary>
        bool LatestVersion { get; }

        /// <summary>
        /// Controls the visibility of the view.
        /// Settings this to true displays the view.
        /// </summary>
        bool Visible { get; set; }

        /// <summary>
        /// Invoked when the user hits clicks convert button.
        /// </summary>
        event EventHandler Convert;

        /// <summary>
        /// Does some cleanup when the object is no longer needed.
        /// </summary>
        void Destroy();
    }
}
