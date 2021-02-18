using Models.Core;
using Models.CLEM.Activities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core.Attributes;
using System.ComponentModel.DataAnnotations;
using Models.CLEM.Resources;
using Newtonsoft.Json;
using System.IO;

namespace Models.CLEM.Groupings
{
    ///<summary>
    /// Contains a group of filters to identify individual in labour pool
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(LabourActivityFeed))]
    [Description("This labour filter group selects specific individuals from the labour pool using any number of Labour Filters. This filter group includes feeding rules. No filters will apply rules to all individuals. Multiple feeding groups will select groups of individuals required.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Filters/LabourFeedGroup.htm")]
    public class LabourFeedGroup: CLEMModel, IFilterGroup
    {
        /// <summary>
        /// Value to supply for each month
        /// </summary>
        [Description("Value to supply")]
        [GreaterThanValue(0)]
        public double Value { get; set; }

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
        public LabourFeedGroup()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubActivity;
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
                if (this.Parent.GetType() != typeof(LabourActivityFeed))
                {
                    htmlWriter.Write("<div class=\"warningbanner\">This Labour Feed Group must be placed beneath a Labour Activity Feed component</div>");
                    return htmlWriter.ToString();
                }

                LabourFeedActivityTypes ft = (this.Parent as LabourActivityFeed).FeedStyle;
                htmlWriter.Write("\r\n<div class=\"activityentry\">");
                switch (ft)
                {
                    case LabourFeedActivityTypes.SpecifiedDailyAmountPerAE:
                    case LabourFeedActivityTypes.SpecifiedDailyAmountPerIndividual:
                        htmlWriter.Write("<span class=\"" + ((Value <= 0) ? "errorlink" : "setvalue") + "\">" + Value.ToString() + "</span>");
                        break;
                    default:
                        break;
                }

                ZoneCLEM zoneCLEM = FindAncestor<ZoneCLEM>();
                ResourcesHolder resHolder = zoneCLEM.FindChild<ResourcesHolder>();
                HumanFoodStoreType food = resHolder.GetResourceItem(this, (this.Parent as LabourActivityFeed).FeedTypeName, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.Ignore) as HumanFoodStoreType;
                if (food != null)
                {
                    htmlWriter.Write(" " + food.Units + " ");
                }

                htmlWriter.Write("<span class=\"setvalue\">");
                switch (ft)
                {
                    case LabourFeedActivityTypes.SpecifiedDailyAmountPerIndividual:
                        htmlWriter.Write(" per individual per day");
                        break;
                    case LabourFeedActivityTypes.SpecifiedDailyAmountPerAE:
                        htmlWriter.Write(" per AE per day");
                        break;
                    default:
                        break;
                }
                htmlWriter.Write("</span> ");
                switch (ft)
                {
                    case LabourFeedActivityTypes.SpecifiedDailyAmountPerAE:
                    case LabourFeedActivityTypes.SpecifiedDailyAmountPerIndividual:
                        htmlWriter.Write("is fed to each individual");
                        break;
                }
                htmlWriter.Write(" that matches the following conditions:");

                htmlWriter.Write("</div>");
                return htmlWriter.ToString(); 
            }
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
                if (this.FindAllChildren<LabourFilter>().Count() == 0)
                {
                    htmlWriter.Write("<div class=\"filter\">All individuals</div>");
                }
                return htmlWriter.ToString(); 
            }
        }

        #endregion
    }
}
