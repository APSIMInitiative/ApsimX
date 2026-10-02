using Models.CLEM.Groupings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for the Ruminant Group filter
/// </summary>
public class RuminantActivityGroupSummary : GroupSummaryBase<RuminantActivityGroup>
{
    /// <summary>
    /// Constructor
    /// </summary>
    public RuminantActivityGroupSummary()
    {
        SummaryStyle = HTMLSummaryStyle.SubActivity;
    }

    /// <inheritdoc/>
    public override void BuildSummary()
    {
        generator.AddBlockWithText("This ruminant filter is applied to this activity and all activities within this branch");
    }
}
