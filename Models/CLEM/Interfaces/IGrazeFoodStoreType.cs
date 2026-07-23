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
        /// Calculate gut fill for the feed type
        /// </summary>
        /// <param name="dmd">Dry matter digestibility if needed</param>
        /// <returns></returns>
        public double CalculateGutFill(double dmd);

        /// <summary>
        /// Method to create mixed pasture intake groups for feeding.
        /// </summary>
        /// <param name="numberOfTimesteps">Number of timesteps to convert daily to total intake</param>
        /// <param name="greenAge">
        /// The age (in months) for pasture to be considered green. (-1 ignore green details)
        /// </param>
        /// <param name="dmdStep">The step size for Dry Matter Digestibility (DMD) categories (100 no groups).</param>
        public List<FoodResourceStore> GenerateIntakeGroups(int numberOfTimesteps, int greenAge = -1, int dmdStep = 10);

        /// <summary>
        /// Decrease pending for specified food resource store
        /// </summary>
        /// <param name="request">Resource request involved</param>
        /// <param name="store">Food resource store</param>
        /// <param name="amount">Amount to decrease</param>
        public void DecreasePendingByStore(ResourceRequest request, FoodResourceStore store, double amount);
    }
}
