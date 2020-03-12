using APSIM.Shared.Utilities;
using Models.Core;
using Models.PMF.Interfaces;
using Models.Soils.Arbitrator;
using System;
using System.Collections.Generic;
using System.Linq;

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

        /// <summary>The method used to arbitrate N allocations</summary>
        [Link(Type = LinkType.Ancestor, ByName = true)]
        protected OrganArbitrator Arbitrator = null;

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
                UptakeDemands.PlantAvailableNO3N = new double[zone.NO3N.Length];
                UptakeDemands.PlantAvailableNH4N = new double[zone.NO3N.Length];
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
                        N.UptakeSupply[i] += (MathUtilities.Sum(organNH4Supply) + MathUtilities.Sum(organNO3Supply)) * kgha2gsm * zone.Zone.Area / plant.Zone.Area;
                        NSupply += (MathUtilities.Sum(organNH4Supply) + MathUtilities.Sum(organNO3Supply)) * zone.Zone.Area;
                    }
                zones.Add(UptakeDemands);
            }

            double NDemand = (N.TotalPlantDemand - N.TotalReallocation) / kgha2gsm * plant.Zone.Area; //NOTE: This is in kg, not kg/ha, to arbitrate N demands for spatial simulations.

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
                NSupply += (MathUtilities.Sum(Z.NO3N) + MathUtilities.Sum(Z.NH4N)) * Z.Zone.Area;

            //Reset actual uptakes to each organ based on uptake allocated by soil arbitrator and the organs proportion of potential uptake
            //NUptakeSupply units should be g/m^2
            for (int i = 0; i < Organs.Count(); i++)
                N.UptakeSupply[i] = NSupply / plant.Zone.Area * N.UptakeSupply[i] / N.TotalUptakeSupply * kgha2gsm;

        }
    }
}
