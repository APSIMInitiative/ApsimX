using Models.CLEM.Activities;
using Models.CLEM.Resources;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Activity Folder component descriptive summary
/// </summary>
public class ActivityFolderSummary : DescriptiveSummaryProviderBase<ActivityFolder>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        generator.AddBlockWithText("folder",  $"{ModelTyped.Name} folder {((ModelTyped.Enabled == false) ? " - DISABLED!" : "")}");
    }

    /// <inheritdoc/>
    public override void CreateSummaryOpeningBlocks()
    {
        generator.OpenBlock("activityborder", styleString: $"opacity: {ModelTyped.SummaryOpacity(FormatForParentControl)};", id: $"{ModelTyped.Name}_main");
    }
}
