using System;
using APSIM.Numerics;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Models.Soils;

namespace Models.Functions
{
    /// <summary>Fraction of urea that hydrolyses per day</summary>
    /// \pre All children have to contain a public function "Value"
    /// \retval fraction of Urea hydrolysed.
    [Serializable]
    [Description("Urea hydrolysis model from CERES-Maize")]
    public class CERESUreaHydrolysisModel : Model, IFunction
    {
        [Link]
        Organic organic = null;

        [Link]
        Chemical chemical = null;

        [Link]
        ISoilTemperature soilTemperature = null;

        [Link(Type = LinkType.Child)]
        CERESMineralisationWaterFactor CERESWF = null;


        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            if (arrayIndex == -1)
                throw new Exception("Layer number must be provided to CERES Urea Hydrolysis Model");

            double potentialRate = -1.12 + 1.31 * organic.Carbon[arrayIndex] + 0.203 * chemical.PH[arrayIndex] - 0.155 * organic.Carbon[arrayIndex] * chemical.PH[arrayIndex];
            potentialRate = MathUtilities.Bound(potentialRate, 0, 1);

            double WF = MathUtilities.Bound(CERESWF.Value(arrayIndex) + 0.2, 0, 1);
            double TF = MathUtilities.Bound(soilTemperature.Value[arrayIndex] / 40 + 0.2, 0, 1);
            double rateModifer = Math.Min(WF, TF);

            return potentialRate * rateModifer;
        }
    }
}