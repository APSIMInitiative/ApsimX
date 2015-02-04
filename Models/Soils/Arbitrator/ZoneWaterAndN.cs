using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Models.Soils.Arbitrator
{
    /// <summary>
    /// 
    /// </summary>
    public class ZoneWaterAndN
    {
        /// <summary>The zone</summary>
        public string Name;
        /// <summary>The amount</summary>
        public double[] Water;
        public double[] NO3N;
        public double[] NH4N;
        public double TotalWater
        {
            get
            {
                return Utility.Math.Sum(Water);
            }
        }
        public static ZoneWaterAndN operator *(ZoneWaterAndN Z, double value)
        {
            ZoneWaterAndN NewZ = new ZoneWaterAndN();
            NewZ.Name = Z.Name;
            NewZ.Water = Utility.Math.Multiply_Value(Z.Water, value);
            NewZ.NO3N = Utility.Math.Multiply_Value(Z.NO3N, value);
            NewZ.NH4N = Utility.Math.Multiply_Value(Z.NH4N, value);
            return NewZ;
        }
        public static ZoneWaterAndN operator +(ZoneWaterAndN Z1, ZoneWaterAndN Z2)
        {
            if (Z1.Name != Z2.Name)
                throw new Exception("Cannot add zones with different names");
            ZoneWaterAndN NewZ = new ZoneWaterAndN();
            NewZ.Name = Z1.Name;
            NewZ.Water = Utility.Math.Add(Z1.Water, Z2.Water);
            NewZ.NO3N = Utility.Math.Add(Z1.NO3N, Z2.NO3N);
            NewZ.NH4N = Utility.Math.Add(Z1.NH4N, Z2.NH4N);
            return NewZ;
        }
        public static ZoneWaterAndN operator -(ZoneWaterAndN Z1, ZoneWaterAndN Z2)
        {
            if (Z1.Name != Z2.Name)
                throw new Exception("Cannot subtract zones with different names");
            ZoneWaterAndN NewZ = new ZoneWaterAndN();
            NewZ.Name = Z1.Name;
            NewZ.Water = Utility.Math.Subtract(Z1.Water, Z2.Water);
            NewZ.NO3N = Utility.Math.Subtract(Z1.NO3N, Z2.NO3N);
            NewZ.NH4N = Utility.Math.Subtract(Z1.NH4N, Z2.NH4N);
            return NewZ;
        }
    }

}
