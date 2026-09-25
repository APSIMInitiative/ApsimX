using Models.CLEM.Resources;
using Models.Core;
using System.Collections.Generic;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for OtherAnimals resource
/// </summary>
public class OtherAnimalsSummary : DescriptiveSummaryProviderBase<OtherAnimals>
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
                childType: typeof(OtherAnimalsType),
                missing: "default"
                )
        ];
    }

    /// <inheritdoc/>
    public override void BuildSummary()
    {
    }


}