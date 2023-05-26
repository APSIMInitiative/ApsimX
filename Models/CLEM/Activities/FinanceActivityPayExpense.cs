using Models.Core;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Newtonsoft.Json;
using Models.Core.Attributes;
using System.IO;

namespace Models.CLEM.Activities
{
    /// <summary>Activity to arrange and pay an enterprise expenses
    /// </summary>
    /// <version>1.0</version>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Perform payment of a specified expense")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Finances/PayExpenses.htm")]
    public class FinanceActivityPayExpense : CLEMActivityBase
    {
        private FinanceType bankAccount;

        /// <summary>
        /// Amount payable
        /// </summary>
        [Description("Amount payable")]
        [Required, GreaterThanValue(0)]
        public double Amount { get; set; }

        /// <summary>
        /// Account to use
        /// </summary>
        [Description("Account to use")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new Type[] { typeof(Finance) } })]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Account to use required")]
        public string AccountName { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public FinanceActivityPayExpense()
        {
            this.SetDefaults();
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            bankAccount = Resources.FindResourceType<Finance, FinanceType>(this, AccountName, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.ReportErrorAndStop);
        }

        /// <inheritdoc/>
        public override List<ResourceRequest> RequestResourcesForTimestep(double argument = 0)
        {
            List<ResourceRequest> resourcesNeeded = new List<ResourceRequest>
            {
                new ResourceRequest()
                {
                    Resource = bankAccount,
                    ResourceType = typeof(Finance),
                    AllowTransmutation = false,
                    Required = this.Amount,
                    ResourceTypeName = this.AccountName,
                    ActivityModel = this,
                    Category = TransactionCategory
                }
            };
            return resourcesNeeded;
        }

        /// <inheritdoc/>
        public override void PerformTasksForTimestep(double argument = 0)
        {
            if(ResourceRequestList.Any())
            {
                var finRequest = ResourceRequestList.Where(a => a.ResourceType == typeof(Finance)).FirstOrDefault<ResourceRequest>();
                if (finRequest != null)
                {
                    SetStatusSuccessOrPartial(finRequest.Provided != finRequest.Required);
                }
            }
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write($"\r\n<div class=\"activityentry\">Pay {CLEMModel.DisplaySummaryValueSnippet(Amount, warnZero:true)}");
                htmlWriter.Write($" from {CLEMModel.DisplaySummaryValueSnippet(AccountName, "Not set", HTMLSummaryStyle.Resource)}");
                htmlWriter.Write("</div>");
                return htmlWriter.ToString(); 
            }
        } 
        #endregion

    }
}
