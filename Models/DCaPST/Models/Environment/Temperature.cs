using System;
using Models.DCAPST.Interfaces;

namespace Models.DCAPST.Environment
{
    /// <summary>
    /// Models the environmental temperature
    /// </summary>
    public class Temperature : ITemperature
    {
        /// <summary>
        /// The solar geometry
        /// </summary>
        private ISolarGeometry solar;

        /// <summary>
        /// The atmospheric pressure
        /// </summary>
        public double AtmosphericPressure { get; set; } = 1.01325;
        
        /// <summary>
        /// The daily maximum temperature
        /// </summary>
        public double MaxTemperature { get; set; }

        /// <summary>
        /// The daily minimum temperature
        /// </summary>
        public double MinTemperature { get; set; }

        /// <summary>
        /// Maximum temperature lag coefficient
        /// </summary>
        public double XLag { get; set; } = 1.8;

        /// <summary>
        /// Night time temperature lag coefficient
        /// </summary>
        public double YLag { get; set; } = 2.2;

        /// <summary>
        /// Minimum temperature lag coefficient
        /// </summary>
        public double ZLag { get; set; } = 1;

        /// <summary>
        /// The current air temperature
        /// </summary>
        public double AirTemperature { get; set; }

        /// <summary>
        /// Air density in mols
        /// </summary>
        public double AirMolarDensity
        {
            get
            {
                var atm_to_Pa = 100000;
                var pressure = AtmosphericPressure * atm_to_Pa;

                var molarMassAir = 28.966;

                var kg_to_g = 1000;

                var specificHeat = 287;
                var absolute0C = 273;

                var numerator = pressure * kg_to_g / molarMassAir;
                var denominator = specificHeat * (AirTemperature + absolute0C);

                return numerator / denominator;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="solar"></param>
        public Temperature(ISolarGeometry solar)
        {
            this.solar = solar;
        }

        /// <summary>
        /// Calculates the air temperature based on the current time
        /// </summary>
        public void UpdateAirTemperature(double time)
        {
            if (time < 0 || 24 < time) throw new Exception("The time must be between 0 and 24");

            double timeOfMinT = 12.0 - solar.DayLength / 2.0 + ZLag;
            double deltaT = MaxTemperature - MinTemperature;

            if /*DAY*/ (timeOfMinT < time && time < solar.Sunset)
            {
                double m = time - timeOfMinT;
                AirTemperature = deltaT * Math.Sin((Math.PI * m) / (solar.DayLength + 2 * XLag)) + MinTemperature;
            }
            else /*NIGHT*/
            {
                double n = time - solar.Sunset;
                if (n < 0) n += 24;

                double tempChange = deltaT * Math.Sin(Math.PI * (solar.DayLength - ZLag) / (solar.DayLength + 2 * XLag));
                AirTemperature = MinTemperature + tempChange * Math.Exp(-YLag * n / (24.0 - solar.DayLength));
            }
        }
    }
}
