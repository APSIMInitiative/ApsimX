using Models.CLEM.Activities;
using System.IO;

namespace Models.CLEM.DescriptiveSummary
{
    /// <summary>
    /// Descriptive summary provider for Ruminant Activity GrazeAll
    /// </summary>
    public class RuminantActivityGrazeAllSummary : RuminantActivitySummaryBase<RuminantActivityGrazeAll>
    {
        /// <inheritdoc/>
        public override void BuildSummary()
        {
            var model = ModelTyped;
            if (model is null) return;

            generator.AddBlockWithText("activityentry",
                $"Grazes all herds and pastures for {generator.DisplaySummaryValueSnippet(model.HoursGrazed, warnZero: true)} hours of the maximum 8 hours per day.");

            generator.AddBlockWithText("defaultbanner", "This activity creates grazing sub-activities for each pasture/breed combination present in the simulation.");
        }
    }
}