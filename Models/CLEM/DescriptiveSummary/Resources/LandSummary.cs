using Models.CLEM.Resources;
using Models.Core;
using System.Collections.Generic;

namespace Models.CLEM.DescriptiveSummary.Resources
{
    /// <summary>
    /// Descriptive summary provider for Land resource
    /// </summary>
    public class LandSummary : DescriptiveSummaryProviderBase<Land>
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
                    childType: typeof(LandType),
                    missing: "default"
                    )
            ];
        }

        /// <inheritdoc/>
        public override void BuildSummary()
        {
        }

    }
}