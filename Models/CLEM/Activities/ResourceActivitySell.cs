using Models.Core;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Models.Core.Attributes;
using System.IO;

namespace Models.CLEM.Activities
{
    /// <summary>
    /// Activity to price and sell resources
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This activity manages the sale of a specified resource.")]
    [HelpUri(@"Content/Features/Activities/All resources/SellResource.htm")]
    [Version(1, 0, 3, "Added Proportion of last gain as selling style. Allows you to sell a proportion of the harvest")]
    [Version(1, 0, 2, "Automatically handles transactions with Marketplace if present")]
    [Version(1, 0, 1, "")]
    public class ResourceActivitySell: CLEMActivityBase, IValidatableObject
    {
        /// <summary>
        /// Bank account to use
        /// </summary>
        [Description("Bank account to use")]
        [Models.Core.Display(Type = DisplayType.CLEMResource, CLEMResourceGroups = new Type[] { typeof(Finance) })]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name of account to use required")]
        public string AccountName { get; set; }

        /// <summary>
        /// Resource type to sell
        /// </summary>
        [Description("Resource to sell")]
        [Models.Core.Display(Type = DisplayType.CLEMResource, CLEMResourceGroups = new Type[] { typeof(AnimalFoodStore), typeof(HumanFoodStore), typeof(Equipment), typeof(GreenhouseGases), typeof(OtherAnimals), typeof(ProductStore), typeof(WaterStore) })]
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

        private FinanceType bankAccount;
        private IResourceType resourceToSell;
        private IResourceType resourceToPlace;
        private ResourcePricing price;
        private double unitsAvailable;

        #region validation
        /// <summary>
        /// Validate model
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            // check that this activity has a parent of type CropActivityManageProduct

            switch (SellStyle)
            {
                case ResourceSellStyle.ProportionOfStore:
                case ResourceSellStyle.ProportionOfLastGain:
                case ResourceSellStyle.ReserveProportion:
                    if (Value > 1)
                    {
                        string[] memberNames = new string[] { "Selling style" };
                        results.Add(new ValidationResult("The specified selling style expects a value between 0 and 1", memberNames));
                    }
                    break;
                default:
                    break;
            }
            return results;
        }

        #endregion

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            // get bank account object to use
            bankAccount = Resources.GetResourceItem(this, AccountName, OnMissingResourceActionTypes.ReportWarning, OnMissingResourceActionTypes.ReportErrorAndStop) as FinanceType;
            // get resource type to sell
            resourceToSell = Resources.GetResourceItem(this, ResourceTypeName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as IResourceType;
            // find market if present
            Market market = Resources.FoundMarket;
            // find a suitable store to place resource
            if(market != null)
            {
                resourceToPlace = market.Resources.LinkToMarketResourceType(resourceToSell as CLEMResourceTypeBase) as IResourceType;
            }
            if(resourceToPlace != null)
            {
                price = resourceToPlace.Price(PurchaseOrSalePricingStyleType.Purchase);
            }
            if(price is null && resourceToSell.Price(PurchaseOrSalePricingStyleType.Sale)  != null)
            {
                price = resourceToSell.Price(PurchaseOrSalePricingStyleType.Sale);
            }
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
                        amount = resourceToSell.Amount - Value;
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
                {
                    units = Math.Truncate(units);
                }
                return units;
            }
        }

        /// <summary>
        /// Method to determine resources required for this activity in the current month
        /// </summary>
        /// <returns>List of required resource requests</returns>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            unitsAvailable = unitsAvailableForSale;
            return null;
        }

        /// <summary>
        /// Determines how much labour is required from this activity based on the requirement provided
        /// </summary>
        /// <param name="requirement">The details of how labour are to be provided</param>
        /// <returns></returns>
        public override GetDaysLabourRequiredReturnArgs GetDaysLabourRequired(LabourRequirement requirement)
        {
            double daysNeeded;
            switch (requirement.UnitType)
            {
                case LabourUnitType.Fixed:
                    daysNeeded = requirement.LabourPerUnit;
                    break;
                case LabourUnitType.perUnit:
                    daysNeeded = unitsAvailable * requirement.LabourPerUnit;
                    break;
                default:
                    throw new Exception(String.Format("LabourUnitType {0} is not supported for {1} in {2}", requirement.UnitType, requirement.Name, this.Name));
            }
            return new GetDaysLabourRequiredReturnArgs(daysNeeded, "Sell", (resourceToSell as CLEMModel).NameWithParent);
        }

        /// <summary>
        /// The method allows the activity to adjust resources requested based on shortfalls (e.g. labour) before they are taken from the pools
        /// </summary>
        public override void AdjustResourcesNeededForActivity()
        {
            // adjust resources sold based on labour shortfall
            double labourLimit = this.LabourLimitProportion;
            unitsAvailable *= labourLimit;
            return;
        }


        /// <summary>
        /// Method used to perform activity if it can occur as soon as resources are available.
        /// </summary>
        public override void DoActivity()
        {
            Status = ActivityStatus.NotNeeded;
            double labourlimit = this.LabourLimitProportion;
            double units = 0;
            if (labourlimit == 1 || this.OnPartialResourcesAvailableAction == OnPartialResourcesAvailableActionTypes.UseResourcesAvailable)
            {
                units = unitsAvailableForSale * labourlimit;
                if (price.UseWholePackets)
                {
                    units = Math.Truncate(units);
                }
            }

            if(units>0)
            {
                // remove resource
                ResourceRequest purchaseRequest = new ResourceRequest
                {
                    ActivityModel = this,
                    Required = units * price.PacketSize,
                    AllowTransmutation = true,
                    Category = "Sell",
                    RelatesToResource = (resourceToSell as CLEMModel).NameWithParent
                };
                resourceToSell.Remove(purchaseRequest);

                // transfer money earned
                if (bankAccount != null)
                {
                    bankAccount.Add(units * price.PricePerPacket, this, (resourceToSell as CLEMModel).NameWithParent, "Sales");
                    if (bankAccount.EquivalentMarketStore != null)
                    {
                        purchaseRequest.Required = units * price.PricePerPacket;
                        purchaseRequest.Category = "Sales";
                        purchaseRequest.RelatesToResource = (resourceToSell as CLEMModel).NameWithParent;
                        (bankAccount.EquivalentMarketStore as FinanceType).Remove(purchaseRequest);
                    }
                }

                SetStatusSuccess();
            }
        }

        /// <summary>
        /// Method to determine resources required for initialisation of this activity
        /// </summary>
        /// <returns></returns>
        public override List<ResourceRequest> GetResourcesNeededForinitialisation()
        {
            return null;
        }

        /// <summary>
        /// Resource shortfall event handler
        /// </summary>
        public override event EventHandler ResourceShortfallOccurred;

        /// <summary>
        /// Shortfall occurred 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnShortfallOccurred(EventArgs e)
        {
            ResourceShortfallOccurred?.Invoke(this, e);
        }

        /// <summary>
        /// Resource shortfall occured event handler
        /// </summary>
        public override event EventHandler ActivityPerformed;

        /// <summary>
        /// Shortfall occurred 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnActivityPerformed(EventArgs e)
        {
            ActivityPerformed?.Invoke(this, e);
        }

        #region descriptive summary 

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"activityentry\">Sell ");
                switch (SellStyle)
                {
                    case ResourceSellStyle.SpecifiedAmount:
                        htmlWriter.Write("<span class=\"resourcelink\">" + Value.ToString("#,##0") + "</span> of ");
                        break;
                    case ResourceSellStyle.ProportionOfStore:
                        htmlWriter.Write("<span class=\"resourcelink\">" + Value.ToString("#0%") + "</span> percent of ");
                        break;
                    case ResourceSellStyle.ProportionOfLastGain:
                        htmlWriter.Write("<span class=\"resourcelink\">" + Value.ToString("#0%") + "</span> percent of the last gain transaction recorded for ");
                        break;
                    case ResourceSellStyle.ReserveAmount:
                        htmlWriter.Write("all but <span class=\"resourcelink\">" + Value.ToString("#,##0") + "</span> as reserve of ");
                        break;
                    case ResourceSellStyle.ReserveProportion:
                        htmlWriter.Write("all but leaving <span class=\"resourcelink\">" + Value.ToString("##0%") + "</span> percent of store as reserve of ");
                        break;
                    default:
                        break;
                }

                if (ResourceTypeName == null || ResourceTypeName == "")
                {
                    htmlWriter.Write("<span class=\"errorlink\">[RESOURCE NOT SET]</span>");
                }
                else
                {
                    htmlWriter.Write("<span class=\"resourcelink\">" + ResourceTypeName + "</span>");
                }
                htmlWriter.Write(" with sales placed in ");
                if (AccountName == null || AccountName == "")
                {
                    htmlWriter.Write(" <span class=\"errorlink\">[ACCOUNT NOT SET]</span>");
                }
                else
                {
                    htmlWriter.Write(" <span class=\"resourcelink\">" + AccountName + "</span>");
                }
                htmlWriter.Write("</div>");

                return htmlWriter.ToString(); 
            }
        }

        #endregion
    }
}
