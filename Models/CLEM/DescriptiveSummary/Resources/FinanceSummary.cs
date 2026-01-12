using Models.CLEM.Resources;
using Models.Core;
using System.Collections.Generic;

namespace Models.CLEM.DescriptiveSummary.Resources
{
    /// <summary>
    /// Descriptive summary provider for the Finances Resource
    /// </summary>
    public class FinanceSummary : DescriptiveSummaryProviderBase<Finance>
    {
        ///<inheritdoc/>
        public override List<(IEnumerable<IModel> models, bool include, string borderClass, string introText, string missingText)> GetChildrenInSummary()
        {
            var model = ModelTyped;
            if (model is null) return [];

            return
            [
                (model.Structure.FindChildren<FinanceType>(), true, "", "", $"No {CLEMModel.DisplaySummaryValueSnippet("FinanceType", entryStyle: HTMLSummaryStyle.Resource)} provided!")
            ];
        }


        /// <inheritdoc/>
        public override void BuildSummary()
        {
            var model = ModelTyped;
            if (model is null) return;

            Generator.AddBlockWithText("activityentry", $"Currency is {CLEMModel.DisplaySummaryValueSnippet(model.CurrencyName, "Not specified")}");
            Generator.AddBlockWithText("activityentry", $"The financial year starts in {CLEMModel.DisplaySummaryValueSnippet(model.FirstMonthOfFinancialYear, "Not specified", warnZero: true)}");
        }
    }
}
