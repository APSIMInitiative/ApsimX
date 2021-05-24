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
    [ValidParent(ParentType = typeof(RuminantDestockGroup))]
    [ValidParent(ParentType = typeof(AnimalPriceGroup))]
    [Description("This ruminant sort rule is used to order results by the value assicated with a named Attribute. Multiple sorts can be chained, with sorts higher in the tree taking precedence.")]
    [Version(1, 0, 0, "")]
    public class SortByAttributeValue : CLEMModel, ISort
    {
        /// <summary>
        /// Name of attribute to sort by
        /// </summary>
        [Description("Name of Attribute to sort by")]
        [Required]
        public string AttributeName { get; set; }

        /// <inheritdoc/>
        [Description("Sort direction")]
        public System.ComponentModel.ListSortDirection SortDirection { get; set; } = System.ComponentModel.ListSortDirection.Ascending;

        /// <inheritdoc/>
        public object OrderRule<T>(T t) =>  (t as Ruminant).GetAttributeValue(AttributeName);

        /// <summary>
        /// Convert sort to string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            using (StringWriter sortString = new StringWriter())
            {
                sortString.Write("Sort by attribute ");
                if (this.AttributeName == null)
                {
                    sortString.Write($"NOT DEFINED");
                }
                else
                {
                    sortString.Write($"{AttributeName} value {SortDirection.ToString().ToLower()}");
                }
                return sortString.ToString();
            }
        }

        #region descriptive summary

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            return $"<div class=\"filter\" style=\"opacity: {((this.Enabled) ? "1" : "0.4")}\">{this}</div>";
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryClosingTags(bool formatForParentControl)
        {
            return "";
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryOpeningTags(bool formatForParentControl)
        {
            return "";
        }
        #endregion
    }
}
