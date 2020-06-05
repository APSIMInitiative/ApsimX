using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.Sensitivity
{
    /// <summary>
    /// Likelihood functions used as the crit function to estimate parameters
    /// using bayesian methods via CroptimizR.
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
