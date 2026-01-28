using Models.CLEM.Groupings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for LabourPriceGroup
/// </summary>
public class LabourPriceGroupSummary : DescriptiveSummaryProviderBase<LabourPriceGroup>
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
            generator.AddBlockWithText($"Each individual is paid {CLEMModel.DisplaySummaryValueSnippet(model.Value, warnZero:true)} per day");
        }
    }

    /// <inheritdoc/>
    public override void CreateSummaryInnerClosingBlocks(ChildComponentGroup group)
    {
        if (group.Id != "default") return;

        var model = ModelTyped;
        if (model is null) return;

        generator.CloseMostRecentBlock("labourPriceGroup_filters");
        if (FormatForParentControl)
        {
            generator.AddBlockWithText(generator.DisplaySummaryValueSnippet(model.Value, warnZero: true), tag: "td", classString: "");
            generator.CloseMostRecentBlock("animalPriceGroup_row");
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
            generator.OpenBlock("", "", tag: "tr", id: "labourPriceGroup_row");
            generator.AddBlockWithText(cm.Name, tag: "td", classString: "");
            generator.OpenBlock("", "", tag: "td", id: "labourPriceGroup_filters");
        }
        else
        {
            generator.OpenBlock("childgroupborder filteritems clearfix", "", id: "labourPriceGroup_filters");
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
