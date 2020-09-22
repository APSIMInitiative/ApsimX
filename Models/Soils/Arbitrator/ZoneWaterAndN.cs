namespace Models.Soils.Arbitrator
{
    using System;
    using System.Linq;
    using APSIM.Shared.Utilities;
    using Core;
    using Models.Interfaces;
    using Models.Soils.Nutrients;

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

            Water = from.Water;
            NO3N = from.NO3N;
            NH4N = from.NH4N;
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

        /// <summary>Implements the operator *.</summary>
        /// <param name="zone">The zone</param>
        /// <param name="value">The value.</param>
        /// <returns>The result of the operator.</returns>
        public static ZoneWaterAndN operator *(ZoneWaterAndN zone, double value)
        {
            ZoneWaterAndN NewZ = new ZoneWaterAndN(zone);
            NewZ.Water = MathUtilities.Multiply_Value(zone.Water, value);
            NewZ.NO3N = MathUtilities.Multiply_Value(zone.NO3N, value);
            NewZ.NH4N = MathUtilities.Multiply_Value(zone.NH4N, value);
            return NewZ;
        }

        /// <summary>Implements the operator +.</summary>
        /// <param name="ZWN1">Zone 1</param>
        /// <param name="ZWN2">Zone 2</param>
        /// <returns>The result of the operator.</returns>
        /// <exception cref="System.Exception">Cannot add zones with different names</exception>
        public static ZoneWaterAndN operator +(ZoneWaterAndN ZWN1, ZoneWaterAndN ZWN2)
        {
            if (ZWN1.Zone.Name != ZWN2.Zone.Name)
                throw new Exception("Cannot add zones with different names");
            ZoneWaterAndN NewZ = new ZoneWaterAndN(ZWN1);
            NewZ.Water = MathUtilities.Add(ZWN1.Water, ZWN2.Water);
            NewZ.NO3N = MathUtilities.Add(ZWN1.NO3N, ZWN2.NO3N);
            NewZ.NH4N = MathUtilities.Add(ZWN1.NH4N, ZWN2.NH4N);
            return NewZ;
        }

        /// <summary>Implements the operator -.</summary>
        /// <param name="ZWN1">Zone 1</param>
        /// <param name="ZWN2">Zone 2</param>
        /// <returns>The result of the operator.</returns>
        /// <exception cref="System.Exception">Cannot subtract zones with different names</exception>
        public static ZoneWaterAndN operator -(ZoneWaterAndN ZWN1, ZoneWaterAndN ZWN2)
        {
            if (ZWN1.Zone.Name != ZWN2.Zone.Name)
                throw new Exception("Cannot subtract zones with different names");
            ZoneWaterAndN NewZ = new ZoneWaterAndN(ZWN1);
            NewZ.Water = MathUtilities.Subtract(ZWN1.Water, ZWN2.Water);
            NewZ.NO3N = MathUtilities.Subtract(ZWN1.NO3N, ZWN2.NO3N);
            NewZ.NH4N = MathUtilities.Subtract(ZWN1.NH4N, ZWN2.NH4N);
            return NewZ;
        }
    }

}
