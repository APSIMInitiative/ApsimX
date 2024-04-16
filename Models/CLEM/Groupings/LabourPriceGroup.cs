using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Models.CLEM.Groupings
{
    ///<summary>
    /// Contains a group of filters to identify individual labour in a set price group
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(LabourPricing))]
    [Description("Set the pay rate for the selected group of individuals")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Filters/Groups/LabourPriceGroup.htm")]
    public class LabourPriceGroup : FilterGroup<LabourType>
    {
        /// <summary>
        /// Pay rate
        /// </summary>
        [Description("Daily pay rate")]
        [Required, GreaterThanEqualValue(0)]
        public double Value { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        protected LabourPriceGroup()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubResource;
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            string html = "";
            if (!FormatForParentControl)
            {
                html += "\r\n<div class=\"activityentry\">";
                html += $"Pay {CLEMModel.DisplaySummaryValueSnippet(Value, warnZero: true)} for a days work</div>";
            }
            return html;
        }

        /// <inheritdoc/>
        public override string ModelSummaryInnerClosingTags()
        {
            string html = "";
            if (FormatForParentControl)
            {
                html += $"</td><td>{CLEMModel.DisplaySummaryValueSnippet(Value, warnZero: true)}</td>";
                html += "</tr>";
            }
            else
            {
                html += "\r\n</div>";
            }
            return html;
        }

        /// <inheritdoc/>
        public override string ModelSummaryInnerOpeningTags()
        {
            string html = "";
            if (FormatForParentControl)
                html += "<tr><td>" + this.Name + "</td><td>";
            else
                html += "\r\n<div class=\"filterborder clearfix\">";

            if (FindAllChildren<Filter>().Count() < 1)
                html += "<div class=\"filter\">All individuals</div>";

            return html;
        }

        /// <inheritdoc/>
        public override string ModelSummaryClosingTags()
        {
            return !FormatForParentControl ? base.ModelSummaryClosingTags() : "";
        }

        /// <inheritdoc/>
        public override string ModelSummaryOpeningTags()
        {
            return !FormatForParentControl ? base.ModelSummaryOpeningTags() : "";
        }
        #endregion
    }
}
