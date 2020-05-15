namespace Models.AgPasture
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Interfaces;
    using Models.PMF;
    using Models.Soils;
    using Models.Soils.Arbitrator;
    using System;
    using System.Linq;
    using System.Xml.Serialization;

    /// <summary>Describes a generic below ground organ of a pasture species.</summary>
    [Serializable]
    public class PastureBelowGroundOrgan : Model
    {
        /// <summary>Nutrient model.</summary>
        [Link]
        private PastureSpecies species = null;

        /// <summary>The collection of tissues for this organ.</summary>
        [Link(Type=LinkType.Child)]
        internal RootTissue[] Tissue = null;

        /// <summary>Soil object where these roots are growing.</summary>
        public Soil mySoil = null;

        /// <summary>Soil nitrogen model.</summary>
        private INutrient SoilNitrogen;

        /// <summary>The NO3 solute.</summary>
        public ISolute NO3 = null;

        /// <summary>The NH4 solute.</summary>
        public ISolute NH4 = null;

        /// <summary>Constructor, initialise tissues for the roots.</summary>
        /// <param name="zone">The zone the roots belong in.</param>
        /// <param name="initialDM">Initial dry matter weight</param>
        /// <param name="initialDepth">Initial root depth</param>
        /// <param name="minLiveDM">The minimum biomass for this organ</param>
        /// <param name="waterAvailableMethod">Method to compute water available</param>
        /// <param name="nitrogenAvailableMethod">Method to compute N available</param>
        /// <param name="kNH4">Parameter to compute NN4 available, default method</param>
        /// <param name="kNO3">Parameter to compute NO3 available, default method</param>
        /// <param name="maxNUptake">Parameter to compute N uptake, default method</param>
        /// <param name="kuNH4">Parameter to compute NH4 available, alternative method</param>
        /// <param name="kuNO3">Parameter to compute NO3 available, alternative method</param>
        /// <param name="referenceKSuptake">Parameter to compute available water, conductivity</param>
        /// <param name="referenceRLD">Parameter to compute available water, roots</param>
        /// <param name="exponentSoilMoisture">Parameter to compute available water</param>
        public void Initialise(Zone zone, double initialDM, double initialDepth,
                               double minLiveDM,
                               PastureSpecies.PlantAvailableWaterMethod waterAvailableMethod,
                               PastureSpecies.PlantAvailableNitrogenMethod nitrogenAvailableMethod,
                               double kNH4, double kNO3, double maxNUptake,
                               double kuNH4, double kuNO3, double referenceKSuptake,
                               double referenceRLD, double exponentSoilMoisture)
        {
            mySoil = Apsim.Find(zone, typeof(Soil)) as Soil;
            if (mySoil == null)
                throw new Exception($"Cannot find soil in zone {zone.Name}");

            SoilNitrogen = Apsim.Find(zone, typeof(INutrient)) as INutrient;
            if (SoilNitrogen == null)
                throw new Exception($"Cannot find SoilNitrogen in zone {zone.Name}");

            NO3 = Apsim.Find(zone, "NO3") as ISolute;
            if (NO3 == null)
                throw new Exception($"Cannot find NO3 solute in zone {zone.Name}");
            NH4 = Apsim.Find(zone, "NH4") as ISolute;
            if (NH4 == null)
                throw new Exception($"Cannot find NH4 solute in zone {zone.Name}");

            // save the parameters for this organ
            nLayers = mySoil.Thickness.Length;
            MinimumLiveDM = minLiveDM;
            myWaterAvailableMethod = waterAvailableMethod;
            myNitrogenAvailableMethod = nitrogenAvailableMethod;
            myKNO3 = kNO3;
            myKNH4 = kNH4;
            myMaximumNUptake = maxNUptake;
            myKuNH4 = kuNH4;
            myKuNO3 = kuNO3;
            myReferenceKSuptake = referenceKSuptake;
            myReferenceRLD = referenceRLD;
            myExponentSoilMoisture = exponentSoilMoisture;

            // Link to soil and initialise variables
            myZoneName = mySoil.Parent.Name;
            mySoilNH4Available = new double[nLayers];
            mySoilNO3Available = new double[nLayers];

            // Initialise root DM, N, depth, and distribution
            Depth = initialDepth;
            TargetDistribution = RootDistributionTarget();

            double[] initialDMByLayer = MathUtilities.Multiply_Value(CurrentRootDistributionTarget(), initialDM);
            double[] initialNByLayer = MathUtilities.Multiply_Value(initialDMByLayer, NConcOptimum);

            // Initialise the live tissue.
            Tissue[0].Initialise(initialDMByLayer, initialNByLayer);
            Tissue[1].Initialise(null, null);
        }

        #region Root specific characteristics  -----------------------------------------------------------------------------

        /// <summary>Name of root zone.</summary>
        internal string myZoneName { get; private set; }

        /// <summary>Gets or sets the N concentration for optimum growth (kg/kg).</summary>
        [XmlIgnore]
        public double NConcOptimum { get; set; } = 0.02;

        /// <summary>Gets or sets the minimum N concentration, structural N (kg/kg).</summary>
        [XmlIgnore]
        public double NConcMinimum { get; set; } = 0.006;

        /// <summary>Gets or sets the maximum N concentration, for luxury uptake (kg/kg).</summary>
        [XmlIgnore]
        public double NConcMaximum { get; set; } = 0.025;

        /// <summary>Depth from surface where root proportion starts to decrease (mm).</summary>
        [XmlIgnore]
        [Units("mm")]
        public double RootDistributionDepthParam { get; set; } = 90.0;

        /// <summary>Exponent controlling the root distribution as function of depth (>0.0).</summary>
        [XmlIgnore]
        [Units("-")]
        public double RootDistributionExponent { get; set; } = 3.2;

        /// <summary>Factor for root distribution; controls where the function is zero below maxRootDepth.</summary>
        [XmlIgnore]
        public double RootBottomDistributionFactor { get; set; } = 1.05;

        /// <summary>Minimum DM amount of live tissues (kg/ha).</summary>
        internal double MinimumLiveDM = 0.0;

        /// <summary>Specific root length (m/gDM).</summary>
        [XmlIgnore]
        public double SpecificRootLength { get; set; } = 100.0;

        /// <summary>Minimum rooting depth (mm).</summary>
        [XmlIgnore]
        public double RootDepthMinimum { get; set; } = 50.0;

        /// <summary>Maximum rooting depth (mm).</summary>
        [XmlIgnore]
        public double RootDepthMaximum { get; set; } = 750.0;

        /// <summary>Daily root elongation rate at optimum temperature (mm/day).</summary>
        [Units("mm/day")]
        [XmlIgnore]
        public double RootElongationRate { get; set; } = 25.0;

        /// <summary>Flag which method for computing soil available water will be used.</summary>
        private PastureSpecies.PlantAvailableWaterMethod myWaterAvailableMethod;

        /// <summary>Flag which method for computing available soil nitrogen will be used.</summary>
        private PastureSpecies.PlantAvailableNitrogenMethod myNitrogenAvailableMethod = PastureSpecies.PlantAvailableNitrogenMethod.BasicAgPasture;

        /// <summary>Ammonium uptake coefficient.</summary>
        private double myKNH4 = 1.0;

        /// <summary>Nitrate uptake coefficient.</summary>
        private double myKNO3 = 1.0;

        /// <summary>Maximum daily amount of N that can be taken up by the plant (kg/ha).</summary>
        private double myMaximumNUptake = 10.0;

        /// <summary>Availability factor for NH4.</summary>
        private double myKuNH4 = 0.50;

        /// <summary>Availability factor for NO3.</summary>
        private double myKuNO3 = 0.95;

        /// <summary>Reference value for root length density for the Water and N availability.</summary>
        private double myReferenceRLD = 5.0;

        /// <summary>Exponent controlling the effect of soil moisture variations on water extractability.</summary>
        private double myExponentSoilMoisture = 1.50;

        /// <summary>Reference value of Ksat for water availability function.</summary>
        private double myReferenceKSuptake = 15.0;

        /// <summary>Number of layers in the soil.</summary>
        private int nLayers;

        /// <summary>Gets or sets the rooting depth (mm).</summary>
        internal double Depth { get; set; }

        /// <summary>Gets or sets the layer at the bottom of the root zone.</summary>
        internal int BottomLayer 
        {
            get { return RootZoneBottomLayer(); }
        }

        /// <summary>Gets or sets the target (ideal) DM fractions for each layer (0-1).</summary>
        internal double[] TargetDistribution { get; set; }

        #endregion ---------------------------------------------------------------------------------------------------------

        #region Organ Properties (summary of tissues)  ---------------------------------------------------------------------

        /// <summary>Gets the total dry matter in this organ (kg/ha).</summary>
        internal double DMTotal
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < Tissue.Length; t++)
                    result += Tissue[t].DM.Wt;

                return result;
            }
        }

        /// <summary>Gets the dry matter in the live (green) tissues (kg/ha).</summary>
        internal double DMLive
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < Tissue.Length - 1; t++)
                    result += Tissue[t].DM.Wt;

                return result;
            }
        }

        /// <summary>Gets the dry matter in the dead tissues (kg/ha).</summary>
        /// <remarks>Last tissues is assumed to represent dead material.</remarks>
        internal double DMDead
        {
            get { return Tissue[Tissue.Length - 1].DM.Wt; }
        }

        /// <summary>The total N amount in this tissue (kg/ha).</summary>
        internal double NTotal
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < Tissue.Length; t++)
                    result += Tissue[t].DM.N;

                return result;
            }
        }

        /// <summary>Gets the N amount in the live (green) tissues (kg/ha).</summary>
        internal double NLive
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < Tissue.Length - 1; t++)
                    result += Tissue[t].DM.N;

                return result;
            }
        }

        /// <summary>Gets the N amount in the dead tissues (kg/ha).</summary>
        /// <remarks>Last tissues is assumed to represent dead material.</remarks>
        internal double NDead
        {
            get { return Tissue[Tissue.Length - 1].DM.N; }
        }

        /// <summary>Gets the average N concentration in this organ (kg/kg).</summary>
        internal double NconcTotal
        {
            get { return MathUtilities.Divide(NTotal, DMTotal, 0.0); }
        }

        /// <summary>Gets the average N concentration in the live tissues (kg/kg).</summary>
        internal double NconcLive
        {
            get { return MathUtilities.Divide(NLive, DMLive, 0.0); }
        }

        /// <summary>Gets the average N concentration in dead tissues (kg/kg).</summary>
        internal double NconcDead
        {
            get { return MathUtilities.Divide(NDead, DMDead, 0.0); }
        }

        /// <summary>Gets the amount of senesced N available for remobilisation (kg/ha).</summary>
        internal double NSenescedRemobilisable
        {
            get { return Tissue[Tissue.Length - 1].NRemobilisable; }
        }

        /// <summary>Gets the amount of luxury N available for remobilisation (kg/ha).</summary>
        internal double NLuxuryRemobilisable
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < Tissue.Length - 1; t++)
                    result += Tissue[t].NRemobilisable;

                return result;
            }
        }

        /// <summary>Finds out the amount of plant available water in the soil.</summary>
        /// <param name="myZone">The soil information</param>
        internal double[] EvaluateSoilWaterAvailable(ZoneWaterAndN myZone)
        {
            if (myWaterAvailableMethod == PastureSpecies.PlantAvailableWaterMethod.DefaultAPSIM)
                return PlantAvailableSoilWaterDefault(myZone);
            else if (myWaterAvailableMethod == PastureSpecies.PlantAvailableWaterMethod.AlternativeKL)
                return PlantAvailableSoilWaterAlternativeKL(myZone);
            else if (myWaterAvailableMethod == PastureSpecies.PlantAvailableWaterMethod.AlternativeKS)
                return PlantAvailableSoilWaterAlternativeKS(myZone);
            else
                throw new Exception("Invalid water uptake method found");
        }

        /// <summary>Estimates the amount of plant available water in each soil layer of the root zone.</summary>
        /// <remarks>This is the default APSIM method, with kl representing the daily rate for water extraction</remarks>
        /// <param name="myZone">The soil information</param>
        /// <returns>The amount of available water in each layer (mm)</returns>
        internal double[] PlantAvailableSoilWaterDefault(ZoneWaterAndN myZone)
        {
            double[] result = new double[nLayers];
            SoilCrop soilCropData = (SoilCrop)mySoil.Crop(species.Name);
            for (int layer = 0; layer <= BottomLayer; layer++)
            {
                result[layer] = Math.Max(0.0, myZone.Water[layer] - (soilCropData.LL[layer] * mySoil.Thickness[layer]));
                result[layer] *= FractionLayerWithRoots(layer) * soilCropData.KL[layer] * KLModiferDueToDamage(layer);
            }

            return result;
        }

        /// <summary>Estimates the amount of plant available  water in each soil layer of the root zone.</summary>
        /// <remarks>
        /// This is an alternative method, kl representing a soil limiting factor for water extraction (clayey soils have lower values)
        ///  this is further modified by soil water content (a reduction for dry soil). A plant related factor is defined based on root
        ///  length density (limiting conditions when RLD is below ReferenceRLD)
        /// </remarks>
        /// <param name="myZone">The soil information</param>
        /// <returns>The amount of available water in each layer (mm)</returns>
        internal double[] PlantAvailableSoilWaterAlternativeKL(ZoneWaterAndN myZone)
        {
            double[] result = new double[nLayers];
            SoilCrop soilCropData = (SoilCrop)mySoil.Crop(species.Name);
            for (int layer = 0; layer <= BottomLayer; layer++)
            {
                double rldFac = Math.Min(1.0, RootLengthDensity[layer] / myReferenceRLD);
                double swFac;
                if (mySoil.SoilWater.SWmm[layer] >= mySoil.DULmm[layer])
                    swFac = 1.0;
                else if (mySoil.SoilWater.SWmm[layer] <= mySoil.LL15mm[layer])
                    swFac = 0.0;
                else
                {
                    double waterRatio = (myZone.Water[layer] - mySoil.LL15mm[layer]) /
                                        (mySoil.DULmm[layer] - mySoil.LL15mm[layer]);
                    swFac = 1.0 - Math.Pow(1.0 - waterRatio, myExponentSoilMoisture);
                }

                // Total available water
                result[layer] = Math.Max(0.0, myZone.Water[layer] - (soilCropData.LL[layer] * mySoil.Thickness[layer]));

                // Actual plant available water
                result[layer] *= FractionLayerWithRoots(layer) * Math.Min(1.0, soilCropData.KL[layer] 
                                                                               * KLModiferDueToDamage(layer) 
                                                                               * swFac * rldFac);
            }

            return result;
        }

        /// <summary>Estimates the amount of plant available water in each soil layer of the root zone.</summary>
        /// <remarks>
        /// This is an alternative method, which does not use kl. A factor based on Ksat is used instead. This is further modified
        ///  by soil water content and a plant related factor, defined based on root length density. All three factors are normalised 
        ///  (using ReferenceKSat and ReferenceRLD for KSat and root and DUL for soil water content). The effect of all factors are
        ///  assumed to vary between zero and one following exponential functions, such that the effect is 90% at the reference value.
        /// </remarks>
        /// <param name="myZone">The soil information</param>
        /// <returns>The amount of available water in each layer (mm)</returns>
        internal double[] PlantAvailableSoilWaterAlternativeKS(ZoneWaterAndN myZone)
        {
            double[] result = new double[nLayers];
            SoilCrop soilCropData = (SoilCrop)mySoil.Crop(species.Name);
            for (int layer = 0; layer <= BottomLayer; layer++)
            {
                double condFac = 1.0 - Math.Pow(10.0, -mySoil.KS[layer] / myReferenceKSuptake);
                double rldFac = 1.0 - Math.Pow(10.0, -RootLengthDensity[layer] / myReferenceRLD);
                double swFac;
                if (mySoil.SoilWater.SWmm[layer] >= mySoil.DULmm[layer])
                    swFac = 1.0;
                else if (mySoil.SoilWater.SWmm[layer] <= mySoil.LL15mm[layer])
                    swFac = 0.0;
                else
                {
                    double waterRatio = (myZone.Water[layer] - mySoil.LL15mm[layer]) /
                                        (mySoil.DULmm[layer] - mySoil.LL15mm[layer]);
                    swFac = 1.0 - Math.Pow(1.0 - waterRatio, myExponentSoilMoisture);
                }

                // Total available water
                result[layer] = Math.Max(0.0, myZone.Water[layer] - soilCropData.LL[layer]) * mySoil.Thickness[layer];

                // Actual plant available water
                result[layer] *= FractionLayerWithRoots(layer) * Math.Min(1.0, rldFac * condFac * swFac);
            }

            return result;
        }

        /// <summary>Gets the root length density by volume (mm/mm^3).</summary>
        public double[] RootLengthDensity
        {
            get
            {
                double[] result = new double[nLayers];
                double totalRootLength = Tissue[0].DM.Wt * SpecificRootLength; // m root/m2 
                totalRootLength *= 0.0000001; // convert into mm root/mm2 soil)
                for (int layer = 0; layer < result.Length; layer++)
                {
                    result[layer] = Tissue[0].FractionWt[layer] * totalRootLength / mySoil.Thickness[layer];
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

        /// <summary>Gets the KL modifier due to root damage (0-1).</summary>
        private double KLModiferDueToDamage(int layerIndex)
        {
            var threshold = 0.01;
            if (!IsKLModiferDueToDamageActive)
                return 1;
            else if (RootLengthDensity[layerIndex] < 0)
                return 0;
            else if (RootLengthDensity[layerIndex] >= threshold)
                return 1;
            else
                return (1 / threshold) * RootLengthDensity[layerIndex];
        }
        #endregion ---------------------------------------------------------------------------------------------------------

        #region Organ methods  ---------------------------------------------------------------------------------------------


        /// <summary>
        /// Reset this root organ's state.
        /// </summary>
        /// <param name="rootWt">The amount of root biomass (kg/ha).</param>
        /// <param name="rootDepth">The depth of roots to reset to(mm).</param>
        public void Reset(double rootWt, double rootDepth)
        {
            Depth = rootDepth;

            var rootFractions = CurrentRootDistributionTarget();
            var rootBiomass = MathUtilities.Multiply_Value(CurrentRootDistributionTarget(), rootWt);
            Tissue[0].ResetTo(rootBiomass);
        }

        /// <summary>Reset all amounts to zero in all tissues of this organ.</summary>
        internal void DoResetOrgan()
        {
            Depth = 0;
            for (int t = 0; t < Tissue.Length; t++)
            {
                Tissue[t].Reset();
                DoCleanTransferAmounts();
            }
        }

        /// <summary>Reset the transfer amounts in all tissues of this organ.</summary>
        internal void DoCleanTransferAmounts()
        {
            for (int t = 0; t < Tissue.Length; t++)
                Tissue[t].DailyReset();
        }

        /// <summary>Kills part of the organ (transfer DM and N to dead tissue).</summary>
        /// <param name="fractionToRemove">The fraction to kill in each tissue</param>
        internal void DoKillOrgan(double fractionToRemove = 1.0)
        {
            Tissue[0].MoveFractionToTissue(fractionToRemove, Tissue[1]);
        }

        /// <summary>Removes biomass from root layers when harvest, graze or cut events are called.</summary>
        /// <param name="biomassRemoveType">Name of event that triggered this biomass remove call.</param>
        /// <param name="biomassToRemove">The fractions of biomass to remove</param>
        public void RemoveBiomass(string biomassRemoveType, OrganBiomassRemovalType biomassToRemove)
        {
            // Live removal
            for (int t = 0; t < Tissue.Length - 1; t++)
            {
                Tissue[t].RemoveBiomass(biomassToRemove.FractionLiveToRemove, sendToSurfaceOrganicMatter: false);
                Tissue[t].RemoveBiomass(biomassToRemove.FractionLiveToResidue, sendToSurfaceOrganicMatter: true);
            }

            // Dead removal
            Tissue[Tissue.Length - 1].RemoveBiomass(biomassToRemove.FractionDeadToRemove, sendToSurfaceOrganicMatter: false);
            Tissue[Tissue.Length - 1].RemoveBiomass(biomassToRemove.FractionDeadToResidue, sendToSurfaceOrganicMatter:true);

            if (biomassRemoveType != "Harvest")
                IsKLModiferDueToDamageActive = true;
        }

        /// <summary>Computes the DM and N amounts turned over for all tissues.</summary>
        /// <param name="turnoverRate">The turnover rate for each tissue</param>
        /// <returns>The DM and N amount detached from this organ</returns>
        internal BiomassAndN DoTissueTurnover(double[] turnoverRate)
        {
            Tissue[0].DoTissueTurnover(turnoverRate[0], BottomLayer, Tissue[1], NconcLive - NConcOptimum);
            return Tissue[1].DoTissueTurnover(turnoverRate[1], BottomLayer, null, NconcLive - NConcMinimum);
        }

        /// <summary>Updates each tissue, make changes in DM and N effective.</summary>
        internal void DoOrganUpdate()
        {
            RootTissue.UpdateTissues(Tissue[0], Tissue[1]);
        }

        /// <summary>Finds out the amount of plant available nitrogen (NH4 and NO3) in the soil.</summary>
        /// <param name="myZone">The soil information</param>
        /// <param name="mySoilWaterUptake">Soil water uptake</param>
        internal void EvaluateSoilNitrogenAvailable(ZoneWaterAndN myZone, double[] mySoilWaterUptake)
        {
            if (myNitrogenAvailableMethod == PastureSpecies.PlantAvailableNitrogenMethod.BasicAgPasture)
                PlantAvailableSoilNBasicAgPasture(myZone);
            else if (myNitrogenAvailableMethod == PastureSpecies.PlantAvailableNitrogenMethod.DefaultAPSIM)
                PlantAvailableSoilNDefaultAPSIM(myZone);
            else if (myNitrogenAvailableMethod == PastureSpecies.PlantAvailableNitrogenMethod.AlternativeRLD)
                PlantAvailableSoilNAlternativeRLD(myZone);
            else if (myNitrogenAvailableMethod == PastureSpecies.PlantAvailableNitrogenMethod.AlternativeWup)
                PlantAvailableSoilNAlternativeWup(myZone, mySoilWaterUptake);
        }

        /// <summary>Estimates the amount of plant available nitrogen in each soil layer of the root zone.</summary>
        /// <remarks>This is a basic method, used as default in old AgPasture, all N in the root zone is available</remarks>
        /// <param name="myZone">The soil information</param>
        private void PlantAvailableSoilNBasicAgPasture(ZoneWaterAndN myZone)
        {
            double layerFrac; // the fraction of layer within the root zone
            for (int layer = 0; layer <= BottomLayer; layer++)
            {
                layerFrac = FractionLayerWithRoots(layer);
                mySoilNH4Available[layer] = myZone.PlantAvailableNH4N[layer] * layerFrac;
                mySoilNO3Available[layer] = myZone.PlantAvailableNO3N[layer] * layerFrac;
            }
        }

        /// <summary>Estimates the amount of plant available nitrogen in each soil layer of the root zone.</summary>
        /// <remarks>
        /// This method approximates the default approach in APSIM plants (method 3 in Plant1 models)
        /// Soil water status and uptake coefficient control the availability, which is a square function of N content.
        /// Uptake is capped for a maximum value plants can take in one day.
        /// </remarks>
        /// <param name="myZone">The soil information</param>
        private void PlantAvailableSoilNDefaultAPSIM(ZoneWaterAndN myZone)
        {
            double layerFrac; // the fraction of layer within the root zone
            double swFac;  // the soil water factor
            double bdFac;  // the soil density factor
            double potAvailableN; // potential available N
            for (int layer = 0; layer <= BottomLayer; layer++)
            {
                layerFrac = FractionLayerWithRoots(layer);
                bdFac = 100.0 / (mySoil.Thickness[layer] * mySoil.BD[layer]);
                if (myZone.Water[layer] >= mySoil.DULmm[layer])
                    swFac = 1.0;
                else if (myZone.Water[layer] <= mySoil.LL15mm[layer])
                    swFac = 0.0;
                else
                {
                    double waterRatio = (myZone.Water[layer] - mySoil.LL15mm[layer]) /
                                        (mySoil.DULmm[layer] - mySoil.LL15mm[layer]);
                    waterRatio = MathUtilities.Bound(waterRatio, 0.0, 1.0);
                    swFac = 1.0 - Math.Pow(1.0 - waterRatio, myExponentSoilMoisture);
                }

                // get NH4 available
                potAvailableN = Math.Pow(myZone.PlantAvailableNH4N[layer] * layerFrac, 2.0) * swFac * bdFac * myKNH4;
                mySoilNH4Available[layer] = Math.Min(myZone.PlantAvailableNH4N[layer] * layerFrac, potAvailableN);

                // get NO3 available
                potAvailableN = Math.Pow(myZone.PlantAvailableNO3N[layer] * layerFrac, 2.0) * swFac * bdFac * myKNO3;
                mySoilNO3Available[layer] = Math.Min(myZone.PlantAvailableNO3N[layer] * layerFrac, potAvailableN);
            }

            // check for maximum uptake
            potAvailableN = mySoilNH4Available.Sum() + mySoilNO3Available.Sum();
            if (potAvailableN > myMaximumNUptake)
            {
                double upFraction = myMaximumNUptake / potAvailableN;
                for (int layer = 0; layer <= BottomLayer; layer++)
                {
                    mySoilNH4Available[layer] *= upFraction;
                    mySoilNO3Available[layer] *= upFraction;
                }
            }
        }

        /// <summary>Estimates the amount of plant available nitrogen in each soil layer of the root zone.</summary>
        /// <remarks>
        /// This method considers soil water status and root length density to define factors controlling N availability.
        /// Soil water status is used to define a factor that varies from zero at LL, below which no uptake can happen, 
        ///  to one at DUL, above which no restrictions to uptake exist.
        /// Root length density is used to define a factor varying from zero if there are no roots to one when root length
        ///  density is equal to a ReferenceRLD, above which there are no restrictions for uptake.
        /// Factors for each N form can also alter the amount available.
        /// Uptake is caped for a maximum value plants can take in one day.
        /// </remarks>
        /// <param name="myZone">The soil information</param>
        private void PlantAvailableSoilNAlternativeRLD(ZoneWaterAndN myZone)
        {
            double layerFrac; // the fraction of layer within the root zone
            double swFac;  // the soil water factor
            double rldFac;  // the root density factor
            double potAvailableN; // potential available N
            for (int layer = 0; layer <= BottomLayer; layer++)
            {
                layerFrac = FractionLayerWithRoots(layer);
                rldFac = Math.Min(1.0, MathUtilities.Divide(RootLengthDensity[layer], myReferenceRLD, 1.0));
                if (myZone.Water[layer] >= mySoil.DULmm[layer])
                    swFac = 1.0;
                else if (myZone.Water[layer] <= mySoil.LL15mm[layer])
                    swFac = 0.0;
                else
                {
                    double waterRatio = (myZone.Water[layer] - mySoil.LL15mm[layer]) /
                                        (mySoil.DULmm[layer] - mySoil.LL15mm[layer]);
                    swFac = 1.0 - Math.Pow(1.0 - waterRatio, myExponentSoilMoisture);
                }

                // get NH4 available
                potAvailableN = myZone.PlantAvailableNH4N[layer] * layerFrac;
                mySoilNH4Available[layer] = potAvailableN * Math.Min(1.0, swFac * rldFac * myKuNH4);

                // get NO3 available
                potAvailableN = myZone.PlantAvailableNO3N[layer] * layerFrac;
                mySoilNO3Available[layer] = potAvailableN * Math.Min(1.0, swFac * rldFac * myKuNO3);
            }

            // check for maximum uptake
            potAvailableN = mySoilNH4Available.Sum() + mySoilNO3Available.Sum();
            if (potAvailableN > myMaximumNUptake)
            {
                double upFraction = myMaximumNUptake / potAvailableN;
                for (int layer = 0; layer <= BottomLayer; layer++)
                {
                    mySoilNH4Available[layer] *= upFraction;
                    mySoilNO3Available[layer] *= upFraction;
                }
            }
        }

        /// <summary>Estimates the amount of plant available nitrogen in each soil layer of the root zone.</summary>
        /// <remarks>
        /// This method considers soil water as the main factor controlling N availability/uptake.
        /// Availability is given by the proportion of water taken up in each layer, further modified by uptake factors
        /// Uptake is caped for a maximum value plants can take in one day.
        /// </remarks>
        /// <param name="myZone">The soil information</param>
        /// <param name="mySoilWaterUptake">Soil water uptake</param>
        private void PlantAvailableSoilNAlternativeWup(ZoneWaterAndN myZone, double[] mySoilWaterUptake)
        {
            double layerFrac; // the fraction of layer within the root zone
            double potAvailableN; // potential available N
            for (int layer = 0; layer <= BottomLayer; layer++)
            {
                layerFrac = FractionLayerWithRoots(layer);
                double swuFac = MathUtilities.Divide(mySoilWaterUptake[layer], myZone.Water[layer], 0.0);

                // get NH4 available
                potAvailableN = myZone.PlantAvailableNH4N[layer] * layerFrac;
                mySoilNH4Available[layer] = potAvailableN * Math.Min(1.0, swuFac * myKuNH4);

                // get NO3 available
                potAvailableN = myZone.PlantAvailableNO3N[layer] * layerFrac;
                mySoilNO3Available[layer] = potAvailableN * Math.Min(1.0, swuFac * myKuNO3);
            }   

            // check for maximum uptake
            potAvailableN = mySoilNH4Available.Sum() + mySoilNO3Available.Sum();
            if (potAvailableN > myMaximumNUptake)
            {
                double upFraction = myMaximumNUptake / potAvailableN;
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
                    depthTillTopThisLayer += mySoil.Thickness[z];
                fractionInLayer = (Depth - depthTillTopThisLayer) / mySoil.Thickness[layer];
                fractionInLayer = Math.Min(1.0, Math.Max(0.0, fractionInLayer));
            }

            return fractionInLayer;
        }

        /// <summary>Gets the index of the layer at the bottom of the root zone.</summary>
        /// <returns>The index of a layer</returns>
        private int RootZoneBottomLayer()
        {
            int result = 0;
            double currentDepth = 0.0;
            for (int layer = 0; layer < nLayers; layer++)
            {
                if (Depth > currentDepth)
                {
                    result = layer;
                    currentDepth += mySoil.Thickness[layer];
                }
                else
                    layer = nLayers;
            }

            return result;
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
            SoilCrop soilCropData = (SoilCrop)mySoil.Crop(species.Name);
            double depthTop = 0.0;
            double depthBottom = 0.0;
            double depthFirstStage = Math.Min(RootDepthMaximum, RootDistributionDepthParam);

            for (int layer = 0; layer < nLayers; layer++)
            {
                depthBottom += mySoil.Thickness[layer];
                if (depthTop >= RootDepthMaximum)
                {
                    // totally out of root zone
                    result[layer] = 0.0;
                }
                else if (depthBottom <= depthFirstStage)
                {
                    // totally in the first stage
                    result[layer] = mySoil.Thickness[layer] * soilCropData.XF[layer];
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

                depthTop += mySoil.Thickness[layer];
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
                topLayersDepth += mySoil.Thickness[layer];
            }
            // Then consider layer at the bottom of the root zone
            double layerFrac = Math.Min(1.0, (RootDepthMaximum - topLayersDepth) / (Depth - topLayersDepth));
            cumProportion += TargetDistribution[BottomLayer] * layerFrac;

            // Normalise the weights to be a fraction, adds up to one
            if (cumProportion > Epsilon)
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
            if (dGrowthRootDM > Epsilon)
            {
                // root DM is changing due to growth, check potential changes in distribution
                double[] growthRootFraction;
                double[] currentRootTarget = CurrentRootDistributionTarget();
                if (MathUtilities.AreEqual(Tissue[0].FractionWt, currentRootTarget))
                {
                    // no need to change the distribution
                    growthRootFraction = Tissue[0].FractionWt;
                }
                else
                {
                    // root distribution should change, get preliminary distribution (average of current and target)
                    growthRootFraction = new double[nLayers];
                    for (int layer = 0; layer <= BottomLayer; layer++)
                        growthRootFraction[layer] = 0.5 * (Tissue[0].FractionWt[layer] + currentRootTarget[layer]);

                    // normalise distribution of allocation
                    double layersTotal = growthRootFraction.Sum();
                    for (int layer = 0; layer <= BottomLayer; layer++)
                        growthRootFraction[layer] = growthRootFraction[layer] / layersTotal;
                }

                Tissue[0].SetBiomassTransferIn(dm: MathUtilities.Multiply_Value(growthRootFraction, dGrowthRootDM),
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
            if (((dGrowthRootDM - detachedRootDM) > Epsilon) && (Depth < RootDepthMaximum))
            {
                double tempFactor = 0.5 + 0.5 * temperatureLimitingFactor;
                dRootDepth = RootElongationRate * tempFactor;
                Depth = Math.Min(RootDepthMaximum, Math.Max(RootDepthMinimum, Depth + dRootDepth));
            }
            else
            {
                // No net growth
                dRootDepth = 0.0;
            }
        }

        /// <summary>User is ending the pasture.</summary>
        public void DoEndCrop()
        {
            Tissue[0].DetachBiomass(DMTotal, NTotal);
        }

        #endregion ---------------------------------------------------------------------------------------------------------


        /// <summary>Minimum significant difference between two values.</summary>
        const double Epsilon = 0.000000001;
    }
   
}
