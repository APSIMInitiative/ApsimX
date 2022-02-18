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
    /// <summary>Activity to arrange income into an account
    /// </summary>
    /// <version>1.0</version>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Define an income source")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Finances/Income.htm")]
    public class FinanceActivityIncome : CLEMActivityBase
    {
        private FinanceType bankAccount;

        /// <summary>
        /// Amount earned
        /// </summary>
        [Description("Amount earned")]
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
        public FinanceActivityIncome()
        {
            this.SetDefaults();
            TransactionCategory = "Income";
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
        protected override void PerformTasksForActivity()
        {
            if (Amount > 0)
            {
                bankAccount.Add(Amount, this, "", TransactionCategory);
                SetStatusSuccess();
            }
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"activityentry\">Earn ");
                htmlWriter.Write("<span class=\"setvalue\">" + Amount.ToString("#,##0.00") + "</span> into ");
                if (AccountName == null || AccountName == "")
                    htmlWriter.Write("<span class=\"errorlink\">[ACCOUNT NOT SET]</span>");
                else
                    htmlWriter.Write("<span class=\"resourcelink\">" + AccountName + "</span>");

                htmlWriter.Write("</div>");
                return htmlWriter.ToString(); 
            }
        } 
        #endregion

    }
}
