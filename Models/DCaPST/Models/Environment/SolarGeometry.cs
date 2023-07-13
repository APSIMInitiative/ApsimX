using System;
using Models.DCAPST.Interfaces;

namespace Models.DCAPST.Environment
{
    /// <summary>
    /// Models the position of the sun
    /// </summary>
    public class SolarGeometry : ISolarGeometry
    {
        /// <summary>
        /// Geographic latitude (radians)
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// The angle between the solar disk and the equatorial plane
        /// </summary>
        public double SolarDeclination { get; private set; }

        /// <summary>
        /// Angle of the sun at sunset (radians)
        /// </summary>
        public double SunsetAngle { get; private set; }

        /// <summary>
        /// The rate at which the suns energy reaches the earth
        /// </summary>
        public double SolarConstant { get; } = 1360;

        /// <summary>
        /// Day of the year
        /// </summary>
        public double DayOfYear { get; set; }

        /// <summary>
        /// Time the sun is in the sky (hours)
        /// </summary>
        public double DayLength { get; private set; }

        /// <summary>
        /// Time of sunrise (hours)
        /// </summary>
        public double Sunrise { get; private set; }

        /// <summary>
        /// Time of sunset (hours)
        /// </summary>
        public double Sunset { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public SolarGeometry()
        { }

        /// <summary>
        /// Initialise the solar geometry model
        /// </summary>
        public void Initialise()
        {
            SolarDeclination = CalcSolarDeclination();
            SunsetAngle = CalcSunsetAngle();
            DayLength = 2 * SunsetAngle.ToDegrees() / 15;
            Sunrise = 12.0 - DayLength / 2.0;
            Sunset = 12.0 + DayLength / 2.0;
        }

        /// <summary>
        /// Calculates the solar declination angle (radians)
        /// </summary>
        private double CalcSolarDeclination() => 23.45.ToRadians() * Math.Sin(2 * Math.PI * (284 + DayOfYear) / 365);

        /// <summary>
        /// Calculates the angle of the sun at sunset
        /// </summary>
        private double CalcSunsetAngle() => Math.Acos(-1 * Math.Tan(Latitude) * Math.Tan(SolarDeclination));

        /// <summary>
        /// Calculates the angle of the sun in the sky (radians)
        /// </summary>
        /// <param name="hour">The time in hours</param>        
        public double SunAngle(double hour)
        {
            var angle = Math.Asin(Math.Sin(Latitude) * Math.Sin(SolarDeclination)
                + Math.Cos(Latitude)
                * Math.Cos(SolarDeclination)
                * Math.Cos(Math.PI / 12.0 * DayLength * (((hour - Sunrise) / DayLength) - 0.5)));
            return angle;
        }

    }
}
