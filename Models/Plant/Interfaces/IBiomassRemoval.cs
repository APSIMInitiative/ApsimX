// -----------------------------------------------------------------------
// <copyright file="IOrgan.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.PMF.Interfaces
{
    using System;
    using Models.Soils.Arbitrator;
    using System.Collections.Generic;

    /// <summary>
    /// Specifies parameters to be passed to organs with cut, graze and harvest events
    /// </summary>
    public interface IBiomassRemoval
    {
        ///<summary>Container to hold organ sized list</summary>
        OrganBiomassRemovalType RemoveBiomass { set; }
    }
    ///<summary>Data passed to each organ when a biomass remove event occurs</summary>
    [Serializable]
    public class OrganBiomassRemovalType
    {
        /// <summary>
        /// The amount of biomass to removeed from each organ on harvest, cut or graze.
        /// </summary>
        public double FractionRemoved { get; set; }
        /// <summary>
        /// The amount of biomass to removed from each organ and passed to residue pools on defoliation events
        /// </summary>
        public double FractionToResidue { get; set; }
        /// <summary>
        /// The amount of biomass to removed from each organ and passed to residue pools on defoliation events
        /// </summary>
        public string RemovalMethod { get; set; }       
    }
}



   
