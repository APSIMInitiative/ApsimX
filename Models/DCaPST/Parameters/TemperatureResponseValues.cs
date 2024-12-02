using Models.Core;
using System;

namespace Models.DCAPST
{
    /// <summary>
    /// Describes a temperature response.
    /// </summary>
    [Serializable]
    public struct TemperatureResponseValues
    {
        /// <summary>
        /// The value of the temperature response factor for a given parameter
        /// </summary>
        [Description("The value of the temperature response factor for a given parameter")]
        public double Factor;

        /// <summary>
        /// The value of the temperature response factor at 25 degrees
        /// </summary>
        [Description("The value of the temperature response factor at 25 degrees")]
        [Units("")]
        public double At25;
    }
}
