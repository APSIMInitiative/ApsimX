using Docker.DotNet.Models;
using Models.CLEM.Activities;
using System;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary.Activities;

/// <summary>
/// Descriptive summary provider for Finance Activity Pay Expense
/// </summary>
public class FinanceActivityPayExpenseSummary : DescriptiveSummaryProviderBase<FinanceActivityPayExpense>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        generator.AddBlockWithText($"Pay {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(ModelTyped.Amount, warnZero: true)} from {DescriptiveSummaryGenerator.DisplaySummaryResourceTypeSnippet(ModelTyped.AccountName)}");
    }
}
