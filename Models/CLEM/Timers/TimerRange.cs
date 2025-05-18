using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Timers
{
    /// <summary>
    /// The range class for timers
    /// </summary>
    public class TimerRange
    {
        private readonly CLEMEvents events;
        private bool minimalSetup;

        /// <summary>
        /// Start of range
        /// </summary>
        public TimerRangeItem Start { get; set; }
        /// <summary>
        /// End of range
        /// </summary>
        public TimerRangeItem End { get; set; }

        /// <summary>
        /// Is this a floating range
        /// </summary>
        public bool IsFloatingRange { get; set; } = false;

        /// <summary>
        /// Must use whole time-step in range
        /// </summary>
        public bool WholeTimeStepInRange { get; set; } = false;

        private IEnumerable<ActivityTimerSequence> sequenceTimerList;
        private AgeSpecifier repeatInterval = new int[] { 0, 0, 0 };

        /// <summary>
        /// Constructor
        /// </summary>
        public TimerRange(CLEMEvents events, AgeSpecifier startDetails, AgeSpecifier endDetails, AgeSpecifier repeatInterval, bool wholeTimeStepInRange, IEnumerable<ActivityTimerSequence> sequences, bool minimalSetup = true)
        {
            if (events is null)
                throw new Exception("CLEMEvents link not provided to timer range");

            Start = new(events, startDetails, true, wholeTimeStepInRange);
            End = new(events, endDetails, false, wholeTimeStepInRange);
            this.sequenceTimerList = sequences;
            this.events = events;
            this.WholeTimeStepInRange = wholeTimeStepInRange;
            this.IsFloatingRange = (Start.ymd.year == 0) && (End.ymd.year == 0);
            this.repeatInterval = repeatInterval;
            this.minimalSetup = minimalSetup;
            InitialiseRange();
        }

        /// <summary>
        /// Convert the repeat interval to a string
        /// </summary>
        /// <returns></returns>
        public string RepeatIntervalToString()
        {
            string repeatString = "";
            if (IsFloatingRange)
            {
                if (repeatInterval.Parts.Sum() > 0)
                {
                    if (repeatInterval.Parts[0] > 0)
                        repeatString = $"{(repeatInterval.Parts[0] > 1 ? repeatInterval.Parts[0]: "") } year{(repeatInterval.Parts[0] > 1 ? "s" : "")}";
                    if (repeatInterval.Parts[1] > 0)
                        repeatString += $"{(repeatString == "" ? "" : "and ")}{(repeatInterval.Parts[1] > 1 ? repeatInterval.Parts[1] : "")} month{(repeatInterval.Parts[1] > 1 ? "s" : "")}";
                    if (repeatInterval.Parts[2] > 0)
                        repeatString += $"{(repeatString == "" ? "" : "and ")}{(repeatInterval.Parts[2] > 1 ? repeatInterval.Parts[2] : "")} day{(repeatInterval.Parts[2] > 1 ? "s" : "")}";
                    string moreThanYearly = "";
                    if (repeatInterval.Parts[0] == 0 & repeatInterval.Parts.Sum() > 0)
                        moreThanYearly = " then";
                    repeatString = $"{moreThanYearly} every {repeatString}";
                }
                else
                {
                    repeatString = " every year";
                }
            }
            return repeatString;
        }

        /// <summary>
        /// Initialisation of the range based on intial dates provided.
        /// </summary>
        public void InitialiseRange()
        {
            // set up floating range and years when range crosses Dec-Jan
            if (IsFloatingRange)
            {
                if (Start.ymd.month > 0 && Start.ymd.month > End.ymd.month)
                {
                    Start.YearOffset = -1;
                }

                if (events.TimeStepStart.Year != 1)
                {
                    Start.Date = new DateTime(events.TimeStepStart.Year + Start.YearOffset, Start.ymd.month, Start.ymd.day);
                    End.Date = new DateTime(events.TimeStepStart.Year + End.YearOffset, End.ymd.month, End.ymd.day);
                }
                else
                {
                    // required to display descriptive summary in UI before the model is run
                    Start.Date = new DateTime(events.Clock.StartDate.Year + Start.YearOffset, Start.ymd.month, Start.ymd.day);
                    End.Date = new DateTime(events.Clock.StartDate.Year + End.YearOffset, End.ymd.month, End.ymd.day);
                }

                // if no interval provided then default to 1 year
                if (repeatInterval.Parts.Sum() == 0)
                {
                    repeatInterval.SetAgeSpecifier([1, 0, 0]);
                }

                // if being called for a descriptive summary presentation
                if (minimalSetup)
                {
                    return;
                }

                if (End.Date < events.TimeStepStart)
                {
                    while (End.Date < events.TimeStepStart)
                    {
                        MoveToNextRange();
                    }
                    return;
                }
            }
            TrimAndIdentifyTimeStepIndex();
        }

        /// <summary>
        /// Shift the range to the next defined occurence
        /// </summary>
        public void MoveToNextRange()
        {
            // if not floating range then return
            if (!IsFloatingRange)
                return;

            // if floating range then move start and end dates forward by interval specified
            Start.Date = Start.Date.AddYears(repeatInterval.Parts[0]).AddMonths(repeatInterval.Parts[1]).AddDays(repeatInterval.Parts[2]); // shift end date by interval
            End.Date = End.Date.AddYears(repeatInterval.Parts[0]).AddMonths(repeatInterval.Parts[1]).AddDays(repeatInterval.Parts[2]); // shift end date by interval

            // if monthly increment (no daily commponent)
            // and if enddate month is february
            if (repeatInterval.Parts[2] == 0 && End.Date.Month == 2)
            {
                // set end day to account for leap years
                DateTime.DaysInMonth(End.Date.Year, End.Date.Month);
            }

            TrimAndIdentifyTimeStepIndex();
        }

        /// <summary>
        /// Shift the range to the next defined occurence
        /// </summary>
        private void TrimAndIdentifyTimeStepIndex()
        {
            // alter start and end dates to be in range if whole time step in range required
            var startend = events.GetTimeStepRangeContainingDate(Start.Date);
            if (WholeTimeStepInRange && startend.start < Start.Date)
                Start.Date = startend.end;
            startend = events.GetTimeStepRangeContainingDate(End.Date);
            if (WholeTimeStepInRange && startend.end > End.Date)
                End.Date = startend.start;

            // get time-step index for start and end
            Start.TimeStepIndex = events.CalculateTimeStepIntervalIndex(Start.Date);
            End.TimeStepIndex = events.CalculateTimeStepIntervalIndex(End.Date);
        }

        /// <summary>
        /// Is date in range
        /// </summary>
        /// <param name="date"></param>
        /// <returns>True if date is in this range</returns>
        public bool IsInRange(DateTime date)
        {
            _ = events.CalculateTimeStepIntervalIndex(date);
            if (IsFloatingRange)
            {
                //todo
                // create copy of start and end dates
                // walk back in annual steps until end of range is < date
                // if in range true
            }
            else
                if (date < Start.Date || date > End.Date)
                return true;
            return false;
        }

        /// <summary>
        /// Is current CLEMEvents.TimeStep in range
        /// </summary>
        /// <returns>True if the current time-step is in this range</returns>
        public bool IsInRange()
        {
            if (events is null)
                return false;
            return IsInRange(events.IntervalIndex);
        }

        /// <summary>
        /// Is time-step index in range
        /// </summary>
        /// <param name="timeStepIndex">The index of the current time step</param>
        /// <returns>True if index is in this range</returns>
        public bool IsInRange(int timeStepIndex)
        {
            if (events is null)
                return false;
            if (timeStepIndex >= Start.TimeStepIndex && timeStepIndex <= End.TimeStepIndex)
            {
                return ActivityTimerSequence.IsInSequence(sequenceTimerList, timeStepIndex - Start.TimeStepIndex);
            }

            if (IsFloatingRange && timeStepIndex > End.TimeStepIndex)
            {
                MoveToNextRange();
            }
            return false;
        }
    }
}
