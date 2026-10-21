using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Wordprocessing;
using Models.CLEM.Groupings;
using Models.CLEM.Timers;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary for ActivityTimerSequence component
/// </summary>
public class ActivityTimerSequenceSummary : TimerSummaryBase<ActivityTimerSequence>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        using StringWriter htmlWriter = new();
        if (ModelTyped.Sequence is null || ModelTyped.Sequence == "")
        {
            htmlWriter.Write($"Sequence {generator.DisplayErrorSnippet("Not Set")}");
        }
        else
        {
            htmlWriter.Write("Use sequence: ");
            string seqString = ActivityTimerSequence.FormatSequence(ModelTyped.Sequence);
            for (int i = 0; i < seqString.Length; i++)
            {
                htmlWriter.Write(generator.DisplaySummaryValueSnippet((seqString[i] == '1' ? "OK" : "SKIP"), spanClass: "entryValue filterValue")+" ");
            }
        }
        generator.AddBlockWithText(htmlWriter.ToString(), "entryValue filterItem floatLeft");
    }

    /// <inheritdoc/>
    public override void CreateSummaryOpeningBlocks()
    {
        if (!FormatForParentControl)
            base.CreateSummaryOpeningBlocks();
    }

    /// <inheritdoc/>
    public override void CreateSummaryClosingBlocks()
    {
        if (!FormatForParentControl)
            base.CreateSummaryClosingBlocks();
    }
}