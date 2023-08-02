using System;
using Models.Core;

namespace Models.Functions
{
    /// <summary>
    /// Calculates the amount of time that the supplied thermal time was within the specified window as a fraction of the whole window length
    /// </summary>
    [Serializable]
    public class ThermalTimeWindowFunction : Model, IFunction
    {
        /// <summary>
        /// Start of Thermal Time Window
        /// </summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("degree days")]
        private IFunction ttWindowStart = null;

        /// <summary>
        /// End of Thermal Time Window
        /// </summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("degree days")]
        private IFunction ttWindowEnd = null;

        /// <summary>
        /// Current Thermal Time
        /// </summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("degree days")]
        private IFunction thermalTime = null;

        /// <summary>
        /// Today's Thermal Time
        /// </summary>
        [Link(Type = LinkType.Child, ByName = true)]
        [Units("degree days")]
        private IFunction dltThermalTime = null;

        private double windowLength(int arrayIndex = -1) => ttWindowEnd.Value(arrayIndex) - ttWindowStart.Value(arrayIndex);

        /// <summary>Gets the value.</summary>
        /// <value>The value.</value>
        public double Value(int arrayIndex = -1)
        {
            if (windowLength(arrayIndex) == 0) return 0;
            if (windowLength(arrayIndex) < 0) throw new Exception("ThermalTimeWindowFunction::WindowEnd must be greater than WindowStart");

            if (dltThermalTime.Value(arrayIndex) == 0) return 0;
            if (thermalTime.Value(arrayIndex) == 0) return 0;

            //check if it falls within the window
            if (thermalTime.Value(arrayIndex) < ttWindowStart.Value(arrayIndex)) return 0.0;
            if (thermalTime.Value(arrayIndex) - dltThermalTime.Value(arrayIndex) > ttWindowEnd.Value(arrayIndex)) return 0.0;

            //assume that the ThermalTime specified will include the dltThermalTime as well
            var todaysStart = Math.Max(thermalTime.Value(arrayIndex) - dltThermalTime.Value(arrayIndex), ttWindowStart.Value(arrayIndex));
            var todaysEnd = Math.Min(thermalTime.Value(arrayIndex), ttWindowEnd.Value(arrayIndex));
            var fraction = (todaysEnd - todaysStart) / windowLength(arrayIndex);
            return fraction;
        }
    }
}
