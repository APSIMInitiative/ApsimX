using System;
using System.Linq;
using Models.PMF;
using Models.Core;
using Models.Soils;
using Models.Interfaces;
using Models.Soils.Nutrients;
using Models.Soils.Arbitrator;
using APSIM.Shared.Utilities;
using Models.PMF.Interfaces;
using System.Collections.Generic;
using APSIM.Numerics;

namespace Models.AgPasture
{

    /// <summary>Describes a generic below ground organ of a pasture species.</summary>
    [Serializable]
    public class PastureBelowGroundOrgan : Model
    {
        /// <summary>Nutrient model.</summary>
        [Link(Type = LinkType.Ancestor)]
        private PastureSpecies species = null;

        /// <summary>Collection of tissues for this organ.</summary>
        [Link(Type = LinkType.Child)]
        public RootTissue[] Tissue;

        /// <summary>Live root tissue.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public RootTissue Live { get; private set; }

        /// <summary>Dead root tissue.</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        public RootTissue Dead { get; private set; }

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

        /// <summary>NO3 solute in the soil.</summary>
        private ISolute no3 = null;

        /// <summary>NH4 solute in the soil.</summary>
        private ISolute nh4 = null;

        //---------------------------- Parameters -----------------------

        /// <summary>Minimum rooting depth (mm).</summary>
        public double MinimumRootingDepth { get; set; } = 50.0;

        /// <summary>Maximum potential rooting depth (mm).</summary>
        public double MaximumPotentialRootingDepth { get; set; } = 750.0;

        /// <summary>Maximum rooting depth allowed by soil condition (mm).</summary>
        public double MaximumAllowedRootingDepth { get; set; } = 500.0;

        /// <summary>Daily root elongation rate at optimum temperature (mm/day).</summary>
        [Units("mm/day")]
        public double ElongationRate { get; set; } = 25.0;

        /// <summary>Factor for root distribution; depth from surface where root proportion starts to decrease (mm).</summary>
        [Units("mm")]
        public double DepthDistributionParamTop { get; set; } = 90.0;

        /// <summary>Exponent controlling the root distribution as function of depth (>0.0).</summary>
        [Units("-")]
        public double DepthDistributionExponent { get; set; } = 3.2;

        /// <summary>Factor for root distribution; controls where the function is zero below maxRootDepth.</summary>
        public double DepthDistributionParamBottom { get; set; } = 1.05;

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
        public double MinimumLiveDM { get; set; } = 1.0;

        //----------------------- Constants -----------------------

        /// <summary>Minimum significant difference between two values.</summary>
        internal const double Epsilon = 0.000000001;

        //----------------------- States -----------------------

        /// <summary>Rooting depth (mm).</summary>
        public double Depth { get; set; }

        /// <summary>Soil layer at the bottom of the root zone.</summary>
        internal int BottomLayer { get; private set; }

        /// <summary>Target (idealised) DM fractions for each layer (0-1).</summary>
        internal double[] TargetDistribution { get; set; }

        /// <summary>Total dry matter in this organ (kg/ha).</summary>
        internal double DMTotal { get { return Live.DM.Wt + Dead.DM.Wt; } }

        /// <summary>Dry matter in the live (green) tissues (kg/ha).</summary>
        internal double DMLive { get { return Live.DM.Wt; } }

        /// <summary>Dry matter in the dead tissues (kg/ha).</summary>
        /// <remarks>Last tissue is assumed to represent dead material.</remarks>
        internal double DMDead { get { return Dead.DM.Wt; } }

        /// <summary>Proportion of dry matter in each soil layer (0-1).</summary>
        internal double[] DMFractions { get { return Live.FractionWt; } }

        /// <summary>Total N amount in this organ (kg/ha).</summary>
        internal double NTotal { get { return Live.DM.N + Dead.DM.N; } }

        /// <summary>N amount in the live (green) tissues (kg/ha).</summary>
        internal double NLive { get { return Live.DM.N; } }

        /// <summary>N amount in the dead tissues (kg/ha).</summary>
        /// <remarks>Last tissues is assumed to represent dead material.</remarks>
        internal double NDead { get { return Dead.DM.N; } }

        /// <summary>Average N concentration in this organ (kg/kg).</summary>
        internal double NConcTotal{ get { return MathUtilities.Divide(NTotal, DMTotal, 0.0); } }

        /// <summary>Average N concentration in the live tissues (kg/kg).</summary>
        internal double NConcLive { get { return MathUtilities.Divide(NLive, DMLive, 0.0); } }

        /// <summary>Average N concentration in dead tissues (kg/kg).</summary>
        internal double NConcDead { get { return MathUtilities.Divide(NDead, DMDead, 0.0); } }

        /// <summary>Amount of luxury N available for remobilisation (kg/ha).</summary>
        internal double NLuxuryRemobilisable { get { return Live.NRemobilisable; } }

        /// <summary>Luxury N remobilised into new growth (kg/ha).</summary>
        internal double NLuxuryRemobilised { get { return Live.NRemobilised; } }

        /// <summary>Amount of senesced N available for remobilisation (kg/ha).</summary>
        internal double NSenescedRemobilisable { get { return Dead.NRemobilisable; } }

        /// <summary>Senesced N remobilised into new growth (kg/ha).</summary>
        internal double NSenescedRemobilised { get { return Dead.NRemobilised; } }

        /// <summary>DM senescing from this organ (kg/ha).</summary>
        public double DMSenesced { get { return Live.DMTransferredOut; } }

        /// <summary>N senescing from this organ (kg/ha).</summary>
        public double NSenesced { get { return Live.NTransferredOut; } }

        /// <summary>DM detached from this organ (kg/ha).</summary>
        public double DMDetached { get { return Dead.DMTransferredOut; } }

        /// <summary>N detached from this organ (kg/ha).</summary>
        public double NDetached { get { return Dead.NTransferredOut; } }

        /// <summary>DM removed from this tissue (kg/ha).</summary>
        public double DMRemoved { get { return Live.DMRemoved + Dead.DMRemoved; } }

        /// <summary>N removed from this tissue (kg/ha).</summary>
        public double NRemoved { get { return Live.NRemoved + Dead.NRemoved; } }

        /// <summary>DM added to this organ via growth (kg/ha).</summary>
        public double DMGrowth { get { return Live.DMTransferredIn; } }

        /// <summary>N added to this organ via growth (kg/ha).</summary>
        public double NGrowth { get { return Live.NTransferredIn; } }

        /// <summary>Root length density by volume (mm/mm^3).</summary>
        public double[] LengthDensity
        {
            get
            {
                double[] result = new double[nLayers];
                double totalRootLength = Tissue[0].DM.Wt * SpecificRootLength * 0.1; // m root/m2
                totalRootLength *= 0.001; // convert into mm root/mm2 soil)
                for (int layer = 0; layer < result.Length; layer++)
                {
                    result[layer] = Tissue[0].FractionWt[layer] * totalRootLength / soilPhysical.Thickness[layer];
                }
                return result;
            }
        }

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

        /// <summary>Initialise this root instance (and tissues).</summary>
        /// <param name="zone">The zone the roots belong to.</param>
        /// <param name="minimumLiveWt">Minimum live DM biomass for this organ (kg/ha).</param>
        public void Initialise(Zone zone, double minimumLiveWt)
        {
            // link to soil models parameters
            soil = zone.FindInScope<Soil>();
            if (soil == null)
            {
                throw new Exception($"Cannot find soil in zone {zone.Name}");
            }

            soilPhysical = soil.FindInScope<IPhysical>();
            if (soilPhysical == null)
            {
                throw new Exception($"Cannot find soil physical in soil {soil.Name}");
            }

            waterBalance = soil.FindInScope<ISoilWater>();
            if (waterBalance == null)
            {
                throw new Exception($"Cannot find a water balance model in soil {soil.Name}");
            }

            soilCropData = soil.FindDescendant<SoilCrop>(species.Name + "Soil");
            if (soilCropData == null)
            {
                throw new Exception($"Cannot find a soil crop parameterisation called {species.Name + "Soil"}");
            }

            nutrient = zone.FindInScope<INutrient>();
            if (nutrient == null)
            {
                throw new Exception($"Cannot find SoilNitrogen in zone {zone.Name}");
            }

            no3 = zone.FindInScope("NO3") as ISolute;
            if (no3 == null)
            {
                throw new Exception($"Cannot find NO3 solute in zone {zone.Name}");
            }

            nh4 = zone.FindInScope("NH4") as ISolute;
            if (nh4 == null)
            {
                throw new Exception($"Cannot find NH4 solute in zone {zone.Name}");
            }

            // initialise soil related variables
            zoneName = soil.Parent.Name;
            nLayers = soilPhysical.Thickness.Length;
            mySoilNH4Available = new double[nLayers];
            mySoilNO3Available = new double[nLayers];
            mySoilWaterAvailable = new double[nLayers];

            // check rooting depth
            MaximumAllowedRootingDepth = Math.Min(MaximumPotentialRootingDepth, soilPhysical.ThicknessCumulative[soilPhysical.Thickness.Length - 1]);
            for (int z = 0; z < soilPhysical.Thickness.Length; z++)
            {
                if (soilCropData.XF[z] < 0.000001)
                { // root depth limited by some soil issue
                    if (z > 0)
                    {
                        MaximumAllowedRootingDepth = soilPhysical.ThicknessCumulative[z - 1];
                        break;
                    }
                    else
                    { // not a soil...
                        MaximumAllowedRootingDepth = 0.0;
                    }
                }
            }

            // save minimum DM and get target root distribution
            MinimumLiveDM = minimumLiveWt;
            TargetDistribution = RootDistributionTarget();

            // initialise tissues
            Live.Initialise();
            Dead.Initialise();
        }

        /// <summary>Set this root organ's biomass state.</summary>
        /// <param name="rootWt">The DM amount of root biomass (kg/ha).</param>
        /// <param name="rootN">The amount of N in root biomass (kg/ha).</param>
        /// <param name="rootDepth">The depth of root zone (mm).</param>
        public void SetBiomassState(double rootWt, double rootN, double rootDepth)
        {
            Depth = Math.Min(rootDepth, MaximumAllowedRootingDepth);
            CalculateRootZoneBottomLayer();

            var rootBiomassWt = MathUtilities.Multiply_Value(CurrentRootDistributionTarget(), rootWt);
            var rootBiomassN = MathUtilities.Multiply_Value(rootBiomassWt, MathUtilities.Divide(rootN, rootWt, 0.0));
            Live.SetBiomass(rootBiomassWt, rootBiomassN);
            var blankArray = MathUtilities.Multiply_Value(CurrentRootDistributionTarget(), 0.0);
            Dead.SetBiomass(blankArray, blankArray); // assumes there's no dead material
        }

        /// <summary>Remove biomass from organ.</summary>
        /// <param name="liveToRemove">Fraction of live biomass to remove from simulation (0-1).</param>
        /// <param name="deadToRemove">Fraction of dead biomass to remove from simulation (0-1).</param>
        /// <param name="liveToResidue">Fraction of live biomass to remove and send to residue pool(0-1).</param>
        /// <param name="deadToResidue">Fraction of dead biomass to remove and send to residue pool(0-1).</param>
        /// <returns>The amount of biomass (live+dead) removed from the plant (g/m2).</returns>
        public double RemoveBiomass(double liveToRemove = 0, double deadToRemove = 0, double liveToResidue = 0, double deadToResidue = 0)
        {
            // Remove live tissue
            Live.RemoveBiomass(liveToRemove, liveToResidue);

            // Remove dead tissue
            Dead.RemoveBiomass(deadToRemove, deadToResidue);

            // Update LAI and herbage digestibility
            species.EvaluateLAI();
            species.EvaluateDigestibility();

            return Live.DMRemoved + Dead.DMRemoved;
        }

        /// <summary>Reset the transfer amounts in all tissues of this organ.</summary>
        internal void ClearDailyTransferredAmounts()
        {
            for (int t = 0; t < Tissue.Length; t++)
            {
                Tissue[t].ClearDailyTransferredAmounts();
            }
        }

        /// <summary>Kills part of the organ (transfer DM and N to dead tissue).</summary>
        /// <param name="fractionToRemove">The fraction to kill in each tissue</param>
        internal void KillOrgan(double fractionToRemove)
        {
            double[] dmKilled = MathUtilities.Multiply_Value(Live.FractionWt, Live.DM.Wt * fractionToRemove);
            double[] nKilled = MathUtilities.Multiply_Value(Live.FractionWt, Live.DM.N * fractionToRemove);
            Dead.AddBiomass(dmKilled, nKilled);
            Live.AddBiomass(MathUtilities.Multiply_Value(dmKilled, -1.0), MathUtilities.Multiply_Value(nKilled, -1.0));
        }

        /// <summary>Computes the DM and N amounts turned over for all tissues.</summary>
        /// <param name="turnoverRate">The turnover rate for each tissue</param>
        internal void CalculateTissueTurnover(double[] turnoverRate)
        {
            Live.DoTissueTurnover(turnoverRate[0], Dead, NConcOptimum);
            Dead.DoTissueTurnover(turnoverRate[1], null, NConcMinimum);
        }

        /// <summary>Updates each tissue, make changes in DM and N effective.</summary>
        internal bool Update()
        {
            // save current state
            double previousDM = DMTotal;
            double previousN = NTotal;

            // update all tissues
            Live.Update();
            Dead.Update();

            // check mass balance
            bool dmIsOk = MathUtilities.FloatsAreEqual(previousDM + DMGrowth - DMDetached, DMTotal, 0.000001);
            bool nIsOk = MathUtilities.FloatsAreEqual(previousN + NGrowth - NLuxuryRemobilised - NSenescedRemobilised - NDetached, NTotal, 0.000001);
            return (dmIsOk || nIsOk);
        }

        /// <summary>Finds out the amount of plant available water in the soil.</summary>
        /// <param name="myZone">The soil information</param>
        internal void EvaluateSoilWaterAvailability(ZoneWaterAndN myZone)
        {
            for (int layer = 0; layer <= BottomLayer; layer++)
            {
                mySoilWaterAvailable[layer] = Math.Max(0.0, myZone.Water[layer] - soilCropData.LLmm[layer]);
                mySoilWaterAvailable[layer] *= FractionLayerWithRoots(layer) * soilCropData.KL[layer] * KLModiferDueToDamage(layer);
            }
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
        /// <remarks>
        ///  N availability is considered only within the root zone, and is affected by moisture (dry soils
        ///   having less N available) and N concentration (low concentration leads to reduced availability).
        ///  The effect of soil moisture is a curve starting at LL, where it is zero, reaching one at DUL.
        ///  An exponent bends the pattern between these two values, making it concave if the exponent is
        ///   greater than one, with the derivative being zero at DUL;
        ///  The effect of concentration is a simple linear function starting at zero when there is no N in
        ///   the soil, and reaching its maximum (one) at a concentration defined by the 'kNxx' parameter.
        ///   This (1/kNxx) represents the critical concentration (in ppm), below which N availability is
        ///   limited (e.g. a KNO3 = 0.02 means no limitations if the NO3 concentration is above 50 ppm).
        /// </remarks>
        /// <param name="myZone">The soil information from the zone that contains the roots.</param>
        internal void EvaluateSoilNitrogenAvailability(ZoneWaterAndN myZone)
        {
            var thickness = soilPhysical.Thickness;
            var bd = soilPhysical.BD;
            var dulMM = soilPhysical.DULmm;
            var llMM = soilCropData.LLmm;
            var swMM = myZone.Water;
            var nh4 = myZone.NH4N;
            var no3 = myZone.NO3N;
            double depthAtTopOfLayer = 0;
            for (int layer = 0; layer <= BottomLayer; layer++)
            {
                // get the fraction of this layer that is within the root zone
                double layerFraction = MathUtilities.Bound((Depth - depthAtTopOfLayer) / thickness[layer], 0.0, 1.0);

                // get the soil moisture factor (less N available in drier soil)
                double rwc = MathUtilities.Bound((swMM[layer] - llMM[layer]) / (dulMM[layer] - llMM[layer]), 0.0, 1.0);
                double moistureFactor = 1.0 - Math.Pow(1.0 - rwc, ExponentSoilMoisture);

                // get NH4 available
                double nh4ppm = nh4[layer] * 100.0 / (thickness[layer] * bd[layer]);
                double concentrationFactor = Math.Min(1.0, nh4ppm * KNH4);
                mySoilNH4Available[layer] = nh4[layer] * layerFraction * Math.Min(0.999999, moistureFactor * concentrationFactor);

                // get NO3 available
                double no3ppm = no3[layer] * 100.0 / (thickness[layer] * bd[layer]);
                concentrationFactor = Math.Min(1.0, no3ppm * KNO3);
                mySoilNO3Available[layer] = no3[layer] * layerFraction * Math.Min(0.999999, moistureFactor * concentrationFactor);

                depthAtTopOfLayer += thickness[layer];
            }

            // check totals, reduce available N if greater than maximum uptake
            double potentialAvailableN = mySoilNH4Available.Sum() + mySoilNO3Available.Sum();
            if (potentialAvailableN > MaximumNUptake)
            {
                double upFraction = MaximumNUptake / potentialAvailableN;
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
                    break;
                }
                else
                {
                    layer = nLayers;
                }
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
            double depthFirstStage = Math.Min(MaximumAllowedRootingDepth, DepthDistributionParamTop);

            for (int layer = 0; layer < nLayers; layer++)
            {
                depthBottom += soilPhysical.Thickness[layer];
                if (depthTop >= MaximumAllowedRootingDepth)
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
                    double maxRootDepth = MaximumAllowedRootingDepth * DepthDistributionParamBottom;
                    result[layer] = Math.Pow(maxRootDepth - Math.Max(depthTop, depthFirstStage), DepthDistributionExponent + 1)
                                  - Math.Pow(maxRootDepth - Math.Min(depthBottom, MaximumAllowedRootingDepth), DepthDistributionExponent + 1);
                    result[layer] /= (DepthDistributionExponent + 1) * Math.Pow(maxRootDepth - depthFirstStage, DepthDistributionExponent);
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
            double layerFrac = Math.Min(1.0, (MaximumAllowedRootingDepth - topLayersDepth) / (Depth - topLayersDepth));
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
        /// The current target distribution for roots changes whenever the root depth changes, this is then used to allocate
        ///  new growth to each layer within the root zone. The existing distribution is used on any DM removal, so it may
        ///  take some time for the actual distribution to evolve to be equal to the target.
        /// </remarks>
        /// <param name="rootDMToAdd">Root dry matter grown (kg/ha).</param>
        /// <param name="rootNToAdd">Nitrogen in root grown (kg/ha).</param>
        public void DoRootGrowthAllocation(double rootDMToAdd, double rootNToAdd)
        {
            if (MathUtilities.IsGreaterThan(rootDMToAdd, 0))
            {
                // root DM is changing due to growth, check potential changes in distribution
                double[] newGrowthFraction;
                double[] currentRootTarget = CurrentRootDistributionTarget();
                if (MathUtilities.AreEqual(Live.FractionWt, currentRootTarget))
                {
                    // no need to change the distribution
                    newGrowthFraction = Live.FractionWt;
                }
                else
                {
                    // root distribution should change, get preliminary distribution (average of current and target)
                    newGrowthFraction = new double[nLayers];
                    for (int layer = 0; layer <= BottomLayer; layer++)
                    {
                        newGrowthFraction[layer] = 0.5 * (Live.FractionWt[layer] + currentRootTarget[layer]);
                    }

                    // normalise distribution of allocation
                    double layersTotal = newGrowthFraction.Sum();
                    for (int layer = 0; layer <= BottomLayer; layer++)
                    {
                        newGrowthFraction[layer] = newGrowthFraction[layer] / layersTotal;
                    }
                }

                Live.SetBiomassTransferIn(dm: MathUtilities.Multiply_Value(newGrowthFraction, rootDMToAdd),
                                           n: MathUtilities.Multiply_Value(newGrowthFraction, rootNToAdd));
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
            if (MathUtilities.IsGreaterThan(dGrowthRootDM - detachedRootDM, 0.0) && (Depth < MaximumAllowedRootingDepth))
            {
                double tempFactor = 0.5 + 0.5 * temperatureLimitingFactor;
                var dRootDepth = ElongationRate * soilCropData.XF[BottomLayer] * tempFactor;
                Depth = Math.Min(MaximumAllowedRootingDepth, Math.Max(MinimumRootingDepth, Depth + dRootDepth));
                CalculateRootZoneBottomLayer();
            }
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
    }
}
