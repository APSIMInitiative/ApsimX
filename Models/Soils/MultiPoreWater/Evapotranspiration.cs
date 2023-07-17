using System;
using Models.Core;

namespace Models.Soils
{
    /// <summary>
    /// Calculates Penman Evaporation to drive potential evaporation from soil model
    /// </summary>
    [Serializable]
    [ValidParent(ParentType = typeof(Soil))]
    public class Evapotranspiration : Model
    {
        /// <summary>Penman Evapotranspiration potential (mm/day)
        ///The amount of water that will be transpired by a short, actively growing area of crop that is
        ///fully covering the ground.This is the formulation given by French BK, Legg BJ. 1979. 
        ///Rothamsted irrigation 1964-76. Journal of Agricultural Science, U.K, 92: 15-37.
        /// </summary>
        ///<param name="Radiation"> is total incomming solar radiaion (Units MJ/M2/day) </param>
        ///<param name="Temperature">the mean air temperature for the day measured in a stevenson screen at 1.2m height(Units Degrees celcius)</param> 
        ///<param name ="Windrun"> is the distance the wind would travel in a day at is mean speed(units km/d)</param>
        ///<param name = "VaporPressure"> is the vapor pressure of the air at 1.2m height(units mbar) </param>
        /// <param name="Albedo">the proportion of radiation reflected by the surface</param>
        /// <param name="DayOfYear"></param>
        /// <param name="Lattitude"></param>
        public double PenmanEO(double Radiation, double Temperature, double Windrun, double VaporPressure, double Albedo, double Lattitude, double DayOfYear)
        {
            double D = SatVaporPressureSlope(Temperature) * 10; //Function returns kPa, * 10 to convert to hPa
            double l = lamda(Temperature);
            double G = gama(Temperature) * 10;  //Function returns kPa, * 10 to convert to hPa
            double p = AirDensity(Temperature);
            double Rad = NetRadiation(Radiation, Temperature, VaporPressure / 10, Lattitude, DayOfYear, Albedo); //Function uses VP in kPa, /10 to convert from hPa
            double VPD = VaporPressureDeficit(Temperature, VaporPressure); //This function returns hPa
            double Ea = 0.27 * VPD * (1 + Windrun / 160);
            return (D * Rad / l + G * Ea) / (D + G);
        }
        ///<summary>latent heat of vapourisation (MJ/kg) 
        /// ET calculations solve an energy balance to work out how much energy is being removed from the system
        /// by evaporation.This is given by the latent heat of vapourisation.
        /// We need to divide latent heat flux(MJ) by LAMDA to convert to mm of water evaporated
        ///</summary>
        ///<param name="Temperature"> Air temperature(units degrees C)</param>
        public double lamda(double Temperature) { return 2.5 - 0.002365 * Temperature; }

        ///<summary>The phycometric constant (kPa/oK)</summary>
        ///<param name = "Temperature">air temperature(units degrees C)</param>
        public double gama(double Temperature)
        {
            double Cp = 0.001013;
            double p = 101;
            double e = 0.622;
            double l = lamda(Temperature);
            return (Cp * p) / (e * l);
        }
        ///<summary> Slope of the saturated vapor pressure line at give temperature (kPa).</summary>
        public double SatVaporPressureSlope(double Temperature)
        {
            return (4098 * 0.6108 * Math.Exp((17.27 * Temperature) / (Temperature + 237.3))) / Math.Pow(Temperature + 237.3, 2);
        }
        ///<summary>This is the difference (in mbar) between the current vapour presure and the saturated vapor pressure
        ///at the current air temperature</summary>
        ///<param name="Temperature">Temperature is Air temperature(units degrees C)</param>
        ///<param name="VaporPressure">Vapor pressure in mbar</param>
        public double VaporPressureDeficit(double Temperature, double VaporPressure)
        {
            double saturated_vp = SatVaporPressure(Temperature);
            return saturated_vp - VaporPressure;
        }
        ///<summary>This is the vapour pressure (in mbar) that the airs capacity to absorb water vapor is saturated.
        ///It increases exponentially with temperature.The equation used here is from:
        ///Jenson ME, Burman RD, Allen RG. 1990.Evapotranspiration and irrigation requirements: a manual. 
        ///New York, U.S.A: American Society of Civil Engineers.
        ///</summary>
        ///<param name = "Temperature"> temperature of the air (units degrees C)</param>
        public double SatVaporPressure(double Temperature)
        {
            return 6.11 * Math.Exp((17.27 * Temperature) / (Temperature + 237.3));
        }

        /// <summary>
        /// "Density of air (kg/m3)
        /// </summary>
        /// <param name="Temperature">temperature of the air (units degrees C) </param>
        /// <returns></returns>
        public double AirDensity(double Temperature)
        {
            double p = 101;
            double GC = 287;
            return (1000 * p) / (GC * 1.01 * (273.16 + Temperature));
        }
        /// <summary>Net solar radiation (MJ/m2) at the crop surface.
        ///This is total incomming radiation less that which is reflected.
        ///Reference: ASCE-EWRI. 2005. The ASCE Standardized Reference Evapotranspiration Equation.
        ///Report of the Task Committee on Standardization of Reference Evapotranspiration.
        /// </summary>
        /// <param name="Radiation">is the total incomming solar radiation measured by a pyranometer for the period (Units MJ/m2)</param>
        /// <param name="Tmean">is the mean temperature for the period measured in a Stevenson screen at 1.2 m height(degrees C)</param>
        /// <param name="VapourPressure">is the mean vapor pressure for the period measured in a Stevenson screen at 1.2 m height(Units kPa)</param>
        /// <param name="Lattitude">(units degrees)</param>
        /// <param name="DOY">is day of year 1 Jan = 1</param>
        /// <param name="albedo">is the proportion of radiation that the surface reflects back to the sky</param>
        /// <returns></returns>
        public double NetRadiation(double Radiation, double Tmean, double VapourPressure, double Lattitude, double DOY, double albedo)
        {
            double RShortWave = 0;
            double RLongWave = 0;
            if (Radiation > 0)
            {
                RShortWave = (1 - albedo) * Radiation;
                RLongWave = Math.Max(0, NetLongwaveRadiation(Radiation, Tmean, VapourPressure, Lattitude, DOY));
            }
            if ((RShortWave > 0) && (RLongWave > 0))
                return RShortWave - RLongWave;
            else
                return 0;
        }

        /// <summary>
        /// Radiation assuming no cloud cover
        /// </summary>
        /// <param name="Lattitude">(units degrees)</param>
        /// <param name="DOY"> is day of year 1 Jan = 1</param>
        public double ClearSkyRadiation(double Lattitude, double DOY)
        {
            return (0.75 + 0.00002 * 17) * ExtraterestialRadiation(Lattitude, DOY);
        }
        /// <summary>
        /// Solar radiation absorbed by the crop and lost again to the atmosphere and space by longwave radiation
        /// </summary>
        /// <param name="Radiation"> is the total incomming solar radiation measured by a pyranometer for the period(Units MJ/m2)</param>
        /// <param name="Tmean"> is the mean temperature for the period measured in a Stevenson screen at 1.2 m height(degrees C)</param>
        /// <param name="VapourPressure"> is the mean vapor pressure for the period measured in a Stevenson screen at 1.2 m height(Units kPa)</param>
        /// <param name="Lattitude">(units degrees)</param>
        /// <param name="DOY"> is day of year 1 Jan = 1</param>
        public double NetLongwaveRadiation(double Radiation, double Tmean, double VapourPressure, double Lattitude, double DOY)
        {
            double RClearSky = ClearSkyRadiation(Lattitude, DOY);
            if (RClearSky > 0)
            {
                double sb = 0.000000004903;
                double a = Math.Pow((Tmean + 273.16), 4);
                double b = 0.34 - 0.14 * Math.Pow(VapourPressure, 0.5);
                double SSoRatio = Radiation / RClearSky;
                if (SSoRatio > 1)
                    SSoRatio = 1;
                double c = 1.35 * SSoRatio - 0.35;
                if (c < 0.05)
                    c = 0.05;
                if (c > 1)
                    c = 1;
                return sb * a * b * c;
            }
            else
                return 0;
        }
        /// <summary>
        /// Radiation at the top of the atmosphere
        /// </summary>
        /// <param name="Lattitude">(units degrees)</param>
        /// <param name="DOY"> is day of year 1 Jan = 1</param>
        public double ExtraterestialRadiation(double Lattitude, double DOY)
        {
            double DR = InverseRelativeDistance(DOY);
            double SD = SolarDecimation(DOY);
            double Lat = Lattitude * Math.PI / 180;
            double SH = SunsetHourAngel(DOY, Lattitude);
            return (24 / Math.PI) * 4.92 * DR * (SH * Math.Sin(Lat) * Math.Sin(SD) + Math.Cos(Lat) * Math.Cos(SD) * Math.Sin(SH));
        }
        double InverseRelativeDistance(double DOY) { return 1 + 0.033 * Math.Cos((2 * Math.PI) / 365 * DOY); }

        double SunsetHourAngel(double DOY, double Lattitude)
        {
            double Lat = Lattitude * Math.PI / 180;
            return Math.Acos(-Math.Tan(Lat) * Math.Tan(SolarDecimation(DOY)));
        }

        double SolarDecimation(double DOY) { return 0.409 * Math.Sin((2 * Math.PI) / 365 * DOY - 1.39); }
    }
}
