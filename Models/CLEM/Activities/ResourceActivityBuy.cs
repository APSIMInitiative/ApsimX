using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Activities
{
    /// <summary>
    /// Activity to buy resources
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This activity manages the purchase of a specified resource.")]
    [HelpUri(@"Content/Features/Activities/All resources/BuyResource.htm")]
    [Version(1, 0, 2, "Automatically handles transactions with Marketplace if present")]
    [Version(1, 0, 1, "")]
    public class ResourceActivityBuy : CLEMActivityBase
    {
        /// <summary>
        /// Bank account to use
        /// </summary>
        [Description("Bank account to use")]
        [Models.Core.Display(Type = DisplayType.CLEMResourceName, CLEMResourceNameResourceGroups = new Type[] { typeof(Finance) })]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name of account to use required")]
        public string AccountName { get; set; }

        /// <summary>
        /// Resource type to buy
        /// </summary>
        [Description("Resource to buy")]
        [Models.Core.Display(Type = DisplayType.CLEMResourceName, CLEMResourceNameResourceGroups = new Type[] { typeof(AnimalFoodStore), typeof(HumanFoodStore), typeof(Equipment), typeof(GreenhouseGases), typeof(HumanFoodStore), typeof(OtherAnimals), typeof(ProductStore), typeof(WaterStore) })]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name of resource type required")]
        public string ResourceTypeName { get; set; }

        /// <summary>
        /// Units to purchase
        /// </summary>
        [Description("Number of packets")]
        [Required, GreaterThanEqualValue(0)]
        public double Units { get; set; }
        private double units;

        private ResourcePricing price;
        private FinanceType bankAccount;
        private IResourceType resourceToBuy;
        private double unitsCanAfford;

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            // get bank account object to use
            bankAccount = Resources.GetResourceItem(this, AccountName, OnMissingResourceActionTypes.ReportWarning, OnMissingResourceActionTypes.ReportErrorAndStop) as FinanceType;
            // get resource type to buy
            resourceToBuy = Resources.GetResourceItem(this, ResourceTypeName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as IResourceType;

            // get pricing
            if((resourceToBuy as CLEMResourceTypeBase).MarketStoreExists)
            {
                if ((resourceToBuy as CLEMResourceTypeBase).EquivalentMarketStore.PricingExists(PurchaseOrSalePricingStyleType.Sale))
                {
                    price = (resourceToBuy as CLEMResourceTypeBase).EquivalentMarketStore.Price(PurchaseOrSalePricingStyleType.Sale);
                }
            }
            // no market price found... look in local resources and allow 0 price if not found
            if(price is null)
            {
                price = resourceToBuy.Price(PurchaseOrSalePricingStyleType.Purchase);
            }
        }

        /// <summary>
        /// Method to determine resources required for this activity in the current month
        /// </summary>
        /// <returns>List of required resource requests</returns>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            List<ResourceRequest> requests = new List<ResourceRequest>();

            // calculate units
            units = Units;
            if (price!=null && price.UseWholePackets)
            {
                units = Math.Truncate(Units);
            }

            unitsCanAfford = units;
            if (units > 0 & (resourceToBuy as CLEMResourceTypeBase).MarketStoreExists)
            {
                // determine how many units we can afford
                double cost = units * price.PricePerPacket;
                if (cost > bankAccount.FundsAvailable)
                {
                    unitsCanAfford = bankAccount.FundsAvailable / price.PricePerPacket;
                    if(price.UseWholePackets)
                    {
                        unitsCanAfford = Math.Truncate(unitsCanAfford);
                    }
                }

                CLEMResourceTypeBase mkt = (resourceToBuy as CLEMResourceTypeBase).EquivalentMarketStore;

                requests.Add(new ResourceRequest()
                {
                    AllowTransmutation = true,
                    Required = unitsCanAfford * price.PacketSize * this.FarmMultiplier,
                    Resource = mkt as IResourceType,
                    ResourceType = mkt.Parent.GetType(),
                    ResourceTypeName = (mkt as IModel).Name,
                    Reason = "Purchase " + (resourceToBuy as Model).Name,
                    ActivityModel = this
                });
            }
            return requests;
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
                    daysNeeded = units * requirement.LabourPerUnit;
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
            // adjust amount needed by labour shortfall.
            double labprop = this.LabourLimitProportion;

            // get additional reduction based on labour cost shortfall as cost has already been accounted for
            double priceprop = 1;
            if (labprop < 1)
            {
                if(unitsCanAfford < units)
                {
                    priceprop = unitsCanAfford / units;
                }
                if(labprop < priceprop)
                {
                    unitsCanAfford = units * labprop;
                    if(price.UseWholePackets)
                    {
                        unitsCanAfford = Math.Truncate(unitsCanAfford);
                    }
                }
            }

            if (unitsCanAfford > 0 & (resourceToBuy as CLEMResourceTypeBase).MarketStoreExists)
            {
                // find resource entry in market if present and reduce
                ResourceRequest rr = ResourceRequestList.Where(a => a.Resource == (resourceToBuy as CLEMResourceTypeBase).EquivalentMarketStore).FirstOrDefault();
                if(rr.Required != unitsCanAfford * price.PacketSize * this.FarmMultiplier)
                {
                    rr.Required = unitsCanAfford * price.PacketSize * this.FarmMultiplier;
                }
            }
            return;
        }

        /// <summary>
        /// Method used to perform activity if it can occur as soon as resources are available.
        /// </summary>
        public override void DoActivity()
        {
            Status = ActivityStatus.NotNeeded;
            // take local equivalent of market from resource

            double provided = 0;
            if ((resourceToBuy as CLEMResourceTypeBase).MarketStoreExists)
            {
                // find resource entry in market if present and reduce
                ResourceRequest rr = ResourceRequestList.Where(a => a.Resource == (resourceToBuy as CLEMResourceTypeBase).EquivalentMarketStore).FirstOrDefault();
                provided = rr.Provided / this.FarmMultiplier;
            }
            else
            {
                provided = unitsCanAfford * price.PacketSize;
            }

            if (provided > 0)
            {
                resourceToBuy.Add(provided, this, "Purchase " + (resourceToBuy as Model).Name);
                Status = ActivityStatus.Success;
            }

            // make financial transactions
            if (bankAccount != null)
            {
                ResourceRequest payment = new ResourceRequest()
                {
                    AllowTransmutation = false,
                    MarketTransactionMultiplier = this.FarmMultiplier,
                    Required = provided / price.PacketSize * price.PricePerPacket,
                    ResourceType = typeof(Finance),
                    ResourceTypeName = bankAccount.Name,
                    Reason = "Purchase " + (resourceToBuy as Model).Name,
                    ActivityModel = this
                };
                bankAccount.Remove(payment);
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
            html += "\n<div class=\"activityentry\">Buy ";
            if (Units <= 0)
            {
                html += "<span class=\"errorlink\">[VALUE NOT SET]</span>";
            }
            else
            {
                html += "<span class=\"setvalue\">" + Units.ToString("0.###") + "</span>";
            }
            html += " packages of ";
            if (ResourceTypeName == null || ResourceTypeName == "")
            {
                html += "<span class=\"errorlink\">[RESOURCE NOT SET]</span>";
            }
            else
            {
                html += "<span class=\"resourcelink\">" + ResourceTypeName + "</span>";
            }
            if (AccountName == null || AccountName == "")
            {
                html += " using <span class=\"errorlink\">[ACCOUNT NOT SET]</span>";
            }
            else
            {
                html += " using <span class=\"resourcelink\">" + AccountName + "</span>";
            }
            html += "</div>";

            return html;
        }

    }
}
