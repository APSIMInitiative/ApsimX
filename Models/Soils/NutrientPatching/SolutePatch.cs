

namespace Models.Soils.NutrientPatching
{
    using Core;
    using Interfaces;
    using System;
    using APSIM.Shared.Utilities;
    using Models.Soils.Nutrients;

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
    [ValidParent(ParentType = typeof(NutrientPatchManager))]
    public class SolutePatch : Model, ISolute
    {
        private Soil soil;
        private Sample initial = null;
        private IPhysical soilPhysical = null;

        private NutrientPatchManager patchManager;

        /// <summary>Solute amount (kg/ha)</summary>
        public double[] kgha 
        { 
            get 
            { 
                return patchManager.GetSoluteKgha(Name); 
            } 
            set
            {
                patchManager?.SetSoluteKgha(SoluteSetterType.Other, Name, value);
            }
        }

        /// <summary>Solute amount (ppm)</summary>
        public double[] ppm { get { return SoilUtilities.kgha2ppm(soilPhysical.Thickness, soilPhysical.BD, kgha); } }

        /// <summary>
        /// Invoked when model is first created.
        /// </summary>
        public override void OnCreated()
        {
            base.OnCreated();
            soil = FindAncestor<Soil>();
            patchManager = FindAncestor<NutrientPatchManager>();
        }

        /// <summary>Performs the initial checks and setup</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            initial = soil.FindChild<Sample>();
            soilPhysical = soil.FindChild<IPhysical>();
            if (!Name.Contains("PlantAvailable"))
                Reset();
        }

        /// <summary>
        /// Set solute to initialisation state
        /// </summary>
        public void Reset()
        {
            double[] initialkgha = initial.FindByPath(Name + "N")?.Value as double[];
            if (initialkgha == null)
                SetKgHa(SoluteSetterType.Other, new double[soilPhysical.Thickness.Length]);  // Urea will fall to here.
            else
                SetKgHa(SoluteSetterType.Other, ReflectionUtilities.Clone(initialkgha) as double[]);
        }
        /// <summary>Setter for kgha.</summary>
        /// <param name="callingModelType">Type of calling model.</param>
        /// <param name="value">New values.</param>
        public void SetKgHa(SoluteSetterType callingModelType, double[] value)
        {
            patchManager.SetSoluteKgha(callingModelType, Name, value);
        }

        /// <summary>Setter for kgha delta.</summary>
        /// <param name="callingModelType">Type of calling model</param>
        /// <param name="delta">New delta values</param>
        public void AddKgHaDelta(SoluteSetterType callingModelType, double[] delta)
        {
            var values = kgha;
            for (int i = 0; i < delta.Length; i++)
                kgha[i] += delta[i];
            SetKgHa(callingModelType, values);
        }
    }
}
