using Models.CLEM.Groupings;
using System.IO;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for SortByProperty
/// </summary>
public class SortByPropertySummary : FilterSummaryBase<SortByProperty>
{
    /// <inheritdoc/>
    public override string FilterString()
    {
        using (StringWriter sortWriter = new StringWriter())
        {
            sortWriter.Write($"Sort: ");
            sortWriter.Write($"{generator.DisplaySummaryValueSnippet(ModelTyped.PropertyOfIndividual, "Not set", HTMLSummaryStyle.Filter)}");
            sortWriter.Write($" {generator.DisplaySummaryValueSnippet(ModelTyped.SortDirection.ToString().ToLower())}");
            return sortWriter.ToString();
        }
    }
}