using Models.CLEM;
using Models.CLEM.DescriptiveSummary;
using Models.Core;

namespace Models.CLEM.DescriptiveSummary
{
    /// <summary>
    /// Descriptive summary provider for FileSQLiteCrop data reader
    /// </summary>
    public class FileSQLiteCropSummary : DescriptiveSummaryProviderBase<FileSQLiteCrop>
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
                generator.AddBlockWithText("activityentry", $"File reader {generator.DisplaySummaryValueSnippet(m.FullFileName, entryStyle: HTMLSummaryStyle.FileReader)} {generator.DisplayErrorSnippet("Not Found")}");
                return;
            }

            generator.AddBlockWithText("activityentry", $"Using {generator.DisplaySummaryValueSnippet(m.FileName, "Filename not set", HTMLSummaryStyle.FileReader)}");


            using (generator.OpenBlock("activityentryindent", id: $"{ModelTyped.Name}_columns"))
            {
                generator.AddBlockWithText("activityentry", $"Using table {generator.DisplaySummaryValueSnippet(m.TableName, "TABLE NOT SET", HTMLSummaryStyle.FileReader)}");

                using (generator.OpenBlock("activityentryindent", id: $"{ModelTyped.Name}_columnlinks"))
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
}