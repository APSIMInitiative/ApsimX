using System;
using APSIM.Core;
using APSIM.Numerics;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;

namespace Models.Functions
{
    /// <summary>Temperature effect on denitrification</summary>
    /// \pre All children have to contain a public function "Value"
    /// \retval Temperature effect on denitrification.
    [Serializable]
    [Description("Soil NO3 Denitrification temperature factor from CERES-Maize")]
    public class CERESDenitrificationTemperatureFactor : Model, IFunction
    {

        [Link]
        ISoilTemperature soilTemperature = null;

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            if (arrayIndex == -1)
                throw new Exception("Layer number must be provided to CERES Denitrification Temperature Factor");

            return MathUtilities.Bound(0.1 * Math.Exp(0.046 * soilTemperature.Value[arrayIndex]), 0, 1);
        }

        /// <summary>
        /// Get the values for all soil layers.
        /// </summary>
        public double[] Values
        {
            get
            {
                if (soilTemperature?.Value == null)
                    return null;
                double[] result = new double[soilTemperature.Value.Length];
                for (int i = 0; i < result.Length; i++)
                    result[i] = Value(i);
                return result;
            }
        }
    }
}