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
        public override List<(IEnumerable<IModel> models, bool include, string borderClass, string introText, string missingText)> GetChildrenInSummary()
        {
            var model = ModelTyped;
            if (model is null) return [];

            return
            [
                (model.Structure.FindChildren<ProductStoreType>(), true, "", "", $"No {CLEMModel.DisplaySummaryValueSnippet("ProductStoreType", entryStyle: HTMLSummaryStyle.Resource)} provided!")
            ];
        }

        /// <inheritdoc/>
        public override void BuildSummary()
        {
        }


    }
}