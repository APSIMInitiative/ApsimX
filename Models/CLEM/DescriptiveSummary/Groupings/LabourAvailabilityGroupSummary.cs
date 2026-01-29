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
public class LabourAvailabilityGroupSummary : GroupSummaryBase<LabourAvailabilityGroup>
{
    /// <summary>
    /// Constructor
    /// </summary>
    public LabourAvailabilityGroupSummary()
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
                introduction: "The following individuals have availability (days per month) specified:",
                missing: "No individuals have availability specified."
                )
        ];
    }

    /// <inheritdoc/>
    public override void BuildSummary()
    {
    }

    /// <inheritdoc/>
    public override void CreateSummaryInnerClosingBlocks(ChildComponentGroup group)
    {
        if (group.Id != "default") return;

        var model = ModelTyped;
        if (model is null) return;

        generator.CloseMostRecentBlock("labourAvailabilityItem_filters");
        if (FormatForParentControl)
        {
            generator.AddBlockWithText(generator.DisplaySummaryValueSnippet(model.Value, warnZero: true), tag: "td", classString: "");
            generator.CloseMostRecentBlock("labourAvailabilityItem_row");
        }
    }

    /// <inheritdoc/>
    public override void CreateSummaryInnerOpeningBlocks(ChildComponentGroup group)
    {
        if (group.Id != "default") return;

        var cm = CLEMModel;
        if (cm is null) return;

        if (FormatForParentControl)
        {
            generator.OpenBlock("", "", tag: "tr", id: "labourAvailabilityItem_row");
            generator.AddBlockWithText(cm.Name, tag: "td", classString: "");
            generator.OpenBlock("", "", tag: "td", id: "labourAvailabilityItem_filters");
        }
        else
        {
            generator.OpenBlock("childgroupborder filteritems clearfix", "", id: "labourAvailabilityItem_filters");
        }
        if (cm.Structure.FindChildren<Filter>().Any() == false)
        {
            generator.AddBlockWithText("All individuals", "entryValue filterItem floatLeft");
        }
    }

    /// <inheritdoc/>
    public override void CreateSummaryOpeningBlocks()
    {
        if (!FormatForParentControl)
            base.CreateSummaryOpeningBlocks();
    }

    /// <inheritdoc/>
    public override void CreateSummaryClosingBlocks()
    {
        if (!FormatForParentControl)
            base.CreateSummaryClosingBlocks();
    }

}
