using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM.Timers
{
    /// <summary>
    /// Class to hold the start or end of range details
    /// </summary>
    public class TimerRangeItem
    {
        /// <summary>
        /// Date of range item
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public (int year, int month, int day) ymd = (0, 0, 0);

        /// <summary>
        /// Determines if this is a month only range item
        /// </summary>
        public bool IsMonthOnly { get; set; } = false;

        /// <summary>
        /// Determines if this is item represents the start of a range
        /// </summary>
        public bool IsStartOfRange { get; set; } = false;

        /// <summary>
        /// Provides the time step index for this range date item
        /// </summary>
        public int TimeStepIndex { get; set; }

        /// <summary>
        /// A list of error messages generated while creating this item
        /// </summary>
        public List<string> ErrorMessages { get; set; } = new();

        /// <summary>
        /// Offset for year to allow for month range across Dec-Jan
        /// </summary>
        public int YearOffset { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="events">Link to the CLEMEvents model</param>
        /// <param name="details">Details specifying the date and time as an AgeSpecifier</param>
        /// <param name="startOfRange">Determines if this is a date unit at start of range</param>
        /// <param name="wholeTimeStepInRange"></param>
        public TimerRangeItem(CLEMEvents events, AgeSpecifier details, bool startOfRange, bool wholeTimeStepInRange)
        {
            this.IsStartOfRange = startOfRange;
            ymd = GetDateParts(details, events);
            UpdateDate(events, wholeTimeStepInRange);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            string dateString = "";

            if (ErrorMessages.Any())
                return string.Join(", ", ErrorMessages);
 
            if (ymd.month > 0)
                if (ymd.day > 0 & IsMonthOnly == false)
                    dateString = $"{ymd.day} {DateTimeFormatInfo.CurrentInfo.GetAbbreviatedMonthName(ymd.month)}";
                else
                    dateString = $"{DateTimeFormatInfo.CurrentInfo.GetAbbreviatedMonthName(ymd.month)}";
            else
                dateString = new DateTime(1999, 1, 1).AddDays(ymd.day - 1).ToString("dd MMM", CultureInfo.CurrentCulture);

            if (ymd.year > 0)
                dateString += $" {ymd.year}";

            return dateString;
        }

        private void UpdateDate(CLEMEvents events, bool wholeTimeStepInRange)
        {
            if (ymd.year == 0 && ymd.month == 0 && ymd.day == 0)
            {
                ErrorMessages.Add("Empty date specifier supplied");
                return;
            }

            if (ymd.year > 0)
                Date = new DateTime(ymd.year, ymd.month, ymd.day);
            else if (ymd.month > 0)
                Date = new DateTime(events.Clock.StartDate.Year, ymd.month, ymd.day);
            else if (ymd.day > 0)
                Date = new DateTime(events.Clock.StartDate.Year, events.Clock.StartDate.Month, ymd.day);
            else
                Date = new DateTime(1900,0,0);
        }

        private (int year, int month, int day) GetDateParts(AgeSpecifier details, CLEMEvents events)
        {
            (int year, int month, int day) dateParts = (0, 0, 0);

            // if only day provided assume day of year supplied
            if (details.Parts[2] > 0 & (details.Parts[1] + details.Parts[0]) == 0)
            {
                // if days <= number of days in year then set date to that day of year
                if (details.Parts[2] > 365)
                {
                    ErrorMessages.Add("Day of year must be between 1 and 365 when providing details in day style (0,0,d)");
                    return (0, 0, 0); // will be treated as error in validation
                }
                DateTime date = new DateTime(1999, 1, 1).AddDays(details.Parts[2] - 1);
                return (0, date.Month, date.Day);
            }

            int yearToUse = 1999;
            IsMonthOnly = true;
            // year provided
            if (details.Parts[0] > 0)
            {
                dateParts.year = details.Parts[0];
                IsMonthOnly = false;
                yearToUse = dateParts.year;
            }
            // month provided
            if (details.Parts[1] > 0)
            {
                if (details.Parts[1] > 12 | details.Parts[1] <= 0)
                {
                    ErrorMessages.Add("Month must be between 1 and 12 when providing details with date or month style (x,m,x)");
                    return (0, 0, 0); // will be treated as error in validation
                }
                dateParts.month = details.Parts[1];
            }
            // if day provided
            if (details.Parts[2] > 0)
            {
                IsMonthOnly = false;
                if (dateParts.month == 0)
                {
                    ErrorMessages.Add("Month must be provided with date style (x,m,d)");
                    return (0, 0, 0); // will be treated as error in validation
                }
                if (details.Parts[2] > DateTime.DaysInMonth(yearToUse, details.Parts[1]))
                {
                    ErrorMessages.Add($"Invalid days [{details.Parts[2]}] of month [{details.Parts[1]}] for style (x,m,d)");
                    return (0, 0, 0); // will be treated as error in validation
                }
                dateParts.day = details.Parts[2];
            }
            else
            {
                // if no day provided then set to first or last day of month depending on startOfMonth flag
                dateParts.day = IsStartOfRange ? 1 : DateTime.DaysInMonth(yearToUse, details.Parts[1]);
            }

            // if year and missing month
            if (dateParts.year > 0 && dateParts.month == 0)
            {
                ErrorMessages.Add($"Missing month for style (x,m,d)");
            }
            // if month and missing day
            if (dateParts.month > 0 && dateParts.day == 0)
            {
                ErrorMessages.Add($"Missing day for style (x,m,d)");
            }

            return dateParts;
        }
    }
}
