using Models.CLEM.Groupings;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for SortRandom
/// </summary>
public class SortRandomSummary : FilterSummaryBase<SortRandom>
{
    /// <inheritdoc/>
    public override string FilterString(bool htmltags)
    {
        if (htmltags)
        {
            return "<span class = \"filterset\">Randomise order</span>";
        }
        return "Randomise order";
    }
}