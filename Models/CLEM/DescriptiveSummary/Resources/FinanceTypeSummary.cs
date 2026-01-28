using Models.CLEM.Resources;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for the Finances Resource
/// </summary>
public class FinanceTypeSummary : DescriptiveSummaryProviderBase<FinanceType>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        var model = ModelTyped;
        if (model is null) return;

        string balance = $"Opening balance of {generator.DisplaySummaryValueSnippet(model.OpeningBalance)}";
        if (model.EnforceWithdrawalLimit)
        {
            balance += $" that can be withdrawn to {generator.DisplaySummaryValueSnippet(model.WithdrawalLimit)}";
        }
        else
        {
            balance += " with no withdrawal limit";
        }
        Generator.AddBlockWithText(balance);

        string interest = "No interest rates included";
        if (model.InterestRateCharged + model.InterestRatePaid > 0)
        {
            interest = "Annual interest rate of ";
            if (model.InterestRateCharged > 0)
            {
                interest += $"{generator.DisplaySummaryValueSnippet(model.InterestRateCharged)}% charged ";
                if (model.InterestRatePaid > 0)
                    interest += "and ";
            }
            if (model.InterestRatePaid > 0)
            {
                interest += $"{generator.DisplaySummaryValueSnippet(model.InterestRatePaid)}% paid";
            }
        }
        Generator.AddBlockWithText(interest);
    }
}
