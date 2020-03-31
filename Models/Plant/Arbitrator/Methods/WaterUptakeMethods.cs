using APSIM.Shared.Utilities;
using Models.Core;
using Models.PMF.Interfaces;
using Models.Soils.Arbitrator;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Models.PMF.Arbitrator
{
    /// <summary>The method used to do WaterUptake</summary>
    [Serializable]
    [ValidParent(ParentType = typeof(IArbitrator))]
    public class WaterUptakeMethod : Model, IUptakeMethod
    {
        /// <summary>Reference to Plant to find WaterDemands</summary>
        [Link(Type = LinkType.Ancestor)]
        protected Plant plant = null;

        /// <summary>A list of organs or suborgans that have watardemands</summary>
        protected List<IHasWaterDemand> WaterDemands = new List<IHasWaterDemand>();

        /// <summary>Gets the water demand.</summary>
        /// <value>The water demand.</value>
        [XmlIgnore]
        public double WDemand { get; protected set; }

        /// <summary>Gets the water Supply.</summary>
        /// <value>The water supply.</value>
        [XmlIgnore]
        public double WSupply { get; protected set; }

        /// <summary>Gets the water allocated in the plant (taken up).</summary>
        /// <value>The water uptake.</value>
        [XmlIgnore]
        public double WAllocated { get; protected set; }


        /// <summary>Things the plant model does when the simulation starts</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        virtual protected void OnSimulationCommencing(object sender, EventArgs e) 
        {
            List<IHasWaterDemand> Waterdemands = new List<IHasWaterDemand>();

            foreach (Model Can in Apsim.FindAll(plant, typeof(IHasWaterDemand)))
                Waterdemands.Add(Can as IHasWaterDemand);

            WaterDemands = Waterdemands;
        }

        /// <summary>The method used to arbitrate N allocations</summary>
        public List<ZoneWaterAndN> GetUptakeEstimates(SoilState soilstate, IArbitration[] Organs)
        {
            // Get all water supplies.
            double waterSupply = 0;  //NOTE: This is in L, not mm, to arbitrate water demands for spatial simulations.

            List<double[]> supplies = new List<double[]>();
            List<ZoneWaterAndN> zones = new List<ZoneWaterAndN>();
            foreach (ZoneWaterAndN zone in soilstate.Zones)
                foreach (IOrgan o in Organs)
                    if (o is IWaterNitrogenUptake)
                    {
                        double[] organSupply = (o as IWaterNitrogenUptake).CalculateWaterSupply(zone);
                        if (organSupply != null)
                        {
                            supplies.Add(organSupply);
                            zones.Add(zone);
                            waterSupply += MathUtilities.Sum(organSupply) * zone.Zone.Area;
                        }
                    }

            // Calculate total water demand.
            double waterDemand = 0; //NOTE: This is in L, not mm, to arbitrate water demands for spatial simulations.

            foreach (IHasWaterDemand WD in WaterDemands)
                waterDemand += WD.CalculateWaterDemand() * plant.Zone.Area;

            // Calculate demand / supply ratio.
            double fractionUsed = 0;
            if (waterSupply > 0)
                fractionUsed = Math.Min(1.0, waterDemand / waterSupply);

            // Apply demand supply ratio to each zone and create a ZoneWaterAndN structure
            // to return to caller.
            List<ZoneWaterAndN> ZWNs = new List<ZoneWaterAndN>();
            for (int i = 0; i < supplies.Count; i++)
            {
                // Just send uptake from my zone
                ZoneWaterAndN uptake = new ZoneWaterAndN(zones[i]);
                uptake.Water = MathUtilities.Multiply_Value(supplies[i], fractionUsed);
                uptake.NO3N = new double[uptake.Water.Length];
                uptake.NH4N = new double[uptake.Water.Length];
                uptake.PlantAvailableNO3N = new double[uptake.Water.Length];
                uptake.PlantAvailableNH4N = new double[uptake.Water.Length];
                ZWNs.Add(uptake);
            }
            return ZWNs;
        }

        /// Calculating the actual water uptake across all zones.
        public void SetActualUptakes(List<ZoneWaterAndN> zones, IArbitration[] Organs)
        {
            // Calculate the total water supply across all zones.
            double waterSupply = 0;   //NOTE: This is in L, not mm, to arbitrate water demands for spatial simulations.
            foreach (ZoneWaterAndN Z in zones)
            {
                waterSupply += MathUtilities.Sum(Z.Water) * Z.Zone.Area;
            }
            // Calculate total plant water demand.
            WDemand = 0.0; //NOTE: This is in L, not mm, to arbitrate water demands for spatial simulations.
            foreach (IArbitration o in Organs)
                if (o is IHasWaterDemand)
                    WDemand += (o as IHasWaterDemand).CalculateWaterDemand() * plant.Zone.Area;

            // Calculate the fraction of water demand that has been given to us.
            double fraction = 1;
            if (WDemand > 0)
                fraction = Math.Min(1.0, waterSupply / WDemand);

            // Proportionally allocate supply across organs.
            WAllocated = 0.0;

            foreach (IHasWaterDemand WD in WaterDemands)
            {
                double demand = WD.CalculateWaterDemand();
                double allocation = fraction * demand;
                WD.WaterAllocation = allocation;
                WAllocated += allocation;
            }

        }
    }
}
