namespace Models.Core
{
    /// <summary>
    /// An enum that is used to indicate message severity when writing
    /// summary messages.
    /// </summary>
    public enum MessageType
    {
        /// <summary>Error message.</summary>
        Error = 0,

        /// <summary>Warning message.</summary>
        Warning = 1,

        /// <summary>Information message.</summary>
        Information = 2,

        /// <summary>Diagnostic message.</summary>
        Diagnostic = 3,

        /// <summary>All</summary>
        All = 100,
    };
}
