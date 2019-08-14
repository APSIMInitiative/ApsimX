namespace Models.Soils.Nutrients
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
            double[] initialppm = soil.Initial.CL;
            if (initialppm == null)
                initialppm = new double[soil.Thickness.Length];
            kgha = soil.ppm2kgha(initialppm);
        }

        /// <summary>Setter for kgha.</summary>
        /// <param name="callingModelType">Type of calling model.</param>
        /// <param name="value">New values.</param>
        public void SetKgHa(SoluteSetterType callingModelType, double[] value)
        {
            kgha = value;
        }


        /// <summary>Setter for kgha delta.</summary>
        /// <param name="callingModelType">Type of calling model</param>
        /// <param name="delta">New delta values</param>
        public void AddKgHaDelta(SoluteSetterType callingModelType, double[] delta)
        {
            for (int i = 0; i < delta.Length; i++)
                kgha[i] += delta[i];
        }
    }
}
