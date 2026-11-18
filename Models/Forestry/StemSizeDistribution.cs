using System;
using System.Collections.Generic;
using APSIM.Core;
using MathNet.Numerics;
using Models.Core;
using Models.Functions;
using Models.PMF;
using Models.PMF.Interfaces;

namespace Models.Forestry
{
    /// <summary>
    /// Stem diameter (DBH) distribution of an even-aged stand, represented as a two-parameter Weibull.
    ///
    /// The distribution is a state, not a daily re-fit. Its relative spread (coefficient of variation, CV) is
    /// carried from day to day, and the Weibull scale is always derived from the stand's mean DBH so the mean is
    /// preserved exactly: scale = MeanDBH / Gamma(1 + 1/shape). The shape follows from the CV.
    ///
    /// The CV changes in three ways:
    /// 1. Establishment: set from EstablishmentShape the first time the stand has a mean DBH.
    /// 2. Growth: if diameter increment scales with D^alpha across trees, ln(CV) changes by (alpha - 1) x ln(mean DBH ratio).
    ///    GrowthAsymmetry supplies (alpha - 1). Positive values (size-asymmetric competition in crowded stands) widen the
    ///    distribution; zero keeps the relative spread constant; negative values let small trees catch up.
    /// 3. Removal (thinning or mortality): stems are removed with probability proportional to (D / mean)^-selectivity,
    ///    where selectivity &gt; 0 removes small trees first (thinning from below), 0 is random and &lt; 0 removes large trees
    ///    first. The retained trees give the new CV. The share of stem biomass removed uses mass proportional to
    ///    D^SizeMassExponent.
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Plant))]
    [ValidParent(ParentType = typeof(IOrgan))]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    public class StemSizeDistribution : Model
    {
        /// <summary>Stem population (/ha).</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("/ha")]
        private IFunction StemPopulation = null;

        /// <summary>Arithmetic mean stem diameter at breast height (cm).</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("cm")]
        private IFunction MeanDBH = null;

        /// <summary>Weibull shape when the stand is first given a mean DBH.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("-")]
        private IFunction EstablishmentShape = null;

        /// <summary>Growth asymmetry (alpha - 1): exponent of the diameter-increment-to-size relationship, minus 1.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("-")]
        private IFunction GrowthAsymmetry = null;

        /// <summary>Exponent relating tree stem mass to DBH, used to convert stem removals to biomass removals.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("-")]
        private IFunction SizeMassExponent = null;

        /// <summary>Lowest coefficient of variation allowed (Weibull shape about 40).</summary>
        private const double MinimumCV = 0.03;

        /// <summary>Highest coefficient of variation allowed (Weibull shape about 1.6).</summary>
        private const double MaximumCV = 0.65;

        /// <summary>Number of quantile points used to integrate over the distribution.</summary>
        private const int QuadraturePoints = 400;

        private double cv;
        private double previousMeanDBH;
        private bool initialised;
        private bool resynchronise;
        private List<TreeSizeClass> sizeClasses = new List<TreeSizeClass>();

        /// <summary>One DBH class and the trees per hectare in it.</summary>
        internal sealed record TreeSizeClass(double DBHLower, double DBHUpper, double DBHMid, double TreesPerHa);

        /// <summary>Number of DBH size classes reported. The last class is open-ended.</summary>
        [Units("")]
        [Description("Number of DBH size classes reported (the last class is open-ended)")]
        public int SizeClassNumber { get; set; }

        /// <summary>Width of each DBH size class.</summary>
        [Units("cm")]
        [Description("The size class interval for DBH")]
        public double SizeClassInterval { get; set; }

        /// <summary>
        /// Largest size selectivity (either direction) applied to a removal. Selectivities inferred from stand-level stem
        /// and biomass fractions can be implausibly strong when those fractions come from different measurements; at BFG
        /// the 1988 thinning had tree-measured selectivities of 1.6-2.0 (one common value, 1.83, fits all seven plots
        /// thinned from below, with no random component), while plot-level volume fractions implied 3-7.
        /// </summary>
        [Units("-")]
        [Description("Largest size selectivity applied to a removal (BFG 1988 thinning from below: 1.83)")]
        public double MaximumRemovalSelectivity { get; set; } = 1.83;

        /// <summary>Coefficient of variation of DBH.</summary>
        [Units("-")]
        public double CoefficientOfVariation => HasDistribution ? cv : 0.0;

        /// <summary>Weibull shape parameter.</summary>
        [Units("-")]
        public double WeibullShape => HasDistribution ? Weibull2P.ShapeFromCV(cv) : 0.0;

        /// <summary>Weibull scale parameter.</summary>
        [Units("cm")]
        public double WeibullScale => HasDistribution ? CurrentDistribution().Scale : 0.0;

        /// <summary>Quadratic mean DBH implied by the distribution.</summary>
        [Units("cm")]
        public double QuadraticMeanDBH => HasDistribution ? MeanDBH.Value() * Math.Sqrt(1.0 + cv * cv) : 0.0;

        /// <summary>Basal area implied by the distribution (uses the quadratic mean DBH).</summary>
        [Units("m^2/ha")]
        public double BasalArea => HasDistribution ? Math.PI / 40000.0 * Math.Pow(QuadraticMeanDBH, 2) * StemPopulation.Value() : 0.0;

        /// <summary>Reineke stand density index, N x (Dq / 25.4 cm)^1.605, in stems/ha.</summary>
        [Units("/ha")]
        public double StandDensityIndex => HasDistribution ? StemPopulation.Value() * Math.Pow(QuadraticMeanDBH / 25.4, 1.605) : 0.0;

        /// <summary>DBH at the 10th percentile.</summary>
        [Units("cm")]
        public double DBH10thPercentile => HasDistribution ? CurrentDistribution().Quantile(0.1) : 0.0;

        /// <summary>DBH at the 50th percentile.</summary>
        [Units("cm")]
        public double DBH50thPercentile => HasDistribution ? CurrentDistribution().Quantile(0.5) : 0.0;

        /// <summary>DBH at the 90th percentile.</summary>
        [Units("cm")]
        public double DBH90thPercentile => HasDistribution ? CurrentDistribution().Quantile(0.9) : 0.0;

        /// <summary>Trees per hectare in each DBH size class.</summary>
        [Units("/ha")]
        public double[] Population
        {
            get
            {
                List<double> values = new List<double>();
                foreach (TreeSizeClass sizeClass in SizeClasses())
                    values.Add(sizeClass.TreesPerHa);
                return values.ToArray();
            }
        }

        /// <summary>Midpoint of each DBH size class.</summary>
        [Units("cm")]
        public double[] DBHmid
        {
            get
            {
                List<double> values = new List<double>();
                foreach (TreeSizeClass sizeClass in SizeClasses())
                    values.Add(sizeClass.DBHMid);
                return values.ToArray();
            }
        }

        /// <summary>Selectivity of the most recent removal (positive = from below).</summary>
        [Units("-")]
        public double LastRemovalSelectivity { get; private set; }

        /// <summary>Stem biomass fraction of the most recent removal.</summary>
        [Units("0-1")]
        public double LastRemovalBiomassFraction { get; private set; }

        /// <summary>Retained-to-original mean DBH ratio of the most recent removal.</summary>
        [Units("-")]
        public double LastRemovalMeanDBHRatio { get; private set; }

        /// <summary>True when the stand has stems and a positive mean DBH.</summary>
        private bool HasDistribution => initialised && MeanDBH.Value() > 0 && StemPopulation.Value() > 0;

        /// <summary>
        /// Remove a fraction of stems with a given size selectivity, update the distribution to the retained trees and
        /// return the fraction of stem biomass removed. Call this before the plant's population and biomass are reduced.
        /// </summary>
        /// <param name="stemFraction">Fraction of stems removed (0-1).</param>
        /// <param name="selectivity">Size selectivity: &gt; 0 from below, 0 random, &lt; 0 from above.</param>
        /// <returns>Fraction of stem biomass removed (0-1).</returns>
        public double RemoveBySize(double stemFraction, double selectivity)
        {
            EnsureInitialised();
            if (!HasDistribution || stemFraction <= 0)
                return Math.Max(0, stemFraction);
            if (stemFraction >= 1)
                return 1.0;

            selectivity = Math.Clamp(selectivity, -MaximumRemovalSelectivity, MaximumRemovalSelectivity);
            Weibull2P distribution = CurrentDistribution();
            RemovalResult result = distribution.Remove(stemFraction, selectivity, SizeMassExponent.Value(), QuadraturePoints);
            ApplyRemovalResult(result, selectivity);
            return result.BiomassFraction;
        }

        /// <summary>
        /// Update the distribution after a removal whose stem and biomass fractions are both known. The size selectivity
        /// that reproduces the biomass fraction is inferred and the retained-tree CV is applied. Call this before the
        /// plant's population and biomass are reduced.
        /// </summary>
        /// <param name="stemFraction">Fraction of stems removed (0-1).</param>
        /// <param name="biomassFraction">Fraction of stem biomass removed (0-1).</param>
        public void ApplyRemoval(double stemFraction, double biomassFraction)
        {
            EnsureInitialised();
            resynchronise = true;
            if (!HasDistribution || stemFraction <= 0 || stemFraction >= 1)
                return;

            Weibull2P distribution = CurrentDistribution();
            double massExponent = SizeMassExponent.Value();
            double selectivity = distribution.SelectivityForBiomassFraction(stemFraction, biomassFraction, massExponent,
                                                                            -MaximumRemovalSelectivity, MaximumRemovalSelectivity, QuadraturePoints);
            RemovalResult result = distribution.Remove(stemFraction, selectivity, massExponent, QuadraturePoints);
            ApplyRemovalResult(result, selectivity);
        }

        [EventSubscribe("Commencing")]
        private void OnCommencing(object sender, EventArgs e)
        {
            cv = 0;
            previousMeanDBH = 0;
            initialised = false;
            resynchronise = false;
            sizeClasses.Clear();
            LastRemovalSelectivity = 0;
            LastRemovalBiomassFraction = 0;
            LastRemovalMeanDBHRatio = 1;
        }

        [EventSubscribe("DoDailyInitialisation")]
        private void OnDoDailyInitialisation(object sender, EventArgs e)
        {
            sizeClasses.Clear();
        }

        [EventSubscribe("EndOfDay")]
        private void OnEndOfDay(object sender, EventArgs e)
        {
            sizeClasses.Clear();
            double mean = MeanDBH.Value();
            if (mean <= 0 || StemPopulation.Value() <= 0)
            {
                initialised = false;
                previousMeanDBH = 0;
                return;
            }

            if (!initialised)
            {
                EnsureInitialised();
                return;
            }

            if (resynchronise || previousMeanDBH <= 0)
            {
                // Mean DBH jumped because of a removal, not growth: start growth tracking again from today.
                resynchronise = false;
                previousMeanDBH = mean;
                return;
            }

            if (mean > previousMeanDBH)
            {
                double asymmetry = GrowthAsymmetry.Value();
                cv = Math.Clamp(cv * Math.Pow(mean / previousMeanDBH, asymmetry), MinimumCV, MaximumCV);
            }
            previousMeanDBH = mean;
        }

        private void EnsureInitialised()
        {
            if (initialised)
                return;
            double mean = MeanDBH.Value();
            if (mean <= 0 || StemPopulation.Value() <= 0)
                return;
            double shape = EstablishmentShape.Value();
            if (!double.IsFinite(shape) || shape <= 0)
                throw new Exception($"{Name}: EstablishmentShape must be a positive number but is {shape}.");
            cv = Math.Clamp(Weibull2P.CVFromShape(shape), MinimumCV, MaximumCV);
            previousMeanDBH = mean;
            initialised = true;
        }

        private void ApplyRemovalResult(RemovalResult result, double selectivity)
        {
            cv = Math.Clamp(result.RetainedCV, MinimumCV, MaximumCV);
            LastRemovalSelectivity = selectivity;
            LastRemovalBiomassFraction = result.BiomassFraction;
            LastRemovalMeanDBHRatio = result.RetainedMeanRatio;
            resynchronise = true;
            sizeClasses.Clear();
        }

        private Weibull2P CurrentDistribution()
        {
            return Weibull2P.FromMeanAndCV(MeanDBH.Value(), cv);
        }

        private List<TreeSizeClass> SizeClasses()
        {
            if (sizeClasses.Count == 0 && HasDistribution && SizeClassNumber > 0 && SizeClassInterval > 0)
            {
                Weibull2P distribution = CurrentDistribution();
                double stems = StemPopulation.Value();
                double lower = 0;
                for (int i = 0; i < SizeClassNumber; i++)
                {
                    bool last = i == SizeClassNumber - 1;
                    double upper = lower + SizeClassInterval;
                    double probability = (last ? 1.0 : distribution.CDF(upper)) - distribution.CDF(lower);
                    if (!double.IsFinite(probability) || probability < 0)
                        probability = 0;
                    sizeClasses.Add(new TreeSizeClass(lower, upper, (lower + upper) / 2.0, stems * probability));
                    lower = upper;
                }
            }
            return sizeClasses;
        }

        /// <summary>Outcome of removing stems from a distribution.</summary>
        internal readonly record struct RemovalResult(double BiomassFraction, double RetainedCV, double RetainedMeanRatio);

        /// <summary>Two-parameter Weibull distribution of DBH, with the moment and removal maths used by the model.</summary>
        internal readonly struct Weibull2P
        {
            /// <summary>Shape parameter.</summary>
            public double Shape { get; }

            /// <summary>Scale parameter (cm).</summary>
            public double Scale { get; }

            /// <summary>Create from shape and scale.</summary>
            public Weibull2P(double shape, double scale)
            {
                if (!double.IsFinite(shape) || shape <= 0)
                    throw new ArgumentException($"Weibull shape must be positive and finite but is {shape}.");
                if (!double.IsFinite(scale) || scale < 0)
                    throw new ArgumentException($"Weibull scale must be non-negative and finite but is {scale}.");
                Shape = shape;
                Scale = scale;
            }

            /// <summary>Create the distribution with a given arithmetic mean and coefficient of variation.</summary>
            public static Weibull2P FromMeanAndCV(double mean, double cv)
            {
                double shape = ShapeFromCV(cv);
                return new Weibull2P(shape, mean / SpecialFunctions.Gamma(1.0 + 1.0 / shape));
            }

            /// <summary>Mean of the distribution.</summary>
            public double Mean => Scale * SpecialFunctions.Gamma(1.0 + 1.0 / Shape);

            /// <summary>Coefficient of variation implied by a shape.</summary>
            public static double CVFromShape(double shape)
            {
                double g1 = SpecialFunctions.Gamma(1.0 + 1.0 / shape);
                double g2 = SpecialFunctions.Gamma(1.0 + 2.0 / shape);
                return Math.Sqrt(Math.Max(0.0, g2 / (g1 * g1) - 1.0));
            }

            /// <summary>Shape implied by a coefficient of variation (CV decreases monotonically with shape).</summary>
            public static double ShapeFromCV(double cv)
            {
                if (!double.IsFinite(cv) || cv <= 0)
                    throw new ArgumentException($"Coefficient of variation must be positive and finite but is {cv}.");
                double low = 0.2, high = 200.0;
                if (cv >= CVFromShape(low)) return low;
                if (cv <= CVFromShape(high)) return high;
                for (int i = 0; i < 100; i++)
                {
                    double mid = 0.5 * (low + high);
                    if (CVFromShape(mid) > cv) low = mid; else high = mid;
                    if (high - low < 1e-9) break;
                }
                return 0.5 * (low + high);
            }

            /// <summary>Cumulative distribution function.</summary>
            public double CDF(double d)
            {
                if (d <= 0 || Scale <= 0) return 0.0;
                return 1.0 - Math.Exp(-Math.Pow(d / Scale, Shape));
            }

            /// <summary>Quantile function.</summary>
            public double Quantile(double p)
            {
                if (p < 0 || p >= 1) throw new ArgumentException($"Probability must be in [0, 1) but is {p}.");
                if (p == 0) return 0.0;
                return Scale * Math.Pow(-Math.Log(1.0 - p), 1.0 / Shape);
            }

            /// <summary>Diameters at equally spaced probability midpoints, used to integrate over the distribution.</summary>
            public double[] QuadratureDiameters(int points)
            {
                double[] d = new double[points];
                for (int j = 0; j < points; j++)
                    d[j] = Quantile((j + 0.5) / points);
                return d;
            }

            /// <summary>
            /// Remove a fraction of stems with removal probability min(1, c x (D/mean)^-selectivity), with c set so the
            /// expected fraction removed equals stemFraction.
            /// </summary>
            public RemovalResult Remove(double stemFraction, double selectivity, double massExponent, int points)
            {
                double[] d = QuadratureDiameters(points);
                // Use the quadrature's own mean so a random removal leaves the mean ratio at exactly 1.
                double mean = 0;
                foreach (double dj in d)
                    mean += dj;
                mean /= points;
                double[] weight = new double[points];
                for (int j = 0; j < points; j++)
                    weight[j] = Math.Pow(d[j] / mean, -selectivity);
                double c = SolveRemovalConstant(weight, stemFraction);

                double massTotal = 0, massRemoved = 0, keptN = 0, keptD = 0, keptD2 = 0;
                for (int j = 0; j < points; j++)
                {
                    double p = Math.Min(1.0, c * weight[j]);
                    double mass = Math.Pow(d[j], massExponent);
                    massTotal += mass;
                    massRemoved += p * mass;
                    double kept = 1.0 - p;
                    keptN += kept;
                    keptD += kept * d[j];
                    keptD2 += kept * d[j] * d[j];
                }
                double biomassFraction = massTotal > 0 ? massRemoved / massTotal : stemFraction;
                if (keptN <= 1e-12)
                    return new RemovalResult(1.0, CVFromShape(Shape), 1.0);
                double keptMean = keptD / keptN;
                double keptVariance = Math.Max(0.0, keptD2 / keptN - keptMean * keptMean);
                double keptCV = Math.Sqrt(keptVariance) / keptMean;
                // The quadrature slightly under-represents the tails, so its CV is ~0.1-0.2% below the true CV. Apply only
                // the change in CV caused by the removal; otherwise every removal (even a random one) would ratchet the CV
                // down, and frequent small mortality removals would narrow the distribution steadily.
                double allVariance = 0;
                foreach (double dj in d)
                    allVariance += (dj - mean) * (dj - mean);
                double quadratureCV = Math.Sqrt(allVariance / points) / mean;
                double retainedCV = quadratureCV > 0 ? CVFromShape(Shape) * keptCV / quadratureCV : keptCV;
                return new RemovalResult(biomassFraction, retainedCV, keptMean / mean);
            }

            /// <summary>Find the selectivity whose removal gives the requested biomass fraction.</summary>
            public double SelectivityForBiomassFraction(double stemFraction, double biomassFraction, double massExponent,
                                                        double minSelectivity, double maxSelectivity, int points)
            {
                // Biomass fraction removed falls as selectivity rises (more small trees taken first).
                double atMin = Remove(stemFraction, minSelectivity, massExponent, points).BiomassFraction;
                double atMax = Remove(stemFraction, maxSelectivity, massExponent, points).BiomassFraction;
                if (biomassFraction >= atMin) return minSelectivity;
                if (biomassFraction <= atMax) return maxSelectivity;
                double low = minSelectivity, high = maxSelectivity;
                for (int i = 0; i < 80; i++)
                {
                    double mid = 0.5 * (low + high);
                    if (Remove(stemFraction, mid, massExponent, points).BiomassFraction > biomassFraction) low = mid; else high = mid;
                    if (high - low < 1e-6) break;
                }
                return 0.5 * (low + high);
            }

            private static double SolveRemovalConstant(double[] weight, double stemFraction)
            {
                double ExpectedFraction(double c)
                {
                    double sum = 0;
                    foreach (double w in weight)
                        sum += Math.Min(1.0, c * w);
                    return sum / weight.Length;
                }
                double low = 0.0, high = 1.0;
                while (ExpectedFraction(high) < stemFraction && high < 1e300)
                    high *= 2.0;
                for (int i = 0; i < 200; i++)
                {
                    double mid = 0.5 * (low + high);
                    if (ExpectedFraction(mid) < stemFraction) low = mid; else high = mid;
                    if (high - low <= 1e-14 * Math.Max(1.0, high)) break;
                }
                return 0.5 * (low + high);
            }
        }
    }
}
