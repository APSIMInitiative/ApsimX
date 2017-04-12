namespace Models.Soils.Nutrient
{
    using Models.Core;
    using PMF.Functions;
    using System;
    using System.Collections.Generic;
    using APSIM.Shared.Utilities;
    /// <summary>
    /// Carbon / nitrogen pool
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Nutrient))]
    public class NutrientPool : Model
    {
        [Link]
        Soil soil = null;

        [Link]
        IFunction InitialiseCarbon = null;

        [Link]
        IFunction InitialiseNitrogen = null;

        /// <summary>Initial carbon/nitrogen ratio</summary>
        public double[] CNRatio { get { return MathUtilities.Divide(C, N); } }

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
            C = new double[soil.Thickness.Length];
            for (int i = 0; i < C.Length; i++)
                C[i] = InitialiseCarbon.Value(i);

            N = new double[soil.Thickness.Length];
            for (int i = 0; i < N.Length; i++)
                N[i] = InitialiseNitrogen.Value(i);
        }

        /// <summary>
        /// Get the information on potential residue decomposition - perform daily calculations as part of this.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("DoSoilOrganicMatter")]
        private void OnDoSoilOrganicMatter(object sender, EventArgs e)
        {

        }
    }
}
