using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Models.Core;

namespace Models.Soils.Arbitrator
{
    public class SoilState
    {
        public List<ZoneWaterAndN> Zones = new List<ZoneWaterAndN>();
        IModel Parent;
        public SoilState(IModel parent)
        {
            Parent = parent;
        }
        public void Initialise()
        {
            foreach (Zone Z in Apsim.Children(this.Parent, typeof(Zone)))
            {
                ZoneWaterAndN NewZ = new ZoneWaterAndN();
                NewZ.Name = Z.Name;
                Soil soil = Apsim.Child(Z, typeof(Soil)) as Soil;
                NewZ.Water = soil.Water;
                NewZ.NO3N = soil.NO3N;
                NewZ.NH4N = soil.NH4N;
                Zones.Add(NewZ);
            }
        }

        public ZoneWaterAndN ZoneState(string ZoneName)
        {
            foreach (ZoneWaterAndN Z in Zones)
                if (Z.Name == ZoneName)
                    return Z;
            throw new Exception("Could not find zone called " + ZoneName);
        }
        public static SoilState operator -(SoilState State, SoilArbitrator.Estimate E)
        {
            SoilState NewState = new SoilState(State.Parent);
            foreach (ZoneWaterAndN Z in State.Zones)
            {
                ZoneWaterAndN NewZ = new ZoneWaterAndN();
                NewZ.Name = Z.Name;
                NewZ.Water = Z.Water;
                NewZ.NO3N = Z.NO3N;
                NewZ.NH4N = Z.NH4N;
                NewState.Zones.Add(NewZ);
            }

            foreach (SoilArbitrator.CropUptakes C in E.Value)
                foreach (ZoneWaterAndN Z in C.Zones)
                    foreach (ZoneWaterAndN NewZ in NewState.Zones)
                        if (Z.Name == NewZ.Name)
                        {
                            NewZ.Water = Utility.Math.Subtract(NewZ.Water, Z.Water);
                            NewZ.NO3N = Utility.Math.Subtract(NewZ.NO3N, Z.NO3N);
                            NewZ.NH4N = Utility.Math.Subtract(NewZ.NH4N, Z.NH4N);
                        }
            return NewState;
        }

    }

}
