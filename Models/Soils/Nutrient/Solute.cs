

namespace Models.Soils.Nutrient
{
    using Core;
    using Interfaces;
    using Functions;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// # [Name]
    /// Encapsulates a solute class.
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
            double[] initialppm = Apsim.Get(soil, "Initial" + Name + "N", true) as double[];
            if (initialppm == null)
                initialppm = new double[soil.Thickness.Length];
            kgha = soil.ppm2kgha(initialppm);
        }
    }
}
