using APSIM.Numerics;
using DocumentFormat.OpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for the Rainfall Shuffler
/// </summary>
public class RelationshipRunningValueSummary : DescriptiveSummaryProviderBase<RelationshipRunningValue>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        using StringWriter htmlWriter = new();
        htmlWriter.Write($"A running value starting at {generator.DisplaySummaryValueSnippet(ModelTyped.StartingValue)}");
        htmlWriter.Write($" and ranging between {generator.DisplaySummaryValueSnippet(ModelTyped.Minimum)} and ");
        if (MathUtilities.IsLessThanOrEqual(ModelTyped.Maximum, ModelTyped.Minimum))
        {
            htmlWriter.Write(generator.DisplayErrorSnippet("Invalid"));
        }
        else
        {
            htmlWriter.Write($"{generator.DisplaySummaryValueSnippet(ModelTyped.Maximum)}");
        }
        generator.AddBlockWithText(htmlWriter.ToString());
    }
}
