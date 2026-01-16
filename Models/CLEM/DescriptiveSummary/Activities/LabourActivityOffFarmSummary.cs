using Models.CLEM.Activities;
using System;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for Labour Activity Off Farm
/// </summary>
public class LabourActivityOffFarmSummary : DescriptiveSummaryProviderBase<LabourActivityOffFarm>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        generator.AddBlockWithText("activityentry", $"Earnings will be paid to {generator.DisplaySummaryValueSnippet(ModelTyped.BankAccountName, "Account not set", HTMLSummaryStyle.Resource)}" +
            $" based on {generator.DisplaySummaryValueSnippet("Labour pricing", entryStyle: HTMLSummaryStyle.Filter)} set in the {generator.DisplaySummaryResourceTypeSnippet("Labour")}");
    }
}
