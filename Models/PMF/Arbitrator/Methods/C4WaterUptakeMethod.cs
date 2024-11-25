using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Models.PMF.Interfaces;
using Models.PMF.Organs;
using Models.Soils;
using Models.Soils.Arbitrator;
using Newtonsoft.Json;

namespace Models.PMF.Arbitrator
{
    /// <summary>The method used to do WaterUptake</summary>
    [Serializable]
    [ValidParent(ParentType = typeof(IArbitrator))]
    public class C4WaterUptakeMethod : Model, IUptakeMethod
    {
        /// <summary>Reference to Plant to find WaterDemands</summary>
        [Link(Type = LinkType.Ancestor)]
        protected Plant plant = null;

        /// <summary>The zone.</summary>
        [Link(Type = LinkType.Ancestor)]
        protected IZone zone = null;

        [Link(Type = LinkType.Scoped, ByName = true)]
        private Root root = null;

        ///<summary>The soil</summary> needed to get KL values
        [Link]
        public Soils.Soil Soil = null;
        
        //Used to access the soil properties for this crop
        private SoilCrop soilCrop = null;

        /// <summary>A list of organs or suborgans that have watardemands</summary>
        protected List<IHasWaterDemand> WaterDemands = new List<IHasWaterDemand>();

        /// <summary>Gets the water demand.</summary>
        /// <value>The water demand.</value>
        [JsonIgnore]
        public double WDemand { get; protected set; }

        /// <summary>Gets the water Supply.</summary>
        /// <value>The water supply.</value>
        [JsonIgnore]
        public double WatSupply { get; protected set; }

        /// <summary>Gets the water allocated in the plant (taken up).</summary>
        /// <value>The water uptake.</value>
        [JsonIgnore]
        public double WAllocated { get; protected set; }

        ///TotalSupply divided by WaterDemand - used to lookup ExpansionStress table - when calculating Actual LeafArea and calcStressedLeafArea
		[JsonIgnore]
        public double SDRatio { get; set; }

        /// <summary>Total available SW.</summary>
		[JsonIgnore]
        public double SWAvail { get; private set; }

        /// <summary>Things the plant model does when the simulation starts</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        virtual protected void OnSimulationCommencing(object sender, EventArgs e)
        {
            List<IHasWaterDemand> Waterdemands = new List<IHasWaterDemand>();
            soilCrop = Soil.FindDescendant<SoilCrop>(plant.Name + "Soil");
            if (soilCrop == null)
                throw new Exception($"Cannot find a soil crop parameterisation called {plant.Name + "Soil"} under Soil.Physical");

            foreach (Model Can in plant.FindAllInScope<IHasWaterDemand>())
                Waterdemands.Add(Can as IHasWaterDemand);

            WaterDemands = Waterdemands;
            SDRatio = 0.0;
            SWAvail = 0.0;
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
                waterDemand += WD.CalculateWaterDemand() * zone.Area;

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
                    WDemand += (o as IHasWaterDemand).CalculateWaterDemand() * zone.Area;

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
            foreach (ZoneWaterAndN Z in zones)
                StoreWaterVariablesForNitrogenUptake(Z);
        }

        private void StoreWaterVariablesForNitrogenUptake(ZoneWaterAndN zoneWater)
        {
            ZoneState myZone = root.Zones.Find(z => z.Name == zoneWater.Zone.Name);
            if (myZone != null)
            {
                var soilPhysical = myZone.Soil.FindChild<Soils.IPhysical>();
                var waterBalance = myZone.Soil.FindChild<ISoilWater>();

                //store Water variables for N Uptake calculation
                //Old sorghum doesn't do actualUptake of Water until end of day
                myZone.StartWater = new double[soilPhysical.Thickness.Length];
                myZone.AvailableSW = new double[soilPhysical.Thickness.Length];
                myZone.PotentialAvailableSW = new double[soilPhysical.Thickness.Length];
                myZone.Supply = new double[soilPhysical.Thickness.Length];

                double[] kl = soilCrop.KL;

                double[] llDep = MathUtilities.Multiply(soilCrop.LL, soilPhysical.Thickness);

                var currentLayer = SoilUtilities.LayerIndexOfDepth(myZone.Physical.Thickness, myZone.Depth);
                for (int layer = 0; layer <= currentLayer; ++layer)
                {
                    myZone.StartWater[layer] = waterBalance.SWmm[layer];

                    myZone.AvailableSW[layer] = Math.Max(waterBalance.SWmm[layer] - llDep[layer] * myZone.LLModifier[layer], 0) * myZone.RootProportions[layer];
                    myZone.PotentialAvailableSW[layer] = Math.Max(soilPhysical.DULmm[layer] - llDep[layer] * myZone.LLModifier[layer], 0) * myZone.RootProportions[layer];

                    myZone.Supply[layer] = Math.Max(myZone.AvailableSW[layer] * kl[layer] * myZone.RootProportionVolume[layer], 0.0);
                }
                var totalAvail = SWAvail = myZone.AvailableSW.Sum();
                var totalAvailPot = myZone.PotentialAvailableSW.Sum();
                var totalSupply = myZone.Supply.Sum();
                WatSupply = totalSupply;

                //used for SWDef ExpansionStress table lookup
                // TODO - COME BACK TO THIS.
                SDRatio = MathUtilities.Bound(MathUtilities.Divide(totalSupply, WDemand, 1.1), 0.0, 1000);
            }
        }
    }
}
