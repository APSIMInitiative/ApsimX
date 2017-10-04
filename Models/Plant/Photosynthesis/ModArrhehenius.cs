using System;

namespace Models.PMF.Photosynthesis
{
    /// <summary></summary>
    public class ModArrhenius
    {
        /// <summary></summary>
        public ModArrhenius() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="temperature"></param>
        /// <param name="val25"></param>
        /// <param name="R"></param>
        /// <param name="Ea"></param>
        /// <param name="S"></param>
        /// <param name="H"></param>
        /// <returns></returns>
        public static double calc(double temperature, double val25, double R, double Ea, double S, double H)
        {
            double temperatureK = temperature + 273;

            return val25 * Math.Exp((temperatureK - 298) * Ea / (R * temperatureK * 298)) * (1 + Math.Exp((S * 298 - H) / (R * 298))) / (1 + Math.Exp((S * temperatureK - H) / (R * temperatureK)));
        }
    }
}