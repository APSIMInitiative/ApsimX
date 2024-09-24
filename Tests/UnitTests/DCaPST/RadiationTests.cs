using System;
using NUnit.Framework;
using Models.DCAPST.Environment;
using Models.DCAPST.Interfaces;
using Moq;

namespace UnitTests.DCaPST
{
    [TestFixture]
    public class RadiationTests
    {    
        public Mock<ISolarGeometry> SetupMockSolar(double time, double angle)
        {
            Mock<ISolarGeometry> mock = new Mock<ISolarGeometry>(MockBehavior.Strict);
            mock.Setup(s => s.Sunrise).Returns(5.5206087540876512).Verifiable();
            mock.Setup(s => s.Sunset).Returns(18.47939124591235).Verifiable();
            mock.Setup(s => s.DayLength).Returns(12.958782491824698);
            mock.Setup(s => s.SolarConstant).Returns(1360).Verifiable();            
            //mock.Setup(s => s.SunAngle(6.0)).Returns(0.111379441989282).Verifiable();                        
            mock.Setup(s => s.SunAngle(time)).Returns(angle);            

            return mock;
        }        

        [TestCaseSource(typeof(RadiationTestData), nameof(RadiationTestData.HourlyRadiationTestCases))]
        public void HourlyRadiation_WhenTimeOutOfBounds_ThrowsException(double time, double sunAngle)
        {
            // Arrange            
            var mock = SetupMockSolar(time, sunAngle);
            var radiation = new SolarRadiation(mock.Object)
            {
                Daily = 16.5,
                RPAR = 0.5
            };

            // Act

            // Assert
            Assert.Throws<Exception>(() => radiation.UpdateRadiationValues(time));
        }

        [TestCaseSource(typeof(RadiationTestData), nameof(RadiationTestData.IncidentRadiationTestCases))]
        public void IncidentRadiation_GivenValidInput_MatchesExpectedValue(double time, double expected, double sunAngle)
        {
            // Arrange
            var mock = SetupMockSolar(time, sunAngle);
            var radiation = new SolarRadiation(mock.Object)
            {
                Daily = 16.5,
                RPAR = 0.5
            };

            // Act
            radiation.UpdateRadiationValues(time);
            var actual = radiation.Total;

            // Assert
            Assert.That(actual, Is.EqualTo(expected));
            mock.Verify();
        }

        [TestCaseSource(typeof(RadiationTestData), nameof(RadiationTestData.DiffuseRadiationTestCases))]
        public void DiffuseRadiation_GivenValidInput_MatchesExpectedValue(double time, double expected, double sunAngle)
        {
            // Arrange
            var mock = SetupMockSolar(time, sunAngle);
            var radiation = new SolarRadiation(mock.Object)
            {
                Daily = 16.5,
                RPAR = 0.5
            };

            // Act
            radiation.UpdateRadiationValues(time);
            var actual = radiation.Diffuse;

            // Assert
            Assert.That(actual, Is.EqualTo(expected));
            mock.Verify();
        }

        [TestCaseSource(typeof(RadiationTestData), nameof(RadiationTestData.DirectRadiationTestCases))]
        public void DirectRadiation_GivenValidInput_MatchesExpectedValue(double time, double expected, double sunAngle)
        {
            // Arrange
            var mock = SetupMockSolar(time, sunAngle);
            var radiation = new SolarRadiation(mock.Object)
            {
                Daily = 16.5,
                RPAR = 0.5
            };

            // Act
            radiation.UpdateRadiationValues(time);
            var actual = radiation.Direct;

            // Assert
            Assert.That(actual, Is.EqualTo(expected));
            mock.Verify();
        }

        [TestCaseSource(typeof(RadiationTestData), nameof(RadiationTestData.DiffuseRadiationParTestCases))]
        public void DiffuseRadiationPAR_GivenValidInput_MatchesExpectedValue(double time, double expected, double sunAngle)
        {
            // Arrange
            var mock = SetupMockSolar(time, sunAngle);
            var radiation = new SolarRadiation(mock.Object)
            {
                Daily = 16.5,
                RPAR = 0.5
            };

            // Act
            radiation.UpdateRadiationValues(time);
            var actual = radiation.DiffusePAR;

            // Assert
            Assert.That(actual, Is.EqualTo(expected));
            mock.Verify();
        }

        [TestCaseSource(typeof(RadiationTestData), nameof(RadiationTestData.DirectRadiationParTestCases))]
        public void DirectRadiationPAR_GivenValidInput_MatchesExpectedValue(double time, double expected, double sunAngle)
        {
            // Arrange
            var mock = SetupMockSolar(time, sunAngle);
            var radiation = new SolarRadiation(mock.Object)
            {
                Daily = 16.5,
                RPAR = 0.5
            };

            // Act
            radiation.UpdateRadiationValues(time);
            var actual = radiation.DirectPAR;

            // Assert
            Assert.That(actual, Is.EqualTo(expected));
            mock.Verify();
        }
    }
}
