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
    public class AnimalFoodStoreType : CLEMResourceTypeBase, IResourceWithTransactionType, IFeed, IResourceType
    {
        private double amount { get { return roundedAmount; } set { roundedAmount = Math.Round(value, 9); } }
        private double roundedAmount;

        /// <inheritdoc/>
        public string Units { get; private set; } = "kg";

        /// <inheritdoc/>
        [Required]
        [Description("Broad type of feed")]
        public FeedType TypeOfFeed { get; set; }

        /// <inheritdoc/>
        [System.ComponentModel.DefaultValueAttribute(18.4)]
        [Required, GreaterThanValue(0)]
        [Description("Gross energy content")]
        [Units("MJ/kg digestible DM")]
        public double EnergyContent { get; set; }

        /// <inheritdoc/>
        [System.ComponentModel.DefaultValueAttribute(0)]
        [Description("Fat content (%)")]
        [Required, Percentage, GreaterThanEqualValue(0)]
        public double FatContent { get; set; }

        /// <inheritdoc/>
        [Description("Dry Matter Digestibility (%)")]
        [Required, Percentage, GreaterThanValue(0)]
        public double DryMatterDigestibility { get; set; }

        /// <inheritdoc/>
        [Description("Nitrogen content (%)")]
        [Required, Percentage, GreaterThanEqualValue(0)]
        public double NitrogenContent { get; set; }

        /// <inheritdoc/>
        [Description("Degradable protein content (g/g DM)")]
        [Required, Percentage, GreaterThanEqualValue(0)]
        public double RumenDegradableProteinContent { get; set; }

        /// <inheritdoc/>
        [Description("Acid detergent insoluable protein")]
        [Required, Percentage, GreaterThanEqualValue(0)]
        public double ADIP { get; set; }

        /// <inheritdoc/>
        [Description("Nitrogen to Crude protein factor")]
        [Required, Percentage, GreaterThanEqualValue(0)]
        public double NitrogenToCrudeProteinFactor { get; set; }

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

        /// <summary>
        /// A packet to pass the current food quality to activities. Allows for mixing of feed into store
        /// </summary>
        public FoodResourcePacket CurrentStoreDetails { get; set; }

        /// <summary>
        /// A packet to store the quality details of this food
        /// </summary>
        public FoodResourcePacket StoreDetails { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public AnimalFoodStoreType()
        {
            base.SetDefaults();
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

            // initialise the current state and details of this store
            CurrentStoreDetails = new FoodResourcePacket()
            {
                EnergyContent = EnergyContent,
                DryMatterDigestibility = DryMatterDigestibility,
                FatContent= FatContent,
                NitrogenContent = NitrogenContent
            };
            StoreDetails = new FoodResourcePacket()
            {
                EnergyContent = EnergyContent,
                DryMatterDigestibility = DryMatterDigestibility,
                FatContent = FatContent,
                NitrogenContent = NitrogenContent
            };
        }

        #region Transactions

        /// <summary>
        /// Add to food store
        /// </summary>
        /// <param name="resourceAmount">Object to add. This object can be double or contain additional information (e.g. Nitrogen) of food being added</param>
        /// <param name="activity">Name of activity adding resource</param>
        /// <param name="relatesToResource">Resource the transasction relates to</param>
        /// <param name="category">Transaction category</param>
        public new void Add(object resourceAmount, CLEMModel activity, string relatesToResource, string category)
        {
            FoodResourcePacket foodPacket;
            double addAmount;
            switch (resourceAmount.GetType().ToString())
            {
                case "System.Double":
                    foodPacket = StoreDetails;
                    addAmount = (double)resourceAmount;
                    break;
                case "Models.CLEM.Resources.FoodResourcePacket":
                    foodPacket = resourceAmount as FoodResourcePacket;
                    addAmount = foodPacket.Amount;
                    break;
                default:
                    throw new Exception($"ResourceAmount object of type {resourceAmount.GetType()} is not supported Add method in {this.Name}");
            }

            if (addAmount > 0)
            {
                // update quality details to allow mixed feed inputs
                CurrentStoreDetails.NitrogenContent = ((CurrentStoreDetails.NitrogenContent * Amount) + (foodPacket.NitrogenContent * addAmount)) / (Amount + addAmount);
                CurrentStoreDetails.DryMatterDigestibility = ((CurrentStoreDetails.DryMatterDigestibility * Amount) + (foodPacket.DryMatterDigestibility * addAmount)) / (Amount + addAmount);
                CurrentStoreDetails.FatContent = ((CurrentStoreDetails.FatContent * Amount) + (foodPacket.FatContent * addAmount)) / (Amount + addAmount);
                CurrentStoreDetails.EnergyContent = ((CurrentStoreDetails.EnergyContent * Amount) + (foodPacket.EnergyContent * addAmount)) / (Amount + addAmount);

                this.amount += addAmount;

                ReportTransaction(TransactionType.Gain, addAmount, activity, relatesToResource, category, this);
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

                //FoodResourcePacket additionalDetails = new FoodResourcePacket
                //{
                //    DMD = this.DMD,
                //    PercentN = this.CurrentStoreNitrogen
                //};
                request.AdditionalDetails = CurrentStoreDetails;

                request.Provided = amountRemoved;

                // send to market if needed
                if (request.MarketTransactionMultiplier > 0 && EquivalentMarketStore != null)
                {
                    FoodResourcePacket marketDetails = CurrentStoreDetails.Clone(amountRemoved * request.MarketTransactionMultiplier);
                    //additionalDetails.Amount = amountRemoved * request.MarketTransactionMultiplier;
                    (EquivalentMarketStore as AnimalFoodStoreType).Add(marketDetails, request.ActivityModel, request.ResourceTypeName, "Farm sales");
                }

                ReportTransaction(TransactionType.Loss, amountRemoved, request.ActivityModel, request.RelatesToResource, request.Category, this);
            }
            return;
        }

        ///// <inheritdoc/>
        //public new void Set(double newValue)
        //{
        //    this.amount = newValue;
        //}

        ///// <inheritdoc/>
        //public event EventHandler TransactionOccurred;

        ///// <inheritdoc/>
        //protected virtual void OnTransactionOccurred(EventArgs e)
        //{
        //    TransactionOccurred?.Invoke(this, e);
        //}

        ///// <inheritdoc/>
        //[JsonIgnore]
        //public ResourceTransaction LastTransaction { get; set; }

        #endregion

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("<div class=\"activityentry\">");
                htmlWriter.Write($"This food has a nitrogen content of <span class=\"setvalue\">{NitrogenContent.ToString("0.###")}%</span>");
                if (DryMatterDigestibility > 0)
                    htmlWriter.Write($" and a Dry Matter Digesibility of <span class=\"setvalue\">{DryMatterDigestibility.ToString("0.###")}%</span>");
                else
                    htmlWriter.Write(" and a Dry Matter Digesibility estimated from N%");

                htmlWriter.Write("</div>");
                if (StartingAmount > 0)
                {
                    htmlWriter.Write("<div class=\"activityentry\">");
                    htmlWriter.Write($"Simulation starts with <span class=\"setvalue\">{StartingAmount.ToString("#,##0.##")}</span> kg");
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