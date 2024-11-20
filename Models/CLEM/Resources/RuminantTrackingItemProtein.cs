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
        /// <summary>
        /// The proportion of dry relative to wet protein mass
        /// </summary>
        public double ProportionDry { get; set; } = 1.0;

        /// <inheritdoc/>
        public double Amount { get; set; }

        /// <summary>
        /// The total mass of wet protein (plus water and ash) as used by Oddy et a Model
        /// </summary>
        public double AmountWet { get { return Amount / ProportionDry; } }

        /// <inheritdoc/>
        public double Change { get; private set; }

        /// <summary>
        /// Change in the total mass of wet protein
        /// </summary>
        public double ChangeWet { get { return Change / ProportionDry; } }

        /// <inheritdoc/>
        public double Previous { get { return Amount - Change; } }

        /// <summary>
        /// Previous total mass of wet protein
        /// </summary>
        public double PreviousWet { get { return Previous / ProportionDry; } }

        /// <inheritdoc/>
        public double Extra { get; set; }

        /// <summary>
        /// Report protein for maintenance (kg)
        /// </summary>
        public double ForMaintenence { get; set; }

        /// <summary>
        /// Report protein for wool (kg)
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

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantTrackingItemProtein(double proportionDry, double initialAmount = 0)
        {
            ProportionDry = proportionDry;
            Amount = initialAmount;
        }

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
