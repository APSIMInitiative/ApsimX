using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Models.PMF.Interfaces;
using Models.Soils.Arbitrator;

namespace Models.PMF.Arbitrator
{
    /// <summary>The method used to do NitrogenUptake</summary>
    [Serializable]
    [ValidParent(ParentType = typeof(IArbitrator))]
    public class NitrogenUptakeMethod : Model, IUptakeMethod
    {
        /// <summary>The method used to arbitrate N allocations</summary>
        [Link(Type = LinkType.Ancestor)]
        protected Plant plant = null;

        /// <summary>The zone.</summary>
        [Link(Type = LinkType.Ancestor)]
        protected IZone zone = null;

        /// <summary>The method used to arbitrate N allocations</summary>
        [Link(Type = LinkType.Ancestor, ByName = true)]
        protected IArbitrator Arbitrator = null;

        private const double kgha2gsm = 0.1;

        /// <summary>Calculate Nitrogen UptakeEstimates</summary>
        public List<ZoneWaterAndN> GetUptakeEstimates(SoilState soilstate, IArbitration[] Organs)
        {
            var N = Arbitrator.N;
            double NSupply = 0;//NOTE: This is in kg, not kg/ha, to arbitrate N demands for spatial simulations.

            for (int i = 0; i < Organs.Count(); i++)
                N.UptakeSupply[i] = 0;

            List<ZoneWaterAndN> zones = new List<ZoneWaterAndN>();
            foreach (ZoneWaterAndN zone in soilstate.Zones)
            {
                ZoneWaterAndN UptakeDemands = new ZoneWaterAndN(zone);

                UptakeDemands.NO3N = new double[zone.NO3N.Length];
                UptakeDemands.NH4N = new double[zone.NH4N.Length];
                UptakeDemands.Water = new double[UptakeDemands.NO3N.Length];

                //Get Nuptake supply from each organ and set the PotentialUptake parameters that are passed to the soil arbitrator
                for (int i = 0; i < Organs.Count(); i++)
                    if (Organs[i] is IWaterNitrogenUptake)
                    {
                        double[] organNO3Supply = new double[zone.NO3N.Length];
                        double[] organNH4Supply = new double[zone.NH4N.Length];
                        (Organs[i] as IWaterNitrogenUptake).CalculateNitrogenSupply(zone, ref organNO3Supply, ref organNH4Supply);
                        UptakeDemands.NO3N = MathUtilities.Add(UptakeDemands.NO3N, organNO3Supply); //Add uptake supply from each organ to the plants total to tell the Soil arbitrator
                        UptakeDemands.NH4N = MathUtilities.Add(UptakeDemands.NH4N, organNH4Supply);
                        double organSupply = organNH4Supply.Sum() + organNO3Supply.Sum();
                        N.UptakeSupply[i] += organSupply * kgha2gsm * zone.Zone.Area / this.zone.Area;
                        NSupply += organSupply * zone.Zone.Area;
                    }
                zones.Add(UptakeDemands);
            }

            double NDemand = (N.TotalPlantDemand - N.TotalReallocation) / kgha2gsm * zone.Area; //NOTE: This is in kg, not kg/ha, to arbitrate N demands for spatial simulations.
            if (NDemand < 0) NDemand = 0;  //NSupply should be zero if Reallocation can meet all demand (including small rounding errors which can make this -ve)

            if (NSupply > NDemand)
            {
                //Reduce the PotentialUptakes that we pass to the soil arbitrator
                double ratio = Math.Min(1.0, NDemand / NSupply);
                foreach (ZoneWaterAndN UptakeDemands in zones)
                {
                    UptakeDemands.NO3N = MathUtilities.Multiply_Value(UptakeDemands.NO3N, ratio);
                    UptakeDemands.NH4N = MathUtilities.Multiply_Value(UptakeDemands.NH4N, ratio);
                }
            }
            return zones;
        }

        /// <summary>Calculate the Actual Nitrogen Uptakes</summary>
        public void SetActualUptakes(List<ZoneWaterAndN> zones, IArbitration[] Organs)
        {
            var N = Arbitrator.N;
            // Calculate the total no3 and nh4 across all zones.
            double NSupply = 0;//NOTE: This is in kg, not kg/ha, to arbitrate N demands for spatial simulations.
            foreach (ZoneWaterAndN Z in zones)
                NSupply += (Z.NO3N.Sum() + Z.NH4N.Sum()) * Z.Zone.Area;

            //Reset actual uptakes to each organ based on uptake allocated by soil arbitrator and the organs proportion of potential uptake
            //NUptakeSupply units should be g/m^2
            for (int i = 0; i < Organs.Count(); i++)
                N.UptakeSupply[i] = NSupply / zone.Area * MathUtilities.Divide(N.UptakeSupply[i], N.TotalUptakeSupply, 0) * kgha2gsm;

        }
    }
}
