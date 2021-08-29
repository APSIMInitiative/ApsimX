using Models.CLEM.Interfaces;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;

using Display = Models.Core.DisplayAttribute;

namespace Models.CLEM.Groupings
{
    ///<summary>
    /// Individual sort rule based on value of property or method
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantFeedGroupMonthly))]
    [ValidParent(ParentType = typeof(RuminantFeedGroup))]
    [ValidParent(ParentType = typeof(RuminantGroup))]
    [ValidParent(ParentType = typeof(AnimalPriceGroup))]
    [Description("Defines a sort rule using the value of a property or method of the individual")]
    [Version(1, 0, 0, "")]
    public class SortByProperty : CLEMModel, ISort
    {
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
        private IEnumerable<string> GetParameters() => Parent.Parameters;

        /// <inheritdoc/>
        [Description("Sort direction")]
        public System.ComponentModel.ListSortDirection SortDirection { get; set; } = System.ComponentModel.ListSortDirection.Ascending;

        /// <inheritdoc/>
        public object OrderRule<T>(T t) => Parent.GetProperty(PropertyOfIndividual).GetValue(t, null);

        /// <summary>
        /// Convert sort to string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            using (StringWriter sortString = new StringWriter())
            {
                sortString.Write($"Sort by [{PropertyOfIndividual}] {SortDirection.ToString().ToLower()}");
                return sortString.ToString();
            }
        }

        /// <summary>
        /// Convert sort to html string
        /// </summary>
        /// <returns></returns>
        public string ToHTMLString()
        {
            using (StringWriter sortString = new StringWriter())
            {
                sortString.Write($"Sort by {CLEMModel.DisplaySummaryValueSnippet(PropertyOfIndividual, "Not set")} {SortDirection.ToString().ToLower()}");
                return sortString.ToString();
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