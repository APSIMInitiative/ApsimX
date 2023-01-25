using System;

namespace Models.CLEM.Interfaces
{
    /// <summary>
    /// Event timer interface
    /// </summary>
    public interface IActivityTimer
    {
        /// <summary>
        /// Method to determine whether the activity is due
        /// </summary>
        /// <returns>Whether the activity is due in the current month</returns>
        bool ActivityDue { get; }

        /// <summary>
        /// Method to determine whether the activity is due based on a specified date
        /// </summary>
        /// <returns>Whether the activity is due based on the specified date</returns>
        bool Check(DateTime dateToCheck);

        /// <summary>
        /// Timer due and performed trigger
        /// </summary>
        /// <param name="e"></param>
        void OnActivityPerformed(EventArgs e);

        /// <summary>
        /// A status message to provide with this perfromed item
        /// </summary>
        string StatusMessage { get; set; }
    }
}
