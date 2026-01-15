using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary;

internal class RuminantParametersGrowPFCISummary : DescriptiveSummaryProviderBase<RuminantParametersGrowPFCI>
{
    /// <inheritdoc/>
    public override void BuildSummary()
    {
    }

    /// <inheritdoc/>
    public override void CreateSummaryClosingBlocks()
    {
        if (!FormatForParentControl)
            base.CreateSummaryClosingBlocks();
    }

    /// <inheritdoc/>
    public override void CreateSummaryOpeningBlocks()
    {
        if (!FormatForParentControl)
            base.CreateSummaryOpeningBlocks();
    }


    /// <inheritdoc/>
    public override void CreateSummaryInnerOpeningBlocksBeforeSummary()
    {
        if (!FormatForParentControl)
            Generator.AddBlockWithText("detailsnote", $"Intake (CI) parameters for GrowPF");
    }

}
