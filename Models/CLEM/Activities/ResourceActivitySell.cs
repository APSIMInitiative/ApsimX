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
    [HelpUri(@"Content/Features/activities/All resources/SellResource.htm")]
    [Version(1, 0, 2, "Automatically handles transactions with Marketplace if present")]
    [Version(1, 0, 1, "")]
    public class ResourceActivitySell: CLEMActivityBase
    {
        /// <summary>
        /// Bank account to use
        /// </summary>
        [Description("Bank account to use")]
        [Models.Core.Display(Type = DisplayType.CLEMResourceName, CLEMResourceNameResourceGroups = new Type[] { typeof(Finance) })]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name of account to use required")]
        public string AccountName { get; set; }

        /// <summary>
        /// Resource type to sell
        /// </summary>
        [Description("Resource to sell")]
        [Models.Core.Display(Type = DisplayType.CLEMResourceName, CLEMResourceNameResourceGroups = new Type[] { typeof(AnimalFoodStore), typeof(HumanFoodStore), typeof(Equipment), typeof(GreenhouseGases), typeof(OtherAnimals), typeof(ProductStore), typeof(WaterStore) })]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name of resource type required")]
        public string ResourceTypeName { get; set; }

        /// <summary>
        /// Amount reserved from sale
        /// </summary>
         [Description("Amount reserved from sale")]
        [Required, GreaterThanEqualValue(0)]
        public double AmountReserved { get; set; }

        /// <summary>
        /// Store finance type to use
        /// </summary>
        private FinanceType bankAccount;

        /// <summary>
        /// Store type to use
        /// </summary>
        private IResourceType resourceToSell;

        /// <summary>
        /// Store type to place resource within market if present
        /// </summary>
        private IResourceType resourceToPlace;

        private ResourcePricing price;
        private double unitsAvailable;
        private FinanceType marketBank;

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
            Market market = FindMarket();
            // find a suitable store to place resource
            if(market != null)
            {
                marketBank = market.BankAccount;
                resourceToPlace = market.Resources.GetResourceItem(this, ResourceTypeName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as IResourceType;
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
                double amountForSale = resourceToSell.Amount - AmountReserved;
                double units = amountForSale / price.PacketSize;
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
        public override double GetDaysLabourRequired(LabourRequirement requirement)
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
            return daysNeeded;
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
                    Reason = "Sell " + (resourceToSell as Model).Name
                };
                resourceToSell.Remove(purchaseRequest);

                // transfer money earned
                if (bankAccount != null)
                {
                    bankAccount.Add(units * price.PricePerPacket, this, "Sales");
                    if (bankAccount.EquivalentMarketStore != null)
                    {
                        purchaseRequest.Required = units * price.PricePerPacket;
                        purchaseRequest.Reason = "Sales to " + (resourceToSell as Model).Name;
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

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "";
            html += "\n<div class=\"activityentry\">Sell ";
            if (ResourceTypeName == null || ResourceTypeName == "")
            {
                html += "<span class=\"errorlink\">[RESOURCE NOT SET]</span>";
            }
            else
            {
                html += "<span class=\"resourcelink\">" + ResourceTypeName + "</span>";
            }
            if(AmountReserved > 0)
            {
                html += " with <span class=\"resourcelink\">" + AmountReserved.ToString("#,##0") + "</span> reserved in the store";
            }
            if (AccountName == null || AccountName == "")
            {
                html += " with sales placed in <span class=\"errorlink\">[ACCOUNT NOT SET]</span>";
            }
            else
            {
                html += " with sales placed in <span class=\"resourcelink\">" + AccountName + "</span>";
            }
            html += "</div>";

            return html;
        }

    }
}
