using System;
using System.Linq;
using Models.Core;
using Models.Interfaces;

namespace Models.Soils.Arbitrator
{

    /// <summary>
    /// Represents a zone (point, field etc) that has water and N values.
    /// </summary>
    [Serializable]
    public class ZoneWaterAndN
    {
        private ISolute NO3Solute;
        private ISolute NH4Solute;
        private ISoilWater WaterBalance;
        private Soil soilInZone;

        /// <summary>
        /// The Zone for this water and N
        /// </summary>
        public Zone Zone { get; private set; }

        /// <summary>Amount of water (mm)</summary>
        public double[] Water;

        /// <summary>Amount of NO3 (kg/ha)</summary>
        public double[] NO3N;

        /// <summary>Amount of NH4 (kg/ha)</summary>
        public double[] NH4N;

        /// <summary>Gets the sum of 'Water' (mm)</summary>
        public double TotalWater { get { return Water.Sum(); } }

        /// <summary>Gets the sum of 'NO3N' (mm)</summary>
        public double TotalNO3N { get { return NO3N.Sum(); } }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="zone"></param>
        public ZoneWaterAndN(Zone zone)
        {
            Zone = zone;
        }

        /// <summary>
        /// Constructor. Copy state from another instance.
        /// </summary>
        /// <param name="from">The instance to copy from.</param>
        public ZoneWaterAndN(ZoneWaterAndN from)
        {
            NO3Solute = from.NO3Solute;
            NH4Solute = from.NH4Solute;
            soilInZone = from.soilInZone;
            Zone = from.Zone;
            WaterBalance = from.WaterBalance;

            Water = (double[])from.Water.Clone();
            NO3N = (double[])from.NO3N.Clone();
            NH4N = (double[])from.NH4N.Clone();
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="zone"></param>
        /// <param name="soil">The soil in the zone.</param>
        public ZoneWaterAndN(Zone zone, Soil soil)
        {
            Zone = zone;
            soilInZone = soil;
            Initialise();
        }

        /// <summary>Initialises this instance.</summary>
        public void Initialise()
        {
            WaterBalance = soilInZone.FindInScope<ISoilWater>();
            NO3Solute = soilInZone.FindInScope<ISolute>("NO3");
            NH4Solute = soilInZone.FindInScope<ISolute>("NH4");
            var PlantAvailableNO3Solute = soilInZone.FindInScope<ISolute>("PlantAvailableNO3");
            if (PlantAvailableNO3Solute != null)
                NO3Solute = PlantAvailableNO3Solute;
            var PlantAvailableNH4Solute = soilInZone.FindInScope<ISolute>("PlantAvailableNH4");
            if (PlantAvailableNH4Solute != null)
                NH4Solute = PlantAvailableNH4Solute;
        }

        /// <summary>Initialises this instance.</summary>
        public void InitialiseToSoilState()
        {
            Water = WaterBalance.SWmm;
            NO3N = NO3Solute.kgha;
            NH4N = NH4Solute.kgha;
        }
    }
}
