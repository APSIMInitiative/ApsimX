using Models.CLEM.Activities;
using Models.CLEM.Interfaces;
using Models.CLEM.Reporting;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;

namespace Models.CLEM.Timers
{
    /// <summary>
    /// Activity timer based on month range
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(CLEMActivityBase))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [ValidParent(ParentType = typeof(ActivitiesHolder))]
    [ValidParent(ParentType = typeof(ResourcePricing))]
    [ValidParent(ParentType = typeof(GrazeFoodStoreFertilityLimiter))]
    [ValidParent(ParentType = typeof(SummariseRuminantHerd))]
    [ValidParent(ParentType = typeof(ReportRuminantHerd))]
    [Description("This timer defines a range between months upon which to perform activities.")]
    [HelpUri(@"Content/Features/Timers/MonthRange.htm")]
    [Version(1, 0, 1, "")]
    public class ActivityTimerMonthRange : CLEMModel, IActivityTimer, IActivityPerformedNotifier
    {
        [Link]
        private IClock clock = null;
        private DateTime lastCheck = DateTime.MinValue;
        private bool lastReturn = false;

        private int startMonth;
        private int endMonth;
        private IEnumerable<ActivityTimerSequence> sequenceTimerList;

        /// <summary>
        /// Notify CLEM that this Timer was performed
        /// </summary>
        public event EventHandler ActivityPerformed;

        /// <summary>
        /// Start month of annual period to perform activities
        /// </summary>
        [Description("Start month of annual period to perform activity")]
        [System.ComponentModel.DefaultValueAttribute(1)]
        [Required, Month]
        public MonthsOfYear StartMonth { get; set; }

        /// <summary>
        /// End month of annual period to perform activities
        /// </summary>
        [Description("End month of annual period to perform activity")]
        [Required, Month]
        [System.ComponentModel.DefaultValueAttribute(12)]
        public MonthsOfYear EndMonth { get; set; }

        ///<inheritdoc/>
        public string StatusMessage { get; set; }

        /// <inheritdoc/>
        public bool ActivityDue
        {
            get
            {
                if(clock.Today != lastCheck)
                {
                    lastCheck = clock.Today;
                    lastReturn = IsMonthInRange(clock.Today);
                }
                return lastReturn;
            }
        }

        /// <inheritdoc/>
        public bool Check(DateTime dateToCheck)
        {
            return IsMonthInRange(dateToCheck);
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            sequenceTimerList = Structure.FindChildren<ActivityTimerSequence>();
            startMonth = (int)StartMonth;
            endMonth = (int)EndMonth;
        }

        private bool IsMonthInRange(DateTime date)
        {
            bool due = false;
            if (startMonth <= endMonth)
            {
                if ((date.Month >= startMonth) & (date.Month <= endMonth))
                {
                    due = ActivityTimerSequence.IsInSequence(sequenceTimerList, date.Month - startMonth);
                }
            }
            else
            {
                if ((date.Month <= endMonth) | (date.Month >= startMonth))
                {
                    int? index;
                    if (date.Month >= startMonth)
                    {
                        index = date.Month - startMonth;
                    }
                    else
                    {
                        index = 12 - startMonth + date.Month;
                    }
                    due = ActivityTimerSequence.IsInSequence(sequenceTimerList, index);
                }
            }
            return due;
        }

        /// <inheritdoc/>
        public virtual void OnActivityPerformed(EventArgs e)
        {
            ActivityPerformed?.Invoke(this, e);
        }
    }
}
