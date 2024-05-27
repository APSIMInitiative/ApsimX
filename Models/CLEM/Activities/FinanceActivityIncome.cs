using APSIM.Shared.Utilities;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;

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
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
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
        public override void PerformTasksForTimestep(double argument = 0)
        {
            if (MathUtilities.IsPositive(Amount))
            {
                bankAccount.Add(Amount, this, null, TransactionCategory);
                SetStatusSuccessOrPartial();
            }
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using StringWriter htmlWriter = new();
            htmlWriter.Write($"\r\n<div class=\"activityentry\">Earn {CLEMModel.DisplaySummaryValueSnippet(Amount, warnZero: true)}");
            htmlWriter.Write($" paid into {CLEMModel.DisplaySummaryValueSnippet(AccountName, "Not set", HTMLSummaryStyle.Resource)}");
            htmlWriter.Write("</div>");
            return htmlWriter.ToString();
        } 
        #endregion

    }
}
