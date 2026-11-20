using Models.CLEM.Groupings;
using System.IO;

namespace Models.CLEM.DescriptiveSummary.Groupings;

/// <summary>
/// Descriptive summary provider for SortByProperty
/// </summary>
public class SortByPropertySummary : FilterSummaryBase<SortByProperty>
{
    /// <inheritdoc/>
    public override string FilterString()
    {
        using StringWriter sortWriter = new();
        sortWriter.Write($"Sort: ");
        sortWriter.Write($"{DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(ModelTyped.PropertyOfIndividual, "Not set", HTMLSummaryStyle.Filter)}");
        sortWriter.Write($" {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(ModelTyped.SortDirection.ToString().ToLower())}");
        return sortWriter.ToString();
    }
}