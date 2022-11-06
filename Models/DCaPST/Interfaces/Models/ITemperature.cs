namespace Models.DCAPST.Interfaces
{
    /// <summary>
    /// Represents an object that models temperature across a day
    /// </summary>
    public interface ITemperature
    {
        /// <summary>
        /// Air pressure on location
        /// </summary>
        double AtmosphericPressure { get; }

        /// <summary>
        /// Air density on location in terms of mols
        /// </summary>
        double AirMolarDensity { get; }

        /// <summary>
        /// Current air temperature
        /// </summary>
        double AirTemperature { get; }

        /// <summary>
        /// Maximum daily temperature
        /// </summary>
        double MaxTemperature { get; }

        /// <summary>
        /// Minimum daily temperature
        /// </summary>
        double MinTemperature { get; }

        /// <summary>
        /// Sets the AirTemperature value based on the provided time
        /// </summary>
        void UpdateAirTemperature(double time);
    }
}
