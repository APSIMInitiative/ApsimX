using Models.CLEM.Resources;
using Models.Core;
using System.Collections.Generic;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for the Greenhouse gases Resource
/// </summary>
public class GreenhouseGasesSummary : DescriptiveSummaryProviderBase<GreenhouseGases>
{
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
                childType: typeof(GreenhouseGasesType),
                missing: "default"
                )
        ];
    }

    /// <inheritdoc/>
    public override void BuildSummary()
    {
    }
}
