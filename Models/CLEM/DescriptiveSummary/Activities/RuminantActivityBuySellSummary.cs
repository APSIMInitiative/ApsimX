using Models.CLEM.Activities;
using System.IO;

namespace Models.CLEM.DescriptiveSummary
{
    /// <summary>
    /// Descriptive summary provider for Ruminant Activity Buy/Sell
    /// </summary>
    public class RuminantActivityBuySellSummary : RuminantActivitySummaryBase<RuminantActivityBuySell>
    {
        /// <inheritdoc/>
        public override void BuildSummary()
        {
            var model = ModelTyped;
            if (model is null) return;

            // Add a short explanatory line depending on style wording
            if (!string.IsNullOrWhiteSpace(model.ActivityStyle))
            {
                var styleLower = model.ActivityStyle.ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(styleLower))
                    generator.AddBlockWithText("This activity has no style set.", "infoBanner warning");
                else if (styleLower.Contains("sale"))
                    generator.AddBlockWithText($"Arrange all {generator.DisplaySummaryValueSnippet("Sales")}{generator.DisplayLineBreak()}This activity performs sales of individuals flagged for sale.");
                else if (styleLower.Contains("purchase"))
                    generator.AddBlockWithText($"Arrange all {generator.DisplaySummaryValueSnippet("Purchases")}{generator.DisplayLineBreak()}This activity purchases individuals requested by all activities.");
            }
        }
    }
}