using Models.CLEM.Interfaces;
using System;
using System.Collections.Generic;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// Ruminant tracking item
    /// </summary>
    public class RuminantTrackingItemBodyStore : IRuminantTrackingItem, IRuminantTrackingItemBodyStore
    {
        /// <inheritdoc/>
        public double Amount { get; protected set; }

        /// <inheritdoc/>
        public double Change { get; protected set; }

        /// <inheritdoc/>
        public double Previous { get { return Amount - Change; } }

        /// <inheritdoc/>
        public double Net { get; set; }

        /// <summary>
        /// Amount provided (used to track excess) energy or protein
        /// </summary>
        public double ProteinLimited { get; set; }

        /// <summary>
        /// Dictionary of mobilisation pools by reason
        /// </summary>
        protected readonly Dictionary<MobilisationReasonType, MobilisedPool> mobilisationPools = [];

        /// <inheritdoc/>
        public double MobiliseAmount(double amount, double efficiency, MobilisationReasonType destination)
        {
            var destinationPool = mobilisationPools.TryGetValue(destination, out MobilisedPool value) ? value : null;
            if (destinationPool == null)
            {
                destinationPool = new MobilisedPool();
                mobilisationPools[destination] = destinationPool;
            }
            amount = Math.Min(amount, this.Amount);
            destinationPool.Amount += amount * efficiency;
            destinationPool.Additional += amount * (1.0 - efficiency);
            return amount * efficiency;
        }

        /// <inheritdoc/>
        public double MobiliseAmountNeeded(double amount, double efficiency, MobilisationReasonType destination)
        {
            amount = Math.Min(amount / efficiency, this.Amount);
            return MobiliseAmount(amount, efficiency, destination);
        }

        /// <inheritdoc/>
        public double GetMobilisationProvidedByReason(MobilisationReasonType reason)
        {
            return mobilisationPools.TryGetValue(reason, out MobilisedPool value) ? value.Amount : 0;
        }

        /// <inheritdoc/>
        public double GetTotalMobilisedByReason(MobilisationReasonType? reason = null)
        {
            double total = 0;
            foreach (var pool in mobilisationPools)
            {
                if (!reason.HasValue || pool.Key == reason.Value)
                {
                    total += pool.Value.Amount + pool.Value.Additional;
                }
            }
            return total;
        }

        /// <summary>
        /// Ruminant tracking item Constructor
        /// </summary>
        public RuminantTrackingItemBodyStore(double initialAmount = 0)
        {
            Adjust(initialAmount);   
        }

        /// <summary>
        /// Adjust this tracking item with change.
        /// </summary>
        /// <param name="ind">Individual to determine age and dry protein content</param>
        /// <param name="change">Amount to change by.</param>
        public void Adjust(double change, Ruminant ind = null)
        {
            Change = change;
            if (Amount + change < 0)
                Change = -Amount;
            Amount += Change;
        }

        /// <summary>
        /// Define the last change and define previous based on current amount and change
        /// </summary>
        /// <param name="change">Amount of change</param>
        public void SetPreviousChange(double change)
        {
            if (change >= 0)
                Change = Math.Min(Amount, change);
        }

        /// <summary>
        /// Set this tracking item.
        /// </summary>
        /// <param name="amount">Amount to set.</param>
        public void Set(double amount)
        {
            Change = amount-Amount;
            Amount = amount;
        }

        /// <summary>
        /// Reset this tracking item to 0.
        /// </summary>
        public void Reset()
        {
            Change =  - Amount;
            Amount = 0;
            Net = 0;
        }

        /// <inheritdoc/>
        public void TimeStepReset()
        {
            // clear values in mobilisation pools
            foreach (var pool in mobilisationPools)
            {
                pool.Value.Reset();
            }
        }

    }

    /// <summary>
    /// Tracks the amount mobilised and extra needed for mobilisation efficiency 
    /// </summary>
    public class MobilisedPool
    {
        /// <summary>
        /// The amount mobilised for use elsewhere
        /// </summary>
        public double Amount { get; set; }
        /// <summary>
        /// Gets or sets the additional mobilised amount due to mobilisation efficiency.
        /// </summary>
        public double Additional { get; set; }

        /// <summary>
        /// Reset the pool amounts to zero.
        /// </summary>
        public void Reset()
        {
            Amount = 0;
            Additional = 0;
        }
    }


}
