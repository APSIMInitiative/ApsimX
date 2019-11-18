namespace Models.PMF.Interfaces
{
    /// <summary>
    /// Interface used by models (e.g. STOCK, pests and diseases) to access an organ's properties to calculate damage.
    /// The actual damage is done through the plant damage interface.
    /// </summary>
    public interface IOrganDamage
    {
        /// <summary>Name of the organ.</summary>
        string Name { get; }

        /// <summary>Gets the live biomass of the organ.</summary>
        Biomass Live { get; }

        /// <summary>Gets the dead biomass of the organ.</summary>
        Biomass Dead { get; }

        /// <summary>Gets a value indicating whether the organ is above ground or not.</summary>
        bool IsAboveGround { get; }

        /// <summary>
        /// Biomass removal logic for this organ.
        /// </summary>
        /// <param name="biomassRemoveType">Name of event that triggered this biomass remove call.</param>
        /// <param name="biomassToRemove">Biomass to remove</param>
        void RemoveBiomass(string biomassRemoveType, OrganBiomassRemovalType biomassToRemove);
    }
}



   
