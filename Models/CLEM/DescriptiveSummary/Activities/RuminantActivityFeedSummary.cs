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
public class RuminantActivityFeedSummary : RuminantActivitySummaryBase<RuminantActivityFeed>
{
    ///<inheritdoc/>
    public override List<ChildComponentGroup> GetChildrenInSummary()
    {
        return
        [
            new ChildComponentGroup(
                id: "default",
                model: CLEMModel,
                childType: typeof(RuminantFeedGroup),
                missing: "",
                introduction: "The following individuals will be fed:",
                borderClass: "childgroupfilterborder"
                )
        ];
    }

    /// <inheritdoc/>
    public override void BuildSummary()
    {
        string feedstyleStart = "";
        switch (ModelTyped.FeedStyle)
        {
            case RuminantFeedActivityTypes.SpecifiedDailyAmount:
                feedstyleStart = "a specified total daily amount from";
                break;
            case RuminantFeedActivityTypes.SpecifiedDailyAmountPerIndividual:
                feedstyleStart = "a specified daily amount per individual from";
                break;
            case RuminantFeedActivityTypes.ProportionOfWeight:
                feedstyleStart = "a proportion of each individuals body weight from";
                break;
            case RuminantFeedActivityTypes.ProportionOfPotentialIntake:
                feedstyleStart = "a proportion of each individuals potential intake from";
                break;
            case RuminantFeedActivityTypes.ProportionOfRemainingIntakeRequired:
                feedstyleStart = "a proportion of each individuals remaining intake required from";
                break;
            case RuminantFeedActivityTypes.ProportionOfFeedAvailable:
                feedstyleStart = "a proportion of the available feed from";
                break;
            default:
                break;
        }

        generator.AddBlockWithText("activityentry", $"Feed {feedstyleStart} {generator.DisplaySummaryValueSnippet(ModelTyped.FeedTypeName, "Feed not set", HTMLSummaryStyle.Resource)} to ruminants");
        if (ModelTyped.ProportionTramplingWastage > 0)
        {
            generator.AddBlockWithText("activityentry", $"{generator.DisplaySummaryValueSnippet(ModelTyped.ProportionTramplingWastage)} x AmountNeeded will be lost through trampling");
        }
        if (ModelTyped.StopFeedingWhenSatisfied)
        {
            generator.AddBlockWithText("activityentry", "Feeding will stop when all individuals are satisfied!");
        }
        if (!ModelTyped.ForceFeed)
        {
            generator.AddBlockWithText("activityentry", "Individuals will be forced to eat the specified amount!");
        }
    }
}
