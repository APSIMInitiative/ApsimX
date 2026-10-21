using Models.CLEM.Resources;
using Models.Core;
using System.Collections.Generic;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary;

/// <summary>
/// Descriptive summary provider for the GrazeFoodStore Resource
/// </summary>
public class GrazeFoodStoreSummary: DescriptiveSummaryProviderBase<GrazeFoodStore>
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
                childType: typeof(GrazeFoodStoreType),
                missing: "default"
                )
        ];
    }

    /// <inheritdoc/>
    public override void BuildSummary()
    {
    }
}
