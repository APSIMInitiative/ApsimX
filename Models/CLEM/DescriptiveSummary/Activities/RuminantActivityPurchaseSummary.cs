using Models.CLEM.Activities;
using Models.CLEM.Resources;
using System.IO;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary
{
    /// <summary>
    /// Descriptive summary provider for Ruminant Activity Purchase
    /// </summary>
    public class RuminantActivityPurchaseSummary : RuminantActivitySummaryBase<RuminantActivityPurchase>
    {
        /// <inheritdoc/>
        public override void BuildSummary()
        {
            var model = ModelTyped;
            if (model is null) return;

            generator.AddBlockWithText("activityentry", $"Purchased individuals will be placed in {generator.DisplaySummaryValueSnippet(model.GrazeFoodStoreName, "Not specified - general yards", HTMLSummaryStyle.Resource)}");

            Relationship numberRelationship = ModelTyped.Structure.FindChildren<Relationship>().Where(a => a.Identifier == "Number to stock vs pasture").FirstOrDefault();
            if (string.IsNullOrWhiteSpace(ModelTyped.GrazeFoodStoreName) == false && ModelTyped.GrazeFoodStoreName.StartsWith("Not specified") == false && numberRelationship != null)
            {
                generator.AddBlockWithText("activityentry", $"The relationship {generator.DisplaySummaryValueSnippet(numberRelationship.Name)} will be used to calculate numbers purchased based on destination pasture biomass (t\\ha)");
            }
            else
            {
                generator.AddBlockWithText("activityentry", $"{generator.DisplaySummaryValueSnippet(model.NumberToPurchase, warnZero: true)} individuals will be purchased with cohorts able to determine proportional breakdown.");
            }

            if (!string.IsNullOrWhiteSpace(model.TagLabel))
                generator.AddBlockWithText("activityentry", $"Purchased individuals will be assigned the tag {generator.DisplaySummaryValueSnippet(model.TagLabel)} for identification.");
        }
    }
}