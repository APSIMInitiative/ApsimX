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
            generator.AddBlockWithText($"Ruminant growth approach: Oddy et al protein driven growth approach");
            generator.AddBlockWithText($"Requires ruminant fat and protein allocation to muscle and viscera.");
            generator.AddBlockWithText($"Test implementation. Only valid for individuals currently in growth phase.", "infoBanner warning");
        }
    }
}