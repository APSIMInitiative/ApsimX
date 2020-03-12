using Models.PMF.Interfaces;
using Models.Soils.Arbitrator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models.PMF
{
    /// <summary>
    /// Interface class for Uptake Methods.
    /// </summary>
    public interface IUptakeMethod
    {
        /// <summary>
        /// Calculate the actual uptakes.
        /// </summary>
        void SetActualUptakes(List<Soils.Arbitrator.ZoneWaterAndN> zones, IArbitration[] Organs);

        /// <summary>
        /// Calculate the uptake estimates.
        /// </summary>
        List<Soils.Arbitrator.ZoneWaterAndN> GetUptakeEstimates(SoilState soilstate, IArbitration[] Organs);

    }
}
