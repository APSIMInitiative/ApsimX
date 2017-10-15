
namespace Models.Core
{
    using System;
    using System.Collections.Generic;

    /// <summary>An EventArgs class for 'Loaded' events</summary>
    public class LoadedEventArgs : EventArgs
    {
        /// <summary>List of errors that occurred during 'Loaded' events</summary>
        public List<Exception> errors = new List<Exception>();
    }
}
