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
            Assert.AreEqual(title, axis.Title);
            Assert.AreEqual(position, axis.Position);
            Assert.AreEqual(inverted, axis.Inverted);
            Assert.AreEqual(crossesAtZero, axis.CrossesAtZero);
            Assert.AreEqual(min, axis.Minimum);
            Assert.AreEqual(max, axis.Maximum);
            Assert.AreEqual(interval, axis.Interval);
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
            Assert.AreEqual(title, axis.Title);
            Assert.AreEqual(position, axis.Position);
            Assert.AreEqual(false, axis.Inverted);
            Assert.AreEqual(false, axis.CrossesAtZero);
            Assert.AreEqual(null, axis.Minimum);
            Assert.AreEqual(null, axis.Maximum);
            Assert.AreEqual(null, axis.Interval);
        }
    }
}