using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
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
            using StringWriter htmlWriter = new();
            if (!FormatForParentControl)
            {
                htmlWriter.Write("\r\n<div class=\"activityentry\">");
                htmlWriter.Write($"Pay {CLEMModel.DisplaySummaryValueSnippet(Value, warnZero: true)} for a days work</div>");
            }
            return htmlWriter.ToString();
        }

        /// <inheritdoc/>
        public override string ModelSummaryInnerClosingTags()
        {
            using StringWriter htmlWriter = new();
            if (FormatForParentControl)
            {
                htmlWriter.Write($"</td><td>{CLEMModel.DisplaySummaryValueSnippet(Value, warnZero: true)}</td></tr>");
            }
            else
            {
                htmlWriter.Write("\r\n</div>");
            }
            return htmlWriter.ToString();
        }

        /// <inheritdoc/>
        public override string ModelSummaryInnerOpeningTags()
        {
            using StringWriter htmlWriter = new();
            if (FormatForParentControl)
                htmlWriter.Write("<tr><td>" + this.Name + "</td><td>");
            else
                htmlWriter.Write("\r\n<div class=\"filterborder clearfix\">");

            if (!FindAllChildren<Filter>().Any())
                htmlWriter.Write("<div class=\"filter\">All individuals</div>");

            return htmlWriter.ToString();
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
