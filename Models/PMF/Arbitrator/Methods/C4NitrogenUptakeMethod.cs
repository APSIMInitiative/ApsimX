using System;
using System.Collections.Generic;
using System.Linq;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Functions;
using Models.Interfaces;
using Models.PMF.Interfaces;
using Models.PMF.Organs;
using Models.Soils.Arbitrator;
using Newtonsoft.Json;

namespace Models.PMF.Arbitrator
{
    /// <summary>The method used to do WaterUptake</summary>
    [Serializable]
    [ValidParent(ParentType = typeof(IArbitrator))]
    public class C4NitrogenUptakeMethod : Model, IUptakeMethod
    {
        /// <summary>Reference to Plant to find WaterDemands</summary>
        [Link(Type = LinkType.Ancestor)]
        protected Plant plant = null;

        /// <summary>The zone.</summary>
        [Link(Type = LinkType.Ancestor)]
        protected IZone zone;

        /// <summary>The method used to arbitrate N allocations</summary>
        [Link(Type = LinkType.Ancestor, ByName = true)]
        protected OrganArbitrator Arbitrator = null;

        /// <summary>Accumulated ThermalTime from Simulation start</summary>
        [Link(Type = LinkType.Path, Path = "[Phenology].DltTT")]
        protected IFunction DltTT = null;

        /// <summary>Accumulated ThermalTime from Flowering - uses TTFM during Grainfill</summary>
        [Link(Type = LinkType.Path, Path = "[Phenology].TTFMFromFlowering")]
        protected IFunction TTFMFromFlowering = null;

        /// <summary>ThermalTime after Flowering to stop N Uptake</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction NUptakeCease { get; set; }

        /// <summary>Maximum Nitrogen Uptake Rate</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction MaxNUptakeRate = null;

        /// <summary>Maximum Nitrogen Uptake Rate</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction NSupplyFraction = null;

        /// <summary>Used to calc maximim diffusion rate</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public IFunction MaxDiffusion = null;

        private const double kgha2gsm = 0.1;
        /// <summary>Gets or sets MassFlow during NitrogenUptake Calcs</summary>
        [JsonIgnore]
        public double[] MassFlow { get; private set; }

        /// <summary>Gets or sets Diffusion during NitrogenUptake Calcs</summary>
        [JsonIgnore]
        public double[] Diffusion { get; private set; }

        /// <summary>N Uptaken through Mass Flow</summary>
        [JsonIgnore]
        public double NMassFlowSupply { get; private set; }

        /// <summary>N Teken up through Diffusion.</summary>
        [JsonIgnore]
        public double NDiffusionSupply { get; private set; }

        //cache indexes on start of simulation
        private int grainIndex = 0;
        private int rootIndex = 1;
        private int leafIndex = 2;
        private int stemIndex = 4;

        /// <summary>The method used to arbitrate N allocations</summary>
        public List<ZoneWaterAndN> GetUptakeEstimates(SoilState soilstate, IArbitration[] Organs)
        {
            var N = Arbitrator.N;

            var nSupply = 0.0;//NOTE: This is in kg, not kg/ha, to arbitrate N demands for spatial simulations.

            //this function is called 4 times as part of estimates
            //shouldn't set public variables in here

            var rootDemand = N.StructuralDemand[rootIndex] + N.MetabolicDemand[rootIndex];
            var stemDemand = /*N.StructuralDemand[stemIndex] + */N.MetabolicDemand[stemIndex];
            var leafDemand = N.MetabolicDemand[leafIndex];
            var grainDemand = N.StructuralDemand[grainIndex] + N.MetabolicDemand[grainIndex];
            //have to correct the leaf demand calculation
            var leaf = Organs[leafIndex] as SorghumLeaf;
            var leafAdjustment = leaf.CalculateClassicDemandDelta();

            //double NDemand = (N.TotalPlantDemand - N.TotalReallocation) / kgha2gsm * Plant.Zone.Area; //NOTE: This is in kg, not kg/ha, to arbitrate N demands for spatial simulations.
            //old sorghum uses g/m^2 - need to convert after it is used to calculate actual diffusion
            // leaf adjustment is not needed here because it is an adjustment for structural demand - we only look at metabolic here.

            // dh - In old sorghum, root only has one type of NDemand - it doesn't have a structural/metabolic division.
            // In new apsim, root only uses structural, metabolic is always 0. Therefore, we have to include root's structural
            // NDemand in this calculation.

            // dh - In old sorghum, totalDemand is metabolic demand for all organs. However in new apsim, grain has no metabolic
            // demand, so we must include its structural demand in this calculation.
            double totalDemand = N.TotalMetabolicDemand + N.StructuralDemand[rootIndex] + N.StructuralDemand[grainIndex];
            double nDemand = Math.Max(0, totalDemand - grainDemand); // to replicate calcNDemand in old sorghum 
            List<ZoneWaterAndN> zones = new List<ZoneWaterAndN>();

            foreach (ZoneWaterAndN zone in soilstate.Zones)
            {
                ZoneWaterAndN UptakeDemands = new ZoneWaterAndN(zone.Zone);

                UptakeDemands.NO3N = new double[zone.NO3N.Length];
                UptakeDemands.NH4N = new double[zone.NH4N.Length];
                UptakeDemands.Water = new double[UptakeDemands.NO3N.Length];

                //only using Root to get Nitrogen from - temporary code for sorghum
                var root = Organs[rootIndex] as Root;

                //Get Nuptake supply from each organ and set the PotentialUptake parameters that are passed to the soil arbitrator

                double[] organNO3Supply = new double[zone.NO3N.Length]; //kg/ha - dltNo3 in old apsim
                double[] organNH4Supply = new double[zone.NH4N.Length];

                ZoneState myZone = root.Zones.Find(z => z.Name == zone.Zone.Name);
                if (myZone != null)
                {
                    //update the MassFlow and Diffusion variabes in myZone
                    CalculateNitrogenSupply(myZone, zone);

                    double[] diffnAvailable = new double[myZone.Diffusion.Length];
                    for (var i = 0; i < myZone.Diffusion.Length; ++i)
                    {
                        diffnAvailable[i] = myZone.Diffusion[i] - myZone.MassFlow[i];
                    }
                    var totalMassFlow = MathUtilities.Sum(myZone.MassFlow); //g/m^2
                    var totalDiffusion = MathUtilities.Sum(diffnAvailable);//g/m^2

                    var potentialSupply = totalMassFlow + totalDiffusion;
                    var actualDiffusion = 0.0;

                    if (TTFMFromFlowering.Value() > NUptakeCease.Value())
                        totalMassFlow = 0;
                    var actualMassFlow = totalMassFlow;

                    if (totalMassFlow < nDemand && TTFMFromFlowering.Value() < NUptakeCease.Value()) // fixme && ttElapsed < nUptakeCease
                    {
                        actualDiffusion = MathUtilities.Bound(nDemand - totalMassFlow, 0.0, totalDiffusion);
                        actualDiffusion = MathUtilities.Divide(actualDiffusion, MaxDiffusion.Value(), 0.0);

                        var maxUptakeRateFrac = Math.Min(1.0, (potentialSupply / NSupplyFraction.Value())) * MaxNUptakeRate.Value();
                        var maxUptake = Math.Max(0, maxUptakeRateFrac * DltTT.Value() - actualMassFlow);
                        actualDiffusion = Math.Min(actualDiffusion, maxUptake);
                    }

                    NDiffusionSupply = actualDiffusion;
                    NMassFlowSupply = actualMassFlow;

                    //adjust diffusion values proportionally
                    //make sure organNO3Supply is in kg/ha
                    for (int layer = 0; layer < organNO3Supply.Length; layer++)
                    {
                        var massFlowLayerFraction = MathUtilities.Divide(myZone.MassFlow[layer], totalMassFlow, 0.0);
                        var diffusionLayerFraction = MathUtilities.Divide(diffnAvailable[layer], totalDiffusion, 0.0);
                        organNO3Supply[layer] = (massFlowLayerFraction * actualMassFlow +
                            diffusionLayerFraction * actualDiffusion) / kgha2gsm;  //convert to kg/ha
                    }
                }
                //originalcode
                UptakeDemands.NO3N = MathUtilities.Add(UptakeDemands.NO3N, organNO3Supply); //Add uptake supply from each organ to the plants total to tell the Soil arbitrator
                if (UptakeDemands.NO3N.Any(n => MathUtilities.IsNegative(n)))
                    throw new Exception("-ve no3 uptake demand");
                UptakeDemands.NH4N = MathUtilities.Add(UptakeDemands.NH4N, organNH4Supply);

                N.UptakeSupply[rootIndex] += MathUtilities.Sum(organNO3Supply) * kgha2gsm * zone.Zone.Area / this.zone.Area;  //g/m2
                if (MathUtilities.IsNegative(N.UptakeSupply[rootIndex]))
                    throw new Exception($"-ve uptake supply for organ {(Organs[rootIndex] as IModel).Name}");
                nSupply += MathUtilities.Sum(organNO3Supply) * zone.Zone.Area;
                zones.Add(UptakeDemands);
            }

            return zones;
        }

        private void CalculateNitrogenSupply(ZoneState myZone, ZoneWaterAndN zone)
        {
            var soilPhysical = myZone.Soil.FindChild<Soils.IPhysical>();

            myZone.MassFlow = new double[soilPhysical.Thickness.Length];
            myZone.Diffusion = new double[soilPhysical.Thickness.Length];

            int currentLayer = SoilUtilities.LayerIndexOfDepth(myZone.Physical.Thickness, myZone.Depth);
            for (int layer = 0; layer <= currentLayer; layer++)
            {
                var swdep = myZone.StartWater[layer]; //mm
                var dltSwdep = myZone.WaterUptake[layer];

                //NO3N is in kg/ha - old sorghum used g/m^2
                var no3conc = MathUtilities.Divide(zone.NO3N[layer] * kgha2gsm, swdep, 0);
                var no3massFlow = no3conc * (-dltSwdep);
                myZone.MassFlow[layer] = Math.Min(no3massFlow, zone.NO3N[layer] * kgha2gsm);

                //diffusion
                var swAvailFrac = MathUtilities.Divide(myZone.AvailableSW[layer], myZone.PotentialAvailableSW[layer], 0);
                //old sorghum stores N03 in g/ms not kg/ha
                var no3Diffusion = MathUtilities.Bound(swAvailFrac, 0.0, 1.0) * (zone.NO3N[layer] * kgha2gsm);

                myZone.Diffusion[layer] = Math.Min(no3Diffusion, zone.NO3N[layer] * kgha2gsm) * myZone.RootProportions[layer];
            }
        }
        /// Calculating the actual water uptake across all zones.
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
                N.UptakeSupply[i] = NSupply / zone.Area * N.UptakeSupply[i] / N.TotalUptakeSupply * kgha2gsm;
        }

        /// <summary>Called when crop is sown</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantSowing")]
        virtual protected void OnPlantSowing(object sender, SowingParameters data)
        {
            NDiffusionSupply = 0.0;
            NMassFlowSupply = 0.0;

            var organNames = Arbitrator.OrganNames;
            grainIndex = organNames.IndexOf("Grain");
            rootIndex = organNames.IndexOf("Root");
            leafIndex = organNames.IndexOf("Leaf");
            stemIndex = organNames.IndexOf("Stem");
        }
    }
}
