using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;

namespace Models.CLEM.Activities
{
    /// <summary>
    /// Activity to buy resources
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Perform the purchase of a specified resource")]
    [HelpUri(@"Content/Features/Activities/All resources/BuyResource.htm")]
    [Version(1, 0, 2, "Automatically handles transactions with Marketplace if present")]
    [Version(1, 0, 1, "")]
    public class ResourceActivityBuy : CLEMActivityBase, IHandlesActivityCompanionModels
    {
        private double unitsToDo;
        private double unitsToSkip;
        private ResourcePricing price;
        private FinanceType bankAccount;
        private IResourceType resourceToBuy;
        private ActivityFee fee;
        private ResourceRequest marketRequest = null;

        /// <summary>
        /// Bank account to use
        /// </summary>
        [Description("Bank account to use")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { typeof(Finance) } })]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name of account to use required")]
        public string AccountName { get; set; }

        /// <summary>
        /// Resource type to buy
        /// </summary>
        [Description("Resource to buy")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { typeof(AnimalFoodStore), typeof(HumanFoodStore), typeof(Equipment), typeof(GreenhouseGases), typeof(HumanFoodStore), typeof(OtherAnimals), typeof(ProductStore), typeof(WaterStore) } })]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name of resource type required")]
        public string ResourceTypeName { get; set; }

        /// <summary>
        /// Units to purchase
        /// </summary>
        [Description("Number of packets")]
        [Required, GreaterThanEqualValue(0)]
        public double Units { get; set; }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            // get bank account object to use
            bankAccount = Resources.FindResourceType<Finance, FinanceType>(this, AccountName, OnMissingResourceActionTypes.ReportWarning, OnMissingResourceActionTypes.ReportErrorAndStop);
            // get resource type to buy
            resourceToBuy = Resources.FindResourceType<ResourceBaseWithTransactions, IResourceType>(this, ResourceTypeName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop);

            // get pricing
            if((resourceToBuy as CLEMResourceTypeBase).MarketStoreExists)
                if ((resourceToBuy as CLEMResourceTypeBase).EquivalentMarketStore.PricingExists(PurchaseOrSalePricingStyleType.Sale))
                    price = (resourceToBuy as CLEMResourceTypeBase).EquivalentMarketStore.Price(PurchaseOrSalePricingStyleType.Sale);

            // no market price found... look in local resources and allow 0 price if not found
            if(price is null)
                price = resourceToBuy.Price(PurchaseOrSalePricingStyleType.Purchase);

            // if there's a bank account and no suitable fee add an ActivityFee
            if(bankAccount == null)
            {
                var activityFee = FindAllChildren<ActivityFee>().Where(a => a.Measure == "per packet").FirstOrDefault();
                if (activityFee is null)
                {
                    fee = new ActivityFee()
                    {
                        Name = "PurchaseFee",
                        TransactionCategory = $"{TransactionCategory}.Fee",
                        Amount = price.PricePerPacket,
                        Parent = this,
                        BankAccountName = AccountName,
                        Measure = "per packet",
                        UniqueID = ActivitiesHolder.AddToGuID(UniqueID, 1)
                    };
                    Children.Insert(0, fee);
                }
            }
        }

        /// <inheritdoc/>
        public override LabelsForCompanionModels DefineCompanionModelLabels(string type)
        {
            switch (type)
            {
                case "LabourRequirement":
                case "ActivityFee":
                    return new LabelsForCompanionModels(
                        identifiers: new List<string>(),
                        measures: new List<string>() {
                            "fixed",
                            "per packet",
                            "perchase value"
                        }
                        );
                default:
                    return new LabelsForCompanionModels();
            }
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> RequestResourcesForTimestep(double argument = 0)
        {
            unitsToSkip = 0;
            List<ResourceRequest> requests = new List<ResourceRequest>();

            // calculate units
            unitsToDo = Units;
            if (price!=null && price.UseWholePackets)
                unitsToDo = Math.Truncate(unitsToDo);

            // if market then ensure then try and take request from market to determine shortfalls
            if(unitsToDo > 0)
            {
                if ((resourceToBuy as CLEMResourceTypeBase).MarketStoreExists)
                {
                    CLEMResourceTypeBase mkt = (resourceToBuy as CLEMResourceTypeBase).EquivalentMarketStore;
                    marketRequest = new ResourceRequest()
                    {
                        AllowTransmutation = true,
                        Required = unitsToDo * price.PacketSize * this.FarmMultiplier,
                        Resource = mkt as IResourceType,
                        ResourceType = mkt.Parent.GetType(),
                        ResourceTypeName = (mkt as IModel).Name,
                        Category = "Purchase " + (resourceToBuy as Model).Name,
                        ActivityModel = this
                    };
                    requests.Add(marketRequest);
                }
            }

            // provide updated measure for companion models
            foreach (var valueToSupply in valuesForCompanionModels.ToList())
            {
                switch (valueToSupply.Key.unit)
                {
                    case "fixed":
                        valuesForCompanionModels[valueToSupply.Key] = 1;
                        break;
                    case "purchase value":
                        valuesForCompanionModels[valueToSupply.Key] = unitsToDo * price.PacketSize;
                        break;
                    case "per packet":
                        valuesForCompanionModels[valueToSupply.Key] = unitsToDo;
                        break;
                    default:
                        throw new NotImplementedException(UnknownUnitsErrorText(this, valueToSupply.Key));
                }
            }

            return requests;
        }

        /// <inheritdoc/>
        protected override void AdjustResourcesForTimestep()
        {
            // check finance and labour reduction
            IEnumerable<ResourceRequest> shortfalls = MinimumShortfallProportion();
            if (shortfalls.Any())
            {
                var unitShort = shortfalls.FirstOrDefault();
                unitsToSkip = Convert.ToInt32(unitsToDo * (1 - unitShort.Available / unitShort.Required));
                if (unitShort.Available == 0)
                {
                    Status = ActivityStatus.Warning;
                    AddStatusMessage("Resource shortfall prevented any action");
                }

                if (marketRequest != null)
                    marketRequest.Required *= (1 - unitShort.Available / unitShort.Required);
            }
        }

        /// <inheritdoc/>
        public override void PerformTasksForTimestep(double argument = 0)
        {
            if (unitsToDo > 0)
            {
                resourceToBuy.Add((unitsToDo - unitsToSkip) * price.PacketSize, this, null, TransactionCategory);
                SetStatusSuccessOrPartial(unitsToSkip > 0);
            }
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write($"\r\n<div class=\"activityentry\">Buy {CLEMModel.DisplaySummaryValueSnippet(Units, warnZero:true)} ");
                htmlWriter.Write(" packets of ");
                htmlWriter.Write(CLEMModel.DisplaySummaryValueSnippet(ResourceTypeName, "Resource not set", HTMLSummaryStyle.Resource));
                htmlWriter.Write(" using ");
                htmlWriter.Write(CLEMModel.DisplaySummaryValueSnippet(AccountName, "Account not set", HTMLSummaryStyle.Resource));
                htmlWriter.Write("</div>");
                return htmlWriter.ToString(); 
            }
        } 
        #endregion

    }
}
