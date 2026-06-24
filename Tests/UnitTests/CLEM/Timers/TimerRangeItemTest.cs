using DocumentFormat.OpenXml.Bibliography;
using Models.CLEM;
using Models.CLEM.Timers;
using NUnit.Framework;
using System;
using System.Globalization;
using System.Reflection;

namespace UnitTests.CLEM;

[TestFixture]
public class TimerRangeItemTest
{
    private CLEMEvents clemEvents;

    [SetUp]
    public void SetUp()
    {
        clemEvents = new CLEMEvents()
        {
            TimeStep = TimeStepTypes.Weekly,
            Clock = new Models.Clock()
            {
                StartDate = new DateTime(2004, 1, 1),
                EndDate = new DateTime(2005, 1, 1)
            }
        };
    }

    // initialise valid AgeSpecifier inputs
    // test initialisation of TimerRangeItem with various AgeSpecifier inputs
    [TestCase(2000, 10, 10, "10/10/2000", true)] // year, month and day all provided for start of range
    [TestCase(2000, 0, 0, "1/01/2000", true)] // year only provided for start of range
    [TestCase(2000, 0, 0, "31/12/2000", false)] // year only provided for end of range
    [TestCase(2000, 6, 0, "1/06/2000", true)] // year and month only provided for start of range
    [TestCase(2000, 6, 0, "30/06/2000", false)] // year and month only provided for end of range
    [TestCase(0, 10, 0, "1/10/2004", true)] // month only provided for start of range - uses events.clock.startYear
    [TestCase(0, 10, 0, "31/10/2004", false)] // month only provided for end of range - uses events.clock.startYear
    [TestCase(0, 10, 5, "5/10/2004", true)] // month only provided for start of range - uses events.clock.startYear
    [TestCase(0, 0, 60, "29/02/2004", true)] // days only provided for start of range - uses events.clock.startYear
    [TestCase(0, 0, 100, "10/04/2004", false)] // days only provided for end of range allowing for leap year
    [TestCase(2002, 0, 100, "10/04/2002", true)] // year and days provided for start of range when not a leap year
    public void TimerInitialiseValidAgeSpecifier(int year, int month, int day, string dateString, bool startOfRange)
    {
        AgeSpecifier ageSpecifier = new()
        {
            Parts = [year, month, day]
        };
        // Define the expected date format
        string format = "d/MM/yyyy";
        CultureInfo provider = CultureInfo.InvariantCulture;

        var date = DateTime.ParseExact(dateString, format, provider);
        TimerRangeItem timerRangeItem = new(clemEvents, ageSpecifier, startOfRange, true);

        Assert.That(timerRangeItem.Date, Is.EqualTo(date));
        Assert.That(timerRangeItem.ErrorMessages.Count, Is.EqualTo(0));
    }

    // initialise invalid AgeSpecifier inputs
    // test initialisation of TimerRangeItem with various invalid AgeSpecifier inputs
    [TestCase(0, 0, 0, "Empty date specifier supplied")] // missing value
    [TestCase(2000, 14, 0, "Month must be between 1 and 12 when providing details with date or month style (x,m,x)")] // invalid month > 12
    [TestCase(2000, -2, 0, "Month must be between 1 and 12 when providing details with date or month style (x,m,x)")] // invalid month < 0
    [TestCase(0, 0, 366, "Day of year must be between 1 and 365 when providing details in day style (0,0,d)")] // invalid day for non-leap year
    [TestCase(2000, 0, 3, "Month must be provided with date style(x, m, d)")] // missing month
    [TestCase(2000, 9, 31, "Invalid days [31] of month [9] for style (x,m,d)")] // invalid days for specified month
    [TestCase(2000, 9, 0, "Invalid date parts. Days cannot be 0 with year and month (y,m,0)")] // invalid days for specified month
    public void TimerNextStartDate(int year, int month, int day, string errorString)
    {
        var ageSpecifier = new AgeSpecifier()
        {
            Parts = [day, month, year]
        };
        TimerRangeItem timerRangeItem = new(clemEvents, ageSpecifier, true, true);

        Assert.That(timerRangeItem.ErrorMessages.Contains(errorString), Is.EqualTo(true));
        Assert.That(timerRangeItem.ErrorMessages.Count, Is.GreaterThan(0));
    }
}
