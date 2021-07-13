using Models.CLEM.Interfaces;
using Models.Core;
using System;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// Details of a resource price change
    /// </summary>
    [Serializable]
    public class ResourcePriceChangeDetails
    {
        /// <summary>
        /// Previous price
        /// </summary>
        public double PreviousPrice { get; set; }

        /// <summary>
        /// Current price
        /// </summary>
        public double CurrentPrice { get; set; }

        /// <summary>
        /// Model making change
        /// </summary>
        public IModel ChangedPriceModel { get; set; }
    }

    /// <summary>
    /// Event arguments for price change to bubble details
    /// </summary>
    [Serializable]
    public class PriceChangeEventArgs: EventArgs
    {
        /// <summary>
        /// Price change details
        /// </summary>
        public ResourcePriceChangeDetails Details { get; set; }
    }
}
