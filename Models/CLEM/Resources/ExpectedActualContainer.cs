using System;
using System.Collections.Generic;
using System.Text;

namespace Models.CLEM.Resources
{
    /// <summary>
    /// Object to manage measures that have an expected (potential) and actual (realised) value
    /// </summary>
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
        /// Clear values
        /// </summary>
        public void Reset() 
        {
            Expected = 0;
            Actual = 0;
        }

        /// <summary>
        /// Get the proportion of expected achieved
        /// </summary>
        public double ProportionAchieved { get { return Actual / Expected; } }
    }
}
