using System;
using System.Linq;
using APSIM.Shared.Utilities;
using APSIM.Numerics;
using Models.Core;
using Models.Soils;
using Models.Soils.Nutrients;
using Models.Surface;

namespace Models.AgPasture
{

    /// <summary>Describes a root tissue of a pasture species.</summary>
    [Serializable]
    public class RootTissue : Model
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

        //----------------------- Constants -----------------------

        /// <summary>Average carbon content in plant dry matter (kg/kg).</summary>
        private const double CarbonConcentration = 0.4;

        /// <summary>Minimum significant difference between two values.</summary>
        internal const double Epsilon = 0.000000001;

        //---------------------------- Parameters -----------------------

        /// <summary>Fraction of excess N, above optimum N for live tissues and minimum for dead tissue, that is remobilisable per day (0-1).</summary>
        public double FractionNRemobilisable { get; set; }

        //----------------------- Daily Deltas -----------------------

        /// <summary>Amount of dry matter transferred into this tissue, for each layer (kg/ha).</summary>
        private double[] dmTransferredInByLayer;

        /// <summary>Amount of nitrogen transferred into this tissue, for each layer (kg/ha).</summary>
        private double[] nTransferredInByLayer;

        /// <summary>Amount of dry matter transferred out of this tissue, for each layer (kg/ha).</summary>
        private double[] dmTransferredOutByLayer;

        /// <summary>Amount of nitrogen transferred out of this tissue, for each layer (kg/ha).</summary>
        private double[] nTransferredOutByLayer;

        /// <summary>Amount of dry matter removed from this tissue, for each layer (kg/ha).</summary>
        private double[] dmRemovedByLayer;

        /// <summary>Amount of nitrogen removed from this tissue, for each layer (kg/ha).</summary>
        private double[] nRemovedByLayer;

        /// <summary>Amount of nitrogen in this tissue that is potentially remobilisable, for each layer (kg/ha).</summary>
        private double[] nRemobilisableByLayer;

        /// <summary>Amount of nitrogen remobilised from this tissue, for each layer (kg/ha).</summary>
        private double[] nRemobilisedByLayer;

        /// <summary>Dry matter amount transferred into this tissue (kg/ha).</summary>
        public double DMTransferredIn { get { return dmTransferredInByLayer.Sum(); } }

        /// <summary>Nitrogen transferred into this tissue (kg/ha).</summary>
        public double NTransferredIn { get { return nTransferredInByLayer.Sum(); } }

        /// <summary>Dry matter amount transferred out of this tissue (kg/ha).</summary>
        public double DMTransferredOut { get { return dmTransferredOutByLayer.Sum(); } }

        /// <summary>Nitrogen transferred out of this tissue (kg/ha).</summary>
        public double NTransferredOut { get { return nTransferredOutByLayer.Sum(); } }

        /// <summary>DM removed from this tissue (kg/ha).</summary>
        public double DMRemoved { get { return dmRemovedByLayer.Sum(); } }

        /// <summary>N removed from this tissue (kg/ha).</summary>
        public double NRemoved { get { return nRemovedByLayer.Sum(); } }

        /// <summary>Fraction of DM removed from this tissue.</summary>
        public double FractionRemoved { get { return MathUtilities.Divide(DMRemoved, DM.Wt, 0.0); } }

        /// <summary>Amount of N available for remobilisation (kg/ha).</summary>
        public double NRemobilisable { get { return nRemobilisableByLayer.Sum(); } }

        /// <summary>Nitrogen remobilised into new growth (kg/ha).</summary>
        public double NRemobilised { get { return nRemobilisedByLayer.Sum(); } }

        /// <summary>Fraction of N from this tissue that was remobilised to new growth.</summary>
        public double FractionRemobilised { get { return MathUtilities.Divide(NRemobilised, DM.N, 0.0); } }

        //----------------------- States -----------------------

        /// <summary>Tissue dry matter biomass.</summary>
        private AGPBiomass biomass = new AGPBiomass();

        /// <summary>Dry matter amount of tissue biomass within each layer (kg/ha).</summary>
        private double[] dmByLayer;

        /// <summary>Nitrogen content of tissue biomass within each layer (kg/ha).</summary>
        private double[] nByLayer;

        /// <summary>Phosphorus content of tissue biomass within each layer (kg/ha).</summary>
        private double[] pByLayer;

        /// <summary>Fraction of tissue biomass within each soil layer (0-1).</summary>
        private double[] dmFractions;

        /// <summary>Dry matter biomass.</summary>
        public IAGPBiomass DM { get { return biomass; } }

        /// <summary>Dry matter amount in this tissue within each soil layer (kg/ha).</summary>
        public double[] DMLayered { get { return dmByLayer; } }

        /// <summary>Nitrogen content in this tissue within each soil layer (kg/ha).</summary>
        public double[] NLayered { get { return nByLayer; } }

        /// <summary>Fraction of dry matter for this tissue within each soil layer (0-1).</summary>
        public double[] DMFraction { get { return dmFractions; } }

        /// <summary>Number of layers in the soil.</summary>
        private int nLayers;

        //----------------------- Public methods -----------------------

        /// <summary>Initialise this tissue instance.</summary>
        public void Initialise()
        {
            nLayers = soilPhysical.Thickness.Length;
            dmByLayer = new double[nLayers];
            nByLayer = new double[nLayers];
            pByLayer = new double[nLayers];
            dmFractions = new double[nLayers];
            dmTransferredInByLayer = new double[nLayers];
            nTransferredInByLayer = new double[nLayers];
            dmTransferredOutByLayer = new double[nLayers];
            nTransferredOutByLayer = new double[nLayers];
            dmRemovedByLayer = new double[nLayers];
            nRemovedByLayer = new double[nLayers];
            nRemobilisableByLayer = new double[nLayers];
            nRemobilisedByLayer = new double[nLayers];
        }

        /// <summary>Sets the biomass of this tissue.</summary>
        /// <param name="dmAmount">The DM amount, by layer, to set to (kg/ha).</param>
        /// <param name="nAmount">The amount of N, by layer, to set to (kg/ha).</param>
        public void SetBiomass(double[] dmAmount, double[] nAmount)
        {
            for (int layer = 0; layer < nLayers; layer++)
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
            for (int layer = 0; layer < nLayers; layer++)
            {
                dmByLayer[layer] += dmToAdd[layer];
                nByLayer[layer] += nToAdd[layer];
            }

            UpdateDM();
        }

        /// <summary>Removes a fraction of the biomass from this tissue.</summary>
        /// <param name="fractionToRemove">The fraction of biomass to remove off field.</param>
        /// <param name="fractionToSoil">The fraction of biomass to sent to soil.</param>
        /// <remarks>The same removal fractions are used for all layers.</remarks>
        public void RemoveBiomass(double fractionToRemove, double fractionToSoil)
        {
            double[] dmToSoil = new double[nLayers];
            double[] nToSoil = new double[nLayers];
            var totalFraction = fractionToRemove + fractionToSoil;
            for (int layer = 0; layer < nLayers; layer++)
            {
                var dmToRemove = dmByLayer[layer] * totalFraction;
                var nToRemove = nByLayer[layer] * totalFraction;
                dmToSoil[layer] = dmByLayer[layer] * fractionToSoil;
                nToSoil[layer] = nByLayer[layer] * fractionToSoil;
                dmByLayer[layer] -= dmToRemove;
                nByLayer[layer] -= nToRemove;
                dmRemovedByLayer[layer] += dmToRemove;
                nRemovedByLayer[layer] += nToRemove;
            }

            UpdateDM();

            if (fractionToSoil > 0.0)
            {
                DetachBiomass(dmToSoil, nToSoil);
            }
        }

        /// <summary>Removes a fraction of the biomass from this tissue.</summary>
        /// <param name="fractionToRemove">The fraction of biomass to remove, for each layer.</param>
        /// <param name="fractionToSoil">The fraction of biomass to sent to soil, for each layer.</param>
        /// <remarks>The fraction should be give for each layer, if array is short no biomass is removed at bottom of profile.</remarks>
        public void RemoveBiomass(double[] fractionToRemove, double[] fractionToSoil)
        {
            var numLayers = Math.Min(fractionToRemove.Length, fractionToSoil.Length);
            double[] dmToSoil = new double[numLayers];
            double[] nToSoil = new double[numLayers];
            for (int layer = 0; layer < numLayers; layer++)
            {
                var totalFraction = fractionToRemove[layer] + fractionToSoil[layer];
                var dmToRemove = dmByLayer[layer] * totalFraction;
                var nToRemove = nByLayer[layer] * totalFraction;
                dmToSoil[layer] = dmByLayer[layer] * fractionToSoil[layer];
                nToSoil[layer] = nByLayer[layer] * fractionToSoil[layer];
                dmByLayer[layer] -= dmToRemove;
                nByLayer[layer] -= nToRemove;
                dmRemovedByLayer[layer] += dmToRemove;
                nRemovedByLayer[layer] += nToRemove;
            }

            UpdateDM();

            if (fractionToSoil.Sum() > 0.0)
            {
                DetachBiomass(dmToSoil, nToSoil);
            }
        }

        /// <summary>Updates the tissue state, make changes in DM and N effective.</summary>
        public void Update()
        {
            if ((DMTransferredIn + DMTransferredOut > 0.0) || (NTransferredIn + NTransferredOut > 0.0))
            {
                for (int layer = 0; layer < nLayers; layer++)
                {
                    // update values
                    dmByLayer[layer] += dmTransferredInByLayer[layer] - dmTransferredOutByLayer[layer];
                    nByLayer[layer] += nTransferredInByLayer[layer] - (nTransferredOutByLayer[layer] + nRemobilisedByLayer[layer]);

                    // ensure that small values are zeroed (prevent small negatives)
                    if (MathUtilities.FloatsAreEqual(dmByLayer[layer], 0.0, Epsilon))
                    {
                        dmByLayer[layer] = 0.0;
                        nByLayer[layer] = 0.0;
                    }

                    // check that biomass doesn't go negative
                    if (dmByLayer[layer] < 0.0)
                    {
                        throw new Exception($"{species.Name} {Name} tissue has negative dry matter");
                    }
                    if (nByLayer[layer] < 0.0)
                    {
                        throw new Exception($"{species.Name} {Name} tissue has negative N content");
                    }

                    // check that N concentration are within bounds
                    if (dmByLayer[layer] > 0.0)
                    {
                        double nConcLayer = nByLayer[layer] / dmByLayer[layer];
                        if (MathUtilities.IsLessThan(nConcLayer, (Parent as PastureBelowGroundOrgan).NConcMinimum, Epsilon))
                        {
                            throw new Exception($"{species.Name} {Name} tissue has N content lower than minimum in layer {layer}");
                        }
                        if (MathUtilities.IsGreaterThan(nConcLayer, (Parent as PastureBelowGroundOrgan).NConcMaximum, Epsilon))
                        {
                            throw new Exception($"{species.Name} {Name} tissue has N content greater than maximum in layer {layer}");
                        }
                    }
                }

                UpdateDM();
            }
        }

        /// <summary>Update dry matter.</summary>
        private void UpdateDM()
        {
            biomass.Wt = dmByLayer.Sum();
            biomass.N = nByLayer.Sum();
            dmFractions = MathUtilities.Divide_Value(dmByLayer, biomass.Wt);
        }

        /// <summary>Adds a given amount of detached root material (DM and N) to the soil's FOM pool.</summary>
        /// <remarks>This assumes the same detachment rate across the profile (will not change relative distribution).</remarks>
        /// <param name="amountDM">The DM amount to detach (kg/ha).</param>
        /// <param name="amountN">The N amount to detach (kg/ha).</param>
        public void DetachBiomass(double amountDM, double amountN)
        {
            if (amountDM + amountN > 0.0)
            {
                // split the amounts into values for each layer
                var amountDMLayered = MathUtilities.Multiply_Value(dmFractions, amountDM);
                var amountNLayered = MathUtilities.Multiply_Value(dmFractions, amountN);

                // do the actual detachment
                DetachBiomass(amountDMLayered, amountNLayered);
            }
        }

        /// <summary>Adds given amounts of detached root material (DM and N) to the soil's FOM pool.</summary>
        /// <param name="amountDM">The DM amounts to detach from each layer (kg/ha).</param>
        /// <param name="amountN">The N amounts to detach from each layer (kg/ha).</param>
        public void DetachBiomass(double[] amountDM, double[] amountN)
        {
            if (amountDM.Sum() + amountN.Sum() > 0.0)
            {
                FOMLayerLayerType[] FOMdataLayer = new FOMLayerLayerType[nLayers];
                for (int layer = 0; layer < nLayers; layer++)
                {
                    FOMType fomData = new FOMType();
                    fomData.amount = amountDM[layer];
                    fomData.N = amountN[layer];
                    fomData.C = fomData.amount * CarbonConcentration;
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

        /// <summary>Computes the DM and N amounts turned over for this tissue.</summary>
        /// <param name="turnoverRate">The turnover rate for the tissue today.</param>
        /// <param name="receivingTissue">The tissue to move the turned over biomass to.</param>
        public void DoTissueTurnover(double turnoverRate, RootTissue receivingTissue)
        {
            if (biomass.Wt > 0.0 && turnoverRate > 0.0)
            {
                // get the amounts turned over
                for (int layer = 0; layer < nLayers; layer++)
                {
                    dmTransferredOutByLayer[layer] = dmByLayer[layer] * turnoverRate;
                    nTransferredOutByLayer[layer] = nByLayer[layer] * turnoverRate;
                }

                // pass the amounts from this to the receiving tissue
                if (receivingTissue != null)
                {
                    receivingTissue.SetBiomassTransferIn(dmTransferredOutByLayer, nTransferredOutByLayer);
                }
            }
        }

        /// <summary>Set the biomass moving into the tissue.</summary>
        /// <param name="dm">Dry matter to add (kg/ha).</param>
        /// <param name="n">The nitrogen to add (kg/ha).</param>
        public void SetBiomassTransferIn(double[] dm, double[] n)
        {
            for (int layer = 0; layer < nLayers; layer++)
            {
                dmTransferredInByLayer[layer] += dm[layer];
                nTransferredInByLayer[layer] += n[layer];
            }
        }

        /// <summary>Computes the N amount that is potentially remobilisable.</summary>
        /// <param name="nConcThreshold">The N concentration above which remobilisation can occur.</param>
        /// <remarks>The nConc threshold should be the optimum for live tissue and for dead is the minimum.</remarks>
        public void GetRemobilisableN(double nConcThreshold)
        {
            // get the N amount remobilisable (all N in this tissue above the given nConc threshold)
            for (int layer = 0; layer < nLayers; layer++)
            {
                double potentialRemobilisableN = 0.0;

                // first, get the available N in the tissue
                if (this.Name != "Dead")
                {
                    potentialRemobilisableN = (dmByLayer[layer] - dmTransferredOutByLayer[layer]) * Math.Max(0.0, biomass.NConc - nConcThreshold);
                    // NOTE: N already in dead tissue is no longer available for remobilisation
                }

                // then get the N that is available in the material being transferred in (includes into dead, i.e. senesced)
                potentialRemobilisableN += Math.Max(0.0, nTransferredInByLayer[layer] - dmTransferredInByLayer[layer] * nConcThreshold);

                // only a fraction of the potentially remobilisable N can actually be remobilised each day
                nRemobilisableByLayer[layer] = Math.Max(0.0, potentialRemobilisableN * FractionNRemobilisable);
            }
        }

        /// <summary>Removes a fraction of remobilisable N for use into new growth.</summary>
        /// <param name="fraction">The fraction to remove (0-1)</param>
        public void DoRemobiliseN(double fraction)
        {
            for (int layer = 0; layer < nLayers; layer++)
            {
                nRemobilisedByLayer[layer] = nRemobilisableByLayer[layer] * fraction;
            }
        }

        /// <summary>Reset the transfer amounts in this tissue.</summary>
        public void ClearDailyTransferredAmounts()
        {
            if (dmTransferredInByLayer != null)
            {
                Array.Clear(dmTransferredInByLayer, 0, nLayers);
                Array.Clear(nTransferredInByLayer, 0, nLayers);
                Array.Clear(dmTransferredOutByLayer, 0, nLayers);
                Array.Clear(nTransferredOutByLayer, 0, nLayers);
                Array.Clear(dmRemovedByLayer, 0, nLayers);
                Array.Clear(nRemovedByLayer, 0, nLayers);
                Array.Clear(nRemobilisableByLayer, 0, nLayers);
                Array.Clear(nRemobilisedByLayer, 0, nLayers);
            }
        }
    }
}
