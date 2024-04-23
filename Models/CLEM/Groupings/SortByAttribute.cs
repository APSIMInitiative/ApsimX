using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;

namespace Models.CLEM.Groupings
{
    ///<summary>
    /// Individual sort rule based on Attribute value
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(IFilterGroup))]
    [Description("Defines a sort order using the Attribute details of the individual")]
    [Version(1, 0, 0, "")]
    [HelpUri(@"Content/Features/Filters/SortByAttribute.htm")]
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

        /// <summary>
        /// Value to sort by if attribute is missing
        /// </summary>
        [Description("Value used when attribute missing")]
        [Required(AllowEmptyStrings = false)]
        public string MissingAttributeValue { get; set; }

        /// <inheritdoc/>
        public object OrderRule<T>(T t)
        {
            if (t is IAttributable)
            {
                if (FilterStyle == AttributeFilterStyle.Exists)
                {
                    bool exists = ((IAttributable)t).Attributes.Exists(AttributeTag);
                    return Convert.ToInt32(exists);
                }
                else
                {
                    IndividualAttributeList indAttList = ((IAttributable)t).Attributes;
                    bool exists = (indAttList.AttributesPresent) ? indAttList.Exists(AttributeTag) : false;
                    if (exists)
                        return indAttList.GetValue(AttributeTag).StoredValue;
                    else
                        switch (indAttList.GetValue(AttributeTag))
                        {
                            case IndividualAttribute _:
                                return float.Parse(MissingAttributeValue, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);
                            default:
                                return null;
                        }
                }
            }
            return 0;
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
        public override string ModelSummary()
        {
            return $"<div class=\"filter\" style=\"opacity: {((Enabled) ? "1" : "0.4")}\">{ToHTMLString()}</div>";
        }

        /// <inheritdoc/>
        public override string ModelSummaryClosingTags()
        {
            // allows for collapsed box and simple entry
            return "";
        }

        /// <inheritdoc/>
        public override string ModelSummaryOpeningTags()
        {
            // allows for collapsed box and simple entry
            return "";
        }
        #endregion
    }
}
