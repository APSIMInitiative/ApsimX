using System.Linq;
using Models.CLEM.Resources;

namespace Models.CLEM.DescriptiveSummary.Resources
{
    /// <summary>
    /// Descriptive summary provider for RuminantType
    /// </summary>
    public class RuminantTypeSummary : DescriptiveSummaryProviderBase<RuminantType>
    {
        /// <inheritdoc/>
        public override void BuildSummary()
        {
            var model = ModelTyped;
            if (model is null) return;

            // Basic identification
            Generator.AddBlockWithText("activityentry", $"Breed: {CLEMModel.DisplaySummaryValueSnippet(model.Name)}");

            // initial cohorts
            bool hasInitialCohorts = model.Children?.Any(c => c is RuminantInitialCohorts) ?? false;
            if (hasInitialCohorts)
                Generator.AddBlockWithText("activityentry", "Initial cohorts are defined for this breed.");
            else
                Generator.AddBlockWithText("activityentry", "No initial cohorts defined for this breed.");

            // child model count and list (safe fallback)
            int childCount = model.Children?.Count ?? 0;
            if (childCount > 0)
            {
                var childNames = model.Children.Select(c => CLEMModel.DisplaySummaryValueSnippet(c.Name)).ToArray();
                Generator.AddBlockWithText("activityentry", $"Contains {CLEMModel.DisplaySummaryValueSnippet(childCount)} child model{(childCount == 1 ? "" : "s")}: {string.Join(", ", childNames)}");
            }
        }
    }
}