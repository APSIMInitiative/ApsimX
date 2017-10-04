using System;

namespace Models.PMF.Photosynthesis
{
    /// <summary></summary>
    public class TempFunctionNormal
    {
        /// <summary></summary>
        public TempFunctionNormal() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="temp"></param>
        /// <param name="P25"></param>
        /// <param name="TOpt"></param>
        /// <param name="Omega"></param>
        /// <returns></returns>
        public static double val(double temp, double P25, double TOpt, double Omega)
        {
            return P25 * Math.Exp(-1 * (Math.Pow((temp - TOpt) / Omega, 2)) + (Math.Pow((25 - TOpt) / Omega, 2)));
        }
    }
}