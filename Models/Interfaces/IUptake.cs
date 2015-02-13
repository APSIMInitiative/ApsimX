// -----------------------------------------------------------------------
// <copyright file="IUptake.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.Interfaces
{
    using System.Collections.Generic;
    using Models.Soils.Arbitrator;

    /// <summary>
    /// This interface defines the communications between a soil arbitrator and
    /// and crop.
    /// </summary>
    public interface IUptake
    {
        /// <summary>
        /// Calculate the potential sw uptake for today. Should return null if crop is not in the ground.
        /// </summary>
        List<ZoneWaterAndN> GetSWUptakes(SoilState soilstate);

        /// <summary>
        /// Calculate the potential sw uptake for today. Should return null if crop is not in the ground.
        /// </summary>
        List<ZoneWaterAndN> GetNUptakes(SoilState soilstate);

        /// <summary>
        /// Set the sw uptake for today.
        /// </summary>
        void SetSWUptake(List<ZoneWaterAndN> info);
        
        /// <summary>
        /// Set the sw uptake for today
        /// </summary>
        void SetNUptake(List<ZoneWaterAndN> info);
    }
}
