using APSIM.Shared.Utilities;
using DocumentFormat.OpenXml.Bibliography;
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
            //build list of support month names
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
                            //our date to compare against
                            DateTime date = new DateTime(int.Parse(fullYear), monthNum, day);

                            //Day-Month-Year
                            DateTime parsed = DateUtilities.GetDate(day.ToString() + seperator.ToString() + months[month] + seperator.ToString() + year.ToString());
                            Assert.AreEqual(date, parsed);

                            date = new DateTime(DateUtilities.DEFAULT_YEAR, monthNum, day);
                            
                            if (months[month].Length > 2) { //we don't want to do these when month is a number, that should fail
                                //Month-Day-Year
                                parsed = DateUtilities.GetDate(months[month] + seperator.ToString() + day.ToString() + seperator.ToString() + year.ToString());
                                Assert.AreEqual(date, parsed);

                                //Day-Month
                                parsed = DateUtilities.GetDate(day.ToString() + seperator.ToString() + months[month]);
                                Assert.AreEqual(date, parsed);

                                //Month-Day
                                parsed = DateUtilities.GetDate(months[month] + seperator.ToString() + day.ToString());
                                Assert.AreEqual(date, parsed);

                                //DayMonth
                                parsed = DateUtilities.GetDate(day.ToString() + months[month]);
                                Assert.AreEqual(date, parsed);

                                //MonthDay
                                parsed = DateUtilities.GetDate(months[month] + day.ToString());
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
            Assert.Throws<Exception>(() => DateUtilities.GetDate("01-23-2000")); //US Format with no month name
            Assert.Throws<Exception>(() => DateUtilities.GetDate("01-13-2000")); //Impossible month
            Assert.Throws<Exception>(() => DateUtilities.GetDate("40-01-2000")); //Impossible day
        }
    }
}
