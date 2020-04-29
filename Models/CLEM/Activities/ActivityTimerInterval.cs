using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Models.CLEM.Activities
{
    /// <summary>
    /// Activity timer based on monthly interval
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ResourcePricing))]
    [Description("This activity time defines a start month and interval upon which to perform activities.")]
    [HelpUri(@"Content/Features/Timers/Interval.htm")]
    [Version(1, 0, 1, "")]
    public class ActivityTimerInterval: CLEMModel, IActivityTimer, IActivityPerformedNotifier
    {
        [XmlIgnore]
        [Link]
        Clock Clock = null;

        /// <summary>
        /// Notify CLEM that timer was ok
        /// </summary>
        public event EventHandler ActivityPerformed;

        /// <summary>
        /// The payment interval (in months, 1 monthly, 12 annual)
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(12)]
        [Description("The interval (in months, 1 monthly, 12 annual)")]
        [Required, GreaterThanValue(1)]
        public int Interval { get; set; }

        /// <summary>
        /// First month to pay overhead
        /// </summary>
        [System.ComponentModel.DefaultValueAttribute(1)]
        [Description("First month to start interval (1-12)")]
        [Required, Month]
        public int MonthDue { get; set; }

        /// <summary>
        /// Month this overhead is next due.
        /// </summary>
        [XmlIgnore]
        public DateTime NextDueDate { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ActivityTimerInterval()
        {
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
                return (this.NextDueDate.Year == Clock.Today.Year && this.NextDueDate.Month == Clock.Today.Month);
            }
        }

        /// <summary>
        /// Method to determine whether the activity is due based on a specified date
        /// </summary>
        /// <returns>Whether the activity is due based on the specified date</returns>
        public bool Check(DateTime dateToCheck)
        {
            // compare with next due date
            if (this.NextDueDate.Year == Clock.Today.Year && this.NextDueDate.Month == Clock.Today.Month)
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
                    {
                        return true;
                    }

                    dd = dd.AddMonths(Interval*-1);
                }
                return false;
            }
            else
            {
                while (dd2c >= dd)
                {
                    if (dd2c == dd)
                    {
                        return true;
                    }

                    dd = dd.AddMonths(Interval);
                }
                return false;
            }
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("EndOfMonth")]
        private void OnEndOfMonth(object sender, EventArgs e)
        {
            if (this.ActivityDue)
            {
                NextDueDate = NextDueDate.AddMonths(Interval);
            }
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            if (MonthDue >= Clock.StartDate.Month)
            {
                NextDueDate = new DateTime(Clock.StartDate.Year, MonthDue, Clock.StartDate.Day);
            }
            else
            {
                NextDueDate = new DateTime(Clock.StartDate.Year, MonthDue, Clock.StartDate.Day);
                while (Clock.StartDate > NextDueDate)
                {
                    NextDueDate = NextDueDate.AddMonths(Interval);
                }
            }

        }

        /// <summary>
        /// Activity has occurred 
        /// </summary>
        /// <param name="e"></param>
        public virtual void OnActivityPerformed(EventArgs e)
        {
            ActivityPerformed?.Invoke(this, e);
        }

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "";
            html += "\n<div class=\"filter\">";
            html += "Perform every ";
            if (Interval > 0)
            {
                html += "<span class=\"setvalueextra\">";
                html += Interval.ToString();
            }
            else
            {
                html += "<span class=\"errorlink\">";
                html += "NOT SET";
            }
            html += "</span> months from ";
            if(MonthDue > 0)
            {
                html += "<span class=\"setvalueextra\">";
                html += new DateTime(2000, MonthDue, 1).ToString("MMMM");
            }
            else
            {
                html += "<span class=\"errorlink\">";
                html += "NOT SET";
            }
            html += "</span></div>";
            if(!this.Enabled)
            {
                html += " - DISABLED!";
            }
            return html;
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryClosingTags(bool formatForParentControl)
        {
            return "</div>";
        }

        /// <summary>
        /// Provides the closing html tags for object
        /// </summary>
        /// <returns></returns>
        public override string ModelSummaryOpeningTags(bool formatForParentControl)
        {
            string html = "";
            html += "\n<div class=\"filterborder clearfix\" style=\"opacity: " + SummaryOpacity(formatForParentControl).ToString() + "\">";
            return html;
        }

    }
}
