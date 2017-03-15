namespace Models.Soils.Nutrient
{
    using Interfaces;
    using Models.Core;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Soil carbon model
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Soil))]
    public class Nutrient : Model, ISolute
    {
        [Link]
        Soil soil = null;

        /// <summary>Nitrate (ppm)</summary>
        [Solute]
        public double[] NO3 { get; set; }

        /// <summary>Ammonia (ppm)</summary>
        [Solute]
        public double[] NH4 { get; set; }

        /// <summary>Performs the initial checks and setup</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            NO3 = soil.InitialNO3N;
            NH4 = soil.InitialNH4N;
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
