using Models.CLEM.Activities;
using System.IO;

namespace Models.CLEM.DescriptiveSummary
{
    /// <summary>
    /// Descriptive summary provider for Ruminant Activity PredictiveStockingENSO
    /// </summary>
    public class RuminantActivityPredictiveStockingENSOSummary : RuminantActivitySummaryBase<RuminantActivityPredictiveStockingENSO>
    {
        /// <inheritdoc/>
        public override void BuildSummary()
        {
            var model = ModelTyped;
            if (model is null) return;

            generator.AddBlockWithText($"Monthly SOI data: {generator.DisplaySummaryValueSnippet(model.MonthlySOIFile, "File not set", HTMLSummaryStyle.FileReader)}");

            generator.AddBlockWithText($"ENSO assessment over {generator.DisplaySummaryValueSnippet(model.AssessMonths, warnZero: true)} months; El Niño cutoff: {generator.DisplaySummaryValueSnippet(model.SOIForElNino, warnZero: true)}, La Niña cutoff: {generator.DisplaySummaryValueSnippet(model.SOIForLaNina, warnZero: true)}.");

            generator.AddBlockWithText($"Minimum pasture (kg/ha) required before restocking: {generator.DisplaySummaryValueSnippet(model.MinimumFeedBeforeRestock, warnZero: true)}");

            generator.AddBlockWithText($"Uses relationships 'PastureToStockingChangeElNino' and 'PastureToStockingChangeLaNina' (if provided) to calculate destock/restock changes; companion models may supply restock/destock ordering.");
        }
    }
}