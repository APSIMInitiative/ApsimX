using Models.CLEM;
using Models.CLEM.DescriptiveSummary;
using Models.Core;
using System.IO;

namespace Models.CLEM.DescriptiveSummary
{
    /// <summary>
    /// Descriptive summary provider for FileResource data reader
    /// </summary>
    public class FileResourceSummary : DescriptiveSummaryProviderBase<FileResource>
    {
        /// <inheritdoc/>
        public override void BuildSummary()
        {
            var m = ModelTyped;
            if (m is null) return;

            if (m.FileName == null || m.FileName == "")
            {
                generator.AddBlockWithText($"Using {generator.DisplayErrorSnippet("Filename not set")}");
                return;
            }
            if (!m.FileExists)
            {
                generator.AddBlockWithText($"File {generator.DisplaySummaryValueSnippet(m.FullFileName, entryStyle: HTMLSummaryStyle.FileReader)} {generator.DisplayErrorSnippet("Not Found")}");
                return;
            }

            generator.AddBlockWithText($"Using {generator.DisplaySummaryValueSnippet(m.FileName, "Filename not set", HTMLSummaryStyle.FileReader)}");

            using (generator.OpenBlock("entryHolder indent", id: $"{ModelTyped.Name}_columns"))
            {
                if (m.FileName != null && m.FileName.Contains(".xls"))
                {
                    generator.AddBlockWithText($"Using worksheet {generator.DisplaySummaryValueSnippet(m.ExcelWorkSheetName, "Not Set", errorNotSet: true, entryStyle: HTMLSummaryStyle.FileReader)}");
                }

                using (generator.OpenBlock("entryHolder indent", id: $"{ModelTyped.Name}_columnlinks"))
                {
                    FileSQLitePastureSummary.ColumnNameSnippet("Resource name", m.ResourceNameColumnName, generator);
                    string yearLabel = ((m.StyleOfDateEntry == DateStyle.DateStamp) ? "Date" : "Year");
                    FileSQLitePastureSummary.ColumnNameSnippet(yearLabel, m.YearColumnName, generator);
                    if (m.StyleOfDateEntry == DateStyle.YearAndMonth)
                    {
                        FileSQLitePastureSummary.ColumnNameSnippet("Month", m.MonthColumnName, generator);
                    }
                    FileSQLitePastureSummary.ColumnNameSnippet("Amount", m.AmountColumnName, generator);
                }
            }
        }
    }
}