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
public class OtherAnimalsFeedGroupSummary : GroupSummaryBase<OtherAnimalsFeedGroup>
{
    /// <summary>
    /// Constructor
    /// </summary>
    public OtherAnimalsFeedGroupSummary()
    {
        SummaryStyle = HTMLSummaryStyle.SubActivity;
    }

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
        if (ModelTyped.Parent.GetType() != typeof(OtherAnimalsActivityFeed))
        {
            generator.AddBlockWithText("This Other Animals Feed Group must be placed beneath a Other Animals Activity Feed component", "infoBanner warning");
            return;
        }

        OtherAnimalsActivityFeed feedActivity = (ModelTyped.Parent as OtherAnimalsActivityFeed);
        OtherAnimalsFeedActivityTypes ft = feedActivity.FeedStyle;

        string starter = "";
        switch (ft)
        {
            case OtherAnimalsFeedActivityTypes.SpecifiedDailyAmount:
            case OtherAnimalsFeedActivityTypes.SpecifiedDailyAmountPerIndividual:
                htmlWriter.Write($"{generator.DisplaySummaryValueSnippet(ModelTyped.Value, errorNotSet: true)} kg");
                break;
            case OtherAnimalsFeedActivityTypes.ProportionOfWeight:
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

        string feedstyle = "";
        switch (ft)
        {
            case OtherAnimalsFeedActivityTypes.SpecifiedDailyAmountPerIndividual:
                feedstyle = "per individual per day";
                break;
            case OtherAnimalsFeedActivityTypes.SpecifiedDailyAmount:
                feedstyle = "per day";
                break;
            case OtherAnimalsFeedActivityTypes.ProportionOfWeight:
                feedstyle = "live weight";
                break;
            default:
                break;
        }
        htmlWriter.Write($"{starter} {generator.DisplaySummaryValueSnippet(feedstyle)} ");

        switch (ft)
        {
            case OtherAnimalsFeedActivityTypes.SpecifiedDailyAmount:
                htmlWriter.Write($"combined is fed to all individuals that match the following conditions:");
                break;
            case OtherAnimalsFeedActivityTypes.SpecifiedDailyAmountPerIndividual:
                htmlWriter.Write($"is fed to each individual that matches the following conditions:");
                break;
            default:
                htmlWriter.Write($"is fed to the individuals that match the following conditions:");
                break;
        }

        generator.AddBlockWithText( htmlWriter.ToString());

        if (ft == OtherAnimalsFeedActivityTypes.SpecifiedDailyAmount)
        {
            generator.AddBlockWithText("Note: This is a specified daily amount fed to the entire herd. If insufficient provided, each individual's potential intake will not be met", "infoBanner warning");
        }
    }
}
