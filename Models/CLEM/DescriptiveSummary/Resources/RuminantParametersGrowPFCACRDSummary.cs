using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary.Resources
{
    internal class RuminantParametersGrowPFCACRDSummary : DescriptiveSummaryProviderBase<RuminantParametersGrowPFCACRD>
    {
        /// <inheritdoc/>
        public override void BuildSummary(RuminantParametersGrowPFCACRD model)
        {
        }

        /// <inheritdoc/>
        public override void CreateSummaryInnerOpeningBlocksBeforeSummary()
        {
            Generator.AddBlockWithText("detailsnote", $"Digestibility (CA) rumen degradability (CRD) parameters for GrowPF");
        }

    }
}
