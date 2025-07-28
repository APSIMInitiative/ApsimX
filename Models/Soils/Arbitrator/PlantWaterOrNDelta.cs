using System;
using System.Collections.Generic;
using System.Linq;
using DocumentFormat.OpenXml.Bibliography;
using Models.Core;

namespace Models.Soils.Arbitrator
{
    /// <summary>
    /// Class to hold data on water or n supplies or uptakes and return values in kg (for the entire zone area) or kg/ha for the total area or by zone
    /// </summary>
    public class PlantWaterOrNDelta
    {
        /// <summary>Area of each zone</summary>
        [Units("ha")]
        public double[] AreaByZone { get; private set; }

        /// <summary>Area of all zones</summary>
        [Units("ha")]
        public double Area { get { return AreaByZone.Sum(); } }

        /// <summary>the mass of the supply or uptake in kg</summary>
        [Units("kg")]
        public double Amount { get { return AmountByZone.Sum(); } }

        /// <summary>the mass of the supply or uptake in g/m2</summary>
        public double Gpm2 { get { return (Amount * 1000) / (Area * 10000); } }

        /// <summary>the amount of resource per m</summary>
        public double Pm2 { get { return Amount / (Area * 10000); } }

        /// <summary>the amount of resource per m</summary>
        public double MM { get { return Pm2; } }

        /// <summary>The amount of resouce for each zone, kg or mm/// </summary>
        public double[] AmountByZone { get; set; }

        /// <summary>the mass of the supply or uptake in kg for each zone</summary>
        public double[] ByZoneAmountPha
        {
            get
            {
                double[] returnVals = new double[AreaByZone.Length];
                for (int z = 0; z < AreaByZone.Length; z++)
                {
                    returnVals[z] = AmountByZone[z] / AreaByZone[z];
                }
                return returnVals;
            }
        }

        /// <summary>Constructor</summary>
        public PlantWaterOrNDelta(List<double> zoneAreas)
        {
            AreaByZone = zoneAreas.ToArray(); 
            AmountByZone = new double[zoneAreas.Count];
        }

        /// <summary>Constructor</summary>
        public PlantWaterOrNDelta(List<double> zoneAreas, List<double> amountByZone)
        {
            AreaByZone = zoneAreas.ToArray();
            AmountByZone = amountByZone.ToArray();
        }

        /// <summary>return sum </summary>
        public static PlantWaterOrNDelta Add(PlantWaterOrNDelta a, PlantWaterOrNDelta b)
        {
            List<double> areas = a.AreaByZone.ToList();
            List<double> amounts = a.AmountByZone.ToList();
            for (int i =0; i< areas.Count; i++)
            {
                amounts[i] += b.AmountByZone[i];
            }

            return new PlantWaterOrNDelta(areas, amounts);
        }

    }
}
