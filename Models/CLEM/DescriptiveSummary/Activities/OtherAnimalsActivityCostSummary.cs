using Models.CLEM.Activities;
using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for Other Animals Activity Cost
/// </summary>
public class OtherAnimalsActivityCostSummary : DescriptiveSummaryProviderBase<OtherAnimalsActivityCost>
{
    ///<inheritdoc/>
    public override List<ChildComponentGroup> GetChildrenInSummary()
    {
        return
        [
            new ChildComponentGroup(
                id: "default",
                model: CLEMModel,
                childType: typeof(OtherAnimalsGroup),
                introduction: "Costs calculated based on the individuals specified below:",
                borderClass: "childgroupborder filtergroup"
                )
        ];
    }

    /// <inheritdoc/>
    public override void BuildSummary()
    {
        generator.AddBlockWithText("Each cohort will only be considered once (when first specified) regardless of multiple filter groups", "infoBanner warning");
    }
}
