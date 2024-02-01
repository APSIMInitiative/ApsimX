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
            if (Amount == 0)
                Amount = change;
            else
                Amount += Math.Min(Amount, change);
        }

        /// <summary>
        /// Reset this tracking item to 0.
        /// </summary>
        public void Reset()
        {
            Change = Amount;
            Amount = 0;
        }
    }
}
