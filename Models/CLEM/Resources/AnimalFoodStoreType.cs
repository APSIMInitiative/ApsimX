using Microsoft.CodeAnalysis.CSharp.Syntax;
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
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    [ModelAssociations(associatedModels: new Type[] { typeof(AnimalFoodStore) }, associationStyles: new ModelAssociationStyle[] { ModelAssociationStyle.Parent })]
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
        [Units("MJ/kg DM")]
        public double GrossEnergyContent { get; set; }

        /// <inheritdoc/>
        [Required, GreaterThanValue(0)]
        [Description("Metabolisable energy content")]
        [Units("MJ/kg DM")]
        public double MetabolisableEnergyContent { get; set; }

        /// <inheritdoc/>
        [System.ComponentModel.DefaultValueAttribute(0)]
        [Description("Fat content")]
        [Required, Percentage, GreaterThanEqualValue(0)]
        [Units("%")]
        public double FatContent { get; set; }

        /// <inheritdoc/>
        [Description("Dry Matter Digestibility")]
        [Required, Percentage, GreaterThanValue(0)]
        [Units("%")]
        public double DryMatterDigestibility { get; set; }

        /// <summary>
        /// Style of providing the crude protein content
        /// </summary>
        [Description("Style of providing crude protein")]
        [Required, Percentage, GreaterThanEqualValue(0)]
        public CrudeProteinContentStyle CPContentStyle { get; set; } = CrudeProteinContentStyle.SpecifyCrudeProteinContent;

        /// <inheritdoc/>
        [Description("Nitrogen content"),]
        [Core.Display(VisibleCallback = "NitrogenPropertiesVisible")]
        [Required, Percentage, GreaterThanEqualValue(0)]
        [Units("%")]
        public double UserNitrogenContent { get; set; }

        /// <summary>
        /// Crude protein content (%)
        /// </summary>
        [Description("Crude protein content")]
        [Core.Display(VisibleCallback = "CrudeProteinPropertiesVisible")]
        [Required, Percentage, GreaterThanEqualValue(0)]
        [Units("%")]
        public double UserCrudeProteinContent { get; set; }

        /// <summary>
        /// Crude protein content
        /// </summary>
        public double CrudeProteinContent { get; set; }

        /// <summary>
        /// Nitrogen content
        /// </summary>
        public double NitrogenContent { get; set; }

        /// <inheritdoc/>
        [Description("Degradable protein content")]
        [Required, GreaterThanEqualValue(0), Proportion]
        [Units("g/g CP")]
        public double RumenDegradableProteinContent { get; set; } = 0.7;

        /// <inheritdoc/>
        [Description("Acid detergent insoluable protein")]
        [Required, GreaterThanEqualValue(0), Proportion]
        [Units("g/g Protein")]
        public double AcidDetergentInsoluableProtein { get; set; }

        /// <summary>
        /// Starting Amount (kg)
        /// </summary>
        [Description("Starting Amount (kg)")]
        [Required, GreaterThanEqualValue(0)]
        [Units("kg")]
        public double StartingAmount { get; set; }

        /// <summary>
        /// Amount currently available (kg dry)
        /// </summary>
        [JsonIgnore]
        public double Amount { get { return amount; } set { return; } }

        /// <summary>
        /// A packet to pass the current food quality to activities. Allows for mixing of feed into store
        /// </summary>
        [JsonIgnore]
        public FoodResourcePacket CurrentStoreDetails { get; set; }

        /// <summary>
        /// A packet to store the quality details of this food
        /// </summary>
        [JsonIgnore]
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

        /// <summary>
        /// Determine whether N% or CP% are displayed to user based on CP style.
        /// </summary>
        /// <returns>Bool indicating that CP content is needed</returns>
        public bool CrudeProteinPropertiesVisible()
        {
            return CPContentStyle == CrudeProteinContentStyle.SpecifyCrudeProteinContent;
        }

        /// <summary>
        /// Determine whether N% or CP% are displayed to user based on CP style.
        /// </summary>
        /// <returns>Bool indicating that CP content is needed</returns>
        public bool NitrogenPropertiesVisible()
        {
            return (CrudeProteinPropertiesVisible() == false);
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseResource")]
        private void OnCLEMInitialiseResource(object sender, EventArgs e)
        {
            if (CPContentStyle == CrudeProteinContentStyle.EstimateFromNitrogenContent)
            {
                NitrogenContent = UserNitrogenContent;
                CrudeProteinContent = UserNitrogenContent * FoodResourcePacket.FeedProteinToNitrogenFactor;
            }
            else
            {
                NitrogenContent = UserCrudeProteinContent / FoodResourcePacket.FeedProteinToNitrogenFactor;
                CrudeProteinContent = UserCrudeProteinContent;
            }

            // initialise the current state and details of this store
            CurrentStoreDetails = new FoodResourcePacket(this);
            StoreDetails = new FoodResourcePacket(this);

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
                CurrentStoreDetails.MetabolisableEnergyContent = ((CurrentStoreDetails.MetabolisableEnergyContent * Amount) + (foodPacket.MetabolisableEnergyContent * addAmount)) / (Amount + addAmount);

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

        #endregion

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using StringWriter htmlWriter = new StringWriter();
            htmlWriter.Write("<div class=\"activityentry\">");
            htmlWriter.Write($"This food has a nitrogen content of <span class=\"setvalue\">{NitrogenContent:0.###}%</span>");
            if (DryMatterDigestibility > 0)
                htmlWriter.Write($" and a Dry Matter Digesibility of <span class=\"setvalue\">{DryMatterDigestibility:0.###}%</span>");
            else
                htmlWriter.Write(" and a Dry Matter Digesibility estimated from N%");

            htmlWriter.Write("</div>");
            if (StartingAmount > 0)
            {
                htmlWriter.Write("<div class=\"activityentry\">");
                htmlWriter.Write($"Simulation starts with <span class=\"setvalue\">{StartingAmount:#,##0.##}</span> kg");
                htmlWriter.Write("</div>");
            }
            return htmlWriter.ToString();
        }

        /// <inheritdoc/>
        public override string ModelSummaryInnerOpeningTags()
        {
            return "";
        }
        #endregion

    }
}