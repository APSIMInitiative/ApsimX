using Models.CLEM.Groupings;
using System.IO;

namespace Models.CLEM.DescriptiveSummary.Groupings;

/// <summary>
/// Descriptive summary provider for SortByAttribute
/// </summary>
public class SortByAttributeSummary : FilterSummaryBase<SortByAttribute>
{
    /// <inheritdoc/>
    public override string FilterString()
    {
        using StringWriter sortWriter = new();
        sortWriter.Write($"Sort: Attribute-");
        sortWriter.Write($" {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(ModelTyped.AttributeTag, "Not set", HTMLSummaryStyle.Filter)}");
        sortWriter.Write($" {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(ModelTyped.FilterStyle.ToString().ToLower())}");
        sortWriter.Write($" {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(ModelTyped.SortDirection.ToString().ToLower())}");
        return sortWriter.ToString();
    }
}