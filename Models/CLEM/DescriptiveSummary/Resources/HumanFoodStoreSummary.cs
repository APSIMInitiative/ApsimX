using Models.CLEM.Resources;
using Models.Core;
using System.Collections.Generic;

namespace Models.CLEM.DescriptiveSummary.Resources
{
    /// <summary>
    /// Descriptive summary provider for HumanFoodStore resource
    /// </summary>
    public class HumanFoodStoreSummary : DescriptiveSummaryProviderBase<HumanFoodStore>
    {
        ///<inheritdoc/>
        public override List<(IEnumerable<IModel> models, bool include, string borderClass, string introText, string missingText)> GetChildrenInSummary()
        {
            var model = ModelTyped;
            if (model is null) return [];

            return
            [
                (model.Structure.FindChildren<HumanFoodStoreType>(), true, "", "", $"No {CLEMModel.DisplaySummaryValueSnippet("HumanFoodStoreType", entryStyle: HTMLSummaryStyle.Resource)} provided!")
            ];
        }


        /// <inheritdoc/>
        public override void BuildSummary()
        {
        }

    }
}