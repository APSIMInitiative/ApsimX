using Models.CLEM.Activities;
using System.IO;

namespace Models.CLEM.DescriptiveSummary
{
    /// <summary>
    /// Descriptive summary provider for Ruminant Activity Grow (Oddy)
    /// </summary>
    public class RuminantActivityGrowOddySummary : RuminantActivitySummaryBase<RuminantActivityGrowOddy>
    {
        /// <inheritdoc/>
        public override void BuildSummary()
        {
            generator.AddBlockWithText("activityentry", $"Ruminant growth approach: Oddy et al protein driven growth approach");
            generator.AddBlockWithText("activityentry", $"Requires ruminant fat and protein allocation to muscle and viscera.");
            generator.AddBlockWithText("warningbanner", $"Test implementation. Only valid for individuals currently in growth phase.");
        }
    }
}