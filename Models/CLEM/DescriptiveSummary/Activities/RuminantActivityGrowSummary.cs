using Models.CLEM.Activities;
using System.IO;

namespace Models.CLEM.DescriptiveSummary
{
    /// <summary>
    /// Descriptive summary provider for Ruminant Activity Grow
    /// </summary>
    public class RuminantActivityGrowSummary : RuminantActivitySummaryBase<RuminantActivityGrow>
    {
        /// <inheritdoc/>
        public override void BuildSummary()
        {
            var model = ModelTyped;
            if (model is null) return;

            generator.AddBlockWithText($"Ruminant growth approach: Original CLEM growth as used in IAT and NABSA models");

            generator.AddBlockWithText($"A gross energy content of {generator.DisplaySummaryValueSnippet(model.EnergyGross, errorNotSet: true)} MJ/kg dry matter is used for all feed.");
        }
    }
}