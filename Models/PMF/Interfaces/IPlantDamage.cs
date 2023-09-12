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

        /// <summary>Total amount of above ground biomass.</summary>
        IBiomass AboveGround { get; }

        /// <summary>Total amount of harvestable above ground biomass.</summary>
        IBiomass AboveGroundHarvestable { get; }

        /// <summary>Plant population.</summary>
        double Population { get; }

        /// <summary>Leaf area index.</summary>
        double LAI { get; }

        /// <summary>
        /// Reduce the plant population.
        /// </summary>
        /// <param name="newPlantPopulation">The new plant population.</param>
        void ReducePopulation(double newPlantPopulation);
    }
}
