using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary;

internal class RuminantParametersGrowPFCDSummary : DescriptiveSummaryProviderBase<RuminantParametersGrowPFCD>
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
            Generator.AddBlockWithText("detailsnote", $"Death (CD) parameters used only by GrowPF");
    }

}
