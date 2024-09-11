using System;
using NUnit.Framework;
using Models.DCAPST.Environment;
using Models.DCAPST.Interfaces;
using Moq;
using Models.DCAPST;

namespace UnitTests.DCaPST
{
    public class WaterInteractionTests
    {
        [Test]
        public void UnlimitedRtw_WhenCalculated_ReturnsExpectedValue()
        {
            // Arrange
            var temperature = new Mock<ITemperature>(MockBehavior.Strict);
            temperature.Setup(t => t.AtmosphericPressure).Returns(1.01325).Verifiable();
            temperature.Setup(t => t.MinTemperature).Returns(16.2).Verifiable();
            temperature.Setup(t => t.AirMolarDensity).Returns(40.63).Verifiable();

            var leafTemp = 27.0;
            var gbh = 0.127634;

            var A = 4.5;
            var Ca = 380.0;
            var Ci = 152.0;

            var expected = 1262.0178666386046;

            // Act
            var water = new WaterInteraction(temperature.Object);
            water.SetConditions(gbh, 0.0);
            water.LeafTemp = leafTemp;
            var actual = water.UnlimitedWaterResistance(A, Ca, Ci);

            // Assert
            Assert.That(actual, Is.EqualTo(expected));
            temperature.Verify();
        }

        [Test]
        public void LimitedRtw_WhenCalculated_ReturnsExpectedValue()
        {
            // Arrange
            var temperature = new Mock<ITemperature>(MockBehavior.Strict);
            temperature.Setup(t => t.AirTemperature).Returns(27.0).Verifiable();
            temperature.Setup(t => t.MinTemperature).Returns(16.2).Verifiable();

            var leafTemp = 27;
            var gbh = 0.127634;

            var available = 0.15;
            var rn = 230;

            var expected = 340.83946167121144;

            // Act
            var water = new WaterInteraction(temperature.Object);
            water.SetConditions(gbh, rn);
            water.LeafTemp = leafTemp;
            var actual = water.LimitedWaterResistance(available);

            // Assert
            Assert.That(actual, Is.EqualTo(expected));
            temperature.Verify();
        }

        [Test]
        public void HourlyWaterUse_WhenCalculated_ReturnsExpectedValue()
        {
            // Arrange
            var temperature = new Mock<ITemperature>(MockBehavior.Strict);
            temperature.Setup(t => t.AirTemperature).Returns(27.0).Verifiable();
            temperature.Setup(t => t.MinTemperature).Returns(16.2).Verifiable();

            var leafTemp = 27;
            var gbh = 0.127634;

            var rtw = 700;
            var rn = 320;

            var expected = 0.080424818708166368;

            // Act
            var water = new WaterInteraction(temperature.Object);
            water.SetConditions(gbh, rn);
            water.LeafTemp = leafTemp;
            var actual = water.HourlyWaterUse(rtw);

            // Assert
            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void Gt_WhenCalculated_ReturnsExpectedValue()
        {
            // Arrange
            var temperature = new Mock<ITemperature>(MockBehavior.Strict);
            temperature.Setup(t => t.AirMolarDensity).Returns(40.63).Verifiable();
            temperature.Setup(t => t.AtmosphericPressure).Returns(1.01325).Verifiable();

            var leafTemp = 27;
            var gbh = 0.127634;

            var rtw = 180;

            var expected = 0.1437732786549164;

            // Act
            var water = new WaterInteraction(temperature.Object);
            water.SetConditions(gbh, 0.0);
            water.LeafTemp = leafTemp;
            var actual = water.TotalCO2Conductance(rtw);

            // Assert
            Assert.That(actual, Is.EqualTo(expected));
            temperature.Verify();
        }

        [Test]
        public void Temperature_WhenCalculated_ReturnsExpectedValue()
        {
            // Arrange
            var temperature = new Mock<ITemperature>(MockBehavior.Strict);
            temperature.Setup(t => t.AirTemperature).Returns(27.0).Verifiable();
            temperature.Setup(t => t.MinTemperature).Returns(16.2).Verifiable();

            var leafTemp = 27;
            var gbh = 0.127634;

            var rtw = 700;
            var rn = 320;

            var expected = 28.732384941224293;

            // Act
            var water = new WaterInteraction(temperature.Object);
            water.SetConditions(gbh, rn);
            water.LeafTemp = leafTemp;
            var actual = water.LeafTemperature(rtw);

            // Assert
            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}
