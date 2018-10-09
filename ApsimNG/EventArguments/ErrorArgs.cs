using System;

namespace UserInterface.EventArguments
{
    /// <summary>
    /// Lightweight event args used to pass data when an error is thrown in a view.
    /// </summary>
    public class ErrorArgs : EventArgs
    {
        /// <summary>
        /// Exception which has been thrown.
        /// </summary>
        public Exception Error { get; set; }
    }
}
