using DocumentFormat.OpenXml.Wordprocessing;
using Models.CLEM.Timers;
using System;
using System.IO;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary for ActivityTimerCropHarvest component
/// </summary>
public class ActivityTimerCropHarvestSummary : TimerSummaryBase<ActivityTimerCropHarvest>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        using StringWriter htmlWriter = new();
        if (ModelTyped.OffsetMonthHarvestStart + ModelTyped.OffsetMonthHarvestStop == 0)
        {
            htmlWriter.Write("At harvest");
        }
        else if (ModelTyped.OffsetMonthHarvestStop == 0 && ModelTyped.OffsetMonthHarvestStart < 0)
        {
            htmlWriter.Write($"All {GetMonthsSnippet("start")} before harvest (\"first\" if using HarvestType)");
        }
        else if (ModelTyped.OffsetMonthHarvestStop > 0 && ModelTyped.OffsetMonthHarvestStart == 0)
        {
            htmlWriter.Write($"All {GetMonthsSnippet("stop")} after harvest (\"last\" if using HarvestType)");
        }
        else if (ModelTyped.OffsetMonthHarvestStop == ModelTyped.OffsetMonthHarvestStart)
        {
            htmlWriter.Write($"Perform {GetMonthsSnippet("stop")} {((ModelTyped.OffsetMonthHarvestStop < 0) ? "before \"first\" (if using HarvestType)" : "after \"last\" (if using HarvestType)")} harvest");
        }
        else
        {
            htmlWriter.Write($"Start {GetMonthsSnippet("start")} ");
            htmlWriter.Write((ModelTyped.OffsetMonthHarvestStart > 0) ? "after \"last\" (if using HarvestType) " : "before \"first\" (if using HarvestType) ");
            htmlWriter.Write($" harvest and stop {GetMonthsSnippet("stop")} ");
            htmlWriter.Write((ModelTyped.OffsetMonthHarvestStop > 0) ? "after \"last\" (if using HarvestType)" : "before \"first\" (if using HarvestType)");
        }
        generator.AddBlockWithText(htmlWriter.ToString(), "entryValue filterItem");
    }

    private string GetMonthsSnippet(string startorstop)
    {
        if (startorstop.Equals("start", StringComparison.OrdinalIgnoreCase))
        {
            return $"{generator.DisplaySummaryValueSnippet(Math.Abs(ModelTyped.OffsetMonthHarvestStart))} {generator.DisplayPlural(Math.Abs(ModelTyped.OffsetMonthHarvestStart), "month")}";
        }
        return $"{generator.DisplaySummaryValueSnippet(Math.Abs(ModelTyped.OffsetMonthHarvestStop))} {generator.DisplayPlural(Math.Abs(ModelTyped.OffsetMonthHarvestStop), "month")}";
    }

}
