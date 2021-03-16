using Models.Core;
using Models.CLEM.Activities;
using Models.CLEM.Reporting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core.Attributes;
using Newtonsoft.Json;
using Models.CLEM.Resources;
using System.ComponentModel.DataAnnotations;

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
    [ValidParent(ParentType = typeof(RuminantActivityMove))]
    [Description("No longer supported. Please use RuminantGroup.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Filters/RuminantDestockGroup.htm")]
    public class RuminantDestockGroup : CLEMModel, IFilterGroup, IValidatableObject
    {
        /// <summary>
        /// Combined ML ruleset for LINQ expression tree
        /// </summary>
        [JsonIgnore]
        public object CombinedRules { get; set; } = null;

        /// <summary>
        /// Proportion of group to use
        /// </summary>
        [JsonIgnore]
        public double Proportion { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantDestockGroup()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubActivity;
        }

        #region validation

        /// <summary>
        /// Validate model
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            string[] memberNames = new string[] { "Ruminant destock group" };
            results.Add(new ValidationResult("This component is no longer supported. Please change to a [f=RuminantGroup]", memberNames));
            return results;
        }
        #endregion

        #region descriptive summary
        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "NO LONGER SUPPORTED! Please change to [f=RuminantGroup]";
            return html;
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryInnerClosingTags(bool formatForParentControl)
        {
            string html = "";
            if (this.FindAllChildren<RuminantFilter>().Count() >= 1)
            {
                html += "\r\n</div>";
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
            html += "\r\n<div class=\"filterborder clearfix\">";
            if (FindAllChildren<RuminantFilter>().Count() < 1)
            {
                html += "<div class=\"filter\">All individuals</div>";
            }
            return html;
        } 
        #endregion

    }
}