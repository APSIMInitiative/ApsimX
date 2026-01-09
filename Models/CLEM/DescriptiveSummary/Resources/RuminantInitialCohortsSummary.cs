using System.Collections.Generic;
using System.Linq;
using Models.CLEM.Resources;

namespace Models.CLEM.DescriptiveSummary.Resources
{
    /// <summary>
    /// Descriptive summary provider for RuminantInitialCohorts
    /// </summary>
    public class RuminantInitialCohortsSummary : DescriptiveSummaryProviderBase<RuminantInitialCohorts>
    {
        List<string> headerLabels = [];
        RuminantInitialCohorts cohortsModel;

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantInitialCohortsSummary()
        {
            SummaryStyle = HTMLSummaryStyle.SubResourceLevel2;
        }

        /// <inheritdoc/>
        public override void BuildSummary(RuminantInitialCohorts model)
        {
            cohortsModel = model;
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
        public override void CreateSummaryInnerOpeningBlocks()
        {
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

        /// <inheritdoc/>
        public override void CreateSummaryInnerClosingBlocks()
        {
            Generator.CloseTable();
            if (cohortsModel.WeightWarningOccurred)
                Generator.AddBlockWithText("warningbanner", "Warning: Initial weight differs from the expected normalised weight by more than 20%");

        }
    }
}