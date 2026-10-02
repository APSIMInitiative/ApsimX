// -----------------------------------------------------------------------
// GrazPlan Supplement model
// -----------------------------------------------------------------------
using Models.Core;

namespace Models.GrazPlan
{
    /// <summary>
    /// Mix an amount of supplement
    /// </summary>
    public class MixSuppType
    {
        /// <summary>
        /// Gets or sets the source supplement name.
        /// </summary>
        /// <value>
        /// The source supplement name.
        /// </value>
        [Units("-")]
        public string Source { get; set; }

        /// <summary>
        /// Gets or sets the amount of supplement transferred (kg).
        /// </summary>
        /// <value>
        /// The amount of supplement transferred (kg).
        /// </value>
        [Units("kg")]
        public double Amount { get; set; }

        /// <summary>
        /// Gets or sets the destination supplement name.
        /// </summary>
        /// <value>
        /// The destination supplement name.
        /// </value>
        [Units("-")]
        public string Destination { get; set; }
    }
}
