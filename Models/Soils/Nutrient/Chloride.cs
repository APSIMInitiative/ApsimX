namespace Models.Soils.Nutrient
{
    using Core;
    using Interfaces;
    using System;

    /// <summary>
    /// # [Name]
    /// Encapsulates a solute class.
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Nutrient))]
    [ValidParent(ParentType = typeof(Soil))]
    public class Chloride : Model, ISolute
    {
        [Link]
        Soil soil = null;

        /// <summary>Solute amount (kg/ha)</summary>
        public double[] kgha { get; set; } 

        /// <summary>Solute amount (ppm)</summary>
        public double[] ppm { get { return soil.kgha2ppm(kgha); } }

        /// <summary>Performs the initial checks and setup</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            double[] initialppm = soil.Cl;
            if (initialppm == null)
                initialppm = new double[soil.Thickness.Length];
            kgha = soil.ppm2kgha(initialppm);
        }
    }
}
