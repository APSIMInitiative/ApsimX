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
public class RuminantGroupSummary : GroupSummaryBase<RuminantGroup>
{
    /// <summary>
    /// Constructor
    /// </summary>
    public RuminantGroupSummary()
    {
        SummaryStyle = HTMLSummaryStyle.Filter;
    }

    /// <inheritdoc/>
    public override void BuildSummary()
    {
    }
}
