using Models.CLEM.Resources;
using Models.Core;
using System.Collections.Generic;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for HumanFoodStore resource
/// </summary>
public class HumanFoodStoreSummary : DescriptiveSummaryProviderBase<HumanFoodStore>
{
    ///<inheritdoc/>
    public override List<ChildComponentGroup> GetChildrenInSummary()
    {
        var model = ModelTyped;
        if (model is null) return [];

        return
        [
            new ChildComponentGroup(
                id: "defaulttypes",
                model: CLEMModel,
                childType: typeof(HumanFoodStoreType),
                missing: "default"
                )
        ];
    }

    /// <inheritdoc/>
    public override void BuildSummary()
    {
    }

}