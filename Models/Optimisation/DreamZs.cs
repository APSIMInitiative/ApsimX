using System;
using System.Text;
using Models.Core;

namespace Models.Optimisation
{
    /// <summary>
    /// Encapsulates the DREAM-zs algorithm used by <see cref="CroptimizR"/>. From the CroptimizR doucmentation:
    /// 
    /// # Bayesian algorithms
    /// 
    /// In a Bayesian approach estimated parameters are treated as random variables and one seeks to determine their joint probability distribution, called the posterior distribution.The uncertainty in the estimated parameters are thus central in this approach.An advantage of the Bayesian approach is that it uses prior information about the parameters values.
    /// 
    /// The DREAM-zs algorithm is a multi-chain MCMC method which is recognized has an efficient method for complex, high-dimensional and multi-modal target distributions.It is extensively described in (Vrugt 2016). In CroptimizR, it is interfaced from the BayesianTools package(Hartig, Minunno, and Paul 2019).
    /// 
    /// It provides different types of plots and results including plots of prior and posterior densities, correlation plots, Gelman diagnostic plot, sample of posterior distribution and associated statistics.
    /// 
    /// https://sticsrpacks.github.io/CroptimizR/articles/Parameter_estimation_DREAM.html
    /// </summary>
    /// <remarks>
    /// Need to add gamma, but no idea what type it is. It's not well documented.
    /// </remarks>
    [Serializable]
    public class DreamZs : IOptimizationMethod
    {
        /// <summary>
        /// Likelihood function to be used for optimization.
        /// </summary>
        [Description("Likelihood function")]
        [Tooltip("Likelihood/critical function to be used for optimization")]
        public Likelihood CriticalFunction { get; set; } = Likelihood.LogCiidn;

        /// <summary>
        /// Number of iterations/model evaluations.
        /// </summary>
        [Description("Number of model evaluations")]
        [Tooltip("Number of model evaluations")]
        public int Iterations { get; set; } = 10000;

        /// <summary>
        /// Number of cross-over proposals.
        /// </summary>
        [Description("Number of crossover proposals.")]
        [Tooltip("If nCR = 1, all parameters are updated jointly")]
        public int Ncr { get; set; } = 3;

        /// <summary>
        /// Ergodicity term.
        /// </summary>
        /// <remarks>
        /// The CRAN documentation for this parameter is also flaky.
        /// </remarks>
        [Description("Eps")]
        [Tooltip("Ergodicity term")]
        public double Eps { get; set; } = 0;

        /// <summary>
        /// Ergodicity term.
        /// </summary>
        /// <remarks>
        /// The CRAN documentation for this parameter is also flaky.
        /// </remarks>
        [Description("E")]
        [Tooltip("Ergodicity term")]
        public double E { get; set; } = 0.05;

        /// <summary>
        /// Update of crossover probabilities.
        /// </summary>
        [Description("Update of crossover probabilities")]
        [Tooltip("Update of crossover probabilities")]
        public bool PcrUpdate { get; set; } = false;

        /// <summary>
        /// Determines the interval for the pCR (crossover probabilities) update.
        /// </summary>
        [Description("Update interval")]
        [Tooltip("Determines the interval for the pCR (crossover probabilities) update.")]
        public int UpdateInterval { get; set; } = 10;

        /// <summary>
        /// Number of intervals treated as burn-in.
        /// </summary>
        [Description("Burn-in")]
        [Tooltip("Number of iterations treated as burn-in. These iterations are not recorded in the chain.")]
        public int BurnIn { get; set; } = 0;

        /// <summary>
        /// Thinning parameter. Determines the interval in which values are recorded.
        /// </summary>
        [Description("Thinning parameter")]
        [Tooltip("Determiens the interval in which values are recorded")]
        public int Thin { get; set; } = 1;

        /// <summary>
        /// Number of percentage of samples that are used for the adaptation in DREAM.
        /// </summary>
        [Description("Adaptation")]
        [Tooltip("Number of percentage of samples that are used for the adaptation in DREAM")]
        public double Adaptation { get; set; } = 0.2;

        /// <summary>
        /// Determines whether parallel computing should be attempted.
        /// </summary>
        [Description("Parallel")]
        [Tooltip("Determines whether parallel computing should be attempted")]
        public bool Parallel { get; set; } = false;

        /// <summary>
        /// Frequency with which to update Z matrix.
        /// </summary>
        [Description("Z update frequency")]
        [Tooltip("Frequency with which to update Z matrix")]
        public int ZUpdateFrequency { get; set; } = 10;

        /// <summary>
        /// Probability of snooker update.
        /// </summary>
        [Description("Probability of snooker update")]
        [Tooltip("Probability of snooker update")]
        public double PSnooker { get; set; } = 0.1;

        /// <summary>
        /// Number of pairs used to generate proposal.
        /// </summary>
        [Description("DE pairs")]
        [Tooltip("Number of pairs used to generate proposal")]
        public int DEPairs { get; set; } = 2;

        /// <summary>
        /// Interval in which the sampling progress is printed to the console.
        /// </summary>
        [Description("Console updates")]
        [Tooltip("Interval in which the sampling progress is printed to the console")]
        public int ConsoleUpdates { get; set; } = 10;

        /// <summary>
        /// Number of markov chains.
        /// </summary>
        /// <remarks>
        /// This can be passed to R as either a matrix containing the start
        /// values, an integer to define the number of chains that are run, a
        /// function to sample the start values or NUll, in which case the
        /// values are sampled from the prior.
        /// 
        /// The only type supported in apsim for now is an int, as the number
        /// of chains.
        /// </remarks>
        [Description("Number of markov chains")]
        [Tooltip("The only value for this supported by APSIM is the number of markov chains")]
        public int StartValue { get; set; } = 3;

        /// <summary>
        /// Optimization algorithm to be used.
        /// </summary>
        public OptimizationTypeEnum Type
        {
            get
            {
                return OptimizationTypeEnum.Bayesian;
            }
        }

        /// <summary>
        /// Get the string value for the optim_param variable passed
        /// into the CroptimizR estim_param method.
        /// </summary>
        public string ROptimizerName
        {
            get
            {
                return "BayesianTools.dreamzs";
            }
        }

        /// <summary>
        /// Name of the Critical function used for optimization.
        /// </summary>
        public string CritFunction
        {
            get
            {
                switch (CriticalFunction)
                {
                    case Likelihood.LogCiidn:
                        return "CroptimizR::likelihood_log_ciidn";
                    case Likelihood.LogCiidnCorr:
                        return "CroptimizR::likelihood_log_ciidn_corr";
                    default:
                        throw new Exception($"Unsupported critical function type: {CriticalFunction}");
                }
            }
        }

        /// <summary>
        /// A method which generates R code defining a variable with
        /// optimization parameters specific to this optimization method.
        /// </summary>
        /// <param name="variableName">Name of the variable to be generated.</param>
        public string GenerateOptimizationOptions(string variableName)
        {
            StringBuilder result = new StringBuilder();
            result.AppendLine($"{variableName}=list()");

            result.AppendLine($"{variableName}$iterations <- {Iterations}");
            result.AppendLine($"{variableName}$nCR <- {Ncr}");
            result.AppendLine($"{variableName}$eps <- {Eps}");
            result.AppendLine($"{variableName}$e <- {E}");
            result.AppendLine($"{variableName}$pCRupdate <- {PcrUpdate.ToString().ToUpper()}");
            result.AppendLine($"{variableName}$updateInterval <- {UpdateInterval}");
            result.AppendLine($"{variableName}$burnin <- {BurnIn}");
            result.AppendLine($"{variableName}$thin <- {Thin}");
            result.AppendLine($"{variableName}$adaptation <- {Adaptation}");
            result.AppendLine($"{variableName}$parallel <- {Parallel.ToString().ToUpper()}");
            result.AppendLine($"{variableName}$ZupdateFrequency <- {ZUpdateFrequency}");
            result.AppendLine($"{variableName}$pSnooker <- {PSnooker}");
            result.AppendLine($"{variableName}$DEpairs <- {DEPairs}");
            result.AppendLine($"{variableName}$consoleUpdates <- {ConsoleUpdates}");
            result.AppendLine($"{variableName}$startValue <- {StartValue}");

            return result.ToString();
        }
    }
}
