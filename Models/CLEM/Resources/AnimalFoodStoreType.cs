using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace Models.CLEM.Resources
{

    /// <summary>
    /// This stores the initialisation parameters for a fodder type.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(AnimalFoodStore))]
    [Description("This resource represents an animal food store (e.g. lucerne)")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/AnimalFoodStore/AnimalFoodStoreType.htm")]
    public class AnimalFoodStoreType : CLEMResourceTypeBase, IFeedType, IResourceType
    {
        /// <summary>
        /// Unit type
        /// </summary>
        public string Units { get; private set; }

        /// <summary>
        /// Dry Matter Digestibility (%)
        /// </summary>
        [Description("Dry Matter Digestibility (%)")]
        [Required, Percentage, GreaterThanValue(0)]
        public double DMD { get; set; }

        /// <summary>
        /// Nitrogen (%)
        /// </summary>
        [Description("Nitrogen (%)")]
        [Required, Percentage, GreaterThanValue(0)]
        public double Nitrogen { get; set; }

        /// <summary>
        /// Current store nitrogen (%)
        /// </summary>
        [JsonIgnore]
        public double CurrentStoreNitrogen { get; set; }

        /// <summary>
        /// Starting Amount (kg)
        /// </summary>
        [Description("Starting Amount (kg)")]
        [Required, GreaterThanEqualValue(0)]
        public double StartingAmount { get; set; }

        /// <summary>
        /// Amount currently available (kg dry)
        /// </summary>
        [JsonIgnore]
        public double Amount { get { return amount; } set { return; } }
        private double amount { get { return roundedAmount; } set { roundedAmount = Math.Round(value, 9); } }
        private double roundedAmount;

        /// <summary>
        /// Constructor
        /// </summary>
        public AnimalFoodStoreType()
        {
            Units = "kg";
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

        #region Transactions

        /// <summary>
        /// Add to food store
        /// </summary>
        /// <param name="resourceAmount">Object to add. This object can be double or contain additional information (e.g. Nitrogen) of food being added</param>
        /// <param name="activity">Name of activity adding resource</param>
        /// <param name="relatesToResource"></param>
        /// <param name="category"></param>
        public new void Add(object resourceAmount, CLEMModel activity, string relatesToResource, string category)
        {
            double amountAdded;
            double nAdded;
            switch (resourceAmount.GetType().ToString())
            {
                case "System.Double":
                    amountAdded = (double)resourceAmount;
                    nAdded = Nitrogen;
                    break;
                case "Models.CLEM.Resources.FoodResourcePacket":
                    amountAdded = ((FoodResourcePacket)resourceAmount).Amount;
                    nAdded = ((FoodResourcePacket)resourceAmount).PercentN;
                    break;
                default:
                    throw new Exception(String.Format("ResourceAmount object of type {0} is not supported Add method in {1}", resourceAmount.GetType().ToString(), this.Name));
            }

            if (amountAdded > 0)
            {
                // update N based on new input added
                CurrentStoreNitrogen = ((CurrentStoreNitrogen * Amount) + (nAdded * amountAdded)) / (Amount + amountAdded);

                this.amount += amountAdded;

                ReportTransaction(TransactionType.Gain, amountAdded, activity, relatesToResource, category, this);
            }
        }

        /// <summary>
        /// Remove from animal food store
        /// </summary>
        /// <param name="request">Resource request class with details.</param>
        public new void Remove(ResourceRequest request)
        {
            if (request.Required == 0)
                return;

            // if this request aims to trade with a market see if we need to set up details for the first time
            if (request.MarketTransactionMultiplier > 0)
                FindEquivalentMarketStore();

            double amountRemoved = request.Required;
            // avoid taking too much
            amountRemoved = Math.Min(this.amount, amountRemoved);

            if (amountRemoved > 0)
            {
                this.amount -= amountRemoved;

                FoodResourcePacket additionalDetails = new FoodResourcePacket
                {
                    DMD = this.DMD,
                    PercentN = this.CurrentStoreNitrogen
                };
                request.AdditionalDetails = additionalDetails;

                request.Provided = amountRemoved;

                // send to market if needed
                if (request.MarketTransactionMultiplier > 0 && EquivalentMarketStore != null)
                {
                    additionalDetails.Amount = amountRemoved * request.MarketTransactionMultiplier;
                    (EquivalentMarketStore as AnimalFoodStoreType).Add(additionalDetails, request.ActivityModel, request.ResourceTypeName, "Farm sales");
                }

                ReportTransaction(TransactionType.Loss, amountRemoved, request.ActivityModel, request.RelatesToResource, request.Category, this);
            }
            return;
        }

        /// <summary>
        /// Set amount of animal food available
        /// </summary>
        /// <param name="newValue">New value to set food store to</param>
        public new void Set(double newValue)
        {
            this.amount = newValue;
        }

        #endregion

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("<div class=\"activityentry\">");
                htmlWriter.Write("This food has a nitrogen content of <span class=\"setvalue\">" + this.Nitrogen.ToString("0.###") + "%</span>");
                if (DMD > 0)
                    htmlWriter.Write(" and a Dry Matter Digesibility of <span class=\"setvalue\">" + this.DMD.ToString("0.###") + "%</span>");
                else
                    htmlWriter.Write(" and a Dry Matter Digesibility estimated from N%");

                htmlWriter.Write("</div>");
                if (StartingAmount > 0)
                {
                    htmlWriter.Write("<div class=\"activityentry\">");
                    htmlWriter.Write("Simulation starts with <span class=\"setvalue\">" + this.StartingAmount.ToString("#,##0.##") + "</span> kg");
                    htmlWriter.Write("</div>");
                }
                return htmlWriter.ToString();
            }
        }

        /// <inheritdoc/>
        public override string ModelSummaryInnerOpeningTags()
        {
            return "";
        }
        #endregion

    }
}