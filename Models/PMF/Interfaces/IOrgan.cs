using Models.Core;

namespace Models.PMF.Interfaces
{
    /// <summary>
    /// Organ interface
    /// </summary>
    public interface IOrgan : IModel
    {

        /// <summary>
        /// Biomass removal logic for this organ.
        /// </summary>
        /// <param name="biomassRemoveType">Name of event that triggered this biomass remove call.</param>
        /// <param name="biomassToRemove">Biomass to remove</param>
        void RemoveBiomass(string biomassRemoveType, OrganBiomassRemovalType biomassToRemove);

    }
}
