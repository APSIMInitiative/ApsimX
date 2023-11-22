using System;
using System.Text;
using Models.Core;

namespace Models.Optimisation
{
    /// <summary>
    /// Encapsulates the simplex algorithm used by <see cref="CroptimizR"/>.
    /// 
    /// From the CroptimizR documentation:
    /// 
    /// # Frequentist algorithms
    ///
    /// Frequentist methods involve minimizing a goodness-of-fit criterion.Crop models often have discontinuities that make it difficult to use gradient-based minimization methods such as Gauss–Newton or Levenberg–Marquardt.A common approach, adopted in CroptimizR, is thus to use the Nelder–Mead simplex algorithm, which is adapted to non-smooth functions because the search of the optimum is not based on the computation of the function’s gradient. Although few theoretical results are available (Lagarias et al. 1998), this algorithm is very popular because it can be used for multidimensional minimization for essentially any function.
    /// 
    /// The simplex algorithm used in CroptimizR is interfaced from the nloptr package(Johnson, n.d.) but a multi-start feature has been implemented in addition.Indeed, as it is a local optimization method, CroptimizR proposes to repeat automatically the minimization from different starting parameter values to minimize the risk of converging to a local minimum.The user specifies the number of repetitions and possibly the starting parameter values(if not provided, they are sampled within the parameters’ bounds.To help analyze the behavior of the algorithm, CroptimizR generates plots of final estimated parameter values versus initial values.
    /// 
    /// A simple example of application of the method is provided in this vignette.
    /// </summary>
    [Serializable]
    public class Simplex : IOptimizationMethod
    {
        /// <summary>
        /// Critical function to be used for optimization.
        /// </summary>
        [Description("Critical function")]
        [Tooltip("Critical function which to be minimized")]
        public Criteria CriticalFunction { get; set; } = Criteria.CritLogCwss;

        /// <summary>
        /// Number of times we run the minimisation with different parameters.
        /// </summary>
        [Description("Number of repetitions")]
        [Tooltip("Number of times the we run the minimsation with different parameters.")]
        public int NoRepetitions { get; set; } = 3;

        /// <summary>
        /// Tolerance criterion between two iterations.
        /// </summary>
        [Description("Tolerance criterion between two iterations")]
        [Tooltip("Iterations will cease if the parameters are changing by less than this amount.")]
        public double Tolerance { get; set; } = 1e-5;

        /// <summary>
        /// Maximum number of iterations executed by the optimisation algorithm.
        /// </summary>
        [Description("Max number of iterations")]
        [Tooltip("Maximum number of iterations executed by the optimisation algorithm.")]
        public int MaxEval { get; set; } = 2;

        /// <summary>
        /// Nelder-Meade simplex method implemented in the nloptr package.
        /// https://sticsrpacks.github.io/CroptimizR/articles/Parameter_estimation_simple_case.html
        /// </summary>
        public OptimizationTypeEnum Type
        {
            get
            {
                return OptimizationTypeEnum.Simplex;
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
                return "nloptr.simplex";
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
                    case Criteria.CritLogCwss:
                        return "CroptimizR::crit_log_cwss";
                    case Criteria.CritLogCwssCorr:
                        return "CroptimizR::crit_log_cwss_corr";
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
            result.AppendLine($"{variableName}$nb_rep <- {NoRepetitions}");
            result.AppendLine($"{variableName}$xtol_rel <- {Tolerance}");
            result.Append($"{variableName}$maxeval <- {MaxEval}");
            return result.ToString();
        }
    }
}
