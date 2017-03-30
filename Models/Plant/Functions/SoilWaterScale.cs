using System;
using System.Collections.Generic;
using Models.Core;
using Models.Soils;
using APSIM.Shared.Utilities;

namespace Models.PMF.Functions
{
    /// <summary>
    /// A simple scale to convert soil water content into a value between 0 and 2
    /// </summary>
    [Serializable]
    public class SoilWaterScale : Model, IFunction
    {
        [Link]
        Soil Soil = null;

        /// <summary>Gets the value of the function.</summary>
        public double Value(int arrayIndex = -1)
        {
            // temporary water factor (0-1)
            double wfd;
            if (Soil.SoilWater.SWmm[arrayIndex] > Soil.SoilWater.DULmm[arrayIndex])
            { // saturated
                wfd = Math.Max(1.0, Math.Min(2.0, 1.0 +
                    MathUtilities.Divide(Soil.SoilWater.SWmm[arrayIndex] - Soil.SoilWater.DULmm[arrayIndex], Soil.SoilWater.SATmm[arrayIndex] - Soil.SoilWater.DULmm[arrayIndex], 0.0)));
            }
            else
            { // unsaturated
              // assumes rate of mineralisation is at optimum rate until soil moisture midway between dul and ll15
                wfd = Math.Max(0.0, Math.Min(1.0, MathUtilities.Divide(Soil.SoilWater.SWmm[arrayIndex] - Soil.SoilWater.LL15mm[arrayIndex], Soil.SoilWater.DULmm[arrayIndex] - Soil.SoilWater.LL15mm[arrayIndex], 0.0)));
            }

            return wfd;
        }

    }
}