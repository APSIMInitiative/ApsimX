using Models.Core;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Newtonsoft.Json;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core.Attributes;
using System.IO;
using Models.CLEM.Reporting;
using Models.CLEM.Activities;

namespace Models.CLEM.Timers
{
    /// <summary>
    /// Activity timer based on date range
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ResourcePricing))]
    [ValidParent(ParentType = typeof(SummariseRuminantHerd))]
    [ValidParent(ParentType = typeof(ReportRuminantHerd))]
    [Description("This timer defines a date range in which to perform activities")]
    [HelpUri(@"Content/Features/Timers/DateRange.htm")]
    [Version(1, 0, 1, "")]
    public class ActivityTimerDateRange : CLEMModel, IActivityTimer, IActivityPerformedNotifier
    {
        [Link]
        private Clock clock = null;

        /// <summary>
        /// Start date of period to perform activities
        /// </summary>
        [Description("Start date of period to perform activities")]
        [System.ComponentModel.DefaultValue(typeof(DateTime), "1/1/1900")]
        [Required]
        public DateTime StartDate { get; set; }
        
        /// <summary>
        /// End date of period to perform activities
        /// </summary>
        [Description("End date of period to perform activities")]
        [System.ComponentModel.DefaultValue(typeof(DateTime), "1/1/1900")]
        [DateGreaterThanAttribute("StartDate", ErrorMessage = "End date must be greater than Start date")]
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Invert (NOT in selected range)
        /// </summary>
        [Description("Invert (NOT in selected range)")]
        [Required]
        public bool Invert { get; set; }

        ///<inheritdoc/>
        public string StatusMessage { get; set; }

        /// <summary>
        /// Activity performed
        /// </summary>
        public event EventHandler ActivityPerformed;

        /// <summary>
        /// Constructor
        /// </summary>
        public ActivityTimerDateRange()
        {
            ModelSummaryStyle = HTMLSummaryStyle.Filter;
            this.SetDefaults();
        }

        /// <summary>
        /// Method to determine whether the activity is due
        /// </summary>
        /// <returns>Whether the activity is due in the current month</returns>
        public bool ActivityDue
        {
            get
            {
                if (clock is null)
                {
                    return true;
                }
                bool inrange = IsMonthInRange(clock.Today);
                if(inrange)
                {
                    // report activity performed.
                    ActivityPerformedEventArgs activitye = new ActivityPerformedEventArgs
                    {
                        Name = this.Name,
                        Status = ActivityStatus.Timer,
                        Id = this.UniqueID.ToString(),
                    };
                    this.OnActivityPerformed(activitye);
                }
                return inrange;
            }
        }

        /// <inheritdoc/>
        public bool Check(DateTime dateToCheck)
        {
            return IsMonthInRange(dateToCheck);
        }

        private bool IsMonthInRange(DateTime date)
        {
            DateTime endDate = new DateTime(EndDate.Year, EndDate.Month, DateTime.DaysInMonth(EndDate.Year, EndDate.Month));
            DateTime startDate = new DateTime(StartDate.Year, StartDate.Month, 1);

            bool inrange = ((date >= startDate) && (date <= endDate));
            if (Invert)
                inrange = !inrange;
            return inrange;
        }

        /// <inheritdoc/>
        public virtual void OnActivityPerformed(EventArgs e)
        {
            ActivityPerformed?.Invoke(this, e);
        }

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            DateTime endDate = new DateTime(EndDate.Year, EndDate.Month, DateTime.DaysInMonth(EndDate.Year, EndDate.Month));
            DateTime startDate = new DateTime(StartDate.Year, StartDate.Month, 1);

            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"filter\">");
                string invertString = "";
                if (Invert)
                    invertString = "when <b>NOT</b> ";

                htmlWriter.Write("Perform " + invertString + "between ");
                if (startDate.Year == 1)
                    htmlWriter.Write("<span class=\"errorlink\">NOT SET</span>");
                else
                {
                    htmlWriter.Write("<span class=\"setvalueextra\">");
                    htmlWriter.Write(startDate.ToString("d MMM yyyy"));
                    htmlWriter.Write("</span>");
                }
                htmlWriter.Write(" and ");
                if (EndDate <= StartDate)
                    htmlWriter.Write("<span class=\"errorlink\">[must be > StartDate]");
                else
                {
                    htmlWriter.Write("<span class=\"setvalueextra\">");
                    htmlWriter.Write(endDate.ToString("d MMM yyyy"));
                }
                htmlWriter.Write("</span>");
                if (StartDate != startDate || EndDate != endDate)
                    htmlWriter.Write(" (modified for monthly timestep)");
                htmlWriter.Write("</div>");
                if (!this.Enabled & !FormatForParentControl)
                    htmlWriter.Write(" - DISABLED!");
                return htmlWriter.ToString(); 
            }
        }

        /// <inheritdoc/>
        public override string ModelSummaryClosingTags()
        {
            return "</div>";
        }

        /// <inheritdoc/>
        public override string ModelSummaryOpeningTags()
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("<div class=\"filtername\">");
                if (!this.Name.Contains(this.GetType().Name.Split('.').Last()))
                    htmlWriter.Write(this.Name);
                htmlWriter.Write($"</div>");
                htmlWriter.Write("\r\n<div class=\"filterborder clearfix\" style=\"opacity: " + SummaryOpacity(FormatForParentControl).ToString() + "\">");
                return htmlWriter.ToString(); 
            }
        } 
        #endregion

    }
}
