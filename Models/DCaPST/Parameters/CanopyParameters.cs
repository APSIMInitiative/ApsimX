using System;
using Models.Core;
using Models.DCAPST.Interfaces;

namespace Models.DCAPST
{
    /// <summary>
    /// Implements the canopy parameters
    /// </summary>
    [Serializable]
    public class CanopyParameters : ICanopyParameters
    {
        /// <summary>
        /// Canopy type.
        /// </summary>
        [Description("Canopy Type")]
        public CanopyType Type { get; set; }

        /// <summary>
        /// Partial pressure of O2 in air.
        /// </summary>
        [Description("Partial pressure of O2 in air")]
        [Units("μbar")]
        public double AirO2 { get; set; }

        /// <summary>
        /// Partial pressure of CO2 in air
        /// </summary>
        [Description("Partial pressure of CO2 in air")]
        [Units("μbar")]
        public double AirCO2 { get; set; }

        /// <summary>
        /// Canopy average leaf inclination relative to the horizontal (degrees)
        /// </summary>
        [Description("Average leaf angle (relative to horizontal)")]
        [Units("Degrees")]
        public double LeafAngle { get; set; }

        /// <summary>
        /// The leaf width in the canopy
        /// </summary>
        [Description("Average leaf width")]
        [Units("")]
        public double LeafWidth { get; set; }

        /// <summary>
        /// Leaf-level coefficient of scattering radiation
        /// </summary>
        [Description("Leaf-level coefficient of scattering radiation")]
        [Units("")]
        public double LeafScatteringCoeff { get; set; }

        /// <summary>
        /// Leaf-level coefficient of near-infrared scattering radiation
        /// </summary>
        [Description("Leaf-level coefficient of scattering NIR")]
        [Units("")]
        public double LeafScatteringCoeffNIR { get; set; }

        /// <summary>
        /// Extinction coefficient for diffuse radiation
        /// </summary>
        [Description("Diffuse radiation extinction coefficient")]
        [Units("")]
        public double DiffuseExtCoeff { get; set; }


        /// <summary>
        /// Used to reduce the ExtCoeff based on LAI
        /// </summary>
        [Description("Extinction coefficient reduction slope")]
        [Units("")]
        public double ExtCoeffReductionSlope { get; set; }

        /// <summary>
        /// Used to reduce the ExtCoeff based on LAI
        /// </summary>
        [Description("Extinction coefficient reduction intercept")]
        [Units("")]
        public double ExtCoeffReductionIntercept { get; set; }

        /// <summary>
        /// Extinction coefficient for near-infrared diffuse radiation
        /// </summary>
        [Description("Diffuse NIR extinction coefficient")]
        [Units("")]
        public double DiffuseExtCoeffNIR { get; set; }

        /// <summary>
        /// Reflection coefficient for diffuse radiation
        /// </summary>
        [Description("Diffuse radiation reflection coefficient")]
        [Units("")]
        public double DiffuseReflectionCoeff { get; set; }

        /// <summary>
        /// Reflection coefficient for near-infrared diffuse radiation
        /// </summary>
        [Description("Diffuse NIR reflection coefficient")]
        [Units("")]
        public double DiffuseReflectionCoeffNIR { get; set; }

        /// <summary>
        /// Local wind speed
        /// </summary>
        [Description("Local wind speed")]
        [Units("")]
        public double Windspeed { get; set; }

        /// <summary>
        /// Extinction coefficient for local wind speed
        /// </summary>
        [Description("Wind speed extinction coefficient")]
        [Units("")]
        public double WindSpeedExtinction { get; set; }

        /// <summary>
        /// Empirical curvature factor
        /// </summary>
        [Description("Empirical curvature factor")]
        [Units("")]
        public double CurvatureFactor { get; set; }

        /// <inheritdoc />
        [Description("Diffusivity solubility ratio")]
        [Units("")]
        public double DiffusivitySolubilityRatio { get; set; }

        /// <summary>
        /// The minimum nitrogen value at or below which CO2 assimilation rate is zero (mmol N m^-2)
        /// </summary>
        [Description("Minimum nitrogen for assimilation")]
        [Units("")]
        public double MinimumN { get; set; }

        /// <summary>
        /// Ratio of the average canopy specific leaf nitrogen (SLN) to the SLN at the top of canopy (g N m^-2 leaf)
        /// </summary>
        [Description("Ratio of average SLN to canopy top SLN")]
        [Units("")]
        public double SLNRatioTop { get; set; }
    }
}
