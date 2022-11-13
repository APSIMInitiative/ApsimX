namespace StdUnits
{
    using System;

    /// <summary>
    /// GrazPlan date utilities
    /// </summary>
    public static class StdDate
    {
        /// <summary>
        /// No. of days up to the last day of previous month. 29 Feb not included   [months 1..12]
        /// </summary>
        public static int[] CumulDays = { 0, 0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334 };

        /// <summary>
        /// Last day of each month   [1..12]
        /// </summary>
        public static int[] LastDay = { 0, 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
        
        /// <summary>
        /// Short text name of the month
        /// </summary>
        public static string[] MonthText = { string.Empty, "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };    // [1..12]

        /// <summary>
        /// Get the date value for specified dmy
        /// </summary>
        /// <param name="day">Day number</param>
        /// <param name="month">Month number</param>
        /// <param name="year">Year number</param>
        /// <returns>The date value integer</returns>
        public static int DateVal(int day, int month, int year)
        {
            return day + month * 0x100 + year * 0x10000;
        }

        /// <summary>
        /// Get the day of month from the date value
        /// </summary>
        /// <param name="dateVal">Date value</param>
        /// <returns>Day of month</returns>
        public static int DayOf(int dateVal)
        {
            return (dateVal & 0xFF);
        }
        
        /// <summary>
        /// Get the month of the year from the date value
        /// </summary>
        /// <param name="dateVal">Date value</param>
        /// <returns>Month of year</returns>
        public static int MonthOf(int dateVal)
        {
            return ((dateVal >> 8) & 0xFF);
        }
        
        /// <summary>
        /// Get the year number from the date value
        /// </summary>
        /// <param name="dateVal">The date value</param>
        /// <returns>Year number</returns>
        public static int YearOf(int dateVal)
        {
            return ((dateVal >> 16) & 0xFFFF);
        }
        
        /// <summary>
        /// Test that this is a real date
        /// </summary>
        /// <param name="dateVal">Date value</param>
        /// <returns>True if this is a valid date</returns>
        public static bool DateValid(int dateVal)
        {
            DMY dateInYear;

            dateInYear.D = DayOf(dateVal);
            dateInYear.M = MonthOf(dateVal);
            dateInYear.Y = YearOf(dateVal);
            if (dateInYear.M == 0)                                                         // 0-0-Y date                               
                return ((dateInYear.D == 0) && (dateInYear.Y != 0));
            else if (dateInYear.D == 0)                                                     // 0-M-Y or 0-M-0 date                      
                return ((dateInYear.M >= 1) && (dateInYear.M <= 12));
            else                                                                    // D-M-Y or D-M-0 date                      
                return (((dateInYear.M >= 1) && (dateInYear.M <= 12))
                          && ((dateInYear.D <= LastDay[dateInYear.M])
                                || ((dateInYear.D == 29) && (dateInYear.M == 2) && LeapYear(dateInYear.Y))));       // N.B. LeapYear(0)=TRUE
        }

        /// <summary>
        /// Is a year a leap year?  N.B. returns TRUE for Y=0                         }
        /// </summary>
        /// <param name="yr">Year number</param>
        /// <returns>True if leap year</returns>
        public static bool LeapYear(int yr)
        {
            return (((yr % 4 == 0) && (yr % 100 != 0)) || (yr % 400 == 0));
        }

        /// <summary>
        /// Format a number with or without zeros
        /// </summary>
        /// <param name="number">The input number</param>
        /// <param name="len">Picture length</param>
        /// <param name="zeroFill">Fill with zeros?</param>
        /// <returns>Formatted number</returns>
        public static string Num2Str(int number, int len, bool zeroFill)
        {
            if (zeroFill)
                return string.Format("{0, 0" + len.ToString() + ":d" + len.ToString() + "}", number);
            else
                return string.Format("{0, " + len.ToString() + "}", number);
        }

        /// <summary>
        /// Format a date value
        /// </summary>
        /// <param name="dateVal">Date value</param>
        /// <param name="format">Format string YYYY, yy, YY, mmm, MM, mm, M, DD, dd, D</param>
        /// <returns>Formatted date string</returns>
        public static string DateStrFmt(int dateVal, string format)
        {
            // Longer pictures must precede shorter     
            string[] pictures = { "YYYY", "yy", "YY", "mmm", "MM", "mm", "M", "DD", "dd", "D" };
            const int NOPICTURE = 10;

            string dateStr;
            int picIdx;
            DMY dateInYear;

            dateInYear.D = DayOf(dateVal);
            dateInYear.M = MonthOf(dateVal);
            dateInYear.Y = YearOf(dateVal);

            dateStr = string.Empty;

            // Work through the formatting string
            while (format != string.Empty)                                                     
            {
                picIdx = 0;                                                             // Search for a string picture              
                while ((picIdx < NOPICTURE) && (format.IndexOf(pictures[picIdx]) != 0))
                    picIdx++;

                switch (picIdx)
                {
                    case 0: dateStr = dateStr + Num2Str(dateInYear.Y, 4, false);        // YYYY                                     
                        break;
                    case 1: if ((dateInYear.Y >= 1900) && (dateInYear.Y <= 1999))       // yy                                      
                            dateStr = dateStr + Num2Str(dateInYear.Y % 100, 2, true);
                        else
                            dateStr = dateStr + Num2Str(dateInYear.Y, 4, true);
                        break;
                    case 2: if (dateInYear.Y % 100 == 0)                                // YY                                       
                            dateStr = dateStr + "00";
                        else
                            dateStr = dateStr + Num2Str(dateInYear.Y % 100, 2, true);
                        break;
                    case 3: if ((dateInYear.M >= 1) && (dateInYear.M <= 12))            // mmm                                      
                            dateStr = dateStr + MonthText[dateInYear.M];
                        break;
                    case 4: dateStr = dateStr + Num2Str(dateInYear.M, 2, true);         // MM                                       
                        break;
                    case 5: dateStr = dateStr + Num2Str(dateInYear.M, 2, false);        // mm                                       
                        break;
                    case 6: dateStr = dateStr + Num2Str(dateInYear.M, 0, false);        // M                                        
                        break;
                    case 7: dateStr = dateStr + Num2Str(dateInYear.D, 2, true);         // DD                                       
                        break;
                    case 8: dateStr = dateStr + Num2Str(dateInYear.D, 2, false);        // dd                                       
                        break;
                    case 9: dateStr = dateStr + Num2Str(dateInYear.D, 0, false);        // D                                        
                        break;
                    case NOPICTURE: dateStr = dateStr + format[0];                      // Literal text                             
                        break;
                }

                if (picIdx == NOPICTURE)                                                // Delete the part of Fmt we have just used 
                    format = format.Remove(0, 1);                                       // Delete( Fmt, 1, 1 )
                else
                    format = format.Remove(0, pictures[picIdx].Length);                 // Delete( Fmt, 1, Length(Pictures[Pic]) ); //TODO: check this
            }
            return dateStr;
        }

        /// <summary>
        /// Get the length of the month in days
        /// </summary>
        /// <param name="month">Month number</param>
        /// <param name="yr">Year number</param>
        /// <returns>Days in the month</returns>
        private static int MonthLength(int month, int yr)
        {
            if (month == 0)
                return 31;
            else if ((month == 2) && LeapYear(yr))
                return 29;
            else
                return LastDay[month];
        }

        /// <summary>
        ///  Shift a date by a given number of months                              
        /// </summary>
        /// <param name="dateInYear">Original date value</param>
        /// <param name="months">Months to increment by</param>
        private static void MonthShift(ref DMY dateInYear, int months)
        {
            int totalMonths;

            if ((dateInYear.Y != 0) && (dateInYear.M != 0))
            {
                totalMonths = 12 * dateInYear.Y + dateInYear.M + months - 13;                // Months from 1 Jan 0001 
                dateInYear.Y = 1 + totalMonths / 12;
                dateInYear.M = 1 + totalMonths % 12;
            }
            else if (dateInYear.Y != 0)
                dateInYear.Y += months / 12;
            else if (dateInYear.M != 0)
                dateInYear.M = 1 + (dateInYear.M + months - 12 * (months / 12) + 11) % 12;
        }

        /// <summary>
        /// Change a date by a given number of days, months and/or years, forward or back.                                                                     
        /// </summary>
        /// <param name="dateVal">Starting date value</param>
        /// <param name="shiftDays">Number of days</param>
        /// <param name="shiftMonths">Number of months</param>
        /// <param name="shiftYears">Number of years</param>
        /// <returns>Date moved</returns>
        public static int DateShift(int dateVal, int shiftDays, int shiftMonths, int shiftYears)
        {
            int tempDay;
            DMY dateInYear;

            dateInYear.D = DayOf(dateVal);
            dateInYear.M = MonthOf(dateVal);
            dateInYear.Y = YearOf(dateVal);

            // Increment the number of years if dateInYear is a historical date (Y <> 0)               
            if (dateInYear.Y != 0)                                                       
                dateInYear.Y += shiftYears;                                             

            if (dateInYear.M != 0)                                                                               
            {
                // Shift the months
                MonthShift(ref dateInYear, shiftMonths);
                dateInYear.D = Math.Min(dateInYear.D, MonthLength(dateInYear.M, dateInYear.Y));     // If we move from, say 31 Aug to 31 Sep,   
            }                                                                           // go back to the end of the month       

            if (dateInYear.D != 0)
            {
                tempDay = dateInYear.D + shiftDays;                                     // Use temporary storage for the day so     
                                                                                        // as to avoid overflows                 
                while (tempDay > MonthLength(dateInYear.M, dateInYear.Y))
                {
                    tempDay -= MonthLength(dateInYear.M, dateInYear.Y);
                    MonthShift(ref dateInYear, 1);
                }
                while (tempDay < 1)
                {
                    tempDay += MonthLength(dateInYear.M - 1, dateInYear.Y);
                    MonthShift(ref dateInYear, -1);
                }

                dateInYear.D = tempDay;                                                 // Return the day value to DMY(Dt)          
            } // _ IF (D <> 0) _

            return DateVal(dateInYear.D, dateInYear.M, dateInYear.Y);
        }

        // TODO: test this function

        /// <summary>
        ///  Interval between two dates.  Note that Interval(D,D) = 0.                 
        /// </summary>
        /// <param name="date1">First date</param>
        /// <param name="date2">End date</param>
        /// <returns>The interval in days</returns>
        public static int Interval(int date1, int date2)
        {
            int sum;
            int i;
            DMY dateInYear1, dateInYear2;

            dateInYear1.D = DayOf(date1);
            dateInYear1.M = MonthOf(date1);
            dateInYear1.Y = YearOf(date1);
            dateInYear2.D = DayOf(date2);
            dateInYear2.M = MonthOf(date2);
            dateInYear2.Y = YearOf(date2);
            int result;

            // If the first date is after the second
            if (date1 > date2)                                                       
                result = -Interval(date2, date1);                                   // return a negative value                
            else if ((date1 == 0) || (date2 == 0))                                  
                result = 0;                                                         // if any of the dates are zero, interval is always zero
            else
            {
                if (dateInYear1.M == 0)                                              
                {
                    // if the month is zero make the date 1 Jan
                    dateInYear1.D = 1;                                              
                    dateInYear1.M = 1;
                }
                if (dateInYear2.M == 0)                                              
                {
                    // if the month is zero make the date 1 Jan
                    dateInYear2.D = 1;                                              
                    dateInYear2.M = 1;
                }

                sum = 365 - CumulDays[dateInYear1.M] - dateInYear1.D;               // Days to the end of the year of D1 

                // Add one if Feb 29 falls in the period from D1 to the end of the year
                if ((LeapYear(dateInYear1.Y)) &&                                    
                   ((dateInYear1.M == 1) ||                                                 
                    ((dateInYear1.M == 2) && (dateInYear1.D <= 29))))
                    sum++;

                // Now move to the end of the year before that containing D2.
                if (dateInYear1.Y == dateInYear2.Y)                                        
                {                                                                                
                    sum = sum - 365;                                                // Same year; subtract the number of     
                    if (LeapYear(dateInYear2.Y)) sum--;                             // days in the year                      
                }
                else
                    //// Otherwise work through the intervening years, adding the number of days in each
                    for (i = dateInYear1.Y + 1; i <= dateInYear2.Y - 1; i++)                    
                        if (LeapYear(i))                                               
                            sum += 366;                                                                     
                        else
                            sum += 365;

                sum = sum + CumulDays[dateInYear2.M] + dateInYear2.D;               // add number of days from the start     
                                                                                    // of the year of D2                     
                if (LeapYear(dateInYear2.Y) && (dateInYear2.M >= 3))
                    sum++;
                result = sum;
            }
            return result;
        }

        /// <summary>
        /// Day of year function.                                                     
        /// </summary>
        /// <param name="dateVal">Date to convert to day-of-year</param>
        /// <param name="useYr">If TRUE, Feb 29 is counted if YearOf(D) is zero or a leap year.  
        ///          If FALSE, Feb 29 is counted regardless of the year.</param>
        /// <returns>Day of year</returns>
        public static int DOY(int dateVal, bool useYr)
        {
            DMY dateInYear;

            dateInYear.D = DayOf(dateVal);
            dateInYear.M = MonthOf(dateVal);
            dateInYear.Y = YearOf(dateVal);
            if ((dateInYear.M <= 2) || (useYr && !LeapYear(dateInYear.Y)))
                return CumulDays[dateInYear.M] + dateInYear.D;
            else
                return CumulDays[dateInYear.M] + 1 + dateInYear.D;
        }
    }

    /// <summary>
    /// Split the long integer into three       
    /// parts: day and month get a byte each,  
    /// year gets two. Note that this arrangement allows relational                   
    /// operators to be used on date values
    /// </summary>
    public struct DMY
    {
        // TODO: may need to change this if overloaded operators are required

        /// <summary>
        /// Day value
        /// </summary>
        public int D;

        /// <summary>
        /// Month value
        /// </summary>
        public int M;

        /// <summary>
        /// Year value
        /// </summary>
        public int Y;
    }
}