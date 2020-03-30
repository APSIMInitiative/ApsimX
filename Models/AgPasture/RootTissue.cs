namespace Models.AgPasture
{
    using APSIM.Shared.Utilities;
    using Models.Interfaces;
    using Models.Soils;
    using System;
    using System.Linq;

    /// <summary>Describes a root tissue of a pasture species.</summary>
    [Serializable]
    internal class RootTissue : GenericTissue
    {
        private readonly string speciesName;
        private readonly INutrient nutrientModel;

        /// <summary>Constructor.</summary>
        /// <param name="speciesNam">The name of the species this tissue belongs to.</param>
        /// <param name="nutrient">The nutrient model.</param>
        /// <param name="numLayers">The number of layers in the soil</param>
        public RootTissue(string speciesNam, INutrient nutrient, int numLayers)
        {
            speciesName = speciesNam;
            nutrientModel = nutrient;
            nLayers = numLayers;
            DMLayer = new double[nLayers];
            NamountLayer = new double[nLayers];
            PamountLayer = new double[nLayers];
            DMLayersTransferedIn = new double[nLayers];
            NLayersTransferedIn = new double[nLayers];
        }

        /// <summary>Number of layers in the soil.</summary>
        private int nLayers;

        /// <summary>Average carbon content in plant dry matter (kg/kg).</summary>
        const double CarbonFractionInDM = 0.4;

        #region Basic properties  ------------------------------------------------------------------------------------------

        ////- State properties >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Gets or sets the DM amount for each layer (kg/ha).</summary>
        internal double[] DMLayer;

        /// <summary>Gets or sets the N content for each layer (kg/ha).</summary>
        internal double[] NamountLayer;

        /// <summary>Gets or sets the P content for each layer (kg/ha).</summary>
        internal double[] PamountLayer { get; set; }

        ////- Amounts in and out >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Gets or sets the DM amount transferred into this tissue, for each layer (kg/ha).</summary>
        internal double[] DMLayersTransferedIn { get; set; }

        /// <summary>Gets or sets the amount of N transferred into this tissue, for each layer (kg/ha).</summary>
        internal double[] NLayersTransferedIn { get; set; }

        #endregion ---------------------------------------------------------------------------------------------------------

        #region Derived properties (outputs)  ------------------------------------------------------------------------------

        /// <summary>Gets the dry matter fraction for each layer (0-1).</summary>
        internal double[] FractionWt
        {
            get
            {
                double[] result = new double[nLayers];
                for (int layer = 0; layer < nLayers; layer++)
                    result[layer] = MathUtilities.Divide(DMLayer[layer], dm.Wt, 0.0);
                return result;
            }
        }

        #endregion ---------------------------------------------------------------------------------------------------------

        /// <summary>Updates the tissue state, make changes in DM and N effective.</summary>
        internal override void DoUpdateTissue()
        {
            // removals first as they do not change distribution over the profile
            var amountDMToRemove = dm.Wt - DMTransferedOut;
            var amountNToRemove = dm.N - NTransferedOut;
            double[] prevRootFraction = FractionWt;
            for (int layer = 0; layer < nLayers; layer++)
                DMLayer[layer] = amountDMToRemove * prevRootFraction[layer];

            UpdateDM();
            for (int layer = 0; layer < nLayers; layer++)
                NamountLayer[layer] = amountNToRemove * FractionWt[layer];

            // additions need to consider distribution over the profile
            DMTransferedIn = DMLayersTransferedIn.Sum();
            NTransferedIn = NLayersTransferedIn.Sum();
            if ((DMTransferedIn > MyPrecision) || (NTransferedIn > MyPrecision))
            {
                for (int layer = 0; layer < nLayers; layer++)
                {
                    DMLayer[layer] += DMLayersTransferedIn[layer];
                    NamountLayer[layer] += NLayersTransferedIn[layer] - (NRemobilised * (NLayersTransferedIn[layer] / NTransferedIn));
                }
            }
            UpdateDM();
        }

        /// <summary>Adds a given amount of detached root material (DM and N) to the soil's FOM pool.</summary>
        /// <param name="amountDM">The DM amount to send (kg/ha)</param>
        /// <param name="amountN">The N amount to send (kg/ha)</param>
        public override void DetachBiomass(double amountDM, double amountN)
        {
            if (amountDM + amountN > 0.0)
            {
                FOMLayerLayerType[] FOMdataLayer = new FOMLayerLayerType[nLayers];
                for (int layer = 0; layer < nLayers; layer++)
                {
                    FOMType fomData = new FOMType();
                    fomData.amount = amountDM * FractionWt[layer];
                    fomData.N = amountN * FractionWt[layer];
                    fomData.C = amountDM * CarbonFractionInDM * FractionWt[layer];
                    fomData.P = 0.0; // P not considered here
                    fomData.AshAlk = 0.0; // Ash not considered here

                    FOMLayerLayerType layerData = new FOMLayerLayerType();
                    layerData.FOM = fomData;
                    layerData.CNR = 0.0; // not used here
                    layerData.LabileP = 0.0; // not used here

                    FOMdataLayer[layer] = layerData;
                }

                FOMLayerType FOMData = new FOMLayerType();
                FOMData.Type = speciesName;
                FOMData.Layer = FOMdataLayer;
                nutrientModel.DoIncorpFOM(FOMData);
            }
        }

        public void UpdateDM()
        {
            dm.Wt = DMLayer.Sum();
            dm.N = NamountLayer.Sum();
        }

        public override void ResetTo(double dmAmount, double nAmount)
        {
            base.ResetTo(dmAmount, nAmount);
            throw new NotImplementedException("Not implemented: Need to check how root reset works.");
        }

        public override void Reset()
        {
            base.Reset();
            throw new NotImplementedException("Not implemented: Need to check how root reset works.");
        }
    }
}
