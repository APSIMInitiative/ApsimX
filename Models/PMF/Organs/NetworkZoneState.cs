﻿using System;
using System.Linq;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Models.Soils;

namespace Models.PMF.Organs
{
    /// <summary>The state of each zone that root knows about.</summary>
    [Serializable]
    public class NetworkZoneState : Model, IRootGeometryData
    {
        /// <summary>The soil in this zone</summary>
        public Soil Soil { get; set; }


        /// <summary>The soil in this zone</summary>
        public IPhysical Physical { get; set; }

        /// <summary>The water balance in this zone</summary>
        public ISoilWater WaterBalance { get; set; }

        /// <summary>The NO3 solute.</summary>
        public ISolute NO3 = null;

        /// <summary>The NH4 solute.</summary>
        public ISolute NH4 = null;

        /// <summary>The parent plant</summary>
        public Plant plant { get; set; }

        private RootNetwork parentNetwork { get; set; }

        /// <summary>Is the Weirdo model present in the simulation?</summary>
        public bool IsWeirdoPresent { get; set; }

        /// <summary>Zone name</summary>
        new public string Name { get; set; }

        /// <summary>The water uptake</summary>
        public double[] WaterUptake { get; set; }

        /// <summary>The delta n h4</summary>
        public double[] DeltaNH4 { get; set; }

        /// <summary>The delta n o3</summary>
        public double[] DeltaNO3 { get; set; }

        /// <summary>The Nuptake</summary>
        public double[] NitUptake { get; set; }

        /// <summary>Gets or sets the nuptake supply.</summary>
        public double NuptakeSupply { get; set; }

        /// <summary>Gets or sets the layer live.</summary>
        public OrganNutrientsState[] LayerLive { get; set; }

        /// <summary>Gets or sets the layer dead.</summary>
        public OrganNutrientsState[] LayerDead { get; set; }

        /// <summary>Gets or sets the layer live.</summary>
        public OrganNutrientsState[] LayerLiveProportion { get; set; }

        /// <summary>Gets or sets the layer dead.</summary>
        public OrganNutrientsState[] LayerDeadProportion { get; set; }

        /// <summary>Gets or sets the depth.</summary>
        [Units("mm")]
        public double Depth { get; set; }

        /// <summary>Gets the RootFront</summary>
        public double RootLength { get { return Depth - plant.SowingData.Depth; } }

        /// <summary>Gets the RootFront</summary>
        public double RootFront { get; set; }
        /// <summary>Gets the RootFront</summary>
        public double RootSpread { get; set; }
        /// <summary>Gets the RootFront</summary>
        public double LeftDist { get; set; }
        /// <summary>Gets the RootFront</summary>
        public double RightDist { get; set; }

        /// <summary>Gets the RootProportions</summary>
        public double[] RootProportions { get; set; }

        /// <summary>Gets the LLModifier for leaf angles != RootAngleBase</summary>
        public double[] LLModifier { get; set; }

        /// <summary>Soil area occipied by roots</summary>
        [Units("m2")]
        public double RootArea { get; set; }

        /// <summary>Gets or sets AvailableSW during SW Uptake
        /// Old Sorghum does actual uptake at end of day
        /// PMF does actual uptake before N uptake</summary>
        public double[] AvailableSW { get; set; }

        /// <summary>Gets or sets PotentialAvailableSW during SW Uptake</summary>
        public double[] PotentialAvailableSW { get; set; }

        /// <summary>Record the Water level before </summary>
        public double[] StartWater { get; set; }

        /// <summary>Record the Water Supply before </summary>
        public double[] Supply { get; set; }

        /// <summary>Gets or sets MassFlow during NitrogenUptake Calcs</summary>
        public double[] MassFlow { get; set; }

        /// <summary>Gets or sets Diffusion during NitrogenUptake Calcs</summary>
        public double[] Diffusion { get; set; }

        private double[] xf { get; set; }

        private double maxDepth { get; set; }

        /// <summary>The activity of roots in a layer relative to all other layers</summary>
        public double[] RAw { get; set; }



        /// <summary>Constructor</summary>
        /// <param name="Plant">The parant plant</param>
        /// <param name="soil">The soil in the zone.</param>
        public NetworkZoneState(Plant Plant, Soil soil)
        {
            this.Soil = soil;
            this.plant = Plant;
            this.parentNetwork = Plant.FindDescendant<RootNetwork>();
            Physical = soil.FindChild<IPhysical>();
            WaterBalance = soil.FindChild<ISoilWater>();
            IsWeirdoPresent = soil.FindChild("Weirdo") != null;

            Clear();
            Zone zone = soil.FindAncestor<Zone>();
            if (zone == null)
                throw new Exception("Soil " + soil + " is not in a zone.");
            NO3 = zone.FindInScope<ISolute>("NO3");
            NH4 = zone.FindInScope<ISolute>("NH4");
            Name = zone.Name;
        }


        /// <summary>Determine if XF constrains root growth to a maximum depth.</summary>
        public void SetMaxDepthFromXF()
        {
            var soilCrop = Soil.FindDescendant<SoilCrop>(plant.Name + "Soil");
            if (soilCrop == null)
                throw new Exception($"Cannot find a soil crop parameterisation called {plant.Name}Soil");

            xf = soilCrop.XF;

            for (int i = 0; i < Physical.Thickness.Length; i++)
            {
                if (xf[i] > 0)
                    maxDepth += Physical.Thickness[i];
                else
                    break;
            }
        }

        /// <summary>Calculate starting states.</summary>
        public void Initialize(double depth)
        {
            Clear();
            Depth = depth;
            RootFront = depth;
            SetMaxDepthFromXF();
            CalculateRAw();
            for (int layer = 0; layer < Physical.Thickness.Length; layer++)
            {
                LLModifier[layer] = 1;
            }
        }


        /// <summary>Calculate RAw for each layer.</summary>
        public void CalculateRAw()
        {
            // Set root activity functions.
            RAw = new double[Physical.Thickness.Length];
            for (int layer = 0; layer < Physical.Thickness.Length; layer++)
            {
                if (layer <= SoilUtilities.LayerIndexOfDepth(Physical.Thickness, Depth))
                    if (LayerLive[layer].Wt > 0)
                    {
                        RAw[layer] = -WaterUptake[layer] / LayerLive[layer].Wt
                                   * Physical.Thickness[layer]
                                   * SoilUtilities.ProportionThroughLayer(Physical.Thickness, layer, Depth);
                        RAw[layer] = Math.Max(RAw[layer], 1e-20);  // Make sure small numbers to avoid lack of info for partitioning
                    }
                    else if (layer > 0)
                        RAw[layer] = RAw[layer - 1];
                    else
                        RAw[layer] = 0;
            }
        }

        /// <summary>
        /// Calculates the proportion of total root biomass in each zone layer
        /// </summary>
        public void CalculateRelativeBiomassProportions()
        {
            for (int i = 0; i < Physical.Thickness.Length; i++)
            {
                LayerLiveProportion[i] = LayerLive[i] / parentNetwork.parentOrgan.Live;
                LayerDeadProportion[i] = LayerDead[i] / parentNetwork.parentOrgan.Dead;
            }
        }

        /// <summary>Clears this instance.</summary>
        public void Clear()
        {
            WaterUptake = null;
            NitUptake = null;
            DeltaNO3 = new double[Physical.Thickness.Length];
            DeltaNH4 = new double[Physical.Thickness.Length];
            RootProportions = new double[Physical.Thickness.Length];
            LLModifier = new double[Physical.Thickness.Length];

            Depth = 0.0;

            if (LayerLive == null || LayerLive.Length == 0)
            {
                LayerLive = new OrganNutrientsState[Physical.Thickness.Length];
                LayerDead = new OrganNutrientsState[Physical.Thickness.Length];
                LayerLiveProportion = new OrganNutrientsState[Physical.Thickness.Length];
                LayerDeadProportion = new OrganNutrientsState[Physical.Thickness.Length];
                double rootCconc = parentNetwork.parentOrgan.Cconc;
                for (int i = 0; i < Physical.Thickness.Length; i++)
                {
                    LayerLive[i] = new OrganNutrientsState();
                    LayerDead[i] = new OrganNutrientsState();
                    LayerLiveProportion[i] = new OrganNutrientsState();
                    LayerDeadProportion[i] = new OrganNutrientsState();
                }
            }
            else
            {
                for (int i = 0; i < Physical.Thickness.Length; i++)
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
            int RootLayer = SoilUtilities.LayerIndexOfDepth(Physical.Thickness, Depth);
            var rootfrontvelocity = parentNetwork.RootFrontVelocity;
            var rootDepthWaterStress = parentNetwork.RootDepthStressFactor.Value(RootLayer);

            double MaxDepth;
            double[] xf = null;
            if (!IsWeirdoPresent)
            {
                var soilCrop = Soil.FindDescendant<SoilCrop>(plant.Name + "Soil");
                if (soilCrop == null)
                    throw new Exception($"Cannot find a soil crop parameterisation called {plant.Name}Soil");

                xf = soilCrop.XF;

                Depth = Depth + rootfrontvelocity * xf[RootLayer] * rootDepthWaterStress;
                MaxDepth = 0;
                // Limit root depth for impeded layers
                for (int i = 0; i < Physical.Thickness.Length; i++)
                {
                    if (xf[i] > 0)
                        MaxDepth += Physical.Thickness[i];
                    else
                        break;
                }
            }
            else
            {
                Depth = Depth + rootfrontvelocity;
                MaxDepth = Physical.Thickness.Sum();
            }

            // Limit root depth for the crop specific maximum depth
            MaxDepth = Math.Min(parentNetwork.MaximumRootDepth, MaxDepth);
            Depth = Math.Min(Depth, MaxDepth);

            //RootFront - needed by sorghum
            if (parentNetwork.RootFrontCalcSwitch?.Value() == 1)
            {
                var dltRootFront = rootfrontvelocity * xf[RootLayer] * parentNetwork.RootDepthStressFactor.Value();

                double maxFront = Math.Sqrt(Math.Pow(Depth, 2) + Math.Pow(LeftDist, 2));
                dltRootFront = Math.Min(dltRootFront, maxFront - RootFront);
                RootFront += dltRootFront;
            }
            else
            {
                RootFront = Depth;
            }
            parentNetwork.RootShape.CalcRootProportionInLayers(this);
        }
    }
}
