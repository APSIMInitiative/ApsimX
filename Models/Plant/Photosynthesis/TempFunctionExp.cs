using System;

namespace Models.PMF.Photosynthesis
{
    /// <summary></summary>
    public class TempFunctionExp
    {
        /// <summary></summary>
        public TempFunctionExp() { }
    
        /// <summary>
        /// 
        /// </summary>
        /// <param name="temp"></param>
        /// <param name="P25"></param>
        /// <param name="c"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static double val(double temp,  double P25, double c, double b)
        {
            return P25 * Math.Exp(c - b / (temp + 273));
        }
    }
}
