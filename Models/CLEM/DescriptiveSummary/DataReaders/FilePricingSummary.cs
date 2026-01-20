using Models.CLEM;
using Models.CLEM.DescriptiveSummary;
using Models.Core;

namespace Models.CLEM.DescriptiveSummary
{
    /// <summary>
    /// Descriptive summary provider for FilePricing data reader
    /// </summary>
    public class FilePricingSummary : DescriptiveSummaryProviderBase<FilePricing>
    {
        /// <inheritdoc/>
        public override void BuildSummary()
        {
            var m = ModelTyped;
            if (m is null) return;

            if (m.FileName == null || m.FileName == "")
            {
                generator.AddBlockWithText("activityentry", $"Using {generator.DisplayErrorSnippet("Filename not set")}");
                return;
            }
            if (!m.FileExists)
            {
                generator.AddBlockWithText("activityentry", $"File {generator.DisplaySummaryValueSnippet(m.FullFileName, entryStyle: HTMLSummaryStyle.FileReader)} {generator.DisplayErrorSnippet("Not Found")}");
                return;
            }

            generator.AddBlockWithText("activityentry", $"Using {generator.DisplaySummaryValueSnippet(m.FileName, "Filename not set", HTMLSummaryStyle.FileReader)}");

            using (generator.OpenBlock("activityentryindent", id: $"{ModelTyped.Name}_columns"))
            {
                if (m.FileName != null && m.FileName.Contains(".xls"))
                {
                    generator.AddBlockWithText("activityentry", $"Using worksheet {generator.DisplaySummaryValueSnippet(m.ExcelWorkSheetName, "Not Set", errorNotSet: true, entryStyle: HTMLSummaryStyle.FileReader)}");
                }

                using (generator.OpenBlock("activityentryindent", id: $"{ModelTyped.Name}_columnlinks"))
                {
                    FileSQLitePastureSummary.ColumnNameSnippet("Date", m.DateColumnName, generator);
                }

                generator.AddBlockWithText("activityentry", "Price columns are matched to resource/pricing component names in the simulation; each column (other than the date) sets the price for the component with the same name.");
            }
        }
    }
}