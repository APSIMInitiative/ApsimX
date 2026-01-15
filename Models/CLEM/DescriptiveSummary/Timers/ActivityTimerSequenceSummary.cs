using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Wordprocessing;
using Models.CLEM.Timers;
using System.IO;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary for ActivityTimerSequence component
/// </summary>
public class ActivityTimerSequenceSummary : TimerSummaryBase<ActivityTimerSequence>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        using (generator.OpenBlock("filter"))
        {
            if (ModelTyped.Sequence is null || ModelTyped.Sequence == "")
            {
                generator.Append($"Sequence {generator.DisplayErrorSnippet("Not Set")}");
            }
            else
            {
                generator.DisplaySummaryValueSnippet("Use sequence", spanClass: "filtersettitle");
                string seqString = ActivityTimerSequence.FormatSequence(ModelTyped.Sequence);
                for (int i = 0; i < seqString.Length; i++)
                {
                    generator.DisplaySummaryValueSnippet((seqString[i] == '1' ? "OK" : "SKIP"), spanClass: "filterset");
                }
            }
        }
    }
}