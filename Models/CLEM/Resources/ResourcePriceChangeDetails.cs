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
        /// Pricing component
        /// </summary>
        public IResourcePricing PriceChanged { get; set; }

        /// <summary>
        /// Model making change
        /// </summary>
        public IModel ChangedBy { get; set; }
    }

    /// <summary>
    /// Event arguments for price change to bubble details
    /// </summary>
    [Serializable]
    public class PriceChangeEventArgs : EventArgs
    {
        /// <summary>
        /// Price change details
        /// </summary>
        public ResourcePriceChangeDetails Details { get; set; }
    }
}
