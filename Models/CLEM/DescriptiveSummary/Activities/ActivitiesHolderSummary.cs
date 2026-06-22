using Models.CLEM.Activities;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Activities Holder component descriptive summary
/// </summary>
public class ActivitiesHolderSummary : DescriptiveSummaryProviderBase<ActivitiesHolder>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        generator.Append("<h1>Activities summary</h1>");
    }

    /// <inheritdoc/>
    public override void CreateSummaryOpeningBlocks()
    {
        var model = ModelTyped;
        if (model is null) return;
        generator.OpenBlock("activity", styleString: $"opacity: {SummaryOpacity()};", id: $"{model.Name}_main");
    }

    /// <inheritdoc/>
    public override void CreateSummaryClosingBlocks()
    {
        generator.CloseMostRecentBlock(id: $"{ModelTyped.Name}_main");
    }

}
