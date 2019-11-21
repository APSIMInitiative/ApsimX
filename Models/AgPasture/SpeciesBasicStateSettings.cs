namespace Models.AgPasture
{
    using System;

    /// <summary>Basic values defining the state of a pasture species.</summary>
    [Serializable]
    internal class SpeciesBasicStateSettings
    {
        /// <summary>Plant phenological stage.</summary>
        internal int PhenoStage;

        /// <summary>DM weight for each biomass pool (kg/ha).</summary>
        internal double[] DMWeight;

        /// <summary>N amount for each biomass pool (kg/ha).</summary>
        internal double[] NAmount;

        /// <summary>Root depth (mm).</summary>
        internal double RootDepth;

        /// <summary>Constructor, initialise the arrays.</summary>
        public SpeciesBasicStateSettings()
        {
            // there are 12 tissue pools, in order: leaf1, leaf2, leaf3, leaf4, stem1, stem2, stem3, stem4, stolon1, stolon2, stolon3, and root
            DMWeight = new double[12];
            NAmount = new double[12];
        }
    }
}