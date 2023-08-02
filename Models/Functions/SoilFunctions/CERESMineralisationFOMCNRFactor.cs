using System;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Soils.Nutrients;

namespace Models.Functions
{
    /// <summary>C:N Ratio factor for daily FOM Mineralisation</summary>
    /// \pre All children have to contain a public function "Value"
    /// \retval C:N Ratio factor for daily FOM Mineralisation
    [Serializable]
    [Description("C:N Ratio factor for daily FOM Mineralisation from CERES-Maize")]
    public class CERESMineralisationFOMCNRFactor : Model, IFunction
    {

        [Link]
        Nutrient nutrient = null;

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            if (arrayIndex == -1)
                throw new Exception("Layer number must be provided to CERES mineralisation water factor Model");

            double CNRF = Math.Exp(-0.693 * (nutrient.FOMCNRFactor[arrayIndex] - 25) / 25);
            return MathUtilities.Bound(CNRF, 0, 1);
        }
    }
}