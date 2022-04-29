using APSIM.Shared.Utilities;
using Models;
using Models.Core;
using Moq;
using NUnit.Framework;
using System;

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
        /// Ensure that the given reporting frequency is a valid event reporting frequency.
        /// </summary>
        /// <param name="frequency">Input line - should be any valid reporting frequency using a model event.</param>
        [TestCase("[Clock].DoReport")]
        [TestCase("[Clock with space].DoReport")]
        public void TestReportingFrequency(string frequency)
        {
            Assert.True(EventReportFrequency.TryParse(frequency, new Models.Report(), mockEvents.Object));
        }
    }
}