using Models.CLEM.Resources;

namespace Models.CLEM.DescriptiveSummary.Resources
{
    /// <summary>
    /// Descriptive summary provider for the Finances Resource
    /// </summary>
    public class FinanceTypeSummary : DescriptiveSummaryProviderBase<FinanceType>
    {
        /// <inheritdoc/>
        public override void BuildSummary(FinanceType model)
        {
            string balance = $"Opening balance of {CLEMModel.DisplaySummaryValueSnippet(model.OpeningBalance)}";
            if (model.EnforceWithdrawalLimit)
            {
                balance += $" that can be withdrawn to {CLEMModel.DisplaySummaryValueSnippet(model.WithdrawalLimit)}";
            }
            else
            {
                balance += " with no withdrawal limit";
            }
            Generator.AddBlockWithText("activityentry", balance);

            string interest = "No interest rates included";
            if (model.InterestRateCharged + model.InterestRatePaid > 0)
            {
                interest = "Annual interest rate of ";
                if (model.InterestRateCharged > 0)
                {
                    interest += $"{CLEMModel.DisplaySummaryValueSnippet(model.InterestRateCharged)}% charged ";
                    if (model.InterestRatePaid > 0)
                        interest += "and ";
                }
                if (model.InterestRatePaid > 0)
                {
                    interest += $"{CLEMModel.DisplaySummaryValueSnippet(model.InterestRatePaid)}% paid";
                }
            }
            Generator.AddBlockWithText("activityentry", interest);
        }
    }
}
