using System;
using APSIM.Core;
using Models.Core;
using Models.Interfaces;

namespace Models.Functions
{
    /// <summary>Temperature effect on P Availability Loss</summary>
    /// \pre All children have to contain a public function "Value"
    /// \retval Temperature effect on denitrification.
    [Serializable]
    [Description("Soil P Availability Loss Temperature factor from Barrow")]
    public class PAvailabilityLossTemperatureFactor : Model, IFunction
    {

        [Link]
        ISoilTemperature soilTemperature = null;

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            if (arrayIndex == -1)
                throw new Exception("Layer number must be provided to P Availability Loss Temperature Factor");

            double ActivationEnergy = 90000;  //J/mole
            return Math.Exp(ActivationEnergy / 8.314 * ((1.0 / 298.0) - (1.0 / (273.0 + soilTemperature.Value[arrayIndex]))));
        }
    }
}