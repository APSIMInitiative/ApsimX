namespace Models.PMF.Interfaces
{
    /// <summary>
    /// Interface used by code (e.g. STOCK) to remove biomass from an organ.
    /// </summary>
    public interface IRemovableBiomass
    {
        /// <summary>Gets the live biomass</summary>
        Biomass Live { get; }

        /// <summary>Gets the dead biomass</summary>
        Biomass Dead { get; }

        /// <summary>Gets a value indicating whether the biomass is above ground or not</summary>
        bool IsAboveGround { get; }

        /// <summary>
        /// Biomass removal logic for this organ.
        /// </summary>
        /// <param name="biomassRemoveType">Name of event that triggered this biomass remove call.</param>
        /// <param name="biomassToRemove">Biomass to remove</param>
        void RemoveBiomass(string biomassRemoveType, OrganBiomassRemovalType biomassToRemove);
    }
}



   
