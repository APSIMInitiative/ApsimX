using System.Collections.Generic;

namespace Models.PMF.Interfaces
{

    /// <summary>
    /// Interface used by models (e.g. STOCK, pests and diseases) to damage a plant.
    /// </summary>
    public interface IPlantDamage
    {
        /// <summary>Name of plant that can be damaged.</summary>
        string Name { get; }

        /// <summary>Return true if plant is alive and in the ground.</summary>
        bool IsAlive { get; }

        /// <summary>A list of organs that can be damaged.</summary>
        List<IOrganDamage> Organs { get; }

        /// <summary>Total amount of above ground biomass.</summary>
        IBiomass AboveGround { get; }

        /// <summary>Total amount of harvestable above ground biomass.</summary>
        IBiomass AboveGroundHarvestable { get; }

        /// <summary>Plant population.</summary>
        double Population { get; }

        /// <summary>Leaf area index.</summary>
        double LAI { get; }

        /// <summary>Amount of assimilate available to be damaged.</summary>
        double AssimilateAvailable { get; }

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
