using Models.CLEM.Activities;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

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
    [ValidParent(ParentType = typeof(LabourFilterGroup))]
    [ValidParent(ParentType = typeof(TransmuteLabour))]
    [Description("Defines specific individuals from the labour pool to undertake labour")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Filters/Groups/LabourFilterGroup.htm")]
    public class LabourFilterGroup : FilterGroup<LabourType>
    {
        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "";
            return html;
        }

        /// <inheritdoc/>
        public override string ModelSummaryClosingTags(bool formatForParentControl)
        {
            return "";
        }

        /// <inheritdoc/>
        public override string ModelSummaryOpeningTags(bool formatForParentControl)
        {
            return "";
        }

        /// <inheritdoc/>
        public override string ModelSummaryInnerClosingTags(bool formatForParentControl)
        {
            string html = "";
            html += "\r\n</div>";
            return html;
        }

        /// <inheritdoc/>
        public override string ModelSummaryInnerOpeningTags(bool formatForParentControl)
        {
            string html = "";
            if (Parent.GetType() == typeof(LabourFilterGroup))            
                html += "<div class=\"labournote\" style=\"clear: both;\">If insufficient labour use the specifications below</div>";
            
            html += "\r\n<div class=\"filterborder clearfix\">";

            if (FindAllChildren<Filter>().Count() < 1)            
                html += "<div class=\"filter\">Any labour</div>";
            
            return html;
        } 
        #endregion
    }
}
