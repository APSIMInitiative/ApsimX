using Models.CLEM.Resources;
using Models.Core;
using System.Collections.Generic;

namespace Models.CLEM.DescriptiveSummary.Resources
{
    /// <summary>
    /// Descriptive summary provider for OtherAnimals resource
    /// </summary>
    public class ProductStoreSummary : DescriptiveSummaryProviderBase<ProductStore>
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
                    childType: typeof(ProductStoreType),
                    missing: "default"
                    ),
                new ChildComponentGroup(
                    id: "manuretype",
                    model: CLEMModel,
                    childType: typeof(ProductStoreTypeManure),
                    missing: "",
                    introduction: "The following product store types can automatically receive manure."
                    ),
            ];
        }

        /// <inheritdoc/>
        public override void BuildSummary()
        {
        }


    }
}