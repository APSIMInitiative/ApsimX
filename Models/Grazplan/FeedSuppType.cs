// -----------------------------------------------------------------------
// GrazPlan Supplement model
// -----------------------------------------------------------------------
using Models.Core;

namespace Models.GrazPlan
{
    /// <summary>
    /// Feed an amount of supplement by name
    /// </summary>
    public class FeedSuppType
    {
        /// <summary>
        /// Gets or sets the supplement name.
        /// </summary>
        /// <value>
        /// The supplement name.
        /// </value>
        [Units("-")]
        public string Supplement { get; set; }

        /// <summary>
        /// Gets or sets the amount of supplement offered (kg).
        /// </summary>
        /// <value>
        /// The amount of supplement offered (kg).
        /// </value>
        [Units("kg")]
        public double Amount { get; set; }

        /// <summary>
        /// Gets or sets the paddock name.
        /// </summary>
        /// <value>
        /// The paddock name.
        /// </value>
        [Units("-")]
        public string Paddock { get; set; }
    }
}
