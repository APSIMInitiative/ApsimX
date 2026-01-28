using Models.CLEM.Activities;
using Models.CLEM.Groupings;
using System.Collections.Generic;
using System.IO;

namespace Models.CLEM.DescriptiveSummary
{
    /// <summary>
    /// Descriptive summary provider for Ruminant Activity PredictiveStocking
    /// </summary>
    public class RuminantActivityPredictiveStockingSummary : RuminantActivitySummaryBase<RuminantActivityPredictiveStocking>
    {
        ///<inheritdoc/>
        public override List<ChildComponentGroup> GetChildrenInSummary()
        {
            return
            [
                new ChildComponentGroup(
                id: "default",
                model: CLEMModel,
                childType: typeof(RuminantGroup),
                missing: "No individuals identified for destocking",
                introduction: "The following filter groups will identify selection rules and order for destocking",
                borderClass: "childgroupborder filtergroup"
                )
            ];
        }

        /// <inheritdoc/>
        public override void BuildSummary()
        {
            var model = ModelTyped;
            if (model is null) return;

            generator.AddBlockWithText($"Pasture will be assessed in months defined by a Timer and assessed until {generator.DisplaySummaryValueSnippet(model.LastAssessmentMonth, warnZero: true)} to maintain {generator.DisplaySummaryValueSnippet(model.FeedLowLimit, warnZero: true)} kg/ha at the end of the period.");
            generator.AddBlockWithText($"This activity evaluates paddock biomass and marks individuals for destocking if required. Companion models may be used to specify sell order and costs.");
        }
    }
}