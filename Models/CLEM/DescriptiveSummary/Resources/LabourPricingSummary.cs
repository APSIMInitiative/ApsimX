using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for AnimalPricing
/// </summary>
public class LabourPricingSummary : DescriptiveSummaryProviderBase<LabourPricing>
{
    /// <summary>
    /// Constructor
    /// </summary>
    public LabourPricingSummary()
    {
        SummaryStyle = HTMLSummaryStyle.SubResourceLevel2;
    }

    ///<inheritdoc/>
    public override List<ChildComponentGroup> GetChildrenInSummary()
    {
        var model = ModelTyped;
        if (model is null) return [];

        return
        [
            new ChildComponentGroup(
                id: "defaulttype",
                model: CLEMModel,
                childType: typeof(LabourPriceGroup),
                missing: "default",
                introduction: "The following Labour Price Groups are applied in the order provided to determine the pay rate of any individual."
                )
        ];
    }

    /// <inheritdoc/>
    public override void BuildSummary()
    {
    }

    /// <inheritdoc/>
    public override void CreateSummaryInnerOpeningBlocks(ChildComponentGroup group)
    {
        if (group.Id != "defaulttype" && group.SelectedModels.Any())
        {
            Generator.CreateTable(new string[] { "Name", "Filter", "Rate per day" });
        }
    }

    /// <inheritdoc/>
    public override void CreateSummaryInnerClosingBlocks(ChildComponentGroup group)
    {
        if (group.Id != "defaulttype" && group.SelectedModels.Any())
        {
            Generator.CloseTable();
        }
    }
}
