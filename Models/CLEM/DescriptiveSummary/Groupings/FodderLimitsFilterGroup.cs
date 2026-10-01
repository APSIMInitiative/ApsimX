using Models.CLEM.Groupings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for the Fodder limits filter group
/// </summary>
public class FodderLimitsFilterGroupSummary : GroupSummaryBase<FodderLimitsFilterGroup>
{
    /// <summary>
    /// Constructor
    /// </summary>
    public FodderLimitsFilterGroupSummary()
    {
        SummaryStyle = HTMLSummaryStyle.Filter;
    }

    /// <inheritdoc/>
    public override void BuildSummary()
    {
        generator.AddBlockWithText("The following ruminants will have a set monthly proportion of intake that can come from each pasture age pool");
    }
}
