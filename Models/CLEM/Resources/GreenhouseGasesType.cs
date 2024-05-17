using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace Models.CLEM.Resources
{
    ///<summary>
    /// Store for emission type
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(GreenhouseGases))]
    [Description("This resource represents a greenhouse gas (e.g. CO2)")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Greenhouse gases/GreenhouseGasType.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    public class GreenhouseGasesType : CLEMResourceTypeBase, IResourceWithTransactionType, IResourceType
    {
        private double amount { get { return roundedAmount; } set { roundedAmount = Math.Round(value, 9); } }
        private double roundedAmount;

        /// <summary>
        /// Unit type
        /// </summary>
        [JsonIgnore]
        [Description("Units (nominal)")]
        public string Units { get { return "kg"; } }

        /// <summary>
        /// Auto collect emissions
        /// </summary>
        [Description("Auto collect")]
        [Required]
        public GreenhouseGasTypes AutoCollectType { get; set; }

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

        #region transactions

        /// <summary>
        /// Add money to account
        /// </summary>
        /// <param name="resourceAmount">Object to add. This object can be double or contain additional information (e.g. Nitrogen) of food being added</param>
        /// <param name="activity">Name of activity adding resource</param>
        /// <param name="relatesToResource"></param>
        /// <param name="category"></param>
        public new void Add(object resourceAmount, CLEMModel activity, string relatesToResource, string category)
        {
            if (resourceAmount.GetType().ToString() != "System.Double")
                throw new Exception(String.Format("ResourceAmount object of type {0} is not supported Add method in {1}", resourceAmount.GetType().ToString(), this.Name));

            double amountAdded = (double)resourceAmount;
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
                (EquivalentMarketStore as GreenhouseGasesType).Add(amountRemoved * request.MarketTransactionMultiplier, request.ActivityModel, this.NameWithParent, "Farm sales");

            request.Provided = amountRemoved;

            ReportTransaction(TransactionType.Loss, amountRemoved, request.ActivityModel, request.RelatesToResource, request.Category, this);
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
            if (StartingAmount > 0)
            {
                using StringWriter htmlWriter = new();
                htmlWriter.Write("<div class=\"activityentry\">");
                htmlWriter.Write("There is a starting amount of <span class=\"setvalue\">" + this.StartingAmount.ToString("0.#") + "</span></div>");
                return htmlWriter.ToString();
            }
            return "";
        }
        #endregion

    }
}
