namespace Models.Soils.Arbitrator
{
    using System;
    using System.Collections.Generic;
    using Models.Core;
    using APSIM.Shared.Utilities;

    /// <summary>
    /// Encapsulates the state of water and N in multiple zones.
    /// </summary>
    [Serializable]
    public class SoilState
    {
        private List<Zone> allZones;

        /// <summary>Initializes a new instance of the <see cref="SoilState"/> class.</summary>
        public SoilState(List<Zone> allZones)
        {
            this.allZones = allZones;
            Zones = new List<ZoneWaterAndN>();
            foreach (Zone Z in allZones)
            {
                Soil soil = Apsim.Child(Z, typeof(Soil)) as Soil;
                if (soil != null)
                    Zones.Add(new ZoneWaterAndN(Z, soil, this));
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="previousUptakeEstimate"></param>
        /// <param name="previousUptakeMultipler"></param>
        /// <param name="calculationType"></param>
        public void ApplyTransform(Estimate previousUptakeEstimate, double previousUptakeMultipler, Estimate.CalcType calculationType)
        {
            foreach (ZoneWaterAndN zone in Zones)
                zone.ApplyTransform(previousUptakeEstimate, previousUptakeMultipler, calculationType);
        }
    }
}
