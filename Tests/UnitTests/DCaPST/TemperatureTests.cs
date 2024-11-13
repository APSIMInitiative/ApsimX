using System;
using Moq;
using NUnit.Framework;
using Models.DCAPST.Environment;
using Models.DCAPST.Interfaces;

namespace UnitTests.DCaPST
{
    [TestFixture]
    public class TemperatureTests
    {
        [TestCaseSource(typeof(TemperatureTestData), nameof(TemperatureTestData.InvalidTimeTestCases))]
        public void UpdateAirTemperature_IfInvalidTime_ThrowsException(double time)
        {
            // Arrange
            var mock = new Mock<ISolarGeometry>(MockBehavior.Strict);
            mock.Setup(s => s.Sunset).Returns(18.47939124591235);
            mock.Setup(s => s.DayLength).Returns(12.958782491824698);

            // Act
            var temp = new Temperature(mock.Object)
            {
                MaxTemperature = 28,
                MinTemperature = 16
            };

            // Assert
            Assert.Throws<Exception>(() => temp.UpdateAirTemperature(time));
        }

        [TestCaseSource(typeof(TemperatureTestData), nameof(TemperatureTestData.ValidTimeTestCases))]
        public void UpdateAirTemperature_IfValidTime_SetsCorrectTemperature(double time, double expected)
        {
            // Arrange
            var mock = new Mock<ISolarGeometry>(MockBehavior.Strict);
            mock.Setup(s => s.Sunset).Returns(18.47939124591235).Verifiable();
            mock.Setup(s => s.DayLength).Returns(12.958782491824698).Verifiable();

            // Act
            var temp = new Temperature(mock.Object)
            {
                MaxTemperature = 28,
                MinTemperature = 16
            };

            temp.UpdateAirTemperature(time);
            var actual = temp.AirTemperature;

            // Assert
            Assert.That(actual, Is.EqualTo(expected));
            mock.Verify();
        }        
    }
}
