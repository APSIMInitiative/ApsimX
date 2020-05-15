namespace Models.PMF.Interfaces
{
    using System.Collections.Generic;

    /// <summary>
    /// Interface used by models (e.g. STOCK, pests and diseases) to damage a plant.
    /// </summary>
    public interface IPlantDamage
    {
        /// <summary>Name of plant that can be damaged.</summary>
        string Name { get; }

        /// <summary>A list of organs that can be damaged.</summary>
        List<IOrganDamage> Organs { get; }

        /// <summary>Total amount of above ground biomass.</summary>
        Biomass AboveGround { get;  }

        /// <summary>Total amount of harvestable above ground biomass.</summary>
        Biomass AboveGroundHarvestable { get; }

        /// <summary>Plant population.</summary>
        double Population { get; }

        /// <summary>Leaf area index.</summary>
        double LAI { get; }

        /// <summary>Amount of assimilate available to be damaged.</summary>
        double AssimilateAvailable { get; }

        /// <summary>
        /// Remove biomass from a plant.
        /// </summary>
        /// <param name="amount">Amount of biomass to remove (kg/ha).</param>
        /// <returns>Amount of biomass removed.</returns>
        Biomass RemoveBiomass(double amount);

        /// <summary>
        /// Remove biomass from an organ.
        /// </summary>
        /// <param name="organName">Name of organ.</param>
        /// <param name="biomassRemoveType">Name of event that triggered this biomass remove call.</param>
        /// <param name="biomassToRemove">Biomass to remove.</param>
        void RemoveBiomass(string organName, string biomassRemoveType, OrganBiomassRemovalType biomassToRemove);

        /// <summary>
        /// Set the plant leaf area index.
        /// </summary>
        /// <param name="deltaLAI">Delta LAI.</param>
        void ReduceCanopy(double deltaLAI);

        /// <summary>
        /// Set the plant root length density.
        /// </summary>
        /// <param name="deltaRLD">New root length density.</param>
        void ReduceRootLengthDensity(double deltaRLD);

        /// <summary>
        /// Remove an amount of assimilate from the plant.
        /// </summary>
        /// <param name="deltaAssimilate">The amount of assimilate to remove (g/m2).</param>
        void RemoveAssimilate(double deltaAssimilate);

        /// <summary>
        /// Reduce the plant population.
        /// </summary>
        /// <param name="newPlantPopulation">The new plant population.</param>
        void ReducePopulation(double newPlantPopulation);
    }
}
