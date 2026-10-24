using System;
using APSIM.Core;
using APSIM.Numerics;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.PMF;
using Models.PMF.Phen;
using Models.Soils;
using Newtonsoft.Json;

namespace Models.Functions
{
    /// <summary>
    /// Calculates the soil water stress factor on the germination rate, fW(ψ, T), at the sowing depth.
    /// The seed layer matric potential ψ is taken from the soil water balance model ([Soil].Water.Potential),
    /// so germination uses the same state variable as the rest of the simulation (SoilWater or SWIM).
    /// The base water potential ψb(T) (child BaseWaterPotential) is
    /// the drought limit below which germination stops. fW = 1 when ψ ≥ 0,
    /// fW = ((ψ - ψb) / (0 - ψb))^q when ψb &lt; ψ &lt; 0 and fW = 0 when ψ ≤ ψb.
    /// </summary>
    [Serializable]
    [Description("Returns the soil water potential factor on germination rate")]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(GerminatingPhaseHydroThermal))]
    public class GerminatingWaterPotentialResponse : Model
    {
        [Link]
        private Plant plant = null;

        [Link]
        private IPhysical soilPhysical = null;

        [Link]
        private Water water = null;

        /// <summary>Base water potential (MPa) as a function of seed layer temperature, ψb(T).</summary>
        [Link(Type = LinkType.Child, ByName = true)]
        private IFunction BaseWaterPotential = null;

        /// <summary>Curvature of the water stress factor (q).</summary>
        [Description("Curvature of the water stress factor (q)")]
        public double Curvature { get; set; } = 1.80845439962828;

        /// <summary>Volumetric soil water content of the seed layer.</summary>
        [JsonIgnore]
        [Units("mm/mm")]
        public double SeedWaterContent { get; private set; }

        /// <summary>Soil water potential of the seed layer.</summary>
        [JsonIgnore]
        [Units("MPa")]
        public double SeedWaterPotential { get; private set; }

        /// <summary>Base water potential applied today, ψb(T).</summary>
        [JsonIgnore]
        [Units("MPa")]
        public double BaseWaterPotentialToday { get; private set; }

        /// <summary>Water stress factor (0-1) applied today, fW(ψ, T).</summary>
        [JsonIgnore]
        public double WaterFactor { get; private set; }

        /// <summary>
        /// Calculates the water stress factor for today from the seed layer water content.
        /// Call after the seed layer temperature used by BaseWaterPotential has been updated.
        /// </summary>
        public double Calculate()
        {
            int i = SoilUtilities.LayerIndexOfDepth(soilPhysical.Thickness, plant.SowingData.Depth);
            SeedWaterContent = water.Volumetric[i];
            double[] potential;
            try
            {
                potential = water.Potential;
            }
            catch (NotImplementedException)
            {
                throw new Exception($"{Name}: the soil water model does not provide soil water potential. Use SoilWater or SWIM.");
            }
            SeedWaterPotential = KPaToMPa(potential[i]);
            BaseWaterPotentialToday = BaseWaterPotential.Value();
            WaterFactor = WaterFactorAt(SeedWaterPotential, BaseWaterPotentialToday, Curvature);
            return WaterFactor;
        }

        /// <summary>Water stress factor (0-1) for a water potential and a base water potential.</summary>
        /// <param name="psi">Seed layer water potential (MPa).</param>
        /// <param name="psiBase">Base water potential (MPa, negative).</param>
        /// <param name="q">Curvature.</param>
        public static double WaterFactorAt(double psi, double psiBase, double q)
        {
            if (psi >= 0)
                return 1;
            if (psiBase >= 0 || psi <= psiBase)
                return 0;
            return Math.Pow((psi - psiBase) / (0 - psiBase), q);
        }

        /// <summary>Converts a soil water potential from kPa (Water.Potential) to MPa.</summary>
        /// <param name="psiKPa">Soil water potential (kPa).</param>
        public static double KPaToMPa(double psiKPa)
        {
            return psiKPa / 1000;
        }
    }
}
