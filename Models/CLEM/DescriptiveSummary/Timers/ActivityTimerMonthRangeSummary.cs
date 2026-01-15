using Models.CLEM.Timers;
using System.IO;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary for ActivityTimerMonthRange component
/// </summary>
public class ActivityTimerMonthRangeSummary : TimerSummaryBase<ActivityTimerMonthRange>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        generator.AddBlockWithText("filter", $"Perform between {generator.DisplaySummaryValueSnippet(ModelTyped.StartMonth, errorNotSet: true, spanClass:"setvalueextra")} and {generator.DisplaySummaryValueSnippet(ModelTyped.StartMonth, errorNotSet: true, spanClass: "setvalueextra")}");
    }
}
