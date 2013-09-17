using System;
using Models.Core;

namespace Models.Soils
{
    public class InitialWater : Model
    {
        public enum PercentMethodEnum { FilledFromTop, EvenlyDistributed };
        public PercentMethodEnum PercentMethod { get; set; }
        public double FractionFull = double.NaN;
        public double DepthWetSoil = double.NaN;
        public string RelativeTo { get; set; }

        /// <summary>
        /// Method to set SW to a percent full.
        /// </summary>
        public void SetSW(double FractionFull, PercentMethodEnum PercentMethod)
        {
            this.FractionFull = FractionFull;
            this.PercentMethod = PercentMethod;
            this.DepthWetSoil = double.NaN;
        }

        /// <summary>
        /// Method to set SW to a depth of wet soil.
        /// </summary>
        public void SetSW(double DepthWetSoil)
        {
            this.DepthWetSoil = DepthWetSoil;
            this.FractionFull = double.NaN;
        }

        /// <summary>
        /// Calculate a layered soil water. Units: mm/mm
        /// </summary>
        internal double[] SW(double[] Thickness, double[] LL, double[] DUL, double[] XF)
        {
            if (double.IsNaN(DepthWetSoil))
            {
                if (PercentMethod == InitialWater.PercentMethodEnum.FilledFromTop)
                    return SWFilledFromTop(Thickness, LL, DUL, XF);
                else
                    return SWEvenlyDistributed(LL, DUL);
            }
            else
                return SWDepthWetSoil(Thickness, LL, DUL);
        }

        /// <summary>
        /// Calculate a layered soil water using a FractionFull and filled from the top. Units: mm/mm
        /// </summary>
        private double[] SWFilledFromTop(double[] Thickness, double[] LL, double[] DUL, double[] XF)
        {
            double[] SW = new double[Thickness.Length];
            if (Thickness.Length != LL.Length ||
                Thickness.Length != DUL.Length)
                return SW;
            double[] PAWCmm = Utility.Math.Multiply(Utility.Math.Subtract(DUL, LL), Thickness);

            double AmountWater = Utility.Math.Sum(PAWCmm) * FractionFull;
            for (int Layer = 0; Layer < LL.Length; Layer++)
            {
                if (AmountWater >= 0 && XF != null && XF[Layer] == 0)
                    SW[Layer] = LL[Layer];
                else if (AmountWater >= PAWCmm[Layer])
                {
                    SW[Layer] = DUL[Layer];
                    AmountWater = AmountWater - PAWCmm[Layer];
                }
                else
                {
                    double Prop = AmountWater / PAWCmm[Layer];
                    SW[Layer] = Prop * (DUL[Layer] - LL[Layer]) + LL[Layer];
                    AmountWater = 0;
                }
            }
            return SW;
        }

        /// <summary>
        /// Calculate a layered soil water using a FractionFull and evenly distributed. Units: mm/mm
        /// </summary>
        private double[] SWEvenlyDistributed(double[] LL, double[] DUL)
        {
            double[] SW = new double[LL.Length];
            for (int Layer = 0; Layer < LL.Length; Layer++)
                SW[Layer] = FractionFull * (DUL[Layer] - LL[Layer]) + LL[Layer];
            return SW;
        }

        /// <summary>
        /// Calculate a layered soil water using a depth of wet soil. Units: mm/mm
        /// </summary>
        private double[] SWDepthWetSoil(double[] Thickness, double[] LL, double[] DUL)
        {
            double[] SW = new double[LL.Length];
            double DepthSoFar = 0;
            for (int Layer = 0; Layer < Thickness.Length; Layer++)
            {
                if (DepthWetSoil > DepthSoFar + Thickness[Layer])
                    SW[Layer] = DUL[Layer];
                else
                {
                    double Prop = Math.Max(DepthWetSoil - DepthSoFar, 0) / Thickness[Layer];
                    SW[Layer] = Prop * (DUL[Layer] - LL[Layer]) + LL[Layer];
                }
                DepthSoFar += Thickness[Layer];
            }
            return SW;
        }
    }

}