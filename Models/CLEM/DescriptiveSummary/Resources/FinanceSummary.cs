using Models.CLEM.Resources;

namespace Models.CLEM.DescriptiveSummary.Resources
{
    /// <summary>
    /// Descriptive summary provider for the Finances Resource
    /// </summary>
    public class FinanceSummary : DescriptiveSummaryProviderBase<Finance>
    {
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
