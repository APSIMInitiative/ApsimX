using APSIM.Shared.Utilities;
using Models.Core;
using Models.PMF.Interfaces;
using Models.PMF.Organs;
using Models.Soils.Arbitrator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

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

        [Link(Type = LinkType.Scoped, ByName = true)]
        private Root root = null;
        
        ///<summary>The soil</summary> needed to get KL values
        [Link]
        public Soils.Soil Soil = null;

        /// <summary>A list of organs or suborgans that have watardemands</summary>
        protected List<IHasWaterDemand> WaterDemands = new List<IHasWaterDemand>();

        /// <summary>Gets the water demand.</summary>
        /// <value>The water demand.</value>
        [XmlIgnore]
        public double WDemand { get; protected set; }

        /// <summary>Gets the water Supply.</summary>
        /// <value>The water supply.</value>
        [XmlIgnore]
        public double WatSupply { get; protected set; }

        /// <summary>Gets the water allocated in the plant (taken up).</summary>
        /// <value>The water uptake.</value>
        [XmlIgnore]
        public double WAllocated { get; protected set; }

        ///TotalAvailable divided by TotalPotential - used to lookup PhenologyStress table
        public double SWAvailRatio { get; set; }

        ///TotalSupply divided by WaterDemand - used to lookup ExpansionStress table - when calculating Actual LeafArea and calcStressedLeafArea
        public double SDRatio { get; set; }


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
            foreach (ZoneWaterAndN Z in zones)
                StoreWaterVariablesForNitrogenUptake(Z);
        }

        private void StoreWaterVariablesForNitrogenUptake(ZoneWaterAndN zoneWater)
        {
            ZoneState myZone = root.Zones.Find(z => z.Name == zoneWater.Zone.Name);
            if (myZone != null)
            {
                //store Water variables for N Uptake calculation
                //Old sorghum doesn't do actualUptake of Water until end of day
                myZone.StartWater = new double[myZone.soil.Thickness.Length];
                myZone.AvailableSW = new double[myZone.soil.Thickness.Length];
                myZone.PotentialAvailableSW = new double[myZone.soil.Thickness.Length];
                myZone.Supply = new double[myZone.soil.Thickness.Length];

                var soilCrop = Soil.Crop(plant.Name);
                double[] kl = soilCrop.KL;

                double[] llDep = MathUtilities.Multiply(soilCrop.LL, myZone.soil.Thickness);

                if (root.Depth != myZone.Depth)
                    myZone.Depth += 0; // wtf??

                var currentLayer = myZone.soil.LayerIndexOfDepth(myZone.Depth);
                var currentLayerProportion = myZone.soil.ProportionThroughLayer(currentLayer, myZone.Depth);
                for (int layer = 0; layer <= currentLayer; ++layer)
                {
                    myZone.StartWater[layer] = myZone.soil.Water[layer];

                    myZone.AvailableSW[layer] = Math.Max(myZone.soil.Water[layer] - llDep[layer], 0);
                    myZone.PotentialAvailableSW[layer] = myZone.soil.DULmm[layer] - llDep[layer];

                    if (layer == currentLayer)
                    {
                        myZone.AvailableSW[layer] *= currentLayerProportion;
                        myZone.PotentialAvailableSW[layer] *= currentLayerProportion;
                    }

                    var proportion = root.rootProportionInLayer(layer, myZone);
                    myZone.Supply[layer] = Math.Max(myZone.AvailableSW[layer] * kl[layer] * proportion, 0.0);
                }
                var totalAvail = myZone.AvailableSW.Sum();
                var totalAvailPot = myZone.PotentialAvailableSW.Sum();
                var totalSupply = myZone.Supply.Sum();
                WatSupply = totalSupply;

                // Set reporting variables.
                //Avail = myZone.AvailableSW;
                //PotAvail = myZone.PotentialAvailableSW;

                //used for SWDef PhenologyStress table lookup
                SWAvailRatio = MathUtilities.Bound(MathUtilities.Divide(totalAvail, totalAvailPot, 1.0), 0.0, 10.0);

                //used for SWDef ExpansionStress table lookup
                SDRatio = MathUtilities.Bound(MathUtilities.Divide(totalSupply, WDemand, 1.0), 0.0, 10);

                //used for SwDefPhoto Stress
                //PhotoStress = MathUtilities.Bound(MathUtilities.Divide(totalSupply, WDemand, 1.0), 0.0, 1.0);
            }
        }
    }
}
