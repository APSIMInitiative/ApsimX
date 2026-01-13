using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary.Resources
{
    /// <summary>
    /// Descriptive summary provider for SpecifyRuminant
    /// </summary>
    public class SpecifyRuminantSummary : DescriptiveSummaryProviderBase<SpecifyRuminant>
    {
        ///<inheritdoc/>
        public override List<ChildComponentGroup> GetChildrenInSummary()
        {
            var model = ModelTyped;
            if (model is null) return [];

            return
            [
                new ChildComponentGroup(
                    id: "cohort",
                    model: CLEMModel,
                    childType: typeof(RuminantTypeCohort),
                    missing: "default"
                    )
            ];
        }

        /// <inheritdoc/>
        public override void BuildSummary()
        {
            var model = ModelTyped;
            if (model is null) return;

            string extra = "";
            bool cohortFound = model.Structure?.FindChildren<RuminantTypeCohort>(relativeTo: model).Any() ?? false;

            if (cohortFound)
            {
                extra = " with the following details.";
            }

            Generator.AddBlockWithText("activityentry", $"{CLEMModel.DisplaySummaryValueSnippet<double>(model.Proportion, warnZero: true)} of the individuals will be {CLEMModel.DisplaySummaryResourceTypeSnippet(model.RuminantTypeName)} {extra}");

            if (!cohortFound)
            {
                Generator.AddBlockWithText("activityentry", $"No {CLEMModel.DisplaySummaryResourceTypeSnippet("RuminantCohort")} describing the individuals was provided!", styleString:"errorlink");
            }
        }
    }
}