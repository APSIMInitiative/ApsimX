using Models.CLEM.Activities;
using Models.CLEM.Groupings;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for Labour Activity Feed
/// </summary>
public class LabourActivityFeedSummary : DescriptiveSummaryProviderBase<LabourActivityFeed>
{
    ///<inheritdoc/>
    public override List<ChildComponentGroup> GetChildrenInSummary()
    {
        return
        [
            new ChildComponentGroup(
                id: "default",
                model: CLEMModel,
                childType: typeof(LabourFeedGroup),
                introduction: "The following groups will be fed:",
                borderClass: "childgroupactivityborder"
                )
        ];
    }

    /// <inheritdoc/>
    public override void BuildSummary()
    {
        generator.AddBlockWithText("activityentry", $"Feed people {generator.DisplaySummaryResourceTypeSnippet(ModelTyped.FeedTypeName, "Not set")}");
    }
}
