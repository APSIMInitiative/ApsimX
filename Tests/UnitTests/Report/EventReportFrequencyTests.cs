using Models;
using Models.Core;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;

namespace UnitTests.Reporting
{
    /// <summary>
    /// Unit tests for <see cref="EventReportFrequency"/> class.
    /// </summary>
    [TestFixture]
    public class EventReportFrequencyTests
    {
        /// <summary>
        /// Mocked events service.
        /// </summary>
        private Mock<IEvent> mockEvents;

        /// <summary>
        /// Setup the test environment.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            mockEvents = new Mock<IEvent>();
        }

        /// <summary>
        /// Ensure that the given reporting frequency date is a valid date to parse
        /// </summary>
        /// <param name="line">Input line - should be any valid reporting frequency using a model event.</param>
        [TestCase("1-May")]
        public void TestReportingDate(string line)
        {
            Assert.That(DateReportFrequency.TryParse(line, new Models.Report(), mockEvents.Object), Is.True);
        }

        /// <summary>
        /// Ensure that the given reporting frequency is a valid event reporting frequency.
        /// </summary>
        /// <param name="line">Input line - should be any valid reporting frequency using a model event.</param>
        [TestCase("[Clock].DoReport")]
        [TestCase("[Clock with space].DoReport")] 
        public void TestReportingFrequency(string line)
        {
            //this is the order lines are parsed by Report.cs
            Assert.That(DateReportFrequency.TryParse(line, new Models.Report(), mockEvents.Object), Is.False);
            Assert.That(EventReportFrequency.TryParse(line, new Models.Report(), mockEvents.Object), Is.True);
        }

        /// <summary>
        /// Ensure that the given reporting frequency expression is a valid expression to compile and
        /// does not get parsed by an earlier method
        /// </summary>
        /// <param name="line">Input line - should be any valid reporting frequency using a model event.</param>
        [TestCase("[Clock].Today.Month == 1 && [Clock].Today.Day == 1")]
        [TestCase("[Clock].DoReport && [Clock].Today.Year > 2000")]
        public void TestReportingExpression(string line)
        {
            Models.Report report = new Models.Report()
            {
                Name = "Report",
                Children = new List<IModel>() { new Clock() }
            };

            ScriptCompiler compiler = new ScriptCompiler();
            //this is the order lines are parsed by Report.cs
            Assert.That(DateReportFrequency.TryParse(line, new Models.Report(), mockEvents.Object), Is.False);
            Assert.That(EventReportFrequency.TryParse(line, new Models.Report(), mockEvents.Object), Is.False);
            Assert.That(ExpressionReportFrequency.TryParse(line, report, mockEvents.Object, compiler), Is.True);
        }
    }
}