using System;
using System.Linq;
using Models.Core;
using Models.Soils;
using Models.Soils.Nutrients;
using APSIM.Shared.Utilities;

namespace Models.AgPasture
{

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

        //----------------------- Constants -----------------------

        /// <summary>Average carbon content in plant dry matter (kg/kg).</summary>
        private const double carbonFractionInDM = 0.4;

        /// <summary>Minimum significant difference between two values.</summary>
        internal const double Epsilon = 0.000000001;

        //---------------------------- Parameters -----------------------

        /// <summary>Fraction of excess N, above optimum N for live tissues and minimum for dead tissue, that is remobilisable per day (0-1).</summary>
        public double FractionNRemobilisable { get; set; } = 0.1;

        //----------------------- Daily Deltas -----------------------

        /// <summary>Amount of dry matter transferred into this tissue, for each layer (kg/ha).</summary>
        private double[] dmTransferredInByLayer;

        /// <summary>Amount of nitrogen transferred into this tissue, for each layer (kg/ha).</summary>
        private double[] nTransferredInByLayer;

        /// <summary>Dry matter amount transferred into this tissue (kg/ha).</summary>
        public double DMTransferredIn { get; private set; }

        /// <summary>Dry matter amount transferred out of this tissue (kg/ha).</summary>
        public double DMTransferredOut = 0.0;

        /// <summary>Nitrogen transferred into this tissue (kg/ha).</summary>
        public double NTransferredIn { get; private set; }

        /// <summary>Nitrogen transferred out of this tissue (kg/ha).</summary>
        public double NTransferredOut { get; private set; }

        /// <summary>DM removed from this tissue (kg/ha).</summary>
        public double DMRemoved { get;  set; }

        /// <summary>The fraction of DM removed from this tissue.</summary>
        public double FractionRemoved { get; private set; }

        /// <summary>N removed from this tissue (kg/ha).</summary>
        public double NRemoved { get;  set; }

        /// <summary>Amount of N available for remobilisation (kg/ha).</summary>
        public double NRemobilisable { get;  set; }

        /// <summary>Nitrogen remobilised into new growth (kg/ha).</summary>
        public double NRemobilised { get; set; }

        //----------------------- States -----------------------

        /// <summary>Tissue dry matter biomass.</summary>
        private AGPBiomass biomass = new AGPBiomass();

        /// <summary>Dry matter amount for each layer (kg/ha).</summary>
        private double[] dmByLayer;

        /// <summary>Nitrogen content for each layer (kg/ha).</summary>
        private double[] nByLayer;

        /// <summary>Phosphorus content for each layer (kg/ha).</summary>
        private double[] pByLayer;

        /// <summary>Dry matter biomass.</summary>
        public IAGPBiomass DM { get { return biomass; } }

        /// <summary>Dry matter fraction for each layer (0-1).</summary>
        public double[] FractionWt { get { return MathUtilities.Divide_Value(dmByLayer, DM.Wt); } }

        //----------------------- Public methods -----------------------

        /// <summary>Initialise this tissue instance.</summary>
        public void Initialise()
        {
            dmByLayer = new double[soilPhysical.Thickness.Length];
            nByLayer = new double[soilPhysical.Thickness.Length];
            pByLayer = new double[soilPhysical.Thickness.Length];
            dmTransferredInByLayer = new double[soilPhysical.Thickness.Length];
            nTransferredInByLayer = new double[soilPhysical.Thickness.Length];
        }

        /// <summary>Updates the tissue state, make changes in DM and N effective.</summary>
        public void Update()
        {
            // removals first as they do not change distribution over the profile
            double[] prevRootFraction = FractionWt;
            if (DMTransferredOut > 0.0 || NTransferredOut > 0.0)
            {
                for (int layer = 0; layer < dmByLayer.Length; layer++)
                {
                    dmByLayer[layer] -= DMTransferredOut * prevRootFraction[layer];
                    nByLayer[layer] -= NTransferredOut * prevRootFraction[layer];
                }
            }

            // additions need to consider distribution over the profile
            if (DMTransferredIn > 0.0 || NTransferredIn > 0.0)
            {
                for (int layer = 0; layer < dmByLayer.Length; layer++)
                {
                    dmByLayer[layer] += dmTransferredInByLayer[layer];
                    nByLayer[layer] += nTransferredInByLayer[layer] - (NRemobilised * (nTransferredInByLayer[layer] / NTransferredIn));
                }
            }

            UpdateDM();
        }

        /// <summary>Update dry matter.</summary>
        private void UpdateDM()
        {
            biomass.Wt = dmByLayer.Sum();
            biomass.N = nByLayer.Sum();
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
        /// <param name="amountDM">The DM amounts to detach (kg/ha).</param>
        /// <param name="amountN">The N amounts to detach (kg/ha).</param>
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

        /// <summary>Computes the DM and N amounts turned over for this tissue.</summary>
        /// <param name="turnoverRate">The turnover rate for the tissue today.</param>
        /// <param name="receivingTissue">The tissue to move the turned over biomass to.</param>
        /// <param name="nConc">The N concentration threshold to consider.</param>
        /// <remarks>For live tissues, potential N remobilisable is above optimum concentration, for dead is all above minimum</remarks>
        public void DoTissueTurnover(double turnoverRate, RootTissue receivingTissue, double nConc)
        {
            if (DM.Wt > 0.0 && turnoverRate > 0.0)
            {
                var turnedoverDM = DM.Wt * turnoverRate;
                var turnedoverN = DM.N * turnoverRate;
                DMTransferredOut += turnedoverDM;
                NTransferredOut += turnedoverN;
                if (receivingTissue != null)
                {
                    receivingTissue.SetBiomassTransferIn(dm: MathUtilities.Multiply_Value(FractionWt, turnedoverDM),
                                                          n: MathUtilities.Multiply_Value(FractionWt, turnedoverN));
                }

                // get the N amount remobilisable (all N in this tissue above the given nConc concentration)
                double totalRemobilisableN = (DM.Wt - DMTransferredOut) * Math.Max(0.0, DM.NConc - nConc);
                totalRemobilisableN += Math.Max(0.0, NTransferredIn - DMTransferredIn * nConc);
                NRemobilisable = Math.Max(0.0, totalRemobilisableN * FractionNRemobilisable);
            }
        }

        /// <summary>Set the biomass moving into the tissue.</summary>
        /// <param name="dm">Dry matter to add (kg/ha).</param>
        /// <param name="n">The nitrogen to add (kg/ha).</param>
        public void SetBiomassTransferIn(double[] dm, double[] n)
        {
            int nLayers = Math.Min(dm.Length, nTransferredInByLayer.Length);
            for (int layer = 0; layer < nLayers; layer++)
            {
                dmTransferredInByLayer[layer] += dm[layer];
                nTransferredInByLayer[layer] += n[layer];
            }

            DMTransferredIn = dmTransferredInByLayer.Sum();
            NTransferredIn = nTransferredInByLayer.Sum();
        }

        /// <summary>Removes a fraction of remobilisable N for use into new growth.</summary>
        /// <param name="fraction">The fraction to remove (0-1)</param>
        public void DoRemobiliseN(double fraction)
        {
            NRemobilised = NRemobilisable * fraction;
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
        /// <param name="fractionToSoil">The fracton of biomass to sent to soil.</param>
        /// <remarks>The same fraction is used for all layers.</remarks>
        public void RemoveBiomass(double fractionToRemove, double fractionToSoil)
        {
            var nLayers = dmByLayer.Length;
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
                DMRemoved += dmToRemove;
                NRemoved += nToRemove;
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
            var nLayers = Math.Min(fractionToRemove.Length, fractionToSoil.Length);
            double[] dmToSoil = new double[nLayers];
            double[] nToSoil = new double[nLayers];
            for (int layer = 0; layer < nLayers; layer++)
            {
                var totalFraction = fractionToRemove[layer] + fractionToSoil[layer];
                var dmToRemove = dmByLayer[layer] * totalFraction;
                var nToRemove= nByLayer[layer] * totalFraction;
                dmToSoil[layer] = dmByLayer[layer] * fractionToSoil[layer];
                nToSoil[layer] = nByLayer[layer] * fractionToSoil[layer];
                dmByLayer[layer] -= dmToRemove;
                nByLayer[layer] -= nToRemove;
                DMRemoved += dmToRemove;
                NRemoved += nToRemove;
            }

            UpdateDM();

            if (fractionToSoil.Sum() > 0.0)
            {
                DetachBiomass(dmToSoil, nToSoil);
            }
        }

        /// <summary>Reset the transfer amounts in this tissue.</summary>
        public void ClearDailyTransferredAmounts()
        {
            DMTransferredIn = 0.0;
            DMTransferredOut = 0.0;
            NTransferredIn = 0.0;
            NTransferredOut = 0.0;
            NRemobilisable = 0.0;
            NRemobilised = 0.0;
            DMRemoved = 0.0;
            NRemoved = 0.0;
            FractionRemoved = 0.0;
            if (dmTransferredInByLayer != null && nTransferredInByLayer != null)
            {
                Array.Clear(dmTransferredInByLayer, 0, dmTransferredInByLayer.Length);
                Array.Clear(nTransferredInByLayer, 0, nTransferredInByLayer.Length);
            }
        }
    }
}
