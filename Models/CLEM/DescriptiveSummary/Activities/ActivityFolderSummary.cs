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
        generator.AddBlockWithText($"{ModelTyped.Name} folder {((ModelTyped.Enabled == false) ? " - DISABLED!" : "")}", "folder");
    }

    /// <inheritdoc/>
    public override void CreateSummaryOpeningBlocks()
    {
        generator.OpenBlock("activityborder", styleString: $"opacity: {SummaryOpacity()};", id: $"{ModelTyped.Name}_main");
    }
}
