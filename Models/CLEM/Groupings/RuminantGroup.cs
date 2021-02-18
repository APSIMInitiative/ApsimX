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
using System.IO;

namespace Models.CLEM.Groupings
{
    ///<summary>
    /// Contains a group of filters to identify individual ruminants
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ReportRuminantHerd))]
    [ValidParent(ParentType = typeof(SummariseRuminantHerd))]
    [ValidParent(ParentType = typeof(RuminantActivityManage))]
    [ValidParent(ParentType = typeof(RuminantActivityPredictiveStocking))]
    [ValidParent(ParentType = typeof(RuminantActivityPredictiveStockingENSO))]
    [ValidParent(ParentType = typeof(RuminantActivityMove))]
    [ValidParent(ParentType = typeof(RuminantActivityMarkForSale))]
    [Description("This group selects specific individuals from the ruminant herd using any number of Ruminant Filters.")]
    [Version(1, 0, 1, "Added ability to select random proportion of the group to use")]
    [HelpUri(@"Content/Features/Filters/RuminantFilterGroup.htm")]
    public class RuminantGroup : CLEMModel, IFilterGroup
    {
        /// <summary>
        /// Combined ML ruleset for LINQ expression tree
        /// </summary>
        [JsonIgnore]
        public object CombinedRules { get; set; } = null;

        /// <summary>
        /// Proportion of group to use
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(1)]
        [Description("Proportion of group to use")]
        [Required, GreaterThanValue(0), Proportion]
        public double Proportion { get; set; }

        /// <summary>
        /// Constructor to apply defaults
        /// </summary>
        public RuminantGroup()
        {
            this.SetDefaults();
        }

        #region descriptive summary

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("<div class=\"filtername\">");
                if (!this.Name.Contains(this.GetType().Name.Split('.').Last()))
                {
                    htmlWriter.Write(this.Name);
                }
                htmlWriter.Write($"</div>");
                return htmlWriter.ToString(); 
            }
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

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryInnerClosingTags(bool formatForParentControl)
        {
            return "\r\n</div>";
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryInnerOpeningTags(bool formatForParentControl)
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"filterborder clearfix\">");

                if (Proportion < 1)
                {
                    htmlWriter.Write("<div class=\"filter\">");
                    if (Proportion <= 0)
                    {
                        htmlWriter.Write("<span class=\"errorlink\">[NOT SET%]</span>");
                    }
                    else
                    {
                        htmlWriter.Write($"{Proportion.ToString("P0")} of");
                    }
                    htmlWriter.Write("</div>");
                }
                if (FindAllChildren<RuminantFilter>().Count() < 1)
                {
                    htmlWriter.Write("<div class=\"filter\">All individuals</div>");
                }
                return htmlWriter.ToString(); 
            }
        } 
        #endregion

    }
}