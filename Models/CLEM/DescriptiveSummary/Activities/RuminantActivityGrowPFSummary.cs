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
            generator.AddBlockWithText("activityentry", $"Ruminant growth approach: Grow Protein and Fat (Dougherty et al)");
            generator.AddBlockWithText("activityentry", $"Requires ruminant fat and protein initialisation");

            generator.AddBlockWithText("activityentry",
                $"Unfed individuals {generator.DisplaySummaryValueSnippet(ModelTyped.ReportUnfed?"Are":"Are not")} identified for reporting");
        }
    }
}