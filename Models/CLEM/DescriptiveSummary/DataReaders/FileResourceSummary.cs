using Models.CLEM;
using Models.CLEM.DataReaders;
using Models.CLEM.DescriptiveSummary;
using Models.Core;
using System.IO;

namespace Models.CLEM.DescriptiveSummary.DataReaders;

/// <summary>
/// Descriptive summary provider for FileResource data reader
/// </summary>
public class FileResourceSummary : DescriptiveSummaryProviderBase<FileResource>
{
    /// <summary>
    /// Constructor
    /// </summary>
    public FileResourceSummary()
    {
        SummaryStyle = HTMLSummaryStyle.FileReader;
    }

    /// <inheritdoc/>
    public override void BuildSummary()
    {
        var m = ModelTyped;
        if (m is null) return;

        if (m.FileName == null || m.FileName == "")
        {
            generator.AddBlockWithText($"Using {DescriptiveSummaryGenerator.DisplayErrorSnippet("Filename not set")}");
            return;
        }
        if (!m.FileExists)
        {
            generator.AddBlockWithText($"File {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(m.FullFileName, entryStyle: HTMLSummaryStyle.FileReader)} {DescriptiveSummaryGenerator.DisplayErrorSnippet("Not Found")}");
            return;
        }

        generator.AddBlockWithText($"Using {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(m.FileName, "Filename not set", HTMLSummaryStyle.FileReader)}");

        using (generator.OpenBlock("entryHolder indent", id: $"{ModelTyped.Name}_columns"))
        {
            if (m.FileName != null && m.FileName.Contains(".xls"))
            {
                generator.AddBlockWithText($"Using worksheet {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(m.ExcelWorkSheetName, "Not Set", errorNotSet: true, entryStyle: HTMLSummaryStyle.FileReader)}");
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