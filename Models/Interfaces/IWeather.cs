namespace Models.Interfaces
{
    using Models.Core;
    using System;

    /// <summary>A weather interface.</summary>
    public interface IWeather
    {
        /// <summary>Gets the start date of the weather file.</summary>
        DateTime StartDate { get; }

        /// <summary>Gets the end date of the weather file.</summary>
        DateTime EndDate { get; }

        /// <summary>Gets or sets the maximum temperature (oc)</summary>
        double MaxT { get; set; }

        /// <summary>Gets or sets the minimum temperature (oc)</summary>
        double MinT { get; set; }

        /// <summary>Mean temperature  /// </summary>
        double MeanT { get; }

        /// <summary>Daily mean VPD  /// </summary>
        double VPD { get; }

        /// <summary>Gets or sets the rainfall (mm)</summary>
        double Rain { get; set; }

        /// <summary>Gets or sets the solar radiation. MJ/m2/day</summary>
        double Radn { get; set; }

        /// <summary>Gets or sets the vapor pressure</summary>
        double VP { get; set; }

        /// <summary> Gets or sets the wind value found in weather file or zero if not specified.</summary>
        double Wind { get; set; }

        /// <summary> Gets or sets the CO2 level. If not specified in the weather file the default is 350.</summary>
        double CO2 { get; set; }
        
        /// <summary>Gets or sets the atmospheric air pressure. If not specified in the weather file the default is 1010 hPa.</summary>
        double AirPressure { get; set; }

        /// <summary> Gets or sets the diffuse radiation fraction. If not specified in the weather file the default is 1. </summary>
        double DiffuseFraction { get; set; }

        /// <summary>Gets the latitude</summary>
        double Latitude { get; }

        /// <summary>Gets the longitude</summary>
        double Longitude { get; }

        /// <summary>Gets the average temperature</summary>
        double Tav { get; }

        /// <summary>Gets the temperature amplitude.</summary>
        double Amp { get; }

        /// <summary>Gets the average temperature</summary>
        string FileName { get; }

        /// <summary>Gets the duration of the day in hours.</summary>
        double CalculateDayLength(double Twilight);

        /// <summary> Gets the time the sun came up.</summary>
        double CalculateSunRise();

        /// <summary> Gets the time the sun went down. </summary>
        double CalculateSunSet();



        /// <summary> MetData for tomorrow </summary>
        DailyMetDataFromFile TomorrowsMetData { get; }

        /// <summary> MetData for tomorrow </summary>
        DailyMetDataFromFile YesterdaysMetData { get; }
    }

    /// <summary>
    /// Structure containing daily met data variables
    /// </summary>
    [Serializable]
    public class DailyMetDataFromFile: Model
    {
        /// <summary>Gets or sets the maximum temperature (oc)</summary>
        public double MaxT { get; set; }

        /// <summary>Gets or sets the minimum temperature (oc)</summary>
        public double MinT { get; set; }

        /// <summary>Daily mean VPD  /// </summary>
        public double PanEvap { get; set; }

        /// <summary>Gets or sets the rainfall (mm)</summary>
        public double Rain { get; set; }

        /// <summary>Gets or sets the solar radiation. MJ/m2/day</summary>
        public double Radn { get; set; }

        /// <summary>Gets or sets the vapor pressure</summary>
        public double VP { get; set; }

        /// <summary> Gets or sets the wind value found in weather file or zero if not specified. /// </summary>
        public double Wind { get; set; }

        /// <summary> Gets or sets the CO2 level. If not specified in the weather file the default is 350. </summary>
        public double RainfallHours { get; set; }

        /// <summary> Gets or sets the atmospheric air pressure. If not specified in the weather file the default is 1010 hPa. </summary>
        public double AirPressure { get; set; }

        /// <summary> Gets or sets the diffuse radiation fraction. If not specified in the weather file the default is 1010 hPa. </summary>
        public double DiffuseFraction { get; set; }

        /// <summary> Gets or sets the diffuse radiation fraction. If not specified in the weather file the default is 1010 hPa. </summary>
        public double DayLength { get; set; }
    }
}
