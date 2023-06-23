using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace APSIM.Shared.Utilities
{

    /// <summary>
    /// Some date manipulation routines, transcribed from their Fortran counterparts
    /// </summary>
    public class DateUtilities
    {
        /// <summary>
        /// a list of month names in lower case.
        /// </summary>
        static public string[] LowerCaseMonths = CultureInfo.InvariantCulture.DateTimeFormat.AbbreviatedMonthNames;

        /// <summary>
        /// A regular expression
        /// </summary>
        static Regex
            rxDD = new Regex(@"\d\d?"),
            rxMMM = new Regex(@"\w{3}"),
            rxDMY = new Regex(@"^(\d{1,2})[-/](\d{1,2})[-/](\d{4})$"),
            rxddMMM = new Regex(@"^(([0-9])|([0-2][0-9])|([3][0-1]))\-(Jan|jan|Feb|feb|Mar|mar|Apr|apr|May|may|Jun|jun|Jul|jul|Aug|aug|Sep|sep|Oct|oct|Nov|nov|Dec|dec)");

        private const int DEFAULT_YEAR = 1900;

        static private Regex
            rxDay = new Regex(@"^\d\d?$"),
            rxMonthNum = new Regex(@"^\d\d?$"),
            rxMonth3Letter = new Regex(@"^\w\w\w$"),
            rxMonth4Letter = new Regex(@"^\w\w\w\w$"),
            rxMonthFull = new Regex(@"^[^0-9]\w+[^0-9]$"),
            rxYear = new Regex(@"^\d\d\d\d$");

        /// <summary>
        /// Convert any valid date string into a DateTime objects.
        /// Valid seprators are: / - , . _
        /// If a Day-Month is provided, the year is set to 1900
        /// Can take dates in the following formats:
        /// Jun-01
        /// Jun-1
        /// 01-Jun
        /// 1-Jun
        /// 01-Jun-2000
        /// 1-Jun-2000
        /// 2000-06-01
        /// 2000-June-01
        /// </summary>
        /// <param name="dateString">The date</param>
        public static DateTime ParseDate(string dateString)
        {
            string dateWithSymbolsParsed = dateString;

            //check that the string has a valid symbol
            //replace it with a tab character

            //valid choices: / - , . _
            char[] validSymbols = new char[] { '/', '-', ',', '.', '_' };
            char symbolReplacement = '\t';
            int types = 0;
            foreach (char c in validSymbols)
            {
                if (dateString.Contains(c))
                {
                    types += 1;
                    //change symbol to \t
                    dateWithSymbolsParsed = dateWithSymbolsParsed.Replace(c, symbolReplacement);
                }
            }

            //make sure only 1 or two of symbol and only has one type of these symbols
            if ((types == 0) || (types > 1))
            {
                string symbols = " ";
                foreach (char c in validSymbols)
                    symbols += c + " ";

                if (types == 0)
                    throw new Exception($"Date {dateString} cannot be parsed as it contains no valid symbols. ({symbols}).");
                else if (types > 1)
                    throw new Exception($"Date {dateString} cannot be parsed as it multiple symbol types. ({symbols}).");
            }

            //seperate by \t to get parts
            string[] parts = dateWithSymbolsParsed.Split('\t');

            //check that there are 2 or 3 parts and that each part has text in it
            if (parts.Length < 2 || parts.Length > 3)
                throw new Exception($"Date {dateString} cannot be parsed as it only has {parts.Length} parts. Date should have 2 or 3 parts (day-month-year or day-month).");

            foreach (string part in parts)
                if (part.Length == 0)
                    throw new Exception($"Date {dateString} cannot be parsed as it it has an empty part after a symbol.");

            int dayNum;
            int monthNum;
            int yearNum;
            //if first part is 4 characters - ISO 2000-01-01 or 2000-Jan-01
            if (parts.Length == 3 && parts[0].Length == 4)
            {
                yearNum = ParseYearString(parts[0], dateString);
                monthNum = ParseMonthString(parts[1], dateString);
                dayNum = ParseDayString(parts[2], dateString);
            }
            else
            {
                //if first part is numbers, it's a day
                if (rxDay.Match(parts[0]).Success)
                {
                    //first part is day
                    dayNum = ParseDayString(parts[0], dateString);
                    //second part is month
                    monthNum = ParseMonthString(parts[1], dateString);
                }
                //else if first part is a word (we can just reused the full month name regex for that)
                else if (rxMonthFull.Match(parts[0]).Success)
                {
                    //second part is day
                    dayNum = ParseDayString(parts[1], dateString);
                    //first part is month
                    monthNum = ParseMonthString(parts[0], dateString);
                }
                else
                {
                    throw new Exception($"Date {dateString} cannot be parsed as the first part {parts[0]} is neither a valid day or month name.)");
                }

                //optional third part is year
                yearNum = DEFAULT_YEAR;
                if (parts.Length == 3)
                {
                    yearNum = ParseYearString(parts[2], dateString);
                }
            }

            return ParseDate(yearNum, monthNum, dayNum);
        }

        /// <summary>
        /// Construct a DateTime from <paramref name="ddMMM"/> and <paramref name="today"/> then 'CompareTo' <paramref name="today"/>
        /// </summary>
        /// <param name="ddMMM">String containing 'day of month' and at least the first 3 letters of a month's name</param>
        /// <param name="today">Today's date</param>
        /// <returns>+1 if <paramref name="ddMMM"/> is less than <paramref name="today"/>, 0 if equal, -1 if greater</returns>
        public static int CompareDates(string ddMMM, DateTime today)
        {
            return today.CompareTo(ParseDate(ddMMM));
        }

        /// <summary>
        /// Compare <paramref name="date"/> and <paramref name="today"/> (ignoring year component)
        /// </summary>
        /// <param name="date">String, "dd-mmm" </param>
        /// <param name="today">DateTime, to compare to date (e.g clock.Today)</param>
        /// <returns>true if the day and month components of <paramref name="today"/> match ddMMM, else false</returns>
        public static bool DatesEqual(string date, DateTime today)
        {
            //this needs to be renamed to make it clear it's only day and month comparision
            int result = CompareDates(date, today);
            if (result == 0)
                return true;
            else
                return false;
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
            //this is poorly written and needs updating
            DateTime start = ParseDate(ddMMM_start, today.Year);
            DateTime end = ParseDate(ddMMM_end, today.Year);

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
        /// Convert a dd/mm/yyyy to yyyy-mm-dd string
        /// </summary>
        /// <param name="dmy">[d]d/[m]m/yyyy</param>
        /// <returns>yyyy-mm-dd</returns>
        public static string DMYtoISO(string dmy)
        {
            DateTime newDateTime = ParseDate(dmy);
            return newDateTime.ToString("yyyy-MM-dd");

            //Match m = rxDMY.Match(dmy);
            //if (m.Success)
            //    return System.String.Format("{0}-{1,02:d2}-{2,02:d2}", m.Groups[3].Value, Convert.ToInt32(m.Groups[2].Value, CultureInfo.InvariantCulture), Convert.ToInt32(m.Groups[1].Value, CultureInfo.InvariantCulture));
            //else
            //    return "0001-01-01";    // default??
        }

        /// <summary>
        /// Does a date comparison where a datestring can be dd-mmm or yyyy-mm-dd
        /// </summary>
        /// <param name="dateStr">The date as a string, (ie, 01-jan or 2010-01-21)</param>
        /// <param name="d">The date to compare the string with.</param>
        /// <returns>True if the string matches the date.</returns>
        public static bool DatesAreEqual(string dateStr, DateTime d)
        {
            return d == ParseDate(dateStr, d.Year);
        }

        /// <summary>
        /// Is a specified date at the end of a month?
        /// </summary>
        /// <param name="date">The date.</param>
        public static bool IsEndOfMonth(DateTime date)
        {
            return date.AddDays(1).Day == 1;
        }

        /// <summary>
        /// Is a specified date at the end of a year?
        /// </summary>
        /// <param name="date">The date.</param>
        public static bool IsEndOfYear(DateTime date)
        {
            return date.Day == 31 && date.Month == 12;
        }

        // TODO: Replace with parseMonth.
        private static string ReformatDayMonthString(string ddMMM)
        {
            string ddMMMReformatted = "";
            if (!String.IsNullOrEmpty(ddMMM))
            {
                if (ddMMM.Length == 7)
                {
                    ddMMMReformatted = ddMMM.Substring(0, ddMMM.Length - 1);
                }
                else if (ddMMM.Length > 7 || ddMMM.Length < 5)
                {
                    throw new Exception("Format of ddMMM string is too long.");
                }
                else
                {
                    return ddMMM;
                }

            }
            else throw new Exception("ddMMM string must not be null.");

            return ddMMMReformatted;
        }

        private static int ParseDayString(string dayString, string fullDate)
        {
            if (!rxDay.Match(dayString).Success)
                throw new Exception($"Date {fullDate} is formatted for ISO, however has {dayString} for day. Day must be exactly 2 numbers.");
            else
                return int.Parse(dayString);
        }

        private static int ParseMonthString(string monthString, string fullDate)
        {
            string monthLowerString = monthString.ToLower();

            string[] month3Letters = CultureInfo.InvariantCulture.DateTimeFormat.AbbreviatedMonthNames;
            string[] monthAU = CultureInfo.GetCultureInfo("en-AU").DateTimeFormat.AbbreviatedMonthNames;
            string[] monthFull = CultureInfo.InvariantCulture.DateTimeFormat.MonthNames;

            //make all our names lower case because they are capitalised
            for (int i = 0; i < month3Letters.Length; i++)
            {
                month3Letters[i] = month3Letters[i].ToLower();
                monthAU[i] = month3Letters[i].ToLower();
                monthFull[i] = month3Letters[i].ToLower();
            }

            int index = -1;
            if (rxMonthNum.Match(monthLowerString).Success)
            {
                index = int.Parse(monthLowerString);
            }
            else
            {
                if (rxMonth3Letter.Match(monthLowerString).Success)
                    index = Array.IndexOf(month3Letters, monthLowerString) + 1;
                else if (rxMonth4Letter.Match(monthLowerString).Success)
                    index = Array.IndexOf(monthAU, monthLowerString) + 1;
                else if (rxMonthFull.Match(monthLowerString).Success)
                    index = Array.IndexOf(monthFull, monthLowerString) + 1;
                else
                    throw new Exception($"Date {fullDate} has {monthLowerString} for month. Month must be exsactly 1 or 2 digits, a 3 or 4 letter abbrivation or the full name. (eg: 1, 01, Jun, June, September)");
            }

            if (index > 0)
                return index;
            else
                throw new Exception($"Date {fullDate} has {monthLowerString} for month, was not found in a month name list.");
        }

        private static int ParseYearString(string yearString, string fullDate)
        {
            if (!rxYear.Match(yearString).Success)
                return int.Parse(yearString);
            else
                throw new Exception($"Date {fullDate} has {yearString} for year. Year must be exactly 4 numbers.");
        }

        /// <summary>
        /// Takes a <paramref name="dateString"/> and <paramref name="formatString"/> and returns a DateTime in the specified format.
        /// </summary>
        /// <param name="dateString"></param>
        /// <param name="formatString"></param>
        /// <returns></returns>
        public static DateTime ParseDate(string dateString, string formatString)
        {
            DateTime newDateTime = new();
            if (!String.IsNullOrEmpty(dateString) || !String.IsNullOrEmpty(formatString))
            {
                return newDateTime = DateTime.ParseExact(dateString, formatString, CultureInfo.InvariantCulture);
            }
            else throw new Exception("One or both parameters in ParseDate(dateString, formatString) where null or empty.");
        }

        /// <summary>
        /// Takes a <paramref name="dayMonthString"/>
        /// </summary>
        /// <returns></returns>
        public static DateTime ParseDate(string dayMonthString, int year)
        {
            if (!String.IsNullOrEmpty(dayMonthString) && year.ToString().Length == 4)
            {
                DateTime tempNewDateTime = ParseDate(dayMonthString);
                DateTime newDateTime = ParseDate(year, tempNewDateTime.Month, tempNewDateTime.Day);
                return newDateTime;
            }
            else throw new ArgumentNullException(nameof(dayMonthString));
        }

        /// <summary>
        /// Takes 3 integers <paramref name="yearNum"/>, <paramref name="monthNum"/>, <paramref name="dayNum"/> and returns a DateTime.
        /// </summary>
        /// <param name="yearNum"></param>
        /// <param name="monthNum"></param>
        /// <param name="dayNum"></param>
        /// <returns>A DateTime object.</returns>
        public static DateTime ParseDate(int yearNum, int monthNum, int dayNum)
        {
            // TODO: Requires all data checking.
            return new DateTime(year: yearNum, month: monthNum, day: dayNum);
        }

        /// <summary>
        /// Takes an int <paramref name="dayOfYear"/>(0-366) and int <paramref name="year"/> and returns a DateTime.
        /// </summary>
        /// <param name="dayOfYear">An int in the range of 0-366.</param>
        /// <param name="year">An valid 4 digit year.</param>
        /// <returns>A DateTime object.</returns>
        public static DateTime ParseDate(int dayOfYear, int year)
        {
            if (dayOfYear < 366 && dayOfYear > 0)
            {
                // Converting dayOfYear to DateTime to extract the month and days for use below.
                DateTime tempDateTime = new DateTime(year, 1, 1).AddDays(dayOfYear - 1);
                // This is necessary as error checking is performed in this method.
                DateTime newDateTime = ParseDate(tempDateTime.Year, tempDateTime.Month, tempDateTime.Day);
                return newDateTime;
            }
            else throw new ArgumentException("dayOfYear is not a valid value. Must be between 0-366.");
        }

        /// <summary>
        /// Convert a Julian Date to a DateTime object
        /// Where the Julian day begins at Greenwich mean noon 12pm. 12h UT.
        /// 2429996.0 is 1/1/1941 12:00
        /// </summary>
        /// <param name="julian_date"></param>
        /// <returns>A DateTime object.</returns>
        private static DateTime GetJulianDate(double julian_date)
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
        [Obsolete("Please use ParseDate instead", false)]
        public static DateTime GetDate(string ddMMM)
        {
            return ParseDate(ddMMM);
        }

        /// <summary>
        /// Get a DateTime from a 'ddMMM' string (ie '01Jan' OR '1-Jan' OR '1 Jan' etc)
        /// </summary>
        /// <param name="ddMMM">String containing 'day of month' and at least the first 3 letters of a month's name</param>
        /// <param name="year">The year to use when constructing the DateTime object</param>
        /// <returns>A DateTime constructed from <paramref name="ddMMM"/> using <paramref name="year"/></returns>
        [Obsolete("Please use ParseDate instead", false)]
        public static DateTime GetDate(string ddMMM, int year)
        {
            return ParseDate(ddMMM, year);
        }

        /// <summary>
        /// Get a DateTime from a 'ddMMM' string (ie '01Jan' OR '1-Jan' OR '1 Jan' etc), using <paramref name="today"/> to get the year to use
        /// </summary>
        /// <param name="ddMMM">String containing 'day of month' and at least the first 3 letters of a month's name</param>
        /// <param name="today">The year in this parameter will be used to construct the result</param>
        /// <returns>A DateTime constructed from <paramref name="ddMMM"/> using the year of <paramref name="today"/></returns>
        [Obsolete("Please use ParseDate instead", false)]
        public static DateTime GetDate(string ddMMM, DateTime today)
        {
            return ParseDate(ddMMM, today.Year);
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
            return GetNextDate(ParseDate(ddMMM, today.Year), today);
        }

        /// <summary>
        /// Get a Julian Date from a DateTime. Where the Julian day begins at Greenwich mean noon 12pm. 12h UT.
        /// 2429995.5 is 1/1/1941 00:00
        /// 2429996.0 is 1/1/1941 12:00
        /// </summary>
        /// <param name="date">The DateTime to convert</param>
        /// <returns>The Julian Date representation of <paramref name="date"/></returns>
        [Obsolete("To be removed", false)]
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
        [Obsolete("To be removed", false)]
        public static void JulianDayNumberToDayOfYear(int JDN, out int dyoyr, out int year)
        {
            DateTime date = GetJulianDate(JDN);
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
        [Obsolete("To be removed", false)]
        public static DateTime JulianDayNumberToDateTime(int JDN)
        {
            double jd = JDN - 0.5;  //Convert to true julian date value (at 00:00).
            return GetJulianDate(jd);     //equiv to new DateTime(y,m,d)
        }

        /// <summary>
        /// Convert the DateTime value to the Julian day number equivalent to what
        /// Clock would emit. Given the DateTime for a day would give the whole number
        /// for the standard Julian date at 12h on this day.
        /// e.g. 1/1/1941 00:00 - 23:59 --> 2429996
        /// </summary>
        /// <param name="adatetime"></param>
        /// <returns></returns>
        [Obsolete("To be removed", false)]
        public static int DateTimeToJulianDayNumber(DateTime adatetime)
        {
            return (int)System.Math.Truncate(GetJulianDate(adatetime) + 0.5);
        }

        /// <summary>
        /// Convert a dd/mm/yyyy to DateTime
        /// </summary>
        /// <param name="dmy">[d]d/[m]m/yyyy</param>
        /// <returns>The date</returns>
        [Obsolete("Please use ParseDate instead", false)]
        public static DateTime DMYtoDate(string dmy)
        {
            return ParseDate(dmy);
        }

        /// <summary>
        /// Takes in a string and checks to see if it is in the correct format for either a 'dd-mmm' value, or a full date
        /// with year, month and date (in any recognised date format).
        /// </summary>
        /// <param name="dateStr"></param>
        /// <returns>a string with the valid dd-Mmm string or a valid date as a string (yyyy-mm-dd)</returns>
        [Obsolete("To be deleted", false)]
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
                if (d == DateTime.MinValue)
                    return null;
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
        [Obsolete("To be deleted", false)]
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
