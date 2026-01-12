using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary.Resources
{
    internal class RuminantParametersMethaneCharmleySummary : DescriptiveSummaryProviderBase<RuminantParametersMethaneCharmley>
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
                Generator.AddBlockWithText("detailsnote", $"Parameters for the Chermley et al method of calculating enteric methane emissions");
        }

    }
}
