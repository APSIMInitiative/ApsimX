using Models.CLEM.Timers;

namespace Models.CLEM.DescriptiveSummary.Timers;

/// <summary>
/// Descriptive summary provider for ActivityTimerLinked component
/// </summary>
public class ActivityTimerLinkedSummary : TimerSummaryBase<ActivityTimerLinked>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        // Minimal safe summary using key properties
        Generator.AddBlockWithText($"Links to existing timer {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(ModelTyped.ExistingTimerName, errorString: "No timer selected", errorNotSet: true)}", "entryValue filterItem");
    }
}