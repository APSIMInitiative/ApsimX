using Models.CLEM.Resources;
using System.Collections.Generic;

namespace Models.CLEM.Interfaces
{
    /// <summary>
    /// Graze food store type interface
    /// </summary>
    public interface IGrazeFoodStoreType
    {
        /// <summary>
        /// Amount of edible pasture
        /// </summary>
        public double Amount { get; }

    }
}
