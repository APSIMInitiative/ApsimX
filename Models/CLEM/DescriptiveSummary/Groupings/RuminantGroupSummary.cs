using Models.CLEM.Groupings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for the Ruminant Group filter
/// </summary>
public class RuminantGroupSummary : GroupSummaryBase<RuminantGroup>
{
    /// <summary>
    /// Constructor
    /// </summary>
    public RuminantGroupSummary()
    {
        SummaryStyle = HTMLSummaryStyle.Filter;
    }

    /// <inheritdoc/>
    public override void BuildSummary()
    {
        var model = ModelTyped;
        if (model is null) return;

        if (!FormatForParentControl)
        {
        }
    }

    /// <inheritdoc/>
    public override void CreateSummaryInnerClosingBlocks(ChildComponentGroup group)
    {
        //if (group.Id != "default") return;

        var model = ModelTyped;
        if (model is null) return;

        generator.CloseMostRecentBlock("ruminantGroup_filters");
    }

    /// <inheritdoc/>
    public override void CreateSummaryInnerOpeningBlocks(ChildComponentGroup group)
    {
        //if (group.Id != "default") return;

        var cm = CLEMModel;
        if (cm is null) return;

        generator.OpenBlock("filterborder clearfix", "", id: "ruminantGroup_filters");
        if (group.SelectedModels.Any() == false)
        {
            generator.AddBlockWithText("filter", "All individuals");
        }
    }
}
