// -----------------------------------------------------------------------
// <copyright file="DateUtilities.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace APSIM.Shared.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Some date manipulation routines, transcribed from their Fortran counterparts
    /// </summary>
    public class DateUtilities
    {
        /// <summary>
        /// a list of month names in lower case.
        /// </summary>
        static public string[] LowerCaseMonths =  new string[] { "jan", "feb", "mar", "apr", "may", "jun", "jul", "aug", "sep", "oct", "nov", "dec" };

        /// <summary>
        /// A regular expression
        /// </summary>
        static Regex
            rxDD = new Regex(@"\d\d?"),
            rxMMM = new Regex(@"\w{3}"),
            rxDMY = new Regex(@"^(\d{1,2})[-/](\d{1,2})[-/](\d{4})$"),
            rxddMMM = new Regex(@"^(([0-9])|([0-2][0-9])|([3][0-1]))\-(Jan|jan|Feb|feb|Mar|mar|Apr|apr|May|may|Jun|jun|Jul|jul|Aug|aug|Sep|sep|Oct|oct|Nov|nov|Dec|dec)");


        /// <summary>
        /// Convert a Julian Date to a DateTime object
        /// Where the Julian day begins at Greenwich mean noon 12pm. 12h UT.
        /// 2429996.0 is 1/1/1941 12:00
        /// </summary>
        /// <param name="julian_date"></param>
        /// <returns></returns>
        private static DateTime GetDate(double julian_date)
        {
            double a, b, c, d, e, f, z, alpha, decDay;
            int yr, mnth, day, hr, min, sec, ms;
            double decHr, decMin, decSec;

            julian_date += 0.5;
            z = System.Math.Truncate(julian_date); //store int part of JD
            f = julian_date - z;    //store the frac part of JD
            if (z < 2299161)
                a = z;
            else
            {
                alpha = System.Math.Truncate((z - 1867216.25) / 36524.25);
                a = z + 1 + alpha - System.Math.Truncate(alpha / 4);
            }
            b = a + 1524;
            c = System.Math.Truncate((b - 122.1) / 365.25);
            d = System.Math.Truncate(365.25 * c);
            e = System.Math.Truncate((b - d) / 30.6001);

            decDay = b - d - System.Math.Truncate(30.6001 * e) + f;
            if (e < 13.5)
                mnth = Convert.ToInt32(e - 1, CultureInfo.InvariantCulture);
            else
                mnth = Convert.ToInt32(e - 13, CultureInfo.InvariantCulture);

            if (mnth > 2)
                yr = Convert.ToInt32(c - 4716, CultureInfo.InvariantCulture);
            else
                yr = Convert.ToInt32(c - 4715, CultureInfo.InvariantCulture);

            //convert decDay to d,hr,min,sec
            day = Convert.ToInt32(System.Math.Truncate(decDay), CultureInfo.InvariantCulture);
            decHr = (decDay - day) * 24;
            hr = Convert.ToInt32(System.Math.Truncate(decHr), CultureInfo.InvariantCulture);
            decMin = (decHr - hr) * 60;
            min = Convert.ToInt32(System.Math.Truncate(decMin), CultureInfo.InvariantCulture);
            decSec = (decMin - min) * 60;
            sec = Convert.ToInt32(System.Math.Truncate(decSec), CultureInfo.InvariantCulture);
            ms = Convert.ToInt32(System.Math.Truncate(decSec - sec * 1000), CultureInfo.InvariantCulture);

            return new DateTime(yr, mnth, day, hr, min, sec, ms);
        }

        /// <summary>
        /// Get a DateTime from a ddMMM string (ie '01Jan' OR '1-Jan' OR '1 Jan' etc), year is automatically set to 1900
        /// </summary>
        /// <param name="ddMMM">String containing 'day of month' and at least the first 3 letters of a month's name</param>
        /// <returns>A DateTime with the specified date and month, year = 1900</returns>
        public static DateTime GetDate(string ddMMM)
        {
            return GetDate(ddMMM, 1900);
        }

        /// <summary>
        /// Get a DateTime from a 'ddMMM' string (ie '01Jan' OR '1-Jan' OR '1 Jan' etc)
        /// </summary>
        /// <param name="ddMMM">String containing 'day of month' and at least the first 3 letters of a month's name</param>
        /// <param name="year">The year to use when constructing the DateTime object</param>
        /// <returns>A DateTime constructed from <paramref name="ddMMM"/> using <paramref name="year"/></returns>
        public static DateTime GetDate(string ddMMM, int year)
        {
            try
            {
                int posDelimiter = ddMMM.IndexOfAny(new char[] {'/', '-'});
                if (posDelimiter == -1)
                    throw new ArgumentException();

                int month = StringUtilities.IndexOfCaseInsensitive(LowerCaseMonths, ddMMM.Substring(posDelimiter + 1)) + 1;
                int day = Convert.ToInt32(ddMMM.Substring(0, posDelimiter), CultureInfo.InvariantCulture);
                return new DateTime(year, month, day);

                //return new DateTime(
                //    year,
                //    Array.IndexOf(LowerCaseMonths, rxMMM.Match(ddMMM).Value.ToLower()) + 1,
                //    int.Parse(rxDD.Match(ddMMM).Value),
                //    0,
                //    0,
                //    0
                //    );
            }
            catch
            {
                throw new Exception("Error in 'GetDate' - input string should be in form ddmmm (any delimiter may appear between dd and mmm), input string: " + ddMMM);
            }
        }

        /// <summary>
        /// Get a DateTime from a 'ddMMM' string (ie '01Jan' OR '1-Jan' OR '1 Jan' etc), using <paramref name="today"/> to get the year to use
        /// </summary>
        /// <param name="ddMMM">String containing 'day of month' and at least the first 3 letters of a month's name</param>
        /// <param name="today">The year in this parameter will be used to construct the result</param>
        /// <returns>A DateTime constructed from <paramref name="ddMMM"/> using the year of <paramref name="today"/></returns>
        public static DateTime GetDate(string ddMMM, DateTime today)
        {
            return GetDate(ddMMM, today.Year);
        }

        /// <summary>
        /// Given today's date (<paramref name="today"/>), get the next occurrence of <paramref name="thedate"/> by adding/subtracting year(s)
        /// </summary>
        /// <param name="thedate">The date to change</param>
        /// <param name="today">Today's date</param>
        /// <returns>The next occurrence of <paramref name="thedate"/></returns>
        public static DateTime GetNextDate(DateTime thedate, DateTime today)
        {
            thedate = thedate.AddYears(today.Year - thedate.Year);
            return today.CompareTo(thedate) < 0 ? thedate : thedate.AddYears(1);
        }
        
        /// <summary>
        /// Given a 'ddMMM' string (ie '01Jan' OR '1-Jan' OR '1 Jan' etc) and <paramref name="today"/>, return the next occurrence of <paramref name="ddMMM"/>
        /// </summary>
        /// <param name="ddMMM">String containing 'day of month' and at least the first 3 letters of a month's name</param>
        /// <param name="today">Today's date</param>
        /// <returns>The next occurrence of <paramref name="ddMMM"/></returns>
        public static DateTime GetNextDate(string ddMMM, DateTime today)
        {
            return GetNextDate(GetDate(ddMMM, today), today);
        }

        /// <summary>
        /// Construct a DateTime from <paramref name="ddMMM"/> and <paramref name="today"/> then 'CompareTo' <paramref name="today"/>
        /// </summary>
        /// <param name="ddMMM">String containing 'day of month' and at least the first 3 letters of a month's name</param>
        /// <param name="today">Today's date</param>
        /// <returns>+1 if <paramref name="ddMMM"/> is less than <paramref name="today"/>, 0 if equal, -1 if greater</returns>
        public static int CompareDates(string ddMMM, DateTime today)
        {
            return today.CompareTo(GetDate(ddMMM, today));
        }

        /// <summary>
        /// Compare <paramref name="ddMMM"/> and <paramref name="today"/> (ignoring year component)
        /// </summary>
        /// <param name="ddMMM">String containing 'day of month' and at least the first 3 letters of a month's name</param>
        /// <param name="today">The date to check</param>
        /// <returns>true if the day and month components of <paramref name="today"/> match ddMMM, else false</returns>
        public static bool DatesEqual(string ddMMM, DateTime today)
        {
            return 
                today.Month == Array.IndexOf(LowerCaseMonths, rxMMM.Match(ddMMM).Value.ToLower()) + 1
                &&
                today.Day == int.Parse(rxDD.Match(ddMMM).Value);
        }

        /// <summary>
        /// Look to see if <paramref name="today"/> lies between <paramref name="ddMMM_start"/> and <paramref name="ddMMM_end"/> (will handle year boundaries)
        /// </summary>
        /// <param name="ddMMM_start">The start date - a string containing 'day of month' and at least the first 3 letters of a month's name</param>
        /// <param name="today">The date to check</param>
        /// <param name="ddMMM_end">The end date - a string containing 'day of month' and at least the first 3 letters of a month's name</param>
        /// <returns>true if within date window</returns>
        public static bool WithinDates(string ddMMM_start, DateTime today, string ddMMM_end)
        {
            DateTime
                start = GetDate(ddMMM_start, today.Year),
                end = GetDate(ddMMM_end, today.Year);

            //if start after end (spans end-of-year boundary)
            if (start.CompareTo(end) > 0)
            {
                if (today.CompareTo(start) >= 0)
                    end = end.AddYears(1);
                else
                    start = start.AddYears(-1);
            }

            return WithinDates(start, today, end);
        }

        /// <summary>
        /// Look to see if <paramref name="today"/> lies between <paramref name="start"/> and <paramref name="end"/> (will handle year boundaries)
        /// </summary>
        /// <param name="start">The start date</param>
        /// <param name="today">The date to check</param>
        /// <param name="end">The end date</param>
        /// <returns>true if within date window</returns>
        public static bool WithinDates(DateTime start, DateTime today, DateTime end)
        {
            return today.CompareTo(start) >= 0 && today.CompareTo(end) <= 0;
        }
        
        /// <summary>
        /// Get a Julian Date from a DateTime. Where the Julian day begins at Greenwich mean noon 12pm. 12h UT.
        /// 2429995.5 is 1/1/1941 00:00
        /// 2429996.0 is 1/1/1941 12:00
        /// </summary>
        /// <param name="date">The DateTime to convert</param>
        /// <returns>The Julian Date representation of <paramref name="date"/></returns>
        private static double GetJulianDate(DateTime date)
        {
            double yr;
            double a, b = 0;
            double JD;

            double
                y = date.Year,
                m = date.Month,
                d = date.Day + date.TimeOfDay.TotalHours / 24d;

            //make a yyyy.MMDDdd value
            yr = y + ((double)m / 100) + (d / 10000.0);

            if ((m == 1) || (m == 2))
            {
                y -= 1;
                m += 12;
            }
            if (yr >= 1582.1015)
            { //use yyyy.MMDDdd value
                a = System.Math.Truncate(y / 100);  
                b = 2 - a + System.Math.Truncate(a / 4.0);
            }

            JD = b + System.Math.Truncate(365.25 * y) + System.Math.Truncate(30.6001 * (m + 1)) + d + 1720994.5;

            return JD;
        }
        
        /// <summary>
        /// Converts a Julian Day Number to Day of year. 
        /// </summary>
        /// <param name="JDN"> Julian day number.</param>
        /// <param name="dyoyr">Day of year</param>
        /// <param name="year">Year</param>
        /// <returns>Date time value.</returns>
        public static void JulianDayNumberToDayOfYear(int JDN, out int dyoyr, out int year)
        {
            DateTime date = GetDate(JDN);
            dyoyr = date.DayOfYear;
            year = date.Year;
        }
        
        /// <summary>
        /// Convert the Julian day number (int value) emitted from Clock to a DateTime. 
        /// This will be the DateTime at 00:00 of the day. 
        /// For example 2429996 => 1/1/1941 by the apsim clock. 
        /// (Where 2429996 is really 1/1/1941 12pm. 2429995.5 is really 1/1/1941 00:00.) 
        /// </summary>
        /// <param name="JDN"></param>
        /// <returns></returns>
        public static DateTime JulianDayNumberToDateTime(int JDN)
        {
            double jd = JDN - 0.5;  //Convert to true julian date value (at 00:00).
            return GetDate(jd);     //equiv to new DateTime(y,m,d)
        }
        
        /// <summary>
        /// Convert the DateTime value to the Julian day number equivalent to what
        /// Clock would emit. Given the DateTime for a day would give the whole number
        /// for the standard Julian date at 12h on this day.
        /// e.g. 1/1/1941 00:00 - 23:59 --> 2429996
        /// </summary>
        /// <param name="adatetime"></param>
        /// <returns></returns>
        public static int DateTimeToJulianDayNumber(DateTime adatetime)
        {
            return (int)System.Math.Truncate(GetJulianDate(adatetime) + 0.5);
        }

        /// <summary>
        /// Convert a dd/mm/yyyy to yyyy-mm-dd string
        /// </summary>
        /// <param name="dmy">[d]d/[m]m/yyyy</param>
        /// <returns>yyyy-mm-dd</returns>
        public static string DMYtoISO(string dmy)
        {
            Match m = rxDMY.Match(dmy);
            if (m.Success)
                return System.String.Format("{0}-{1,02:d2}-{2,02:d2}", m.Groups[3].Value, Convert.ToInt32(m.Groups[2].Value, CultureInfo.InvariantCulture), Convert.ToInt32(m.Groups[1].Value, CultureInfo.InvariantCulture));
            else
                return "0001-01-01";    // default??
        }

        /// <summary>
        /// Convert a dd/mm/yyyy to DateTime
        /// </summary>
        /// <param name="dmy">[d]d/[m]m/yyyy</param>
        /// <returns>The date</returns>
        public static DateTime DMYtoDate(string dmy)
        {
            Match m = rxDMY.Match(dmy);
            if (m.Success)
                return new DateTime(Convert.ToInt32(m.Groups[3].Value), Convert.ToInt32(m.Groups[2].Value), Convert.ToInt32(m.Groups[1].Value));
            else
                return new DateTime();    // default??
        }

        /// <summary>
        /// Takes in a string and checks to see if it is in the correct format for either a 'dd-mmm' value, or a full date
        /// with year, month and date (in any recognised date format).
        /// </summary>
        /// <param name="dateStr"></param>
        /// <returns>a string with the valid dd-Mmm string or a valid date as a string (yyyy-mm-dd)</returns>
        public static string validateDateString(string dateStr)
        {
            string returnDate = string.Empty;
            DateTime d;

            Match m = rxddMMM.Match(dateStr);
            //also need to look at the length just in case input value is a full date as 20-Jan-2016 (and not just 20-Jan).
            if ((m.Success) && (dateStr.Length <= 6))
            {
                d = GetDate(dateStr, 2000);
                //for consistency, return it as 'Title' case (ie, 01-Jan, not 1-jan)
                returnDate = d.ToString("dd-MMM");  
            }
            else
            {
                DateTime.TryParse(dateStr, out d);
                returnDate = d.ToString("yyyy-MM-dd");
            }
            return returnDate;
        }

        /// <summary>
        /// Takes in a string and validates it as a 'dd-mmm' value, or as a full date, and a year value.  When
        /// the 'dd-MMM' value is passed the year value is used to build a valid date.
        /// </summary>
        /// <param name="dateStr">the date as a string, (ie, 01-jan or 2010-01-21)</param>
        /// <param name="year">the year to be added to date, if it doesn't exist (ie, 01-jan)</param>
        /// <returns>a valid date as a datetime value</returns>
        public static DateTime validateDateString(string dateStr, int year)
        {
            DateTime returnDate = new DateTime();

            Match m = rxddMMM.Match(dateStr);
            //also need to look at the length just in case input value is a full date as 20-Jan-2016 (and not just 20-Jan).
            if ((m.Success) && (dateStr.Length <= 6))
                returnDate = DateUtilities.GetDate(dateStr, year);
            else
            {
                DateTime.TryParse(dateStr, out returnDate);
            }
            return returnDate;
        }

    }
}
