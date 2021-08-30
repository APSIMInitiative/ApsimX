using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq.Expressions;

namespace Models.CLEM.Groupings
{
    ///<summary>
    /// Individual filter rule based on Attribute exists or associated value
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [Description("Defines a filter rule using Attribute details of the individual")]
    [ValidParent(ParentType = typeof(RuminantFeedGroupMonthly))]
    [ValidParent(ParentType = typeof(RuminantFeedGroup))]
    [ValidParent(ParentType = typeof(RuminantGroup))]
    [ValidParent(ParentType = typeof(AnimalPriceGroup))]
    [Version(1, 0, 0, "")]
    public class FilterByAttribute : Filter
    {
        /// <summary>
        /// Attribute tag to filter by
        /// </summary>
        [Description("Attribute tag")]
        [Required]
        public string AttributeTag { get; set; }

        /// <summary>
        /// Style to assess attribute
        /// </summary>
        [Description("Assessment style")]
        [Required]
        public AttributeFilterStyle FilterStyle { get; set; }

        /// <inheritdoc/>
        public override Func<T, bool> Compile<T>()
        {
            Func<T, bool> lambda = t =>
            {
                if (!(t is IAttributable attributable))
                    return false;

                if (!attributable.Attributes.Exists(AttributeTag))
                    return false;

                if (FilterStyle == AttributeFilterStyle.Exists)
                    return attributable.Attributes.Exists(AttributeTag);
                else
                    return attributable.Attributes.GetValue(AttributeTag).storedValue == Value;
            };

            return lambda;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public FilterByAttribute()
        {
            base.SetDefaults();
        }

        /// <summary>
        /// Convert sort to string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return filterString(false);
        }

        /// <summary>
        /// Convert filter to html string
        /// </summary>
        /// <returns></returns>
        public string ToHTMLString()
        {
            return filterString(true);
        }

        private string filterString(bool htmltags)
        {
            using (StringWriter filterWriter = new StringWriter())
            {
                filterWriter.Write($"Filter:");
                bool truefalse = IsOperatorTrueFalseTest();
                if (FilterStyle == AttributeFilterStyle.Exists | truefalse)
                {
                    filterWriter.Write(" is");
                    if (truefalse)
                    {
                        if (Operator == ExpressionType.IsFalse | Value.ToString().ToLower() == "false")
                            filterWriter.Write(" not");
                    }
                    filterWriter.Write($" Attribute({CLEMModel.DisplaySummaryValueSnippet(AttributeTag, "No tag", htmlTags: htmltags)})");
                }
                else
                {
                    filterWriter.Write($" Attribute({CLEMModel.DisplaySummaryValueSnippet(AttributeTag, "No tag", htmlTags: htmltags)})");
                    filterWriter.Write($" {CLEMModel.DisplaySummaryValueSnippet(OperatorToSymbol(), "Unknown operator", htmlTags: htmltags)}");
                    filterWriter.Write($" {CLEMModel.DisplaySummaryValueSnippet(Value.ToString(), "No value", htmlTags: htmltags)}");
                }
                return filterWriter.ToString();
            }
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary(bool formatForParentControl)
        {
            return $"<div class=\"filter\" style=\"opacity: {((Enabled) ? "1" : "0.4")}\">{ToHTMLString()}</div>";
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryClosingTags(bool formatForParentControl)
        {
            // allows for collapsed box and simple entry
            return "";
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryOpeningTags(bool formatForParentControl)
        {
            // allows for collapsed box and simple entry
            return "";
        }
        #endregion

    }
}
