// -----------------------------------------------------------------------
// <copyright file="ZoneWaterAndN.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.Soils.Arbitrator
{
    using System;
    using APSIM.Shared.Utilities;

    /// <summary>
    /// Represents a zone (point, field etc) that has water and N values.
    /// </summary>
    public class ZoneWaterAndN
    {
        /// <summary>The zone</summary>
        public string Name;

        /// <summary>Amount of water (mm)</summary>
        public double[] Water;

        /// <summary>Amount of N (kg/ha)</summary>
        public double[] NO3N;

        /// <summary>Amount of NH4 (kg/ha)</summary>
        public double[] NH4N;

        /// <summary>Gets the sum of 'Water' (mm)</summary>
        public double TotalWater { get { return MathUtilities.Sum(Water); } }

        /// <summary>Gets the sum of 'NO3N' (mm)</summary>
        public double TotalNO3N { get { return MathUtilities.Sum(NO3N); } }

        /// <summary>Implements the operator *.</summary>
        /// <param name="zone">The zone</param>
        /// <param name="value">The value.</param>
        /// <returns>The result of the operator.</returns>
        public static ZoneWaterAndN operator *(ZoneWaterAndN zone, double value)
        {
            ZoneWaterAndN NewZ = new ZoneWaterAndN();
            NewZ.Name = zone.Name;
            NewZ.Water = MathUtilities.Multiply_Value(zone.Water, value);
            NewZ.NO3N = MathUtilities.Multiply_Value(zone.NO3N, value);
            NewZ.NH4N = MathUtilities.Multiply_Value(zone.NH4N, value);
            return NewZ;
        }

        /// <summary>Implements the operator +.</summary>
        /// <param name="zone1">Zone 1</param>
        /// <param name="zone2">Zone 2</param>
        /// <returns>The result of the operator.</returns>
        /// <exception cref="System.Exception">Cannot add zones with different names</exception>
        public static ZoneWaterAndN operator +(ZoneWaterAndN zone1, ZoneWaterAndN zone2)
        {
            if (zone1.Name != zone2.Name)
                throw new Exception("Cannot add zones with different names");
            ZoneWaterAndN NewZ = new ZoneWaterAndN();
            NewZ.Name = zone1.Name;
            NewZ.Water = MathUtilities.Add(zone1.Water, zone2.Water);
            NewZ.NO3N = MathUtilities.Add(zone1.NO3N, zone2.NO3N);
            NewZ.NH4N = MathUtilities.Add(zone1.NH4N, zone2.NH4N);
            return NewZ;
        }

        /// <summary>Implements the operator -.</summary>
        /// <param name="zone1">Zone 1</param>
        /// <param name="zone2">Zone 2</param>
        /// <returns>The result of the operator.</returns>
        /// <exception cref="System.Exception">Cannot subtract zones with different names</exception>
        public static ZoneWaterAndN operator -(ZoneWaterAndN zone1, ZoneWaterAndN zone2)
        {
            if (zone1.Name != zone2.Name)
                throw new Exception("Cannot subtract zones with different names");
            ZoneWaterAndN NewZ = new ZoneWaterAndN();
            NewZ.Name = zone1.Name;
            NewZ.Water = MathUtilities.Subtract(zone1.Water, zone2.Water);
            NewZ.NO3N = MathUtilities.Subtract(zone1.NO3N, zone2.NO3N);
            NewZ.NH4N = MathUtilities.Subtract(zone1.NH4N, zone2.NH4N);
            return NewZ;
        }
    }

}
