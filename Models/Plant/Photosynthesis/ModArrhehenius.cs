using System;

namespace Models.PMF.Phenology
{
    public class ModArrhenius
    {
        public ModArrhenius() { }

        public static double calc(double temperature, double val25, double R, double Ea, double S, double H)
        {
            double temperatureK = temperature + 273;

            return val25 * Math.Exp((temperatureK - 298) * Ea / (R * temperatureK * 298)) * (1 + Math.Exp((S * 298 - H) / (R * 298))) / (1 + Math.Exp((S * temperatureK - H) / (R * temperatureK)));
        }
    }
}
