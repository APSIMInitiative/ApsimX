using Models.CLEM.Resources;
using System.Collections.Generic;

namespace Models.CLEM.Interfaces
{
    /// <summary>
    /// Graze food store type interface
    /// </summary>
    public interface IGrazeFoodStoreType: IResourceType
    {
        /// <summary>
        /// Set the current pasture biomass for analysis
        /// </summary>
        public void SetCurrentBiomass();

        /// <summary>
        /// The biomass per hectare of pasture available
        /// </summary>
        public double KilogramsPerHa { get; }

        /// <summary>
        /// Amount available at start of time step (tonnes per ha)
        /// </summary>
        public double TonnesPerHectareStartOfTimeStep { get; set; }

        /// <summary>
        /// Scale the  current intake by a reduction factor before it is applied to the forage model.
        /// </summary>
        /// <param name="fractionReduced">Remaining proportion to apply (0..1).</param>
        public void ApplyDailyIntakeReduction(double fractionReduced);
    }
}
