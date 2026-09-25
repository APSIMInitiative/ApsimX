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
        var model = ModelTyped;
        if (model is null) return;

        if (!FormatForParentControl)
        {
            //ToDo: format value for currency
            generator.AddBlockWithText($"The following individuals have {generator.DisplaySummaryValueSnippet(model.Value, warnZero: true)} days available per month");
        }
    }

    /// <inheritdoc/>
    public override void CreateSummaryInnerClosingBlocks(ChildComponentGroup group)
    {
        if (group.Id != "default") return;

        var model = ModelTyped;
        if (model is null) return;

        generator.CloseMostRecentBlock("labourAvailabilityGroup_filters");
        if (FormatForParentControl)
        {
            generator.AddBlockWithText(generator.DisplaySummaryValueSnippet(ModelTyped.Value, warnZero: true), tag: "td");
            generator.CloseMostRecentBlock("labourAvailabilityGroup_row");
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
            generator.OpenBlock("", "", tag: "tr", id: "labourAvailabilityGroup_row");
            generator.AddBlockWithText(cm.Name, tag: "td");
            generator.OpenBlock("", "", tag: "td", id: "labourAvailabilityGroup_filters");
        }
        else
        {
            generator.OpenBlock("childgroupborder filteritems clearfix", "", id: "labourAvailabilityGroup_filters");
        }
        if (group.SelectedModels.Any() == false)
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
