using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core;
using System.Collections.Generic;
using System.Linq;

namespace Models.CLEM.DescriptiveSummary.Resources
{
    /// <summary>
    /// Descriptive summary provider for RuminantInitialCohorts
    /// </summary>
    public class RuminantInitialCohortsSummary : DescriptiveSummaryProviderBase<RuminantInitialCohorts>
    {
        List<string> headerLabels = [];

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantInitialCohortsSummary()
        {
            SummaryStyle = HTMLSummaryStyle.SubResourceLevel2;
        }

        ///<inheritdoc/>
        public override List<ChildComponentGroup> GetChildrenInSummary()
        {
            var model = ModelTyped;
            if (model is null) return [];

            return
            [
                new ChildComponentGroup(
                    id: "default",
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

            // replicate the important parts of the existing ModelSummary()
            var fileReaders = model.Structure.FindChildren<FileRuminantCohorts>();
            if (fileReaders.Any())
            {
                string readers = "";
                foreach (var reader in fileReaders)
                {
                    readers += $" {CLEMModel.DisplaySummaryValueSnippet(reader.Name, entryStyle: HTMLSummaryStyle.FileReader)}";
                }

                string text = $"Ruminant cohort file readers ({readers.Trim()}) will be used to provide initial cohorts";
                if (model.Structure.FindChildren<RuminantTypeCohort>().Any())
                    text += " which will be included with the cohorts also provided.";
                Generator.AddBlockWithText("activityentry", text + ".");
            }

            if (model.ManagedPastureName != "Not specified")
            {
                bool overridePasture = model.Structure.FindChildren<RuminantTypeCohort>().Where(a => a.ManagedPastureName != "Not specified").Any();

                string prefix = overridePasture ? "New " : "All new ";
                string pastureText = $"{prefix}individuals will be placed on the pasture {CLEMModel.DisplaySummaryValueSnippet(model.ManagedPastureName, entryStyle: HTMLSummaryStyle.Resource)}";
                if (overridePasture)
                    pastureText += " unless overridden by the cohort pasture setting";
                Generator.AddBlockWithText("activityentry", pastureText + ".");
            }
        }

        /// <inheritdoc/>
        public override void CreateSummaryInnerOpeningBlocks(ChildComponentGroup group)
        {
            if (group.Id == "default" && group.SelectedModels.Any())
            {
                var cohortsModel = ModelTyped;
                if (cohortsModel is null) return;

                // Prepare flags used by the original inner-opening tags
                cohortsModel.WeightWarningOccurred = false;
                cohortsModel.ConceptionsFound = cohortsModel.Structure.FindChildren<SetPreviousConception>(recurse: true).Any();
                cohortsModel.AttributesFound = cohortsModel.Structure.FindChildren<SetAttributeWithValue>(recurse: true).Any();

                headerLabels = new() { "Name", "Sex", "Age", "Weight", "Norm Wt.", "Number", "IsSuckling", "IsSire" };
                if (cohortsModel.ConceptionsFound)
                    headerLabels.Add("Pregnant");
                if (cohortsModel.AttributesFound)
                    headerLabels.Add("Attributes");

                Generator.CreateTable(headerLabels);
            }
        }

        /// <inheritdoc/>
        public override void CreateSummaryInnerClosingBlocks(ChildComponentGroup group)
        {
            if (group.Id == "default" && group.SelectedModels.Any())
            {
                var cohortsModel = ModelTyped;
                if (cohortsModel is null) return;
                Generator.CloseTable();
                if (cohortsModel.WeightWarningOccurred)
                    Generator.AddBlockWithText("warningbanner", "Warning: Initial weight differs from the expected normalised weight by more than 20%");
            }

        }
    }
}