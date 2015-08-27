// -----------------------------------------------------------------------
// <copyright file="IOrgan.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.PMF.Interfaces
{
    /// <summary>
    /// Base organ model
    /// </summary>
    public interface IOrgan
    {
        /// <summary>
        /// Do Harvest logic for this organ.
        /// </summary>
        void DoHarvest();

        /// <summary>
        /// Biomass removal logic for this organ.
        /// </summary>
        OrganBiomassRemovalType RemoveBiomass { set; }

        /// <summary>
        /// The default proportions biomass to removeed from each organ on Harvest.
        /// </summary>
        OrganBiomassRemovalType HarvestDefault { get; set; }
        /// <summary>
        /// The default proportions biomass to removeed from each organ on Cutting
        /// </summary>
        OrganBiomassRemovalType CutDefault { get; set; }
        /// <summary>
        /// The default proportions biomass to removeed from each organ on Grazing
        /// </summary>
        OrganBiomassRemovalType GrazeDefault { get; set; }
    }

    /// <summary>An above ground interface</summary>
    public interface AboveGround { }

    /// <summary>A below ground interface</summary>
    public interface BelowGround { }

    /// <summary>Indicates the organ is a reproductive one.</summary>
    public interface Reproductive { }

    /// <summary>Indicates the organ transpires</summary>
    public interface Transpiring { }

}



   
