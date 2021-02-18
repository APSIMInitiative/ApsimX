using Models.Core;
using Models.CLEM.Activities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core.Attributes;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Newtonsoft.Json;
using Models.CLEM.Resources;
using System.IO;

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
        [Required, ArrayItemCount(12)]
        public double[] MonthlyValues { get; set; }

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
        public RuminantFeedGroupMonthly()
        {
            MonthlyValues = new double[12];
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

            if (MonthlyValues.Count() > 0)
            {
                if (MonthlyValues.Max() == 0)
                {
                    Summary.WriteWarning(this, $"No feed values were defined for any month in [{this.Name}]. No feeding will be performed for [a={this.Parent.Name}]");
                }
            }
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
            using (StringWriter htmlWriter = new StringWriter())
            {
                // get amount
                var grps = MonthlyValues.GroupBy(a => a).OrderBy(a => a.Key);

                RuminantFeedActivityTypes ft = (this.Parent as RuminantActivityFeed).FeedStyle;
                htmlWriter.Write("\r\n<div class=\"activityentry\">");
                if (grps.Count() > 1)
                {
                    htmlWriter.Write("From ");
                }
                htmlWriter.Write("<span class=\"setvalue\">");
                switch (ft)
                {
                    case RuminantFeedActivityTypes.SpecifiedDailyAmount:
                        htmlWriter.Write(grps.FirstOrDefault().Key.ToString() + "kg");
                        break;
                    case RuminantFeedActivityTypes.ProportionOfWeight:
                    case RuminantFeedActivityTypes.ProportionOfPotentialIntake:
                    case RuminantFeedActivityTypes.ProportionOfRemainingIntakeRequired:
                    case RuminantFeedActivityTypes.ProportionOfFeedAvailable:
                        if (grps.LastOrDefault().Key != 1)
                        {
                            htmlWriter.Write((Convert.ToDecimal(grps.FirstOrDefault().Key, CultureInfo.InvariantCulture)).ToString("0.##%"));
                        }
                        break;
                    default:
                        break;
                }
                htmlWriter.Write("</span>");

                if (grps.Count() > 1)
                {
                    htmlWriter.Write(" to ");
                    htmlWriter.Write("<span class=\"setvalue\">");
                    switch (ft)
                    {
                        case RuminantFeedActivityTypes.SpecifiedDailyAmount:
                            htmlWriter.Write(grps.LastOrDefault().Key.ToString() + "kg");
                            break;
                        case RuminantFeedActivityTypes.ProportionOfWeight:
                        case RuminantFeedActivityTypes.ProportionOfPotentialIntake:
                        case RuminantFeedActivityTypes.ProportionOfRemainingIntakeRequired:
                        case RuminantFeedActivityTypes.ProportionOfFeedAvailable:
                            htmlWriter.Write((Convert.ToDecimal(grps.LastOrDefault().Key, CultureInfo.InvariantCulture)).ToString("0.##%"));
                            break;
                        default:
                            break;
                    }
                    htmlWriter.Write("</span>");
                }

                string starter = " of ";
                if (grps.Count() == 1 && grps.LastOrDefault().Key == 1)
                {
                    starter = "The ";
                }

                bool overfeed = false;

                htmlWriter.Write("<span class=\"setvalue\">");
                switch (ft)
                {
                    case RuminantFeedActivityTypes.ProportionOfFeedAvailable:
                        htmlWriter.Write(" feed available");
                        overfeed = true;
                        break;
                    case RuminantFeedActivityTypes.SpecifiedDailyAmountPerIndividual:
                        htmlWriter.Write(" per individual per day");
                        overfeed = true;
                        break;
                    case RuminantFeedActivityTypes.SpecifiedDailyAmount:
                        htmlWriter.Write(" per day");
                        overfeed = true;
                        break;
                    case RuminantFeedActivityTypes.ProportionOfWeight:
                        htmlWriter.Write(starter + "live weight");
                        overfeed = true;
                        break;
                    case RuminantFeedActivityTypes.ProportionOfPotentialIntake:
                        htmlWriter.Write(starter + "potential intake");
                        break;
                    case RuminantFeedActivityTypes.ProportionOfRemainingIntakeRequired:
                        htmlWriter.Write(starter + "remaining intake");
                        break;
                    default:
                        break;
                }
                htmlWriter.Write("</span> is fed each month to the individuals that match the following conditions:");

                htmlWriter.Write("</div>");

                if (overfeed)
                {
                    htmlWriter.Write("\r\n<div class=\"activityentry\">");
                    htmlWriter.Write("Individual's intake will be limited to Potential intake x the modifer for max overfeeding, with excess food still utilised but wasted");
                    htmlWriter.Write("</div>");
                }

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
                if (!(this.FindAllChildren<RuminantFilter>().Count() >= 1))
                {
                    htmlWriter.Write("<div class=\"filter\">All individuals</div>");
                }
                return htmlWriter.ToString();
            }
        } 
        #endregion

    }
}
