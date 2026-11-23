using Models.CLEM;
using Models.CLEM.DataReaders;
using Models.CLEM.DescriptiveSummary;
using Models.Core;

namespace Models.CLEM.DescriptiveSummary.DataReaders;

/// <summary>
/// Descriptive summary provider for FileSQLiteCrop data reader
/// </summary>
public class FileSQLiteCropSummary : DescriptiveSummaryProviderBase<FileSQLiteCrop>
{
    /// <summary>
    /// Constructor
    /// </summary>
    public FileSQLiteCropSummary()
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
            generator.AddBlockWithText($"Using {DescriptiveSummaryGenerator.DisplayErrorSnippet("Filename not set")}");
            return;
        }
        if (!m.FileExists)
        {
            generator.AddBlockWithText($"File reader {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(m.FullFileName, entryStyle: HTMLSummaryStyle.FileReader)} {DescriptiveSummaryGenerator.DisplayErrorSnippet("Not Found")}");
            return;
        }

        generator.AddBlockWithText($"Using {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(m.FileName, "Filename not set", HTMLSummaryStyle.FileReader)}");


        using (generator.OpenBlock("entryHolder indent", id: $"{ModelTyped.Name}_columns"))
        {
            generator.AddBlockWithText($"Using table {DescriptiveSummaryGenerator.DisplaySummaryValueSnippet(m.TableName, "TABLE NOT SET", HTMLSummaryStyle.FileReader)}");

            using (generator.OpenBlock("entryHolder indent", id: $"{ModelTyped.Name}_columnlinks"))
            {
                FileSQLitePastureSummary.ColumnNameSnippet("Land id", m.SoilTypeColumnName, generator);
                FileSQLitePastureSummary.ColumnNameSnippet("Crop name", m.CropNameColumnName, generator);
                FileSQLitePastureSummary.ColumnNameSnippet("Date", m.DateColumnName, generator);
                FileSQLitePastureSummary.ColumnNameSnippet("Growth", m.AmountColumnName, generator);
                FileSQLitePastureSummary.ColumnNameSnippet("Percent nitrogen", m.PercentNitrogenColumnName, generator, true);
                FileSQLitePastureSummary.ColumnNameSnippet("Harvest", m.HarvestTypeColumnName, generator, true);
            }
        }
    }
}