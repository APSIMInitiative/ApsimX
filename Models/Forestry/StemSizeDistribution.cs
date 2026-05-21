using System;
using Models.Core;
using Models.PMF.Interfaces;
using Models.PMF;
using APSIM.Core;
using System.Collections.Generic;
using MathNet.Numerics;

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

        private WeibullModel Weibull = null;

        internal class WeibullModel
        {
            public double Shape;
            public double Scale;
            public double Location;
            public int Convergence;
            public double Objective;

            public WeibullModel (double shape, double scale, double location, int convergence, double objective)
            { 
                Shape = shape;
                Scale = scale;
                Location = location;
                Convergence = convergence;
                Objective = objective;
            }
            public WeibullModel()
            {
                Shape = 0.0;
                Scale = 0.0;
                Location = 0.0;
                Convergence = 0;
                Objective = 0.0;
            }
            /// <summary>
            /// Evaluates the cumulative distribution function for a three-parameter Weibull diameter distribution.
            /// </summary>
            /// <param name="d">The diameter value at which to evaluate the CDF.</param>
            /// <returns>The cumulative probability up to the supplied diameter.</returns>
            public double CDF(double d)
            {
                if (d <= Location) return 0;
                var z = (d - Location) / Scale;
                return 1.0 - Math.Exp(-Math.Pow(z, Shape));
            }

            /// <summary>
            /// Estimates the Weibull shape and scale that best reproduce the supplied stand density, basal area, and mean diameter.
            /// </summary>
            /// <param name="stemPopulation">Trees per hectare for the stand.</param>
            /// <param name="basalArea">Basal area in square meters per hectare.</param>
            /// <param name="meanDBH">Mean diameter at breast height in centimeters.</param>
            /// <param name="a">Weibull location parameter representing the minimum diameter offset.</param>
            /// <returns>The fitted Weibull parameters and convergence diagnostics for the stand.</returns>
            /// <summary>Estimates Weibull parameters.</summary>
            /// <returns>The estimated Weibull parameters.</returns>
            public void Estimate(double stemPopulation, double basalArea, double meanDBH, double a)
            {
                var lower = new[] { 0.05, 0.01 };
                var upper = new[] { 20.0, 300.0 };
                var x = new[] { 2.0, Math.Max(meanDBH - a, 0.1) };

                x[0] = Math.Clamp(x[0], lower[0], upper[0]);
                x[1] = Math.Clamp(x[1], lower[1], upper[1]);

                var best = FitError(x[0], x[1], stemPopulation, basalArea, meanDBH, a);
                var step = new[] { 0.8, Math.Max(1.0, x[1] * 0.2) };

                var maxIters = 4000;
                var converged = false;

                for (var iter = 0; iter < maxIters; iter++)
                {
                    var improved = false;

                    for (var dim = 0; dim < 2; dim++)
                        foreach (var direction in new[] { -1.0, 1.0 })
                        {
                            var candidate = new[] { x[0], x[1] };
                            candidate[dim] = Math.Clamp(candidate[dim] + direction * step[dim], lower[dim], upper[dim]);

                            var value = FitError(candidate[0], candidate[1], stemPopulation, basalArea, meanDBH, a);
                            if (value < best)
                            {
                                x = candidate;
                                best = value;
                                improved = true;
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

                
                Shape = x[0];
                Scale = x[1];
                Location = a;
                Convergence=converged?0:1;
                Objective = best;


            }
            /// <summary>
            /// Computes the quantile of a two-parameter Weibull distribution used for the upper DBH cutoff.
            /// </summary>
            /// <param name="p">The cumulative probability to invert.</param>
            /// <returns>The diameter corresponding to the requested cumulative probability.</returns>
            public double Quantile(double p)
            {
                if (p < 0) throw new ArgumentException("Probability in Weibull Quantile method is less than zero");
                if (p == 0) return 0;
                if (p >= 1) throw new ArgumentException("Probability in Weibull Quantile method is equal to or greater than 1.");
                return Scale * Math.Pow(-Math.Log(1 - p), 1 / Shape);
            }


            /// <summary>
            /// Computes the normalized fitting error for a candidate Weibull parameter set against the target stand metrics.
            /// </summary>
            /// <param name="shape">Candidate Weibull shape parameter.</param>
            /// <param name="scale">Candidate Weibull scale parameter.</param>
            /// <param name="stemPopulation">Observed trees per hectare for the stand being fitted.</param>
            /// <param name="basalArea">Observed basal area for the stand being fitted.</param>
            /// <param name="meanDBH">Observed mean diameter for the stand being fitted.</param>
            /// <param name="location">Observed Weibull location parameter for the stand being fitted.</param>
            /// <returns>Initially called Objective from the original implementation.</returns>
            private static double FitError(double shape, double scale, double stemPopulation, double basalArea, double meanDBH, double location)
            {

                if (!double.IsFinite(shape))
                    throw new ArgumentException("Value of shape parameter is infinite in method FitError");
                if (!double.IsFinite(scale))
                    throw new ArgumentException("Value of scale parameter is infinite in method FitError");
                if (shape <= 0)
                    throw new ArgumentException("Value of shape parameter is zero or less than zero in method FitError");
                if (scale <= 0)
                    throw new ArgumentException("Value of scale parameter is zero or less than zero in method FitError");


                var dPred = location + scale * SpecialFunctions.Gamma(1 + 1 / shape);
                var d2Pred = location * location
                                + 2 * location * scale * SpecialFunctions.Gamma(1 + 1 / shape)
                                + scale * scale * SpecialFunctions.Gamma(1 + 2 / shape);

                var gPred = stemPopulation * (Math.PI / 40000.0) * d2Pred;

                var err = Math.Pow((dPred - meanDBH) / Math.Max(meanDBH, 1), 2)
                        + Math.Pow((gPred - basalArea) / Math.Max(basalArea, 1), 2);

                return err;
            }
        }

        /// <summary>
        /// Represents one DBH class interval and the estimated number of trees per hectare assigned to it.
        /// </summary>
        internal sealed record TreeClass(double DBHLower, double DBHUpper, double DBHMid, double TreesPerHa, string StandId);
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
                if (TreeList.Count == 0)
                   CalculateDistributions();
                List<double> _DBH = new List<double>();
                foreach (TreeClass treeClass in TreeList) { _DBH.Add(treeClass.TreesPerHa); }
                return _DBH.ToArray(); 
            } 
        }

        /// <summary>
        /// DBH at 50% Quantile.
        /// </summary>
        [Units("")]
        [Description("DBH at 50% Quantile")]
        public double DBHQ50
        {
            get
            {
                if (TreeList.Count == 0)
                    CalculateDistributions();
                return Weibull.Quantile(0.5);
            }
        }

        /// <summary>
        /// DBH at 10% Quantile.
        /// </summary>
        [Units("")]
        [Description("DBH at 10% Quantile")]
        public double DBHQ10
        {
            get
            {
                if (TreeList.Count == 0)
                    CalculateDistributions();
                return Weibull.Quantile(0.1);
            }
        }

        /// <summary>
        /// DBH at 90% Quantile.
        /// </summary>
        [Units("")]
        [Description("DBH at 90% Quantile")]
        public double DBHQ90
        {
            get
            {
                if (TreeList.Count == 0)
                    CalculateDistributions();
                return Weibull.Quantile(0.9);
            }
        }

        /// <summary>
        /// Midpoint of each of the DBH size class.
        /// </summary>
        [Units("")]
        [Description("Midpoint of each of the DBH size class")]
        public double[] DBHmid
        {
            get
            {
                if (TreeList.Count == 0)
                    CalculateDistributions();
                List<double> _DBHmid = new List<double>();
                foreach (TreeClass treeClass in TreeList) { _DBHmid.Add(treeClass.DBHMid); }
                return _DBHmid.ToArray();
            }
        }

        /// <summary>Things the model does when the simulation starts</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            TreeList.Clear();
        }
        private void CalculateDistributions()
        {
            //ValidateStandData();

            Weibull = new WeibullModel();
            Weibull.Estimate(StemPopulation.Value(), BasalArea.Value(), MeanDBH.Value(), a.Value());
            
            TreeList = GenerateTreeList(StemPopulation.Value(), Weibull, NumSizeClasses, SizeClassInterval, 0.9999);

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
        /// <param name="stemPopulation">Trees per hectare to distribute across DBH classes.</param>
        /// <param name="wiebull">The fitted Weibull parameters used to build the class distribution.</param>
        /// <param name="numSizeClasses">The number of generated size classes.</param>
        /// <param name="classWidth">The DBH class width in centimeters.</param>
        /// <param name="qUpper">The upper Weibull quantile used when deriving a maximum diameter automatically.</param>
        /// <returns>A tree list containing DBH class bounds, class midpoints, and estimated trees per hectare.</returns>
        private static List<TreeClass> GenerateTreeList(double stemPopulation, WeibullModel wiebull, int numSizeClasses, double classWidth, double qUpper)
        {
            List<TreeClass> output = new List<TreeClass>(numSizeClasses);

            double lower = 0;
            for (var i = 0; i < numSizeClasses; i++)
            {
                double upper = lower + classWidth;
                double mid = (lower + upper) / 2.0;
                double prob = wiebull.CDF(upper) - wiebull.CDF(lower);

                if (!double.IsFinite(prob) || prob < 0)
                    prob = 0;

                output.Add(new TreeClass(lower, upper, mid, stemPopulation * prob, null));
                lower += classWidth;
            }

            return output;
        }






    }
}