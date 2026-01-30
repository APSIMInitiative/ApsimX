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
public class LabourAvailabilityListSummary : DescriptiveSummaryProviderBase<LabourAvailabilityList>
{
    /// <summary>
    /// Constructor
    /// </summary>
    public LabourAvailabilityListSummary()
    {
        SummaryStyle = HTMLSummaryStyle.SubResource;
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
                childType: typeof(ILabourSpecificationItem),
                missing: "default",
                introduction: "Availability is assigned from the following groups in the order specified"
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
        if (group.Id == "defaulttype" && group.SelectedModels.Any())
        {
            generator.OpenBlock(id: "tablewrap", addTopBottomMargin: true);
            Generator.CreateTable(new string[] { "Name", "Filter", "Days per month" });
        }
    }

    /// <inheritdoc/>
    public override void CreateSummaryInnerClosingBlocks(ChildComponentGroup group)
    {
        if (group.Id == "defaulttype" && group.SelectedModels.Any())
        {
            Generator.CloseTable();
            generator.CloseMostRecentBlock(id: "tablewrap");
        }

    }
}
