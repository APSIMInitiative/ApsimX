using System.Collections.Generic;

namespace Models.PMF.Interfaces
{

    /// <summary>Interface used by models (e.g. STOCK, pests and diseases) to damage a biomass (e.g. plant or surface residues).</summary>
    public interface IHasDamageableBiomass
    {
        /// <summary>Name of plant that can be damaged.</summary>
        string Name { get; }

        /// <summary>A list of material (biomass) that can be damaged.</summary>
        IEnumerable<DamageableBiomass> Material { get; }

        /// <summary>Remove biomass from organ.</summary>
        /// <param name="liveToRemove">Fraction of live biomass to remove from simulation (0-1).</param>
        /// <param name="deadToRemove">Fraction of dead biomass to remove from simulation (0-1).</param>
        /// <param name="liveToResidue">Fraction of live biomass to remove and send to residue pool(0-1).</param>
        /// <param name="deadToResidue">Fraction of dead biomass to remove and send to residue pool(0-1).</param>
        /// <returns>The amount of biomass (live+dead) removed from the plant (g/m2).</returns>
        double RemoveBiomass(double liveToRemove = 0, double deadToRemove = 0, double liveToResidue = 0, double deadToResidue = 0);
    }
}