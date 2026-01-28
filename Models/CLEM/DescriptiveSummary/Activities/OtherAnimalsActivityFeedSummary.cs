using Models.CLEM.Activities;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for Other Animals Activity Cost
/// </summary>
public class OtherAnimalsActivityFeedSummary : DescriptiveSummaryProviderBase<OtherAnimalsActivityFeed>
{
    ///<inheritdoc/>
    public override List<ChildComponentGroup> GetChildrenInSummary()
    {
        return
        [
            new ChildComponentGroup(
                id: "default",
                model: CLEMModel,
                childType: typeof(OtherAnimalsFeedGroup),
                introduction: "The follow individuals will be fed:",
                borderClass: "childgroupborder filtergroup"
                )
        ];
    }

    /// <inheritdoc/>
    public override void BuildSummary()
    {
        generator.AddBlockWithText($"Feed {generator.DisplaySummaryValueSnippet(ModelTyped.FeedTypeName, "Not set", HTMLSummaryStyle.Resource)}");
    }
}
