using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;

namespace Models.CLEM.Activities
{
    ///<summary>
    /// Target for feed activity
    ///</summary> 
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(LabourActivityFeedToTargets))]
    [Description("This component defines a target to be achieved when trying to feed people to set targets")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/Activities/Labour/LabourActivityFeedTarget.htm")]
    public class LabourActivityFeedTarget: CLEMModel
    {
        /// <summary>
        /// Name of metric to base this target upon
        /// </summary>
        [Description("Metric name")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Metric name required")]
        public string Metric { get; set; }

        /// <summary>
        /// Target level
        /// </summary>
        [Description("Target level")]
        [Units("units per Ae per day")]
        [GreaterThanValue(0)]
        public double TargetValue { get; set; }

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
        /// Stored level achieved
        /// </summary>
        [JsonIgnore]
        public double CurrentAchieved { get; set; }

        /// <summary>
        /// Has target been achieved
        /// </summary>
        [JsonIgnore]
        public bool TargetMet { get { return CurrentAchieved >= Target; } }

        /// <summary>
        /// Constructor
        /// </summary>
        public LabourActivityFeedTarget()
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
                htmlWriter.Write("\r\n<div class=\"activityentry\">");
                if (Metric is null || Metric == "")
                {
                    htmlWriter.Write("<span class=\"errorlink\">METRIC NOT SET</span>: ");
                }
                else
                {
                    htmlWriter.Write("<span class=\"setvalue\">" + Metric + "</span>: ");
                }
                if (TargetValue > 0)
                {
                    htmlWriter.Write("<span class=\"setvalue\">");
                    htmlWriter.Write(TargetValue.ToString("#,##0.##"));
                }
                else
                {
                    htmlWriter.Write("<span class=\"errorlink\">VALUE NOT SET");
                }
                htmlWriter.Write("</span> units per AE per day</div>");

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
