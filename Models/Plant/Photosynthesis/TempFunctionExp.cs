using System;

namespace Models.PMF.Phenology
{
    public class TempFunctionExp
    {
        public TempFunctionExp() { }

        public static double val(double temp,  double P25, double c, double b)
        {
            return P25 * Math.Exp(c - b / (temp + 273));
        }
    }
}
