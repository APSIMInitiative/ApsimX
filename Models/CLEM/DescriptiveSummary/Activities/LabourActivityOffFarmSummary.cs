using Models.CLEM.Activities;
using System;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary.Activities;

/// <summary>
/// Descriptive summary provider for Labour Activity Off Farm
/// </summary>
public class LabourActivityOffFarmSummary : DescriptiveSummaryProviderBase<LabourActivityOffFarm>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        generator.AddBlockWithText($"Earnings will be paid to {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(ModelTyped.BankAccountName, "Account not set", HTMLSummaryStyle.Resource)}" +
            $" based on {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet("Labour pricing", entryStyle: HTMLSummaryStyle.Filter)} set in the {DescriptiveSummaryGenerator.DisplaySummaryResourceTypeSnippet("Labour")}");
    }
}
