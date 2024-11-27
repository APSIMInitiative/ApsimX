using APSIM.Shared.Graphing;
using NUnit.Framework;
using System;

namespace UnitTests.Services.Graphing
{
    /// <summary>
    /// Simple unit test for <see cref="Axis"/> type.
    /// </summary>
    [TestFixture]
    public class AxisTests
    {
        /// <summary>
        /// Ensure that the constructor initialises the correct fields.
        /// </summary>
        [Test]
        public void TestAxisConstructor()
        {
            string title = "axis title";
            AxisPosition position = AxisPosition.Right;
            bool inverted = true;
            bool crossesAtZero = true;
            double min = 23;
            double max = 24;
            double interval = 25;
            Axis axis = new Axis(title, position, inverted, crossesAtZero, min, max, interval);
            Assert.That(axis.Title, Is.EqualTo(title));
            Assert.That(axis.Position, Is.EqualTo(position));
            Assert.That(axis.Inverted, Is.EqualTo(inverted));
            Assert.That(axis.CrossesAtZero, Is.EqualTo(crossesAtZero));
            Assert.That(axis.Minimum, Is.EqualTo(min));
            Assert.That(axis.Maximum, Is.EqualTo(max));
            Assert.That(axis.Interval, Is.EqualTo(interval));
        }

        /// <summary>
        /// Ensure that the simple constructor initialises the correct fields.
        /// </summary>
        [Test]
        public void TestSimpleAxisConstructor()
        {
            string title = "axis title";
            AxisPosition position = AxisPosition.Left;
            Axis axis = new Axis(title, position);
            Assert.That(axis.Title, Is.EqualTo(title));
            Assert.That(axis.Position, Is.EqualTo(position));
            Assert.That(axis.Inverted, Is.EqualTo(false));
            Assert.That(axis.CrossesAtZero, Is.EqualTo(false));
            Assert.That(axis.Minimum, Is.EqualTo(null));
            Assert.That(axis.Maximum, Is.EqualTo(null));
            Assert.That(axis.Interval, Is.EqualTo(null));
        }
    }
}