using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Models.CLEM.Activities
{
    /// <summary>
    /// Activity to processes one resource into another resource with associated labour and costs
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This activity processes one resource into another resource with associated labour and costs.")]
    [HelpUri(@"Content/Features/activities/All resources/ProcessResource.htm")]
    [Version(1, 0, 1, "")]
    public class ResourceActivityProcess : CLEMActivityBase
    {
        /// <summary>
        /// Resource type to process
        /// </summary>
        [Description("Resource to process")]
        [Models.Core.Display(Type = DisplayType.CLEMResourceName, CLEMResourceNameResourceGroups = new Type[] { typeof(AnimalFoodStore), typeof(HumanFoodStore), typeof(Equipment), typeof(GreenhouseGases), typeof(OtherAnimals), typeof(ProductStore), typeof(WaterStore) })]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name of resource type to process required")]
        public string ResourceTypeProcessedName { get; set; }

        /// <summary>
        /// Resource type created
        /// </summary>
        [Description("Resource created")]
        [Models.Core.Display(Type = DisplayType.CLEMResourceName, CLEMResourceNameResourceGroups = new Type[] { typeof(AnimalFoodStore), typeof(HumanFoodStore), typeof(Equipment), typeof(GreenhouseGases), typeof(OtherAnimals), typeof(ProductStore), typeof(WaterStore) })]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name of resource type created required")]
        public string ResourceTypeCreatedName { get; set; }

        /// <summary>
        /// Conversion rate
        /// </summary>
        [Description("Rate to convert processed resource to created resource")]
        [Required, GreaterThanValue(0)]
        public double ConversionRate { get; set; }

        /// <summary>
        /// Reserve
        /// </summary>
        [Description("Amount to reserve")]
        [Required, GreaterThanEqualValue(0)]
        public double Reserve { get; set; }

        /// <summary>
        /// Resource to process
        /// </summary>
        [XmlIgnore]
        private IResourceType resourceTypeProcessModel { get; set; }

        /// <summary>
        /// Resource created
        /// </summary>
        [XmlIgnore]
        private IResourceType resourceTypeCreatedModel { get; set; }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            resourceTypeProcessModel = Resources.GetResourceItem(this, ResourceTypeProcessedName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as IResourceType;
            resourceTypeCreatedModel = Resources.GetResourceItem(this, ResourceTypeCreatedName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as IResourceType;
        }

        /// <summary>
        /// Adjust resources for activity based on shortfalls
        /// </summary>
        public override void AdjustResourcesNeededForActivity()
        {
            // get labour shortfall
            double labprop = this.LimitProportion(typeof(LabourType));
            // get finance shortfall
            double finprop = this.LimitProportion(typeof(FinanceType));

            // reduce amount used
            double limit = Math.Min(labprop, finprop);

            if(limit<1)
            {
                // find process resource entry in resource list
                ResourceRequest rr = ResourceRequestList.Where(a => a.ResourceType == resourceTypeProcessModel.GetType()).FirstOrDefault();
                if (rr != null)
                {
                    // reduce amount required
                    rr.Required *= limit;
                }
            }
        }

        /// <summary>
        /// Perform activity
        /// </summary>
        public override void DoActivity()
        {
            // processed resource should already be taken
            Status = ActivityStatus.NotNeeded;
            // add created resources
            ResourceRequest rr = ResourceRequestList.Where(a => (a.Resource != null && a.Resource.GetType() == resourceTypeProcessModel.GetType())).FirstOrDefault();
            if (rr != null)
            {
                resourceTypeCreatedModel.Add(rr.Provided * ConversionRate, this, "Created " + (resourceTypeCreatedModel as Model).Name);
                if(rr.Provided > 0)
                {
                    Status = ActivityStatus.Success;
                }
            }
        }

        /// <summary>
        /// Work out the amount of labour required for this activity
        /// </summary>
        /// <param name="requirement"></param>
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
                    daysNeeded = requirement.UnitSize * requirement.LabourPerUnit;
                    break;
                default:
                    throw new Exception(String.Format("LabourUnitType {0} is not supported for {1} in {2}", requirement.UnitType, requirement.Name, this.Name));
            }
            return daysNeeded;
        }

        /// <summary>
        /// Resources needed for Activity
        /// </summary>
        /// <returns></returns>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            List<ResourceRequest> resourcesNeeded = new List<ResourceRequest>();

            double amountToProcess = resourceTypeProcessModel.Amount;
            if (Reserve > 0)
            {
                amountToProcess = Math.Min(amountToProcess, Reserve);
            }

            // get finances required.
            foreach (ResourceActivityFee item in Apsim.Children(this, typeof(ResourceActivityFee)))
            {
                if (ResourceRequestList == null)
                {
                    ResourceRequestList = new List<ResourceRequest>();
                }

                double sumneeded = 0;
                switch (item.PaymentStyle)
                {
                    case ResourcePaymentStyleType.Fixed:
                        sumneeded = item.Amount;
                        break;
                    case ResourcePaymentStyleType.perUnit:
                        sumneeded = amountToProcess*item.Amount;
                        break;
                    case ResourcePaymentStyleType.perBlock:
                        ResourcePricing price = resourceTypeProcessModel.Price(PurchaseOrSalePricingStyleType.Both);
                        double blocks = amountToProcess / price.PacketSize;
                        if(price.UseWholePackets)
                        {
                            blocks = Math.Truncate(blocks);
                        }
                        sumneeded = blocks * item.Amount;
                        break;
                    default:
                        throw new Exception(String.Format("PaymentStyle [{0}] is not supported for [{1}] in [a={2}]", item.PaymentStyle, item.Name, this.Name));
                }
                if (sumneeded > 0)
                {
                    ResourceRequestList.Add(new ResourceRequest()
                    {
                        AllowTransmutation = false,
                        Required = sumneeded,
                        ResourceType = typeof(Finance),
                        ResourceTypeName = item.BankAccount.Name,
                        ActivityModel = this,
                        FilterDetails = null,
                        Reason = item.Name
                    }
                    );
                }
            }

            // get process resource required
            if (amountToProcess > 0)
            {
                resourcesNeeded.Add(
                    new ResourceRequest()
                    {
                        AllowTransmutation = true,
                        Required = amountToProcess,
                        ResourceType = (resourceTypeProcessModel as Model).Parent.GetType(),
                        ResourceTypeName = (resourceTypeProcessModel as Model).Name,
                        ActivityModel = this,
                        Reason = "Process "+ (resourceTypeProcessModel as Model).Name
                    }
                );
            }
            return resourcesNeeded;
        }

        /// <summary>
        /// Resources needed for initialisation
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
            html += "\n<div class=\"activityentry\">Process ";
            if (ResourceTypeProcessedName == null || ResourceTypeProcessedName == "")
            {
                html += "<span class=\"errorlink\">[RESOURCE NOT SET]</span>";
            }
            else
            {
                html += "<span class=\"resourcelink\">" + ResourceTypeProcessedName + "</span>";
            }
            html += " into ";
            if (ResourceTypeCreatedName == null || ResourceTypeCreatedName == "")
            {
                html += "<span class=\"errorlink\">[RESOURCE NOT SET]</span>";
            }
            else
            {
                html += "<span class=\"resourcelink\">" + ResourceTypeCreatedName + "</span>";
            }
            html += " at a rate of ";
            if (ConversionRate <= 0)
            {
                html += "<span class=\"errorlink\">[RATE NOT SET]</span>";
            }
            else
            {
                html += "1:<span class=\"resourcelink\">" + ConversionRate.ToString("0.###") + "</span>";
            }
            html += "</div>";
            if(Reserve > 0)
            {
                html += "\n<div class=\"activityentry\">";
                html += "<span class=\"setvalue\">" + Reserve.ToString("0.###") + "</span> will be reserved.";
                html += "</div>";
            }
            return html;
        }
    }
}
