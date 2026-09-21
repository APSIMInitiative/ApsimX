using Models.CLEM.Activities;
using Models.CLEM.Resources;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// ResourceHolder component descriptive summary
/// </summary>
public class ResourcesHolderSummary : DescriptiveSummaryProviderBase<ResourcesHolder>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        Generator.Append("<h1>Resources summary</h1>");
    }
}
