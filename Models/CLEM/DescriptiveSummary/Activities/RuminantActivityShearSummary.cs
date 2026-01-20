using System.IO;
using Models.CLEM.Activities;
using Models.CLEM.Resources;

namespace Models.CLEM.DescriptiveSummary
{
    /// <summary>
    /// Descriptive summary provider for Ruminant Activity Shear
    /// </summary>
    public class RuminantActivityShearSummary : RuminantActivitySummaryBase<RuminantActivityShear>
    {
        /// <inheritdoc/>
        public override void BuildSummary()
        {
            var model = ModelTyped;
            if (model is null) return;

            generator.AddBlockWithText("activityentry",
                $"Shear selected herd and place wool clip in {generator.DisplaySummaryValueSnippet(model.WoolProductStoreName, "Store Type not set", HTMLSummaryStyle.Resource)}");

            if (model.ProportionFleeceRemoved < 1.0)
                generator.AddBlockWithText("activityentry", $"Only {generator.DisplaySummaryValueSnippet(model.ProportionFleeceRemoved)} proportion of the fleece is removed.");
        }
    }
}