using System;
using System.Linq;
using Models.Agroforestry;
using NUnit.Framework;

namespace UnitTests.Agroforestry
{
    [TestFixture]
    public class CrownGeometryHelperTests
    {
        [Test]
        public void ComputeCrownDimensions_ScalesHeightByConfiguredRatios()
        {
            CrownGeometryHelper.CrownDimensions dimensions = CrownGeometryHelper.ComputeCrownDimensions(
                height: 3.0,
                widthHeightRatio: 0.5,
                depthHeightRatio: 0.25);

            Assert.That(dimensions.Width, Is.EqualTo(1.5).Within(1e-12));
            Assert.That(dimensions.Depth, Is.EqualTo(0.75).Within(1e-12));
        }

        [Test]
        public void CalculateProjectedArea_ReturnsEllipseFootprint()
        {
            double projectedArea = CrownGeometryHelper.CalculateProjectedArea(width: 4.0, depth: 2.0);

            Assert.That(projectedArea, Is.EqualTo(2.0 * System.Math.PI).Within(1e-12));
        }

        [TestCase(0.0, 2.0)]
        [TestCase(2.0, 0.0)]
        public void CalculateProjectedArea_ReturnsZeroWhenWidthOrDepthIsZero(double width, double depth)
        {
            double projectedArea = CrownGeometryHelper.CalculateProjectedArea(width, depth);

            Assert.That(projectedArea, Is.EqualTo(0.0).Within(1e-12));
        }

        [TestCase(CrownGeometryHelper.CrownShape.Ellipsoid, 2.0 * System.Math.PI)]
        [TestCase(CrownGeometryHelper.CrownShape.Cone, System.Math.PI)]
        [TestCase(CrownGeometryHelper.CrownShape.Paraboloid, 1.5 * System.Math.PI)]
        public void CalculateVolume_ReturnsExpectedShapeSpecificVolume(
            CrownGeometryHelper.CrownShape shape,
            double expectedVolume)
        {
            double volume = CrownGeometryHelper.CalculateVolume(
                width: 2.0,
                depth: 2.0,
                height: 3.0,
                shape: shape);

            Assert.That(volume, Is.EqualTo(expectedVolume).Within(1e-12));
        }

        [TestCase(0.0, 2.0, 3.0)]
        [TestCase(2.0, 0.0, 3.0)]
        [TestCase(2.0, 2.0, 0.0)]
        public void CalculateVolume_ReturnsZeroWhenAnyDimensionIsZero(double width, double depth, double height)
        {
            foreach (CrownGeometryHelper.CrownShape shape in Enum.GetValues(typeof(CrownGeometryHelper.CrownShape)))
            {
                double volume = CrownGeometryHelper.CalculateVolume(width, depth, height, shape);

                Assert.That(volume, Is.EqualTo(0.0).Within(1e-12));
            }
        }

        [Test]
        public void CalculateVolume_ThrowsForUnsupportedCrownShape()
        {
            Assert.Throws<ArgumentException>(() => CrownGeometryHelper.CalculateVolume(
                width: 2.0,
                depth: 2.0,
                height: 3.0,
                shape: (CrownGeometryHelper.CrownShape)999));
        }

        [Test]
        public void ComputeLayerFractions_UniformDistribution_ReturnsEqualFractionsSummingToOne()
        {
            double[] fractions = CrownGeometryHelper.ComputeLayerFractions(
                layerCount: 4,
                distributionType: CrownGeometryHelper.DensityDistribution.Uniform);

            Assert.That(fractions, Has.Length.EqualTo(4));
            Assert.That(fractions.Sum(), Is.EqualTo(1.0).Within(1e-12));
            Assert.That(fractions.All(f => f > 0.0), Is.True);
            Assert.That(fractions.All(f => System.Math.Abs(f - 0.25) < 1e-12), Is.True);
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void ComputeLayerFractions_ThrowsForLayerCountLessThanOne(int layerCount)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CrownGeometryHelper.ComputeLayerFractions(
                layerCount,
                CrownGeometryHelper.DensityDistribution.Uniform));
        }

        [Test]
        public void ComputeLayerFractions_AllDistributionsReturnFiniteNonNegativeFractions()
        {
            foreach (CrownGeometryHelper.DensityDistribution distribution in Enum.GetValues(typeof(CrownGeometryHelper.DensityDistribution)))
            {
                double[] fractions = CrownGeometryHelper.ComputeLayerFractions(
                    layerCount: 5,
                    distributionType: distribution);

                Assert.That(fractions, Has.Length.EqualTo(5));
                Assert.That(fractions.All(f => double.IsFinite(f)), Is.True);
                Assert.That(fractions.All(f => f >= 0.0), Is.True);
                Assert.That(fractions.Sum(), Is.EqualTo(1.0).Within(1e-12));
            }
        }

        [Test]
        public void ComputeLayerFractions_BottomHeavyDistribution_DecreasesWithHeight()
        {
            double[] fractions = CrownGeometryHelper.ComputeLayerFractions(
                layerCount: 4,
                distributionType: CrownGeometryHelper.DensityDistribution.BottomHeavy);

            Assert.That(fractions.Sum(), Is.EqualTo(1.0).Within(1e-12));
            Assert.That(fractions[0], Is.GreaterThan(fractions[1]));
            Assert.That(fractions[1], Is.GreaterThan(fractions[2]));
            Assert.That(fractions[2], Is.GreaterThan(fractions[3]));
        }

        [Test]
        public void ComputeLayerFractions_TopHeavyDistribution_IncreasesWithHeight()
        {
            double[] fractions = CrownGeometryHelper.ComputeLayerFractions(
                layerCount: 4,
                distributionType: CrownGeometryHelper.DensityDistribution.TopHeavy);

            Assert.That(fractions.Sum(), Is.EqualTo(1.0).Within(1e-12));
            Assert.That(fractions[0], Is.LessThan(fractions[1]));
            Assert.That(fractions[1], Is.LessThan(fractions[2]));
            Assert.That(fractions[2], Is.LessThan(fractions[3]));
        }

        [Test]
        public void ComputeLayerFractions_MidPeakDistribution_PeaksInMiddleLayers()
        {
            double[] fractions = CrownGeometryHelper.ComputeLayerFractions(
                layerCount: 4,
                distributionType: CrownGeometryHelper.DensityDistribution.MidPeak);

            Assert.That(fractions.Sum(), Is.EqualTo(1.0).Within(1e-12));
            Assert.That(fractions[1], Is.EqualTo(fractions[2]).Within(1e-12));
            Assert.That(fractions[0], Is.EqualTo(fractions[3]).Within(1e-12));
            Assert.That(fractions[1], Is.GreaterThan(fractions[0]));
        }

        [Test]
        public void GetLayerSegments_UniformDistributionProducesContiguousLayersWithExpectedMetadata()
        {
            var segments = CrownGeometryHelper.GetLayerSegments(
                height: 4.0,
                width: 2.0,
                depth: 1.0,
                layerCount: 4,
                shape: CrownGeometryHelper.CrownShape.Cone,
                distributionType: CrownGeometryHelper.DensityDistribution.Uniform,
                extinctionCoefficient: 0.7);

            Assert.That(segments, Has.Count.EqualTo(4));

            for (int i = 0; i < segments.Count; i++)
            {
                Assert.That(segments[i].Bottom, Is.EqualTo(i).Within(1e-12));
                Assert.That(segments[i].Top, Is.EqualTo(i + 1.0).Within(1e-12));
                Assert.That(segments[i].Volume, Is.EqualTo(segments[i].ProjectedArea).Within(1e-12));
                Assert.That(segments[i].ExtinctionCoefficient, Is.EqualTo(0.7).Within(1e-12));
            }

            Assert.That(segments[0].ProjectedArea, Is.GreaterThan(segments[1].ProjectedArea));
            Assert.That(segments[1].ProjectedArea, Is.GreaterThan(segments[2].ProjectedArea));
            Assert.That(segments[2].ProjectedArea, Is.GreaterThan(segments[3].ProjectedArea));
            Assert.That(segments[^1].Top, Is.EqualTo(4.0).Within(1e-12));
        }

        [TestCase(0.0)]
        [TestCase(-1.0)]
        public void GetLayerSegments_ThrowsForNonPositiveHeight(double height)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CrownGeometryHelper.GetLayerSegments(
                height: height,
                width: 2.0,
                depth: 1.0,
                layerCount: 4,
                shape: CrownGeometryHelper.CrownShape.Ellipsoid,
                distributionType: CrownGeometryHelper.DensityDistribution.Uniform,
                extinctionCoefficient: 0.7));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void GetLayerSegments_ThrowsForNonPositiveLayerCount(int layerCount)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CrownGeometryHelper.GetLayerSegments(
                height: 4.0,
                width: 2.0,
                depth: 1.0,
                layerCount: layerCount,
                shape: CrownGeometryHelper.CrownShape.Ellipsoid,
                distributionType: CrownGeometryHelper.DensityDistribution.Uniform,
                extinctionCoefficient: 0.7));
        }

        [Test]
        public void GetLayerSegments_AllDistributionsProduceContiguousFiniteLayers()
        {
            const double Height = 4.0;

            foreach (CrownGeometryHelper.DensityDistribution distribution in Enum.GetValues(typeof(CrownGeometryHelper.DensityDistribution)))
            {
                var segments = CrownGeometryHelper.GetLayerSegments(
                    height: Height,
                    width: 2.0,
                    depth: 1.0,
                    layerCount: 5,
                    shape: CrownGeometryHelper.CrownShape.Ellipsoid,
                    distributionType: distribution,
                    extinctionCoefficient: 0.7);

                Assert.That(segments, Has.Count.EqualTo(5));
                Assert.That(segments[0].Bottom, Is.EqualTo(0.0).Within(1e-12));
                Assert.That(segments[^1].Top, Is.EqualTo(Height).Within(1e-12));

                for (int i = 0; i < segments.Count; i++)
                {
                    if (i > 0)
                        Assert.That(segments[i].Bottom, Is.EqualTo(segments[i - 1].Top).Within(1e-12));

                    Assert.That(double.IsFinite(segments[i].Bottom), Is.True);
                    Assert.That(double.IsFinite(segments[i].Top), Is.True);
                    Assert.That(double.IsFinite(segments[i].ProjectedArea), Is.True);
                    Assert.That(double.IsFinite(segments[i].Volume), Is.True);
                    Assert.That(segments[i].ProjectedArea, Is.GreaterThanOrEqualTo(0.0));
                    Assert.That(segments[i].Volume, Is.GreaterThanOrEqualTo(0.0));
                }
            }
        }

        [TestCase(CrownGeometryHelper.CrownShape.Ellipsoid)]
        [TestCase(CrownGeometryHelper.CrownShape.Cone)]
        [TestCase(CrownGeometryHelper.CrownShape.Paraboloid)]
        public void GetLayerSegments_ProjectAreaDeclinesWithHeightForSupportedShapes(CrownGeometryHelper.CrownShape shape)
        {
            var segments = CrownGeometryHelper.GetLayerSegments(
                height: 4.0,
                width: 2.0,
                depth: 1.0,
                layerCount: 5,
                shape: shape,
                distributionType: CrownGeometryHelper.DensityDistribution.Uniform,
                extinctionCoefficient: 0.7);

            for (int i = 1; i < segments.Count; i++)
                Assert.That(segments[i].ProjectedArea, Is.LessThanOrEqualTo(segments[i - 1].ProjectedArea));
        }
    }
}
