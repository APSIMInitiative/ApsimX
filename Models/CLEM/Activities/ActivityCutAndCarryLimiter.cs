using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
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
    [Version(1, 0, 1, "Adam Liedloff", "CSIRO", "")]
    public class ActivityCutAndCarryLimiter: CLEMModel
    {
        /// <summary>
        /// Monthly weight limit (kg/day)
        /// </summary>
        [Description("Monthly weight limit (dry kg/day)")]
        [ArrayItemCount(12)]
        [Units("kg/day")]
        public double[] WeightLimitPerDay { get; set; }

        private double AmountUsedThisMonth = 0;

        /// <summary>
        /// Get the amount of cut and carry available.
        /// </summary>
        /// <param name="Weight"></param>
        public void AddWeightCarried(double Weight)
        {
            AmountUsedThisMonth += Weight;
        }

        /// <summary>
        /// Method to get the amount still available
        /// </summary>
        /// <param name="month"></param>
        /// <returns></returns>
        public double GetAmountAvailable(int month)
        {
            return (WeightLimitPerDay[month - 1] * 30.4) - AmountUsedThisMonth;
        }

        /// <summary>An event handler to allow us to initialise</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMStartOfTimeStep")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            AmountUsedThisMonth = 0;
        }

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="FormatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool FormatForParentControl)
        {
            string html = "";
            html += "\n<div class=\"filterborder clearfix\">";
            html += "\n<div class=\"filter\">";
            html += "Limit cut and carry activities to <span class=\"setvalueextra\">";
            html += WeightLimitPerDay.ToString();
            html += "</span> dry kg/day ";
            html += "</div>";
            html += "\n</div>";
            return html;
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryClosingTags(bool FormatForParentControl)
        {
            return "";
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryOpeningTags(bool FormatForParentControl)
        {
            return "";
        }

    }
}
