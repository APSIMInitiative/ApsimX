using Models.CLEM.Activities;
using Models.CLEM.Groupings;
using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for Labour Activity Feed To Targets
/// </summary>
public class LabourActivityFeedToTargetsSummary : DescriptiveSummaryProviderBase<LabourActivityFeedToTargets>
{
    ///<inheritdoc/>
    public override List<ChildComponentGroup> GetChildrenInSummary()
    {
        return
        [
            new ChildComponentGroup(
                id: "targets",
                model: CLEMModel,
                childType: typeof(LabourActivityFeedTarget),
                introduction: "The following targets are applied:",
                borderClass: "childgroupactivityborder"
                ),
            new ChildComponentGroup(
                id: "purchases",
                model: CLEMModel,
                childType: typeof(LabourActivityFeedTargetPurchase),
                introduction: "The following purchases will be used to supply food:",
                missing: "",
                borderClass: "childgroupactivityborder"
                ),
        ];
    }

    /// <inheritdoc/>
    public override void BuildSummary()
    {
        string output = $"Each Adult Equivalent is able to consume {generator.DisplaySummaryValueSnippet(ModelTyped.DailyIntakeLimit, warnZero: true)} kg per day";
        if (ModelTyped.DailyIntakeOtherSources > 0)
        {
            output += $"{generator.DisplaySummaryValueSnippet(ModelTyped.DailyIntakeOtherSources)} provided from non-modelled sources";
        }
        generator.AddBlockWithText("activityentry", output);

        generator.AddBlockWithText("activityentry", $"Hired labour {generator.DisplaySummaryValueSnippet(((ModelTyped.IncludeHiredLabour) ? "is" : "is not"))} included");

        // find a market place if present
        Simulation sim = ModelTyped.Structure.FindParent<Simulation>(recurse: true);
        if (sim != null)
        {
            Market marketPlace = ModelTyped.Structure.FindChild<Market>(relativeTo: sim);
            if (marketPlace != null)
            {
                generator.AddBlockWithText("activityentry", $"Food with be bought and sold through the market {generator.DisplaySummaryResourceTypeSnippet(marketPlace.Name)} included");
            }
        }
    }
}
