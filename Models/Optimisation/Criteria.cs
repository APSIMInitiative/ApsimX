using Models.Core;

namespace Models.Optimisation
{
    /// <summary>
    /// Criteria used to estimate parameters by minimizing the difference
    /// between the observed and simulated values of model output variables.
    /// 
    /// Used when optimizing with one of CroptimizR's frequentist algorithms.
    /// 
    /// https://sticsrpacks.github.io/CroptimizR/reference/ls_criteria.html
    /// </summary>
    /// <remarks>
    /// - Should this be an interface?
    /// - This will need to be updated as more options are added to CroptimizR.
    /// </remarks>
    public enum Criteria
    {
        /// <summary>
        /// Log transformation of concentrated version of weighted sum of squares.
        /// </summary>
        [Description("Log transformation of concentrated weighted sum of squares")]
        CritLogCwss,

        /// <summary>
        /// Log transformation of concentrated version of weighted sum of
        /// squares with hypothesis of high correlation between errors for
        /// different measurements over time.
        /// </summary>
        [Description("Log transformation of concentrated weighted sum of squares, with hypothesis of high correlation between errors for different measurements over time")]
        CritLogCwssCorr,
    }
}
