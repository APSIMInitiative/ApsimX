using Models.CLEM.Resources;
using Models.Core;
using System.Collections.Generic;

namespace Models.CLEM.DescriptiveSummary.Resources
{
    /// <summary>
    /// Descriptive summary provider for the GrazeFoodStore Resource
    /// </summary>
    public class GrazeFoodStoreSummary: DescriptiveSummaryProviderBase<GrazeFoodStore>
    {
        ///<inheritdoc/>
        public override List<(IEnumerable<IModel> models, bool include, string borderClass, string introText, string missingText)> GetChildrenInSummary()
        {
            var model = ModelTyped;
            if (model is null) return [];

            return
            [
                (model.Structure.FindChildren<GrazeFoodStoreType>(), true, "", "", $"No {CLEMModel.DisplaySummaryValueSnippet("GrazeFoodStoreType", entryStyle: HTMLSummaryStyle.Resource)} provided!")
            ];
        }

        /// <inheritdoc/>
        public override void BuildSummary()
        {
        }
    }
}
