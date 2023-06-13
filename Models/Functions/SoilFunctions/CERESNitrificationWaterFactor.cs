using System;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Models.Soils;

namespace Models.Functions
{
    /// <summary>Water factor for daily nitrification of ammonium</summary>
    /// \pre All children have to contain a public function "Value"
    /// \retval Water factor for daily nitrification of ammonium
    [Serializable]
    [Description("Nitrification Water Factor from CERES-Maize")]
    public class CERESNitrificationWaterFactor : Model, IFunction
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
                throw new Exception("Layer number must be provided to CERES mineralisation water factor Model");
            double WF = 0;

            if (soilwater.SW[arrayIndex] < physical.LL15[arrayIndex])
                WF = 0;
            else if (soilwater.SW[arrayIndex] < physical.DUL[arrayIndex])
                WF = Math.Min(1, 4 * MathUtilities.Divide(soilwater.SW[arrayIndex] - physical.LL15[arrayIndex], physical.DUL[arrayIndex] - physical.LL15[arrayIndex], 0.0));
            else
                WF = 1 - MathUtilities.Divide(soilwater.SW[arrayIndex] - physical.DUL[arrayIndex], physical.SAT[arrayIndex] - physical.DUL[arrayIndex], 0.0);

            return WF;
        }
    }
}