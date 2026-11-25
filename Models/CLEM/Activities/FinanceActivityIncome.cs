using APSIM.Numerics;
using Models.CLEM.Extras;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace Models.CLEM.Activities
{
    /// <summary>Activity to arrange payment of income into a <c>FinanceType</c> account
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Define an income source and arrange payment")]
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
        /// The account to pay into
        /// </summary>
        /// <remarks>
        /// This is a required field and must be specified. The values provided in the drop down list in the UI
        /// represent the <c>FinanceType</c> accounts available
        /// </remarks>
        [Description("Account to use")]
        [Core.Display(Type = DisplayType.DropDown, Values = "GetResourcesAvailableByName", ValuesArgs = new object[] { new Type[] { typeof(Finance) } })]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Account to use required")]
        public string AccountName { get; set; }

        /// <summary>An event handler allowing for any activity initialisation</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        /// <remarks>
        /// This is performed during the <c>CLEMEvents.CLEMInitialiseActivity</c> event
        /// </remarks>
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
                bankAccount.AddToResource(Amount, this, null, TransactionCategory);
                SetStatusSuccessOrPartial();
            }
        }
    }
}
