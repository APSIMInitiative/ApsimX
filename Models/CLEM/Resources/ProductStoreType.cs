using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace Models.CLEM.Resources
{
    ///<summary>
    /// Store for emission type
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ProductStore))]
    [Description("This resource represents a product store (e.g. cotton)")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Products/ProductStoreType.htm")]
    public class ProductStoreType : CLEMResourceTypeBase, IResourceType, IResourceWithTransactionType
    {
        /// <summary>
        /// Unit type
        /// </summary>
        [Description("Units (nominal)")]
        public string Units { get; set; }

        /// <summary>
        /// Starting amount
        /// </summary>
        [Description("Starting amount")]
        [Required]
        public double StartingAmount { get; set; }

        /// <summary>
        /// Current amount of this resource
        /// </summary>
        public double Amount { get { return amount; } }
        double amount { get { return roundedAmount; } set { roundedAmount = Math.Round(value, 9); } }
        [NonSerialized]
        private double roundedAmount;

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseResource")]
        private void OnCLEMInitialiseResource(object sender, EventArgs e)
        {
            this.amount = 0;
            if (StartingAmount > 0)
                Add(StartingAmount, null, null, "Starting value");
        }

        /// <summary>
        /// Total value of resource
        /// </summary>
        public double? Value
        {
            get
            {
                return Price(PurchaseOrSalePricingStyleType.Sale)?.CalculateValue(Amount);
            }
        }

        #region transactions

        /// <summary>
        /// Add product to store
        /// </summary>
        /// <param name="resourceAmount">Object to add. This object can be double or contain additional information (e.g. Nitrogen) of food being added</param>
        /// <param name="activity">Name of activity adding resource</param>
        /// <param name="relatesToResource"></param>
        /// <param name="category"></param>
        public new void Add(object resourceAmount, CLEMModel activity, string relatesToResource, string category)
        {
            double amountAdded;
            switch (resourceAmount)
            {
                case FoodResourcePacket _:
                    amountAdded = (resourceAmount as FoodResourcePacket).Amount;
                    break;
                case double _:
                    amountAdded = (double)resourceAmount;
                    break;
                default:
                    throw new Exception($"ResourceAmount object of type [{resourceAmount.GetType().Name}] is not supported in [r={Name}]");
            }

            if (amountAdded > 0)
            {
                amount += amountAdded;

                ReportTransaction(TransactionType.Gain, amountAdded, activity, relatesToResource, category, this);
            }
        }

        /// <summary>
        /// Remove from finance type store
        /// </summary>
        /// <param name="request">Resource request class with details.</param>
        public new void Remove(ResourceRequest request)
        {
            if (request.Required == 0)
                return;

            // if this request aims to trade with a market see if we need to set up details for the first time
            if (request.MarketTransactionMultiplier > 0)
                FindEquivalentMarketStore();

            // avoid taking too much
            double amountRemoved = request.Required;
            amountRemoved = Math.Min(this.Amount, amountRemoved);
            this.amount -= amountRemoved;

            // send to market if needed
            if (request.MarketTransactionMultiplier > 0 && EquivalentMarketStore != null)
                (EquivalentMarketStore as ProductStoreType).Add(amountRemoved * request.MarketTransactionMultiplier, request.ActivityModel, this.NameWithParent, "Farm sales");

            request.Provided = amountRemoved;
            if (amountRemoved > 0)
            {
                ReportTransaction(TransactionType.Loss, amountRemoved, request.ActivityModel, request.RelatesToResource, request.Category, this);
            }
        }

        /// <summary>
        /// Set the amount in an account.
        /// </summary>
        /// <param name="newAmount"></param>
        public new void Set(double newAmount)
        {
            amount = newAmount;
        }

        #endregion

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            string html = base.ModelSummary();

            html += "\r\n<div class=\"activityentry\">";
            if (StartingAmount > 0)
                html += "There is <span class=\"setvalue\">" + this.StartingAmount.ToString("#.###") + "</span> at the start of the simulation.";
            html += "\r\n</div>";
            return html;
        }

        #endregion

    }
}
