namespace Models.Interfaces
{
    using System;

    /// <summary>A weather interface.</summary>
    public interface IWeather
    {
        /// <summary>Gets the start date of the weather file.</summary>
        DateTime StartDate { get; }

        /// <summary>Gets the end date of the weather file.</summary>
        DateTime EndDate { get; }

        /// <summary>Gets or sets the maximum temperature (oc)</summary>
        double MaxT { get; }

        /// <summary>Gets or sets the minimum temperature (oc)</summary>
        double MinT { get; }

        /// <summary>Mean temperature  /// </summary>
        double MeanT { get; }

        /// <summary>Daily mean VPD  /// </summary>
        double VPD { get; }

        /// <summary>Gets or sets the rainfall (mm)</summary>
        double Rain { get; }

        /// <summary>Gets or sets the solar radiation. MJ/m2/day</summary>
        double Radn { get; }

        /// <summary>Gets or sets the vapor pressure</summary>
        double VP { get; }

        /// <summary>
        /// Gets or sets the wind value found in weather file or zero if not specified.
        /// </summary>
        double Wind { get; }

        /// <summary>
        /// Gets or sets the CO2 level. If not specified in the weather file the default is 350.
        /// </summary>
        double CO2 { get; }

        /// <summary>
        /// Gets or sets the atmospheric air pressure. If not specified in the weather file the default is 1010 hPa.
        /// </summary>
        double AirPressure { get; }

        /// <summary>Gets the latitude</summary>
        double Latitude { get; }

        /// <summary>Gets the average temperature</summary>
        double Tav { get; }

        /// <summary>Gets the temperature amplitude.</summary>
        double Amp { get; }

        /// <summary>
        /// Gets the duration of the day in hours.
        /// </summary>
        double CalculateDayLength(double Twilight);
    }
}
