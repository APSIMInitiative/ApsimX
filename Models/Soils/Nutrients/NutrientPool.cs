namespace Models.Soils.Nutrients
{
    using Models.Core;
    using Functions;
    using System;
    using System.Collections.Generic;
    using APSIM.Shared.Utilities;
    /// <summary>
    /// # [Name]
    /// [DocumentType Memo]
    /// 
    /// ## Initialisation
    /// The initialisation of Carbon and Nutrient contents of this pool is described as follows:
    /// 
    /// [Document InitialCarbon]
    /// [Document InitialNitrogen]
    /// 
    /// ## Organic Matter Flows
    /// [DocumentType CarbonFlow]
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Nutrient))]
    public class NutrientPool : Model, INutrientPool
    {
        [Link]
        Soil soil = null;

        [Link(Type = LinkType.Child, ByName = true)]
        IFunction InitialCarbon = null;

        [Link(Type = LinkType.Child, ByName = true)]
        IFunction InitialNitrogen = null;

        /// <summary>Initial carbon/nitrogen ratio</summary>
        public double[] CNRatio { get { return MathUtilities.Divide(C, N, 0); } }

        /// <summary>Amount of carbon (kg/ha)</summary>
        public double[] C { get; set; }

        /// <summary>Amount of nitrogen (kg/ha)</summary>
        public double[] N { get; set; }

        /// <summary>
        /// Fraction of each layer occupied by this pool.
        /// /// </summary>
        public double[] LayerFraction { get; set; }

        /// <summary>Performs the initial checks and setup</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            Reset();
        }

        /// <summary>
        /// Set nutrient pool to initialisation state
        /// </summary>
        public void Reset()
        {
            C = new double[soil.Thickness.Length];
            for (int i = 0; i < C.Length; i++)
                C[i] = InitialCarbon.Value(i);

            N = new double[soil.Thickness.Length];
            for (int i = 0; i < N.Length; i++)
                N[i] = InitialNitrogen.Value(i);

            // Set fraction of the layer undertaking this flow to 1 - default unless changed by parent model
            LayerFraction = new double[soil.Thickness.Length];
            for (int i = 0; i < LayerFraction.Length; i++)
                LayerFraction[i] = 1.0;
        }

        /// <summary>
        /// Add C and N into nutrient pool
        /// </summary>
        /// <param name="CAdded"></param>
        /// <param name="NAdded"></param>
        public void Add (double[] CAdded, double[] NAdded)
        {
            if (CAdded.Length != NAdded.Length)
                throw new Exception("Arrays for addition of soil organic matter must be of same length.");
            if (CAdded.Length > C.Length)
                throw new Exception("Array for addition of soil organic matter must be less than or equal to the number of soil layers.");

            for (int i = 0; i < CAdded.Length; i++)
            {
                C[i] += CAdded[i];
                N[i] += NAdded[i];
            }
        }

    }
}
