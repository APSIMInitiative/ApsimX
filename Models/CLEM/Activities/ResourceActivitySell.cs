using Models.Core;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    public class ResourceActivitySell: CLEMActivityBase, IValidatableObject
    {
        /// <summary>
        /// Name of account to use
        /// </summary>
        [Description("Name of bank account to use")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name of account to use required")]
        public string AccountName { get; set; }

        /// <summary>
        /// Name of resource group containing resource
        /// </summary>
        [Description("Name of resource group containing resource")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name of resource group required")]
        public string ResourceGroupName { get; set; }

        /// <summary>
        /// Name of resource type to sell
        /// </summary>
         [Description("Name of resource type to sell")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Name of resource type required")]
        public string ResourceTypeName { get; set; }

        /// <summary>
        /// Determines whether sales are restricted to whole units
        /// </summary>
         [Description("Restrict sales to whole units")]
        [Required]
        public bool SellWholeUnitsOnly { get; set; }

        /// <summary>
        /// Amount reserved from sale
        /// </summary>
         [Description("Amount reserved from sale")]
        [Required, GreaterThanEqualValue(0)]
        public double AmountReserved { get; set; }

        /// <summary>
        /// Unit size (amount of the resource per sale unit)
        /// </summary>
         [Description("Unit size (amount of the resource per sale unit)")]
        [Required, GreaterThanEqualValue(1)]
        public double UnitSize { get; set; }

        /// <summary>
        /// Unit price (value of each sale unit)
        /// </summary>
         [Description("Unit price (value of each sale unit)")]
        [Required, GreaterThanEqualValue(1)]
        public double UnitPrice { get; set; }

        /// <summary>
        /// Store finance type to use
        /// </summary>
        private FinanceType bankAccount;

        /// <summary>
        /// Store finance type to use
        /// </summary>
        private IResourceType resourceToSell;

        /// <summary>
        /// Labour settings
        /// </summary>
        private List<LabourFilterGroupUnit> Labour { get; set; }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            // get bank account object to use
            bankAccount = Resources.GetResourceItem(this, typeof(Finance), AccountName, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.ReportErrorAndStop) as FinanceType;
            // get resource type to sell
            var resourceGroup = Resources.GetResourceByName(ResourceGroupName);
            resourceToSell = Resources.GetResourceItem(this, resourceGroup.GetType(), ResourceTypeName, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.ReportErrorAndStop) as IResourceType;
            // get labour required for sale
            Labour = Apsim.Children(this, typeof(LabourFilterGroupUnit)).Cast<LabourFilterGroupUnit>().ToList(); //  this.Children.Where(a => a.GetType() == typeof(LabourFilterGroupSpecified)).Cast<LabourFilterGroupSpecified>().ToList();
            if (Labour == null) Labour = new List<LabourFilterGroupUnit>();
        }

        /// <summary>
        /// Validate object
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            var resourceGroup = Resources.GetResourceByName(ResourceGroupName);
            if (resourceGroup == null)
            {
                results.Add(new ValidationResult("Unable to find resource group named " + ResourceGroupName));
            }
            else
            {
                switch (resourceGroup.GetType().ToString())
                {
                    case "Resources.Labour":
                    case "Resources.Ruminant":
                        string[] memberNames = new string[] { "ResourceGroupName" };
                        results.Add(new ValidationResult("Sales of resource type "+ resourceGroup.GetType().ToString() + " are not supported", memberNames));
                        break;
                }
            }

            Labour = Apsim.Children(this, typeof(LabourFilterGroupUnit)).Cast<LabourFilterGroupUnit>().ToList(); //  this.Children.Where(a => a.GetType() == typeof(LabourFilterGroupSpecified)).Cast<LabourFilterGroupSpecified>().ToList();
            if (Labour == null) Labour = new List<LabourFilterGroupUnit>();
            foreach (var item in Labour)
            {
                switch (item.UnitType)
                {
                    case LabourUnitType.Fixed:
                    case LabourUnitType.perUnit:
                        break;
                    default:
                        string[] memberNames = new string[] { item.Name };
                        results.Add(new ValidationResult("Labour unit type " + item.UnitType.ToString() + " is not supported for item "+item.Name, memberNames));
                        break;
                }
            }
            return results;
        }

        /// <summary>
        /// Gets the number of units available for sale
        /// </summary>
        public double UnitsAvailableForSale
        {
            get
            {
                double amountForSale = resourceToSell.Amount - AmountReserved;
                double unitsAvailable = amountForSale / UnitSize;
                if(SellWholeUnitsOnly)
                {
                    unitsAvailable = Math.Truncate(unitsAvailable);
                }
                return unitsAvailable;
            }
        }

        /// <summary>
        /// Method to determine resources required for this activity in the current month
        /// </summary>
        /// <returns>List of required resource requests</returns>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            ResourceRequestList = null;
            if (this.TimingOK)
            {
                double units = UnitsAvailableForSale;
                if (units > 0)
                {
                    // for each labour item specified
                    foreach (var item in Labour)
                    {
                        double daysNeeded = 0;
                        switch (item.UnitType)
                        {
                            case LabourUnitType.Fixed:
                                daysNeeded = item.LabourPerUnit;
                                break;
                            case LabourUnitType.perUnit:
                                daysNeeded = units * item.LabourPerUnit;
                                break;
                            default:
                                break;
                        }
                        if (daysNeeded > 0)
                        {
                            if (ResourceRequestList == null) ResourceRequestList = new List<ResourceRequest>();
                            ResourceRequestList.Add(new ResourceRequest()
                            {
                                AllowTransmutation = false,
                                Required = daysNeeded,
                                ResourceType = typeof(Labour),
                                ResourceTypeName = "",
                                ActivityModel = this,
                                Reason = "Sales",
                                FilterDetails = new List<object>() { item }
                            }
                            );
                        }
                    }
                }
            }
            return ResourceRequestList;
        }

        /// <summary>
        /// Method used to perform activity if it can occur as soon as resources are available.
        /// </summary>
        public override void DoActivity()
        {
            if (this.TimingOK)
            {
                // reduce if labour limiting
                double labourlimit = 1;
                if(ResourceRequestList != null && ResourceRequestList.Where(a => a.ResourceType == typeof(Labour)).Count() > 0)
                {
                    double amountLabourNeeded = ResourceRequestList.Where(a => a.ResourceType == typeof(Labour)).Sum(a => a.Required);
                    double amountLabourProvided = ResourceRequestList.Where(a => a.ResourceType == typeof(Labour)).Sum(a => a.Provided);
                    if (amountLabourNeeded > 0)
                    {
                        if (amountLabourProvided == 0)
                            labourlimit = 0;
                        else
                            labourlimit = amountLabourNeeded / amountLabourProvided;
                    }
                }
                double units = 0;
                if (labourlimit == 1 || this.OnPartialResourcesAvailableAction == OnPartialResourcesAvailableActionTypes.UseResourcesAvailable)
                {
                    units = UnitsAvailableForSale * labourlimit;
                    if (SellWholeUnitsOnly)
                    {
                        units = Math.Truncate(units);
                    }
                }

                if(units>0)
                {
                    // remove resource
                    ResourceRequest purchaseRequest = new ResourceRequest();
                    purchaseRequest.ActivityModel = this;
                    purchaseRequest.Required = units*UnitSize;
                    purchaseRequest.AllowTransmutation = false;
                    purchaseRequest.Reason = "Sales";
                    resourceToSell.Remove(purchaseRequest);

                    // transfer money earned
                    bankAccount.Add(units * UnitPrice, this.Name, "Sales");
                    SetStatusSuccess();
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
            if (ResourceShortfallOccurred != null)
                ResourceShortfallOccurred(this, e);
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
            if (ActivityPerformed != null)
                ActivityPerformed(this, e);
        }

    }
}
