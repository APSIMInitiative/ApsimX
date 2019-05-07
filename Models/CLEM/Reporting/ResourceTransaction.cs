using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Models.CLEM
{
    /// <summary>
    /// Class for tracking Resource transactions
    /// </summary>
    [Serializable]
    public class ResourceTransaction
    {
        /// <summary>
        /// Type of resource in transaction
        /// </summary>
        public string ResourceType { get; set; }
        /// <summary>
        /// Name of sender or activity
        /// </summary>
        public string Activity { get; set; }
        /// <summary>
        /// Name of sender or activity
        /// </summary>
        public string ActivityType { get; set; }
        /// <summary>
        /// Reason or cateogry
        /// </summary>
        public string Reason { get; set; }
        /// <summary>
        /// Amount removed
        /// </summary>
        public double Gain { get; set; }
        /// <summary>
        /// Amount added
        /// </summary>
        public double Loss { get; set; }
        /// <summary>
        /// Standardised amount removed
        /// </summary>
        public double GainStandardised { get; set; }
        /// <summary>
        /// Standardised amount added
        /// </summary>
        public double LossStandardised { get; set; }

        /// <summary>
        /// Object to sotre specific extra information such as cohort details
        /// </summary>
        public object ExtraInformation { get; set; }
    }

    /// <summary>
    /// Class for reporting transaction details in OnTransactionEvents
    /// </summary>
    [Serializable]
    public class TransactionEventArgs: EventArgs
    {
        /// <summary>
        /// Transaction details
        /// </summary>
        public ResourceTransaction Transaction { get; set; }
    }
}
