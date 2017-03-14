namespace Models.Soils.Nutrient
{
    using Models.Core;
    using System;
    using APSIM.Shared.Utilities;

    /// <summary>
    /// Soil carbon model
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Soil))]
    public class Nutrient : Model
    {
        [Link]
        Soil soil = null;
        ///// <summary>Pools</summary>
        //[Link]
        //private List<Pool> pools = null;

        ///// <summary>List of flows</summary>
        //[Link]
        //private List<Flow> flows = null;

        /// <summary>Nitrate (kg/ha)</summary>
        public double[] NO3 { get; set; }

        /// <summary>Ammonia (kg/ha)</summary>
        public double[] NH4 { get; set; }

        /// <summary>NO3 (ppm)</summary>
        public double[] NO3ppm { get { return kgha2ppm(NO3); } }

        /// <summary>NH4 (ppm)</summary>
        public double[] NH4ppm { get { return kgha2ppm(NH4); } }


        /// <summary>Urea (kg/ha)</summary>
        public double[] Urea { get; set; }

        /// <summary>
        /// Calculate conversion factor from kg/ha to ppm (mg/kg)
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        private double[] kgha2ppm(double[] values)
        {
            for (int i = 0; i < values.Length; i++)
                values[i] *= MathUtilities.Divide(100.0, soil.BD[i] * soil.Thickness[i], 0.0);
            return values;
        }
        /// <summary>Performs the initial checks and setup</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            NO3 = soil.InitialNO3N;
            NH4 = soil.InitialNH4N;
            Urea = new double[NH4.Length];
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

        /// <summary>Gets the changes in mineral N made by other modules</summary>
        /// <param name="NitrogenChanges">The nitrogen changes.</param>
        [EventSubscribe("NitrogenChanged")]
        private void OnNitrogenChanged(NitrogenChangedType NitrogenChanges)
        {
            NO3 = MathUtilities.Add(NO3, NitrogenChanges.DeltaNO3);
            NH4 = MathUtilities.Add(NH4, NitrogenChanges.DeltaNH4);
            Urea = MathUtilities.Add(Urea, NitrogenChanges.DeltaUrea);
        }
    }
}
