using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Activities
{
    /// <summary>
    /// Cut and carry Activity limiter
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [Description("This cut and carry limiter will limit the amount of cut and carry possible for all activities located at or below the UI level is it placed.")]
    [HelpUri(@"Content/Features/Limiters/CutAndCarryLimiter.htm")]
    [Version(1, 0, 1, "")]
    public class ActivityCutAndCarryLimiter: CLEMModel
    {
        /// <summary>
        /// Monthly weight limit (kg/day)
        /// </summary>
        [Description("Monthly weight limit (dry kg/day)")]
        [ArrayItemCount(12)]
        [Units("kg/day")]
        public double[] WeightLimitPerDay { get; set; }

        private double amountUsedThisMonth = 0;

        /// <summary>
        /// Get the amount of cut and carry available.
        /// </summary>
        /// <param name="weight"></param>
        public void AddWeightCarried(double weight)
        {
            amountUsedThisMonth += weight;
        }

        /// <summary>
        /// Method to get the amount still available
        /// </summary>
        /// <param name="month"></param>
        /// <returns></returns>
        public double GetAmountAvailable(int month)
        {
            return (WeightLimitPerDay[month - 1] * 30.4) - amountUsedThisMonth;
        }

        /// <summary>An event handler to allow us to initialise</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMStartOfTimeStep")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            amountUsedThisMonth = 0;
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
                htmlWriter.Write("\r\n<div class=\"filterborder clearfix\">");
                htmlWriter.Write("\r\n<div class=\"filter\">");
                htmlWriter.Write("Limit cut and carry activities to ");
                if (!(WeightLimitPerDay is null) && WeightLimitPerDay.Count() >= 1)
                {
                    htmlWriter.Write("<span class=\"setvalueextra\">");
                    htmlWriter.Write(WeightLimitPerDay.ToString());
                }
                else
                {
                    htmlWriter.Write("<span class=\"errorlink\">Not Set");
                }
                htmlWriter.Write("</span> dry kg/day ");
                htmlWriter.Write("</div>");
                htmlWriter.Write("\r\n</div>");
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
        #endregion

    }
}
