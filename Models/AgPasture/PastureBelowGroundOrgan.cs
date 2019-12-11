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

    /// <summary>Describes a generic below ground organ of a pasture species.</summary>
    [Serializable]
    public class PastureBelowGroundOrgan
    {
        /// <summary>Soil object where these roots are growing.</summary>
        public Soil mySoil = null;

        /// <summary>Soil nitrogen model.</summary>
        private INutrient SoilNitrogen;

        /// <summary>The NO3 solute.</summary>
        public ISolute NO3 = null;

        /// <summary>The NH4 solute.</summary>
        public ISolute NH4 = null;

        /// <summary>The collection of tissues for this organ.</summary>
        internal RootTissue[] Tissue { get; set; }

        /// <summary>Constructor, initialise tissues for the roots.</summary>
        /// <param name="nameOfSpecies">Name of the pasture species</param>
        /// <param name="numTissues">Number of tissues in this organ</param>
        /// <param name="initialDM">Initial dry matter weight</param>
        /// <param name="initialDepth">Initial root depth</param>
        /// <param name="optNconc">The optimum N concentration</param>
        /// <param name="minNconc">The minimum N concentration</param>
        /// <param name="maxNconc">The maximum N concentration</param>
        /// <param name="minLiveDM">The minimum biomass for this organ</param>
        /// <param name="fractionLuxNremobilisable">Fraction of luxury N that can be remobilise in one day</param>
        /// <param name="specificRootLength">The specific root length (m/g)</param>
        /// <param name="rootDepthMaximum">The maximum root depth</param>
        /// <param name="rootDistributionDepthParam">Parameter to compute root distribution, depth with constant root</param>
        /// <param name="rootBottomDistributionFactor">Parameter to compute root distribution, </param>
        /// <param name="rootDistributionExponent">Parameter to compute root distribution, exponent for root decrease</param>
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
        /// <param name="theSoil">Reference to the soil in the zone these roots are in</param>
        public PastureBelowGroundOrgan(string nameOfSpecies, int numTissues,
                                       double initialDM, double initialDepth,
                                       double optNconc, double minNconc, double maxNconc,
                                       double minLiveDM, double fractionLuxNremobilisable,
                                       double specificRootLength, double rootDepthMaximum,
                                       double rootDistributionDepthParam, double rootDistributionExponent,
                                       double rootBottomDistributionFactor,
                                       PastureSpecies.PlantAvailableWaterMethod waterAvailableMethod,
                                       PastureSpecies.PlantAvailableNitrogenMethod nitrogenAvailableMethod,
                                       double kNH4, double kNO3, double maxNUptake,
                                       double kuNH4, double kuNO3, double referenceKSuptake,
                                       double referenceRLD, double exponentSoilMoisture,
                                       Soil theSoil)
        {
            mySoil = theSoil;
            SoilNitrogen = Apsim.Find(mySoil, typeof(INutrient)) as INutrient;
            if (SoilNitrogen == null)
                throw new Exception("Cannot find SoilNitrogen in zone");

            // Typically two tissues below ground, one live and one dead
            Tissue = new RootTissue[numTissues];
            nLayers = theSoil.Thickness.Length;
            for (int t = 0; t < Tissue.Length; t++)
                Tissue[t] = new RootTissue(nameOfSpecies, SoilNitrogen, nLayers);

            // save the parameters for this organ
            mySpeciesName = nameOfSpecies;            
            NConcOptimum = optNconc;
            NConcMinimum = minNconc;
            NConcMaximum = maxNconc;
            MinimumLiveDM = minLiveDM;
            Tissue[0].FractionNLuxuryRemobilisable = fractionLuxNremobilisable;
            mySpecificRootLength = specificRootLength;
            myRootDepthMaximum = rootDepthMaximum;
            myRootDistributionDepthParam = rootDistributionDepthParam;
            myRootDistributionExponent = rootDistributionExponent;
            myRootBottomDistributionFactor = rootBottomDistributionFactor;
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
            NO3 = Apsim.Find(mySoil, "NO3") as ISolute;
            NH4 = Apsim.Find(mySoil, "NH4") as ISolute;

            // Initialise root DM, N, depth, and distribution
            Depth = initialDepth;
            TargetDistribution = RootDistributionTarget();
            double[] iniRootFraction = CurrentRootDistributionTarget();
            for (int layer = 0; layer < nLayers; layer++)
            {
                Tissue[0].DMLayer[layer] = initialDM * iniRootFraction[layer];
                Tissue[0].NamountLayer[layer] = NConcOptimum * Tissue[0].DMLayer[layer];
            }
        }

        #region Root specific characteristics  -----------------------------------------------------------------------------

        /// <summary>Name of pasture species</summary>
        private string mySpeciesName;

        /// <summary>Name of root zone.</summary>
        internal string myZoneName { get; private set; }

        /// <summary>Gets or sets the N concentration for optimum growth (kg/kg).</summary>
        internal double NConcOptimum = 2.0;

        /// <summary>Gets or sets the maximum N concentration, for luxury uptake (kg/kg).</summary>
        internal double NConcMaximum = 2.5;

        /// <summary>Gets or sets the minimum N concentration, structural N (kg/kg).</summary>
        internal double NConcMinimum = 0.6;

        /// <summary>Minimum DM amount of live tissues (kg/ha).</summary>
        internal double MinimumLiveDM = 0.0;

        /// <summary>Specific root length (m/gDM).</summary>
        private double mySpecificRootLength = 100.0;

        /// <summary>Maximum rooting depth (mm).</summary>
        private double myRootDepthMaximum = 750.0;

        /// <summary>Depth from surface where root proportion starts to decrease (mm).</summary>
        private double myRootDistributionDepthParam = 90.0;

        /// <summary>Exponent controlling the root distribution as function of depth (>0.0).</summary>
        private double myRootDistributionExponent = 3.2;

        /// <summary>Factor to compute root distribution (controls where, below maxRootDepth, the function is zero).</summary>
        private double myRootBottomDistributionFactor = 1.05;

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
                    result += Tissue[t].DM;

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
                    result += Tissue[t].DM;

                return result;
            }
        }

        /// <summary>Gets the dry matter in the dead tissues (kg/ha).</summary>
        /// <remarks>Last tissues is assumed to represent dead material.</remarks>
        internal double DMDead
        {
            get { return Tissue[Tissue.Length - 1].DM; }
        }

        /// <summary>The total N amount in this tissue (kg/ha).</summary>
        internal double NTotal
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < Tissue.Length; t++)
                    result += Tissue[t].Namount;

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
                    result += Tissue[t].Namount;

                return result;
            }
        }

        /// <summary>Gets the N amount in the dead tissues (kg/ha).</summary>
        /// <remarks>Last tissues is assumed to represent dead material.</remarks>
        internal double NDead
        {
            get { return Tissue[Tissue.Length - 1].Namount; }
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

        /// <summary>Gets the amount of senesced N remobilised into new growth (kg/ha).</summary>
        internal double NSenescedRemobilised
        {
            get { return Tissue[Tissue.Length - 1].NRemobilised; }
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

        /// <summary>Gets the amount of senesced N remobilised into new growth (kg/ha).</summary>
        internal double NLuxuryRemobilised
        {
            get
            {
                double result = 0.0;
                for (int t = 0; t < Tissue.Length - 1; t++)
                    result += Tissue[t].NRemobilised;

                return result;
            }
        }

        /// <summary>Gets the DM amount added to this organ via growth (kg/ha).</summary>
        internal double DMGrowth
        {
            get { return Tissue[0].DMTransferedIn; }
        }

        /// <summary>Gets the amount of N added to this organ via growth (kg/ha).</summary>
        internal double NGrowth
        {
            get { return Tissue[0].NTransferedIn; }
        }

        /// <summary>Gets the DM amount senescing from this organ (kg/ha).</summary>
        internal double DMSenesced
        {
            get { return Tissue[Tissue.Length - 2].DMTransferedOut; }
        }

        /// <summary>Gets the amount of N senescing from this organ (kg/ha).</summary>
        internal double NSenesced
        {
            get { return Tissue[Tissue.Length - 2].NTransferedOut; }
        }

        /// <summary>Gets the DM amount detached from this organ (kg/ha).</summary>
        internal double DMDetached
        {
            get { return Tissue[Tissue.Length - 1].DMTransferedOut; }
        }

        /// <summary>Gets the amount of N detached from this organ (kg/ha).</summary>
        internal double NDetached
        {
            get { return Tissue[Tissue.Length - 1].NTransferedOut; }
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
            SoilCrop soilCropData = (SoilCrop)mySoil.Crop(mySpeciesName);
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
            SoilCrop soilCropData = (SoilCrop)mySoil.Crop(mySpeciesName);
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
            SoilCrop soilCropData = (SoilCrop)mySoil.Crop(mySpeciesName);
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
                double totalRootLength = Tissue[0].DM * mySpecificRootLength; // m root/m2 
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

        /// <summary>Reset all amounts to zero in all tissues of this organ.</summary>
        internal void DoResetOrgan()
        {
            for (int t = 0; t < Tissue.Length; t++)
            {
                Tissue[t].DM = 0.0;
                Tissue[t].Namount = 0.0;
                Tissue[t].Pamount = 0.0;
                DoCleanTransferAmounts();
            }
        }

        /// <summary>Reset the transfer amounts in all tissues of this organ.</summary>
        internal void DoCleanTransferAmounts()
        {
            for (int t = 0; t < Tissue.Length; t++)
            {
                Tissue[t].DMTransferedIn = 0.0;
                Tissue[t].DMTransferedOut = 0.0;
                Tissue[t].NTransferedIn = 0.0;
                Tissue[t].NTransferedOut = 0.0;
                Tissue[t].NRemobilisable = 0.0;
                Tissue[t].NRemobilised = 0.0;
                Array.Clear(Tissue[t].DMLayersTransferedIn, 0, Tissue[t].DMLayersTransferedIn.Length);
                Array.Clear(Tissue[t].NLayersTransferedIn, 0, Tissue[t].NLayersTransferedIn.Length);
            }
        }

        /// <summary>Kills part of the organ (transfer DM and N to dead tissue).</summary>
        /// <param name="fraction">The fraction to kill in each tissue</param>
        internal void DoKillOrgan(double fraction = 1.0)
        {
            if (1.0 - fraction > Epsilon)
            {
                double fractionRemaining = 1.0 - fraction;
                for (int t = 0; t < Tissue.Length - 1; t++)
                {
                    for (int layer = 0; layer <= BottomLayer; layer++)
                    {
                        Tissue[Tissue.Length - 1].DMLayer[layer] += Tissue[t].DMLayer[layer] * fraction;
                        Tissue[Tissue.Length - 1].NamountLayer[layer] += Tissue[t].NamountLayer[layer] * fraction;
                        Tissue[t].DMLayer[layer] *= fractionRemaining;
                        Tissue[t].NamountLayer[layer] *= fractionRemaining;
                    }
                }
            }
            else
            {
                for (int t = 0; t < Tissue.Length - 1; t++)
                {
                    for (int layer = 0; layer <= BottomLayer; layer++)
                    {
                        Tissue[Tissue.Length - 1].DMLayer[layer] += Tissue[t].DMLayer[layer];
                        Tissue[Tissue.Length - 1].NamountLayer[layer] += Tissue[t].NamountLayer[layer];
                    }
                    Tissue[t].DM = 0.0;
                    Tissue[t].Namount = 0.0;
                }
            }
        }

        /// <summary>Removes biomass from root layers when harvest, graze or cut events are called.</summary>
        /// <param name="biomassRemoveType">Name of event that triggered this biomass remove call.</param>
        /// <param name="biomassToRemove">The fractions of biomass to remove</param>
        public void RemoveBiomass(string biomassRemoveType, OrganBiomassRemovalType biomassToRemove)
        {
            // Live removal
            for (int t = 0; t < Tissue.Length - 1; t++)
                Tissue[t].RemoveBiomass(biomassToRemove.FractionLiveToRemove, biomassToRemove.FractionLiveToResidue);

            // Dead removal
            Tissue[Tissue.Length - 1].RemoveBiomass(biomassToRemove.FractionDeadToRemove, biomassToRemove.FractionDeadToResidue);

            if (biomassRemoveType != "Harvest")
                IsKLModiferDueToDamageActive = true;
        }

        /// <summary>Computes the DM and N amounts turned over for all tissues.</summary>
        /// <param name="turnoverRate">The turnover rate for each tissue</param>
        /// <returns>The DM and N amount detached from this organ</returns>
        internal void DoTissueTurnover(double[] turnoverRate)
        {
            double turnoverDM;
            double turnoverN;

            // get amounts turned over
            for (int t = 0; t < Tissue.Length; t++)
            {
                if (turnoverRate[t] > 0.0)
                {
                    turnoverDM = Tissue[t].DM * turnoverRate[t];
                    turnoverN = Tissue[t].Namount * turnoverRate[t];
                    Tissue[t].DMTransferedOut += turnoverDM;
                    Tissue[t].NTransferedOut += turnoverN;

                    if (t < Tissue.Length - 1)
                    {
                        // pass amounts turned over from this tissue to the next
                        Tissue[t + 1].DMTransferedIn += turnoverDM;
                        Tissue[t + 1].NTransferedIn += turnoverN;

                        // incoming stuff need to be given for each layer
                        for (int layer = 0; layer <= BottomLayer; layer++)
                        {
                            Tissue[t + 1].DMLayersTransferedIn[layer] = turnoverDM * Tissue[t].FractionWt[layer];
                            Tissue[t + 1].NLayersTransferedIn[layer] = turnoverN * Tissue[t].FractionWt[layer];
                        }

                        // get the amounts remobilisable (luxury N)
                        double totalLuxuryN = (Tissue[t].DM + Tissue[t].DMTransferedIn - Tissue[t].DMTransferedOut) * (NconcLive - NConcOptimum);
                        Tissue[t].NRemobilisable = Math.Max(0.0, totalLuxuryN * Tissue[t + 1].FractionNLuxuryRemobilisable);
                    }
                    else
                    {
                        // N transferred into dead tissue in excess of minimum N concentration is remobilisable
                        double remobilisableN = Tissue[t].DMTransferedIn * (NconcLive - NConcMinimum);
                        Tissue[t].NRemobilisable = Math.Max(0.0, remobilisableN);
                    }
                }
            }
        }

        /// <summary>Updates each tissue, make changes in DM and N effective.</summary>
        internal bool DoOrganUpdate()
        {
            // save current state
            double previousDM = DMTotal;
            double previousN = NTotal;

            // update all tissues
            for (int t = 0; t < Tissue.Length; t++)
                Tissue[t].DoUpdateTissue();

            // check mass balance
            bool dmIsOk = Math.Abs(previousDM + DMGrowth - DMDetached - DMTotal) <= Epsilon;
            bool nIsOk = Math.Abs(previousN + NGrowth - NSenescedRemobilised - NDetached - NTotal) <= Epsilon;
            return (dmIsOk || nIsOk);
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
            SoilCrop soilCropData = (SoilCrop)mySoil.Crop(mySpeciesName);
            double depthTop = 0.0;
            double depthBottom = 0.0;
            double depthFirstStage = Math.Min(myRootDepthMaximum, myRootDistributionDepthParam);

            for (int layer = 0; layer < nLayers; layer++)
            {
                depthBottom += mySoil.Thickness[layer];
                if (depthTop >= myRootDepthMaximum)
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
                    double maxRootDepth = myRootDepthMaximum * myRootBottomDistributionFactor;
                    result[layer] = Math.Pow(maxRootDepth - Math.Max(depthTop, depthFirstStage), myRootDistributionExponent + 1)
                                  - Math.Pow(maxRootDepth - Math.Min(depthBottom, myRootDepthMaximum), myRootDistributionExponent + 1);
                    result[layer] /= (myRootDistributionExponent + 1) * Math.Pow(maxRootDepth - depthFirstStage, myRootDistributionExponent);
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
            double layerFrac = Math.Min(1.0, (myRootDepthMaximum - topLayersDepth) / (Depth - topLayersDepth));
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

        #endregion ---------------------------------------------------------------------------------------------------------


        /// <summary>Minimum significant difference between two values.</summary>
        const double Epsilon = 0.000000001;
    }
   
}
