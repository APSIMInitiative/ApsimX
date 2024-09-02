using System;
using System.Collections.Generic;
using NUnit.Framework;
using APSIM.Shared.Utilities;

namespace UnitTests.APSIMShared
{
    /// <summary>
    /// Unit tests for statistical test suite.
    /// Comparison values were generated using equivilent functions in R.
    /// </summary>
    [TestFixture]
    public class RegrStatsTests
    {
        static List<double[]> X = new List<double[]>();
        static List<double[]> Y = new List<double[]>();
        List<MathUtilities.RegrStats> stats = new List<MathUtilities.RegrStats>();

        // expected outputs - see TestStatsSource.R
        static double[] Slope =       { 1, 0.6472,    0.89455,      1, -0.09504,   7624,       252.80,     double.NaN, 0 };
        static double[] Intercept =   { 0, 71.7165,   21.36885,     0, 230.16707,  -118123,    140,        double.NaN, 1.5};
        static double[] SEintercept = { 0, 21.3700,   4.75055,      0, 55.73383,   38009,      208.66,     double.NaN, 1.118034 };
        static double[] SEslope =     { 0, 0.108129,  0.038270674,  0, 0.1630292,  1297.238,   17.63498,   double.NaN, 0.7071068 };
        static double[] R2 =          { 1, 0.7049,    0.8613,       1, 0.01456,    0.4184,     0.8405,     double.NaN, 0};
        static double[] RMSE =        { 0, 18.69386,  23.55145,     0, 326.6446,   186375.5,   3254.854,   0,          0.7071068 };
        static double[] NSE =         { 1, 0.6921534, 0.8309859,    1, -0.8313643, -166798611, -75670.94,  double.NaN, -1 };
        static double[] ME =          { 0, 3.017647,  9.756556,     0, -34.9368,   76251.47,   140 ,       0,          0};
        static double[] MAE =         { 0, 14.04118,  16.63544,     0, 242.7488,   76251.47,   2151.22,    0,          0.5 };
        static double[] RSR =         { 0, 0.5382731, 0.4088229,    0, 1.325937,   12785.25,   271.7099,   double.NaN, 1.224745 };

        [SetUp]
        public void Setup()
        {
            X.Clear();
            Y.Clear();

            // simple test
            X.Add(new double[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });
            Y.Add(new double[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 });

            // actual data - small
            X.Add(new double[] { 246.3, 228.3, 181.7, 169.4, 170.2, 172.2, 163.4, 156.7, 159.3, 156.6, 217.9, 175.5, 265.4, 233.8, 203.3, 186.2, 224.4 });
            Y.Add(new double[] { 243.8, 193.8, 179.8, 177.7, 215.2, 192.7, 191.6, 176.9, 169.2, 168.0, 194.3, 170.0, 261.9, 222.2, 196.8, 187.7, 220.3 });

            //actual data - large
            X.Add(new double[] { 127.14, 93.24, 67.74, 49.44, 40.86, 31.97, 24.66, 16.48, 10.86, 8.75, 7.86, 105.04, 57.95, 48.56, 29.30, 131.30, 115.02, 102.29, 74.14, 59.46, 45.36, 32.63, 18.94, 12.93, 11.08, 9.05, 56.48, 32.40, 38.56, 49.27, 174.27, 161.40, 158.37, 123.78, 112.62, 93.18, 75.48, 55.16, 37.24, 32.97, 22.04, 109.99, 86.03, 101.59, 104.40, 185.71, 176.96, 175.12, 152.27, 151.87, 142.40, 133.77, 123.86, 111.26, 114.40, 100.63, 182.92, 163.29, 169.17, 183.58, 185.62, 177.89, 174.24, 155.99, 157.41, 150.96, 144.01, 132.22, 120.82, 125.53, 114.20, 179.51, 164.13, 162.40, 170.51, 190.24, 182.05, 178.10, 159.18, 160.71, 154.59, 145.42, 136.15, 124.35, 128.24, 119.20, 179.51, 167.95, 167.68, 177.54 });
            Y.Add(new double[] { 136.12, 117.44, 99.14, 72.32, 59.69, 37.96, 19.39, 6.06, 1.47, 2.50, 0.42, 52.99, 40.59, 43.71, 53.33, 154.99, 155.97, 151.49, 131.85, 129.85, 106.14, 69.66, 34.00, 16.88, 14.58, 8.51, 80.31, 67.11, 75.72, 121.65, 176.61, 171.45, 165.34, 149.64, 144.66, 127.01, 106.31, 80.08, 57.27, 46.75, 33.77, 105.80, 92.83, 101.17, 147.20, 186.34, 178.90, 172.08, 159.61, 156.48, 144.15, 132.15, 120.84, 114.41, 113.48, 108.23, 162.26, 150.66, 154.53, 171.67, 186.34, 178.90, 172.08, 159.61, 156.48, 144.15, 132.15, 120.84, 114.41, 113.48, 109.91, 163.81, 153.29, 157.45, 168.07, 192.45, 192.57, 195.08, 192.13, 192.50, 191.19, 189.60, 171.32, 140.59, 126.22, 116.57, 169.88, 158.88, 162.67, 172.79 });

            // minimal sample size
            X.Add(new double[] { 0, 1, 2, 3});
            Y.Add(new double[] { 0, 1, 2, 3 });

            // random sample
            X.Add(new double[] { 0.13, 342.52, 540.40, 426.49, 247.70, 0.00, 191.90, 573.72, 81.27, 37.93, 193.70, 76.18, 735.26, 178.23, 95.70, 762.93, 118.37, 31.35, 15.39, 98.49, 645.01, 478.97, 119.66, 59.43, 1.66 });
            Y.Add(new double[] { 0.86, 124.14, 6.59, 609.92, 272.22, 232.50, 183.71, 326.95, 303.37, 311.03, 116.34, 77.23, 79.16, 197.10, 600.43, 75.01, 326.15, 0.06, 218.99, 695.35, 51.26, 206.28, 23.92, 10.36, 130.04 });

            // large values
            X.Add(new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50 });
            Y.Add(new double[] { 6.36, 8.08, 10.27, 13.06, 16.60, 21.10, 26.83, 34.10, 43.36, 55.12, 70.07, 89.07, 113.23, 143.95, 182.99, 232.63, 295.73, 375.94, 477.92, 607.55, 772.35, 981.85, 1248.18, 1586.74, 2017.14, 2564.29, 3259.85, 4144.09, 5268.17, 6697.15, 8513.75, 10823.10, 13758.86, 17490.93, 22235.33, 28266.65, 35933.95, 45681.01, 58071.94, 73823.91, 93848.58, 119304.93, 151666.29, 192805.64, 245104.01, 311588.26, 396106.31, 503549.81, 640137.27, 813773.96 });

            // negative values
            X.Add(new double[] { -20, -19, -18, -17, -16, -15, -14, -13, -12, -11, -10, -9, -8, -7, -6, -5, -4, -3, -2, -1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 });
            Y.Add(new double[] { -7620.00, -6517.00, -5526.00, -4641.00, -3856.00, -3165.00, -2562.00, -2041.00, -1596.00, -1221.00, -910.00, -657.00, -456.00, -301.00, -186.00, -105.00, -52.00, -21.00, -6.00, -1.00, 0.00, 3.00, 14.00, 39.00, 84.00, 155.00, 258.00, 399.00, 584.00, 819.00, 1110.00, 1463.00, 1884.00, 2379.00, 2954.00, 3615.00, 4368.00, 5219.00, 6174.00, 7239.00, 8420.00 });

            // *********  edge tests ************
            // single pair
            X.Add(new double[] { 0 });
            Y.Add(new double[] { 0 });

            // completly uncorrelated data
            X.Add(new double[] { 1,1,2,2 });
            Y.Add(new double[] { 1,2,1,2 });

            for (int i = 0; i < X.Count; i++)
                stats.Add(MathUtilities.CalcRegressionStats("Test", Y[i], X[i]));
        }

        [Test]
        public void TestN()
        {
            for (int i = 0; i < X.Count; i++)
                Assert.That(stats[i].n, Is.EqualTo(X[i].Length));
        }

        [Test]
        public void TestSlope()
        {
            for (int i = 0; i < X.Count; i++)
                Assert.That(stats[i].Slope, Is.EqualTo(Slope[i]).Within(Math.Abs(Slope[i] * 0.0001)));
        }

        [Test]
        public void TestIntercept()
        {
            for (int i = 0; i < X.Count; i++)
                Assert.That(stats[i].Intercept, Is.EqualTo(Intercept[i]).Within(Math.Abs(Intercept[i] * 0.0001)));
        }

        [Test]
        public void TestSEintercept()
        {
            for (int i = 0; i < X.Count; i++)
                Assert.That(stats[i].SEintercept, Is.EqualTo(SEintercept[i]).Within(Math.Abs(SEintercept[i] * 0.0001)));
        }

        [Test]
        public void TestSEslope()
        {
            for (int i = 0; i < X.Count; i++)
                Assert.That(stats[i].SEslope, Is.EqualTo(SEslope[i]).Within(Math.Abs(SEslope[i] * 0.0001)));
        }

        [Test]
        public void TestR2()
        {
            for (int i = 0; i < X.Count; i++)
                Assert.That(stats[i].R2, Is.EqualTo(R2[i]).Within(Math.Abs(R2[i] * 0.001)));
        }

        [Test]
        public void TestRMSE()
        {
            for (int i = 0; i < X.Count; i++)
                Assert.That(stats[i].RMSE, Is.EqualTo(RMSE[i]).Within(Math.Abs(RMSE[i] * 0.0001)));
        }

        [Test]
        public void TestNSE()
        {
            for (int i = 0; i < X.Count; i++)
                Assert.That(stats[i].NSE, Is.EqualTo(NSE[i]).Within(Math.Abs(NSE[i] * 0.0001)));
        }

        [Test]
        public void TestME()
        {
            for (int i = 0; i < X.Count; i++)
                Assert.That(stats[i].ME, Is.EqualTo(ME[i]).Within(Math.Abs(ME[i] * 0.0001)));
        }

        [Test]
        public void TestMAE()
        {
            for (int i = 0; i < X.Count; i++)
                Assert.That(stats[i].MAE, Is.EqualTo(MAE[i]).Within(Math.Abs(MAE[i] * 0.0001)));
        }

        [Test]
        public void TestRSR()
        {
            for (int i = 0; i < X.Count; i++)
                Assert.That(stats[i].RSR, Is.EqualTo(RSR[i]).Within(Math.Abs(RSR[i] * 0.0001)));
        }
    }
}
