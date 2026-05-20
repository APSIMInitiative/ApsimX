using System;
using Models.Core;
using Models.PMF.Interfaces;
using Models.PMF;
using APSIM.Core;
using System.Collections.Generic;


namespace Models.Forestry
{
    /// <summary>
    /// Calculates the stem size distribution for a forestry model
    /// </summary>
    [Serializable]
    
    [ValidParent(ParentType = typeof(Plant))]
    [ValidParent(ParentType = typeof(IOrgan))]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]

    public class StemSizeDistribution : Model
    {
        /// <summary>Stem Population (/ha)</summary>
        [Link(Type = LinkType.Child, ByName = true)][Units("/ha")] IFunction StemPopulation = null; 
        /// <summary>Basal Area (m2/ha)</summary>
        [Link(Type = LinkType.Child, ByName = true)][Units("m^2/ha")] IFunction BasalArea = null;
        /// <summary>Mean Stem Diameter at Breast Height (cm)</summary>
        [Link(Type = LinkType.Child, ByName = true)][Units("cm")] IFunction MeanDBH = null;
        /// <summary>Wiebull Location Parameter (cm)</summary>
        [Link(Type = LinkType.Child, ByName = true)][Units("cm")] IFunction a = null;  //I think this is the DBH of the smallest stem

        /// <summary>
        /// Stores the fitted three-parameter Weibull coefficients and fit diagnostics.
        /// </summary>
        internal sealed record WeibullParams(double Shape, double Scale, double Location, int Convergence, double Objective);
        //NH make a Weibull class and put params as properties


        /// <summary>
        /// Represents one DBH class interval and the estimated number of trees per hectare assigned to it.
        /// </summary>
        internal sealed record TreeClass(double DBHLower, double DBHUpper, double DBHMid, double TreesPerHa, string StandId);

        private List<double> _DBH;
        private List<TreeClass> TreeList = new List<TreeClass>();

       /// <summary>
       /// Number of DBH size classes used in reporting.
       /// </summary>
       [Units("")]
        [Description("Number of DBH size classes used in reporting")]
        public int NumSizeClasses { get; set; }

        /// <summary>
        /// Size Class Interval.
        /// </summary>
        [Units("cm")]
        [Description("The size class interval for DBH")]
        public double SizeClassInterval { get; set; }

        /// <summary>
        /// Population in each of the DBH size classes.
        /// </summary>
        [Units("")]
        [Description("Population in each of the DBH size classes")]
        public double[] DBH 
        { 
            get 
            {
                CalculateDistributions();
                return _DBH.ToArray(); 
            } 
        }

        /// <summary>Things the model does when the simulation starts</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            

        }
        private void CalculateDistributions()
        {
                ValidateStandData();
                WeibullParams W = EstimateWeibull(StemPopulation.Value(), BasalArea.Value(), MeanDBH.Value(), a.Value());
                TreeList = GenerateTreeList(StemPopulation.Value(), W, NumSizeClasses * SizeClassInterval, SizeClassInterval, 0.9999);

                _DBH = new List<double>();
            foreach (TreeClass treeClass in TreeList) { _DBH.Add(treeClass.TreesPerHa); }
        }

            private void ValidateStandData()
        {
                if (!double.IsFinite(StemPopulation.Value()) || StemPopulation.Value() <= 0) throw new ArgumentException("All StemPopulation values must be finite and > 0.");
                if (!double.IsFinite(BasalArea.Value()) || BasalArea.Value() <= 0) throw new ArgumentException("All BasalArea values must be finite and > 0.");
                if (!double.IsFinite(MeanDBH.Value()) || MeanDBH.Value() <= 0) throw new ArgumentException("All MeanDBH values must be finite and > 0.");
                if (!double.IsFinite(a.Value()) || a.Value() < 0) throw new ArgumentException("All a values must be finite and >= 0.");
                if (MeanDBH.Value() <= a.Value()) throw new ArgumentException("Each MeanDBH must be greater than a.");
        }


        /// <summary>
        /// Converts fitted Weibull parameters into DBH classes with expected trees per hectare in each class.
        /// </summary>
        /// <param name="n">Trees per hectare to distribute across DBH classes.</param>
        /// <param name="p">The fitted Weibull parameters used to build the class distribution.</param>
        /// <param name="maxD">An optional maximum diameter cutoff for the generated classes.</param>
        /// <param name="classWidth">The DBH class width in centimeters.</param>
        /// <param name="qUpper">The upper Weibull quantile used when deriving a maximum diameter automatically.</param>
        /// <returns>A tree list containing DBH class bounds, class midpoints, and estimated trees per hectare.</returns>
        private static List<TreeClass> GenerateTreeList(double n, WeibullParams p, double? maxD, double classWidth, double qUpper)
        {
            //NH pass 3 Weibull params not the structure
            //NH rename n to stempopulation
            //NH rename wiebull params to k and lambda or vice versa
            //Externalise 80 to parameter
            // do parameter checking when they are calculated? and change ArgumentException to normal exception - CHANGE LEAVE AS NOW TO MAKE METHOD SELF CONTAINED
            // use double where we know they are doubles (not var)
            // remove double? for maxD
            // Change MaxDLocal to use the class width and number and to the calculations once at beginning - if so then Qupper may not be required

            var k = p.Shape;
            var lambda = p.Scale;
            var a = p.Location;

            if (!double.IsFinite(k) || !double.IsFinite(lambda) || !double.IsFinite(a))
                throw new ArgumentException("Invalid Weibull parameters.");
            if (k <= 0 || lambda <= 0)
                throw new ArgumentException("Shape and scale must be > 0.");
            if (!double.IsFinite(n) || n <= 0)
                throw new ArgumentException("N must be finite and > 0.");

            var maxDLocal = maxD ?? (a + WeibullQuantile(qUpper, k, lambda));
            maxDLocal = Math.Ceiling(maxDLocal / classWidth) * classWidth;
            maxDLocal = Math.Max(maxDLocal, 80);

            if (maxDLocal <= a + classWidth)
            {
                maxDLocal = a + 10 * classWidth;
            }

            var breaks = new List<double>();
            for (var b = a; b <= maxDLocal + 1e-9; b += classWidth)
            {
                breaks.Add(b);
            }

            if (breaks[^1] < maxDLocal)
            {
                breaks.Add(maxDLocal);
            }

            var output = new List<TreeClass>(breaks.Count - 1);

            for (var i = 0; i < breaks.Count - 1; i++)
            {
                var lower = breaks[i];
                var upper = breaks[i + 1];
                var mid = (lower + upper) / 2.0;

                var pLow = Weibull3Cdf(lower, k, lambda, a);
                var pHigh = Weibull3Cdf(upper, k, lambda, a);
                var prob = pHigh - pLow;

                if (!double.IsFinite(prob) || prob < 0)
                {
                    prob = 0;
                }

                output.Add(new TreeClass(lower, upper, mid, n * prob, null));
            }

            return output;
        }

        /// <summary>
        /// Computes the quantile of a two-parameter Weibull distribution used for the upper DBH cutoff.
        /// </summary>
        /// <param name="p">The cumulative probability to invert.</param>
        /// <param name="shape">The Weibull shape parameter.</param>
        /// <param name="scale">The Weibull scale parameter.</param>
        /// <returns>The diameter corresponding to the requested cumulative probability.</returns>
        private static double WeibullQuantile(double p, double shape, double scale)
        {
            if (p <= 0) return 0;
            if (p >= 1) return double.PositiveInfinity;  //NH throw instead
            return scale * Math.Pow(-Math.Log(1 - p), 1 / shape);
        }

        /// <summary>
        /// Evaluates the cumulative distribution function for a three-parameter Weibull diameter distribution.
        /// </summary>
        /// <param name="d">The diameter value at which to evaluate the CDF.</param>
        /// <param name="shape">The Weibull shape parameter.</param>
        /// <param name="scale">The Weibull scale parameter.</param>
        /// <param name="location">The Weibull location parameter.</param>
        /// <returns>The cumulative probability up to the supplied diameter.</returns>
        private static double Weibull3Cdf(double d, double shape, double scale, double location)
        {
            if (d <= location) return 0;
            var z = (d - location) / scale;
            return 1.0 - Math.Exp(-Math.Pow(z, shape));
        }

        /// <summary>
        /// Estimates the Weibull shape and scale that best reproduce the supplied stand density, basal area, and mean diameter.
        /// </summary>
        /// <param name="n">Trees per hectare for the stand.</param>
        /// <param name="g">Basal area in square meters per hectare.</param>
        /// <param name="dbar">Mean diameter at breast height in centimeters.</param>
        /// <param name="a">Weibull location parameter representing the minimum diameter offset.</param>
        /// <returns>The fitted Weibull parameters and convergence diagnostics for the stand.</returns>
        /// <summary>Estimates Weibull parameters.</summary>
        /// <returns>The estimated Weibull parameters.</returns>
        private static WeibullParams EstimateWeibull(double n, double g, double dbar, double a)
        {
            var lower = new[] { 0.05, 0.01 };
            var upper = new[] { 20.0, 300.0 };
            var x = new[] { 2.0, Math.Max(dbar - a, 0.1) };

            x[0] = Math.Clamp(x[0], lower[0], upper[0]);
            x[1] = Math.Clamp(x[1], lower[1], upper[1]);

            var best = ComputeWeibullFitError(x[0], x[1], n, g, dbar, a);
            var step = new[] { 0.8, Math.Max(1.0, x[1] * 0.2) };

            var maxIters = 4000;
            var converged = false;

            for (var iter = 0; iter < maxIters; iter++)
            {
                var improved = false;

                for (var dim = 0; dim < 2; dim++)
                {
                    foreach (var direction in new[] { -1.0, 1.0 })
                    {
                        var candidate = new[] { x[0], x[1] };
                        candidate[dim] = Math.Clamp(candidate[dim] + direction * step[dim], lower[dim], upper[dim]);

                        var value = ComputeWeibullFitError(candidate[0], candidate[1], n, g, dbar, a);
                        if (value < best)
                        {
                            x = candidate;
                            best = value;
                            improved = true;
                        }
                    }
                }

                if (!improved)
                {
                    step[0] *= 0.6;
                    step[1] *= 0.6;
                }

                if (step[0] < 1e-6 && step[1] < 1e-6)
                {
                    converged = true;
                    break;
                }
            }

            return new WeibullParams(
                x[0],
                x[1],
                a,
                converged ? 0 : 1,
                best
            );
        }

        /// <summary>
        /// Computes the normalized fitting error for a candidate Weibull parameter set against the target stand metrics.
        /// </summary>
        /// <param name="k">Candidate Weibull shape parameter.</param>
        /// <param name="lambda">Candidate Weibull scale parameter.</param>
        /// <param name="nLocal">Observed trees per hectare for the stand being fitted.</param>
        /// <param name="gLocal">Observed basal area for the stand being fitted.</param>
        /// <param name="dbarLocal">Observed mean diameter for the stand being fitted.</param>
        /// <param name="aLocal">Observed Weibull location parameter for the stand being fitted.</param>
        /// <returns>Initially called Objective from the original implementation.</returns>
        private static double ComputeWeibullFitError(double k, double lambda, double nLocal, double gLocal, double dbarLocal, double aLocal)
        {

            if (!double.IsFinite(k) || !double.IsFinite(lambda) || k <= 0 || lambda <= 0)
            {
                return 1e12;
            }

            var dPred = aLocal + lambda * Gamma(1 + 1 / k);
            var d2Pred = aLocal * aLocal
                            + 2 * aLocal * lambda * Gamma(1 + 1 / k)
                            + lambda * lambda * Gamma(1 + 2 / k);

            var gPred = nLocal * (Math.PI / 40000.0) * d2Pred;

            var err = Math.Pow((dPred - dbarLocal) / Math.Max(dbarLocal, 1), 2)
                    + Math.Pow((gPred - gLocal) / Math.Max(gLocal, 1), 2);

            return err;
        }
        /// <summary>
        /// Evaluates the gamma function using the Lanczos approximation for the Weibull moment calculations.
        /// </summary>
        /// <param name="z">The input value for the gamma function.</param>
        /// <returns>The gamma function evaluated at the supplied value.</returns>
        private static double Gamma(double z)
        {
            //NH could this be a math utility call
            var p = new[]
            {
            676.5203681218851,
            -1259.1392167224028,
            771.32342877765313,
            -176.61502916214059,
            12.507343278686905,
            -0.13857109526572012,
            9.9843695780195716e-6,
            1.5056327351493116e-7
        };

            if (z < 0.5)
            {
                return Math.PI / (Math.Sin(Math.PI * z) * Gamma(1 - z));
            }

            z -= 1;
            var x = 0.99999999999980993;
            for (var i = 0; i < p.Length; i++)
            {
                x += p[i] / (z + i + 1);
            }

            var t = z + p.Length - 0.5;
            return Math.Sqrt(2 * Math.PI) * Math.Pow(t, z + 0.5) * Math.Exp(-t) * x;
        }

    }
}