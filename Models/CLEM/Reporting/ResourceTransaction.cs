using Models.CLEM.Activities;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Models.CLEM
{
    /// <summary>
    /// Class for tracking Resource transactions
    /// </summary>
    [Serializable]
    public class ResourceTransaction
    {
        /// <summary>
        /// Resource type in transaction
        /// </summary>
        public CLEMResourceTypeBase ResourceType { get; set; }
        /// <summary>
        /// Sender activity
        /// </summary>
        public CLEMModel Activity { get; set; }
        /// <summary>
        /// Cateogry for data analysis and summary
        /// </summary>
        public string Category { get; set; }
        /// <summary>
        /// Resource this transaction relates to for data analysis and summary
        /// </summary>
        public string RelatesToResource { get; set; }

        /// <summary>
        /// Amount added
        /// </summary>
        public double Gain
        {
            get
            {
                return (Style == TransactionStyle.Gain) ? Amount : 0;
            }
        }
            
        /// <summary>
        /// Amount removed
        /// </summary>
        public double Loss
        {
            get
            {
                return (Style == TransactionStyle.Loss) ? Amount * -1 : 0;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public TransactionStyle Style { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double Amount { get; set; }

        /// <summary>
        /// Object to sotre specific extra information such as cohort details
        /// </summary>
        public object ExtraInformation { get; set; }

        /// <summary>
        /// Convert transaction to another value using ResourceType supplied converter
        /// </summary>
        /// <param name="converterName">Name of converter to use</param>
        /// <param name="transactionType">Indicates if it is a Gain or Loss to convert</param>
        /// <returns>Value to report</returns>
        public object ConvertTo(string converterName, string transactionType)
        {
            if(ResourceType!=null)
            {
                double amount = 0;
                switch (transactionType.ToLower())
                {
                    case "gain":
                        amount = this.Gain;
                        break;
                    case "loss":
                        amount = this.Loss;
                        break;
                    default:

                        break;
                }
                return (ResourceType as CLEMResourceTypeBase).ConvertTo(converterName, amount);
            }
            return null;
        }
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
