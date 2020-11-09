namespace APSIM.Shared.APSoil
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using APSIM.Shared.Utilities;

    /// <summary>
    /// A collection of PAWC methods.
    /// </summary>
    public class PAWC
    {
        /// <summary>Return the plant available water CAPACITY of the soil. Units: mm/mm</summary>
        /// <param name="soil">The soil to calculate PAWC for.</param>
        public static double[] OfSoil(Soil soil)
        {
            return PAWCInternal(soil.Water.Thickness, soil.Water.LL15, soil.Water.DUL, null);
        }

        /// <summary>Return the plant available water CAPACITY for the specified crop. Units: mm/mm</summary>
        /// <param name="soil">The soil to calculate PAWC for.</param>
        /// <param name="crop">The crop.</param>
        /// <returns></returns>
        public static double[] OfCrop(Soil soil, SoilCrop crop)
        {
            return PAWC.PAWCInternal(soil.Water.Thickness,
                        crop.LL,
                        soil.Water.DUL,
                        crop.XF);
        }

        /// <summary>Return the plant available water CAPACITY of the soil. Units: mm</summary>
        /// <param name="soil">The soil to calculate PAWC for.</param>
        public static double[] OfSoilmm(Soil soil)
        {
            double[] pawc = OfSoil(soil);
            return MathUtilities.Multiply(pawc, soil.Water.Thickness);
        }

        /// <summary>Return the plant available water CAPACITY for the specified crop. Units: mm</summary>
        /// <param name="soil">The soil to calculate PAWC for.</param>
        /// <param name="crop">The crop.</param>
        /// <returns></returns>
        public static double[] OfCropmm(Soil soil, SoilCrop crop)
        {
            double[] pawc = PAWC.OfCrop(soil, crop);
            return MathUtilities.Multiply(pawc, soil.Water.Thickness);
        }

        /// <summary>Return plant available water CAPACITY. Units: mm/mm</summary>
        /// <param name="Thickness">The thickness.</param>
        /// <param name="LL">The ll.</param>
        /// <param name="DUL">The dul.</param>
        /// <param name="XF">The xf.</param>
        /// <returns></returns>
        private static double[] PAWCInternal(double[] Thickness, double[] LL, double[] DUL, double[] XF)
        {
            double[] PAWC = new double[Thickness.Length];
            if (LL == null)
                return PAWC;
            if (Thickness.Length != DUL.Length || Thickness.Length != LL.Length)
                throw new Exception("Number of soil layers in Water is different to number of layers in Water.Crop");

            for (int layer = 0; layer != Thickness.Length; layer++)
                if (DUL[layer] == MathUtilities.MissingValue ||
                    LL[layer] == MathUtilities.MissingValue)
                    PAWC[layer] = 0;
                else
                    PAWC[layer] = Math.Max(DUL[layer] - LL[layer], 0.0);

            bool ZeroXFFound = false;
            for (int layer = 0; layer != Thickness.Length; layer++)
                if (ZeroXFFound || XF != null && XF[layer] == 0)
                {
                    ZeroXFFound = true;
                    PAWC[layer] = 0;
                }
            return PAWC;
        }
    }
}
