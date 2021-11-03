using Models.CLEM.Resources;
using Models.Core;
using System;

namespace Models.CLEM.Interfaces
{
    /// <summary>
    /// An interface to interact with resource pricing
    /// </summary>
    public interface IResourcePricing
    {
        /// <summary>
        /// Purchase or sale pricing style
        /// </summary>
        PurchaseOrSalePricingStyleType PurchaseOrSale { get; set; }

        /// <summary>
        /// Previous price
        /// </summary>
        double PreviousPrice { get; set; }

        /// <summary>
        /// Current price
        /// </summary>
        double CurrentPrice { get; }

        /// <summary>
        /// A method to set the current price
        /// </summary>
        /// <param name="amount">New price</param>
        /// <param name="model">Modifying model</param>
        void SetPrice(double amount, IModel model);

        /// <summary>
        /// Resource
        /// </summary>
        IResourceType Resource { get; }

        /// <summary>
        /// Name of model
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Full name of model
        /// </summary>
        string NameWithParent { get; }

    }

    /// <summary>
    /// An interface to interact with resource pricing
    /// </summary>
    public interface IReportPricingChange
    {
        /// <summary>
        /// Price changed event handler
        /// </summary>
        event EventHandler PriceChangeOccurred;

        /// <summary>
        /// Last price change details
        /// </summary>
        ResourcePriceChangeDetails LastPriceChange { get; set; }
    }

}
