using Models.Aqua;
using System;
using System.Collections.Generic;
using System.Text;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// Object to manage measures that have an expected (potential) and actual (realised) value
    /// </summary>
    [Serializable]
    public class ExpectedActualContainer
    {
        /// <summary>
        /// Expected or potential value required
        /// </summary>
        public double Expected { get; set; } = 0;

        /// <summary>
        /// The actual value achieved
        /// </summary>
        public double Actual { get; set; } = 0;

        /// <summary>
        /// Get the proportion of expected achieved
        /// </summary>
        public double ProportionAchieved { get { return (Expected == 0)?0:Actual / Expected; } }

        /// <summary>
        /// Determine the amount still required to attain expected
        /// </summary>
        public double Required { get { return Math.Max(0, Expected - Actual); } }

        /// <summary>
        /// Clear values
        /// </summary>
        public void Reset()
        {
            Expected = 0;
            Actual = 0;
        }

        /// <summary>
        /// Adjust all amounts to change rate
        /// </summary>
        /// <param name="factor"></param>
        public void AdjustAmounts(double factor)
        {
            Expected *= factor;
            Actual *= factor;
        }
    }
}
