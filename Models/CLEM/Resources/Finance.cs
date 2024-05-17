using Models.Core;
using Models.Core.Attributes;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace Models.CLEM.Resources
{
    ///<summary>
    /// Parent model of finance models.
    ///</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ResourcesHolder))]
    [Description("Resource group for all finance types (bank accounts) in the simulation.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Resources/Finance/Finance.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    [ModelAssociations(associatedModels: new Type[] { typeof(ResourcesHolder) }, associationStyles: new ModelAssociationStyle[] { ModelAssociationStyle.Parent })]
    public class Finance : ResourceBaseWithTransactions
    {
        [Link]
        readonly IClock Clock = null;

        /// <summary>
        /// Currency used
        /// </summary>
        [Description("Name of currency")]
        public string CurrencyName { get; set; }

        /// <summary>
        /// Start of financial year
        /// </summary>
        [Description("Start of financial year")]
        [System.ComponentModel.DefaultValueAttribute(7)]
        [Required, Month]
        public MonthsOfYear FirstMonthOfFinancialYear { get; set; }

        /// <summary>
        /// Property to determine the financial year from current date
        /// </summary>
        /// <returns>The financial year</returns>
        public int FinancialYear
        {
            get
            {
                if (Clock.Today.Month < (int)FirstMonthOfFinancialYear)
                    return Clock.Today.Year - 1;
                else
                    return Clock.Today.Year;
            }
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using StringWriter htmlWriter = new();
            htmlWriter.Write($"<div class=\"activityentry\">Currency is {CLEMModel.DisplaySummaryValueSnippet(CurrencyName, "Not specified")}</div>");
            htmlWriter.Write($"<div class=\"activityentry\">The financial year starts in ");
            if (FirstMonthOfFinancialYear == 0)
                htmlWriter.Write("<span class=\"errorlink\">NOT SET</span>");
            else
            {
                htmlWriter.Write("<span class=\"setvalueextra\">");
                htmlWriter.Write(FirstMonthOfFinancialYear.ToString() + "</span>");
            }
            htmlWriter.Write("</div>");
            return htmlWriter.ToString();
        }

        #endregion

    }
}
