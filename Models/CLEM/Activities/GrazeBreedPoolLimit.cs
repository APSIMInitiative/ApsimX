using Models.CLEM.Resources;

namespace Models.CLEM.Activities
{
    /// <summary>
    /// The proportional intake limit for a given pool by breed
    /// This class is used internally in pasture grazing activities
    /// </summary>
    public class GrazeBreedPoolLimit
    {
        /// <summary>
        /// Proportion of intake limit for pool
        /// </summary>
        public double Limit { get; set; }

        /// <summary>
        /// Pool that this limit applies to
        /// </summary>
        public GrazeFoodStorePool Pool { get; set; }
    }
}
