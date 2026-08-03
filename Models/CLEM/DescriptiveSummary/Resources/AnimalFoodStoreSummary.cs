using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using Models.Core;
using System.Collections.Generic;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for the Finances Resource
/// </summary>
public class AnimalFoodStoreSummary : DescriptiveSummaryProviderBase<AnimalFoodStore>
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
                childType: typeof(AnimalFoodStoreType),
                missing: "default"
                )
        ];
    }

    /// <inheritdoc/>
    public override void BuildSummary()
    {
    }
}
