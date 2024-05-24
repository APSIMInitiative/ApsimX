using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using Display = Models.Core.DisplayAttribute;

namespace Models.CLEM.Groupings
{
    ///<summary>
    /// Individual sort rule based on value of property or method
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(IFilterGroup))]
    [Description("Defines a sort order using the value of a property or method of the individual")]
    [Version(1, 0, 0, "")]
    [HelpUri(@"Content/Features/Filters/SortByProperty.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    public class SortByProperty : CLEMModel, ISort
    {
        private IEnumerable<string> GetParameters() => Parent?.GetParameterNames().OrderBy(k => k);

        /// <inheritdoc/>
        [JsonIgnore]
        public new IFilterGroup Parent
        {
            get => base.Parent as IFilterGroup;
            set => base.Parent = value;
        }

        /// <summary>
        /// Name of property to sort by
        /// </summary>
        [Description("Property or method to use")]
        [Required]
        [Display(Type = DisplayType.DropDown, Values = nameof(GetParameters))]
        public string PropertyOfIndividual { get; set; }

        /// <inheritdoc/>
        [Description("Sort direction")]
        public System.ComponentModel.ListSortDirection SortDirection { get; set; } = System.ComponentModel.ListSortDirection.Ascending;

        /// <inheritdoc/>
        public object OrderRule<T>(T t) => Parent.GetProperty(PropertyOfIndividual).First().GetValue(t, null);

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
                sortWriter.Write($"Sort: ");
                sortWriter.Write($" {CLEMModel.DisplaySummaryValueSnippet(PropertyOfIndividual, "Not set", HTMLSummaryStyle.Filter, htmlTags: htmltags)}");
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

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryClosingTags()
        {
            // allows for collapsed box and simple entry
            return "";
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryOpeningTags()
        {
            // allows for collapsed box and simple entry
            return "";
        }
        #endregion

    }

}