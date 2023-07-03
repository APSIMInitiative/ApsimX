using DocumentFormat.OpenXml.Office.MetaAttributes;
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
        /// This class is used to hold the output when parsing a date string.
        /// It has the day, month, year as integers, with a boolean to note if the year was missing from the date.
        /// </summary>
        private class DateAsParts
        {
            public int day { get; set; }
            public int month { get; set; }
            public int year { get; set; }
            public bool yearWasMissing { get; set; }

            public DateAsParts(int day, int month, int year, bool yearWasMissing)
            {
                this.day = day;
                this.month = month;
                this.year = year;
                this.yearWasMissing = yearWasMissing;
            }
        }

        /// <summary>
        /// a list of month names in lower case.
        /// </summary>
        static public string[] LowerCaseMonths = CultureInfo.InvariantCulture.DateTimeFormat.AbbreviatedMonthNames;

        static private readonly char[] VALID_SEPERATORS = new char[] { '/', '-', ',', '.', '_', ' ' };
        private const char SEPERATOR_REPLACEMENT = '-';

        private const int DEFAULT_YEAR = 2000;
        private const string DEFAULT_FORMAT_DAY_MONTH = "dd-MMM";
        private const string DEFAULT_FORMAT_DAY_MONTH_YEAR = "yyyy-MM-dd";

        static private Regex
            rxDay = new Regex(@"^\d\d?$"),
            rxMonthNum = new Regex(@"^\d\d?$"),
            rxMonth3Letter = new Regex(@"^\w\w\w$"),
            rxMonth4Letter = new Regex(@"^\w\w\w\w$"),
            rxMonthFull = new Regex(@"^[^0-9]\w+[^0-9]$"),
            rxYear = new Regex(@"^\d\d\d\d$"),
            rxYearShort = new Regex(@"^\d\d$"),
            rxDateNoSymbol = new Regex(@"^\d\d\w\w\w$|^\w\w\w\d\d$"),
            rxDateAllNums = new Regex(@"^\d\d-\d\d-(\d{4}|\d{2})$"),
            rxISO = new Regex(@"^\d\d\d\d-\d\d-\d\d$|^\d\d\d\d-\d\d-\d\dT\d\d:\d\d:\d\d$");
            

        /// <summary>
        /// Convert any valid date string into a DateTime objects.
        /// </summary>
        /// <param name="dateString">The date</param>
        public static DateTime ParseDate(string dateString)
        {
            DateAsParts parts = ParseDateString(dateString);
            return ParseDate(parts.day, parts.month, parts.year);
        }

        /// <summary>
        /// Construct a DateTime from <paramref name="dateString"/> and <paramref name="date"/> then 'CompareTo' <paramref name="date"/>
        /// </summary>
        /// <param name="dateString">String containing a date in a supported format</param>
        /// <param name="date">A DateTime object such as Clock.Today</param>
        /// <returns>+1 if <paramref name="dateString"/> is less than <paramref name="date"/>, 0 if equal, -1 if greater</returns>
        public static int CompareDates(string dateString, DateTime date)
        {
            return date.CompareTo(ParseDate(dateString));
        }

        /// <summary>
        /// Compares the day and month of <paramref name="date1"/> and <paramref name="date2"/> and ignoring the year.
        /// This version takes two string dates and parses them before comparing.
        /// </summary>
        /// <param name="date1">First Date string</param>
        /// <param name="date2">Second Date string</param>
        /// <returns></returns>
        public static bool DayMonthIsEqual(string date1, string date2)
        {
            return DayMonthEqual(ParseDate(date1), ParseDate(date2));
        }

        /// <summary>
        /// Compares the day and month of <paramref name="date1"/> and <paramref name="date2"/> and ignoring the year.
        /// This version takes one date as a string and the other as a DateTime (such as Clock.Today).
        /// </summary>
        /// <param name="date1">A Date string</param>
        /// <param name="date2">A DateTime</param>
        /// <returns></returns>
        public static bool DayMonthIsEqual(string date1, DateTime date2)
        {
            return DayMonthEqual(ParseDate(date1), date2);
        }

        /// <summary>
        /// Compares the day and month of <paramref name="date1"/> and <paramref name="date2"/> and ignoring the year.
        /// This version takes two DateTime variables and compares them.
        /// </summary>
        /// <param name="date1">First DateTime</param>
        /// <param name="date2">Second DateTime</param>
        /// <returns></returns>
        public static bool DayMonthEqual(DateTime date1, DateTime date2)
        {
            if ((date1.Day == date2.Day) && (date1.Month == date2.Month))
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
            if (rxDay.Match(dayString).Success)
                return int.Parse(dayString);
            else
                throw new Exception($"Date {fullDate} is formatted for ISO, however has {dayString} for day. Day must be exactly 2 numbers.");
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
            if (rxYear.Match(yearString).Success)
            {
                return int.Parse(yearString);
            }
            else if (rxYearShort.Match(yearString).Success)
            {
                return DEFAULT_YEAR + int.Parse(yearString);
            }
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
            else throw new Exception($"One or both parameters in ParseDate({dateString}, {formatString}) are null or empty.");
        }

        /// <summary>
        /// Takes a <paramref name="dayMonthString"/>
        /// </summary>
        /// <returns></returns>
        public static DateTime ParseDate(string dayMonthString, int year)
        {
            DateAsParts parts = ParseDateString(dayMonthString);
            DateTime newDateTime = ParseDate(parts.day, parts.month, year);
            return newDateTime;
        }

        /// <summary>
        /// Takes a DateAsParts object <paramref name="parts"/> and returns a DateTime.
        /// </summary>
        /// <param name="parts"></param>
        /// <returns>A DateTime object.</returns>
        private static DateTime ParseDate(DateAsParts parts)
        {
            return ParseDate(parts.day, parts.month, parts.year);
        }

        /// <summary>
        /// Takes 3 integers <paramref name="day"/>, <paramref name="month"/>, <paramref name="year"/> and returns a DateTime.
        /// </summary>
        /// <param name="day"></param>
        /// <param name="month"></param>
        /// <param name="year"></param>
        /// <returns>A DateTime object.</returns>
        public static DateTime ParseDate(int day, int month, int year)
        {
            try
            {
                DateTime date = new DateTime(year, month, day);
                return date;
            }
            catch (Exception e)
            {
                throw new Exception(e.Message, e.InnerException);
            }
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
        /// Jan01
        /// 01Jan
        /// 01-02-2022 (With Warning as it's ambigous)
        /// </summary>
        /// <param name="dateString">The date</param>
        /// <returns>An int array with [year, month, day]</returns>
        private static DateAsParts ParseDateString(string dateString)
        {
            string dateWithSymbolsParsed = dateString;
            //trim whitespace
            string dateTrimmed = dateWithSymbolsParsed.Trim();

            //check that the string has a valid symbol
            //replace it with a tab character

            //valid choices: / - , . _
            
            int types = 0;
            foreach (char c in VALID_SEPERATORS)
            {
                if (dateString.Contains(c))
                {
                    types += 1;
                    //change symbol to \t
                    dateTrimmed = dateTrimmed.Replace(c, SEPERATOR_REPLACEMENT);
                }
            }

            //make sure only 1 or two of symbol and only has one type of these symbols
            if ((types == 0) || (types > 1))
            {
                string symbols = " ";
                foreach (char c in VALID_SEPERATORS)
                    symbols += c + " ";

                if (types > 1)
                {
                    throw new Exception($"Date {dateString} cannot be parsed as it multiple symbol types. ({symbols}).");
                }
                else if (types == 0)
                {
                    //we may be dealing with a Jan01 or 01Jan string, we need to chekc and handle that
                    Match result = rxDateNoSymbol.Match(dateTrimmed);
                    if (result.Success && result.Groups.Count == 2)
                    {
                        //convert it to 01-Jan format
                        dateTrimmed = result.Groups[0] + SEPERATOR_REPLACEMENT.ToString() + result.Groups[1];
                    }
                    else
                    {
                        throw new Exception($"Date {dateString} cannot be parsed as it contains no valid symbols. ({symbols}).");
                    }
                }
            }

            //seperate by \t to get parts
            string[] parts = dateTrimmed.Split(SEPERATOR_REPLACEMENT);

            //check that there are 2 or 3 parts and that each part has text in it
            if (parts.Length < 2 || parts.Length > 3)
                throw new Exception($"Date {dateString} cannot be parsed as it only has {parts.Length} parts. Date should have 2 or 3 parts (day-month-year or day-month).");

            foreach (string part in parts)
                if (part.Length == 0)
                    throw new Exception($"Date {dateString} cannot be parsed as it it has an empty part after a symbol.");

            int dayNum;
            int monthNum;
            int yearNum;
            bool yearMissing = false;
            //if date is in ISO format 2000-01-01 or 2000-01-01T00:00:00
            if (rxISO.Match(dateTrimmed).Success)
            {
                yearNum = ParseYearString(parts[0], dateTrimmed);
                monthNum = ParseMonthString(parts[1], dateTrimmed);

                //if this is a full ISO, split on the T character
                if (parts[2].Contains('T'))
                    parts[2] = parts[2].Split('T')[0];

                dayNum = ParseDayString(parts[2], dateString);
            }
            //if date is in ambigous 01-01-2000 format
            else if (rxDateAllNums.Match(dateTrimmed).Success)
            {
                //by default we treat these as day-month-year
                dayNum = ParseDayString(parts[0], dateString);
                monthNum = ParseMonthString(parts[1], dateString);
                yearNum = ParseYearString(parts[2], dateString);
                //but we need to give the user a warning that their date is ambigous
                //WARNING HERE
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
                    yearMissing = true;
                }
            }

            DateAsParts values = new DateAsParts(dayNum, monthNum, yearNum, yearMissing);
            return values;
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
        public static string validateDateString(string dateStr)
        {
            DateAsParts parts;
            try
            {
                parts = ParseDateString(dateStr);
            }
            catch
            {
                return null;
            }

            DateTime date = ParseDate(parts);

            if (parts.yearWasMissing)
            {
                //for consistency, return it as 'Title' case (ie, 01-Jan, not 1-jan)
                return date.ToString(DEFAULT_FORMAT_DAY_MONTH, CultureInfo.InvariantCulture);
            } 
            else
            {
                return date.ToString(DEFAULT_FORMAT_DAY_MONTH_YEAR, CultureInfo.InvariantCulture);
            }
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
            DateAsParts parts;
            try
            {
                parts = ParseDateString(dateStr);
            }
            catch
            {
                return new DateTime();
            }

            if (parts.yearWasMissing)
            {
                //for consistency, return it as 'Title' case (ie, 01-Jan, not 1-jan)
                return ParseDate(parts.day, parts.month, year);
            }
            else
            {
                return ParseDate(parts);
            }
        }

        /// <summary>
        /// Compare <paramref name="date"/> and <paramref name="today"/> (ignoring year component)
        /// </summary>
        /// <param name="date">String, "dd-mmm" </param>
        /// <param name="today">DateTime, to compare to date (e.g Clock.Today)</param>
        /// <returns>true if the day and month components of <paramref name="today"/> match ddMMM, else false</returns>
        [Obsolete("Function renamed to DayMonthIsEqual to avoid confusion.", false)]
        public static bool DatesEqual(string date, DateTime today)
        {
            return DayMonthIsEqual(date, today);
        }
    }


}
