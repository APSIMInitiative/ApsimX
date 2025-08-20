using APSIM.Core;
using Models.CLEM.Activities;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.IO;
using System.Linq;

namespace Models.CLEM.Limiters
{
    /// <summary>
    /// Limits the total carried across a range of activities
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [Description("This carry limiter will limit the amount that can be carried for all activities located at or below the UI level it is placed.")]
    [HelpUri(@"Content/Features/Limiters/CutAndCarryLimiter.htm")]
    [Version(1, 0, 1, "")]
    public class ActivityCarryLimiter : CLEMModel
    {
        private double amountUsedThisMonth = 0;

        /// <summary>
        /// Monthly weight limit (kg/day)
        /// </summary>
        [Description("Monthly weight limit (kg/day)")]
        [ArrayItemCount(12)]
        [Units("kg/day")]
        public double[] WeightLimitPerDay { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ActivityCarryLimiter()
        {
            ModelSummaryStyle = HTMLSummaryStyle.Filter;
        }

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

        /// <summary>
        /// Method to locate a ActivityCutAndCarryLimiter from a specified model
        /// </summary>
        /// <param name="model">Model looking for limiter</param>
        /// <param name="structure">Structure instance</param>
        /// <returns></returns>
        public static ActivityCarryLimiter Locate(IModel model, IStructure structure)
        {
            // search children
            ActivityCarryLimiter limiterFound = structure.FindChildren<ActivityCarryLimiter>(relativeTo: model as INodeModel).Cast<ActivityCarryLimiter>().FirstOrDefault();
            if (limiterFound == null)
            {
                if (model.Parent.GetType().IsSubclassOf(typeof(CLEMActivityBase)) || model.Parent.GetType() == typeof(ActivitiesHolder))
                {
                    limiterFound = ActivityCarryLimiter.Locate(model.Parent, structure);
                }
            }
            return limiterFound;
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("<div class=\"filtername\">");
                if (!this.Name.Contains(this.GetType().Name.Split('.').Last()))
                    htmlWriter.Write(this.Name);

                htmlWriter.Write($"</div>");
                htmlWriter.Write("\r\n<div class=\"filterborder clearfix\">");
                htmlWriter.Write("\r\n<div class=\"filter\">");
                htmlWriter.Write($"Limit cut and carry activities to ");
                if (!(WeightLimitPerDay is null) && WeightLimitPerDay.Count() >= 1)
                {
                    htmlWriter.Write("<span class=\"setvalueextra\">");
                    htmlWriter.Write(WeightLimitPerDay.ToString());
                }
                else
                    htmlWriter.Write("<span class=\"errorlink\">Not Set");

                htmlWriter.Write("</span> dry kg/day ");
                htmlWriter.Write("</div>");
                htmlWriter.Write("\r\n</div>");
                return htmlWriter.ToString();
            }
        }

        /// <inheritdoc/>
        public override string ModelSummaryClosingTags()
        {
            return "";
        }

        /// <inheritdoc/>
        public override string ModelSummaryOpeningTags()
        {
            return "";
        }
        #endregion

    }
}
