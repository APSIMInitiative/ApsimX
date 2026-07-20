using Models.CLEM.Groupings;
using System.IO;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for SortByAttribute
/// </summary>
public class SortByAttributeSummary : FilterSummaryBase<SortByAttribute>
{
    /// <inheritdoc/>
    public override string FilterString()
    {
        using (StringWriter sortWriter = new StringWriter())
        {
            sortWriter.Write($"Sort: Attribute-");
            sortWriter.Write($" {generator.DisplaySummaryValueSnippet(ModelTyped.AttributeTag, "Not set", HTMLSummaryStyle.Filter)}");
            sortWriter.Write($" {generator.DisplaySummaryValueSnippet(ModelTyped.FilterStyle.ToString().ToLower())}");
            sortWriter.Write($" {generator.DisplaySummaryValueSnippet(ModelTyped.SortDirection.ToString().ToLower())}");
            return sortWriter.ToString();
        }
    }
}