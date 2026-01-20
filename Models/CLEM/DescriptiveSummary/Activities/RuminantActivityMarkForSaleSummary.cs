using Models.CLEM.Activities;
using System.IO;

namespace Models.CLEM.DescriptiveSummary
{
    /// <summary>
    /// Descriptive summary provider for Ruminant Activity MarkForSale
    /// </summary>
    public class RuminantActivityMarkForSaleSummary : RuminantActivitySummaryBase<RuminantActivityMarkForSale>
    {
        /// <inheritdoc/>
        public override void BuildSummary()
        {
            var model = ModelTyped;
            if (model is null) return;

            generator.AddBlockWithText("activityentry",
                $"Flag individuals for sale as {generator.DisplaySummaryValueSnippet(model.SaleFlagToUse.ToString(), "SaleFlag not set")}");

            if (model.OverwriteFlag)
            {
                generator.AddBlockWithText("activityentry", "Existing sale flags will be overwritten.");
            }
            else
            {
                generator.AddBlockWithText("activityentry", "Existing sale flags will be preserved.");
            }
        }
    }
}