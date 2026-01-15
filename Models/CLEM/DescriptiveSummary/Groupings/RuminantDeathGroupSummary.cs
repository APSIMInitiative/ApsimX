using Models.CLEM.Groupings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for the Ruminant Group filter
/// </summary>
public class RuminantDeathGroupSummary : GroupSummaryBase<RuminantDeathGroup>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
        generator.AddBlockWithText("activityentry", "Any death of specified individuals is determined using the breed base mortality modified by adult mody condition and the condition of mothers for suckling individuals.");
    }
}
