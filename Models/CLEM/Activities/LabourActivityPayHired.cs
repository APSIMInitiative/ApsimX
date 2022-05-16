using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Activities
{
    /// <summary>Activity to arrange payment of hired labour at start of CLEM timestep
    /// Labour can be limited by shortfall
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Performs payment of all hired labour in the time step")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Labour/PayHiredLabour.htm")]
    public class LabourActivityPayHired : CLEMActivityBase, IValidatableObject
    {
        [Link]
        private Clock clock = null;

        private FinanceType bankAccount;
        private Labour labour;

        /// <summary>
        /// Account to use
        /// </summary>
        [Description("Account to use")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new object[] { typeof(Finance) } })]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Account to use required")]
        public string AccountName { get; set; }

        /// <summary>
        /// Pay labour calculation style
        /// </summary>
        [Description("Payment calculation style")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Payment calculation style required")]
        public PayHiredLabourCalculationStyle PaymentCalculationStyle { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public LabourActivityPayHired()
        {
            TransactionCategory = "Labour.Hired";
        }

        /// <summary>An event handler to allow us to initialise</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            // activity is performed in CLEMStartOfTimestep not default CLEMGetResources
            this.AllocationStyle = ResourceAllocationStyle.Manual;

            bankAccount = Resources.FindResourceType<Finance, FinanceType>(this, AccountName, OnMissingResourceActionTypes.ReportErrorAndStop, OnMissingResourceActionTypes.ReportErrorAndStop);
            labour = Resources.FindResourceGroup<Labour>();
        }

        /// <summary>An event handler to allow us to organise payment at start of timestep.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMStartOfTimeStep")]
        private void OnCLEMStartOfTimeStep(object sender, EventArgs e)
        {
            if(PaymentCalculationStyle == PayHiredLabourCalculationStyle.ByAvailableLabour)
                GetResourcesRequiredForActivity();
        }

        /// <summary>An event handler to allow us to organise payment at start of timestep.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMHerdSummary")]
        private void OnCLEMHerdSummary(object sender, EventArgs e)
        {
            if (PaymentCalculationStyle == PayHiredLabourCalculationStyle.ByLabourUsedInTimeStep)
            {
                int currentmonth = clock.Today.Month;
                double total = 0;
                foreach (LabourType item in labour.Items.Where(a => a.Hired))
                {
                    // get days needed
                    double daysUsed = item.LabourAvailability.GetAvailability(currentmonth - 1) - item.AvailableDays;

                    // calculate rate and amount needed
                    double rate = item.PayRate();
                    total += (daysUsed * rate);
                }

                // take hire cost
                bankAccount.Remove(new ResourceRequest()
                {
                    Resource = bankAccount,
                    ResourceType = typeof(Finance),
                    AllowTransmutation = false,
                    Required = total,
                    ResourceTypeName = this.AccountName,
                    ActivityModel = this,
                    Category = TransactionCategory
                });
            }
        }


        /// <inheritdoc/>
        public override void DoActivity()
        {
            if (PaymentCalculationStyle == PayHiredLabourCalculationStyle.ByAvailableLabour)
            {
                Status = ActivityStatus.Warning;

                // get amount of finance needed and provided
                double financeRequired = 0;
                double financeProvided = 0;
                foreach (ResourceRequest item in ResourceRequestList.Where(a => a.ResourceType == typeof(Finance)))
                {
                    financeRequired += item.Required;
                    financeProvided += item.Provided;
                    Status = ActivityStatus.NotNeeded;
                }

                if (financeRequired > 0)
                    Status = ActivityStatus.Success;

                // reduce limiters based on financial shortfall
                if (financeProvided < financeRequired)
                {
                    if (this.OnPartialResourcesAvailableAction == OnPartialResourcesAvailableActionTypes.UseResourcesAvailable)
                    {
                        Status = ActivityStatus.Partial;
                        int currentmonth = clock.Today.Month;
                        double currentCost = 0;

                        // step through all hired labour in order and set limiter where needed
                        foreach (LabourType item in labour.Items.Where(a => a.Hired))
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
            }
            return;
        }

        /// <inheritdoc/>
        public override GetDaysLabourRequiredReturnArgs GetDaysLabourRequired(LabourRequirement requirement)
        {
            return new GetDaysLabourRequiredReturnArgs(0, TransactionCategory, null);
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            List<ResourceRequest> resourcesNeeded = new List<ResourceRequest>();
            if (PaymentCalculationStyle == PayHiredLabourCalculationStyle.ByAvailableLabour)
            {
                int currentmonth = clock.Today.Month;
                double total = 0;
                foreach (LabourType item in labour.Items.Where(a => a.Hired))
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
                    Category = TransactionCategory
                }
                );
            }
            return resourcesNeeded;
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

            if (labour is null)
            {
                string[] memberNames = new string[] { "Labour" };
                results.Add(new ValidationResult("No [r=Labour] is provided in resources\r\nThis activity will not be performed without labour.", memberNames));
            }
            else
            {
                // make sure labour hired present
                if (labour.Items.Where(a => a.Hired).Count() == 0)
                {
                    string[] memberNames = new string[] { "Hired labour" };
                    results.Add(new ValidationResult("No [r=LabourType] of hired labour has been defined in [r=Labour]\r\nThis activity will not be performed without hired labour.", memberNames));
                }
                // make sure pay rates present
                if (!labour.PricingAvailable)
                {
                    string[] memberNames = new string[] { "Labour pay rate" };
                    results.Add(new ValidationResult("No [r=LabourPricing] is available for [r=Labour]\r\nThis activity will not be performed without labour pay rates.", memberNames));
                }
            }
            return results;
        }
        #endregion

        #region descriptive summary
        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"activityentry\">Pay all hired labour based on labour rate from ");
                htmlWriter.Write(CLEMModel.DisplaySummaryValueSnippet(AccountName, "Account not set", HTMLSummaryStyle.Resource));
                htmlWriter.Write("</div>");
                return htmlWriter.ToString();
            }
        } 
        #endregion

    }
}
