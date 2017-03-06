namespace Models.Soils.Nutrient
{
    using Models.Core;
    using PMF.Functions;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Carbon / nitrogen pool
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Nutrient))]
    public class NutrientPool : Model
    {
        [Link]
        SoilOrganicMatter soilOrganicMatter = null;

        [Link]
        IFunctionArray InitialiseCarbon = null;

        [Link]
        IFunctionArray InitialiseNitrogen = null;

        /// <summary>Initial carbon/nitrogen ratio</summary>
        public double CNRatio { get { return soilOrganicMatter.SoilCN; } }

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
            C = InitialiseCarbon.Values;
            N = InitialiseNitrogen.Values;
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
