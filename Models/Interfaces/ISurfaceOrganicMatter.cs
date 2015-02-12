// -----------------------------------------------------------------------
// <copyright file="ISurfaceOrganicMatter.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.Interfaces
{
    using System;

    /// <summary>MicroClimate uses this canopy interface.</summary>
    public interface ISurfaceOrganicMatter
    {
        /// <summary>Adds material to the surface organic matter pool.</summary>
        /// <param name="biomass">The amount of biomass added (kg/ha).</param>
        /// <param name="N">The amount of N added (ppm).</param>
        /// <param name="P">The amount of P added (ppm).</param>
        /// <param name="type">Type of the biomass.</param>
        /// <param name="name">Name of the biomass written to summary file</param>
        void Add(double biomass, double N, double P, string type, string name);
    }

}
