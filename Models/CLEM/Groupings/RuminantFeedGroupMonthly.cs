using Models.Core;
using Models.CLEM.Activities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core.Attributes;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Xml.Serialization;
using Models.CLEM.Resources;

namespace Models.CLEM.Groupings
{
    ///<summary>
    /// Contains a group of filters to identify individual ruminants
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantActivityFeed))]
    [Description("This ruminant filter group selects specific individuals from the ruminant herd using any number of Ruminant Filters. This filter group includes feeding rules. No filters will apply rules to current herd. Multiple feeding groups will select groups of individuals required.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Filters/RuminantFeedGroupMonthly.htm")]
    public class RuminantFeedGroupMonthly: CLEMModel, IValidatableObject, IFilterGroup
    {
        /// <summary>
        /// Daily value to supply for each month
        /// </summary>
        [Description("Daily value to supply for each month")]
        [ArrayItemCount(12), GreaterThanValue(0)]
        public double[] MonthlyValues { get; set; }

        /// <summary>
        /// Combined ML ruleset for LINQ expression tree
        /// </summary>
        [XmlIgnore]
        public object CombinedRules { get; set; } = null;

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantFeedGroupMonthly()
        {
            MonthlyValues = new double[12];
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

            switch ((this.Parent as RuminantActivityFeed).FeedStyle)
            {
                case RuminantFeedActivityTypes.ProportionOfWeight:
                case RuminantFeedActivityTypes.ProportionOfPotentialIntake:
                case RuminantFeedActivityTypes.ProportionOfRemainingIntakeRequired:
                    if(MonthlyValues.Max() > 1)
                    {
                        string[] memberNames = new string[] { "Monthly values" };
                        results.Add(new ValidationResult("Invalid monthly value provided [v"+ MonthlyValues.Max().ToString() + "] for ["+this.Name+"] Feed Group for ["+this.Parent.Name+"] given the style of feeding selected requires a proportion.", memberNames));
                    }
                    break;
                default:
                    break;
            }
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

            // get amount
            var grps = MonthlyValues.GroupBy(a => a).OrderBy(a => a.Key);

            RuminantFeedActivityTypes ft = (this.Parent as RuminantActivityFeed).FeedStyle;
            html += "\n<div class=\"activityentry\">";
            if (grps.Count() > 1)
            {
                html += "From ";
            }
            html += "<span class=\"setvalue\">";
            switch (ft)
            {
                case RuminantFeedActivityTypes.SpecifiedDailyAmount:
                    html += grps.FirstOrDefault().Key.ToString()+ "kg";
                    break;
                case RuminantFeedActivityTypes.ProportionOfWeight:
                case RuminantFeedActivityTypes.ProportionOfPotentialIntake:
                case RuminantFeedActivityTypes.ProportionOfRemainingIntakeRequired:
                    if (grps.LastOrDefault().Key != 1)
                    {
                        html += (Convert.ToDecimal(grps.FirstOrDefault().Key, CultureInfo.InvariantCulture)).ToString("0.##%");
                    }
                    break;
                default:
                    break;
            }
            html += "</span>";

            if (grps.Count() > 1)
            {
                html += " to ";
                html += "<span class=\"setvalue\">";
                switch (ft)
                {
                    case RuminantFeedActivityTypes.SpecifiedDailyAmount:
                        html += grps.LastOrDefault().Key.ToString()+"kg";
                        break;
                    case RuminantFeedActivityTypes.ProportionOfWeight:
                    case RuminantFeedActivityTypes.ProportionOfPotentialIntake:
                    case RuminantFeedActivityTypes.ProportionOfRemainingIntakeRequired:
                            html += (Convert.ToDecimal(grps.LastOrDefault().Key, CultureInfo.InvariantCulture)).ToString("0.##%");
                        break;
                    default:
                        break;
                }
                html += "</span>";
            }

            string starter = " of ";
            if (grps.Count() == 1 && grps.LastOrDefault().Key == 1)
            {
                starter = "The ";
            }

            html += "<span class=\"setvalue\">";
            switch (ft)
            {
                case RuminantFeedActivityTypes.SpecifiedDailyAmount:
                    html += " per day";
                    break;
                case RuminantFeedActivityTypes.ProportionOfWeight:
                    html += starter + "live weight";
                    break;
                case RuminantFeedActivityTypes.ProportionOfPotentialIntake:
                    html += starter + "potential intake";
                    break;
                case RuminantFeedActivityTypes.ProportionOfRemainingIntakeRequired:
                    html += starter + "remaining intake";
                    break;
                default:
                    break;
            }
            html += "</span> is fed each month to the individuals that match the following conditions:";

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
            if (!(Apsim.Children(this, typeof(RuminantFilter)).Count() >= 1))
            {
                html += "<div class=\"filter\">All individuals</div>";
            }
            return html;
        }

    }
}
