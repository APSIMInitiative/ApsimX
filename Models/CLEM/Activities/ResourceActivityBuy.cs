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

        ResourcePricing price;

        private FinanceType bankAccount;

        private IResourceType resourceToBuy;

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
        }

        /// <summary>
        /// Method to determine resources required for this activity in the current month
        /// </summary>
        /// <returns>List of required resource requests</returns>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            // get pricing.
            price = resourceToBuy.Price;
            // calculate units
            units = Units;
            if (price.UseWholePackets)
            {
                units = Math.Truncate(Units);
            }

            if (units > 0)
            {
                return new List<ResourceRequest>()
                    {
                        new ResourceRequest()
                        {
                            AllowTransmutation = false,
                            Required = units*price.PricePerPacket,
                            ResourceType = typeof(Finance),
                            ResourceTypeName = bankAccount.Name,
                            Reason = "Purchase "+(resourceToBuy as Model).Name,
                            ActivityModel = this
                        }
                    };
            }
            else
            {
                return null;
            }
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
            return;
        }

        /// <summary>
        /// Method used to perform activity if it can occur as soon as resources are available.
        /// </summary>
        public override void DoActivity()
        {
            double labourlimit = this.LabourLimitProportion;
            double pricelimit = this.LimitProportion(typeof(FinanceType));
            double limit = Math.Min(labourlimit, pricelimit);
            Status = ActivityStatus.NotNeeded;
            if (price != null)
            {
                if (limit == 1 || this.OnPartialResourcesAvailableAction == OnPartialResourcesAvailableActionTypes.UseResourcesAvailable)
                {
                    double units2buy = units * limit;
                    if (price.UseWholePackets)
                    {
                        units2buy = Math.Truncate(units2buy);
                    }
                    // adjust resources bought based on labour/finance shortfall
                    // buy resources
                    if (units2buy > 0)
                    {
                        resourceToBuy.Add(units2buy * price.PacketSize, this, "Purchase " + (resourceToBuy as Model).Name);
                        Status = ActivityStatus.Success;
                    }
                }
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
