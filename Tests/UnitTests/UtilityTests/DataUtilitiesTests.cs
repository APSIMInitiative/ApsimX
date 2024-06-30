using APSIM.Shared.Utilities;
using NUnit.Framework;
using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace UnitTests.UtilityTests
{
    [TestFixture]
    public class DataUtilitiesTests
    {
        [Test]
        public void TestValidDateFormats()
        {
            //build list of supported month names
            string[] months = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12",
                                "01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "12"};
            for (int i = 0; i <= 12; i++)
                months.Append(DateUtilities.MONTHS_3_LETTERS[i]);
            for (int i = 0; i <= 12; i++)
                months.Append(DateUtilities.MONTHS_AU_LETTERS[i]);
            for (int i = 0; i <= 12; i++)
                months.Append(DateUtilities.MONTHS_FULL_NAME[i]);

            //selection of semi random years so we don't have to test every day of every year
            string[] years = { "1900", "1907", "1923", "1935", "1948", "1952", "1964", "1973", "1985", "1999", "2000", "2004", "2016", "2023", "05", "10", "21" };

            //ISO - test ISO format sperately
            string isoTest1 = $"2000-01-01T00:00:00";
            string isoTest2 = $"2000-01-01";
            Assert.AreEqual(DateTime.Parse(isoTest1, null, DateTimeStyles.RoundtripKind), DateUtilities.GetDate(isoTest1));
            Assert.AreEqual(DateTime.Parse(isoTest2, null, DateTimeStyles.RoundtripKind), DateUtilities.GetDate(isoTest2));

            //check dates are trimmed
            string trimTest = $" 2000-01-01 ";
            Assert.AreEqual(DateTime.Parse(trimTest, null, DateTimeStyles.RoundtripKind), DateUtilities.GetDate(trimTest));

            foreach (char seperator in DateUtilities.VALID_SEPERATORS)
            {
                foreach (string year in years)
                {
                    string fullYear = year;
                    if (fullYear.Length == 2)
                        fullYear = "20" + fullYear;

                    for (int month = 0; month < months.Length; month++)
                    {
                        int monthNum = (month % 12) + 1;
                        int daysInMonth = DateTime.DaysInMonth(int.Parse(year), monthNum);
                        for (int day = 1; day <= daysInMonth; day++)
                        {
                            string ddmmyyyy = $"{day}{seperator}{months[month]}{seperator}{year}";
                            string ddmm = $"{day}{seperator}{months[month]}";
                            string mmdd = $"{months[month]}{seperator}{day}";

                            //our date to compare against
                            DateTime date = new DateTime(int.Parse(fullYear), monthNum, day);

                            //Day-Month-Year
                            DateTime parsed = DateUtilities.GetDate(ddmmyyyy);
                            Assert.AreEqual(date, parsed);

                            //day of year
                            parsed = DateUtilities.GetDate(date.DayOfYear, int.Parse(fullYear));
                            Assert.AreEqual(date, parsed);

                            //day/month with date for year
                            parsed = DateUtilities.GetDate(ddmm, date);
                            Assert.AreEqual(date, parsed);
                            //with year in string
                            parsed = DateUtilities.GetDate(ddmmyyyy, date);
                            Assert.AreEqual(date, parsed);

                            //3 Ints
                            parsed = DateUtilities.GetDate(day, monthNum, int.Parse(fullYear));
                            Assert.AreEqual(date, parsed);

                            //now we do all the date checking that will use a default year
                            date = new DateTime(DateUtilities.DEFAULT_YEAR, monthNum, day);

                            //day/month with mismatched year in string
                            parsed = DateUtilities.GetDate(ddmm + seperator.ToString() + DateUtilities.DEFAULT_YEAR.ToString(), date);
                            Assert.AreEqual(date, parsed);
                            //with int year instead of date
                            parsed = DateUtilities.GetDate(ddmm, DateUtilities.DEFAULT_YEAR);
                            Assert.AreEqual(date, parsed);

                            if (months[month].Length > 2)
                            { //we don't want to do these when month is a number, that should fail
                                //Month-Day-Year
                                parsed = DateUtilities.GetDate(mmdd + seperator.ToString() + year.ToString());
                                Assert.AreEqual(date, parsed);

                                //Day-Month
                                parsed = DateUtilities.GetDate(ddmm);
                                Assert.AreEqual(date, parsed);

                                //Month-Day
                                parsed = DateUtilities.GetDate(mmdd);
                                Assert.AreEqual(date, parsed);

                                //DayMonth
                                parsed = DateUtilities.GetDate(day.ToString() + months[month]);
                                Assert.AreEqual(date, parsed);

                                //MonthDay
                                parsed = DateUtilities.GetDate(months[month] + day.ToString());
                                Assert.AreEqual(date, parsed);

                                //Replace Year
                                parsed = DateUtilities.GetDateReplaceYear(ddmm, DateUtilities.DEFAULT_YEAR);
                                Assert.AreEqual(date, parsed);

                                parsed = DateUtilities.GetDateReplaceYear(ddmmyyyy, DateUtilities.DEFAULT_YEAR);
                                Assert.AreEqual(date, parsed);
                            }
                        }
                    }
                }
            }
        }

        [Test]
        public void TestInvalidDateFormats()
        {
            Assert.Throws<Exception>(() => DateUtilities.GetDate("")); //empty string
            Assert.Throws<Exception>(() => DateUtilities.GetDate("   ")); //whitespace
            Assert.Throws<Exception>(() => DateUtilities.GetDate("01")); //one number
            Assert.Throws<Exception>(() => DateUtilities.GetDate("String")); //a word
            Assert.Throws<Exception>(() => DateUtilities.GetDate("String-String-String")); //a word with seperators
            Assert.Throws<Exception>(() => DateUtilities.GetDate(null)); //null
            Assert.Throws<Exception>(() => DateUtilities.GetDate("29-Feburary-1900")); //no leap year despite being a leap year in 1900
            Assert.Throws<Exception>(() => DateUtilities.GetDate("29-Feburary-2001"));
            Assert.Throws<Exception>(() => DateUtilities.GetDate("29-Feburary-2000")); //Leap Year
            Assert.Throws<Exception>(() => DateUtilities.GetDate("Jab-12")); //bad month
            Assert.Throws<Exception>(() => DateUtilities.GetDate("01:Jan:2000")); //bad symbol
            Assert.Throws<Exception>(() => DateUtilities.GetDate("01-Jan:2000")); //different symbols
            Assert.Throws<Exception>(() => DateUtilities.GetDate("01-Jan-2000-")); //to many symbols
            Assert.Throws<Exception>(() => DateUtilities.GetDate("01-Jan-")); //empty end
            Assert.Throws<Exception>(() => DateUtilities.GetDate("01-23-2000")); //US Format with no month name
            Assert.Throws<Exception>(() => DateUtilities.GetDate("01-13-2000")); //Impossible month
            Assert.Throws<Exception>(() => DateUtilities.GetDate("40-01-2000")); //Impossible day
            Assert.Throws<Exception>(() => DateUtilities.GetDate("1-Aug-15-Aug")); //typo in date list
        }

        [Test]
        public void TestDateFunctions()
        {
            int day = 10;
            string month = "Jan";
            int year = 2000;

            string ddmmyyyy = $"{day}-{month}-{year}";
            string ddmm = $"{day}-{month}";
            string mmdd = $"{month}-{day}";

            string yyyymmdd = $"{year}-01-{day}";

            DateTime date = new DateTime(year, 1, day);

            //Compares
            Assert.Zero(DateUtilities.CompareDates(ddmmyyyy, date));
            Assert.Positive(DateUtilities.CompareDates(ddmmyyyy, date.AddDays(1)));
            Assert.Negative(DateUtilities.CompareDates(ddmmyyyy, date.AddDays(-1)));

            //DayMonth comparisions
            Assert.IsTrue(DateUtilities.DayMonthIsEqual(ddmmyyyy, "2000-01-10"));
            Assert.IsFalse(DateUtilities.DayMonthIsEqual(ddmmyyyy, "2000-01-11"));

            Assert.IsTrue(DateUtilities.DayMonthIsEqual(ddmmyyyy, date));
            Assert.IsFalse(DateUtilities.DayMonthIsEqual(ddmmyyyy, date.AddDays(1)));
            Assert.IsTrue(DateUtilities.DayMonthIsEqual(date, date.AddYears(1)));

            //WithinDates
            string ddmmyyyy_5 = $"{day + 5}-{month}-{year}";
            string ddmmyyyy_10 = $"{day + 10}-{month}-{year}";
            Assert.IsTrue(DateUtilities.WithinDates(ddmmyyyy, new DateTime(2000, 1, 12), ddmmyyyy_5)); //between
            Assert.IsFalse(DateUtilities.WithinDates(ddmmyyyy_5, new DateTime(2000, 1, 12), ddmmyyyy_10)); //not between
            Assert.IsTrue(DateUtilities.WithinDates(ddmmyyyy_10, date, ddmmyyyy_5)); //between, wrap around year
            Assert.IsFalse(DateUtilities.WithinDates(ddmmyyyy_10, new DateTime(2000, 1, 12), ddmmyyyy)); //not between, wrap around year
            Assert.IsTrue(DateUtilities.WithinDates(ddmmyyyy, date, ddmmyyyy)); //on same date

            //DatesAreEqual
            Assert.IsTrue(DateUtilities.DatesAreEqual(ddmmyyyy, date));
            Assert.IsFalse(DateUtilities.DatesAreEqual(ddmmyyyy, date.AddDays(1)));
            Assert.IsFalse(DateUtilities.DatesAreEqual(ddmmyyyy, date.AddMonths(1)));
            Assert.IsFalse(DateUtilities.DatesAreEqual(ddmmyyyy, date.AddYears(1)));

            //IsEndOfMonth
            string ddmmyyyy_endMonth = $"31-Jan-2000";
            Assert.IsTrue(DateUtilities.IsEndOfMonth(DateUtilities.GetDate(ddmmyyyy_endMonth)));
            Assert.IsFalse(DateUtilities.IsEndOfMonth(DateUtilities.GetDate(ddmmyyyy)));

            //IsEndOfYear
            string ddmmyyyy_endYear = $"31-Dec-2000";
            Assert.IsTrue(DateUtilities.IsEndOfYear(DateUtilities.GetDate(ddmmyyyy_endYear)));
            Assert.IsFalse(DateUtilities.IsEndOfYear(DateUtilities.GetDate(ddmmyyyy)));

            //ValidateDateString
            Assert.AreEqual(yyyymmdd, DateUtilities.ValidateDateString("10/January/2000"));
            Assert.AreEqual(ddmm, DateUtilities.ValidateDateString("10 January"));
            Assert.AreEqual(ddmm, DateUtilities.ValidateDateString("January 10"));

            Assert.Null(DateUtilities.ValidateDateString("FakeMonth 10"));

            //ValidateDateStringWithYear
            Assert.AreEqual(yyyymmdd, DateUtilities.ValidateDateStringWithYear("10/January/2000"));
            Assert.Null(DateUtilities.ValidateDateStringWithYear("10 January"));

            //GetNextDate
            Assert.AreEqual(DateUtilities.GetDate("2-Jan-2001"), DateUtilities.GetNextDate("2-Jan", date)); //2-Jan is before date
            Assert.AreEqual(DateUtilities.GetDate("20-Jan-2000"), DateUtilities.GetNextDate("20-Jan", date)); //20-Jan is after date
            Assert.AreEqual(DateUtilities.GetDate("2-Jan-2001"), DateUtilities.GetNextDate("2-Jan-2000", date)); //With year
            Assert.AreEqual(DateUtilities.GetDate("20-Jan-2000"), DateUtilities.GetNextDate("20-Jan-2000", date)); //With year
            Assert.AreEqual(DateUtilities.GetDate("2-Jan-2001"), DateUtilities.GetNextDate("2-Jan-1996", date)); //With multiple years
        }
    }
}
