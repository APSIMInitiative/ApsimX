using DocumentFormat.OpenXml.Wordprocessing;
using Models.CLEM.Timers;
using System.IO;

namespace Models.CLEM.DescriptiveSummary.Timers;

/// <summary>
/// Descriptive summary for ActivityTimerPastureLevel component
/// </summary>
public class ActivityTimerPastureLevelSummary : TimerSummaryBase<ActivityTimerPastureLevel>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        using StringWriter htmlWriter = new();
        htmlWriter.Write($"Perform when {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(ModelTyped.GrazeFoodStoreTypeName, "Pasture not set", HTMLSummaryStyle.Resource)}");
        htmlWriter.Write($" is between {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(ModelTyped.MinimumPastureLevel)} and ");
        if (ModelTyped.MaximumPastureLevel <= ModelTyped.MinimumPastureLevel)
        {
            htmlWriter.Write(DescriptiveSummaryGenerator.DisplayErrorSnippet("must be > MinimumPastureLevel"));
        }
        else
        {
            htmlWriter.Write(DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(ModelTyped.MaximumPastureLevel, warnZero: true));
        }
        htmlWriter.Write(" kg per hectare");
        generator.AddBlockWithText(htmlWriter.ToString(), "entryValue filterItem");
    }
}
