namespace Models.Soils.Arbitrator
{
    using System;
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

        private Soil soilInZone;
        private SoilState soilState;

        /// <summary>
        /// The Zone for this water and N
        /// </summary>
        public Zone Zone { get; private set; }

        /// <summary>Amount of water (mm)</summary>
        public double[] Water { get; set; }

        /// <summary>Amount of NO3 (kg/ha)</summary>
        public double[] NO3N { get; set; }

        /// <summary>Amount of NH4 (kg/ha)</summary>
        public double[] NH4N { get; set; }

        /// <summary>Gets the sum of 'Water' (mm)</summary>
        public double TotalWater { get { return MathUtilities.Sum(Water); } }

        /// <summary>Gets the sum of 'NO3N' (mm)</summary>
        public double TotalNO3N { get { return MathUtilities.Sum(NO3N); } }

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
            soilState = from.soilState;
            Zone = from.Zone;

            Water = from.Water;
            NO3N = from.NO3N;
            NH4N = from.NH4N;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="zone"></param>
        /// <param name="soil">The soil in the zone.</param>
        /// <param name="parentSoilState">Parent soil state.</param>
        public ZoneWaterAndN(Zone zone, Soil soil, SoilState parentSoilState)
        {
            Zone = zone;
            soilInZone = soil;
            soilState = parentSoilState;
            Initialise();
        }

        /// <summary>Initialises this instance.</summary>
        public void Initialise()
        {
            NO3Solute = Apsim.Find(soilInZone, "NO3") as ISolute;
            NH4Solute = Apsim.Find(soilInZone, "NH4") as ISolute;
            var PlantAvailableNO3Solute = Apsim.Find(soilInZone, "PlantAvailableNO3") as ISolute;
            if (PlantAvailableNO3Solute != null)
                NO3Solute = PlantAvailableNO3Solute;
            var PlantAvailableNH4Solute = Apsim.Find(soilInZone, "PlantAvailableNH4") as ISolute;
            if (PlantAvailableNH4Solute != null)
                NH4Solute = PlantAvailableNH4Solute;
        }

        /// <summary>Initialises this instance.</summary>
        public void InitialiseToSoilState()
        {
            if (Water == null)
            {
                Water = new double[soilInZone.Water.Length];
                NO3N = new double[soilInZone.Water.Length];
                NH4N = new double[soilInZone.Water.Length];
            }
            Array.Copy(soilInZone.Water, Water, soilInZone.Water.Length);
            Array.Copy(NO3Solute.kgha, NO3N, NO3Solute.kgha.Length);
            Array.Copy(NH4Solute.kgha, NH4N, NH4Solute.kgha.Length);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="previousUptakeEstimate"></param>
        /// <param name="multipler"></param>
        /// <param name="calculationType"></param>
        public void ApplyTransform(Estimate previousUptakeEstimate, double multipler, Estimate.CalcType calculationType)
        {
            if (calculationType == Estimate.CalcType.Water)
            {
                var totalUptake = previousUptakeEstimate.CalculateWaterUptakeFromZone(Zone.Name);
                if (totalUptake != null)
                    for (int i = 0; i < Water.Length; i++)
                        Water[i] = soilInZone.Water[i] - totalUptake[i] * multipler;
            }
            else
            {
                var totalNO3Uptake = previousUptakeEstimate.CalculateNO3UptakeFromZone(Zone.Name);
                var totalNH4Uptake = previousUptakeEstimate.CalculateNH4UptakeFromZone(Zone.Name);
                if (totalNO3Uptake != null)
                {
                    var no3 = NO3Solute.kgha;
                    var nh4 = NH4Solute.kgha;
                    for (int i = 0; i < Water.Length; i++)
                    {
                        NO3N[i] = no3[i] - totalNO3Uptake[i] * multipler;
                        NH4N[i] = nh4[i] - totalNH4Uptake[i] * multipler;
                    }
                }
            }
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
