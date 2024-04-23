using System.Collections.Generic;
using Models.Interfaces;

namespace Models.Soils.Arbitrator
{

    /// <summary>
    /// A simple class for containing a single set of uptakes for a given crop.
    /// </summary>
    public class CropUptakes
    {
        /// <summary>Crop</summary>
        public IUptake Crop;

        /// <summary>List of uptakes</summary>
        public List<ZoneWaterAndN> Zones = new List<ZoneWaterAndN>();
    }
}
