using System;
using Models.Forestry;
using NUnit.Framework;
using Weibull2P = Models.Forestry.StemSizeDistribution.Weibull2P;

namespace UnitTests.Forestry
{
    /// <summary>Tests of the two-parameter Weibull maths behind the stem size distribution.</summary>
    [TestFixture]
    public class StemSizeDistributionTests
    {
        /// <summary>The scale derived from a mean and CV must reproduce that mean.</summary>
        [TestCase(12.0, 0.17)]
        [TestCase(35.0, 0.10)]
        [TestCase(5.0, 0.40)]
        public void MeanIsPreserved(double mean, double cv)
        {
            Weibull2P w = Weibull2P.FromMeanAndCV(mean, cv);
            Assert.That(w.Mean, Is.EqualTo(mean).Within(1e-9));
        }

        /// <summary>Shape and CV conversions must invert each other.</summary>
        [TestCase(1.8)]
        [TestCase(3.6)]
        [TestCase(6.823)]
        [TestCase(12.0)]
        [TestCase(30.0)]
        public void ShapeAndCVRoundTrip(double shape)
        {
            double cv = Weibull2P.CVFromShape(shape);
            Assert.That(Weibull2P.ShapeFromCV(cv), Is.EqualTo(shape).Within(1e-6));
        }

        /// <summary>The establishment shape used by PinusV2 corresponds to a CV of about 0.17.</summary>
        [Test]
        public void EstablishmentShapeCV()
        {
            Assert.That(Weibull2P.CVFromShape(6.823), Is.EqualTo(0.17).Within(0.005));
        }

        /// <summary>Quantiles are ordered and the median matches the CDF.</summary>
        [Test]
        public void QuantilesAreConsistent()
        {
            Weibull2P w = Weibull2P.FromMeanAndCV(25.0, 0.17);
            Assert.That(w.Quantile(0.1), Is.LessThan(w.Quantile(0.5)));
            Assert.That(w.Quantile(0.5), Is.LessThan(w.Quantile(0.9)));
            Assert.That(w.CDF(w.Quantile(0.5)), Is.EqualTo(0.5).Within(1e-9));
        }

        /// <summary>Random removal takes the same share of biomass as stems and leaves the CV unchanged.</summary>
        [Test]
        public void RandomRemovalIsNeutral()
        {
            Weibull2P w = Weibull2P.FromMeanAndCV(25.0, 0.17);
            var r = w.Remove(0.4, 0.0, 2.5, 400);
            Assert.That(r.BiomassFraction, Is.EqualTo(0.4).Within(1e-9));
            Assert.That(r.RetainedCV, Is.EqualTo(0.17).Within(0.002));
            Assert.That(r.RetainedMeanRatio, Is.EqualTo(1.0).Within(1e-6));
        }

        /// <summary>
        /// Thinning from below takes less biomass than stems, raises the mean and narrows the distribution.
        /// BFG 1988 (8 plots, selectivity 1.6-2.0, 35-56% of stems) removed 80-88% as much biomass as stems and cut CV
        /// by roughly a quarter.
        /// </summary>
        [Test]
        public void ThinningFromBelow()
        {
            Weibull2P w = Weibull2P.FromMeanAndCV(26.0, 0.20);
            var r = w.Remove(0.5, 1.8, 2.5, 400);
            Assert.That(r.BiomassFraction / 0.5, Is.InRange(0.70, 0.95));
            Assert.That(r.RetainedMeanRatio, Is.GreaterThan(1.0));
            Assert.That(r.RetainedCV, Is.LessThan(0.20));
        }

        /// <summary>Thinning from above takes more biomass than stems and lowers the mean.</summary>
        [Test]
        public void ThinningFromAbove()
        {
            Weibull2P w = Weibull2P.FromMeanAndCV(26.0, 0.20);
            var r = w.Remove(0.3, -2.0, 2.5, 400);
            Assert.That(r.BiomassFraction, Is.GreaterThan(0.3));
            Assert.That(r.RetainedMeanRatio, Is.LessThan(1.0));
        }

        /// <summary>The selectivity inferred from a biomass fraction reproduces that fraction.</summary>
        [TestCase(0.457, 0.33)]
        [TestCase(0.563, 0.44)]
        [TestCase(0.46, 0.44)]
        public void SelectivityInversion(double stems, double biomass)
        {
            Weibull2P w = Weibull2P.FromMeanAndCV(24.0, 0.20);
            double s = w.SelectivityForBiomassFraction(stems, biomass, 2.5, -20, 40, 400);
            Assert.That(w.Remove(stems, s, 2.5, 400).BiomassFraction, Is.EqualTo(biomass).Within(1e-4));
        }

        /// <summary>Invalid inputs are rejected rather than producing NaN.</summary>
        [Test]
        public void InvalidInputsThrow()
        {
            Assert.Throws<ArgumentException>(() => Weibull2P.ShapeFromCV(0));
            Assert.Throws<ArgumentException>(() => Weibull2P.ShapeFromCV(double.NaN));
            Assert.Throws<ArgumentException>(() => new Weibull2P(-1, 10));
        }
    }
}
