// -----------------------------------------------------------------------
// GrazPlan Supplement model
// -----------------------------------------------------------------------
using Models.Core;

namespace Models.GrazPlan
{
    /// <summary>
    /// Paddock and amount eaten
    /// </summary>
    public class SuppEatenType
    {
        /// <summary>
        /// Gets or sets the paddock name.
        /// </summary>
        /// <value>
        /// The paddock name.
        /// </value>
        [Units("-")]
        public string Paddock { get; set; }

        /// <summary>
        /// Gets or sets the amount of ration eaten (kg).
        /// </summary>
        /// <value>
        /// The amount of ration eaten (kg).
        /// </value>
        [Units("kg")]
        public double Eaten { get; set; }
    }
}
