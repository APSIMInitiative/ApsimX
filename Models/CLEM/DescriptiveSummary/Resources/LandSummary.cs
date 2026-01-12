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
        public override List<(IEnumerable<IModel> models, bool include, string borderClass, string introText, string missingText)> GetChildrenInSummary()
        {
            var model = ModelTyped;
            if (model is null) return [];

            return
            [
                (model.Structure.FindChildren<LandType>(), true, "", "", $"No {CLEMModel.DisplaySummaryValueSnippet("LandType", entryStyle: HTMLSummaryStyle.Resource)} provided!")
            ];
        }

        /// <inheritdoc/>
        public override void BuildSummary()
        {
        }

    }
}