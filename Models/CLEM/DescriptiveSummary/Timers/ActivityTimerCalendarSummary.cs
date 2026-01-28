using DocumentFormat.OpenXml.Drawing.Diagrams;
using DocumentFormat.OpenXml.Wordprocessing;
using ExCSS;
using Models.CLEM.Timers;
using System.IO;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary for ActivityTimerCalendar component
/// </summary>
public class ActivityTimerCalendarSummary : TimerSummaryBase<ActivityTimerCalendar>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        Clock clock = ModelTyped.Structure.Find<Clock>();
        CLEMEvents clemEvents = ModelTyped.Structure.Find<CLEMEvents>();
        clemEvents.SetInterval();
        clemEvents.Clock = clock;
        if (clock is null || clemEvents is null)
        {
            generator.AddBlockWithText(generator.DisplayErrorSnippet("No Clock or CLEM Events component found above Timer"), "entryValue filterError");
            return;
        }

        if (ModelTyped.StartDetails.Parts[0] > 0 & ModelTyped.StartDetails.Parts[1] == 0)
        {
            generator.AddBlockWithText(generator.DisplayErrorSnippet("Invalid date component specified. Missing month value"), "entryValue filterError");
            return;
        }

        TimerRange range = new(clemEvents, ModelTyped.StartDetails, ModelTyped.EndDetails, ModelTyped.RepeatInterval, ModelTyped.WholeTimeStepMustBeInRange, ModelTyped.Structure.FindChildren<ActivityTimerSequence>(), true);

        using StringWriter htmlWriter = new();

        string invertString = (ModelTyped.Invert) ? "when <b>NOT</b> " : "";

        if (range.Start.ymd.month == range.End.ymd.month & (range.Start.IsMonthOnly | range.Start.ymd.day == range.End.ymd.day))
        {
            if (range.Start.ymd.month > 0 & range.Start.IsMonthOnly)
            {
                htmlWriter.Write($"Perform {invertString} in ");
            }
            else
            {
                htmlWriter.Write($"Perform {invertString} on ");
            }
        }
        else
        {
            htmlWriter.Write($"Perform {invertString} between ");
        }

        if (range.Start.ErrorMessages.Count() > 0)
        {
            htmlWriter.Write(generator.DisplayErrorSnippet(range.Start.ErrorMessages.First()));
        }
        else
        {
            htmlWriter.Write(generator.DisplaySummaryValueSnippet(range.Start, spanClass: "entryValue entryValue-Other"));
        }

        if (range.Start.ymd.month != range.End.ymd.month | (range.Start.IsMonthOnly == false & range.Start.ymd.day != range.End.ymd.day))
        {
            htmlWriter.Write($" and ");
            if (range.End.ErrorMessages.Count() > 0)
            {
                htmlWriter.Write(generator.DisplayErrorSnippet(range.End.ErrorMessages.First()));
            }
            else
            {
                htmlWriter.Write(generator.DisplaySummaryValueSnippet(range.End, spanClass: "entryValue entryValue-Other"));
            }
        }

        if (range.IsFloatingRange)
        {
            htmlWriter.Write(generator.DisplaySummaryValueSnippet(range.RepeatIntervalToString(), spanClass: "entryValue entryValue-Other"));
        }

        if (range.WholeTimeStepInRange)
        {
            htmlWriter.Write(generator.DisplaySummaryValueSnippet("where whole time step must be in range", spanClass: "entryValue entryValue-Other"));
        }

        generator.AddBlockWithText(htmlWriter.ToString(), "entryValue filterItem");
    }
}
