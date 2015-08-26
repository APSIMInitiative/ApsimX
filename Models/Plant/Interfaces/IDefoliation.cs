// -----------------------------------------------------------------------
// <copyright file="IOrgan.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.PMF.Interfaces
{
    /// <summary>
    /// Specifies parameters to be passed to organs with cut, graze and harvest events
    /// </summary>
    public interface IDefoliation
    {
        /// <summary>
        /// The amount of biomass to removeed from each organ on harvest, cut or graze.
        /// </summary>
        double FractionRemoved { get; set; }
        /// <summary>
        /// The amount of biomass to removed from each organ and passed to residue pools on defoliation events
        /// </summary>
        double FractionToResidue { get; set; }
    }  
}



   
