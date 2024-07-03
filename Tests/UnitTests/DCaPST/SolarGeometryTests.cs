using System;
using NUnit.Framework;

using Models.DCAPST;
using Models.DCAPST.Environment;

namespace UnitTests.DCaPST
{
    [TestFixture]
    public class SolarGeometryTests
    {
        private SolarGeometry solar;

        [SetUp]
        public void SetUp()
        {
            solar = new SolarGeometry()
            {
                DayOfYear = 144,
                Latitude = 18.3.ToRadians()
            };
            solar.Initialise();
        }

        [Test]
        public void SolarDeclinationTest()
        {
            var expected = 20.731383108171876;
            var actual = solar.SolarDeclination * 180 / Math.PI;
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void SunsetAngleTest()
        {
            var expected = 97.190868688685228;
            var actual = solar.SunsetAngle * 180 / Math.PI;
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void DayLengthTest()
        {
            var expected = 12.958782491824698;
            var actual = solar.DayLength;
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void SunriseTest()
        {
            var expected = 5.5206087540876512;
            var actual = solar.Sunrise;
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void SunsetTest()
        {
            var expected = 18.47939124591235;
            var actual = solar.Sunset;
            Assert.That(actual, Is.EqualTo(expected));
        }

        [TestCaseSource(typeof(SolarGeometryTestData), nameof(SolarGeometryTestData.SunAngleTestCases))]
        public void SunAngleTest(double hour, double expected)
        {
            var actual = solar.SunAngle(hour) * 180 / Math.PI;
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}
