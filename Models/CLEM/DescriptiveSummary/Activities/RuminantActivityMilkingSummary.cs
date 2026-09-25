using Models.CLEM.Activities;
using System.IO;

namespace Models.CLEM.DescriptiveSummary
{
    /// <summary>
    /// Descriptive summary provider for Ruminant Activity Milking
    /// </summary>
    public class RuminantActivityMilkingSummary : RuminantActivitySummaryBase<RuminantActivityMilking>
    {
        /// <inheritdoc/>
        public override void BuildSummary()
        {
            var model = ModelTyped;
            if (model is null) return;

            generator.AddBlockWithText($"Milk will be placed in {generator.DisplaySummaryValueSnippet(model.ResourceTypeName, "Store not set", HTMLSummaryStyle.Resource)}.");
        }
    }
}