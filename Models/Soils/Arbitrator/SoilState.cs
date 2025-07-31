using System;
using System.Collections.Generic;
using APSIM.Core;
using Models.Core;

namespace Models.Soils.Arbitrator
{

    /// <summary>
    /// Encapsulates the state of water and N in multiple zones.
    /// </summary>
    [Serializable]
    public class SoilState
    {
        private IEnumerable<Zone> allZones;

        /// <summary>Initializes a new instance of the <see cref="SoilState"/> class.</summary>
        /// <param name="allZones">Collection of all zones</param>
        /// <param name="structure">Structure instance</param>
        public SoilState(IEnumerable<Zone> allZones, IStructure structure)
        {
            this.allZones = allZones;
            Zones = new List<ZoneWaterAndN>();
            foreach (Zone Z in allZones)
            {
                Soil soil = Z.FindChild<Soil>();
                if (soil != null)
                    Zones.Add(new ZoneWaterAndN(Z, soil, structure));
            }
        }

        /// <summary>Constructor to copy state from another instance.</summary>
        /// <param name="from">The instance to copy from.</param>
        public SoilState(SoilState from)
        {
            allZones = from.allZones;
            Zones = new List<ZoneWaterAndN>();
            foreach (var Z in from.Zones)
                Zones.Add(new ZoneWaterAndN(Z));
        }

        /// <summary>Initialises this instance.</summary>
        public void Initialise()
        {
            foreach (ZoneWaterAndN zone in Zones)
                zone.InitialiseToSoilState();
        }

        /// <summary>Gets all zones in this soil state.</summary>
        public List<ZoneWaterAndN> Zones { get; private set; }
    }
}
