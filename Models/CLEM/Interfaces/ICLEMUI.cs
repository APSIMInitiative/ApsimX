using System;

namespace Models.CLEM.Interfaces
{
    /// <summary>
    /// Interface for UI parameters associated with models.
    /// </summary>
    public interface ICLEMUI
    {
        /// <summary>
        /// Selected display tab
        /// </summary>
        string SelectedTab { get; set; }
    }
}
