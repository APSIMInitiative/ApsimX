using System;
using System.Collections.Generic;
using System.Text;
using Models.Core;
using Models.PMF.Interfaces;
using APSIM.Shared.Utilities;
using Models.PMF.Library;

namespace Models.PMF.Organs
{
    /// <summary>
    /// A root model for SWIM
    /// </summary>
    [Serializable]
    public class RootSWIM : BaseOrgan, BelowGround
    {
        /// <summary>The uptake</summary>
        private double[] Uptake = null;
        /// <summary>The RLV</summary>
        public double[] rlv = null;


        /// <summary>Link to biomass removal model</summary>
        [ChildLink]
        public BiomassRemoval biomassRemovalModel = null;

        /// <summary>Gets or sets the water uptake.</summary>
        /// <value>The water uptake.</value>
        [Units("mm")]
        public double WaterUptake
        {
            get { return -MathUtilities.Sum(Uptake); }
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
        private new void OnSimulationCommencing(object sender, EventArgs e)
        {
            base.OnSimulationCommencing(sender, e);
            Clear();
        }

        /// <summary>Called when crop is ending</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="data">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("PlantSowing")]
        private void OnPlantSowing(object sender, SowPlant2Type data)
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
            if (Wt > 0.0)
            {
                Detached.Add(Live);
                Detached.Add(Dead);
                SurfaceOrganicMatter.Add(Wt * 10, N * 10, 0, Plant.CropType, Name);
            }

            Clear();
        }

        /// <summary>Removes biomass from organs when harvest, graze or cut events are called.</summary>
        /// <param name="biomassRemoveType">Name of event that triggered this biomass remove call.</param>
        /// <param name="value">The fractions of biomass to remove</param>
        public override void DoRemoveBiomass(string biomassRemoveType, OrganBiomassRemovalType value)
        {
            biomassRemovalModel.RemoveBiomass(biomassRemoveType, value, Live, Dead, Removed, Detached);
        }

    }
}
