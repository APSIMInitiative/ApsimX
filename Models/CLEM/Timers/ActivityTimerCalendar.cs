using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.EMMA;
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
    /// Activity timer based on calendar that handles specific date ranges, month ranges, and day of year ranges for all CLEM time-steps as well as being able to handle floating ranges with a defined repeat interval.
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
    [Description("This timer defines a calendar period in which to perform activities")]
    [HelpUri(@"Content/Features/Timers/Calendar.htm")]
    [Version(1, 0, 1, "Release version based on monthly time-step.")]
    [Version(2, 0, 1, "New combined timer that handles all time-step intervals.")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    public class ActivityTimerCalendar : CLEMModel, IActivityTimer, IActivityPerformedNotifier
    {
        [Link]
        private readonly CLEMEvents events = null;

        private TimerRange Range { get; set; }
        private int lastAssessedTimeStepIndex = -1;
        private bool lastAssessedOutcome = false;

        /// <summary>
        /// Provides the start of range in a user friendly format of "years (optional), months (optional), days"
        /// </summary>
        [Description("Start of range details")]
        [Core.Display(SubstituteSubPropertyName = "Parts")]
        [Category("Timer", "Range")]
        public AgeSpecifier StartDetails { get; set; } = new int[] { 0, 0, 0 };

        /// <summary>
        /// Provides the end of range in a user friendly format of "years (optional), months (optional), days"
        /// </summary>
        [Description("End of range details")]
        [Core.Display(SubstituteSubPropertyName = "Parts")]
        [Category("Timer", "Range")]
        public AgeSpecifier EndDetails { get; set; } = new int[] { 0, 0, 0 };

        /// <summary>
        /// Provides and interval based on years, months, and days before a floating range is repeated
        /// </summary>
        [Description("Interval between repeated ranges")]
        [Core.Display(SubstituteSubPropertyName = "Parts")]
        [Category("Timer", "Repeat")]
        public AgeSpecifier RepeatInterval { get; set; } = new int[] { 0, 0, 0 };

        /// <summary>
        /// Switch to determine if timer is true when time-step crosses range boundary
        /// </summary>
        [Description("Whole time-step must be in range")]
        [Core.Display(VisibleCallback = "IsSubMonthlyTimeStep")]
        [Category("Timer", "Conditions")]
        public bool WholeTimeStepMustBeInRange { get; set; } = false;

        /// <summary>
        /// Invert (NOT in selected range)
        /// </summary>
        [Description("Invert (NOT in selected range)")]
        [Category("Timer", "Range")]
        [Required]
        public bool Invert { get; set; }

        ///<inheritdoc/>
        public string StatusMessage { get; set; }

        /// <summary>
        /// Activity performed
        /// </summary>
        public event EventHandler ActivityPerformed;

        /// <summary>
        /// Method to determine if a non monthly timestep is being used from CLEMEvents for custom property display
        /// </summary>
        /// <returns></returns>
        public bool IsSubMonthlyTimeStep()
        {
            return (events?.TimeStep ?? TimeStepTypes.Daily) != TimeStepTypes.Monthly;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public ActivityTimerCalendar()
        {
            ModelSummaryStyle = HTMLSummaryStyle.Filter;
            this.SetDefaults();
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseActivity")]
        private void OnCLEMInitialiseActivity(object sender, EventArgs e)
        {
            Range = new(events, StartDetails, EndDetails, RepeatInterval, WholeTimeStepMustBeInRange, FindAllChildren<ActivityTimerSequence>());
        }

        /// <summary>
        /// Method to determine whether the activity is due
        /// </summary>
        /// <returns>Whether the activity is due in the current time-step</returns>
        public bool ActivityDue
        {
            get
            {
                if (events is null)
                    return true;

                if (events.IntervalIndex == lastAssessedTimeStepIndex)
                    return lastAssessedOutcome;


                bool inrange = Range.IsInRange();
                inrange = (Invert) ? !inrange : inrange;
                if (inrange)
                {
                    // report activity performed.
                    ActivityPerformedEventArgs activitye = new()
                    {
                        Name = Name,
                        Status = ActivityStatus.Timer,
                        Id = UniqueID.ToString(),
                    };
                    OnActivityPerformed(activitye);
                }
                lastAssessedTimeStepIndex = events.IntervalIndex;
                lastAssessedOutcome = inrange;

                return inrange;
            }
        }

        /// <inheritdoc/>
        public bool Check(DateTime dateToCheck)
        {
            bool inrange = IsInRange(dateToCheck);
            return (Invert) ? !inrange : inrange;
        }

        private bool IsInRange(DateTime date)
        {
            bool inrange = Range.IsInRange();
            return (Invert) ? !inrange : inrange;
        }

        /// <inheritdoc/>
        public virtual void OnActivityPerformed(EventArgs e)
        {
            ActivityPerformed?.Invoke(this, e);
        }

        #region validation

        /// <inheritdoc/>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // if all details blank error for each of start and end
            foreach (string message in Range.Start.ErrorMessages)
            {
                yield return new ValidationResult(message, new[] { nameof(StartDetails) });
            }
            foreach (string message in Range.End.ErrorMessages)
            {
                yield return new ValidationResult(message, new[] { nameof(EndDetails) });
            }

            if (Range.Start.Date > Range.End.Date)
            {
                yield return new ValidationResult("Start date must be before end date", new[] { nameof(StartDetails), nameof(EndDetails) });
            }

            // Check if either but not both Start and End ymd.year = 0
            if ((Range.Start.ymd.year == 0 && Range.End.ymd.year != 0) || (Range.Start.ymd.year != 0 && Range.End.ymd.year == 0))
            {
                yield return new ValidationResult("Either both or neither of Start and End year must be 0", new[] { nameof(StartDetails), nameof(EndDetails) });
            }
        }

        #endregion

        #region descriptive summary

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            Clock clock = this.FindInScope<Clock>();
            CLEMEvents clemEvents = this.FindInScope<CLEMEvents>();
            clemEvents.Clock = clock;
            if (clock is null || clemEvents is null)
                return $"<div class=\"filter\"><span class=\"errorlink\">No CLEM Events component provided below Clock</span></div>";

            TimerRange range = new(clemEvents, StartDetails, EndDetails, RepeatInterval, WholeTimeStepMustBeInRange, FindAllChildren<ActivityTimerSequence>(), true);

            using StringWriter htmlWriter = new();
            htmlWriter.Write("\r\n<div class=\"filter\">");

            string invertString = (Invert) ? "when <b>NOT</b> " : "";

            if (range.Start.ymd.month == range.End.ymd.month & (range.Start.IsMonthOnly | range.Start.ymd.day == range.End.ymd.day))
            {
                if (range.Start.ymd.month > 0 & range.Start.IsMonthOnly)
                    htmlWriter.Write($"Perform {invertString} in ");
                else
                    htmlWriter.Write($"Perform {invertString} on ");
            }
            else
            {
                htmlWriter.Write($"Perform {invertString} between ");
            }

            if (range.Start.ErrorMessages.Count() > 0)
                htmlWriter.Write($"<span class=\"errorlink\">{range.Start.ErrorMessages.First()}</span>");
            else
                htmlWriter.Write($"<span class=\"setvalueextra\">{range.Start.ToString()}</span>");

            if (range.Start.ymd.month != range.End.ymd.month | (range.Start.IsMonthOnly == false & range.Start.ymd.day != range.End.ymd.day))
            {
                htmlWriter.Write($" and ");
                if (range.End.ErrorMessages.Count() > 0)
                    htmlWriter.Write($"<span class=\"errorlink\">{range.End.ErrorMessages.First()}</span>");
                else
                    htmlWriter.Write($"<span class=\"setvalueextra\">{range.End.ToString()}</span>");
            }

            if (range.IsFloatingRange)
                htmlWriter.Write($"<span class=\"setvalueextra\">{range.RepeatIntervalToString()}</span>");

            if (range.WholeTimeStepInRange)
                htmlWriter.Write($"<span class=\"setvalueextra\">where whole time-step must be in range</span>");

            htmlWriter.Write("</div>");
            if (!this.Enabled & !FormatForParentControl)
                htmlWriter.Write(" - DISABLED!");
            return htmlWriter.ToString();
        }

        /// <inheritdoc/>
        public override string ModelSummaryClosingTags()
        {
            return "</div>";
        }

        /// <inheritdoc/>
        public override string ModelSummaryOpeningTags()
        {
            using StringWriter htmlWriter = new();
            htmlWriter.Write("<div class=\"filtername\">");
            if (!this.Name.Contains(this.GetType().Name.Split('.').Last()))
                htmlWriter.Write(this.Name);
            htmlWriter.Write($"</div>");
            htmlWriter.Write("\r\n<div class=\"filterborder clearfix\" style=\"opacity: " + SummaryOpacity(FormatForParentControl).ToString() + "\">");
            return htmlWriter.ToString();
        }

        #endregion

    }
}
