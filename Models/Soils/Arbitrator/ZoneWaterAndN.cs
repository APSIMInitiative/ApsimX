// -----------------------------------------------------------------------
// <copyright file="ZoneWaterAndN.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.Soils.Arbitrator
{
    using System;
    using APSIM.Shared.Utilities;
    using Core;
    /// <summary>
    /// Represents a zone (point, field etc) that has water and N values.
    /// </summary>
    public class ZoneWaterAndN
    {
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

        /// <summary>Amount of plant-avilable NO3 (kg/ha)</summary>
        public double[] PlantAvailableNO3N;

        /// <summary>Amount of plant-available NH4 (kg/ha)</summary>
        public double[] PlantAvailableNH4N;

        

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

        /// <summary>Implements the operator *.</summary>
        /// <param name="zone">The zone</param>
        /// <param name="value">The value.</param>
        /// <returns>The result of the operator.</returns>
        public static ZoneWaterAndN operator *(ZoneWaterAndN zone, double value)
        {
            ZoneWaterAndN NewZ = new ZoneWaterAndN(zone.Zone);
            NewZ.Water = MathUtilities.Multiply_Value(zone.Water, value);
            NewZ.NO3N = MathUtilities.Multiply_Value(zone.NO3N, value);
            NewZ.NH4N = MathUtilities.Multiply_Value(zone.NH4N, value);
            NewZ.PlantAvailableNO3N = MathUtilities.Multiply_Value(zone.PlantAvailableNO3N, value);  
            NewZ.PlantAvailableNH4N = MathUtilities.Multiply_Value(zone.PlantAvailableNH4N, value);
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
            ZoneWaterAndN NewZ = new ZoneWaterAndN(ZWN1.Zone);
            NewZ.Water = MathUtilities.Add(ZWN1.Water, ZWN2.Water);
            NewZ.NO3N = MathUtilities.Add(ZWN1.NO3N, ZWN2.NO3N);
            NewZ.NH4N = MathUtilities.Add(ZWN1.NH4N, ZWN2.NH4N);
            NewZ.PlantAvailableNO3N = MathUtilities.Add(ZWN1.PlantAvailableNO3N, ZWN2.PlantAvailableNO3N);
            NewZ.PlantAvailableNH4N = MathUtilities.Add(ZWN1.PlantAvailableNH4N, ZWN2.PlantAvailableNH4N);
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
            ZoneWaterAndN NewZ = new ZoneWaterAndN(ZWN1.Zone);
            NewZ.Water = MathUtilities.Subtract(ZWN1.Water, ZWN2.Water);
            NewZ.NO3N = MathUtilities.Subtract(ZWN1.NO3N, ZWN2.NO3N);
            NewZ.NH4N = MathUtilities.Subtract(ZWN1.NH4N, ZWN2.NH4N);
            NewZ.PlantAvailableNO3N = MathUtilities.Subtract(ZWN1.PlantAvailableNO3N, ZWN2.PlantAvailableNO3N);
            NewZ.PlantAvailableNH4N = MathUtilities.Subtract(ZWN1.PlantAvailableNH4N, ZWN2.PlantAvailableNH4N);
            return NewZ;
        }
    }

}
