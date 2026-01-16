using Models.CLEM.Activities;
using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for Other Animals Activity Sell
/// </summary>
public class OtherAnimalsActivitySellSummary : DescriptiveSummaryProviderBase<OtherAnimalsActivitySell>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        generator.AddBlockWithText("activityentry", $"Sales will pay to {generator.DisplaySummaryValueSnippet(ModelTyped.BankAccountName, "Not set", HTMLSummaryStyle.Resource)}");
        generator.AddBlockWithText("activityentry", $"Individuals will be flagged as {generator.DisplaySummaryValueSnippet(ModelTyped.SaleFlagToUse, "Not set")}");
    }
}
