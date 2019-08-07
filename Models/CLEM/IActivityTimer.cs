using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM
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

    }
}
