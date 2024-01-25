namespace Models.Optimisation
{
    /// <summary>
    /// Likelihood functions used as the crit function to estimate parameters
    /// using bayesian methods via CroptimizR.
    /// 
    /// https://sticsrpacks.github.io/CroptimizR/reference/Likelihoods.html
    /// </summary>
    public enum Likelihood
    {
        /// <summary>
        /// Log transformation of concentrated version of iid normal likelihood.
        /// </summary>
        LogCiidn,

        /// <summary>
        /// Log transformation of concentrated version of iid normal likelihood
        /// but with hypothesis of high correlation between errors for different
        /// measurements over time
        /// </summary>
        LogCiidnCorr,
    }
}
