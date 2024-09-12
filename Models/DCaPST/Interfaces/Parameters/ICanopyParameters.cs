namespace Models.DCAPST.Interfaces
{
    /// <summary>
    /// Possible canopy types
    /// </summary>
    public enum CanopyType
    {
        /// <summary>
        /// 
        /// </summary> 
        C3,

        /// <summary>
        ///
        /// </summary>
        C4,

        /// <summary>
        ///
        /// </summary>
        CCM
    }

    /// <summary>
    /// Describes parameters used by a crop canopy to calculate photosynthesis
    /// </summary>
    public interface ICanopyParameters
    {
        /// <summary>
        /// The type of canopy
        /// </summary>
        CanopyType Type { get; set; }

        ///// <summary>
        ///// Parameters used in modelling an assimilation pathway
        ///// </summary>
        //IPathwayParameters Pathway { get; set; }

        /// <summary>
        /// Partial pressure of CO2 in air (microbar)
        /// </summary>
        double AirCO2 { get; set; }

        /// <summary>
        /// Partial pressure of O2 in air (microbar)
        /// </summary>
        double AirO2 { get; set; }

        /// <summary>
        /// Empirical curvature factor
        /// </summary>
        double CurvatureFactor { get; set; }

        /// <summary>
        /// The ratio of diffusivity to solubility
        /// </summary>
        double DiffusivitySolubilityRatio { get; set; }
        
        /// <summary>
        /// The minimum nitrogen value at or below which CO2 assimilation rate is zero (mmol N m^-2)
        /// </summary>
        double MinimumN { get; set; }
        
        /// <summary>
        /// Ratio of the average canopy specific leaf nitrogen (SLN) to the SLN at the top of canopy (g N m^-2 leaf)
        /// </summary>
        double SLNRatioTop { get; set; }

        /// <summary>
        /// Canopy-average leaf inclination relative to the horizontal (degrees)
        /// </summary>
        double LeafAngle { get; set; }

        /// <summary>
        /// The leaf width in the canopy
        /// </summary>
        double LeafWidth { get; set; }

        /// <summary>
        /// Leaf-level coefficient of scattering radiation
        /// </summary>
        double LeafScatteringCoeff { get; set; }

        /// <summary>
        /// Leaf-level coefficient of near-infrared scattering radiation
        /// </summary>
        double LeafScatteringCoeffNIR { get; set; }

        /// <summary>
        /// Extinction coefficient for diffuse radiation
        /// </summary>
        double DiffuseExtCoeff { get; set; }

        /// <summary>
        /// Used to reduce the ExtCoeff based on LAI
        /// </summary>
        double ExtCoeffReductionSlope { get; set; }

        /// <summary>
        /// Used to reduce the ExtCoeff based on LAI
        /// </summary>
        double ExtCoeffReductionIntercept { get; set; }

        /// <summary>
        /// Extinction coefficient for near-infrared diffuse radiation
        /// </summary>
        double DiffuseExtCoeffNIR { get; set; }

        /// <summary>
        /// Reflection coefficient for diffuse radiation
        /// </summary>
        double DiffuseReflectionCoeff { get; set; }

        /// <summary>
        /// Reflection coefficient for near-infrared diffuse radiation
        /// </summary>
        double DiffuseReflectionCoeffNIR { get; set; }

        /// <summary>
        /// Local wind speed (m/s)
        /// </summary>
        double Windspeed { get; set; }

        /// <summary>
        /// Extinction coefficient for local wind speed
        /// </summary>
        double WindSpeedExtinction { get; set; }
    }
}
