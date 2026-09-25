using Models.CLEM.Timers;
using System.IO;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary for ActivityTimerBreedForMilkingSummary component
/// </summary>
public class ActivityTimerBreedForMilkingSummary : TimerSummaryBase<ActivityTimerBreedForMilking>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        using StringWriter htmlWriter = new();
        htmlWriter.Write($"Timing of breeding and selection of breeders for continuous milk production{((ModelTyped.RestMonths + ModelTyped.ShortenLactationMonths > 0)? $"{generator.DisplayLineBreak()}Allowing ": "")}");
        if (ModelTyped.RestMonths > 0)
        {
            htmlWriter.Write($"{generator.DisplaySummaryValueSnippet(ModelTyped.RestMonths, spanClass: "entryValue entryValue-Other", warnZero: true)} {generator.DisplayPlural(ModelTyped.RestMonths, "month")} rest after lactation");
        }
        if (ModelTyped.ShortenLactationMonths > 0)
        {
            if (ModelTyped.RestMonths > 0)
            {
                // invalid entry
                htmlWriter.Write($" with {generator.DisplayErrorSnippet("conflicting shortening of lactation also provided!")}");
            }
            else
            {
                htmlWriter.Write($"breeding {ModelTyped.ShortenLactationMonths} {generator.DisplayPlural(ModelTyped.ShortenLactationMonths, "month")} before end of lactation");
            }
        }
        generator.AddBlockWithText(htmlWriter.ToString(), "filter");
    }
}
