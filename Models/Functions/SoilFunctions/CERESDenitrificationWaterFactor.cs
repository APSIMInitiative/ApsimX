using System;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Models.Soils;

namespace Models.Functions
{
    /// <summary>Water effect on denitrification</summary>
    /// \pre All children have to contain a public function "Value"
    /// \retval Water effect on denitrification.
    [Serializable]
    [Description("Soil NO3 Denitrification water factor from CERES-Maize")]
    public class CERESDenitrificationWaterFactor : Model, IFunction
    {

        [Link]
        ISoilWater soilwater = null;
        [Link]
        IPhysical physical = null;

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            if (arrayIndex == -1)
                throw new Exception("Layer number must be provided to CERES Denitrification Water Factor Model");

            double WF = MathUtilities.Divide(soilwater.SW[arrayIndex] - physical.DUL[arrayIndex], physical.SAT[arrayIndex] - physical.DUL[arrayIndex], 0.0);
            return MathUtilities.Bound(WF, 0, 1);
        }

        /// <summary>
        /// Get the values for all soil layers.
        /// </summary>
        public double[] Values
        {
            get
            {
                if (soilwater == null)
                    return null;
                double[] result = new double[soilwater.SW.Length];
                for (int i = 0; i < result.Length; i++)
                    result[i] = Value(i);
                return result;
            }
        }
    }
}