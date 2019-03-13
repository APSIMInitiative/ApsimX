using Models.Core;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Models.Core.Attributes;

namespace Models.CLEM.Activities
{
    /// <summary>
    /// Off farm labour activities
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This activity manages labour supplied and income derived from an off-farm task.")]
    [Version(1, 0, 1, "")]
    public class LabourActivityOffFarm: CLEMActivityBase
    {
        /// <summary>
        /// Daily labour rate
        /// </summary>
        [Description("Daily labour rate")]
        [Required]
        public double DailyRate { get; set; }

        /// <summary>
        /// Days worked
        /// </summary>
        [Description("Days work required")]
        [Required]
        public double DaysWorkRequiredInMonth { get; set; }

        /// <summary>
        /// Bank account name to pay to
        /// </summary>
        [Description("Bank account to pay to")]
        [Models.Core.Display(Type = DisplayType.CLEMResourceName, CLEMResourceNameResourceGroups = new Type[] { typeof(Finance) })]
        public string BankAccountName { get; set; }

        private FinanceType bankType { get; set; }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            // locate BankType resource
            bankType = Resources.GetResourceItem(this, BankAccountName, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.Ignore) as FinanceType;

            if(bankType==null && Resources.GetResourceGroupByType(typeof(Finance))==null)
            {
                Summary.WriteWarning(this, "No bank account has been specified for [a="+this.Name+"]. No funds will be earned!");
            }
        }

        /// <summary>
        /// Determines how much labour is required from this activity based on the requirement provided
        /// </summary>
        /// <param name="requirement">The details of how labour are to be provided</param>
        /// <returns></returns>
        public override double GetDaysLabourRequired(LabourRequirement requirement)
        {
            double daysNeeded = DaysWorkRequiredInMonth;
            return daysNeeded;
        }

        /// <summary>
        /// Method to determine resources required for this activity in the current month
        /// </summary>
        /// <returns></returns>
        public override List<ResourceRequest> GetResourcesNeededForActivity()
        {
            return null;
        }

        /// <summary>
        /// Method used to perform activity if it can occur as soon as resources are available.
        /// </summary>
        public override void DoActivity()
        {
            // days provided from labour set in the all requests in the resourceResquestList
            // receive payment for labour if bank type exists
            if (bankType != null)
            {
                bankType.Add(ResourceRequestList.Sum(a => a.Provided) * DailyRate, this, "Off farm labour");
            }
        }

        /// <summary>
        /// The method allows the activity to adjust resources requested based on shortfalls (e.g. labour) before they are taken from the pools
        /// </summary>
        public override void AdjustResourcesNeededForActivity()
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
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "";
            html += "\n<div class=\"activityentry\">Earn ";
            html += "<span class=\"setvalue\">" + DailyRate.ToString("#,##0.00") + "</span> per day for <span class=\"setvalue\">" + DaysWorkRequiredInMonth.ToString("#0") + "</span> days paid to ";
            if (BankAccountName == null || BankAccountName == "")
            {
                html += "<span class=\"errorlink\">[ACCOUNT NOT SET]</span>";
            }
            else
            {
                html += "<span class=\"resourcelink\">" + BankAccountName + "</span>";
            }
            html += "</div>";

            return html;
        }

    }
}
