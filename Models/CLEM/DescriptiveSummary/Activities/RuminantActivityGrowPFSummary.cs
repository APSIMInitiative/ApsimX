using Models.CLEM.Activities;
using System.IO;

namespace Models.CLEM.DescriptiveSummary
{
    /// <summary>
    /// Descriptive summary provider for Ruminant Activity GrowPF
    /// </summary>
    public class RuminantActivityGrowPFSummary : RuminantActivitySummaryBase<RuminantActivityGrowPF>
    {
        /// <inheritdoc/>
        public override void BuildSummary()
        {
            generator.AddBlockWithText($"Ruminant growth approach: Grow Protein and Fat (Dougherty et al)");
            generator.AddBlockWithText($"Requires ruminant fat and protein initialisation");

            generator.AddBlockWithText($"Unfed individuals {generator.DisplaySummaryValueSnippet(ModelTyped.ReportUnfed?"are":"are not")} identified for reporting");
        }
    }
}