// -----------------------------------------------------------------------
// GrazPlan Supplement model
// -----------------------------------------------------------------------
using Models.Core;

namespace Models.GrazPlan
{
    /// <summary>
    /// Buy an amount of supplement by name
    /// </summary>
    public class BuySuppType
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
        /// Gets or sets the amount of supplement eaten (kg).
        /// </summary>
        /// <value>
        /// The amount of supplement eaten (kg).
        /// </value>
        [Units("kg")]
        public double Amount { get; set; }
    }
}
