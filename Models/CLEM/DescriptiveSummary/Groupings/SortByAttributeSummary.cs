using Models.CLEM.Groupings;
using System.IO;

namespace Models.CLEM.DescriptiveSummary.Groupings
{
    /// <summary>
    /// Descriptive summary provider for SortByAttribute
    /// </summary>
    public class SortByAttributeSummary : FilterSummaryBase<SortByAttribute>
    {
        /// <inheritdoc/>
        public override string FilterString(bool htmltags)
        {
            string cssSet = "";
            string cssClose = "";
            if (htmltags)
            {
                cssSet = "<span class = \"filterset\">";
                cssClose = "</span>";
            }

            using (StringWriter sortWriter = new StringWriter())
            {
                sortWriter.Write($"Sort: Attribute-");
                sortWriter.Write($" {CLEMModel.DisplaySummaryValueSnippet(ModelTyped.AttributeTag, "Not set", HTMLSummaryStyle.Filter, htmlTags: htmltags)}");
                sortWriter.Write($" {cssSet}{ModelTyped.FilterStyle.ToString().ToLower()}{cssClose}");
                sortWriter.Write($" {cssSet}{ModelTyped.SortDirection.ToString().ToLower()}{cssClose}");
                return sortWriter.ToString();
            }
        }
    }
}