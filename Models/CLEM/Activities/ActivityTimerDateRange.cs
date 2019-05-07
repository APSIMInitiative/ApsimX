using Models.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Models.CLEM.Resources;
using Models.Core.Attributes;

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
    [Description("This activity timer defines a date range to perfrom activities.")]
    [HelpUri(@"content/features/timers/daterange.htm")]
    [Version(1, 0, 1, "")]
    public class ActivityTimerDateRange : CLEMModel, IActivityTimer, IActivityPerformedNotifier
    {
        [XmlIgnore]
        [Link]
        Clock Clock = null;

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

        /// <summary>
        /// Activity performed
        /// </summary>
        public event EventHandler ActivityPerformed;

        /// <summary>
        /// Constructor
        /// </summary>
        public ActivityTimerDateRange()
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
                bool inrange = IsMonthInRange(Clock.Today);
                if(inrange)
                {
                    // report activity performed.
                    ActivityPerformedEventArgs activitye = new ActivityPerformedEventArgs
                    {
                        Activity = new BlankActivity()
                        {
                            Status = ActivityStatus.Timer,
                            Name = this.Name
                        }
                    };
                    activitye.Activity.SetGuID(this.UniqueID);
                    this.OnActivityPerformed(activitye);
                }
                return inrange;
            }
        }

        /// <summary>
        /// Method to determine whether the activity is due based on a specified date
        /// </summary>
        /// <returns>Whether the activity is due based on the specified date</returns>
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
            {
                inrange = !inrange;
            }
            return inrange;
        }

        /// <summary>
        /// Activity has occurred 
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnActivityPerformed(EventArgs e)
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
            DateTime endDate = new DateTime(EndDate.Year, EndDate.Month, DateTime.DaysInMonth(EndDate.Year, EndDate.Month));
            DateTime startDate = new DateTime(StartDate.Year, StartDate.Month, 1);

            string html = "";
            html += "\n<div class=\"filterborder clearfix\">";
            html += "\n<div class=\"filter\">";
            string invertString = "";
            if (Invert)
            {
                invertString = "when <b>NOT</b> ";
            }
            html += "Perform "+invertString+"between <span class=\"setvalueextra\">";
            html += startDate.ToString("d MMM yyyy");
            html += "</span> and ";
            if (EndDate <= StartDate)
            {
                html += "<span class=\"errorlink\">[must be > StartDate]";
            }
            else
            {
                html += "<span class=\"setvalueextra\">";
                html += endDate.ToString("d MMM yyyy");
            }
            html += "</span>";
            if(StartDate!=startDate || EndDate != endDate)
            {
                html += " (modified for monthly timestep)";
            }
            html += "</div>";
            html += "\n</div>";
            return html;
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

    }
}
