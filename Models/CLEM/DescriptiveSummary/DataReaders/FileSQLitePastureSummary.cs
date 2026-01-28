using DocumentFormat.OpenXml.Math;
using Models.CLEM;
using Models.CLEM.DescriptiveSummary;
using Models.Core;

namespace Models.CLEM.DescriptiveSummary
{
    /// <summary>
    /// Descriptive summary provider for FileSQLitePasture data reader
    /// </summary>
    public class FileSQLitePastureSummary : DescriptiveSummaryProviderBase<FileSQLitePasture>
    {
        ///<inheritdoc/>
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
                generator.AddBlockWithText($"File reader {generator.DisplaySummaryValueSnippet(m.FullFileName, entryStyle: HTMLSummaryStyle.FileReader)} {generator.DisplayErrorSnippet("Not Found")}");
                return;
            }

            generator.AddBlockWithText($"Using {generator.DisplaySummaryValueSnippet(m.FileName, "Filename not set", HTMLSummaryStyle.FileReader)}");


            using (generator.OpenBlock("entryHolder indent", id: $"{ModelTyped.Name}_columns"))
            {
                generator.AddBlockWithText($"Using table {generator.DisplaySummaryValueSnippet(m.TableName, "TABLE NOT SET", HTMLSummaryStyle.FileReader)}");

                using (generator.OpenBlock("entryHolder indent", id: $"{ModelTyped.Name}_columnlinks"))
                {
                    ColumnNameSnippet("Region id", m.RegionColumnName, generator);
                    ColumnNameSnippet("Land id", m.LandIdColumnName, generator);
                    ColumnNameSnippet("Grass basal area", m.GrassBAColumnName, generator);
                    ColumnNameSnippet("Land condition", m.LandConColumnName, generator);
                    ColumnNameSnippet("Stocking rate", m.StkRateColumnName, generator);
                    ColumnNameSnippet("Year", m.YearColumnName, generator);
                    ColumnNameSnippet("Month", m.MonthColumnName, generator);
                    ColumnNameSnippet("Growth", m.GrowthColumnName, generator);

                    ColumnNameSnippet("Erosion", m.ErosionColumnName, generator, true);
                    ColumnNameSnippet("Runoff", m.RunoffColumnName, generator, true);
                    ColumnNameSnippet("Rainfall", m.RainfallColumnName, generator, true);
                    ColumnNameSnippet("Cover", m.CoverColumnName, generator, true);
                    ColumnNameSnippet("Tree basal area", m.TBAColumnName, generator, true);
                }
            }
            if (m.MissingDataAction != OnMissingResourceActionTypes.Ignore)
                generator.AddBlockWithText($"CAUTION: The simulation will assume no production and associated monthly values such as rainfall if any monthly pasture production entries are missing. You will not be alerted to this possible problem with the pasture database. It is suggested that you run your simulation with another setting of MissingDataAction to check the database when setting up your simulation.", "infoBanner warning");

        }

        /// <summary>
        /// Method to add formatted column to output for all data readers
        /// </summary>
        /// <param name="columnName">Name of column</param>
        /// <param name="linkvalue">Value of the links column name</param>
        /// <param name="generator">The descriptive summary generator to use</param>
        /// <param name="allowNotNeeded">Seperate display for optional entries with an empty linked column name</param>
        public static void ColumnNameSnippet(string columnName, string linkvalue, DescriptiveSummaryGenerator generator, bool allowNotNeeded = false)
        {
            if (allowNotNeeded == false && string.IsNullOrWhiteSpace(linkvalue))
            {
                generator.AddBlockWithText($"No {generator.DisplaySummaryValueSnippet(columnName, entryStyle: HTMLSummaryStyle.FileReader)} data will be collected form the database");
                return;
            }
            generator.AddBlockWithText( $"Column name for {generator.DisplaySummaryValueSnippet(columnName, entryStyle: HTMLSummaryStyle.FileReader)} is {generator.DisplaySummaryValueSnippet(linkvalue ?? (allowNotNeeded ? "NOT NEEDED" : "NOT SET"))}");
        }
    }
}