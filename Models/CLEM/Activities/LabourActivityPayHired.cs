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
    /// <summary>Activity to arrange payment of hired labour at start of CLEM timestep
    /// Labour can be limited by shortfall
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This activity performs payment of all hired labour in the time step.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Labour/PayHiredLabour.htm")]
    public class LabourActivityPayHired : CLEMActivityBase, IValidatableObject
    {
        /// <summary>
        /// Get the Clock.
        /// </summary>
        [Link]
        Clock Clock = null;

        /// <summary>
        /// Account to use
        /// </summary>
        [Description("Account to use")]
        [Models.Core.Display(Type = DisplayType.CLEMResource, CLEMResourceGroups = new Type[] { typeof(Finance) })]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Account to use required")]
        public string AccountName { get; set; }

        /// <summary>
        /// Store finance type to use
        /// </summary>
        private FinanceType bankAccount;

        /// <summary>An event handler to allow us to initialise</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            // activity is performed in CLEMStartOfTimestep not default CLEMGetResources
            this.AllocationStyle = ResourceAllocationStyle.Manual;

            bankAccount = Resources.GetResourceItem(this, AccountName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop) as FinanceType;
        }

        /// <summary>An event handler to allow us to organise payment at start of timestep.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMStartOfTimestep")]
        private void OnCLEMStartOfTimestep(object sender, EventArgs e)
        {
            GetResourcesRequiredForActivity();
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
            Status = ActivityStatus.Warning;

            // get amount of finance needed and provided
            double financeRequired = 0;
            double financeProvided = 0;
            foreach (ResourceRequest item in ResourceRequestList.Where(a => a.ResourceType == typeof(Finance)).ToList())
            {
                financeRequired += item.Required;
                financeProvided += item.Provided;
                Status = ActivityStatus.NotNeeded;
            }

            if(financeRequired > 0)
            {
                Status = ActivityStatus.Success;
            }

            // reduce limiters based on financial shortfall
            if (financeProvided < financeRequired)
            {
                if (this.OnPartialResourcesAvailableAction == OnPartialResourcesAvailableActionTypes.UseResourcesAvailable)
                {
                    Status = ActivityStatus.Partial;
                    int currentmonth = Clock.Today.Month;
                    double currentCost = 0;

                    // step through all hired labour in order and set limiter where needed
                    foreach (LabourType item in Resources.Labour().Items.Where(a => a.Hired))
                    {
                        // get days needed
                        double daysNeeded = item.LabourAvailability.GetAvailability(currentmonth - 1);
                        // calculate rate and amount needed
                        double rate = item.PayRate();

                        double cost = daysNeeded * rate;

                        if (currentCost == financeProvided)
                        {
                            item.AvailabilityLimiter = 0;
                            cost = 0;
                        }
                        else if (currentCost + cost > financeProvided)
                        {
                            //  reduce limit
                            double excess = currentCost + cost - financeProvided;
                            item.AvailabilityLimiter = (cost - excess) / cost;
                            cost = financeProvided - currentCost;
                        }
                        currentCost += cost;
                    }
                }
            }

            return;
        }

        /// <summary>
        /// Determines how much labour is required from this activity based on the requirement provided
        /// </summary>
        /// <param name="requirement">The details of how labour are to be provided</param>
        /// <returns></returns>
        public override GetDaysLabourRequiredReturnArgs GetDaysLabourRequired(LabourRequirement requirement)
        {
            return new GetDaysLabourRequiredReturnArgs(0, "Hire labour", null);
        }

        /// <summary>
        /// Method to determine resources required for this activity in the current month
        /// </summary>
        /// <returns>A list of resource requests</returns>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            List<ResourceRequest> resourcesNeeded = new List<ResourceRequest>();
            int currentmonth = Clock.Today.Month;
            double total = 0;
            foreach (LabourType item in Resources.Labour().Items.Where(a => a.Hired))
            {
                // get days needed
                double daysNeeded = item.LabourAvailability.GetAvailability(currentmonth - 1);

                // calculate rate and amount needed
                double rate = item.PayRate();
                total += (daysNeeded * rate);
            }

            // create resource request
            resourcesNeeded.Add(new ResourceRequest()
            {
                Resource = bankAccount,
                ResourceType = typeof(Finance),
                AllowTransmutation = false,
                Required = total,
                ResourceTypeName = this.AccountName,
                ActivityModel = this,
                Category = "Hire labour"
            }
            );
            return resourcesNeeded;
        }

        /// <summary>
        /// Method to determine resources required for initialisation of this activity
        /// </summary>
        /// <returns></returns>
        public override List<ResourceRequest> GetResourcesNeededForinitialisation()
        {
            return null;
        }

        #region validation
        /// <summary>
        /// Validate model
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            // make sure finance present
            // this is performed in the assignment of bankaccount in InitialiseActivity

            // make sure labour hired present
            if (Resources.Labour().Items.Where(a => a.Hired).Count() == 0)
            {
                string[] memberNames = new string[] { "Hired labour" };
                results.Add(new ValidationResult("No [r=LabourType] of hired labour has been defined in [r=Labour]\r\nThis activity will not be performed without hired labour.", memberNames));
            }
            // make sure pay rates present
            if (!Resources.Labour().PricingAvailable)
            {
                string[] memberNames = new string[] { "Labour pay rate" };
                results.Add(new ValidationResult("No [r=LabourPricing] is available for [r=Labour]\r\nThis activity will not be performed without labour pay rates.", memberNames));
            }
            return results;
        }
        #endregion

        #region descriptive summary
        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "\r\n<div class=\"activityentry\">Pay all hired labour based on PayRates from ";
            if (AccountName == null || AccountName == "")
            {
                html += "<span class=\"errorlink\">[ACCOUNT NOT SET]</span>";
            }
            else
            {
                html += "<span class=\"resourcelink\">" + AccountName + "</span>";
            }
            html += "</div>";
            return html;
        } 
        #endregion

    }
}
