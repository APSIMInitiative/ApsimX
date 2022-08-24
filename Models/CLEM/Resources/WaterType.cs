using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Models.CLEM.Resources
{
    ///<summary>
    /// Store for water type
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(WaterStore))]
    [Description("This resource represents a water store (e.g. dam, bore, tank)")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Water/WaterStoreType.htm")]
    public class WaterType : CLEMResourceTypeBase, IResourceWithTransactionType, IResourceType
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
        public double Amount { get; private set; }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseResource")]
        private void OnCLEMInitialiseResource(object sender, EventArgs e)
        {
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
        /// Back account transaction occured
        /// </summary>
        public event EventHandler TransactionOccurred;

        /// <summary>
        /// Transcation occurred 
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnTransactionOccurred(EventArgs e)
        {
            TransactionOccurred?.Invoke(this, e);
        }

        /// <summary>
        /// Last transaction received
        /// </summary>
        [JsonIgnore]
        public ResourceTransaction LastTransaction { get; set; }

        /// <summary>
        /// Add water to water store
        /// </summary>
        /// <param name="resourceAmount">Object to add. This object can be double or contain additional information (e.g. Nitrogen) of food being added</param>
        /// <param name="activity">Name of activity adding resource</param>
        /// <param name="relatesToResource"></param>
        /// <param name="category"></param>
        public new void Add(object resourceAmount, CLEMModel activity, string relatesToResource, string category)
        {
            if (resourceAmount.GetType().ToString() != "System.Double")
                throw new Exception(String.Format("ResourceAmount object of type {0} is not supported Add method in {1}", resourceAmount.GetType().ToString(), this.Name));

            double addAmount = (double)resourceAmount;
            if (addAmount > 0)
            {
                Amount += addAmount;

                ResourceTransaction details = new ResourceTransaction
                {
                    TransactionType = TransactionType.Gain,
                    Amount = addAmount,
                    Activity = activity,
                    RelatesToResource = relatesToResource,
                    Category = category,
                    ResourceType = this
                };
                base.LastGain = addAmount;
                LastTransaction = details;
                TransactionEventArgs te = new TransactionEventArgs() { Transaction = details };
                OnTransactionOccurred(te);
            }
        }

        /// <summary>
        /// Remove from water type store
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
            amountRemoved = Math.Min(Amount, amountRemoved);
            Amount -= amountRemoved;

            // send to market if needed
            if (request.MarketTransactionMultiplier > 0 && EquivalentMarketStore != null)
                (EquivalentMarketStore as WaterType).Add(amountRemoved * request.MarketTransactionMultiplier, request.ActivityModel, this.NameWithParent, "Farm sales");

            request.Provided = amountRemoved;
            ResourceTransaction details = new ResourceTransaction
            {
                ResourceType = this,
                TransactionType = TransactionType.Loss,
                Amount = amountRemoved,
                Activity = request.ActivityModel,
                Category = request.Category,
                RelatesToResource = request.RelatesToResource
            };
            LastTransaction = details;
            TransactionEventArgs te = new TransactionEventArgs() { Transaction = details };
            OnTransactionOccurred(te);
        }

        /// <summary>
        /// Set the amount in an a water type.
        /// </summary>
        /// <param name="newAmount"></param>
        public new void Set(double newAmount)
        {
            Amount = newAmount;
        }

        #endregion

    }
}
