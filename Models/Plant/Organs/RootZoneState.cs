using Models.Soils;
using Models.Core;
using System;
using Models.Functions;
using Models.Interfaces;
using System.Linq;
using Models.Soils.Standardiser;
using APSIM.Shared.Utilities;
using Models.PMF.Interfaces;

namespace Models.PMF.Organs
{
    /// <summary>The state of each zone that root knows about.</summary>
    [Serializable]
    public class ZoneState
    {
        /// <summary>The soil in this zone</summary>
        public Soil soil = null;

        /// <summary>The NO3 solute.</summary>
        public ISolute NO3 = null;

        /// <summary>The NH4 solute.</summary>
        public ISolute NH4 = null;

        /// <summary>The parent plant</summary>
        private Plant plant = null;

        /// <summary>The root organ</summary>
        private  Root root = null;

        /// <summary>The root front velocity function</summary>
        private IFunction rootFrontVelocity;

        /// <summary>The Maximum Root Depth</summary>
        private IFunction maximumRootDepth = null;

        /// <summary>The cost for remobilisation</summary>
        private IFunction remobilisationCost = null;

        /// <summary>Zone name</summary>
        public string Name = null;

        /// <summary>The water uptake</summary>
        public double[] WaterUptake { get; set; }

        /// <summary>The delta n h4</summary>
        public double[] DeltaNH4 { get; set; }

        /// <summary>The delta n o3</summary>
        public double[] DeltaNO3 { get; set; }

        /// <summary>Holds actual DM allocations to use in allocating N to structural and Non-Structural pools</summary>
        [Units("g/2")]
        public double[] DMAllocated { get; set; }

        /// <summary>Holds potential DM allocations to use in allocating N to structural and Non-Structural pools</summary>
        [Units("g/2")]
        public double[] PotentialDMAllocated { get; set; }

        /// <summary>Demand for structural N, set when Ndemand is called and used again in N allocation</summary>
        [Units("g/2")]
        public double[] StructuralNDemand { get; set; }

        /// <summary>Demand for Non-structural N, set when Ndemand is called and used again in N allocation</summary>
        [Units("g/m2")]
        public double[] StorageNDemand { get; set; }

        /// <summary>Demand for Metabolic N, set when Ndemand is called and used again in N allocation</summary>
        [Units("g/m2")]
        public double[] MetabolicNDemand { get; set; }

        /// <summary>The Nuptake</summary>
        public double[] NitUptake { get; set; }

        /// <summary>Gets or sets the nuptake supply.</summary>
        public double NuptakeSupply { get; set; }

        /// <summary>Gets or sets the layer live.</summary>
        public Biomass[] LayerLive { get; set; }

        /// <summary>Gets or sets the layer dead.</summary>
        public Biomass[] LayerDead { get; set; }

        /// <summary>Gets or sets the length.</summary>
        public double Length { get; set; }

        /// <summary>Gets or sets the depth.</summary>
        [Units("mm")]
        public double Depth { get; set; }

        /// <summary>Gets the RootFront</summary>
        public double RootFront { get; set; }
        /// <summary>Gets the RootFront</summary>
        public double RootSpread { get; set; }
        /// <summary>Gets the RootFront</summary>
        public double LeftDist { get; set; }
        /// <summary>Gets the RootFront</summary>
        public double RightDist { get; set; }

        /// <summary>Gets or sets AvailableSW during SW Uptake
        /// Old Sorghum does actual uptake at end of day
        /// PMF does actual uptake before N uptake</summary>
        public double[] AvailableSW { get;  set; }

        /// <summary>Gets or sets PotentialAvailableSW during SW Uptake</summary>
        public double[] PotentialAvailableSW { get;  set; }

        /// <summary>Record the Water level before </summary>
        public double[] StartWater { get; set; }

        /// <summary>Record the Water Supply before </summary>
        public double[] Supply { get; set; }

        /// <summary>Gets or sets MassFlow during NitrogenUptake Calcs</summary>
        public double[] MassFlow { get;  set; }

        /// <summary>Gets or sets Diffusion during NitrogenUptake Calcs</summary>
        public double[] Diffusion { get;  set; }


        /// <summary>Constructor</summary>
        /// <param name="Plant">The parant plant</param>
        /// <param name="Root">The parent root organ</param>
        /// <param name="soil">The soil in the zone.</param>
        /// <param name="depth">Root depth (mm)</param>
        /// <param name="initialDM">Initial dry matter</param>
        /// <param name="population">plant population</param>
        /// <param name="maxNConc">maximum n concentration</param>
        /// <param name="rfv">Root front velocity</param>
        /// <param name="mrd">Maximum root depth</param>
        /// <param name="remobCost">Remobilisation cost</param>
        public ZoneState(Plant Plant, Root Root, Soil soil, double depth,
                         BiomassDemand initialDM, double population, double maxNConc,
                         IFunction rfv, IFunction mrd, IFunction remobCost)
        {
            this.soil = soil;
            this.plant = Plant;
            this.root = Root;
            this.rootFrontVelocity = rfv;
            this.maximumRootDepth = mrd;
            this.remobilisationCost = remobCost;

            Clear();
            Zone zone = Apsim.Parent(soil, typeof(Zone)) as Zone;
            if (zone == null)
                throw new Exception("Soil " + soil + " is not in a zone.");
            NO3 = Apsim.Find(zone, "NO3") as ISolute;
            NH4 = Apsim.Find(zone, "NH4") as ISolute;
            Name = zone.Name;
            Initialise(depth, initialDM, population, maxNConc);
        }

        /// <summary>Initialise the zone.</summary>
        /// <param name="depth">Root depth (mm)</param>
        /// <param name="initialDM">Initial dry matter</param>
        /// <param name="population">plant population</param>
        /// <param name="maxNConc">maximum n concentration</param>
        public void Initialise(double depth, BiomassDemand initialDM, double population, double maxNConc)
        {
            Depth = depth;
            RootFront = depth;
            //distribute root biomass evenly through root depth
            double[] fromLayer = new double[1] { depth };
            double[] fromStructural = new double[1] { initialDM.Structural.Value() };
            double[] toStructural = Layers.MapMass(fromStructural, fromLayer, soil.Thickness);
            double[] fromMetabolic = new double[1] { initialDM.Metabolic.Value() };
            double[] toMetabolic = Layers.MapMass(fromMetabolic, fromLayer, soil.Thickness);
            double[] fromStorage = new double[1] { initialDM.Storage.Value() };
            double[] toStorage = Layers.MapMass(fromStorage, fromLayer, soil.Thickness);

            for (int layer = 0; layer < soil.Thickness.Length; layer++)
            {
                LayerLive[layer].StructuralWt = toStructural[layer] * population;
                LayerLive[layer].MetabolicWt = toMetabolic[layer] * population;
                LayerLive[layer].StorageWt = toStorage[layer] * population;
                LayerLive[layer].StructuralN = LayerLive[layer].StructuralWt * maxNConc;
            }

            if (plant.SowingData != null)
            {
                if (plant.SowingData.SkipType == 0)
                {
                    LeftDist = plant.SowingData.RowSpacing * 0.5;
                    RightDist = plant.SowingData.RowSpacing * 0.5;
                }
                if (plant.SowingData.SkipType == 1)
                {
                    LeftDist = plant.SowingData.RowSpacing * 1.0;
                    RightDist = plant.SowingData.RowSpacing * 1.0;
                }
                if (plant.SowingData.SkipType == 2)
                {
                    LeftDist = plant.SowingData.RowSpacing * 1.0;
                    RightDist = plant.SowingData.RowSpacing * 0.5;
                }
                if (plant.SowingData.SkipType == 3)
                {
                    LeftDist = plant.SowingData.RowSpacing * 1.5;
                    RightDist = plant.SowingData.RowSpacing * 0.5;
                }
            }
        }

        /// <summary>Clears this instance.</summary>
        public void Clear()
        {
            WaterUptake = null;
            NitUptake = null;
            DeltaNO3 = new double[soil.Thickness.Length];
            DeltaNH4 = new double[soil.Thickness.Length];

            Depth = 0.0;

            if (LayerLive == null || LayerLive.Length == 0)
            {
                LayerLive = new Biomass[soil.Thickness.Length];
                LayerDead = new Biomass[soil.Thickness.Length];
                for (int i = 0; i < soil.Thickness.Length; i++)
                {
                    LayerLive[i] = new Biomass();
                    LayerDead[i] = new Biomass();
                }
            }
            else
            {
                for (int i = 0; i < soil.Thickness.Length; i++)
                {
                    LayerLive[i].Clear();
                    LayerDead[i].Clear();
                }
            }
        }
        /// <summary>
        /// Growth depth of roots in this zone
        /// </summary>
        public void GrowRootDepth()
        {
            // Do Root Front Advance
            int RootLayer = soil.LayerIndexOfDepth(Depth);

            //sorghum calc
            var rootDepthWaterStress = 1.0;
            if (root.RootDepthStressFactor != null)
                rootDepthWaterStress = root.RootDepthStressFactor.Value(RootLayer);

            double MaxDepth;
            double[] xf = null;
            if (soil.Weirdo == null)
            {
                var soilCrop = soil.Crop(plant.Name);
                xf = soilCrop.XF;
                var rootfrontvelocity = rootFrontVelocity.Value(RootLayer);
                var dltRoot = rootFrontVelocity.Value(RootLayer) * xf[RootLayer] * rootDepthWaterStress;
                Depth = Depth + rootFrontVelocity.Value(RootLayer) * xf[RootLayer] * rootDepthWaterStress;
                MaxDepth = 0;
                // Limit root depth for impeded layers
                for (int i = 0; i < soil.Thickness.Length; i++)
                {
                    if (xf[i] > 0)
                        MaxDepth += soil.Thickness[i];
                    else
                        break;
                }
            }
            else
            {
                Depth = Depth + rootFrontVelocity.Value(RootLayer);
                MaxDepth = soil.Thickness.Sum();
            }

            // Limit root depth for the crop specific maximum depth
            MaxDepth = Math.Min(maximumRootDepth.Value(), MaxDepth);
            Depth = Math.Min(Depth, MaxDepth);

            //RootFront - needed by sorghum
            if(root.RootFrontCalcSwitch?.Value() == 1)
            {
                var dltRootFront = rootFrontVelocity.Value(RootLayer) * rootDepthWaterStress * xf[RootLayer];

                double maxFront = Math.Sqrt(Math.Pow(Depth, 2) + Math.Pow(LeftDist, 2));
                dltRootFront = Math.Min(dltRootFront, maxFront - RootFront);
                RootFront = RootFront + dltRootFront;
            }
        }
        /// <summary>
        /// Calculate Root Activity Values for water and nitrogen
        /// </summary>
        public double[] CalculateRootActivityValues()
        {
            double[] RAw = new double[soil.Thickness.Length];
            for (int layer = 0; layer < soil.Thickness.Length; layer++)
            {
                if (layer <= soil.LayerIndexOfDepth(Depth))
                    if (LayerLive[layer].Wt > 0)
                    {
                        RAw[layer] = - WaterUptake[layer] / LayerLive[layer].Wt
                                   * soil.Thickness[layer]
                                   * soil.ProportionThroughLayer(layer, Depth);
                        RAw[layer] = Math.Max(RAw[layer], 1e-20);  // Make sure small numbers to avoid lack of info for partitioning
                    }
                    else if (layer > 0)
                        RAw[layer] = RAw[layer - 1];
                    else
                        RAw[layer] = 0;
            }
            return RAw;
        }

        /// <summary>
        /// Partition root mass into layers
        /// </summary>
        public void PartitionRootMass(double TotalRAw, double TotalDMAllocated)
        {
            DMAllocated = new double[soil.Thickness.Length];

            if (Depth > 0)
            {
                double[] RAw = CalculateRootActivityValues();

                for (int layer = 0; layer < soil.Thickness.Length; layer++)
                    if (TotalRAw > 0)
                    {
                        LayerLive[layer].StructuralWt += TotalDMAllocated * RAw[layer] / TotalRAw;
                        DMAllocated[layer] += TotalDMAllocated * RAw[layer] / TotalRAw;
                    }
            }
        }
    }


}
