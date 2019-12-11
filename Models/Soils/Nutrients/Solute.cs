

namespace Models.Soils.Nutrients
{
    using Core;
    using Interfaces;
    using System;
    using APSIM.Shared.Utilities;

    /// <summary>
    /// # [Name]
    /// [DocumentType Memo]
    /// 
    /// This class used for this nutrient encapsulates the nitrogen within a mineral N pool.  Child functions provide information on flows of N from it to other mineral N pools, or losses from the system.
    /// 
    /// ## Mineral N Flows
    /// [DocumentType NFlow]
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Nutrient))]
    public class Solute : Model, ISolute
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
            Reset();
        }

        /// <summary>
        /// Set solute to initialisation state
        /// </summary>
        public void Reset()
        {
            double[] initialkgha = Apsim.Get(soil.Initial, Name + "N") as double[];           
            if (initialkgha == null)
                kgha = new double[soil.Thickness.Length];  // Urea will fall to here.
            else
                kgha = ReflectionUtilities.Clone(initialkgha) as double[];
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
