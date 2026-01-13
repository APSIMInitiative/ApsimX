using Models.CLEM.Groupings;
using System.IO;

namespace Models.CLEM.DescriptiveSummary.Groupings
{
    /// <summary>
    /// Descriptive summary provider for SortByProperty
    /// </summary>
    public class SortByPropertySummary : FilterSummaryBase<SortByProperty>
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
                sortWriter.Write($"Sort: ");
                sortWriter.Write($" {CLEMModel.DisplaySummaryValueSnippet(ModelTyped.PropertyOfIndividual, "Not set", HTMLSummaryStyle.Filter, htmlTags: htmltags)}");
                sortWriter.Write($" {cssSet}{ModelTyped.SortDirection.ToString().ToLower()}{cssClose}");
                return sortWriter.ToString();
            }
        }
    }
}