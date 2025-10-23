using System;
using APSIM.Numerics;
using APSIM.Shared.Utilities;
using NUnit.Framework;

namespace UnitTests.APSIMShared
{
    /// <summary>
    /// Unit tests for the regression utilities.
    /// </summary>
    class SoilTests
    {
        /// <summary>Test MapInterpolation</summary>
        [Test]
        public void EnsureMapInterpolationHandlesMissingValues()
        {
            double[] fromThickness = new double[] { 50, 100, 100, 100 };
            double[] fromValues = new double[] { 1, 2, double.NaN, 4 };
            double[] newThickness = new double[] { 30, 60, 90, 120 };
            double[] newValues = SoilUtilities.MapInterpolation(fromValues, fromThickness, newThickness, allowMissingValues: true);

            Assert.That(MathUtilities.AreEqual(new double[] { 1, 1.46666666, 2.35, 3.4000000 }, newValues), Is.True);
        }

        /// <summary>Ensure metadata determined correctly when input metadata is null.</summary>
        [Test]
        public void EnsureMetadataDeterminationWorksWithNullMetadata()
        {
            double[] values1 = new double[] { 10, 20, 30 };
            string[] metadata1 = null;
            double[] values2 = new double[] { 10, 25, 30 };
            string[] metadata2 = SoilUtilities.DetermineMetadata(values1, metadata1, values2, null);

            Assert.That(metadata2, Is.EqualTo(new string[] { null, null, null}));
        }

        /// <summary>Ensure metadata determined correctly when input metadata is not null.</summary>
        [Test]
        public void EnsureMetadataDeterminationWorksWithNotNullMetadata()
        {
            double[] values1 = new double[] { 10, 20, 30 };
            string[] metadata1 = new string[] { null, "Calculated", "Calculated"};
            double[] values2 = new double[] { 10, 25, 30 };
            string[] metadata2 = SoilUtilities.DetermineMetadata(values1, metadata1, values2, null);

            Assert.That(metadata2, Is.EqualTo(new string[] { null, null, "Calculated"}));
        }

        /// <summary>Ensure metadata determined correctly when new data is added.</summary>
        [Test]
        public void EnsureMetadataDeterminationWorksWhenDataAdded()
        {
            double[] values1 = new double[] { 10, 20, 30 };
            string[] metadata1 = new string[] { null, null, null};
            double[] values2 = new double[] { 10, 20, 30, 40 };
            string[] metadata2 = SoilUtilities.DetermineMetadata(values1, metadata1, values2, null);

            Assert.That(metadata2, Is.EqualTo(new string[] { null, null, null, null}));
        }

        /// <summary>Ensure metadata determined correctly when new data is added.</summary>
        [Test]
        public void EnsureMetadataDeterminationWorksWhenDataDeleted()
        {
            double[] values1 = new double[] { 10, 20, 30 };
            string[] metadata1 = new string[] { null, null, null};
            double[] values2 = new double[] { 10, 20 };
            string[] metadata2 = SoilUtilities.DetermineMetadata(values1, metadata1, values2, null);

            Assert.That(metadata2, Is.EqualTo(new string[] { null, null}));
        }

        /// <summary>Ensure infilling works when input metadata is null.</summary>
        [Test]
        public void EnsureInFillWorksWithNullMetadata()
        {
            double[] values = new double[] { 10, double.NaN, 30 };
            string[] metadata1 = null;
            var result = SoilUtilities.FillMissingValues(values, metadata1, 3, (i) => 25);

            Assert.That(result.values, Is.EqualTo(new double[] { 10, 25, 30}));
            Assert.That(result.metadata, Is.EqualTo(new string[] { null, "Calculated", null}));
        }

        /// <summary>Ensure infilling works when input metadata is not null.</summary>
        [Test]
        public void EnsureInFillWorksWithNotNullMetadata()
        {
            double[] values = new double[] { 10, double.NaN, 30 };
            string[] metadata1 = new string[] { null, null, "Measured" };
            var result = SoilUtilities.FillMissingValues(values, metadata1, 3, (i) => 25);

            Assert.That(result.values, Is.EqualTo(new double[] { 10, 25, 30}));
            Assert.That(result.metadata, Is.EqualTo(new string[] { null, "Calculated", "Measured"}));
        }

        /// <summary>Test calculation of FASW from SoilUtilities</summary>
        [Test]
        public void TestCalcFASW()
        {
            double[] thickness = new double[] { 100, 200, 300 };
            double[] pawmm = new double[] { 10, 20, 10 };
            double[] pawcmm = new double[] { 20, 80, 100 };

            Assert.That(SoilUtilities.CalcFASW(thickness, pawmm, pawcmm, 50), Is.EqualTo(0.5));
            Assert.That(SoilUtilities.CalcFASW(thickness, pawmm, pawcmm, 100), Is.EqualTo(0.5));
            Assert.That(SoilUtilities.CalcFASW(thickness, pawmm, pawcmm, 300), Is.EqualTo(0.3));
            Assert.That(SoilUtilities.CalcFASW(thickness, pawmm, pawcmm, 600), Is.EqualTo(0.2));
            Assert.That(SoilUtilities.CalcFASW(thickness, pawmm, pawcmm, double.MaxValue), Is.EqualTo(0.2));

            Assert.Throws<Exception>(() => SoilUtilities.CalcFASW(thickness, pawmm, pawcmm, 0));

            double[] pawmmNaN = new double[] { 10, double.NaN, 10 };
            Assert.Throws<Exception>(() => SoilUtilities.CalcFASW(thickness, pawmmNaN, pawcmm, 0));

            double[] pawcmmNaN = new double[] { 20, double.NaN, 100 };
            Assert.Throws<Exception>(() => SoilUtilities.CalcFASW(thickness, pawmm, pawcmmNaN, 0));

        }
    }
}
