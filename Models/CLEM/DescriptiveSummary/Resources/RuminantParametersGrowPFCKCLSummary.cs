using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary.Resources
{
    internal class RuminantParametersGrowPFCKCLSummary : DescriptiveSummaryProviderBase<RuminantParametersGrowPFCKCL>
    {
        /// <inheritdoc/>
        public override void BuildSummary(RuminantParametersGrowPFCKCL model)
        {
        }

        /// <inheritdoc/>
        public override void CreateSummaryInnerOpeningBlocksBeforeSummary()
        {
            Generator.AddBlockWithText("detailsnote", $"Efficiency (CK) and lactation (CL) parameters for GrowPF");
        }

    }
}
