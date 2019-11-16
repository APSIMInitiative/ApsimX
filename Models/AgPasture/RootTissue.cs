namespace Models.AgPasture
{
    using APSIM.Shared.Utilities;
    using System;
    using System.Linq;

    /// <summary>Describes a root tissue of a pasture species.</summary>
    [Serializable]
    internal class RootTissue : GenericTissue
    {
        /// <summary>Constructor, initialise array.</summary>
        /// <param name="numLayers">The number of layers in the soil</param>
        public RootTissue(int numLayers)
        {
            nLayers = numLayers;
            DMLayer = new double[nLayers];
            NamountLayer = new double[nLayers];
            PamountLayer = new double[nLayers];
            DMLayersTransferedIn = new double[nLayers];
            NLayersTransferedIn = new double[nLayers];
        }

        /// <summary>Number of layers in the soil.</summary>
        private int nLayers;

        #region Basic properties  ------------------------------------------------------------------------------------------

        ////- State properties >>>  - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>Gets or sets the dry matter weight (kg/ha).</summary>
        internal override double DM
        {
            get { return DMLayer.Sum(); }
            set
            {
                double[] prevRootFraction = FractionWt;
                for (int layer = 0; layer < nLayers; layer++)
                    DMLayer[layer] = value * prevRootFraction[layer];
            }
        }

        /// <summary>Gets or sets the DM amount for each layer (kg/ha).</summary>
        internal double[] DMLayer;

        /// <summary>Gets or sets the nitrogen content (kg/ha).</summary>
        internal override double Namount
        {
            get { return NamountLayer.Sum(); }
            set
            {
                for (int layer = 0; layer < nLayers; layer++)
                    NamountLayer[layer] = value * FractionWt[layer];
            }
        }

        /// <summary>Gets or sets the N content for each layer (kg/ha).</summary>
        internal double[] NamountLayer;

        /// <summary>Gets or sets the phosphorus content (kg/ha).</summary>
        internal override double Pamount
        {
            get { return PamountLayer.Sum(); }
            set
            {
                for (int layer = 0; layer < nLayers; layer++)
                    PamountLayer[layer] = value * FractionWt[layer];
            }
        }

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
                    result[layer] = MathUtilities.Divide(DMLayer[layer], DM, 0.0);
                return result;
            }
        }

        #endregion ---------------------------------------------------------------------------------------------------------

        #region Tissue methods  --------------------------------------------------------------------------------------------

        /// <summary>Updates the tissue state, make changes in DM and N effective.</summary>
        internal override void DoUpdateTissue()
        {
            // removals first as they do not change distribution over the profile
            DM -= DMTransferedOut;
            Namount -= NTransferedOut;

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
        }

        #endregion ---------------------------------------------------------------------------------------------------------
    }
}
