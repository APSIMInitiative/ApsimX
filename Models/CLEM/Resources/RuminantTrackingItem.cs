using APSIM.Shared.Utilities;
using DocumentFormat.OpenXml.Office2016.Drawing.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// Ruminant tracking item
    /// </summary>
    public class RuminantTrackingItem
    {
        /// <summary>
        /// Current amount
        /// </summary>
        public double Amount { get; set; }

        /// <summary>
        /// Recent change
        /// </summary>
        public double Change { get; private set; }

        /// <summary>
        /// Previous amount
        /// </summary>
        public double Previous { get { return Amount - Change; }  }

        /// <summary>
        /// Adjust this tracking item with change.
        /// </summary>
        /// <param name="change">Amount to change by.</param>
        public void Adjust(double change)
        {
            Change = change;
            if (Amount + change < 0)
                Change = -Amount;
            Amount += Change;
        }

        /// <summary>
        /// Set this tracking item.
        /// </summary>
        /// <param name="amount">Amount to set.</param>
        public void Set(double amount)
        {
            Change = 0; // amount-Amount;
            Amount = amount;
        }

        /// <summary>
        /// Reset this tracking item to 0.
        /// </summary>
        public void Reset()
        {
            Change = 0-Amount;
            Amount = 0;
        }
    }
}
