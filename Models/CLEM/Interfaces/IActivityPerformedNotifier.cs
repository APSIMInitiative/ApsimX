using System;

namespace Models.CLEM.Interfaces
{
    /// <summary>
    /// Interface for Activities able to report performed event
    /// </summary>
    public interface IActivityPerformedNotifier
    {
        /// <summary>
        /// Activity performed event handler
        /// </summary>
        event EventHandler ActivityPerformed;
    }
}
