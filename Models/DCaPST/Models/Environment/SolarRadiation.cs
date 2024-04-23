using System;
using Models.DCAPST.Interfaces;

namespace Models.DCAPST.Environment
{
    /// <summary>
    /// Models the different forms of environmental radiation
    /// </summary>
    public class SolarRadiation : ISolarRadiation
    {
        /// <summary>
        /// Models the solar geometry
        /// </summary>
        private ISolarGeometry solar;

        /// <summary>
        /// Fraction of incoming radiation that is diffuse
        /// </summary>
        private readonly double diffuseFraction = 0.1725;

        /// <summary>
        /// The radiation measured across a day
        /// </summary>
        public double Daily { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="solar"></param>
        public SolarRadiation(ISolarGeometry solar)
        {
            this.solar = solar;
        }

        /// <summary>
        /// PAR energy fraction
        /// </summary>
        public double RPAR { get; set; }

        /// <summary>
        /// The total incoming solar radiation over a time period
        /// </summary>
        public double Total { get; private set; }

        /// <summary>
        /// The amount of incoming direct solar radiation over a time period
        /// </summary>
        public double Direct { get; private set; }

        /// <summary>
        /// The amount of incoming diffuse solar radiation over a time period
        /// </summary>
        public double Diffuse { get; private set; }

        /// <summary>
        /// The amount of incoming direct photosynthetic active radiation over a time period
        /// </summary>
        public double DirectPAR { get; private set; }

        /// <summary>
        /// The amount of incoming diffuse photosynthetic active radiation over a time period
        /// </summary>
        public double DiffusePAR { get; private set; }

        /// <summary>
        /// Updates the incoming radiation values to a new time period 
        /// </summary>
        public void UpdateRadiationValues(double time)
        {
            if (time < 0 || 24 < time) throw new Exception("Time must be between 0 and 24");
            //if (RPAR <= 0) throw new Exception("RPAR must be greater than 0");
            //if (RPAR > 1) throw new Exception("RPAR must not exceed 1.0");

            Total = CurrentTotal(time);
            Diffuse = CurrentDiffuse(time);
            Direct = Total - Diffuse;

            // Photon count
            DiffusePAR = Diffuse * RPAR * 4.25 * 1E6;
            DirectPAR = Direct * RPAR * 4.56 * 1E6;
        }

        /// <summary>
        /// Finds the total radiation value at the current time
        /// </summary>
        private double CurrentTotal(double time)
        {
            double dawn = Math.Floor(solar.Sunrise);
            double dusk = Math.Ceiling(solar.Sunset);

            if (time < dawn || dusk < time) return 0;

            var theta = Math.PI * (time - solar.Sunrise) / solar.DayLength;
            var factor = Math.Sin(theta) * Math.PI / 2;

            // TODO: Adapt this to use the timestep model
            var radiation = Daily / (solar.DayLength * 3600);
            var incident = radiation * factor;

            if (incident < 0) return 0;

            return incident;
        }

        /// <summary>
        /// Finds the diffuse radiation value at the current time
        /// </summary>
        private double CurrentDiffuse(double time)
        {
            var diffuse = Math.Max(diffuseFraction * solar.SolarConstant * Math.Sin(solar.SunAngle(time)) / 1000000, 0);

            if (diffuse > Total)
                return Total;
            else
                return diffuse;
        }
    }
}
