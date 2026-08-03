using Docker.DotNet.Models;
using Models.CLEM.Activities;
using System;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for Finance Activity Income
/// </summary>
public class FinanceActivityIncomeSummary : DescriptiveSummaryProviderBase<FinanceActivityIncome>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        generator.AddBlockWithText($"Earn {generator.DisplaySummaryValueSnippet(ModelTyped.Amount, warnZero: true)} paid into {generator.DisplaySummaryResourceTypeSnippet(ModelTyped.AccountName)}");
    }
}
