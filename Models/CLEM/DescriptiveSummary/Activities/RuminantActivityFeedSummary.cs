using Models.CLEM.Activities;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for Ruminant Activity Feed
/// </summary>
public class RuminantActivityFeedSummary : DescriptiveSummaryProviderBase<RuminantActivityFeed>
{
    ///<inheritdoc/>
    public override List<ChildComponentGroup> GetChildrenInSummary()
    {
        var model = ModelTyped;
        if (model is null) return [];

        return
        [
            new ChildComponentGroup(
                id: "default",
                model: CLEMModel,
                childType: typeof(RuminantFeedGroup),
                missing: "",
                introduction: "The following individuals will be fed:",
                borderClass: "filterborder"
                )
        ];
    }

    /// <inheritdoc/>
    public override void BuildSummary()
    {
        using StringWriter htmlWriter = new();
        generator.AddBlockWithText("activityentry", $"Feed ruminants {generator.DisplaySummaryValueSnippet(ModelTyped.FeedTypeName, "Feed not set", HTMLSummaryStyle.Resource)}");
        if (ModelTyped.ProportionTramplingWastage > 0)
        {
            generator.AddBlockWithText("activityentry", $"{generator.DisplaySummaryValueSnippet(ModelTyped.ProportionTramplingWastage)} is lost through trampling");
        }
    }
}
