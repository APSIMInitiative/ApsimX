using Models.Core;
using Models.CLEM.Activities;
using Models.CLEM.Reporting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core.Attributes;
using System.Xml.Serialization;
using Models.CLEM.Resources;

namespace Models.CLEM.Groupings
{
    ///<summary>
    /// Contains a group of filters to identify individual ruminants for destocking activities
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantActivityManage))]
    [ValidParent(ParentType = typeof(RuminantActivityPredictiveStocking))]
    [ValidParent(ParentType = typeof(RuminantActivityPredictiveStockingENSO))]
    [Description("This ruminant filter group specifies individuals from the ruminant herd for destocking using any number of Ruminant Filters. Multiple filters will select groups of individuals required.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Filters/RuminantDestockGroup.htm")]
    public class RuminantDestockGroup : CLEMModel, IFilterGroup
    {
        /// <summary>
        /// Combined ML ruleset for LINQ expression tree
        /// </summary>
        [XmlIgnore]
        public object CombinedRules { get; set; } = null;

        /// <summary>
        /// Constructor
        /// </summary>
        protected RuminantDestockGroup()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubActivity;
        }

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "";
            return html;
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryInnerClosingTags(bool formatForParentControl)
        {
            string html = "";
            if (Apsim.Children(this, typeof(RuminantFilter)).Count() >= 1)
            {
                html += "\n</div>";
            }
            return html;
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryInnerOpeningTags(bool formatForParentControl)
        {
            string html = "";
            html += "\n<div class=\"filterborder clearfix\">";
            if (!(Apsim.Children(this, typeof(RuminantFilter)).Count() >= 1))
            {
                html += this.Name;
                html += "<div class=\"filter\">All individuals</div>";
            }
            return html;
        }

    }
}