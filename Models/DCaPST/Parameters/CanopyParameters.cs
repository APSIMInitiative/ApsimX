using Models.DCAPST.Interfaces;

namespace Models.DCAPST
{
    /// <summary>
    /// Implements the canopy parameters
    /// </summary>
    public class CanopyParameters : ICanopyParameters
    {
        /// <summary>
        /// 
        /// </summary>
        public CanopyType Type { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double AirO2 { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double AirCO2 { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double LeafAngle { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double LeafWidth { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double LeafScatteringCoeff { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double LeafScatteringCoeffNIR { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double DiffuseExtCoeff { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double DiffuseExtCoeffNIR { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double DiffuseReflectionCoeff { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double DiffuseReflectionCoeffNIR { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double Windspeed { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double WindSpeedExtinction { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double CurvatureFactor { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double DiffusivitySolubilityRatio { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double MinimumN { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public double SLNRatioTop { get; set; }
    }
}
