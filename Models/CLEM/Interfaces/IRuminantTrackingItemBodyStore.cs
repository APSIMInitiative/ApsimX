
using Models.CLEM.Resources;
using System;

namespace Models.CLEM.Interfaces
{
    /// <summary>
    /// Ruminant tracking item
    /// </summary>

    public interface IRuminantTrackingItemBodyStore
    {
        /// <summary>
        /// Performs the resetting of time step tracking stores
        /// </summary>
        public void TimeStepReset();

        /// <summary>
        /// Mobilise amount from body and store amount and additional allocate to pools based on efficiency
        /// </summary>
        /// <param name="amount">Amount mobilised</param>
        /// <param name="efficiency">Mobilisation efficiency</param>
        /// <param name="destination">The reason for mobilisation</param>
        /// <returns>The amount provided from the body store</returns>
        public double MobiliseAmount(double amount, double efficiency, MobilisationReasonType destination);

        /// <summary>
        /// Mobilise amount needed from body and store including that needed for conversion efficiency
        /// </summary>
        /// <param name="amount">Amount required</param>
        /// <param name="efficiency">Mobilisation efficiency</param>
        /// <param name="destination">The reason for mobilisation</param>
        /// <returns>The amount provided from the body store</returns>
        public double MobiliseAmountNeeded(double amount, double efficiency, MobilisationReasonType destination);

        /// <summary>
        /// Get the mobilised amount provided for a specific reason
        /// </summary>
        /// <param name="reason">The reason for mobilisation</param>
        /// <returns>The amount mobilised and provided for the reason</returns>
        public double GetMobilisationProvidedByReason(MobilisationReasonType reason);

        /// <summary>
        /// Get the total mobilised for a specific reason
        /// </summary>
        /// <param name="reason">The reason for mobilisation, ignore for all</param>
        /// <returns>The amount mobilised and provided for the reason</returns>
        public double GetTotalMobilisedByReason(MobilisationReasonType? reason = null);
    }
}