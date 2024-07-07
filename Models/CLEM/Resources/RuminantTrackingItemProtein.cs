using Models.CLEM.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// Ruminant tracking item with protein functionality
    /// </summary>
    public class RuminantTrackingItemProtein: IRuminantTrackingItem
    {
        /// <inheritdoc/>
        public double Amount { get; set; }

        /// <inheritdoc/>
        public double Change { get; private set; }

        /// <inheritdoc/>
        public double Previous { get { return Amount - Change; } }

        /// <inheritdoc/>
        public double Extra { get; set; }

        /// <summary>
        /// Report protein for maintenance (kg)
        /// </summary>
        public double ForMaintenence { get; set; }

        /// <summary>
        /// Report protein for maintenance (kg)
        /// </summary>
        public double ForWool { get; set; }

        /// <summary>
        /// Report protein for pregnancy (kg)
        /// </summary>
        public double ForPregnancy { get; set; }

        /// <summary>
        /// Report protein required for kg gain defined from net energy (kg)
        /// </summary>
        public double ForGain { get; set; }

        /// <summary>
        /// Report protein avalable after leaving stomach and accounting for other protein use (kg)
        /// </summary>
        public double AvailableForGain { get; set; }

        /// <inheritdoc/>
        public void Adjust(double change)
        {
            Change = change;
            if (Amount + change < 0)
                Change = -Amount;
            Amount += Change;
        }

        /// <inheritdoc/>
        public void Reset()
        {
            Amount = 0;
            Change = 0;
            Extra = 0;
        }

        /// <summary>
        /// Performs the resetting of time-step tracking stores
        /// </summary>
        public void TimeStepReset()
        {
            ForMaintenence = 0;
            ForPregnancy = 0;
            ForGain = 0;
            AvailableForGain = 0;
            ForWool = 0;
        }

        /// <inheritdoc/>
        public void Set(double amount)
        {
            Change = 0;
            Amount = amount;
        }
    }
}
