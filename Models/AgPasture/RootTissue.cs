namespace Models.AgPasture
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Interfaces;
    using Models.Soils;
    using Models.Surface;
    using System;
    using System.Linq;

    /// <summary>Describes a root tissue of a pasture species.</summary>
    [Serializable]
    internal class RootTissue: Model
    {
        /// <summary>Average carbon content in plant dry matter (kg/kg).</summary>
        private const double carbonFractionInDM = 0.4;

        /// <summary>The fraction of luxury N remobilisable per day (0-1).</summary>
        private const double fractionNLuxuryRemobilisable = 0.1;

        /// <summary>Tissue biomass.</summary>
        private AGPBiomass biomass = new AGPBiomass();

        /// <summary>Dry matter amount for each layer (kg/ha).</summary>
        private double[] dmLayer;

        /// <summary>Nitrogen content for each layer (kg/ha).</summary>
        private double[] nLayer;

        /// <summary>Phosphorus content for each layer (kg/ha).</summary>
        private double[] pLayer;

        /// <summary>Amount of dry matter transferred into this tissue, for each layer (kg/ha).</summary>
        private double[] dmLayersTransferedIn;

        /// <summary>Amount of nitrogen transferred into this tissue, for each layer (kg/ha).</summary>
        private double[] nLayersTransferedIn;
        
        /// <summary>Dry matter amount transferred into this tissue (kg/ha).</summary>
        private double dmTransferedIn;

        /// <summary>Dry matter amount transferred out of this tissue (kg/ha).</summary>
        private double dmTransferedOut;

        /// <summary>Nitrogen transferred into this tissue (kg/ha).</summary>
        private double nTransferedIn;

        /// <summary>Nitrogen transferred out of this tissue (kg/ha).</summary>
        private double nTransferedOut;

        /// <summary>Nitrogen remobilised into new growth (kg/ha).</summary>
        private double nRemobilised;

        /// <summary>Nutrient model.</summary>
        [Link]
        private PastureSpecies species = null;

        /// <summary>Nutrient model.</summary>
        [Link]
        private Soil soil = null;

        /// <summary>Nutrient model.</summary>
        [Link]
        private INutrient nutrient = null;

        /// <summary>The surface organic matter model.</summary>
        [Link]
        private SurfaceOrganicMatter surfaceOrganicMatter = null;

        /// <summary>Initialise this root instance.</summary>
        /// <param name="initialDMByLayer">Initial dry matter by layer.</param>
        /// <param name="initialNByLayer">Initial nitrogen by layer.</param>
        public void Initialise(double[] initialDMByLayer, double[] initialNByLayer)
        {
            pLayer = new double[soil.Thickness.Length];
            dmLayersTransferedIn = new double[soil.Thickness.Length];
            nLayersTransferedIn = new double[soil.Thickness.Length];
            if (initialNByLayer != null && initialNByLayer != null)
            {
                dmLayer = initialDMByLayer;
                nLayer = initialNByLayer;
            }
            else
            {
                dmLayer = new double[soil.Thickness.Length];
                nLayer = new double[soil.Thickness.Length];
            }
            UpdateDM();
        }

        /// <summary>The amount of N available for remobilisation (kg/ha).</summary>
        public double NRemobilisable { get; private set; }

        /// <summary>Amount of biomass.</summary>
        public IAGPBiomass DM {  get { return biomass; } }

        /// <summary>The dry matter fraction for each layer (0-1).</summary>
        public double[] FractionWt { get { return MathUtilities.Divide_Value(dmLayer, DM.Wt); } }
        
        /// <summary>Set the biomass moving into the tissue.</summary>
        /// <param name="dm">The dry matter (kg/ha).</param>
        /// <param name="n">The nitrogen (kg/ha).</param>
        public void SetBiomassTransferIn(double[] dm, double[] n)
        {
            dmLayersTransferedIn = dm;
            nLayersTransferedIn = n;
        }

        /// <summary>Updates the tissue state, make changes in DM and N effective.</summary>
        public void Update()
        {
            // removals first as they do not change distribution over the profile
            var amountDMToRemove = DM.Wt - dmTransferedOut;
            var amountNToRemove = DM.N - nTransferedOut;
            double[] prevRootFraction = FractionWt;
            for (int layer = 0; layer < dmLayer.Length; layer++)
                dmLayer[layer] = amountDMToRemove * prevRootFraction[layer];

            UpdateDM();
            for (int layer = 0; layer < dmLayer.Length; layer++)
                nLayer[layer] = amountNToRemove * FractionWt[layer];

            // additions need to consider distribution over the profile
            dmTransferedIn = dmLayersTransferedIn.Sum();
            nTransferedIn = nLayersTransferedIn.Sum();
            if (dmTransferedIn > 0 || nTransferedIn > 0)
            {
                for (int layer = 0; layer < dmLayer.Length; layer++)
                {
                    dmLayer[layer] += dmLayersTransferedIn[layer];
                    nLayer[layer] += nLayersTransferedIn[layer] - (nRemobilised * (nLayersTransferedIn[layer] / nTransferedIn));
                }
            }
            UpdateDM();
        }

        /// <summary>Adds a given amount of detached root material (DM and N) to the soil's FOM pool.</summary>
        /// <param name="amountDM">The DM amount to send (kg/ha)</param>
        /// <param name="amountN">The N amount to send (kg/ha)</param>
        public void DetachBiomass(double amountDM, double amountN)
        {
            if (amountDM + amountN > 0.0)
            {
                FOMLayerLayerType[] FOMdataLayer = new FOMLayerLayerType[dmLayer.Length];
                for (int layer = 0; layer < dmLayer.Length; layer++)
                {
                    FOMType fomData = new FOMType();
                    fomData.amount = amountDM * FractionWt[layer];
                    fomData.N = amountN * FractionWt[layer];
                    fomData.C = amountDM * carbonFractionInDM * FractionWt[layer];
                    fomData.P = 0.0; // P not considered here
                    fomData.AshAlk = 0.0; // Ash not considered here

                    FOMLayerLayerType layerData = new FOMLayerLayerType();
                    layerData.FOM = fomData;
                    layerData.CNR = 0.0; // not used here
                    layerData.LabileP = 0.0; // not used here

                    FOMdataLayer[layer] = layerData;
                }

                FOMLayerType FOMData = new FOMLayerType();
                FOMData.Type = species.Name;
                FOMData.Layer = FOMdataLayer;
                nutrient.DoIncorpFOM(FOMData);
            }
        }

        /// <summary>
        /// Remove a fraction of the biomass.
        /// </summary>
        /// <param name="fractionToRemove">The fraction from each layer to remove.</param>
        /// <param name="sendToSurfaceOrganicMatter">Send to surface organic matter?</param>
        /// <returns></returns>
        public BiomassAndNLayered RemoveBiomass(double fractionToRemove, bool sendToSurfaceOrganicMatter)
        {
            var removed = new BiomassAndNLayered();
            removed.Wt = MathUtilities.Multiply_Value(dmLayer, fractionToRemove);
            removed.N = MathUtilities.Multiply_Value(nLayer, fractionToRemove);
            for (int layer = 0; layer < dmLayer.Length; layer++)
            {
                dmLayer[layer] -= removed.Wt[layer];
                nLayer[layer] -= removed.N[layer];
            }
            UpdateDM();

            if (sendToSurfaceOrganicMatter)
                surfaceOrganicMatter.Add(removed.Wt.Sum(), removed.N.Sum(), 0.0, species.Name, species.Name);

            return removed;
        }

        /// <summary>
        /// Move a fraction of the biomass from this tissue to another tissue.
        /// </summary>
        /// <param name="fractionToRemove">The fraction to move.</param>
        /// <param name="toTissue">The tissue to move to biomass to.</param>
        public void MoveFractionToTissue(double fractionToRemove, RootTissue toTissue)
        {
            var removed = RemoveBiomass(fractionToRemove, sendToSurfaceOrganicMatter: false);
            toTissue.AddBiomass(removed.Wt, removed.N);
            if (fractionToRemove == 1)
                Reset();
        }

        /// <summary>Computes the DM and N amounts turned over for all tissues.</summary>
        /// <param name="turnoverRate">The turnover rate for each tissue</param>
        /// <param name="bottomLayer">Bottom layer index where roots are located.</param>
        /// <param name="to">The tissue to move the turned over material to.</param>
        /// <param name="nConc">The n concentration.</param>
        /// <returns>The DM and N amount removed from this tissue.</returns>
        public BiomassAndN DoTissueTurnover(double turnoverRate, int bottomLayer, RootTissue to, double nConc)
        {
            if (turnoverRate > 0.0)
            {
                var turnoverDM = DM.Wt * turnoverRate;
                var turnoverN = DM.N * turnoverRate;
                dmTransferedOut += turnoverDM;
                nTransferedOut += turnoverN;

                if (to != null)
                {
                    to.SetBiomassTurnover(turnoverDM, turnoverN, bottomLayer, FractionWt);

                    // get the amounts remobilisable (luxury N)
                    double totalLuxuryN = (DM.Wt + dmTransferedIn - dmTransferedOut) * nConc;
                    NRemobilisable = Math.Max(0.0, totalLuxuryN * RootTissue.fractionNLuxuryRemobilisable);
                }
                else
                {
                    // N transferred into dead tissue in excess of minimum N concentration is remobilisable
                    double remobilisableN = dmTransferedIn * nConc;
                    NRemobilisable = Math.Max(0.0, remobilisableN);
                }
            }
            return new BiomassAndN()
            {
                Wt = dmTransferedOut,
                N = nTransferedOut
            };
        }

        /// <summary>
        /// Set the tissue turnover rates.
        /// </summary>
        /// <param name="turnoverDM">The dry matter turnover (kg/ha).</param>
        /// <param name="turnoverN"></param>
        /// <param name="bottomLayer">Bottom layer index where roots are located.</param>
        /// <param name="fractionWt">The dry matter fraction for each layer (0-1)</param>
        public void SetBiomassTurnover(double turnoverDM, double turnoverN, int bottomLayer, double[] fractionWt)
        {
            for (int layer = 0; layer <= bottomLayer; layer++)
            {
                dmLayersTransferedIn[layer] = turnoverDM * fractionWt[layer];
                nLayersTransferedIn[layer] = turnoverN * fractionWt[layer];
            }
            dmTransferedIn += turnoverDM;
            nTransferedIn += turnoverN;
            UpdateDM();
        }

        /// <summary>
        /// Set the new growth allocation for the day.
        /// </summary>
        /// <param name="dm">The dry matter (kg/ha).</param>
        /// <param name="n">The nitrogen (kg/ha).</param>
        public BiomassAndN SetNewGrowthAllocation(double dm, double n)
        {
            dmTransferedIn += dm;
            nTransferedIn += n;
            return new BiomassAndN()
            {
                Wt = dmTransferedIn,
                N = nTransferedIn
            };
        }

        /// <summary>
        /// Add biomass.
        /// </summary>
        /// <param name="dmToAdd">The amount of dry matter to add (kg/ha).</param>
        /// <param name="nToAdd">The amount of nitrogen to add (kg/ha).</param>
        public void AddBiomass(double[] dmToAdd, double[] nToAdd)
        {
            for (int layer = 0; layer < dmLayer.Length; layer++)
            {
                dmLayer[layer] += dmToAdd[layer];
                nLayer[layer] += nToAdd[layer];
            }
            UpdateDM();
        }

        /// <summary>Removes a fraction of remobilisable N for use into new growth.</summary>
        /// <param name="fraction">The fraction to remove (0-1)</param>
        public void DoRemobiliseN(double fraction)
        {
            nRemobilised = NRemobilisable * fraction;
        }

        public void UpdateDM()
        {
            biomass.Wt = dmLayer.Sum();
            biomass.N = nLayer.Sum();
        }

        /// <summary>
        /// Reset tissue to the specified amount.
        /// </summary>
        /// <param name="dmAmount">The amount of dry matter by layer to reset to (kg/ha).</param>
        public void ResetTo(double[] dmAmount)
        {
            for (int layer = 0; layer < dmLayer.Length; layer++)
                dmLayer[layer] += dmAmount[layer];

            UpdateDM();
        }

        public void Reset()
        {
            for (int layer = 0; layer < dmLayer.Length; layer++)
            {
                dmLayer[layer] = 0;
                nLayer[layer] = 0;
            }
        }

        /// <summary>Called each day to reset the transfer variables.</summary>
        public void DailyReset()
        {
            dmTransferedIn = 0.0;
            dmTransferedOut = 0.0;
            nTransferedIn = 0.0;
            nTransferedOut = 0.0;
            NRemobilisable = 0.0;
            nRemobilised = 0.0;
            Array.Clear(dmLayersTransferedIn, 0, dmLayersTransferedIn.Length);
            Array.Clear(nLayersTransferedIn, 0, nLayersTransferedIn.Length);
        }

        /// <summary>Updates each tissue, make changes in DM and N effective.</summary>
        public static void UpdateTissues(RootTissue tissue1, RootTissue tissue2)
        {
            // save current state
            double previousDM = tissue1.DM.Wt + tissue2.DM.Wt;
            double previousN = tissue1.DM.N + tissue2.DM.N;

            // update all tissues
            tissue1.Update();
            tissue2.Update();

            var currentDM = tissue1.DM.Wt + tissue2.DM.Wt;
            var currentN = tissue1.DM.N + tissue2.DM.N;
            var nRemobilised = tissue1.nRemobilised + tissue2.nRemobilised;

            // check mass balance
            if (!MathUtilities.FloatsAreEqual(0.0, previousDM + tissue1.dmTransferedIn - tissue2.dmTransferedOut - currentDM))
                throw new Exception("Growth and tissue turnover resulted in loss of dry matter mass balance for roots");
            if (!MathUtilities.FloatsAreEqual(0.0, previousN + tissue1.nTransferedIn - tissue1.nRemobilised - tissue2.nRemobilised - tissue2.nTransferedOut - currentN))
                throw new Exception("Growth and tissue turnover resulted in loss of nitrogen mass balance for roots");
        }
    }
}