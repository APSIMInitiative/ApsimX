using Models.CLEM.Activities;
using System.IO;

namespace Models.CLEM.DescriptiveSummary
{
    /// <summary>
    /// Descriptive summary provider for Ruminant Activity HerdCost
    /// </summary>
    public class RuminantActivityHerdCostSummary : RuminantActivitySummaryBase<RuminantActivityHerdCost>
    {
        /// <inheritdoc/>
        public override void BuildSummary()
        {
            var model = ModelTyped;
            if (model is null) return;

            generator.AddBlockWithText("activityentry",
                "Arranges payment of herd expenses (e.g. vet fees) using companion models (ActivityFee, LabourRequirement).");

            generator.AddBlockWithText("activityentry",
                "Applies the companion labels to calculate fixed/per-head/per-AE costs where configured.");
        }
    }
}