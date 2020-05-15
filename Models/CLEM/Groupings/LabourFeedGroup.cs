using Models.Core;
using Models.CLEM.Activities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core.Attributes;
using System.ComponentModel.DataAnnotations;
using Models.CLEM.Resources;
using System.Xml.Serialization;

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
    public class LabourFeedGroup: CLEMModel, IValidatableObject, IFilterGroup
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
        [XmlIgnore]
        public object CombinedRules { get; set; } = null;

        /// <summary>
        /// Constructor
        /// </summary>
        public LabourFeedGroup()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubActivity;
        }

        /// <summary>
        /// Validate model
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            return results;
        }

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "";

            if(this.Parent.GetType() != typeof(LabourActivityFeed))
            {
                html += "<div class=\"warningbanner\">This Labour Feed Group must be placed beneath a Labour Activity Feed component</div>";
                return html;
            }

            LabourFeedActivityTypes ft = (this.Parent as LabourActivityFeed).FeedStyle;
            html += "\n<div class=\"activityentry\">";
            switch (ft)
            {
                case LabourFeedActivityTypes.SpecifiedDailyAmountPerAE:
                case LabourFeedActivityTypes.SpecifiedDailyAmountPerIndividual:
                    html += "<span class=\"" + ((Value <= 0) ? "errorlink" : "setvalue") + "\">"+Value.ToString() + "</span>";
                    break;
                default:
                    break;
            }


            ZoneCLEM zoneCLEM = Apsim.Parent(this, typeof(ZoneCLEM)) as ZoneCLEM;
            ResourcesHolder resHolder = Apsim.Child(zoneCLEM, typeof(ResourcesHolder)) as ResourcesHolder;
            HumanFoodStoreType food =  resHolder.GetResourceItem(this, (this.Parent as LabourActivityFeed).FeedTypeName, OnMissingResourceActionTypes.Ignore, OnMissingResourceActionTypes.Ignore) as HumanFoodStoreType;
            if (food != null)
            {
                html += " " + food.Units + " ";
            }

            html += "<span class=\"setvalue\">";
            switch (ft)
            {
                case LabourFeedActivityTypes.SpecifiedDailyAmountPerIndividual:
                    html += " per individual per day";
                    break;
                case LabourFeedActivityTypes.SpecifiedDailyAmountPerAE:
                    html += " per AE per day";
                    break;
                default:
                    break;
            }
            html += "</span> ";
            switch (ft)
            {
                case LabourFeedActivityTypes.SpecifiedDailyAmountPerAE:
                case LabourFeedActivityTypes.SpecifiedDailyAmountPerIndividual:
                    html += "is fed to each individual";
                    break;
            }
            html += " that matches the following conditions:";

            html += "</div>";
            return html;
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryInnerClosingTags(bool formatForParentControl)
        {
            string html = "";
            html += "\n</div>";
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
            if (Apsim.Children(this, typeof(LabourFilter)).Count() == 0)
            {
                html += "<div class=\"filter\">All individuals</div>";
            }
            return html;
        }

    }
}
