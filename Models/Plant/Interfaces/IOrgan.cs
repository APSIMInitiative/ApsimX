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
        /// Biomass removal logic for this organ.
        /// </summary>
        /// <param name="biomassRemoveType">Name of event that triggered this biomass remove call.</param>
        /// <param name="biomassToRemove">Biomass to remove</param>
        void DoRemoveBiomass(string biomassRemoveType, OrganBiomassRemovalType biomassToRemove);


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



   
