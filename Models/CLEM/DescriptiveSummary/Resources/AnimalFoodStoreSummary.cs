using Models.CLEM.Groupings;
using Models.CLEM.Resources;
using Models.Core;
using System.Collections.Generic;

namespace Models.CLEM.DescriptiveSummary.Resources
{
    /// <summary>
    /// Descriptive summary provider for the Finances Resource
    /// </summary>
    public class AnimalFoodStoreSummary : DescriptiveSummaryProviderBase<AnimalFoodStore>
    {
        /// <inheritdoc/>
        public override List<(IEnumerable<IModel> models, bool include, string borderClass, string introText, string missingText)> GetChildrenInSummary()
        {
            var model = ModelTyped;
            if (model is null) return [];

            return
            [
                (model.Structure.FindChildren<AnimalFoodStoreType>(), true, "", "", $"No {CLEMModel.DisplaySummaryValueSnippet("AnimalFoodStoreType", entryStyle: HTMLSummaryStyle.Resource)} provided!")
            ];
        }

        /// <inheritdoc/>
        public override void BuildSummary()
        {
        }
    }
}
