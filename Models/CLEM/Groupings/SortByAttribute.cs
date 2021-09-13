using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Groupings
{
    ///<summary>
    /// Individual sort rule based on Attribute value
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantFeedGroupMonthly))]
    [ValidParent(ParentType = typeof(RuminantFeedGroup))]
    [ValidParent(ParentType = typeof(RuminantGroup))]
    [ValidParent(ParentType = typeof(AnimalPriceGroup))]
    [Description("Defines a sort rule using the Attribute details of the individual")]
    [Version(1, 0, 0, "")]
    public class SortByAttribute : CLEMModel, ISort
    {
        /// <summary>
        /// Name of attribute to sort by
        /// </summary>
        [Description("Attribute tag")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Attribute tag must be provided")]
        public string AttributeTag { get; set; }

        /// <summary>
        /// Style to assess attribute
        /// </summary>
        [Description("Assessment style")]
        [Required]
        public AttributeFilterStyle FilterStyle { get; set; }

        /// <inheritdoc/>
        [Description("Sort direction")]
        public System.ComponentModel.ListSortDirection SortDirection { get; set; } = System.ComponentModel.ListSortDirection.Ascending;

        /// <inheritdoc/>
        public object OrderRule<T>(T t)
        {
            if (FilterStyle == AttributeFilterStyle.Exists)
                return (t as Ruminant).Attributes.Exists(AttributeTag);
            else
                return (t as Ruminant).Attributes.GetValue(AttributeTag).StoredValue;
        }

        /// <summary>
        /// Convert sort to string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return SortString(false);
        }

        /// <summary>
        /// Convert sort to html string
        /// </summary>
        /// <returns></returns>
        public string ToHTMLString()
        {
            return SortString(true);
        }

        private string SortString(bool htmltags)
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
                sortWriter.Write($"Sort: Atrribute-");
                sortWriter.Write($" {CLEMModel.DisplaySummaryValueSnippet(AttributeTag, "Not set", HTMLSummaryStyle.Filter, htmlTags: htmltags)}");
                sortWriter.Write($" {cssSet}{FilterStyle.ToString().ToLower()}{cssClose}");
                sortWriter.Write($" {cssSet}{SortDirection.ToString().ToLower()}{cssClose}");
                return sortWriter.ToString();
            }
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary(bool formatForParentControl)
        {
            return $"<div class=\"filter\" style=\"opacity: {((Enabled) ? "1" : "0.4")}\">{ToHTMLString()}</div>";
        }

        /// <inheritdoc/>
        public override string ModelSummaryClosingTags(bool formatForParentControl)
        {
            // allows for collapsed box and simple entry
            return "";
        }

        /// <inheritdoc/>
        public override string ModelSummaryOpeningTags(bool formatForParentControl)
        {
            // allows for collapsed box and simple entry
            return "";
        }
        #endregion
    }
}
