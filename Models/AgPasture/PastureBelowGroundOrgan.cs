using System;
using System.Linq;
using System.Collections.Generic;
using APSIM.Shared.Utilities;
using APSIM.Numerics;
using APSIM.Core;
using Models.Core;
using Models.PMF;
using Models.Soils;
using Models.Interfaces;
using Models.Soils.Arbitrator;
using Models.Soils.Nutrients;
using Models.PMF.Interfaces;

namespace Models.AgPasture
{

    /// <summary>Describes a generic below ground organ of a pasture species.</summary>
    [Serializable]
    public class PastureBelowGroundOrgan : Model, IStructureDependency
    {
        /// <summary>Structure instance supplied by APSIM.core.</summary>
        [field: NonSerialized]
        public IStructure Structure { private get; set; }

        /// <summary>Plant model.</summary>
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

        /// <summary>N concentration for optimum growth (kg/kg).</summary>
        [Units("kg/kg")]
        public double NConcOptimum { get; set; }

        /// <summary>Minimum N concentration, structural N (kg/kg).</summary>
        [Units("kg/kg")]
        public double NConcMinimum { get; set; }

        /// <summary>Maximum N concentration, for luxury uptake (kg/kg).</summary>
        [Units("kg/kg")]
        public double NConcMaximum { get; set; }

        /// <summary>Maximum reduction in N concentration due to elevated CO2 (0-1).</summary>
        [Units("kg/kg")]
        public double MaxCO2EffectOnNRequirement { get; set; } = 0.3;

        /// <summary>Minimum rooting depth (mm).</summary>
        [Units("mm")]
        public double MinimumRootingDepth { get; set; }

        /// <summary>Maximum potential rooting depth (mm).</summary>
        [Units("mm")]
        public double MaximumPotentialRootingDepth { get; set; }

        /// <summary>Daily root elongation rate at optimum temperature (mm/day).</summary>
        [Units("mm/day")]
        public double ElongationRate { get; set; }

        /// <summary>Factor for root distribution; depth from surface where root proportion starts to decrease (mm).</summary>
        [Units("mm")]
        public double DepthDistributionParamTop { get; set; }

        /// <summary>Exponent controlling the root distribution as function of depth (>0.0).</summary>
        [Units("-")]
        public double DepthDistributionExponent { get; set; }

        /// <summary>Factor for root distribution; controls where the function is zero below maxRootDepth.</summary>
        [Units("-")]
        public double DepthDistributionParamBottom { get; set; } = 1.05;

        /// <summary>Specific root length (m/gDM).</summary>
        [Units("m/g")]
        public double SpecificRootLength { get; set; }

        /// <summary>Ammonium uptake coefficient (/ppm).</summary>
        [Units("/ppm")]
        public double KNH4 { get; set; }

        /// <summary>Nitrate uptake coefficient (/ppm).</summary>
        [Units("/ppm")]
        public double KNO3 { get; set; }

        /// <summary>Maximum daily amount of N that can be taken up by the plant (kg/ha).</summary>
        [Units("kg/ha")]
        public double MaximumNUptake { get; set; }

        /// <summary>Exponent controlling the effect of soil moisture variations on nitrogen extractability.</summary>
        [Units("-")]
        public double NExtractionSWFactorExponent { get; set; } = 1.50;

        /// <summary>Minimum DM amount of live tissues (kg/ha).</summary>
        [Units("kg/ha")]
        public double MinimumLiveDM { get; set; }

        /// <summary>Value of minimum N concentration at start of simulation, for resets (kg/kg).</summary>
        private double baseNConcMinimum = 0.0;

        /// <summary>Value of optimum N concentration at start of simulation, for resets (kg/kg).</summary>
        private double baseNConcOptimum = 0.0;

        /// <summary>Value of maximum N concentration at start of simulation, for resets (kg/kg).</summary>
        private double baseNConcMaximum = 0.0;

        //----------------------- Constants -----------------------

        /// <summary>Minimum significant difference between two values.</summary>
        internal const double Epsilon = 0.000000001;

        //----------------------- States -----------------------

        /// <summary>Depth of rootzone (mm).</summary>
        private double rootingDepth = 0.0;

        /// <summary>Rooting depth (mm).</summary>
        [Units("mm")]
        public double Depth
        {
            get { return rootingDepth; }
            set
            {
                rootingDepth = MathUtilities.Bound(value, 0.0, MaximumAllowedDepth);
                BottomLayer = 0;
                if (soilPhysical != null)
                {
                    BottomLayer = SoilUtilities.LayerIndexOfDepth(soilPhysical.Thickness, rootingDepth);
                }
            }
        }

        /// <summary>Soil layer at the bottom of the root zone.</summary>
        public int BottomLayer { get; private set; }

        /// <summary>Maximum rooting depth allowed by soil conditions (mm).</summary>
        [Units("mm")]
        public double MaximumAllowedDepth { get; set; }

        /// <summary>Target (idealised) DM fractions for each layer (0-1).</summary>
        public double[] TargetDistribution { get; set; }

        /// <summary>Total dry matter in this organ (kg/ha).</summary>
        [Units("kg/ha")]
        public double DMTotal { get { return DMLive + DMDead; } }

        /// <summary>Dry matter in the live (green) tissues (kg/ha).</summary>
        [Units("kg/ha")]
        public double DMLive { get { return Live.DM.Wt; } }

        /// <summary>Dry matter in the dead tissues (kg/ha).</summary>
        [Units("kg/ha")]
        public double DMDead { get { return Dead.DM.Wt; } }

        /// <summary>Proportion of dry matter in each soil layer (0-1).</summary>
        [Units("kg/kg")]
        public double[] DMFraction { get { return Live.DMFraction; } }

        /// <summary>Total N amount in this organ (kg/ha).</summary>
        [Units("kg/ha")]
        public double NTotal { get { return NLive + NDead; } }

        /// <summary>N amount in the live (green) tissues (kg/ha).</summary>
        [Units("kg/ha")]
        public double NLive { get { return Live.DM.N; } }

        /// <summary>N amount in the dead tissues (kg/ha).</summary>
        [Units("kg/ha")]
        public double NDead { get { return Dead.DM.N; } }

        /// <summary>Average total N concentration in this organ (kg/kg).</summary>
        [Units("kg/kg")]
        public double NConcTotal { get { return MathUtilities.Divide(NTotal, DMTotal, 0.0); } }

        /// <summary>Average N concentration in the live tissues (kg/kg).</summary>
        [Units("kg/kg")]
        public double NConcLive { get { return MathUtilities.Divide(NLive, DMLive, 0.0); } }

        /// <summary>Average N concentration in dead tissues (kg/kg).</summary>
        [Units("kg/kg")]
        public double NConcDead { get { return MathUtilities.Divide(NDead, DMDead, 0.0); } }

        /// <summary>Luxury N available for remobilisation (kg/ha).</summary>
        [Units("kg/ha")]
        public double NLuxuryRemobilisable { get { return Live.NRemobilisable; } }

        /// <summary>Luxury N remobilised into new growth (kg/ha).</summary>
        [Units("kg/ha")]
        public double NLuxuryRemobilised { get { return Live.NRemobilised; } }

        /// <summary>Senesced N available for remobilisation (kg/ha).</summary>
        [Units("kg/ha")]
        public double NSenescedRemobilisable { get { return Dead.NRemobilisable; } }

        /// <summary>Senesced N remobilised into new growth (kg/ha).</summary>
        [Units("kg/ha")]
        public double NSenescedRemobilised { get { return Dead.NRemobilised; } }

        /// <summary>DM senescing from this organ (kg/ha).</summary>
        [Units("kg/ha")]
        public double DMSenesced { get { return Live.DMTransferredOut; } }

        /// <summary>N senescing from this organ (kg/ha).</summary>
        [Units("kg/ha")]
        public double NSenesced { get { return Live.NTransferredOut; } }

        /// <summary>DM detached from this organ (kg/ha).</summary>
        [Units("kg/ha")]
        public double DMDetached { get { return Dead.DMTransferredOut; } }

        /// <summary>N detached from this organ (kg/ha).</summary>
        [Units("kg/ha")]
        public double NDetached { get { return Dead.NTransferredOut; } }

        /// <summary>DM removed from this tissue (kg/ha).</summary>
        [Units("kg/ha")]
        public double DMRemoved { get { return Live.DMRemoved + Dead.DMRemoved; } }

        /// <summary>N removed from this tissue (kg/ha).</summary>
        [Units("kg/ha")]
        public double NRemoved { get { return Live.NRemoved + Dead.NRemoved; } }

        /// <summary>DM added to this organ via growth (kg/ha).</summary>
        [Units("kg/ha")]
        public double DMGrowth { get { return Live.DMTransferredIn; } }

        /// <summary>N added to this organ via growth (kg/ha).</summary>
        [Units("kg/ha")]
        public double NGrowth { get { return Live.NTransferredIn; } }

        /// <summary>Root length density by volume (mm/mm^3).</summary>
        /// <remarks>Values are for live tissue only.</remarks>
        [Units("mm/mm^3")]
        public double[] LengthDensity
        {
            get
            {
                double[] result = new double[nLayers];
                double totalRootLength = Live.DM.Wt * SpecificRootLength * 0.1; // m root/m2
                totalRootLength *= 0.001; // convert into mm root/mm2 soil)
                for (int layer = 0; layer < nLayers; layer++)
                {
                    result[layer] = Live.DMFraction[layer] * totalRootLength / soilPhysical.Thickness[layer];
                }
                return result;
            }
        }

        /// <summary>Amount of plant available water in the soil (mm).</summary>
        internal double[] mySoilWaterAvailable { get; private set; }

        /// <summary>Amount of soil water taken up by the plant (mm).</summary>
        internal double[] mySoilWaterUptake { get; private set; }

        /// <summary>Amount of NH4-N in the soil available to the plant (kg/ha).</summary>
        internal double[] mySoilNH4Available { get; private set; }

        /// <summary>Amount of NO3-N in the soil available to the plant (kg/ha).</summary>
        internal double[] mySoilNO3Available { get; private set; }

        /// <summary>Amount of soil NH4-N taken up by the plant (kg/ha).</summary>
        internal double[] mySoilNH4Uptake { get; private set; }

        /// <summary>Amount of soil NO3-N taken up by the plant (kg/ha).</summary>
        internal double[] mySoilNO3Uptake { get; private set; }

        /// <summary>Amounts of available soil water computed during Runge-Kutta process (mm).</summary>
        private double[][] waterDuringRungeKutta;

        /// <summary>Amounts of available soil NH4-N computed during Runge-Kutta process (kg/ha).</summary>
        private double[][] nh4DuringRungeKutta;

        /// <summary>Amounts of available soil NO3-N computed during Runge-Kutta process (kg/ha).</summary>
        private double[][] no3DuringRungeKutta;

        /// <summary>Index for each iteration of the Runge-Kutta process (from zero to three).</summary>
        private int indexRungeKutta = 0;

        /// <summary>Returns true if the KL modifier due to root damage is active or not.</summary>
        private bool IsKLModifierDueToDamageActive { get; set; } = false;

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
            soil = Structure.Find<Soil>(relativeTo: zone);
            if (soil == null)
            {
                throw new Exception($"Cannot find soil in zone {zone.Name}");
            }

            soilPhysical = Structure.Find<IPhysical>(relativeTo: soil);
            if (soilPhysical == null)
            {
                throw new Exception($"Cannot find soil physical in soil {soil.Name}");
            }

            waterBalance = Structure.Find<ISoilWater>(relativeTo: soil);
            if (waterBalance == null)
            {
                throw new Exception($"Cannot find a water balance model in soil {soil.Name}");
            }

            soilCropData = Structure.FindChild<SoilCrop>(species.Name + "Soil", relativeTo: soil, recurse: true);
            if (soilCropData == null)
            {
                throw new Exception($"Cannot find a soil crop parameterisation called {species.Name + "Soil"}");
            }

            nutrient = Structure.Find<INutrient>(relativeTo: zone);
            if (nutrient == null)
            {
                throw new Exception($"Cannot find SoilNitrogen in zone {zone.Name}");
            }

            no3 = Structure.Find<ISolute>("NO3", relativeTo: zone);
            if (no3 == null)
            {
                throw new Exception($"Cannot find NO3 solute in zone {zone.Name}");
            }

            nh4 = Structure.Find<ISolute>("NH4", relativeTo: zone);
            if (nh4 == null)
            {
                throw new Exception($"Cannot find NH4 solute in zone {zone.Name}");
            }

            // initialise soil related variables
            zoneName = soil.Parent.Name;
            nLayers = soilPhysical.Thickness.Length;
            mySoilWaterAvailable = new double[nLayers];
            mySoilWaterUptake = new double[nLayers];
            mySoilNH4Available = new double[nLayers];
            mySoilNH4Uptake = new double[nLayers];
            mySoilNO3Available = new double[nLayers];
            mySoilNO3Uptake = new double[nLayers];
            waterDuringRungeKutta = new double[4][];
            nh4DuringRungeKutta = new double[4][];
            no3DuringRungeKutta = new double[4][];

            // check rooting depth
            MaximumAllowedDepth = Math.Min(MaximumPotentialRootingDepth, soilPhysical.ThicknessCumulative[nLayers - 1]);
            for (int z = 0; z < nLayers; z++)
            {
                if (MathUtilities.FloatsAreEqual(soilCropData.XF[z], 0) || MathUtilities.FloatsAreEqual(soilCropData.KL[z], 0))
                { // root depth limited by some soil issue
                    if (z > 0)
                    {
                        MaximumAllowedDepth = Math.Min(MaximumAllowedDepth, soilPhysical.ThicknessCumulative[z - 1]);
                        break;
                    }
                    else
                    { // soil not yet initialised...
                        MaximumAllowedDepth = 0.0;
                    }
                }
            }

            // save minimum DM and get target root distribution
            MinimumLiveDM = minimumLiveWt;
            TargetDistribution = RootDistributionTarget();

            // record base N concentration values (to be used on reset)
            baseNConcMinimum = NConcMinimum;
            baseNConcOptimum = NConcOptimum;
            baseNConcMaximum = NConcMaximum;

            // check the maximum potential reduction in N conc that can result from elevated CO2
            double maxNGap = MathUtilities.Divide(baseNConcOptimum - baseNConcMinimum, baseNConcOptimum, 0.0);
            MaxCO2EffectOnNRequirement = Math.Min(MaxCO2EffectOnNRequirement, 0.99 * maxNGap);
            // Note: using 99% of gap between minimum and optimum to avoid potential errors if difference is needed

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
            Depth = Math.Min(rootDepth, MaximumAllowedDepth);
            var rootBiomassWt = MathUtilities.Multiply_Value(CurrentRootDistributionTarget(), rootWt);
            var rootBiomassN = MathUtilities.Multiply_Value(rootBiomassWt, MathUtilities.Divide(rootN, rootWt, 0.0));
            Live.SetBiomass(rootBiomassWt, rootBiomassN);
            var blankArray = MathUtilities.Multiply_Value(CurrentRootDistributionTarget(), 0.0);
            Dead.SetBiomass(blankArray, blankArray); // assumes there's no dead material

            // reset the N concentrations to base values
            NConcMinimum = baseNConcMinimum;
            NConcOptimum = baseNConcOptimum;
            NConcMaximum = baseNConcMaximum;
        }

        /// <summary>Adjust the values of N concentration as function of atmospheric CO2.</summary>
        /// <param name="co2Factor">Value representing the extent of CO2 effect (>=0.0).</param>
        /// <remarks>The CO2 factor is above one for low CO2 (higher N), and less than one with elevated CO2 (lower N conc).</remarks>
        public void UpdateNConcentrations(double co2Factor)
        {
            // adjust CO2 factor for maximum concentration
            double co2FactorForMaximum = 1.0 + 0.5 * (co2Factor - 1.0);
            // maximum N was reduced in Ecomod (same as optimum), but not in classic AgPasture. Using half of effect here...

            // adjust the value for optimum N concentration
            double adjustment = 1.0 - MaxCO2EffectOnNRequirement + MaxCO2EffectOnNRequirement * co2Factor;
            NConcOptimum = baseNConcOptimum * adjustment;

            // adjust the value for maximum N concentration
            adjustment = 1.0 - MaxCO2EffectOnNRequirement + MaxCO2EffectOnNRequirement * co2FactorForMaximum;
            NConcMaximum = Math.Max(NConcOptimum, baseNConcMaximum * adjustment); // cannot go below optimum

            // NConcMinimum is not modified, it represents 'structural N' and this is assumed not to change with CO2...
        }

        /// <summary>Remove biomass from organ.</summary>
        /// <param name="liveToRemove">Fraction of live biomass to remove from simulation (0-1).</param>
        /// <param name="deadToRemove">Fraction of dead biomass to remove from simulation (0-1).</param>
        /// <param name="liveToResidue">Fraction of live biomass to remove and send to residue pool(0-1).</param>
        /// <param name="deadToResidue">Fraction of dead biomass to remove and send to residue pool(0-1).</param>
        /// <returns>The amount of biomass (live+dead) removed from the plant (g/m2).</returns>
        public double RemoveBiomass(double liveToRemove = 0, double deadToRemove = 0, double liveToResidue = 0, double deadToResidue = 0)
        {
            // remove live tissue
            Live.RemoveBiomass(liveToRemove, liveToResidue);

            // remove dead tissue
            Dead.RemoveBiomass(deadToRemove, deadToResidue);

            return Live.DMRemoved + Dead.DMRemoved;
        }

        /// <summary>Reset the transfer amounts in all tissues of this organ.</summary>
        internal void ClearDailyTransferredAmounts()
        {
            Array.Clear(mySoilWaterAvailable, 0, nLayers);
            Array.Clear(mySoilWaterUptake, 0, nLayers);
            Array.Clear(mySoilNH4Available, 0, nLayers);
            Array.Clear(mySoilNH4Uptake, 0, nLayers);
            Array.Clear(mySoilNO3Available, 0, nLayers);
            Array.Clear(mySoilNO3Uptake, 0, nLayers);

            foreach (RootTissue tissue in Tissue)
            {
                tissue.ClearDailyTransferredAmounts();
            }
        }

        /// <summary>Kills part of the organ (transfer DM and N to dead tissue).</summary>
        /// <param name="fractionToRemove">The fraction to kill in each tissue</param>
        internal void KillOrgan(double fractionToRemove)
        {
            double[] dmKilled = MathUtilities.Multiply_Value(Live.DMFraction, Live.DM.Wt * fractionToRemove);
            double[] nKilled = MathUtilities.Multiply_Value(Live.DMFraction, Live.DM.N * fractionToRemove);
            Dead.AddBiomass(dmKilled, nKilled);
            Live.RemoveBiomass(fractionToRemove, 0.0);
        }

        /// <summary>Computes the DM and N amounts turned over for all tissues.</summary>
        /// <param name="turnoverRate">The turnover rate for each tissue</param>
        internal void CalculateTissueTurnover(double[] turnoverRate)
        {
            Live.DoTissueTurnover(turnoverRate[0], Dead);
            Dead.DoTissueTurnover(turnoverRate[1], null);
        }

        /// <summary>Computes the N amount that is potentially remobilisable for all tissues.</summary>
        public void CalculateRemobilisableN()
        {
            Live.GetRemobilisableN(NConcOptimum);
            Dead.GetRemobilisableN(NConcMinimum);
        }

        /// <summary>Updates each tissue, make changes in DM and N effective.</summary>
        internal bool Update()
        {
            // save current state
            double previousDM = DMTotal;
            double previousN = NTotal;

            // update all tissues
            foreach (RootTissue tissue in Tissue)
            {
                tissue.Update();
            }

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

            // save N availability values to use later // FIX, remove min()
            waterDuringRungeKutta[Math.Min(3, indexRungeKutta)] = (double[])mySoilWaterAvailable.Clone();
            indexRungeKutta += 1;
        }

        /// <summary>KL modifier due to root damage (0-1).</summary>
        private double KLModiferDueToDamage(int layerIndex)
        {
            var threshold = 0.01;
            if (!IsKLModifierDueToDamageActive)
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
        /// <param name="soilWaterUptake">Soil water uptake, for each soil layer</param>
        internal void EvaluateSoilNitrogenAvailability(ZoneWaterAndN myZone, double[] soilWaterUptake)
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
                mySoilNO3Available[layer] = 0.0;
                mySoilNH4Available[layer] = 0.0;
                if (soilWaterUptake[layer] > 0.0)
                {
                    // get the fraction of this layer that is within the root zone
                    double layerFraction = MathUtilities.Bound((rootingDepth - depthAtTopOfLayer) / thickness[layer], 0.0, 1.0);

                    // get the soil moisture factor (less N available in drier soil)
                    double relativeWaterContent = MathUtilities.Divide(swMM[layer] - llMM[layer], dulMM[layer] - llMM[layer], 0.0);
                    relativeWaterContent = MathUtilities.Bound(relativeWaterContent, 0.0, 1.0);
                    double moistureFactor = 1.0 - Math.Pow(1.0 - relativeWaterContent, NExtractionSWFactorExponent);

                    // get NH4 available
                    double nh4ppm = nh4[layer] * 100.0 / (thickness[layer] * bd[layer]);
                    double concentrationFactor = Math.Min(1.0, nh4ppm * KNH4);
                    mySoilNH4Available[layer] = nh4[layer] * layerFraction * Math.Min(0.999999, moistureFactor * concentrationFactor);

                    // get NO3 available
                    double no3ppm = no3[layer] * 100.0 / (thickness[layer] * bd[layer]);
                    concentrationFactor = Math.Min(1.0, no3ppm * KNO3);
                    mySoilNO3Available[layer] = no3[layer] * layerFraction * Math.Min(0.999999, moistureFactor * concentrationFactor);
                }

                depthAtTopOfLayer += thickness[layer];
            }

            // check totals, reduce available N if greater than maximum uptake
            double potentialAvailableN = mySoilNH4Available.Sum() + mySoilNO3Available.Sum();
            if (potentialAvailableN > MaximumNUptake)
            {
                double upFraction = MathUtilities.Divide(MaximumNUptake, potentialAvailableN, 0.0);
                for (int layer = 0; layer <= BottomLayer; layer++)
                {
                    mySoilNH4Available[layer] *= upFraction;
                    mySoilNO3Available[layer] *= upFraction;
                }
            }

            // save N availability values to use later // FIX, remove min()
            nh4DuringRungeKutta[Math.Min(3, indexRungeKutta)] = (double[])mySoilNH4Available.Clone();
            no3DuringRungeKutta[Math.Min(3, indexRungeKutta)] = (double[])mySoilNO3Available.Clone();
            indexRungeKutta += 1;
        }

        /// <summary>Adjusts the amount of plant available nitrogen (NH4 and NO3) in the soil.</summary>
        /// <remarks>
        /// This is a hack to get an evaluation of availability after the Runge-Kutta iteration process.
        /// This is needed to output values consistent with uptake (i.e. not smaller as can happen), and
        ///  because the model needs to re-evaluate the amount of N fixed (the 'current' value is simply
        ///  based on the last iteration of the Runge-Kutta process and not necessarily in agreement with
        ///  its outcome... This can lead to perceive loss in mass balance in outputs.
        /// </remarks>
        internal void ReEvaluateSoilNitrogenAvailability()
        {
            // update soil N available (average of Runge-Kutta estimates, but no less than uptake)
            for (int layer = 0; layer <= BottomLayer; layer++)
            {
                double avgAvailableNH4 = (nh4DuringRungeKutta[0][layer] + nh4DuringRungeKutta[1][layer]
                                       + nh4DuringRungeKutta[2][layer] + nh4DuringRungeKutta[3][layer]) / 4.0;
                mySoilNH4Available[layer] = Math.Max(avgAvailableNH4, mySoilNH4Uptake[layer]);

                double avgAvailableNO3 = (no3DuringRungeKutta[0][layer] + no3DuringRungeKutta[1][layer]
                                       + no3DuringRungeKutta[2][layer] + no3DuringRungeKutta[3][layer]) / 4.0;
                mySoilNO3Available[layer] = Math.Max(avgAvailableNO3, mySoilNO3Uptake[layer]);
            }

            // clear info from Runge-Kutta
            indexRungeKutta = 0;
            Array.Clear(nh4DuringRungeKutta, 0, 4);
            Array.Clear(no3DuringRungeKutta, 0, 4);
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
                {
                    depthTillTopThisLayer += soilPhysical.Thickness[z];
                }
                fractionInLayer = (rootingDepth - depthTillTopThisLayer) / soilPhysical.Thickness[layer];
                fractionInLayer = Math.Min(1.0, Math.Max(0.0, fractionInLayer));
            }

            return fractionInLayer;
        }

        /// <summary>Computes the target (or ideal) distribution of roots in the soil profile.</summary>
        /// <remarks>
        /// This distribution is solely based on root parameters (maximum depth and distribution parameters)
        /// These values will be used to allocate initial rootDM as well as any growth over the profile
        /// </remarks>
        /// <returns>A weighting factor for each soil layer (mm equivalent)</returns>
        public double[] RootDistributionTarget()
        {
            // Base distribution calculated using a combination of linear and power functions:
            //  It considers homogeneous distribution from surface down to a fraction of root depth (DepthForConstantRootProportion),
            //   below this depth the proportion of root decrease following a power function (with exponent ExponentRootDistribution),
            //   it reaches zero slightly below the MaximumRootDepth (defined by rootBottomDistributionFactor), but the function is
            //   truncated at MaximumRootDepth. The values are not normalised.
            //  The values are further adjusted using the values of XF (so there will be less roots in those layers)

            double[] result = new double[nLayers];
            double depthTop = 0.0;
            double depthBottom = 0.0;
            double depthFirstStage = Math.Min(MaximumAllowedDepth, DepthDistributionParamTop);

            for (int layer = 0; layer < nLayers; layer++)
            {
                depthBottom += soilPhysical.Thickness[layer];
                if (depthTop >= MaximumAllowedDepth)
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
                    double maxRootDepth = MaximumAllowedDepth * DepthDistributionParamBottom;
                    result[layer] = Math.Pow(maxRootDepth - Math.Max(depthTop, depthFirstStage), DepthDistributionExponent + 1)
                                  - Math.Pow(maxRootDepth - Math.Min(depthBottom, MaximumAllowedDepth), DepthDistributionExponent + 1);
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

            // get the total weight over the root zone, first layers totally within the root zone
            for (int layer = 0; layer < BottomLayer; layer++)
            {
                cumProportion += TargetDistribution[layer];
                topLayersDepth += soilPhysical.Thickness[layer];
            }
            // then consider layer at the bottom of the root zone
            double layerFrac = Math.Min(1.0, (MaximumAllowedDepth - topLayersDepth) / (rootingDepth - topLayersDepth));
            cumProportion += TargetDistribution[BottomLayer] * layerFrac;

            // normalise the weights to be a fraction, adds up to one
            if (MathUtilities.IsGreaterThan(cumProportion, 0))
            {
                for (int layer = 0; layer < BottomLayer; layer++)
                {
                    result[layer] = TargetDistribution[layer] / cumProportion;
                }
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
            if (MathUtilities.IsGreaterThan(rootDMToAdd, 0.0))
            {
                // root DM is changing due to growth, check potential changes in distribution
                double[] newGrowthFraction;
                double[] currentRootTarget = CurrentRootDistributionTarget();
                if (MathUtilities.AreEqual(Live.DMFraction, currentRootTarget))
                {
                    // no need to change the distribution
                    newGrowthFraction = Live.DMFraction;
                }
                else
                {
                    // root distribution should change, get preliminary distribution (average of current and target)
                    newGrowthFraction = new double[nLayers];
                    for (int layer = 0; layer <= BottomLayer; layer++)
                    {
                        newGrowthFraction[layer] = 0.5 * (Live.DMFraction[layer] + currentRootTarget[layer]);
                    }

                    // normalise distribution of allocation
                    double layersTotal = newGrowthFraction.Sum();
                    for (int layer = 0; layer <= BottomLayer; layer++)
                    {
                        newGrowthFraction[layer] = newGrowthFraction[layer] / layersTotal;
                    }
                }

                // split the amounts into values for each layer (based on new distribution)
                var newDMLayered = MathUtilities.Multiply_Value(newGrowthFraction, rootDMToAdd);
                var newNLayered = MathUtilities.Multiply_Value(newGrowthFraction, rootNToAdd);

                Live.SetBiomassTransferIn(newDMLayered, newNLayered);
            }
            // TODO: currently only the roots at the main / home zone are considered, must add the other zones too
        }

        /// <summary>Computes the variations in rooting depth.</summary>
        /// <remarks>
        /// Root depth will increase if it is smaller than maximumRootDepth and there is a positive net DM accumulation.
        /// The depth increase rate is of zero-order type, given by the ElongationRate, adjusted for temperature, in the
        ///  same ways as plant DM growth, and soil supply status, increasing root elongation when there is a limitation
        ///  in the soil (the most limiting of water or N supply). Note that currently root depth never decreases.
        /// </remarks>
        /// <param name="netGrowthDM">Net root growth dry matter (kg/ha).</param>
        /// <param name="temperatureLimitingFactor">Growth limiting factor due to temperature.</param>
        /// <param name="soilSupplyFactor">Adjustment to elongation due to to soil supply issues (drought or N).</param>
        public void EvaluateRootElongation(double netGrowthDM, double temperatureLimitingFactor, double soilSupplyFactor)
        {
            // check changes in root depth
            if (netGrowthDM > 0.0)
            {
                double dRootDepth = ElongationRate * soilCropData.XF[BottomLayer] * temperatureLimitingFactor * soilSupplyFactor;
                Depth = Math.Min(MaximumAllowedDepth, Math.Max(MinimumRootingDepth, Depth + dRootDepth));
            }
        }

        /// <summary>Remove water from soil - uptake.</summary>
        /// <param name="amount">Amount of water to remove.</param>
        public void PerformWaterUptake(double[] amount)
        {
            if (MathUtilities.IsGreaterThan(amount.Sum(), 0.0))
            {
                Array.Copy(amount, mySoilWaterUptake, nLayers);
                waterBalance.RemoveWater(amount);
            }

            // clear info from Runge-Kutta
            indexRungeKutta = 0;
            Array.Clear(waterDuringRungeKutta, 0, 4);
        }

        /// <summary>Remove nutrients from soil - uptake.</summary>
        /// <param name="no3Amount">Amount of no3 to remove.</param>
        /// <param name="nh4Amount">Amount of nh4 to remove.</param>
        public void PerformNutrientUptake(double[] no3Amount, double[] nh4Amount)
        {
            if (MathUtilities.IsGreaterThan(nh4Amount.Sum(), 0.0))
            {
                Array.Copy(nh4Amount, mySoilNH4Uptake, nLayers);
                nh4.SetKgHa(SoluteSetterType.Plant, MathUtilities.Subtract(nh4.kgha, nh4Amount));
            }
            if (MathUtilities.IsGreaterThan(no3Amount.Sum(), 0.0))
            {
                Array.Copy(no3Amount, mySoilNO3Uptake, nLayers);
                no3.SetKgHa(SoluteSetterType.Plant, MathUtilities.Subtract(no3.kgha, no3Amount));
            }
        }

        /// <summary>Flag indicating whether roots are in the specified zone.</summary>
        /// <param name="zoneName">The zone name.</param>
        public bool IsInZone(string zoneName)
        {
            return this.zoneName == zoneName;
        }
    }
}
