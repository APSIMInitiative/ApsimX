using System;
using System.Collections.Generic;
using APSIM.Core;
using APSIM.Numerics;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Models.PMF;
using Models.PMF.Phen;
using Models.Soils;
using Newtonsoft.Json;

namespace Models.Functions
{
    /// <summary>
    /// Calculates the soil water stress factor on the germination rate, fW(ψ, T), at the sowing depth.
    /// The seed layer water potential ψ is derived from the volumetric soil water content using a
    /// retention curve in which log10(-ψ) is interpolated linearly in water content between the
    /// anchors AirDry (-100 MPa), LL15 (-1.5 MPa), DUL (-0.033 MPa) and SAT (-0.001 MPa), held
    /// constant beyond the end anchors. The base water potential ψb(T) (child BaseWaterPotential) is
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
        private ISoilWater waterBalance = null;

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
            SeedWaterContent = waterBalance.SW[i];
            SeedWaterPotential = PsiFromTheta(SeedWaterContent, soilPhysical.AirDry[i], soilPhysical.LL15[i],
                                              soilPhysical.DUL[i], soilPhysical.SAT[i]);
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

        /// <summary>
        /// Soil water potential (MPa) from volumetric water content, interpolating log10(-ψ) linearly in
        /// water content between AirDry (-100 MPa), LL15 (-1.5 MPa), DUL (-0.033 MPa) and SAT (-0.001 MPa).
        /// The AirDry anchor is dropped when AirDry ≥ LL15. Values are held constant beyond the end anchors.
        /// </summary>
        public static double PsiFromTheta(double theta, double airDry, double ll15, double dul, double sat)
        {
            var x = new List<double>();
            var y = new List<double>();
            if (airDry < ll15)
            {
                x.Add(airDry);
                y.Add(Math.Log10(100));
            }
            x.Add(ll15);
            y.Add(Math.Log10(1.5));
            x.Add(dul);
            y.Add(Math.Log10(0.033));
            x.Add(sat);
            y.Add(Math.Log10(0.001));

            double logPsi = MathUtilities.LinearInterpReal(theta, x.ToArray(), y.ToArray(), out _);
            return -Math.Pow(10, logPsi);
        }
    }
}
