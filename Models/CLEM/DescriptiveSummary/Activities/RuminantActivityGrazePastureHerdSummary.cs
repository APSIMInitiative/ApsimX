using Models.CLEM.Activities;
using System.IO;

namespace Models.CLEM.DescriptiveSummary.Activities;

/// <summary>
/// Descriptive summary provider for Ruminant Activity GrazePastureHerd
/// </summary>
public class RuminantActivityGrazePastureHerdSummary : RuminantActivitySummaryBase<RuminantActivityGrazePastureHerd>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        var model = ModelTyped;
        if (model is null) return;

        generator.AddBlockWithText($"All {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(ModelTyped.RuminantTypeName, "Herd not set", HTMLSummaryStyle.Resource)} in {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(ModelTyped.GrazeFoodStoreTypeName, "Pasture not set", HTMLSummaryStyle.Resource)}" +
            $" will graze for {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(ModelTyped.HoursGrazed, errorNotSet: true, warnZero: true)} hours of the maximum 8 hours per day.");

    }
}