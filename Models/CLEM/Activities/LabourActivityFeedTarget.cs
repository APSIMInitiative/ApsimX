using Models.Core;
using Models.Core.Attributes;
using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using System.IO;

namespace Models.CLEM.Activities
{
    ///<summary>
    /// Target for feed activity
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(LabourActivityFeedToTargets))]
    [Description("Defines a target to be achieved when trying to feed people to set targets")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Labour/LabourActivityFeedTarget.htm")]
    public class LabourActivityFeedTarget: CLEMModel
    {
        /// <summary>
        /// Name of metric for this target
        /// </summary>
        [Description("Metric name")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Metric name required")]
        public string Metric { get; set; }

        /// <summary>
        /// Target level
        /// </summary>
        [Description("Target level")]
        [Units("units per AE per day")]
        [GreaterThanValue(0)]
        public double TargetValue { get; set; }

        /// <summary>
        /// Target max level
        /// </summary>
        [Description("Target maximum level")]
        [Units("units per AE per day")]
        [GreaterThan("TargetValue")]
        public double TargetMaximumValue { get; set; }

        /// <summary>
        /// Amount from other sources
        /// </summary>
        [Description("Amount from other sources")]
        [Units("units per Ae per day")]
        [GreaterThanEqualValue(0)]
        public double OtherSourcesValue { get; set; }

        /// <summary>
        /// Current target
        /// </summary>
        [JsonIgnore]
        public double Target { get; set; }

        /// <summary>
        /// Current target
        /// </summary>
        [JsonIgnore]
        public double TargetMaximum { get; set; }

        /// <summary>
        /// Stored level achieved
        /// </summary>
        [JsonIgnore]
        public double CurrentAchieved { get; set; }

        /// <summary>
        /// Has target been achieved
        /// </summary>
        [JsonIgnore]
        public bool TargetAchieved { get { return Math.Round(CurrentAchieved, 4) >= Math.Round(Target, 4); } }

        /// <summary>
        /// Has target maximum been achieved
        /// </summary>
        [JsonIgnore]
        public bool TargetMaximumAchieved { get { return Math.Round(CurrentAchieved, 4) >= Math.Round(TargetMaximum, 4); } }

        /// <summary>
        /// Constructor
        /// </summary>
        public LabourActivityFeedTarget()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.SubActivity;
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {

            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"activityentry\">");
                htmlWriter.Write(CLEMModel.DisplaySummaryValueSnippet(Metric, "Metric not set"));
                if (TargetValue > 0)
                {
                    htmlWriter.Write("<span class=\"setvalue\">");
                    htmlWriter.Write(TargetValue.ToString("#,##0.##"));
                }
                else
                    htmlWriter.Write("<span class=\"errorlink\">VALUE NOT SET");

                htmlWriter.Write("</span> units per AE per day ");

                if (TargetMaximumValue > 0)
                {
                    htmlWriter.Write("up to maximum of <span class=\"setvalue\">");
                    htmlWriter.Write(TargetMaximumValue.ToString("#,##0.##"));
                }
                else
                    htmlWriter.Write("<span class=\"errorlink\">VALUE NOT SET");

                htmlWriter.Write("</span></div>");

                if (OtherSourcesValue > 0)
                {
                    htmlWriter.Write("\r\n<div class=\"activityentry\">");
                    htmlWriter.Write("<span class=\"setvalue\">" + OtherSourcesValue.ToString("#,##0.##") + "</span> is provided from sources outside the human food store</div>");
                }
                return htmlWriter.ToString(); 
            }
        } 
        #endregion

    }
}
