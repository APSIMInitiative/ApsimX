namespace Models.AgPasture
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Interfaces;
    using Models.Soils;
    using Models.Soils.Nutrients;
    using Models.Surface;
    using System;
    using System.Linq;

    /// <summary>Describes a root tissue of a pasture species.</summary>
    [Serializable]
    public class RootTissue: Model
    {
        /// <summary>Pasture species this tissue belongs to.</summary>
        [Link(Type = LinkType.Ancestor)]
        private PastureSpecies species = null;

        /// <summary>Soil physical parameterisation.</summary>
        [Link]
        private IPhysical soilPhysical = null;

        /// <summary>Soil nutrient model.</summary>
        [Link]
        private INutrient nutrient = null;

        /// <summary>Average carbon content in plant dry matter (kg/kg).</summary>
        private const double carbonFractionInDM = 0.4;

        /// <summary>Fraction of luxury N remobilisable per day (0-1).</summary>
        private const double fractionNLuxuryRemobilisable = 0.1;

        /// <summary>Tissue biomass.</summary>
        private AGPBiomass biomass = new AGPBiomass();

        /// <summary>Dry matter amount for each layer (kg/ha).</summary>
        private double[] dmByLayer;

        /// <summary>Nitrogen content for each layer (kg/ha).</summary>
        private double[] nByLayer;

        /// <summary>Phosphorus content for each layer (kg/ha).</summary>
        private double[] pByLayer;

        /// <summary>Amount of dry matter transferred into this tissue, for each layer (kg/ha).</summary>
        private double[] dmTransferredInByLayer;

        /// <summary>Amount of nitrogen transferred into this tissue, for each layer (kg/ha).</summary>
        private double[] nTransferredInByLayer;

        /// <summary>Dry matter amount transferred into this tissue (kg/ha).</summary>
        private double dmTransferredIn;

        /// <summary>Dry matter amount transferred out of this tissue (kg/ha).</summary>
        private double dmTransferredOut;

        /// <summary>Nitrogen transferred into this tissue (kg/ha).</summary>
        private double nTransferredIn;

        /// <summary>Nitrogen transferred out of this tissue (kg/ha).</summary>
        private double nTransferredOut;

        /// <summary>Nitrogen remobilised into new growth (kg/ha).</summary>
        private double nRemobilised;

        /// <summary>Amount of N available for remobilisation (kg/ha).</summary>
        public double NRemobilisable { get; private set; }

        /// <summary>Dry matter biomass.</summary>
        public IAGPBiomass DM { get { return biomass; } }

        /// <summary>Dry matter fraction for each layer (0-1).</summary>
        public double[] FractionWt { get { return MathUtilities.Divide_Value(dmByLayer, DM.Wt); } }

        /// <summary>Initialise this root instance.</summary>
        /// <param name="initialDMByLayer">Initial dry matter by layer.</param>
        /// <param name="initialNByLayer">Initial nitrogen by layer.</param>
        public void Initialise(double[] initialDMByLayer, double[] initialNByLayer)
        {
            pByLayer = new double[soilPhysical.Thickness.Length];
            dmTransferredInByLayer = new double[soilPhysical.Thickness.Length];
            nTransferredInByLayer = new double[soilPhysical.Thickness.Length];
            if (initialNByLayer != null && initialNByLayer != null)
            {
                dmByLayer = initialDMByLayer;
                nByLayer = initialNByLayer;
            }
            else
            {
                dmByLayer = new double[soilPhysical.Thickness.Length];
                nByLayer = new double[soilPhysical.Thickness.Length];
            }
            UpdateDM();
        }

        /// <summary>Set the biomass moving into the tissue.</summary>
        /// <param name="dm">Dry matter (kg/ha).</param>
        /// <param name="n">The nitrogen (kg/ha).</param>
        public void SetBiomassTransferIn(double[] dm, double[] n)
        {
            dmTransferredInByLayer = dm;
            nTransferredInByLayer = n;
        }

        /// <summary>Updates the tissue state, make changes in DM and N effective.</summary>
        public void Update()
        {
            // removals first as they do not change distribution over the profile
            var amountDMToRemove = DM.Wt - dmTransferredOut;
            var amountNToRemove = DM.N - nTransferredOut;
            double[] prevRootFraction = FractionWt;
            for (int layer = 0; layer < dmByLayer.Length; layer++)
                dmByLayer[layer] = amountDMToRemove * prevRootFraction[layer];

            UpdateDM();
            double[] newRootFraction = FractionWt;
            for (int layer = 0; layer < dmByLayer.Length; layer++)
                nByLayer[layer] = amountNToRemove * newRootFraction[layer];

            // additions need to consider distribution over the profile
            dmTransferredIn = dmTransferredInByLayer.Sum();
            nTransferredIn = nTransferredInByLayer.Sum();
            if (dmTransferredIn > 0 || nTransferredIn > 0)
            {
                for (int layer = 0; layer < dmByLayer.Length; layer++)
                {
                    dmByLayer[layer] += dmTransferredInByLayer[layer];
                    nByLayer[layer] += nTransferredInByLayer[layer] - (nRemobilised * (nTransferredInByLayer[layer] / nTransferredIn));
                }
            }

            UpdateDM();
        }

        /// <summary>Adds a given amount of detached root material (DM and N) to the soil's FOM pool.</summary>
        /// <param name="amountDM">The DM amount to detach (kg/ha).</param>
        /// <param name="amountN">The N amount to detach (kg/ha).</param>
        public void DetachBiomass(double amountDM, double amountN)
        {
            if (amountDM + amountN > 0.0)
            {
                var amountDMLayered = new double[dmByLayer.Length];
                var amountNLayered = new double[dmByLayer.Length];

                var fractionWt = FractionWt;
                for (int layer = 0; layer < dmByLayer.Length; layer++)
                {
                    amountDMLayered[layer] = amountDM * fractionWt[layer];
                    amountNLayered[layer] = amountN * fractionWt[layer];
                }

                DetachBiomass(amountDMLayered, amountNLayered);
            }
        }

        /// <summary>Adds a given amount of detached root material (DM and N) to the soil's FOM pool, per layer.</summary>
        /// <param name="amountDM">The DM amount to detach (kg/ha).</param>
        /// <param name="amountN">The N amount to detach (kg/ha).</param>
        public void DetachBiomass(double[] amountDM, double[] amountN)
        {
            if (amountDM.Sum() + amountN.Sum() > 0.0)
            {
                FOMLayerLayerType[] FOMdataLayer = new FOMLayerLayerType[dmByLayer.Length];
                for (int layer = 0; layer < dmByLayer.Length; layer++)
                {
                    FOMType fomData = new FOMType();
                    fomData.amount = amountDM[layer];
                    fomData.N = amountN[layer];
                    fomData.C = fomData.amount * carbonFractionInDM;
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

        /// <summary>Move a fraction of the biomass from this tissue to another tissue.</summary>
        /// <param name="fractionToRemove">The fraction to move.</param>
        /// <param name="toTissue">The tissue to move to biomass to.</param>
        public void MoveFractionToTissue(double fractionToRemove, RootTissue toTissue)
        {
            var removed = RemoveBiomass(fractionToRemove, sendToSoil: false);
            toTissue.AddBiomass(removed.Wt, removed.N);
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
                dmTransferredOut += turnoverDM;
                nTransferredOut += turnoverN;

                if (to != null)
                {
                    to.SetBiomassTurnover(turnoverDM, turnoverN, bottomLayer, FractionWt);

                    // get the amounts remobilisable (luxury N)
                    double totalLuxuryN = (DM.Wt + dmTransferredIn - dmTransferredOut) * nConc;
                    NRemobilisable = Math.Max(0.0, totalLuxuryN * RootTissue.fractionNLuxuryRemobilisable);
                }
                else
                {
                    // N transferred into dead tissue in excess of minimum N concentration is remobilisable
                    double remobilisableN = dmTransferredIn * nConc;
                    NRemobilisable = Math.Max(0.0, remobilisableN);
                }
            }
            return new BiomassAndN()
            {
                Wt = dmTransferredOut,
                N = nTransferredOut
            };
        }

        /// <summary>Removes a fraction of remobilisable N for use into new growth.</summary>
        /// <param name="fraction">The fraction to remove (0-1)</param>
        public void DoRemobiliseN(double fraction)
        {
            nRemobilised = NRemobilisable * fraction;
        }

        /// <summary>Adds biomass from tissue turnover.</summary>
        /// <param name="turnoverDM">Dry matter amount turned over (kg/ha).</param>
        /// <param name="turnoverN">Nitrogen amount turned over (kg/ha).</param>
        /// <param name="bottomLayer">Bottom layer index where roots are located.</param>
        /// <param name="fractionWt">The dry matter fraction for each layer (0-1)</param>
        public void SetBiomassTurnover(double turnoverDM, double turnoverN, int bottomLayer, double[] fractionWt)
        {
            for (int layer = 0; layer <= bottomLayer; layer++)
            {
                dmTransferredInByLayer[layer] = turnoverDM * fractionWt[layer];
                nTransferredInByLayer[layer] = turnoverN * fractionWt[layer];
            }

            dmTransferredIn += turnoverDM;
            nTransferredIn += turnoverN;
            UpdateDM();
        }

        /// <summary>Adds biomass from new growth.</summary>
        /// <param name="dm">Dry matter amount (kg/ha).</param>
        /// <param name="n">Nitrogen amount (kg/ha).</param>
        public BiomassAndN SetNewGrowthAllocation(double dm, double n)
        {
            dmTransferredIn += dm;
            nTransferredIn += n;
            return new BiomassAndN()
            {
                Wt = dmTransferredIn,
                N = nTransferredIn
            };
        }

        /// <summary>Reset tissue to the specified amount.</summary>
        /// <param name="dmAmount">The amount of dry matter by layer to reset to (kg/ha).</param>
        public void ResetTo(double[] dmAmount)
        {
            for (int layer = 0; layer < dmByLayer.Length; layer++)
                dmByLayer[layer] += dmAmount[layer];

            UpdateDM();
        }

        /// <summary>Sets the biomass of this tissue.</summary>
        /// <param name="dmAmount">The DM amount, by layer, to set to (kg/ha).</param>
        /// <param name="nAmount">The amount of N, by layer, to set to (kg/ha).</param>
        public void SetBiomass(double[] dmAmount, double[] nAmount)
        {
            for (int layer = 0; layer < dmByLayer.Length; layer++)
            {
                dmByLayer[layer] = dmAmount[layer];
                nByLayer[layer] = nAmount[layer];
            }

            UpdateDM();
        }

        /// <summary>Adds an amount of biomass to this tissue.</summary>
        /// <param name="dmToAdd">Dry matter amount to add (kg/ha).</param>
        /// <param name="nToAdd">Nitrogen amount to add (kg/ha).</param>
        public void AddBiomass(double[] dmToAdd, double[] nToAdd)
        {
            for (int layer = 0; layer < dmByLayer.Length; layer++)
            {
                dmByLayer[layer] += dmToAdd[layer];
                nByLayer[layer] += nToAdd[layer];
            }

            UpdateDM();
        }

        /// <summary>Removes a fraction of the biomass from this tissue.</summary>
        /// <param name="fractionToRemove">The fraction of biomass to remove.</param>
        /// <param name="sendToSoil">Whether the biomass should be sent to soil.</param>
        /// <remarks>The same fraction is used for all layers.</remarks>
        /// <returns></returns>
        public BiomassAndNLayered RemoveBiomass(double fractionToRemove, bool sendToSoil)
        {
            var removed = new BiomassAndNLayered();
            removed.Wt = MathUtilities.Multiply_Value(dmByLayer, fractionToRemove);
            removed.N = MathUtilities.Multiply_Value(nByLayer, fractionToRemove);
            for (int layer = 0; layer < dmByLayer.Length; layer++)
            {
                dmByLayer[layer] -= removed.Wt[layer];
                nByLayer[layer] -= removed.N[layer];
            }

            UpdateDM();

            if (sendToSoil)
            {
                DetachBiomass(removed.Wt, removed.N);
            }

            return removed;
        }

        /// <summary>Update dry matter.</summary>
        private void UpdateDM()
        {
            biomass.Wt = dmByLayer.Sum();
            biomass.N = nByLayer.Sum();
        }

        /// <summary>Reset the transfer amounts in this tissue.</summary>
        public void ClearDailyTransferredAmounts()
        {
            dmTransferredIn = 0.0;
            dmTransferredOut = 0.0;
            nTransferredIn = 0.0;
            nTransferredOut = 0.0;
            NRemobilisable = 0.0;
            nRemobilised = 0.0;
            Array.Clear(dmTransferredInByLayer, 0, dmTransferredInByLayer.Length);
            Array.Clear(nTransferredInByLayer, 0, nTransferredInByLayer.Length);
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

            // check mass balance
            if (!MathUtilities.FloatsAreEqual(0.0, previousDM + tissue1.dmTransferredIn - tissue2.dmTransferredOut - currentDM))
                throw new Exception("Growth and tissue turnover resulted in loss of dry matter mass balance for roots");
            if (!MathUtilities.FloatsAreEqual(0.0, previousN + tissue1.nTransferredIn - tissue1.nRemobilised - tissue2.nRemobilised - tissue2.nTransferredOut - currentN))
                throw new Exception("Growth and tissue turnover resulted in loss of nitrogen mass balance for roots");
        }
    }
}
