using Models.CLEM;
using Models.CLEM.DescriptiveSummary;
using Models.Core;

namespace Models.CLEM.DescriptiveSummary
{
    /// <summary>
    /// Descriptive summary provider for FilePasture data reader
    /// </summary>
    public class FilePastureSummary : DescriptiveSummaryProviderBase<FilePasture>
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


            using (generator.OpenBlock("activityentryindent", id: $"{ModelTyped.Name}_columnlinks"))
            {
                if (m.FileName != null && m.FileName.Contains(".xls"))
                {
                    generator.AddBlockWithText("activityentry", $"Using worksheet {generator.DisplaySummaryValueSnippet(m.ExcelWorkSheetName, "Not Set", errorNotSet: true, entryStyle: HTMLSummaryStyle.FileReader)}");
                }
            }

            // FilePasture is legacy - recommend sqlite reader
            generator.AddBlockWithText("warningbanner", "Note: This reader is legacy. For best performance prefer the SQLite pasture reader (FileSQLitePasture).");
        }
    }
}