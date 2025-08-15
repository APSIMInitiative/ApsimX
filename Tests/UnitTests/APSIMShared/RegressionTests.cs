using System;
using System.Linq;
using NUnit.Framework;
using Models.Utilities;
using APSIM.Shared.Utilities;
using System.Globalization;
using APSIM.Numerics;

namespace UnitTests.APSIMShared
{
    /// <summary>
    /// Unit tests for the regression utilities.
    /// </summary>
    class RegressionTests
    {
        private static readonly double[] x = Enumerable.Range(0, 32).Select(t => Convert.ToDouble(t, CultureInfo.InvariantCulture)).ToArray();

        /// <summary>
        /// Test for polynomial regression utility.
        /// </summary>
        [Test]
        public void TestPolyFit()
        {
            double[] xPoly5 = x.Select(xi => Math.Pow(xi, 5)).ToArray();
            MathUtilities.RegrStats stats = RegressionUtilities.PolyFitStats(x, xPoly5, 5);
            Assert.That(MathUtilities.FloatsAreEqual(stats.R2, 1.0), "Polynomial regression test has failed. r2=" + stats.R2);
        }

        /// <summary>
        /// Test for exp regression utility.
        /// </summary>
        [Test]
        public void TestExponentialFit()
        {
            double[] expX = x.Select(xi => 0.5 * Math.Exp(xi * 2.0)).ToArray();
            MathUtilities.RegrStats stats = RegressionUtilities.ExponentialFitStats(x, expX);
            Assert.That(MathUtilities.FloatsAreEqual(stats.R2, 1.0), "Exponential regression test has failed. r2=" + stats.R2);
        }
    }
}
