using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Models.CLEM.Resources
{
    ///<summary>
    /// Store for water type
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(WaterStore))]
    [Description("This resource represents a water store type (e.g. dam, bore, tank).")]
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
        public double Amount { get { return amount; } }
        private double amount { get { return roundedAmount; } set { roundedAmount = Math.Round(value, 9); } }
        private double roundedAmount;

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseResource")]
        private void OnCLEMInitialiseResource(object sender, EventArgs e)
        {
            this.amount = 0;
            if (StartingAmount > 0)
            {
                Add(StartingAmount, this, "Starting value");
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
        [XmlIgnore]
        public ResourceTransaction LastTransaction { get; set; }

        /// <summary>
        /// Add money to account
        /// </summary>
        /// <param name="resourceAmount">Object to add. This object can be double or contain additional information (e.g. Nitrogen) of food being added</param>
        /// <param name="activity">Name of activity adding resource</param>
        /// <param name="reason">Name of individual adding resource</param>
        public new void Add(object resourceAmount, CLEMModel activity, string reason)
        {
            if (resourceAmount.GetType().ToString() != "System.Double")
            {
                throw new Exception(String.Format("ResourceAmount object of type {0} is not supported Add method in {1}", resourceAmount.GetType().ToString(), this.Name));
            }
            double addAmount = (double)resourceAmount;
            if (addAmount > 0)
            {
                amount += addAmount;

                ResourceTransaction details = new ResourceTransaction
                {
                    Gain = addAmount,
                    Activity = activity,
                    Reason = reason,
                    ResourceType = this
                };
                LastTransaction = details;
                TransactionEventArgs te = new TransactionEventArgs() { Transaction = details };
                OnTransactionOccurred(te);
            }
        }

        /// <summary>
        /// Remove from finance type store
        /// </summary>
        /// <param name="request">Resource request class with details.</param>
        public new void Remove(ResourceRequest request)
        {
            if (request.Required == 0)
            {
                return;
            }

            // if this request aims to trade with a market see if we need to set up details for the first time
            if (request.MarketTransactionMultiplier > 0)
            {
                FindEquivalentMarketStore();
            }

            // avoid taking too much
            double amountRemoved = request.Required;
            amountRemoved = Math.Min(this.Amount, amountRemoved);
            this.amount -= amountRemoved;

            // send to market if needed
            if (request.MarketTransactionMultiplier > 0 && EquivalentMarketStore != null)
            {
                (EquivalentMarketStore as WaterType).Add(amountRemoved * request.MarketTransactionMultiplier, request.ActivityModel, "Farm sales");
            }

            request.Provided = amountRemoved;
            ResourceTransaction details = new ResourceTransaction
            {
                ResourceType = this,
                Loss = amountRemoved,
                Activity = request.ActivityModel,
                Reason = request.Reason
            };
            LastTransaction = details;
            TransactionEventArgs te = new TransactionEventArgs() { Transaction = details };
            OnTransactionOccurred(te);
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

    }
}
