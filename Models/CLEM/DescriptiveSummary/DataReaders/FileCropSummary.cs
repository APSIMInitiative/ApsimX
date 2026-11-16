using Models.CLEM;
using Models.CLEM.DataReaders;
using Models.CLEM.DescriptiveSummary;
using Models.Core;
using System.Collections.Generic;

namespace Models.CLEM.DescriptiveSummary.DataReaders;

/// <summary>
/// Descriptive summary provider for FileCrop data reader
/// </summary>
public class FileCropSummary : DescriptiveSummaryProviderBase<FileCrop>
{
    /// <summary>
    /// Constructor
    /// </summary>
    public FileCropSummary()
    {
        SummaryStyle = HTMLSummaryStyle.FileReader;
    }

    ///<inheritdoc/>
    public override void BuildSummary()
    {
        var m = ModelTyped;
        if (m is null) return;

        if (m.FileName == null || m.FileName == "")
        {
            generator.AddBlockWithText($"Using {DescriptiveSummaryGenerator.DisplayErrorSnippet("Filename not set")}", "entryHolder");
            return;
        }
        if (!m.FileExists)
        {
            generator.AddBlockWithText($"File reader {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(m.FullFileName, entryStyle: HTMLSummaryStyle.FileReader)} {DescriptiveSummaryGenerator.DisplayErrorSnippet("Not Found")}", "entryHolder");
            return;
        }

        generator.AddBlockWithText($"Using {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(m.FileName, "Filename not set", HTMLSummaryStyle.FileReader)}", "entryHolder");

        using (generator.OpenBlock("entryHolder indent", id: $"{ModelTyped.Name}_columns"))
        {
            if (m.FileName != null && m.FileName.Contains(".xls"))
            {
                generator.AddBlockWithText($"Using worksheet {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(m.ExcelWorkSheetName, "Not Set", errorNotSet: true, entryStyle: HTMLSummaryStyle.FileReader)}", "entryHolder");
            }

            using (generator.OpenBlock("entryHolder indent", id: $"{ModelTyped.Name}_columnlinks"))
            {
                FileSQLitePastureSummary.ColumnNameSnippet("Land id", m.SoilTypeColumnName, generator);
                FileSQLitePastureSummary.ColumnNameSnippet("Crop name", m.CropNameColumnName, generator);
                FileSQLitePastureSummary.ColumnNameSnippet("Year", m.YearColumnName, generator);
                FileSQLitePastureSummary.ColumnNameSnippet("Month", m.MonthColumnName, generator);
                FileSQLitePastureSummary.ColumnNameSnippet("Amount grown/harvested", m.AmountColumnName, generator);
                FileSQLitePastureSummary.ColumnNameSnippet("Percent nitrogen", m.PercentNitrogenColumnName, generator, true);
                FileSQLitePastureSummary.ColumnNameSnippet("Crude protein", m.PercentCrudeProteinColumnName, generator, true);
                FileSQLitePastureSummary.ColumnNameSnippet("Dry Matter Digestibility", m.DryMatterDigestibilityColumnName, generator, true);
                FileSQLitePastureSummary.ColumnNameSnippet("Energy content", m.MDColumnName, generator, true);
                FileSQLitePastureSummary.ColumnNameSnippet("Harvest", m.HarvestTypeColumnName, generator, true);
            }
        }
    }
}