using DocumentFormat.OpenXml.Spreadsheet;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary.Resources
{
    internal class RuminantParametersGrowPFCGSummary : DescriptiveSummaryProviderBase<RuminantParametersGrowPFCG>
    {
        /// <inheritdoc/>
        public override void BuildSummary()
        {
        }

        /// <inheritdoc/>
        public override void CreateSummaryInnerOpeningBlocksBeforeSummary()
        {
            Generator.AddBlockWithText("detailsnote", $"Growth (CG) parameters for GrowPF");
        }

    }
}
