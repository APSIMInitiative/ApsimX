using Models.CLEM.Activities;
using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary.Activities;

/// <summary>
/// Descriptive summary provider for Other Animals Activity Sell
/// </summary>
public class OtherAnimalsActivitySellSummary : DescriptiveSummaryProviderBase<OtherAnimalsActivitySell>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        generator.AddBlockWithText($"Sales will pay to {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(ModelTyped.BankAccountName, "Not set", HTMLSummaryStyle.Resource)}");
        generator.AddBlockWithText($"Individuals will be flagged as {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(ModelTyped.SaleFlagToUse, "Not set")}");
    }
}
