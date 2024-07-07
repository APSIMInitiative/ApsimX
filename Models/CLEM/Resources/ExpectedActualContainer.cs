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
        /// Maximum expected or potential value required, before condition and lactation factors.
        /// </summary>
        public double MaximumExpected { get; set; } = 0;

        /// <summary>
        /// The actual value achieved
        /// </summary>
        public double Actual { get { return Received - Unneeded;  } }

        /// <summary>
        /// The amount received from feeding
        /// </summary>
        public double Received { get; set; } = 0;

        /// <summary>
        /// The amount not needed by the individual based on feed quality etc
        /// </summary>
        public double Unneeded { get; set; } = 0;

        /// <summary>
        /// Get the proportion of expected achieved
        /// </summary>
        public double ProportionAchieved { get { return (Expected == 0)?0:Actual / Expected; } }

        /// <summary>
        /// Determine the amount still required to attain expected
        /// </summary>
        public double Required { get { return Math.Max(0, Expected - Actual + Unneeded); } }

        /// <summary>
        /// Expected or potential value required over specified time period
        /// </summary>
        public double ExpectedForTimeStep(int days = 1)
        {
            return Expected * days;
        }

        /// <summary>
        /// Actual value required over specified time period
        /// </summary>
        public double ActualForTimeStep(int days = 1)
        {
            return Actual * days;
        }

        /// <summary>
        /// Daily amount still required over specified time period
        /// </summary>
        public double RequiredForTimeStep(int days = 1)
        {
            return Required * days;
        }

        /// <summary>
        /// Clear values
        /// </summary>
        /// <param name="isSuckling">Determine if suckling and therefore not to reset Expected value provided by mother</param>
        public void Reset(bool isSuckling = false)
        {
            if(!isSuckling)
                Expected = 0;
            Received = 0;
            Unneeded = 0;
        }
    }
}
