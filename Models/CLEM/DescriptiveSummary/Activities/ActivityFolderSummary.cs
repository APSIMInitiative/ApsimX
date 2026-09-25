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
        Generator.Append($"<h3>{ModelTyped.Name} folder {((ModelTyped.Enabled == false) ? " - DISABLED!" : "")}</h3>");
    }
}
