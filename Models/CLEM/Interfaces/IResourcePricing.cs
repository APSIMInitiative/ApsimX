using Models.CLEM.Resources;
using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        /// A method to set the current price
        /// </summary>
        /// <param name="amount">New price</param>
        /// <param name="model">Modifying model</param>
        void SetPrice(double amount, IModel model);
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
