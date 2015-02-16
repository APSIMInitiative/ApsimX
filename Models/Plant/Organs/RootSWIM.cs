using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Interfaces;

namespace Models.PMF.Organs
{
    /// <summary>
    /// A root model for SWIM
    /// </summary>
    [Serializable]
    public class RootSWIM : BaseOrgan, BelowGround
    {
        /// <summary>The plant</summary>
        [Link]
        Plant Plant = null;

        /// <summary>The uptake</summary>
        private double[] Uptake = null;
        /// <summary>The RLV</summary>
        public double[] rlv = null;


        /// <summary>Gets or sets the water uptake.</summary>
        /// <value>The water uptake.</value>
        [Units("mm")]
        public override double WaterUptake
        {
            get { return -Utility.Math.Sum(Uptake); }
        }


        /// <summary>Called when [water uptakes calculated].</summary>
        /// <param name="Uptakes">The uptakes.</param>
        [EventSubscribe("WaterUptakesCalculated")]
        private void OnWaterUptakesCalculated(WaterUptakesCalculatedType Uptakes)
        {
            for (int i = 0; i != Uptakes.Uptakes.Length; i++)
            {
                if (Uptakes.Uptakes[i].Name == Plant.Name)
                    Uptake = Uptakes.Uptakes[i].Amount;
            }
        }

        /// <summary>Called when [simulation commencing].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            Clear();
        }

        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Sowing")]
        private void OnSowing(object sender, SowPlant2Type data)
        {
            if (data.Plant == Plant)
                Clear();
        }

        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantEnding")]
        private void OnPlantEnding(object sender, EventArgs e)
        {
            if (sender == Plant)
                Clear();
        }

    }
}
