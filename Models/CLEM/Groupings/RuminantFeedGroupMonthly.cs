using Models.CLEM.Activities;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Models.CLEM.Groupings
{
    ///<summary>
    /// Contains a group of filters and sorts to identify individual ruminants
    ///</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantActivityFeed))]
    [Description("Set monthly feeding values for specified individual ruminants")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Filters/Groups/RuminantFeedGroupMonthly.htm")]
    public class RuminantFeedGroupMonthly : RuminantFeedGroup, IValidatableObject
    {
        [Link]
        private IClock clock = null;

        /// <summary>
        /// Daily value to supply for each month
        /// </summary>
        [Description("Daily value to supply for each month")]
        [Required, ArrayItemCount(12)]
        public double[] MonthlyValues { get; set; }

        /// <inheritdoc/>
        public override double CurrentValue
        {
            get { return MonthlyValues[clock.Today.Month - 1]; }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public RuminantFeedGroupMonthly()
        {
            MonthlyValues = new double[12];
        }

        #region validation

        /// <inheritdoc/>>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (MonthlyValues.Length > 0)
            {
                if (MonthlyValues.Max() == 0)
                {
                    Summary.WriteMessage(this, $"No feed values were defined for any month in [{this.Name}]. No feeding will be performed for [a={this.Parent.Name}]", MessageType.Warning);
                }
            }
            return null;
        }

        #endregion

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using StringWriter htmlWriter = new();
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

        #endregion

    }
}
