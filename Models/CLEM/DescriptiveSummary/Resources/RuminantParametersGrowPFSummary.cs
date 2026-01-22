using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Ruminant Parameters component descriptive summary
/// </summary>
public class RuminantParametersGrowPFSummary : RuminantParametersSummaryBase<RuminantParametersGrowPF>
{
    /// <summary>
    /// Constructor
    /// </summary>
    public RuminantParametersGrowPFSummary()
    {
        SummaryStyle = HTMLSummaryStyle.SubResource;
    }

    /// <inheritdoc/>
    public override void BuildSummary()
    {
    }

    /// <inheritdoc/>
    public override void CreateSummaryInnerOpeningBlocksBeforeSummary()
    {
        Generator.AddBlockWithText("detailsnote", $"Holds all parameter groups relating to the ruminant Protein and fat growth component");
    }

}
