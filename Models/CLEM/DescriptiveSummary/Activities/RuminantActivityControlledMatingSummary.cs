using Models.CLEM.Activities;
using System.IO;

namespace Models.CLEM.DescriptiveSummary.Activities;

/// <summary>
/// Descriptive summary provider for Ruminant Activity Controlled Mating
/// </summary>
public class RuminantActivityControlledMatingSummary : RuminantActivitySummaryBase<RuminantActivityControlledMating>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        var model = ModelTyped;
        if (model is null) return;

        generator.AddBlockWithText($"Maximum female age for mating is {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.MaximumAgeMating.InDays, errorNotSet: true)} days");

        generator.AddBlockWithText($"{DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(model.JoiningsPerMale, warnZero: true)} joinings per individual male per day (genetics) are allowed");
    }
}