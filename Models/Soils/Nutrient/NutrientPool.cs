namespace Models.Soils.Nutrient
{
    using Models.Core;
    using PMF.Functions;
    using System;
    using System.Collections.Generic;
    using APSIM.Shared.Utilities;
    /// <summary>
    /// # [Name]
    /// This pool encapsulates the carbon and nitrogen within a soil organic matter pool.  Child functions provide information on its initialisation and flows of C and N from it to other pools, or losses from the system.
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Nutrient))]
    public class NutrientPool : Model
    {
        [Link]
        Soil soil = null;

        [Link]
        IFunction InitialCarbon = null;

        [Link]
        IFunction InitialNitrogen = null;

        /// <summary>Initial carbon/nitrogen ratio</summary>
        public double[] CNRatio { get { return MathUtilities.Divide(C, N, 0); } }

        /// <summary>Amount of carbon (kg/ha)</summary>
        public double[] C { get; set; }

        /// <summary>Amount of nitrogen (kg/ha)</summary>
        public double[] N { get; set; }

        /// <summary>Performs the initial checks and setup</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            C = InitialCarbon.Values();
            if (C.Length == 1)
                C = MathUtilities.CreateArrayOfValues(C[0], soil.Thickness.Length);
            N = InitialNitrogen.Values();
            if (N.Length == 1)
                N = MathUtilities.CreateArrayOfValues(N[0], soil.Thickness.Length);
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
