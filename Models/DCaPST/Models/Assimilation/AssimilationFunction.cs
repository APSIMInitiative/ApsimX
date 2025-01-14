using System;

namespace Models.DCAPST
{
    /// <summary>
    /// Manages the calculation of CO2 assimilation rate
    /// </summary>
    /// <remarks>
    /// See the supplementary material from the paper
    /// "Simulating daily field crop canopy photosynthesis: an integrated software package",
    /// by A. Wu et al (2018) for the theory behind this function.
    /// 
    /// Note that some adjustments have been made to account for the CCM model, 
    /// which as of Feb 2020 has not been published.
    /// </remarks>
    public class AssimilationFunction
    {
        /// <summary>
        /// Function terms
        /// </summary>
        public Terms x;

        /// <summary>
        /// Intercellular CO2
        /// </summary>
        public double Ci;

        /// <summary>
        /// Mesophyll resistance
        /// </summary>
        public double Rm;

        /// <summary>
        /// Mesophyll respiration
        /// </summary>
        public double MesophyllRespiration;

        /// <summary>
        ///  The bundle sheath conductance
        /// </summary>
        public double BundleSheathConductance;

        /// <summary>
        /// Leaf respiration
        /// </summary>
        public double Respiration;

        /// <summary>
        /// The quadratic equation
        /// </summary>
        private static double SolveQuadratic(double a, double b, double c)
        {
            var root = b * b - 4 * a * c;
            return (-b - Math.Sqrt(root)) / (2 * a);
        }

        /// <summary>
        /// 
        /// </summary>
        public double Value()
        {
            double R_m = MesophyllRespiration;
            double gbs = BundleSheathConductance;
            double R_d = Respiration;

            double p = Ci;
            double q = Rm;

            var n1 = R_d - x._1;
            var n2 = p * x._3 + x._4;
            var n3 = q * x._3 + x._5;

            var a = gbs * (q - x._9) + n3 * x._6;

            var c1 = -p * n1 - R_d * x._2 - x._1 * x._8;
            var c2 = n1 * (R_m - n2);
            var c = gbs * c1 + x._6 * c2;

            var b1 = q * n1 - R_d * x._9 - x._1 * x._7 - x._2 - p;
            var b2 = R_m + n1 * n3 - n2;
            var b = gbs * b1 + x._6 * b2;

            return SolveQuadratic(a, b, c);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public struct Terms
    {
        /// <summary>
        /// 
        /// </summary>
        public double _1;

        /// <summary>
        /// 
        /// </summary>
        public double _2;

        /// <summary>
        /// 
        /// </summary>
        public double _3;

        /// <summary>
        /// 
        /// </summary>
        public double _4;

        /// <summary>
        /// 
        /// </summary>
        public double _5;

        /// <summary>
        /// 
        /// </summary>
        public double _6;

        /// <summary>
        /// 
        /// </summary>
        public double _7;

        /// <summary>
        /// 
        /// </summary>
        public double _8;

        /// <summary>
        /// 
        /// </summary>
        public double _9;
    }
}