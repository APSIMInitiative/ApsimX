using System;
using APSIM.Numerics;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Soils;

namespace Models.Functions
{
    /// <summary>pH factor for daily nitrification of ammonium</summary>
    [Serializable]
    [Description("Nitrification Water Factor from CERES-Maize")]
    public class CERESNitrificationpHFactor : Model, IFunction
    {
        [Link]
        Chemical chemical = null;

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            if (arrayIndex == -1)
                throw new Exception("Layer number must be provided to CERES mineralisation water factor Model");
            double pHF = 0;

            if (chemical.PH[arrayIndex] < 4.5)
                pHF = 0;
            else if (chemical.PH[arrayIndex] < 6)
                pHF = MathUtilities.Divide(chemical.PH[arrayIndex] - 4.5, 6.0 - 4.5, 0);
            else if (chemical.PH[arrayIndex] < 8)
                pHF = 1;
            else if (chemical.PH[arrayIndex] < 9)
                pHF = 1 - MathUtilities.Divide(chemical.PH[arrayIndex] - 8.0, 9.0 - 8.0, 0.0);
            else
                pHF = 0;

            return pHF;
        }
    }
}