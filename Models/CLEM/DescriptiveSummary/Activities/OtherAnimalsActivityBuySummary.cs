using Models.CLEM.Activities;
using Models.CLEM.Resources;
using Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for Other Animals Activity Buy
/// </summary>
public class OtherAnimalsActivityBuySummary : DescriptiveSummaryProviderBase<OtherAnimalsActivityBuy>
{
    ///<inheritdoc/>
    public override List<ChildComponentGroup> GetChildrenInSummary()
    {
        return
        [
            new ChildComponentGroup(
                id: "default",
                model: CLEMModel,
                childType: typeof(OtherAnimalsTypeCohort),
                introduction: "The individuals defined in the following cohorts will be purchased:",
                borderClass: "childgroupactivityborder"
                )
        ];
    }

    /// <inheritdoc/>
    public override void BuildSummary()
    {
    }
}
