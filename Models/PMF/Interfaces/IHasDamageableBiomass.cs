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

        /// <summary>
        /// Remove biomass from an organ.
        /// </summary>
        /// <param name="materialName">Name of organ.</param>
        /// <param name="biomassRemoveType">Name of event that triggered this biomass remove call.</param>
        /// <param name="biomassToRemove">Biomass to remove.</param>
        void RemoveBiomass(string materialName, string biomassRemoveType, OrganBiomassRemovalType biomassToRemove);
    }
}