namespace Models.AgPasture
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Interfaces;
    using Models.PMF;
    using Models.Soils;
    using Models.Soils.Arbitrator;
    using Models.Soils.Nutrients;

    /// <summary>Describes a generic below ground organ of a pasture species.</summary>
    [Serializable]
    public class PastureBelowGroundOrgan : Model
    {
        /// <summary>Nutrient model.</summary>
        [Link(Type = LinkType.Ancestor)]
        private PastureSpecies species = null;

        /// <summary>Collection of tissues for this organ.</summary>
        [Link(Type = LinkType.Child)]
        private RootTissue[] tissue = null;

        /// <summary>Soil object where these roots are growing.</summary>
        private Soil soil = null;

        /// <summary>Soil physical parameterisation.</summary>
        private IPhysical soilPhysical = null;

        /// <summary>Soil-plant parameterisation.</summary>
        private SoilCrop soilCropData;

        /// <summary>Water balance model.</summary>
        private ISoilWater waterBalance = null;

        /// <summary>Soil nutrient model.</summary>
        private INutrient nutrient;

        private double[] dulMM;
        private double[] ll15MM;

        /// <summary>NO3 solute in the soil.</summary>
        private ISolute no3 = null;

        /// <summary>NH4 solute in the soil.</summary>
        private ISolute nh4 = null;

        //---------------------------- Parameters -----------------------

        /// <summary>Minimum rooting depth (mm).</summary>
        public double RootDepthMinimum { get; set; } = 50.0;

        /// <summary>Maximum rooting depth (mm).</summary>
        public double RootDepthMaximum { get; set; } = 750.0;

        /// <summary>Daily root elongation rate at optimum temperature (mm/day).</summary>
        [Units("mm/day")]
        public double RootElongationRate { get; set; } = 25.0;

        /// <summary>Depth from surface where root proportion starts to decrease (mm).</summary>
        [Units("mm")]
        public double RootDistributionDepthParam { get; set; } = 90.0;

        /// <summary>Exponent controlling the root distribution as function of depth (>0.0).</summary>
        [Units("-")]
        public double RootDistributionExponent { get; set; } = 3.2;

        /// <summary>Factor for root distribution; controls where the function is zero below maxRootDepth.</summary>
        public double RootBottomDistributionFactor { get; set; } = 1.05;

        /// <summary>Specific root length (m/gDM).</summary>
        public double SpecificRootLength { get; set; } = 100.0;

        /// <summary>N concentration for optimum growth (kg/kg).</summary>
        public double NConcOptimum { get; set; } = 0.02;

        /// <summary>Minimum N concentration, structural N (kg/kg).</summary>
        public double NConcMinimum { get; set; } = 0.006;

        /// <summary>Maximum N concentration, for luxury uptake (kg/kg).</summary>
        public double NConcMaximum { get; set; } = 0.025;

        /// <summary>Ammonium uptake coefficient (/ppm).</summary>
        public double KNH4 { get; set; } = 0.01;

        /// <summary>Nitrate uptake coefficient (/ppm).</summary>
        public double KNO3 { get; set; } = 0.02;

        /// <summary>Maximum daily amount of N that can be taken up by the plant (kg/ha).</summary>
        public double MaximumNUptake { get; set; } = 10.0;

        /// <summary>Exponent controlling the effect of soil moisture variations on water extractability.</summary>
        public double ExponentSoilMoisture = 1.50;

        /// <summary>Minimum DM amount of live tissues (kg/ha).</summary>
        private double minimumLiveDM = 0.0;

        //----------------------- States -----------------------

        /// <summary>Rooting depth (mm).</summary>
        public double Depth { get; set; }

        /// <summary>Soil layer at the bottom of the root zone.</summary>
        internal int BottomLayer { get; private set; }

        /// <summary>Target (idealised) DM fractions for each layer (0-1).</summary>
        internal double[] TargetDistribution { get; set; }

        /// <summary>Root live tissue.</summary>
        public RootTissue Live { get; private set; }

        /// <summary>Root dead tissue.</summary>
        public RootTissue Dead { get; private set; }

        /// <summary>Total dry matter in this organ (kg/ha).</summary>
        internal double DMTotal
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < tissue.Length; t++)
                    result += tissue[t].DM.Wt;

                return result;
            }
        }

        /// <summary>Dry matter in the live (green) tissues (kg/ha).</summary>
        internal double DMLive
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < tissue.Length - 1; t++)
                    result += tissue[t].DM.Wt;

                return result;
            }
        }

        /// <summary>Dry matter in the dead tissues (kg/ha).</summary>
        /// <remarks>Last tissue is assumed to represent dead material.</remarks>
        internal double DMDead
        {
            get { return tissue[tissue.Length - 1].DM.Wt; }
        }

        /// <summary>Proportion of dry matter in each soil layer (0-1).</summary>
        internal double[] DMFractions
        {
            get
            {
                double[] result = new double[soilPhysical.Thickness.Length];
                for (int layer = 0; layer < soilPhysical.Thickness.Length; layer++)
                    result[layer] = tissue[0].FractionWt[layer];

                return result;
            }
        }

        /// <summary>Total N amount in this organ (kg/ha).</summary>
        internal double NTotal
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < tissue.Length; t++)
                    result += tissue[t].DM.N;

                return result;
            }
        }

        /// <summary>N amount in the live (green) tissues (kg/ha).</summary>
        internal double NLive
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < tissue.Length - 1; t++)
                    result += tissue[t].DM.N;

                return result;
            }
        }

        /// <summary>N amount in the dead tissues (kg/ha).</summary>
        /// <remarks>Last tissues is assumed to represent dead material.</remarks>
        internal double NDead
        {
            get { return tissue[tissue.Length - 1].DM.N; }
        }

        /// <summary>Average N concentration in this organ (kg/kg).</summary>
        internal double NconcTotal
        {
            get { return MathUtilities.Divide(NTotal, DMTotal, 0.0); }
        }

        /// <summary>Average N concentration in the live tissues (kg/kg).</summary>
        internal double NconcLive
        {
            get { return MathUtilities.Divide(NLive, DMLive, 0.0); }
        }

        /// <summary>Average N concentration in dead tissues (kg/kg).</summary>
        internal double NconcDead
        {
            get { return MathUtilities.Divide(NDead, DMDead, 0.0); }
        }

        /// <summary>Amount of senesced N available for remobilisation (kg/ha).</summary>
        internal double NSenescedRemobilisable
        {
            get { return tissue[tissue.Length - 1].NRemobilisable; }
        }

        /// <summary>Amount of luxury N available for remobilisation (kg/ha).</summary>
        internal double NLuxuryRemobilisable
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < tissue.Length - 1; t++)
                    result += tissue[t].NRemobilisable;

                return result;
            }
        }

        /// <summary>Root length density by volume (mm/mm^3).</summary>
        public double[] LengthDensity
        {
            get
            {
                double[] result = new double[nLayers];
                double totalRootLength = tissue[0].DM.Wt * SpecificRootLength * 0.1; // m root/m2 
                totalRootLength *= 0.001; // convert into mm root/mm2 soil)
                for (int layer = 0; layer < result.Length; layer++)
                {
                    result[layer] = tissue[0].FractionWt[layer] * totalRootLength / soilPhysical.Thickness[layer];
                }
                return result;
            }
        }

        /// <summary>N remobilised from live tissue.</summary>
        public double NLiveRemobilisable { get { return tissue[0].NRemobilisable; } }

        /// <summary>Amount of plant available water in the soil (mm).</summary>
        internal double[] mySoilWaterAvailable { get; private set; }

        /// <summary>Amount of NH4-N in the soil available to the plant (kg/ha).</summary>
        internal double[] mySoilNH4Available { get; private set; }

        /// <summary>Amount of NO3-N in the soil available to the plant (kg/ha).</summary>
        internal double[] mySoilNO3Available { get; private set; }

        /// <summary>Returns true if the KL modifier due to root damage is active or not.</summary>
        private bool IsKLModiferDueToDamageActive { get; set; } = false;

        /// <summary>Name of zone where roots are growing.</summary>
        private string zoneName;

        /// <summary>Number of layers in the soil.</summary>
        private int nLayers;

        //----------------------- Public methods -----------------------

        /// <summary>Constructor, initialise tissues for the roots.</summary>
        /// <param name="zone">The zone the roots belong in.</param>
        /// <param name="initialDM">Initial dry matter weight</param>
        /// <param name="initialDepth">Initial root depth</param>
        /// <param name="minLiveDM">The minimum biomass for this organ</param>
        public void Initialise(Zone zone, double initialDM, double initialDepth,
                               double minLiveDM)
        {
            soil = zone.FindInScope<Soil>();
            if (soil == null)
                throw new Exception($"Cannot find soil in zone {zone.Name}");

            soilPhysical = soil.FindInScope<IPhysical>();
            if (soilPhysical == null)
                throw new Exception($"Cannot find soil physical in soil {soil.Name}");

            waterBalance = soil.FindInScope<ISoilWater>();
            if (waterBalance == null)
                throw new Exception($"Cannot find a water balance model in soil {soil.Name}");

            soilCropData = soil.FindDescendant<SoilCrop>(species.Name + "Soil");
            if (soilCropData == null)
                throw new Exception($"Cannot find a soil crop parameterisation called {species.Name + "Soil"}");

            nutrient = zone.FindInScope<INutrient>();
            if (nutrient == null)
                throw new Exception($"Cannot find SoilNitrogen in zone {zone.Name}");

            no3 = zone.FindInScope("NO3") as ISolute;
            if (no3 == null)
                throw new Exception($"Cannot find NO3 solute in zone {zone.Name}");
            nh4 = zone.FindInScope("NH4") as ISolute;
            if (nh4 == null)
                throw new Exception($"Cannot find NH4 solute in zone {zone.Name}");

            // link to soil and initialise related variables
            zoneName = soil.Parent.Name;
            nLayers = soilPhysical.Thickness.Length;
            dulMM = soilPhysical.DULmm;
            ll15MM = soilPhysical.LL15mm;
            mySoilNH4Available = new double[nLayers];
            mySoilNO3Available = new double[nLayers];

            // save minimum DM and get target root distribution
            Depth = initialDepth;
            minimumLiveDM = minLiveDM;
            CalculateRootZoneBottomLayer();
            TargetDistribution = RootDistributionTarget();

            // initialise tissues
            double[] initialDMByLayer = MathUtilities.Multiply_Value(CurrentRootDistributionTarget(), initialDM);
            double[] initialNByLayer = MathUtilities.Multiply_Value(initialDMByLayer, NConcOptimum);
            Live = tissue[0];
            Dead = tissue[1];
            Live.Initialise(initialDMByLayer, initialNByLayer);
            Dead.Initialise(null, null);
        }

        /// <summary>Reset this root organ's state.</summary>
        /// <param name="rootWt">The amount of root biomass (kg/ha).</param>
        /// <param name="rootDepth">The depth of roots to reset to(mm).</param>
        /// <remarks>It is assumed that N is at optimum content.</remarks>
        public void Reset(double rootWt, double rootDepth)
        {
            Depth = rootDepth;
            CalculateRootZoneBottomLayer();

            var rootBiomass = MathUtilities.Multiply_Value(CurrentRootDistributionTarget(), rootWt);
            Live.ResetTo(rootBiomass);
        }

        /// <summary>Set this root organ's biomass state.</summary>
        /// <param name="rootWt">The DM amount of root biomass (kg/ha).</param>
        /// <param name="rootN">The amount of N in root biomass (kg/ha).</param>
        /// <param name="rootDepth">The depth of root zone (mm).</param>
        public void SetBiomassState(double rootWt, double rootN, double rootDepth)
        {
            Depth = rootDepth;
            CalculateRootZoneBottomLayer();

            var rootBiomassWt = MathUtilities.Multiply_Value(CurrentRootDistributionTarget(), rootWt);
            var rootBiomassN = MathUtilities.Multiply_Value(rootBiomassWt, MathUtilities.Divide(rootN, rootWt, 0.0));
            for (int t = 0; t < tissue.Length; t++)
            {
                tissue[t].SetBiomass(rootBiomassWt, rootBiomassN);
            }
        }

        /// <summary>Removes biomass from root layers when harvest, graze or cut events are called.</summary>
        /// <param name="biomassRemoveType">Name of event that triggered this biomass remove call.</param>
        /// <param name="biomassToRemove">The fractions of biomass to remove</param>
        public void RemoveBiomass(string biomassRemoveType, OrganBiomassRemovalType biomassToRemove)
        {
            // Live removal
            Live.RemoveBiomass(biomassToRemove.FractionLiveToRemove, sendToSoil: false);
            Live.RemoveBiomass(biomassToRemove.FractionLiveToResidue, sendToSoil: true);

            // Dead removal
            Dead.RemoveBiomass(biomassToRemove.FractionDeadToRemove, sendToSoil: false);
            Dead.RemoveBiomass(biomassToRemove.FractionDeadToResidue, sendToSoil: true);

            if (biomassRemoveType != "Harvest")
            {
                IsKLModiferDueToDamageActive = true;
            }
        }

        /// <summary>Reset the transfer amounts in all tissues of this organ.</summary>
        internal void ClearDailyTransferredAmounts()
        {
            for (int t = 0; t < tissue.Length; t++)
            {
                tissue[t].ClearDailyTransferredAmounts();
            }
        }

        /// <summary>Kills part of the organ (transfer DM and N to dead tissue).</summary>
        /// <param name="fractionToRemove">The fraction to kill in each tissue</param>
        internal void KillOrgan(double fractionToRemove)
        {
            Live.MoveFractionToTissue(fractionToRemove, Dead);
        }

        /// <summary>Computes the DM and N amounts turned over for all tissues.</summary>
        /// <param name="turnoverRate">The turnover rate for each tissue</param>
        /// <returns>The DM and N amount detached from this organ</returns>
        internal BiomassAndN DoTissueTurnover(double[] turnoverRate)
        {
            Live.DoTissueTurnover(turnoverRate[0], BottomLayer, Dead, NconcLive - NConcOptimum);
            return Dead.DoTissueTurnover(turnoverRate[1], BottomLayer, null, NconcLive - NConcMinimum);
        }

        /// <summary>Updates each tissue, make changes in DM and N effective.</summary>
        internal void DoOrganUpdate()
        {
            RootTissue.UpdateTissues(Live, Dead);
        }

        /// <summary>Finds out the amount of plant available water in the soil.</summary>
        /// <param name="myZone">The soil information</param>
        internal double[] EvaluateSoilWaterAvailable(ZoneWaterAndN myZone)
        {
            double[] result = new double[nLayers];
            for (int layer = 0; layer <= BottomLayer; layer++)
            {
                result[layer] = Math.Max(0.0, myZone.Water[layer] - (soilCropData.LL[layer] * soilPhysical.Thickness[layer]));
                result[layer] *= FractionLayerWithRoots(layer) * soilCropData.KL[layer] * KLModiferDueToDamage(layer);
            }

            return result;
        }

        /// <summary>KL modifier due to root damage (0-1).</summary>
        private double KLModiferDueToDamage(int layerIndex)
        {
            var threshold = 0.01;
            if (!IsKLModiferDueToDamageActive)
            {
                return 1.0;
            }
            else if (LengthDensity[layerIndex] < 0.0)
            {
                return 0.0;
            }
            else if (LengthDensity[layerIndex] >= threshold)
            {
                return 1.0;
            }
            else
            {
                return LengthDensity[layerIndex] / threshold;
            }
        }

        /// <summary>Finds out the amount of plant available nitrogen (NH4 and NO3) in the soil.</summary>
        /// <param name="myZone">The soil information</param>
        /// <param name="mySoilWaterUptake">Soil water uptake</param>
        internal void EvaluateSoilNitrogenAvailable(ZoneWaterAndN myZone, double[] mySoilWaterUptake)
        {
            double layerFrac; // the fraction of layer within the root zone
            double swFac;  // the soil water factor
            double bdFac;  // the soil density factor
            double potAvailableN; // potential available N
            var thickness = soilPhysical.Thickness;
            var bd = soilPhysical.BD;
            var water = myZone.Water;
            var nh4 = myZone.NH4N;
            var no3 = myZone.NO3N;
            double depthOfTopOfLayer = 0;
            for (int layer = 0; layer <= BottomLayer; layer++)
            {
                layerFrac = (Depth - depthOfTopOfLayer) / thickness[layer];
                layerFrac = Math.Min(1.0, Math.Max(0.0, layerFrac));

                bdFac = 100.0 / (thickness[layer] * bd[layer]);
                if (water[layer] >= dulMM[layer])
                    swFac = 1.0;
                else if (water[layer] <= ll15MM[layer])
                    swFac = 0.0;
                else
                {
                    double waterRatio = (water[layer] - ll15MM[layer]) /
                                        (dulMM[layer] - ll15MM[layer]);
                    waterRatio = MathUtilities.Bound(waterRatio, 0.0, 1.0);
                    swFac = 1.0 - Math.Pow(1.0 - waterRatio, ExponentSoilMoisture);
                }

                // get NH4 available
                potAvailableN = nh4[layer] * layerFrac * swFac * bdFac * KNH4;
                mySoilNH4Available[layer] = Math.Min(nh4[layer] * layerFrac, potAvailableN);

                // get NO3 available
                potAvailableN = no3[layer] * layerFrac * swFac * bdFac * KNO3;
                mySoilNO3Available[layer] = Math.Min(no3[layer] * layerFrac, potAvailableN);

                depthOfTopOfLayer += thickness[layer];
            }

            // check for maximum uptake
            potAvailableN = mySoilNH4Available.Sum() + mySoilNO3Available.Sum();
            if (potAvailableN > MaximumNUptake)
            {
                double upFraction = MaximumNUptake / potAvailableN;
                for (int layer = 0; layer <= BottomLayer; layer++)
                {
                    mySoilNH4Available[layer] *= upFraction;
                    mySoilNO3Available[layer] *= upFraction;
                }
            }
        }

        /// <summary>Computes how much of the layer is actually explored by roots (considering depth only).</summary>
        /// <param name="layer">The index for the layer being considered</param>
        /// <returns>The fraction of the layer that is explored by roots (0-1)</returns>
        internal double FractionLayerWithRoots(int layer)
        {
            double fractionInLayer = 0.0;
            if (layer < BottomLayer)
            {
                fractionInLayer = 1.0;
            }
            else if (layer == BottomLayer)
            {
                double depthTillTopThisLayer = 0.0;
                for (int z = 0; z < layer; z++)
                    depthTillTopThisLayer += soilPhysical.Thickness[z];
                fractionInLayer = (Depth - depthTillTopThisLayer) / soilPhysical.Thickness[layer];
                fractionInLayer = Math.Min(1.0, Math.Max(0.0, fractionInLayer));
            }

            return fractionInLayer;
        }

        /// <summary>Gets the index of the layer at the bottom of the root zone.</summary>
        /// <returns>The index of a layer</returns>
        private void CalculateRootZoneBottomLayer()
        {
            BottomLayer = 0;
            double currentDepth = 0.0;
            for (int layer = 0; layer < nLayers; layer++)
            {
                if (Depth > currentDepth)
                {
                    BottomLayer = layer;
                    currentDepth += soilPhysical.Thickness[layer];
                }
                else
                    layer = nLayers;
            }
        }

        /// <summary>Computes the target (or ideal) distribution of roots in the soil profile.</summary>
        /// <remarks>
        /// This distribution is solely based on root parameters (maximum depth and distribution parameters)
        /// These values will be used to allocate initial rootDM as well as any growth over the profile
        /// </remarks>
        /// <returns>A weighting factor for each soil layer (mm equivalent)</returns>
        public double[] RootDistributionTarget()
        {
            // 1. Base distribution calculated using a combination of linear and power functions:
            //  It considers homogeneous distribution from surface down to a fraction of root depth (DepthForConstantRootProportion),
            //   below this depth the proportion of root decrease following a power function (with exponent ExponentRootDistribution),
            //   it reaches zero slightly below the MaximumRootDepth (defined by rootBottomDistributionFactor), but the function is
            //   truncated at MaximumRootDepth. The values are not normalised.
            //  The values are further adjusted using the values of XF (so there will be less roots in those layers)

            double[] result = new double[nLayers];
            double depthTop = 0.0;
            double depthBottom = 0.0;
            double depthFirstStage = Math.Min(RootDepthMaximum, RootDistributionDepthParam);

            for (int layer = 0; layer < nLayers; layer++)
            {
                depthBottom += soilPhysical.Thickness[layer];
                if (depthTop >= RootDepthMaximum)
                {
                    // totally out of root zone
                    result[layer] = 0.0;
                }
                else if (depthBottom <= depthFirstStage)
                {
                    // totally in the first stage
                    result[layer] = soilPhysical.Thickness[layer] * soilCropData.XF[layer];
                }
                else
                {
                    // at least partially on second stage
                    double maxRootDepth = RootDepthMaximum * RootBottomDistributionFactor;
                    result[layer] = Math.Pow(maxRootDepth - Math.Max(depthTop, depthFirstStage), RootDistributionExponent + 1)
                                  - Math.Pow(maxRootDepth - Math.Min(depthBottom, RootDepthMaximum), RootDistributionExponent + 1);
                    result[layer] /= (RootDistributionExponent + 1) * Math.Pow(maxRootDepth - depthFirstStage, RootDistributionExponent);
                    if (depthTop < depthFirstStage)
                    {
                        // partially in first stage
                        result[layer] += depthFirstStage - depthTop;
                    }

                    result[layer] *= soilCropData.XF[layer];
                }

                depthTop += soilPhysical.Thickness[layer];
            }

            return result;
        }

        /// <summary>Computes the current target distribution of roots in the soil profile.</summary>
        /// <remarks>
        /// This distribution is a correction of the target distribution, taking into account the depth of soil
        /// as well as the current rooting depth
        /// </remarks>
        /// <returns>The proportion of root mass expected in each soil layer (0-1)</returns>
        public double[] CurrentRootDistributionTarget()
        {
            double cumProportion = 0.0;
            double topLayersDepth = 0.0;
            double[] result = new double[nLayers];

            // Get the total weight over the root zone, first layers totally within the root zone
            for (int layer = 0; layer < BottomLayer; layer++)
            {
                cumProportion += TargetDistribution[layer];
                topLayersDepth += soilPhysical.Thickness[layer];
            }
            // Then consider layer at the bottom of the root zone
            double layerFrac = Math.Min(1.0, (RootDepthMaximum - topLayersDepth) / (Depth - topLayersDepth));
            cumProportion += TargetDistribution[BottomLayer] * layerFrac;

            // Normalise the weights to be a fraction, adds up to one
            if (MathUtilities.IsGreaterThan(cumProportion, 0))
            {
                for (int layer = 0; layer < BottomLayer; layer++)
                    result[layer] = TargetDistribution[layer] / cumProportion;
                result[BottomLayer] = TargetDistribution[BottomLayer] * layerFrac / cumProportion;
            }

            return result;
        }

        /// <summary>Computes the allocation of new growth to roots for each layer.</summary>
        /// <remarks>
        /// The current target distribution for roots changes whenever then root depth changes, this is then used to allocate 
        ///  new growth to each layer within the root zone. The existing distribution is used on any DM removal, so it may
        ///  take some time for the actual distribution to evolve to be equal to the target.
        /// </remarks>
        /// <param name="dGrowthRootDM">Root growth dry matter (kg/ha).</param>
        /// <param name="dGrowthRootN">Root growth nitrogen (kg/ha).</param>
        public void DoRootGrowthAllocation(double dGrowthRootDM, double dGrowthRootN)
        {
            if (MathUtilities.IsGreaterThan(dGrowthRootDM, 0))
            {
                // root DM is changing due to growth, check potential changes in distribution
                double[] growthRootFraction;
                double[] currentRootTarget = CurrentRootDistributionTarget();
                if (MathUtilities.AreEqual(Live.FractionWt, currentRootTarget))
                {
                    // no need to change the distribution
                    growthRootFraction = Live.FractionWt;
                }
                else
                {
                    // root distribution should change, get preliminary distribution (average of current and target)
                    growthRootFraction = new double[nLayers];
                    for (int layer = 0; layer <= BottomLayer; layer++)
                        growthRootFraction[layer] = 0.5 * (Live.FractionWt[layer] + currentRootTarget[layer]);

                    // normalise distribution of allocation
                    double layersTotal = growthRootFraction.Sum();
                    for (int layer = 0; layer <= BottomLayer; layer++)
                        growthRootFraction[layer] = growthRootFraction[layer] / layersTotal;
                }

                Live.SetBiomassTransferIn(dm: MathUtilities.Multiply_Value(growthRootFraction, dGrowthRootDM),
                                           n: MathUtilities.Multiply_Value(growthRootFraction, dGrowthRootN));
            }
            // TODO: currently only the roots at the main / home zone are considered, must add the other zones too
        }

        /// <summary>Computes the variations in root depth.</summary>
        /// <remarks>
        /// Root depth will increase if it is smaller than maximumRootDepth and there is a positive net DM accumulation.
        /// The depth increase rate is of zero-order type, given by the RootElongationRate, but it is adjusted for temperature
        ///  in a similar fashion as plant DM growth. Note that currently root depth never decreases.
        ///  - The effect of temperature was reduced (average between that of growth DM and one) as soil temp varies less than air
        /// </remarks>
        /// <param name="dGrowthRootDM">Root growth dry matter (kg/ha).</param>
        /// <param name="detachedRootDM">DM amount detached from roots, added to soil FOM (kg/ha)</param>
        /// <param name="temperatureLimitingFactor">Growth limiting factor due to temperature.</param>
        public void EvaluateRootElongation(double dGrowthRootDM, double detachedRootDM, double temperatureLimitingFactor)
        {
            // Check changes in root depth
            var dRootDepth = 0.0;
            if (MathUtilities.IsGreaterThan(dGrowthRootDM - detachedRootDM, 0) && (Depth < RootDepthMaximum))
            {
                double tempFactor = 0.5 + 0.5 * temperatureLimitingFactor;
                dRootDepth = RootElongationRate * tempFactor;
                Depth = Math.Min(RootDepthMaximum, Math.Max(RootDepthMinimum, Depth + dRootDepth));
                CalculateRootZoneBottomLayer();
            }
            else
            {
                // No net growth
                dRootDepth = 0.0;
            }
        }

        /// <summary>Set new growth to root.</summary>
        /// <param name="dmToRoot">Dry matter growth.</param>
        /// <param name="nToRoot">Nitrogen growth.</param>
        /// <returns></returns>
        public BiomassAndN SetNewGrowthAllocation(double dmToRoot, double nToRoot)
        {
            return Live.SetNewGrowthAllocation(dmToRoot, nToRoot);
        }

        /// <summary>Detach roots.</summary>
        /// <param name="dryMatter">Dry matter to detach.</param>
        /// <param name="nitrogen">Nitrogen to detach.</param>
        public void DetachRoots(double dryMatter, double nitrogen)
        {
            Live.DetachBiomass(dryMatter, nitrogen);
        }

        /// <summary>Remobilise N from live tissues.</summary>
        /// <param name="fracRemobilised">Fraction remobilised.</param>
        public void RemobiliseLiveN(double fracRemobilised)
        {
            Live.DoRemobiliseN(fracRemobilised);
        }

        /// <summary>Remobilise N from live tissues.</summary>
        /// <param name="fracRemobilised">Fraction remobilised.</param>
        public void RemobiliseDeadN(double fracRemobilised)
        {
            tissue[1].DoRemobiliseN(fracRemobilised);
        }

        /// <summary>Remove water from soil - uptake.</summary>
        /// <param name="amount">Amount of water to remove.</param>
        public void PerformWaterUptake(double[] amount)
        {
            if (MathUtilities.IsGreaterThan(amount.Sum(), 0))
                waterBalance.RemoveWater(amount);
        }

        /// <summary>Remove nutrients from soil - uptake.</summary>
        /// <param name="no3Amount">Amount of no3 to remove.</param>
        /// <param name="nh4Amount">Amount of nh4 to remove.</param>
        public void PerformNutrientUptake(double[] no3Amount, double[] nh4Amount)
        {
            no3.SetKgHa(SoluteSetterType.Plant, MathUtilities.Subtract(no3.kgha, no3Amount));
            nh4.SetKgHa(SoluteSetterType.Plant, MathUtilities.Subtract(nh4.kgha, nh4Amount));
        }

        /// <summary>Flag indicating whether roots are in the specified zone.</summary>
        /// <param name="zoneName">The zone name.</param>
        public bool IsInZone(string zoneName)
        {
            return this.zoneName == zoneName;
        }

        /// <summary>Computes the turnover rate.</summary>
        /// <param name="gamaR">Daily DM turnover rate for root tissue.</param>
        /// <returns></returns>
        public double EvaluateTissueTurnover(double gamaR)
        {
            // Check minimum DM for roots too
            if (DMLive * (1.0 - gamaR) < minimumLiveDM)
            {
                if (DMLive <= minimumLiveDM)
                    gamaR = 0.0;
                else
                    gamaR = MathUtilities.Divide(DMLive - minimumLiveDM, DMLive, 0.0);
                // TODO: currently only the roots at the main/home zone are considered, must add the other zones too
            }
            return gamaR;
        }
    }
}
