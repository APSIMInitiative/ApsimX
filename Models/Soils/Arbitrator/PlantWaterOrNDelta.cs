using System;
using System.Collections.Generic;
using Models.Core;

namespace Models.Soils.Arbitrator
{
    /// <summary>
    /// Class to hold data on water or n supplies or uptakes and return values in kg (for the entire zone area) or kg/ha for the total area or by zone
    /// </summary>
    public class PlantWaterOrNDelta
    {
        /// <summary>The area of the zone in ha</summary>
        public List<ZoneWaterOrNDelta> Zones { get; set; }
        /// <summary>The area of the zone in ha</summary>
        public double ZoneAreaSum { get; private set; }
        /// <summary>the mass of the supply or uptake in kg</summary>
        public double Amount
        {
            get
            {
                double amount = 0;
                foreach (ZoneWaterOrNDelta z in Zones)
                { amount += z.Amount; }
                return amount;
            }
        }

        /// <summary>the mass of the supply or uptake in g/m2</summary>
        public double Gpm2 { get { return (Amount * 1000) / (ZoneAreaSum * 10000); } }

        /// <summary>the amount of resource per m</summary>
        public double Pm2 { get { return Amount / (ZoneAreaSum * 10000); } }

        /// <summary>the amount of resource per m</summary>
        public double MM { get { return Pm2; } }

        private double[] amountByZone = null;
        /// <summary>the mass of the supply or uptake in kg for each zone</summary>
        public double[] AmountByZone
        {
            get
            {
                return amountByZone;
            }
            set
            {
                amountByZone = value;
                int pos = 0;
                foreach (ZoneWaterOrNDelta z in Zones)
                {
                    z.Amount = value[pos];
                    pos += 1;
                }
            }
        }

        /// <summary>the mass of the supply or uptake in kg for each zone</summary>
        public double[] ByZoneAmountPha
        {
            get
            {
                double[] returnVals = new double[Zones.Count];
                for (int z = 0; z < Zones.Count; z++)
                {
                    returnVals[z] = Zones[z].Amount / Zones[z].Area;
                }
                return returnVals;
            }
        }

        /// <summary>Constructor</summary>
        public PlantWaterOrNDelta(List<double> zoneAreas)
        {
            Zones = new List<ZoneWaterOrNDelta>();
            foreach (double za in zoneAreas)
            {
                Zones.Add(new ZoneWaterOrNDelta(za));
                ZoneAreaSum += za;
            }
        }

        /// <summary>Constructor</summary>
        public PlantWaterOrNDelta(List<double> zoneAreas, List<double> amountByZone)
        {
            Zones = new List<ZoneWaterOrNDelta>();
            foreach (double za in zoneAreas)
            {
                Zones.Add(new ZoneWaterOrNDelta(za));
                ZoneAreaSum += za;
            }
            AmountByZone = amountByZone.ToArray();
        }

        /// <summary>return sum </summary>
        public static PlantWaterOrNDelta Add(PlantWaterOrNDelta a, PlantWaterOrNDelta b, List<double> areas)
        {
            List<double> amountByZone = new List<double>();
            foreach (ZoneWaterOrNDelta z in a.Zones)
            {
                amountByZone.Add(z.Amount);
            }
            for (int i = 0; i < b.Zones.Count; i++)
            {
                amountByZone[i] += b.AmountByZone[i];
            }
            return new PlantWaterOrNDelta(areas, amountByZone);
        }

    }
    /// <summary>Class to hold the mass of N or water delta for a zone</summary>
    public class ZoneWaterOrNDelta
    {
        /// <summary>The area of the zone in ha</summary>
        public double Area { get; private set; }
        /// <summary>the amount of resource over the area</summary>
        public double Amount { get; set; }

        /// <summary>Constructor</summary>
        public ZoneWaterOrNDelta(double area)
        {
            Area = area;
        }

    }
}
