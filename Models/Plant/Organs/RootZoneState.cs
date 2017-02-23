using Models.Soils;
using Models.Core;
using System;

namespace Models.PMF.Organs
{
    /// <summary>The state of each zone that root knows about.</summary>
    [Serializable]
    public class ZoneState
    {
        /// <summary>The soil in this zone</summary>
        public Soil soil = null;

        /// <summary>The parent plant</summary>
        private Plant plant = null;

        /// <summary>The root organ</summary>
        private  Root root = null;

        /// <summary>Zone name</summary>
        public string Name = null;

        /// <summary>The uptake</summary>
        public double[] Uptake { get; set; }

        /// <summary>The delta n h4</summary>
        public double[] DeltaNH4 { get; set; }

        /// <summary>The delta n o3</summary>
        public double[] DeltaNO3 { get; set; }

        /// <summary>Holds actual DM allocations to use in allocating N to structural and Non-Structural pools</summary>
        [Units("g/2")]
        public double[] DMAllocated { get; set; }

        /// <summary>Demand for structural N, set when Ndemand is called and used again in N allocation</summary>
        [Units("g/2")]
        public double[] StructuralNDemand { get; set; }

        /// <summary>Demand for Non-structural N, set when Ndemand is called and used again in N allocation</summary>
        [Units("g/m2")]
        public double[] NonStructuralNDemand { get; set; }

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

        /// <summary>Constructor</summary>
        /// <param name="Plant">The parant plant</param>
        /// <param name="Root">The parent root organ</param>
        /// <param name="soil">The soil in the zone.</param>
        /// <param name="depth">Root depth (mm)</param>
        /// <param name="initialDM">Initial dry matter</param>
        /// <param name="population">plant population</param>
        /// <param name="maxNConc">maximum n concentration</param>
        public ZoneState(Plant Plant, Root Root, Soil soil, double depth, double initialDM, double population, double maxNConc)
        {
            this.soil = soil;
            this.plant = Plant;
            this.root = Root;
            Clear();
            Zone zone = Apsim.Parent(soil, typeof(Zone)) as Zone;
            if (zone == null)
                throw new Exception("Soil " + soil + " is not in a zone.");
            Name = zone.Name;
            Initialise(depth, initialDM, population, maxNConc);
        }

        /// <summary>Initialise the zone.</summary>
        /// <param name="depth">Root depth (mm)</param>
        /// <param name="initialDM">Initial dry matter</param>
        /// <param name="population">plant population</param>
        /// <param name="maxNConc">maximum n concentration</param>
        public void Initialise(double depth, double initialDM, double population, double maxNConc)
        {
            Depth = depth;
            double AccumulatedDepth = 0;
            double InitialLayers = 0;
            for (int layer = 0; layer < soil.Thickness.Length; layer++)
            {
                if (AccumulatedDepth < Depth)
                    InitialLayers += 1;
                AccumulatedDepth += soil.Thickness[layer];
            }
            for (int layer = 0; layer < soil.Thickness.Length; layer++)
            {
                if (layer <= InitialLayers - 1)
                {
                    //distribute root biomass evenly through root depth
                    LayerLive[layer].StructuralWt = initialDM / InitialLayers * population;
                    LayerLive[layer].StructuralN = initialDM / InitialLayers * maxNConc * population;
                }
            }
        }

        /// <summary>Clears this instance.</summary>
        public void Clear()
        {
            Uptake = null;
            NitUptake = null;
            DeltaNO3 = new double[soil.Thickness.Length];
            DeltaNH4 = new double[soil.Thickness.Length];

            Length = 0.0;
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
            int RootLayer = Soil.LayerIndexOfDepth(Depth, soil.Thickness);

            SoilCrop crop = soil.Crop(plant.Name) as SoilCrop;
            if (soil.WEIRDO == null)
                Depth = Depth + root.RootFrontVelocity.Value * crop.XF[RootLayer];
            else
                Depth = Depth + root.RootFrontVelocity.Value;


            // Limit root depth for impeded layers
            double MaxDepth = 0;
            for (int i = 0; i < soil.Thickness.Length; i++)
            {
                if (soil.WEIRDO == null)
                {
                    if (crop.XF[i] > 0)
                        MaxDepth += soil.Thickness[i];
                }
                else
                    MaxDepth += soil.Thickness[i];
            }
            // Limit root depth for the crop specific maximum depth
            MaxDepth = Math.Min(root.MaximumRootDepth.Value, MaxDepth);

            Depth = Math.Min(Depth, MaxDepth);

        }
        /// <summary>
        /// Calculate Root Activity Values for water and nitrogen
        /// </summary>
        public double[] CalculateRootActivityValues()
        {
            double[] RAw = new double[soil.Thickness.Length];
            for (int layer = 0; layer < soil.Thickness.Length; layer++)
            {
                if (layer <= Soil.LayerIndexOfDepth(Depth, soil.Thickness))
                    if (LayerLive[layer].Wt > 0)
                    {
                        RAw[layer] = Uptake[layer] / LayerLive[layer].Wt
                                   * soil.Thickness[layer]
                                   * Soil.ProportionThroughLayer(layer, Depth, soil.Thickness);
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
