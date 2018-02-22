using Models.CLEM.Resources;
using Models.Core;
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
    [Description("This activity time defines a start month and interval upon which to perform activities.")]
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
                if (this.NextDueDate.Year == Clock.Today.Year & this.NextDueDate.Month == Clock.Today.Month)
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
                    this.OnActivityPerformed(activitye);
                }
                return (this.NextDueDate.Year == Clock.Today.Year & this.NextDueDate.Month == Clock.Today.Month);
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
        [EventSubscribe("CLEMInitialiseActivity")]
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
        protected virtual void OnActivityPerformed(EventArgs e)
        {
            if (ActivityPerformed != null)
                ActivityPerformed(this, e);
        }

    }
}
