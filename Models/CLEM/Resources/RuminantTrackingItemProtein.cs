using Models.CLEM.Interfaces;

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
        /// Change in the total mass of wet protein (kg timestep-1)
        /// </summary>
        public double ChangeWet { get { return Change / ProportionDry; } }

        /// <inheritdoc/>
        public double Previous { get { return Amount - Change; } }

        /// <summary>
        /// Previous total mass of wet protein (kg timestep-1)
        /// </summary>
        public double PreviousWet { get { return Previous / ProportionDry; } }

        /// <inheritdoc/>
        public double Net { get; set; }

        /// <summary>
        /// Report protein for maintenance (kg day-1)
        /// </summary>
        public double ForMaintenance { get; set; }

        /// <summary>
        /// Report protein for wool (kg day-1)
        /// </summary>
        public double ForWool { get; set; }

        /// <summary>
        /// Report protein for pregnancy (kg day-1)
        /// </summary>
        public double ForPregnancy { get; set; }

        /// <summary>
        /// Report protein required for kg gain defined from net energy (kg day-1)
        /// </summary>
        public double ForGain { get; set; }

        /// <summary>
        /// Report protein avalable after leaving stomach and accounting for other protein use (kg day-1)
        /// </summary>
        public double AvailableForGain { get; set; }

        /// <summary>
        /// Protein mass at mature (kg)
        /// </summary>
        public double ProteinMassAtSRW { get; set; }

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
            Net = 0;
        }

        /// <summary>
        /// Performs the resetting of time-step tracking stores
        /// </summary>
        public void TimeStepReset()
        {
            ForMaintenance = 0;
            ForPregnancy = 0;
            ForGain = 0;
            AvailableForGain = 0;
            ForWool = 0;
            Net = 0;
        }

        /// <inheritdoc/>
        public void Set(double amount)
        {
            Change = 0;
            Amount = amount;
        }
    }
}
