using System;
using Models.Functions;
using NUnit.Framework;

namespace UnitTests.Functions
{
    /// <summary>
    /// Tests for the germination water potential response against the expected values in the
    /// germination model specification (Germination_ChosenModel_APSIM_spec.md, section 6).
    /// </summary>
    [TestFixture]
    public class GerminatingWaterPotentialResponseTests
    {
        private const double q = 1.80845439962828;

        /// <summary>fW for (psi, psib) pairs from section 6.1 of the specification.</summary>
        [TestCase(0, -5.17634, 1)]
        [TestCase(0, -1.21740, 1)]
        [TestCase(0.2, -2.34853, 1)]
        [TestCase(-1, -4.04521, 0.5983767)]
        [TestCase(-1, -3.47965, 0.5418695)]
        [TestCase(-1, -2.34853, 0.3666725)]
        [TestCase(-1, -1.50018, 0.1371957)]
        [TestCase(-1, -1.21740, 0.0443579)]
        [TestCase(-0.5, -1.21740, 0.3842821)]
        [TestCase(-1.75, -4.04521, 0.3588443)]
        [TestCase(-2.5, -2.34853, 0)]
        public void WaterFactor_MatchesSpecIntermediateValues(double psi, double psiBase, double expected)
        {
            Assert.That(GerminatingWaterPotentialResponse.WaterFactorAt(psi, psiBase, q), Is.EqualTo(expected).Within(1e-4));
        }

        /// <summary>fW for the daily run with a dry spell in section 6.3 of the specification.</summary>
        [TestCase(-0.8, -3.93210, 0.66274)]
        [TestCase(-1.5, -3.81899, 0.40569)]
        [TestCase(-3.0, -4.04521, 0.08652)]
        [TestCase(-4.0, -3.70588, 0)]
        [TestCase(-4.5, -3.59276, 0)]
        [TestCase(-3.8, -3.81899, 0.00007)]
        [TestCase(-1.2, -3.93210, 0.51765)]
        [TestCase(-0.6, -3.70588, 0.72657)]
        [TestCase(-0.4, -3.47965, 0.80184)]
        [TestCase(-0.3, -3.59276, 0.85412)]
        public void WaterFactor_MatchesSpecDrySpell(double psi, double psiBase, double expected)
        {
            Assert.That(GerminatingWaterPotentialResponse.WaterFactorAt(psi, psiBase, q), Is.EqualTo(expected).Within(1e-5));
        }

        /// <summary>
        /// f(T) used by GerminatingPhaseHydroThermal: WangEngelTempFunction.ValueIndexed with the specification's
        /// fitted parameters, against f(T) in sections 6.1 and 6.3 of the specification.
        /// </summary>
        [TestCase(-5, 0)]
        [TestCase(0, 0.0189005)]
        [TestCase(5, 0.1083175)]
        [TestCase(6, 0.13631)]
        [TestCase(7, 0.16768)]
        [TestCase(8, 0.20236)]
        [TestCase(9, 0.24021)]
        [TestCase(10, 0.2810608)]
        [TestCase(20, 0.7786991)]
        [TestCase(27.5, 1)]
        [TestCase(30, 0.9615306)]
        [TestCase(35, 0.5762290)]
        [TestCase(38.5, 0)]
        public void WangEngelTemperatureFactor_MatchesSpec(double temperature, double expected)
        {
            var wangEngel = new WangEngelTempFunction
            {
                MinTemp = -4.54318125758749,
                OptTemp = 27.5,
                MaxTemp = 38.3125484756731,
                RefTemp = 27.5,
                Beta = 1
            };
            Assert.That(wangEngel.ValueIndexed(temperature), Is.EqualTo(expected).Within(1e-5));
        }

        [Test]
        public void WaterFactor_IsZeroWhenBaseWaterPotentialIsNotNegative()
        {
            Assert.That(GerminatingWaterPotentialResponse.WaterFactorAt(-0.5, 0, q), Is.EqualTo(0));
        }

        [Test]
        public void PsiFromTheta_ReturnsAnchorValues()
        {
            double airDry = 0.05, ll15 = 0.10, dul = 0.30, sat = 0.45;
            Assert.That(GerminatingWaterPotentialResponse.PsiFromTheta(airDry, airDry, ll15, dul, sat), Is.EqualTo(-100).Within(1e-9));
            Assert.That(GerminatingWaterPotentialResponse.PsiFromTheta(ll15, airDry, ll15, dul, sat), Is.EqualTo(-1.5).Within(1e-9));
            Assert.That(GerminatingWaterPotentialResponse.PsiFromTheta(dul, airDry, ll15, dul, sat), Is.EqualTo(-0.033).Within(1e-9));
            Assert.That(GerminatingWaterPotentialResponse.PsiFromTheta(sat, airDry, ll15, dul, sat), Is.EqualTo(-0.001).Within(1e-9));
        }

        [Test]
        public void PsiFromTheta_InterpolatesLog10BetweenAnchors()
        {
            double airDry = 0.05, ll15 = 0.10, dul = 0.30, sat = 0.45;
            double theta = (ll15 + dul) / 2;
            double expected = -Math.Pow(10, (Math.Log10(1.5) + Math.Log10(0.033)) / 2);
            Assert.That(GerminatingWaterPotentialResponse.PsiFromTheta(theta, airDry, ll15, dul, sat), Is.EqualTo(expected).Within(1e-9));
        }

        [Test]
        public void PsiFromTheta_IsHeldConstantBeyondEndAnchors()
        {
            double airDry = 0.05, ll15 = 0.10, dul = 0.30, sat = 0.45;
            Assert.That(GerminatingWaterPotentialResponse.PsiFromTheta(0.50, airDry, ll15, dul, sat), Is.EqualTo(-0.001).Within(1e-9));
            Assert.That(GerminatingWaterPotentialResponse.PsiFromTheta(0.01, airDry, ll15, dul, sat), Is.EqualTo(-100).Within(1e-9));
        }

        [Test]
        public void PsiFromTheta_DropsAirDryAnchorWhenNotBelowLL15()
        {
            double ll15 = 0.10, dul = 0.30, sat = 0.45;
            Assert.That(GerminatingWaterPotentialResponse.PsiFromTheta(0.01, 0.10, ll15, dul, sat), Is.EqualTo(-1.5).Within(1e-9));
            Assert.That(GerminatingWaterPotentialResponse.PsiFromTheta(0.01, 0.12, ll15, dul, sat), Is.EqualTo(-1.5).Within(1e-9));
        }
    }
}
