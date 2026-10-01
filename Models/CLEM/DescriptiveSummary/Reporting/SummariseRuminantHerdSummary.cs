using Models.CLEM.Activities;
using Models.CLEM.Reporting;
using System;
using System.IO;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for Summarise Ruminant Herd report
/// </summary>
public class SummariseRuminantHerdSummary : DescriptiveSummaryProviderBase<SummariseRuminantHerd>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        using StringWriter htmlWriter = new();
        htmlWriter.Write("This will report individuals ");
        if (ModelTyped.AddGroupByLocation)
        {
            htmlWriter.Write("based on location and ");
        }

        switch (ModelTyped.GroupStyle)
        {
            case SummarizeRuminantHerdStyle.Classic:
                htmlWriter.Write("with age in years and a column for sex");
                break;
            case SummarizeRuminantHerdStyle.ByClass:
                htmlWriter.Write("grouped by class");
                break;
            case SummarizeRuminantHerdStyle.BySexClass:
                htmlWriter.Write("grouped by a combination of sex and class");
                break;
            case SummarizeRuminantHerdStyle.ByAgeYears:
                htmlWriter.Write("grouped by age (in years)");
                break;
            case SummarizeRuminantHerdStyle.ByAgeMonths:
                htmlWriter.Write("grouped by age (in months)");
                break;
            default:
                break;
        }

        generator.AddBlockWithText(htmlWriter.ToString());
    }
}
