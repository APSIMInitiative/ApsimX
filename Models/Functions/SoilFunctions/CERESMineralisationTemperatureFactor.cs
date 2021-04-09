using System;
using System.Collections.Generic;
using Models.Core;
using Models.Interfaces;

namespace Models.Functions
{
    /// <summary>Fraction of NH4 which nitrifies today</summary>
    /// \pre All children have to contain a public function "Value"
    /// \retval fraction of NH4 nitrified.
    [Serializable]
    [Description("Mineralisation Temperature Factor from CERES-Maize")]
    public class CERESMineralisationTemperatureFactor : Model, IFunction
    {

        [Link]
        ISoilTemperature soiltemperature = null;

   
        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            if (arrayIndex == -1)
                throw new Exception("Layer number must be provided to CERES mineralisation temperature factor Model");

            double TF = 0;
            double ST = soiltemperature.Value[arrayIndex];

            if (ST > 0)
                TF = (ST * ST) / (32 * 32);
            if (TF > 1) TF = 1;

            return TF;
        }
    }
}