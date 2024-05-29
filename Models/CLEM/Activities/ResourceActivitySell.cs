using Models.Core;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Models.Core.Attributes;
using System.IO;
using System.Linq;
using APSIM.Shared.Utilities;
using static Models.Core.ScriptCompiler;

namespace Models.CLEM.Activities
{
    /// <summary>
    /// Activity to price and sell resources
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Manages the sale of a specified resource")]
    [HelpUri(@"Content/Features/Activities/All resources/SellResource.htm")]
    [Version(1, 0, 3, "Added Proportion of last gain as selling style. Allows you to sell a proportion of the harvest")]
    [Version(1, 0, 2, "Automatically handles transactions with Marketplace if present")]
    [Version(1, 0, 1, "")]
    public class ResourceActivitySell: CLEMActivityBase, IValidatableObject, IHandlesActivityCompanionModels
    {
        private double unitsToDo;
        private double unitsToSkip;
        private FinanceType bankAccount;
        private IResourceType resourceToSell;
        private IResourceType resourceToPlace;
        private ResourcePricing price;

        /// <summary>
        /// Bank account to use
        /// </summary>
        [Description("Bank account to use")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { "No finance required", typeof(Finance) } })]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name of account to use required")]
        [System.ComponentModel.DefaultValueAttribute("No finance required")]
        public string AccountName { get; set; }

        /// <summary>
        /// Resource type to sell
        /// </summary>
        [Description("Resource to sell")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { typeof(AnimalFoodStore), typeof(HumanFoodStore), typeof(Equipment), typeof(GreenhouseGases), typeof(OtherAnimals), typeof(ProductStore), typeof(WaterStore) } })]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name of resource type required")]
        public string ResourceTypeName { get; set; }

        /// <summary>
        /// Resource sell style to use
        /// </summary>
        [Description("Selling style")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Selling style required")]
        public ResourceSellStyle SellStyle { get; set; }

        /// <summary>
        /// Value based on selling style
        /// </summary>
        [Description("Value for selling style")]
        [Required, GreaterThanEqualValue(0)]
        public double Value { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ResourceActivitySell()
        {
            this.SetDefaults();
        }

        /// <inheritdoc/>
        public override LabelsForCompanionModels DefineCompanionModelLabels(string type)
        {
            switch (type)
            {
                case "ActivityFee":
                case "LabourRequirement":
                    return new LabelsForCompanionModels(
                        identifiers: new List<string>(),
                        measures: new List<string>() {
                            "fixed",
                            "per packet",
                            "sale value"
                        }
                        );
                default:
                    return new LabelsForCompanionModels();
            }
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            // get bank account object to use
            if(AccountName != "No finance required")
                bankAccount = Resources.FindResourceType<Finance, FinanceType>(this, AccountName, OnMissingResourceActionTypes.ReportWarning, OnMissingResourceActionTypes.ReportErrorAndStop);
            
            // get resource type to sell
            resourceToSell = Resources.FindResourceType<ResourceBaseWithTransactions, IResourceType>(this, ResourceTypeName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop);
            // find market if present
            Market market = Resources.FoundMarket;
            // find a suitable store to place resource
            if(market != null)
                resourceToPlace = market.Resources.LinkToMarketResourceType(resourceToSell as CLEMResourceTypeBase) as IResourceType;

            if(resourceToPlace != null)
                price = resourceToPlace.Price(PurchaseOrSalePricingStyleType.Purchase);

            if(price is null && resourceToSell.Price(PurchaseOrSalePricingStyleType.Sale) != null)
                price = resourceToSell.Price(PurchaseOrSalePricingStyleType.Sale);
        }

        /// <summary>
        /// Gets the number of units available for sale
        /// </summary>
        private double unitsAvailableForSale
        {
            get
            {
                double amount = 0;
                switch (SellStyle)
                {
                    case ResourceSellStyle.SpecifiedAmount:
                        amount = Value;
                        break;
                    case ResourceSellStyle.ProportionOfStore:
                        amount = resourceToSell.Amount * Value;
                        break;
                    case ResourceSellStyle.ProportionOfLastGain:
                        amount = resourceToSell.LastGain * Value;
                        break;
                    case ResourceSellStyle.ReserveAmount:
                        amount = Math.Max(0,resourceToSell.Amount - Value);
                        break;
                    case ResourceSellStyle.ReserveProportion:
                        amount = resourceToSell.Amount * (1 - Value);
                        break;
                    default:
                        break;
                }
                amount = Math.Max(0, amount);
                double units = amount / price.PacketSize;
                if(price.UseWholePackets)
                    units = Math.Truncate(units);

                return units;
            }
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> RequestResourcesForTimestep(double argument = 0)
        {
            unitsToSkip = 0;
            unitsToDo = unitsAvailableForSale;
            if (price.UseWholePackets)
                unitsToDo = Math.Truncate(unitsToDo);

            // provide updated measure for companion models
            foreach (var valueToSupply in valuesForCompanionModels)
            {
                switch (valueToSupply.Key.unit)
                {
                    case "fixed":
                        valuesForCompanionModels[valueToSupply.Key] = 1;
                        break;
                    case "per packet":
                        valuesForCompanionModels[valueToSupply.Key] = unitsToDo;
                        break;
                    case "sale value":
                        valuesForCompanionModels[valueToSupply.Key] = unitsToDo * price.PacketSize;
                        break;
                    default:
                        throw new NotImplementedException(UnknownUnitsErrorText(this, valueToSupply.Key));
                }
            }
            return null;
        }

        /// <inheritdoc/>
        protected override void AdjustResourcesForTimestep()
        {
            IEnumerable<ResourceRequest> shortfalls = MinimumShortfallProportion();
            if (shortfalls.Any())
            {
                // find shortfall by identifiers as these may have different influence on outcome
                var unitShort = shortfalls.FirstOrDefault();
                unitsToSkip = Convert.ToInt32(unitsToDo * (1 - unitShort.Available / unitShort.Required));
                if (unitShort.Available == 0)
                {
                    Status = ActivityStatus.Warning;
                    AddStatusMessage("Resource shortfall prevented any action");
                }
            }
        }

        /// <inheritdoc/>
        public override void PerformTasksForTimestep(double argument = 0)
        {
            if(MathUtilities.IsPositive(unitsToDo-unitsToSkip))
            {
                // remove resource
                ResourceRequest purchaseRequest = new()
                {
                    ActivityModel = this,
                    Required = (unitsToDo-unitsToSkip) * price.PacketSize,
                    AllowTransmutation = true,
                    Category = TransactionCategory,
                    RelatesToResource = (resourceToSell as CLEMModel).NameWithParent
                };
                resourceToSell.Remove(purchaseRequest);

                // transfer money earned
                if (bankAccount != null)
                {
                    if(price.PricePerPacket == 0)
                    {
                        string warn = $"No price set [0] for [r={resourceToSell.Name}] at time of transaction for [a={this.Name}]{Environment.NewLine}No financial transactions will occur.{Environment.NewLine}Ensure price is set or resource pricing file contains entries before this transaction or start of simulation.";
                        Warnings.CheckAndWrite(warn, Summary, this, MessageType.Warning);
                    }

                    bankAccount.Add((unitsToDo - unitsToSkip) * price.PricePerPacket, this, (resourceToSell as CLEMModel).NameWithParent, TransactionCategory);
                    if (bankAccount.EquivalentMarketStore != null)
                    {
                        purchaseRequest.Required = (unitsToDo - unitsToSkip) * price.PricePerPacket;
                        purchaseRequest.Category = TransactionCategory;
                        purchaseRequest.RelatesToResource = (resourceToSell as CLEMModel).NameWithParent;
                        (bankAccount.EquivalentMarketStore as FinanceType).Remove(purchaseRequest);
                    }
                }

                SetStatusSuccessOrPartial(MathUtilities.IsPositive(unitsToSkip));
            }
        }

        #region validation
        /// <inheritdoc/>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // check that this activity has a parent of type CropActivityManageProduct
            switch (SellStyle)
            {
                case ResourceSellStyle.ProportionOfStore:
                case ResourceSellStyle.ProportionOfLastGain:
                case ResourceSellStyle.ReserveProportion:
                    if (Value > 1)
                    {
                        yield return new ValidationResult("The specified selling style expects a value between 0 and 1", new string[] { "Selling style" });
                    }
                    break;
                default:
                    break;
            }
        }

        #endregion

        #region descriptive summary 

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using StringWriter htmlWriter = new();
            htmlWriter.Write("\r\n<div class=\"activityentry\">Sell ");
            switch (SellStyle)
            {
                case ResourceSellStyle.SpecifiedAmount:
                    htmlWriter.Write($"<span class=\"resourcelink\">{Value:#,##0}</span> of ");
                    break;
                case ResourceSellStyle.ProportionOfStore:
                    htmlWriter.Write($"<span class=\"resourcelink\">{Value:#0%}</span> percent of ");
                    break;
                case ResourceSellStyle.ProportionOfLastGain:
                    htmlWriter.Write($"<span class=\"resourcelink\">{Value:#0%}</span> percent of the last gain transaction recorded for ");
                    break;
                case ResourceSellStyle.ReserveAmount:
                    htmlWriter.Write($"all but <span class=\"resourcelink\">{Value:#,##0}</span> as reserve of ");
                    break;
                case ResourceSellStyle.ReserveProportion:
                    htmlWriter.Write($"all but leaving <span class=\"resourcelink\">{Value:##0%}</span> percent of store as reserve of ");
                    break;
                default:
                    break;
            }
            htmlWriter.Write(DisplaySummaryValueSnippet(ResourceTypeName, "Resource not set", HTMLSummaryStyle.Resource));
            if (AccountName != "No finance required")
            {
                htmlWriter.Write(" with sales placed in ");
                htmlWriter.Write(DisplaySummaryValueSnippet(AccountName, "Account not set", HTMLSummaryStyle.Resource));
            }
            htmlWriter.Write("</div>");
            return htmlWriter.ToString();
        }

        #endregion
    }
}
