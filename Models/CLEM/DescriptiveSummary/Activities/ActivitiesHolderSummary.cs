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
}
