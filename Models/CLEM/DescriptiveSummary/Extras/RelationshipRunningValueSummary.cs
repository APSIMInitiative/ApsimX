using Models.CLEM.Extras;
using APSIM.Numerics;
using DocumentFormat.OpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary.Extras;

/// <summary>
/// Descriptive summary provider for the Rainfall Shuffler
/// </summary>
public class RelationshipRunningValueSummary : DescriptiveSummaryProviderBase<RelationshipRunningValue>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        using StringWriter htmlWriter = new();
        htmlWriter.Write($"A running value starting at {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(ModelTyped.StartingValue)}");
        htmlWriter.Write($" and ranging between {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(ModelTyped.Minimum)} and ");
        if (MathUtilities.IsLessThanOrEqual(ModelTyped.Maximum, ModelTyped.Minimum))
        {
            htmlWriter.Write(DescriptiveSummaryGenerator.DisplayErrorSnippet("Invalid"));
        }
        else
        {
            htmlWriter.Write($"{DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(ModelTyped.Maximum)}");
        }
        generator.AddBlockWithText(htmlWriter.ToString());
    }
}
