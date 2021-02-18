using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM
{
    /// <summary>
    /// Interface to add transaction tracking ability to a Resource Type.
    /// </summary>
    public interface IResourceWithTransactionType
    {
        /// <summary>
        /// Resource transaction occured event handler
        /// </summary>
        event EventHandler TransactionOccurred;

        /// <summary>
        /// Last transaction received
        /// </summary>
        ResourceTransaction LastTransaction { get; set; }

        /// <summary>
        /// Last gain transaction amount
        /// </summary>
        double LastGain { get; }

    }
}
