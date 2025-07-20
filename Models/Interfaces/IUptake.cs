using System.Collections.Generic;
using Models.Soils.Arbitrator;

namespace Models.Interfaces
{

    /// <summary>
    /// This interface defines the communications between a soil arbitrator and
    /// and crop.
    /// </summary>
    public interface IUptake
    {
        /// <summary> Flag if the plant is alive</summary>
        bool IsAlive { get; }
        
        /// <summary>
        /// Calculate the potential sw uptake for today. Should return null if crop is not in the ground.
        /// </summary>
        List<ZoneWaterAndN> GetWaterUptakeEstimates(SoilState soilstate);

        /// <summary>
        /// Calculate the potential sw uptake for today. Should return null if crop is not in the ground.
        /// </summary>
        List<ZoneWaterAndN> GetNitrogenUptakeEstimates(SoilState soilstate);

        /// <summary>
        /// Set the sw uptake for today.
        /// </summary>
        void SetActualWaterUptake(List<ZoneWaterAndN> info);

        /// <summary>
        /// Set the sw uptake for today
        /// </summary>
        void SetActualNitrogenUptakes(List<ZoneWaterAndN> info);
    }
}
