// -----------------------------------------------------------------------
// <copyright file="IOrgan.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
using System.Collections.Generic;

namespace Models.PMF.Interfaces
{
    /// <summary>
    /// Base organ model
    /// </summary>
    public interface IOrgan
    {
        /// <summary>
        /// The Name of the organ.
        /// </summary>
        string Name { get; set; }
        
        /// <summary>
        /// Do Plant ending logic for this organ.
        /// </summary>
        void DoPlantEnding();

        /// <summary>
        /// Defaults for biomass removal.
        /// </summary>
        List<OrganBiomassRemovalType> BiomassRemovalDefaults { get; set; }

        /// <summary>
        /// Biomass removal logic for this organ.
        /// </summary>
        /// <param name="biomassToRemove">Biomass to remove</param>
        void DoRemoveBiomass(OrganBiomassRemovalType biomassToRemove);


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



   
