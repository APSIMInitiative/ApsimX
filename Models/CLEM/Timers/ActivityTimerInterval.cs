using Models.CLEM.Reporting;
using Models.CLEM.Activities;
using Models.CLEM.Interfaces;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Newtonsoft.Json;
using System.IO;

namespace Models.CLEM.Timers
{
    /// <summary>
    /// Activity timer based on monthly interval
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ResourcePricing))]
    [ValidParent(ParentType = typeof(ReportResourceBalances))]
    [ValidParent(ParentType = typeof(SummariseRuminantHerd))]
    [ValidParent(ParentType = typeof(ReportRuminantHerd))]
    [Description("This timer defines a start month and interval upon which to perform activities.")]
    [HelpUri(@"Content/Features/Timers/Interval.htm")]
    [Version(1, 0, 1, "")]
    public class ActivityTimerInterval: CLEMModel, IActivityTimer, IActivityPerformedNotifier
    {
        [Link]
        private Clock clock = null;

        /// <summary>
        /// Notify CLEM that timer was ok
        /// </summary>
        public event EventHandler ActivityPerformed;

        /// <summary>
        /// The payment interval (in months, 1 monthly, 12 annual)
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(12)]
        [Description("The interval (in months, 1 monthly, 12 annual)")]
        [Required, GreaterThanEqualValue(1)]
        public int Interval { get; set; }

        /// <summary>
        /// First month to start interval
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(1)]
        [Description("First month to start interval")]
        [Required, Month]
        public MonthsOfYear MonthDue { get; set; }

        /// <summary>
        /// Month this timer is next due.
        /// </summary>
        [JsonIgnore]
        public DateTime NextDueDate { get; set; }

        ///<inheritdoc/>
        public string StatusMessage { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ActivityTimerInterval()
        {
            ModelSummaryStyle = HTMLSummaryStyle.Filter;
            this.SetDefaults();
        }

        /// <inheritdoc/>
        public bool ActivityDue
        {
            get
            {
                return (this.NextDueDate.Year == clock.Today.Year && this.NextDueDate.Month == clock.Today.Month);
            }
        }

        /// <inheritdoc/>
        public bool Check(DateTime dateToCheck)
        {
            // compare with next due date
            if (this.NextDueDate.Year == clock.Today.Year && this.NextDueDate.Month == clock.Today.Month)
            {
                return true;
            }
            DateTime dd = new DateTime(this.NextDueDate.Year, this.NextDueDate.Month, 1);
            DateTime dd2c = new DateTime(dateToCheck.Year, dateToCheck.Month, 1);

            int direction = (dd2c < dd) ? -1 : 1;
            if(direction < 0)
            {
                while(dd2c<=dd)
                {
                    if (dd2c == dd)
                        return true;

                    dd = dd.AddMonths(Interval*-1);
                }
                return false;
            }
            else
            {
                while (dd2c >= dd)
                {
                    if (dd2c == dd)
                        return true;

                    dd = dd.AddMonths(Interval);
                }
                return false;
            }
        }

        /// <summary>An event handler to move timer setting to next timing.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("EndOfMonth")]
        private void OnEndOfMonth(object sender, EventArgs e)
        {
            if (this.ActivityDue)
                NextDueDate = NextDueDate.AddMonths(Interval);
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            int monthDue = (int)MonthDue;
            if (monthDue != 0)
            {
                if (monthDue >= clock.StartDate.Month)
                    NextDueDate = new DateTime(clock.StartDate.Year, monthDue, clock.StartDate.Day);
                else
                {
                    NextDueDate = new DateTime(clock.StartDate.Year, monthDue, clock.StartDate.Day);
                    while (clock.StartDate > NextDueDate)
                        NextDueDate = NextDueDate.AddMonths(Interval);
                }
            }
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
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"filter\">");
                htmlWriter.Write("Perform every ");
                if (Interval > 0)
                {
                    htmlWriter.Write("<span class=\"setvalueextra\">");
                    htmlWriter.Write(Interval.ToString());
                }
                else
                {
                    htmlWriter.Write("<span class=\"errorlink\">");
                    htmlWriter.Write("NOT SET");
                }
                htmlWriter.Write($"</span> month{((Interval == 1)?"":"s")} from ");
                if (MonthDue > 0)
                {
                    htmlWriter.Write("<span class=\"setvalueextra\">");
                    htmlWriter.Write(MonthDue.ToString());
                }
                else
                {
                    htmlWriter.Write("<span class=\"errorlink\">");
                    htmlWriter.Write("NOT SET");
                }
                htmlWriter.Write("</span></div>");
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
