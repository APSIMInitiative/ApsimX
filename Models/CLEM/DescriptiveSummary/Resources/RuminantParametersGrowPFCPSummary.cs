using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary.Resources
{
    internal class RuminantParametersGrowPFCPSummary : DescriptiveSummaryProviderBase<RuminantParametersGrowPFCP>
    {
        /// <inheritdoc/>
        public override void BuildSummary(RuminantParametersGrowPFCP model)
        {
        }

        /// <inheritdoc/>
        public override void CreateSummaryInnerOpeningBlocksBeforeSummary()
        {
            Generator.AddBlockWithText("detailsnote", $"Pregnancy (CP) parameters for GrowPF");
        }

    }
}
