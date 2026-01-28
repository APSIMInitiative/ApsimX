using Models.CLEM;
using Models.CLEM.Activities;
using Models.CLEM.DescriptiveSummary;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for the Labour Feed Group filter
/// </summary>
public class LabourFeedGroupSummary : GroupSummaryBase<LabourFeedGroup>
{
    ///<inheritdoc/>
    public override List<ChildComponentGroup> GetChildrenInSummary()
    {
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
        if (ModelTyped.Parent.GetType() != typeof(LabourActivityFeed))
        {
            generator.AddBlockWithText("This Labour Feed Group must be placed beneath a Labour Activity Feed component", "infoBanner warning");
            return;
        }

        LabourFeedActivityTypes ft = (ModelTyped.Parent as LabourActivityFeed).FeedStyle;
        switch (ft)
        {
            case LabourFeedActivityTypes.SpecifiedDailyAmountPerAE:
            case LabourFeedActivityTypes.SpecifiedDailyAmountPerIndividual:
                htmlWriter.Write($"{generator.DisplaySummaryValueSnippet(ModelTyped.Value, errorNotSet: true)}");
                break;
            default:
                break;
        }

        ZoneCLEM zoneCLEM = ModelTyped.Structure.FindParent<ZoneCLEM>(recurse: true);
        ResourcesHolder resHolder = ModelTyped.Structure.FindChild<ResourcesHolder>(relativeTo: zoneCLEM);
        HumanFoodStoreType food = resHolder.FindResourceType<HumanFoodStore, HumanFoodStoreType>(ModelTyped, (ModelTyped.Parent as LabourActivityFeed).FeedTypeName, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.Ignore);
        if (food != null)
        {
            htmlWriter.Write(" " + food.Units + " ");
        }

        string style = "";
        switch (ft)
        {
            case LabourFeedActivityTypes.SpecifiedDailyAmountPerIndividual:
                style = "per individual per day";
                break;
            case LabourFeedActivityTypes.SpecifiedDailyAmountPerAE:
                style = "per AE per day";
                break;
            default:
                break;
        }
        style += "is fed to each individual that matches the following conditions:";
        generator.DisplaySummaryValueSnippet(style);
        generator.AddBlockWithText(htmlWriter.ToString());
    }
}
