using Models.CLEM;
using Models.CLEM.Activities;
using Models.CLEM.DescriptiveSummary;
using Models.CLEM.Groupings;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for the Ruminant Feed Group filter
/// </summary>
public class RuminantFeedGroupSummary : GroupSummaryBase<RuminantFeedGroup>
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
                childType: typeof(Filter),
                missing: ""
                )
        ];
    }

    /// <inheritdoc/>
    public override void BuildSummary()
    {
        using StringWriter htmlWriter = new();
        if (ModelTyped.Parent.GetType() != typeof(RuminantActivityFeed))
        {
            generator.AddBlockWithText("warningbanner", "This Ruminant Feed Group must be placed beneath a Ruminant Activity Feed component");
            return;
        }

        RuminantActivityFeed feedActivity = (ModelTyped.Parent as RuminantActivityFeed);
        RuminantFeedActivityTypes ft = feedActivity.FeedStyle;

        string starter = "";
        switch (ft)
        {
            case RuminantFeedActivityTypes.SpecifiedDailyAmount:
            case RuminantFeedActivityTypes.SpecifiedDailyAmountPerIndividual:
                htmlWriter.Write($"{generator.DisplaySummaryValueSnippet(ModelTyped.Value, errorNotSet: true)} kg");
                break;
            case RuminantFeedActivityTypes.ProportionOfFeedAvailable:
            case RuminantFeedActivityTypes.ProportionOfWeight:
            case RuminantFeedActivityTypes.ProportionOfPotentialIntake:
            case RuminantFeedActivityTypes.ProportionOfRemainingIntakeRequired:
                if (ModelTyped.Value < 1)
                {
                    htmlWriter.Write($"{generator.DisplaySummaryValueSnippet(ModelTyped.Value, errorNotSet: true)}");
                    starter = " of ";
                }
                else
                {
                    starter = "The ";
                }
                    break;
            default:
                break;
        }

        bool overfeed = false;
        string feedstyle = "";
        switch (ft)
        {
            case RuminantFeedActivityTypes.ProportionOfFeedAvailable:
                feedstyle = "of the available food supply";
                overfeed = true;
                break;
            case RuminantFeedActivityTypes.SpecifiedDailyAmountPerIndividual:
                feedstyle = "per individual per day";
                overfeed = true;
                break;
            case RuminantFeedActivityTypes.SpecifiedDailyAmount:
                overfeed = true;
                feedstyle = "per day";
                break;
            case RuminantFeedActivityTypes.ProportionOfWeight:
                overfeed = true;
                feedstyle = "live weight";
                break;
            case RuminantFeedActivityTypes.ProportionOfPotentialIntake:
                feedstyle = "potential intake";
                break;
            case RuminantFeedActivityTypes.ProportionOfRemainingIntakeRequired:
                feedstyle = "remaining intake";
                break;
            default:
                break;
        }
        htmlWriter.Write($"{starter} {generator.DisplaySummaryValueSnippet(feedstyle)} ");

        string forceFedString = "fed";
        if (feedActivity.ForceFeed && feedActivity.ForceIntakeAllowed())
        {
            forceFedString = "force-fed";
        }

        switch (ft)
        {
            case RuminantFeedActivityTypes.ProportionOfFeedAvailable:
                htmlWriter.Write("will be fed to all individuals that match the following conditions:");
                break;
            case RuminantFeedActivityTypes.SpecifiedDailyAmount:
                htmlWriter.Write($"combined is {forceFedString} to all individuals that match the following conditions:");
                break;
            case RuminantFeedActivityTypes.SpecifiedDailyAmountPerIndividual:
                htmlWriter.Write($"is {forceFedString} to each individual that matches the following conditions:");
                break;
            default:
                htmlWriter.Write($"is {forceFedString} to the individuals that match the following conditions:");
                break;
        }

        generator.AddBlockWithText("activityentry", htmlWriter.ToString());

        if (overfeed)
        {
            string overfedText = "Individual's intake will be limited to Potential intake x the modifier for max overfeeding";
            if (!(ModelTyped.Parent as RuminantActivityFeed).StopFeedingWhenSatisfied)
            {
                overfedText += ", with excess food still utilised but wasted";
            }
            generator.AddBlockWithText("activityentry", overfedText);

        }

        if (ft == RuminantFeedActivityTypes.SpecifiedDailyAmount)
        {
            generator.AddBlockWithText("warningbanner", "Note: This is a specified daily amount fed to the entire herd. If insufficient provided, each individual's potential intake will not be met");
        }
    }
}
