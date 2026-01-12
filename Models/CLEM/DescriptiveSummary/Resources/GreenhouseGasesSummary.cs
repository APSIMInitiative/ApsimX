using Models.CLEM.Resources;
using Models.Core;
using System.Collections.Generic;

namespace Models.CLEM.DescriptiveSummary.Resources
{
    /// <summary>
    /// Descriptive summary provider for the Greenhouse gases Resource
    /// </summary>
    public class GreenhouseGasesSummary : DescriptiveSummaryProviderBase<GreenhouseGases>
    {
        ///<inheritdoc/>
        public override List<(IEnumerable<IModel> models, bool include, string borderClass, string introText, string missingText)> GetChildrenInSummary()
        {
            var model = ModelTyped;
            if (model is null) return [];

            return
            [
                (model.Structure.FindChildren<GreenhouseGasesType>(), true, "", "", $"No {CLEMModel.DisplaySummaryValueSnippet("GreenhouseGasesType", entryStyle: HTMLSummaryStyle.Resource)} provided!")
            ];
        }

        /// <inheritdoc/>
        public override void BuildSummary()
        {
        }
    }
}
