using System;
using APSIM.Shared.Utilities;
using MathNet.Numerics;
using MathNet.Numerics.LinearRegression;

namespace Models.Utilities
{
    /// <summary>
    /// A collection of Regression utilities.
    /// </summary>
    public class RegressionUtilities
    {
        /// <summary>
        /// Performs an exponential regression.
        /// </summary>
        /// <param name="x">x data</param>
        /// <param name="y">y data</param>
        /// <param name="method">Regression method to use.</param>
        public static double[] ExponentialFit(double[] x, double[] y, DirectRegressionMethod method = DirectRegressionMethod.QR)
        {
            double[] yHat = Generate.Map(y, Math.Log);
            double[] pHat = Fit.LinearCombination(x, yHat, method, t => 1.0, t => t);
            double[] coeffs = { Math.Exp(pHat[0]), pHat[1] };
            return Generate.Map(x, xi => coeffs[0] * Math.Exp(coeffs[1] * xi));
        }

        /// <summary>
        /// Gets stats for an exponential regression.
        /// </summary>
        /// <param name="x">x data</param>
        /// <param name="y">y data</param>
        public static MathUtilities.RegrStats ExponentialFitStats(double[] x, double[] y)
        {
            return MathUtilities.CalcRegressionStats("test", y, ExponentialFit(x, y));
        }

        /// <summary>
        /// Performs a polynomial regression.
        /// </summary>
        /// <param name="x">x data</param>
        /// <param name="y">y data</param>
        /// <param name="n">Degree of polynomial.</param>
        /// <returns></returns>
        public static double[] PolyFit(double[] x, double[] y, int n)
        {
            double[] coeffs = Fit.Polynomial(x, y, n);
            return Generate.Map(x, xi =>
            {
                double term = 0.0;
                for (int i = 0; i < coeffs.Length; i++)
                    term += coeffs[i] * Math.Pow(xi, i);
                return term;
            });
        }

        /// <summary>
        /// Gets stats for a polynomial regression.
        /// </summary>
        /// <param name="x">x data</param>
        /// <param name="y">y data</param>
        /// <param name="n">Degree of polynomial.</param>
        public static MathUtilities.RegrStats PolyFitStats(double[] x, double[] y, int n)
        {
            return MathUtilities.CalcRegressionStats("test", y, PolyFit(x, y, n));
        }
    }
}
