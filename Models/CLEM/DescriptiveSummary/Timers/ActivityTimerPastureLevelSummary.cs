using DocumentFormat.OpenXml.Wordprocessing;
using Models.CLEM.Timers;
using System.IO;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary for ActivityTimerPastureLevel component
/// </summary>
public class ActivityTimerPastureLevelSummary : TimerSummaryBase<ActivityTimerPastureLevel>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        using StringWriter htmlWriter = new();
        htmlWriter.Write($"Perform when {generator.DisplaySummaryValueSnippet(ModelTyped.GrazeFoodStoreTypeName, "Pasture not set", HTMLSummaryStyle.Resource)}");
        htmlWriter.Write($" is between {generator.DisplaySummaryValueSnippet(ModelTyped.MinimumPastureLevel)} and ");
        if (ModelTyped.MaximumPastureLevel <= ModelTyped.MinimumPastureLevel)
        {
            htmlWriter.Write(generator.DisplayErrorSnippet("must be > MinimumPastureLevel"));
        }
        else
        {
            htmlWriter.Write(generator.DisplaySummaryValueSnippet(ModelTyped.MaximumPastureLevel, warnZero: true));
        }
        htmlWriter.Write(" kg per hectare");
        generator.AddBlockWithText("filter", htmlWriter.ToString());
    }
}
