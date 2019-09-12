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
                    Zones.Add(new ZoneWaterAndN(Z, soil));
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

        /// <summary>Implements the operator -.</summary>
        /// <param name="state">The soil state.</param>
        /// <param name="estimate">The estimate to subtract from the soil state.</param>
        /// <returns>The result of the operator.</returns>
        public static SoilState operator -(SoilState state, Estimate estimate)
        {
            SoilState NewState = new SoilState(state);

            foreach (CropUptakes C in estimate.Values)
                foreach (ZoneWaterAndN Z in C.Zones)
                    foreach (ZoneWaterAndN NewZ in NewState.Zones)
                        if (Z.Zone.Name == NewZ.Zone.Name)
                        {
                            NewZ.Water = MathUtilities.Subtract(NewZ.Water, Z.Water);
                            NewZ.NO3N = MathUtilities.Subtract(NewZ.NO3N, Z.NO3N);
                            NewZ.NH4N = MathUtilities.Subtract(NewZ.NH4N, Z.NH4N);
                            NewZ.PlantAvailableNO3N = MathUtilities.Subtract(NewZ.PlantAvailableNO3N, Z.PlantAvailableNO3N);
                            NewZ.PlantAvailableNH4N = MathUtilities.Subtract(NewZ.PlantAvailableNH4N, Z.PlantAvailableNH4N);

                            for (int i = 0; i < NewZ.Water.Length; i++)
                            {
                                if (NewZ.Water[i] < 0)
                                    NewZ.Water[i] = 0;
                                if (NewZ.NO3N[i] < 0)
                                    NewZ.NO3N[i] = 0;
                                if (NewZ.NH4N[i] < 0)
                                    NewZ.NH4N[i] = 0;
                                if (NewZ.PlantAvailableNO3N[i] < 0)
                                    NewZ.PlantAvailableNO3N[i] = 0;
                                if (NewZ.PlantAvailableNH4N[i] < 0)
                                    NewZ.PlantAvailableNH4N[i] = 0;
                            }
                        }
            return NewState;
        }

    }
}
