using Models.Core;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Models.Core.Attributes;
using System.IO;

namespace Models.CLEM.Activities
{
    /// <summary>Activity to arrange and pay an enterprise expenses
    /// Expenses can be flagged as overheads for accounting
    /// </summary>
    /// <version>1.0</version>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This activity performs payment of a specified expense.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Finances/PayExpenses.htm")]
    public class FinanceActivityPayExpense : CLEMActivityBase
    {
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
        [Models.Core.Display(Type = DisplayType.CLEMResource, CLEMResourceGroups = new Type[] { typeof(Finance) })]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Account to use required")]
        public string AccountName { get; set; }

        /// <summary>
        /// Farm overhead
        /// </summary>
        [Description("Farm overhead")]
        [Required]
        public bool IsOverhead { get; set; }

        /// <summary>
        /// Store finance type to use
        /// </summary>
        private FinanceType bankAccount;

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
            bankAccount = Resources.GetResourceItem(this, AccountName, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.ReportErrorAndStop) as FinanceType;
        }

        /// <summary>
        /// Method to determine resources required for this activity in the current month
        /// </summary>
        /// <returns>List of required resource requests</returns>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
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
                    Category = ((IsOverhead) ? "Overhead" : "Expense")
                }
            };
            return resourcesNeeded;
        }

        /// <summary>
        /// Method used to perform activity if it can occur as soon as resources are available.
        /// </summary>
        public override void DoActivity()
        {
            return;
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
        /// Determines how much labour is required from this activity based on the requirement provided
        /// </summary>
        /// <param name="requirement">The details of how labour are to be provided</param>
        /// <returns></returns>
        public override GetDaysLabourRequiredReturnArgs GetDaysLabourRequired(LabourRequirement requirement)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The method allows the activity to adjust resources requested based on shortfalls (e.g. labour) before they are taken from the pools
        /// </summary>
        public override void AdjustResourcesNeededForActivity()
        {
            return;
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
                htmlWriter.Write("\r\n<div class=\"activityentry\">Pay ");
                htmlWriter.Write("<span class=\"setvalue\">" + Amount.ToString("#,##0.00") + "</span> from ");
                if (AccountName == null || AccountName == "")
                {
                    htmlWriter.Write("<span class=\"errorlink\">[ACCOUNT NOT SET]</span>");
                }
                else
                {
                    htmlWriter.Write("<span class=\"resourcelink\">" + AccountName + "</span>");
                }
                htmlWriter.Write("</div>");
                if (IsOverhead)
                {
                    htmlWriter.Write("\r\n<div class=\"activityentry\">This is an overhead</div>");
                }

                return htmlWriter.ToString(); 
            }
        } 
        #endregion

    }
}
