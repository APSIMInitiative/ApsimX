using Models.CLEM.Activities;
using Models.CLEM.Resources;

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
        generator.OpenBlock("activity", styleString: $"opacity: {model.SummaryOpacity(FormatForParentControl)};", id: $"{model.Name}_main");
    }

    /// <inheritdoc/>
    public override void CreateSummaryClosingBlocks()
    {
        generator.CloseMostRecentBlock(id: $"{ModelTyped.Name}_main");
    }

}
