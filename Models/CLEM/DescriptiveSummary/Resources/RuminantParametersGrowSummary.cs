using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary.Resources
{
    internal class RuminantParametersGrowSummary : DescriptiveSummaryProviderBase<RuminantParametersGrow>
    {
        /// <inheritdoc/>
        public override void BuildSummary(RuminantParametersGrow model)
        {
        }

        /// <inheritdoc/>
        public override void CreateSummaryInnerOpeningBlocksBeforeSummary()
        {
            Generator.AddBlockWithText("detailsnote", $"Parameters required for orginal ruminant growth activity (RuminantantActivityGrow)");
        }

    }
}
