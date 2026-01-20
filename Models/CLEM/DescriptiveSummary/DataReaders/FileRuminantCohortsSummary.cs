using Models.CLEM;
using Models.CLEM.DescriptiveSummary;
using Models.Core;

namespace Models.CLEM.DescriptiveSummary
{
    /// <summary>
    /// Descriptive summary provider for FileRuminantCohorts data reader
    /// </summary>
    public class FileRuminantCohortsSummary : DescriptiveSummaryProviderBase<FileRuminantCohorts>
    {
        ///<inheritdoc/>
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
                    FileSQLitePastureSummary.ColumnNameSnippet("Cohort name", m.NameColumnName, generator);
                    FileSQLitePastureSummary.ColumnNameSnippet("Sex", m.SexColumnName, generator);
                    FileSQLitePastureSummary.ColumnNameSnippet("Age", m.AgeColumnName, generator);
                    FileSQLitePastureSummary.ColumnNameSnippet("Number", m.NumberColumnName, generator);
                    FileSQLitePastureSummary.ColumnNameSnippet("Weight", m.WeightColumnName, generator, true);
                    FileSQLitePastureSummary.ColumnNameSnippet("Weight SD", m.WeightSDColumnName, generator, true);
                    FileSQLitePastureSummary.ColumnNameSnippet("Age SD", m.AgeSDColumnName, generator, true);
                    FileSQLitePastureSummary.ColumnNameSnippet("Fat/protein allocation style", m.FatProteinAllocationColumnName, generator, true);
                    FileSQLitePastureSummary.ColumnNameSnippet("Fat/protein values", m.FatProteinColumnName, generator, true);
                    FileSQLitePastureSummary.ColumnNameSnippet("Sire flag", m.SireColumnName, generator, true);
                    FileSQLitePastureSummary.ColumnNameSnippet("Suckling flag", m.SucklingColumnName, generator, true);
                    FileSQLitePastureSummary.ColumnNameSnippet("Castrated flag", m.CastratedColumnName, generator, true);
                    FileSQLitePastureSummary.ColumnNameSnippet("Proportion fleece", m.ProportionFleeceColumnName, generator, true);
                    FileSQLitePastureSummary.ColumnNameSnippet("Days pregnant", m.DaysPregnantColumnName, generator, true);
                    FileSQLitePastureSummary.ColumnNameSnippet("Location", m.LocationColumnName, generator, true);
                }
            }
        }
    }
}