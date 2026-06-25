using Models.CLEM.Activities;
using System.IO;

namespace Models.CLEM.DescriptiveSummary
{
    /// <summary>
    /// Descriptive summary provider for Ruminant Activity Grow (SCA07)
    /// </summary>
    public class RuminantActivityGrowSCA07Summary : RuminantActivitySummaryBase<RuminantActivityGrowSCA07>
    {
        /// <inheritdoc/>
        public override void BuildSummary()
        {
            var model = ModelTyped;
            if (model is null) return;

            generator.AddBlockWithText($"Ruminant growth approach: SCA 2007 (SCA07 equations)");
        }
    }
}