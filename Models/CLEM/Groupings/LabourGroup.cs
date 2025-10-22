using Models.CLEM.Activities;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Linq;

namespace Models.CLEM.Groupings
{
    ///<summary>
    /// Contains a group of filters to identify individuals able to undertake labour
    ///</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(LabourRequirement))]
    [ValidParent(ParentType = typeof(LabourRequirementNoUnitSize))]
    [ValidParent(ParentType = typeof(LabourGroup))]
    [ValidParent(ParentType = typeof(TransmuteLabour))]
    [Description("Defines specific individuals from the labour pool to undertake labour")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Filters/Groups/LabourFilterGroup.htm")]
    public class LabourGroup : FilterGroup<LabourType>
    {
        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            string html = "";
            return html;
        }

        /// <inheritdoc/>
        public override string ModelSummaryClosingTags()
        {
            return "";
        }

        /// <inheritdoc/>
        public override string ModelSummaryOpeningTags()
        {
            return "";
        }

        /// <inheritdoc/>
        public override string ModelSummaryInnerClosingTags()
        {
            string html = "";
            html += "\r\n</div>";
            return html;
        }

        /// <inheritdoc/>
        public override string ModelSummaryInnerOpeningTags()
        {
            string html = "";
            if (Parent.GetType() == typeof(LabourGroup))
                html += "<div class=\"labournote\" style=\"clear: both;\">If insufficient labour use the specifications below</div>";

            html += "\r\n<div class=\"filterborder clearfix\">";

            if (Structure.FindChildren<Filter>().Count() < 1)
                html += "<div class=\"filter\">Any labour</div>";

            return html;
        }
        #endregion
    }
}
