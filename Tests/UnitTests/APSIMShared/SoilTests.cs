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

            Assert.IsTrue(MathUtilities.AreEqual(new double[] { 1, 1.46666666, 2.35, 3.4000000 }, newValues));
        }
    }
}
