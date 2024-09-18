using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Models.Climate
{
    ///<summary>
    /// Reads in weather data from a met file and makes it available to other models
    ///</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Simulation))]
    [ValidParent(ParentType = typeof(Zone))]
    public class SimpleWeather : Model, IWeather, IReferenceExternalFiles
    {
        /// <summary>
        /// A link to the clock model
        /// </summary>
        [Link]
        private IClock clock = null;

        /// <summary>
        /// A link to the summary (log file)
        /// </summary>
        [Link]
        private ISummary summary = null;

        /// <summary>
        /// Event that will be invoked immediately before the daily weather data is updated
        /// </summary>
        /// <remarks>
        /// This provides models and scripts an opportunity to change the weather data before
        /// other models access them
        /// </remarks>
        public event EventHandler PreparingNewWeatherData;

        /// <summary>A reference to the text file reader object</summary>
        [NonSerialized]
        private ApsimTextFile reader = null;

        /// <summary>
        /// The index of the maximum temperature column in the weather file
        /// </summary>
        private int maximumTemperatureIndex;

        /// <summary>
        /// The index of the minimum temperature column in the weather file
        /// </summary>
        private int minimumTemperatureIndex;

        /// <summary>
        /// The index of the solar radiation column in the weather file
        /// </summary>
        private int radiationIndex;

        /// <summary>
        /// The index of the day length column in the weather file
        /// </summary>
        private int dayLengthIndex;

        /// <summary>
        /// The index of the diffuse radiation fraction column in the weather file
        /// </summary>
        private int diffuseFractionIndex;

        /// <summary>
        /// The index of the rainfall column in the weather file
        /// </summary>
        private int rainIndex;

        /// <summary>
        /// The index of the evaporation column in the weather file
        /// </summary>
        private int evaporationIndex;

        /// <summary>
        /// The index of the rainfall duration column in the weather file
        /// </summary>
        private int rainfallHoursIndex;

        /// <summary>
        /// The index of the vapour pressure column in the weather file
        /// </summary>
        private int vapourPressureIndex;

        /// <summary>
        /// The index of the wind speed column in the weather file
        /// </summary>
        private int windIndex;

        /// <summary>
        /// The index of the co2 column in the weather file, or -1
        /// if the weather file doesn't contain co2.
        /// </summary>
        private int co2Index;

        /// <summary>
        /// Stores the optional constants file name. This should only be accessed via
        /// <see cref="ConstantsFile"/>, which handles conversion between
        /// relative/absolute paths
        /// </summary>
        private string constantsFile;

        /// <summary>
        /// Gets or sets the optional constants file name. Allows to specify a second file which
        /// contains constants such as latitude, longitude, tav, amp, etc.; really only used when
        /// the actual met data is in a .csv file
        /// </summary>
        [Description("Constants file")]
        public string ConstantsFile
        {
            get
            {
                Simulation simulation = FindAncestor<Simulation>();
                if (simulation != null)
                    return PathUtilities.GetAbsolutePath(constantsFile, simulation.FileName);
                else
                {
                    Simulations simulations = FindAncestor<Simulations>();
                    if (simulations != null)
                        return PathUtilities.GetAbsolutePath(constantsFile, simulations.FileName);
                    else
                        return constantsFile;
                }
            }
            set
            {
                Simulations simulations = FindAncestor<Simulations>();
                if (simulations != null)
                    constantsFile = PathUtilities.GetRelativePath(value, simulations.FileName);
                else
                    constantsFile = value;
            }
        }

        /// <summary>
        /// Gets or sets the weather file name. Should be relative file path where possible
        /// </summary>
        [Summary]
        [Description("Weather file name")]
        public string _fileName { get; set; }

        /// <summary>
        /// Gets or sets the full file name (with path). Needed for the user interface
        /// </summary>
        [JsonIgnore]
        public string FileName
        {
            get
            {
                Simulation simulation = FindAncestor<Simulation>();
                if (simulation != null)
                    return PathUtilities.GetAbsolutePath(_fileName, simulation.FileName);
                else
                {
                    Simulations simulations = FindAncestor<Simulations>();
                    if (simulations != null)
                        return PathUtilities.GetAbsolutePath(_fileName, simulations.FileName);
                    else
                        return _fileName;
                }
            }
            set
            {
                Simulations simulations = FindAncestor<Simulations>();
                if (simulations != null)
                    _fileName = PathUtilities.GetRelativePath(value, simulations.FileName);
                else
                    _fileName = value;
                if (reader != null)
                    reader.Close();
                reader = null;
            }
        }

        /// <summary>
        /// Gets or sets the WorkSheet name with weather data, if data is supplied as an Excel file
        /// </summary>
        public string ExcelWorkSheetName { get; set; }

        /// <summary>Gets the start date of the weather file</summary>
        public DateTime StartDate
        {
            get
            {
                if (reader == null && !OpenDataFile())
                    return new DateTime(0);

                return reader.FirstDate;
            }
        }

        /// <summary>Gets the end date of the weather file</summary>
        public DateTime EndDate
        {
            get
            {
                if (reader == null && !OpenDataFile())
                    return new DateTime(0);

                return reader.LastDate;
            }
        }

        /// <summary>Gets or sets the daily minimum air temperature (oC)</summary>
        [JsonIgnore]
        [Units("°C")]
        public double MinT { get; set; }

        /// <summary>Gets or sets the daily maximum air temperature (oC)</summary>
        [Units("°C")]
        [JsonIgnore]
        public double MaxT { get; set; }

        /// <summary>Gets or sets the daily mean air temperature (oC)</summary>
        [Units("°C")]
        [JsonIgnore]
        public double MeanT { get; set; }

        /// <summary>Gets or sets the solar radiation (MJ/m2)</summary>
        [Units("MJ/m2")]
        [JsonIgnore]
        public double Radn { get; set; }

        /// <summary>Gets or sets the maximum clear sky radiation (MJ/m2)</summary>
        [Units("MJ/m2")]
        [JsonIgnore]
        public double Qmax { get; set; }

        /// <summary>Gets or sets the day length, period with light (h)</summary>
        [Units("h")]
        [JsonIgnore]
        public double DayLength { get; set; }

        /// <summary>Gets or sets the diffuse radiation fraction (0-1)</summary>
        [Units("0-1")]
        [JsonIgnore]
        public double DiffuseFraction { get; set; }

        /// <summary>Gets or sets the rainfall amount (mm)</summary>
        [Units("mm")]
        [JsonIgnore]
        public double Rain { get; set; }

        /// <summary>Gets or sets the class A pan evaporation (mm)</summary>
        [Units("mm")]
        [JsonIgnore]
        public double PanEvap { get; set; }

        /// <summary>Gets or sets the number duration of rainfall within a day (h)</summary>
        [Units("h")]
        [JsonIgnore]
        public double RainfallHours { get; set; }

        /// <summary>Gets or sets the air vapour pressure (hPa)/// </summary>
        [Units("hPa")]
        [JsonIgnore]
        public double VP { get; set; }

        /// <summary>Gets or sets the daily mean vapour pressure deficit (hPa)</summary>
        [Units("hPa")]
        [JsonIgnore]
        public double VPD { get; set; }

        /// <summary>Gets or sets the average wind speed (m/s)</summary>
        [Units("m/s")]
        [JsonIgnore]
        public double Wind { get; set; }

        /// <summary>Gets or sets the CO2 level in the atmosphere (ppm)</summary>
        [Units("ppm")]
        [JsonIgnore]
        public double CO2 { get; set; }

        /// <summary>Gets or sets the mean atmospheric air pressure</summary>
        [Units("hPa")]
        [JsonIgnore]
        public double AirPressure { get; set; }

        /// <summary>Gets or sets the latitude (decimal degrees)</summary>
        [Units("degrees")]
        public double Latitude
        {
            get
            {
                if (reader == null && !OpenDataFile())
                    return 0;

                return reader.ConstantAsDouble("Latitude");
            }
            set
            {
                if (this.reader != null)
                    reader.Constant("Latitude").Value = value.ToString();
            }
        }

        /// <summary>Gets or sets the longitude (decimal degrees)</summary>
        [Units("degrees")]
        public double Longitude
        {
            get
            {
                if (reader == null || reader.Constant("Longitude") == null)
                    return 0;
                else
                    return reader.ConstantAsDouble("Longitude");
            }
            set
            {
                if (reader != null)
                    reader.Constant("Longitude").Value = value.ToString();
            }
        }

        /// <summary>Gets the long-term average air temperature (oC)</summary>
        [Units("°C")]
        public double Tav
        {
            get
            {
                if (reader == null)
                    return 0;
                return reader.ConstantAsDouble("tav");
            }
        }

        /// <summary>Gets the long-term average temperature amplitude (oC)</summary>
        [Units("°C")]
        public double Amp
        {
            get
            {
                if (reader == null)
                    return 0;
                return reader.ConstantAsDouble("amp");
            }
        }

        /// <summary>Met data from yesterday</summary>
        [JsonIgnore]
        public DailyMetDataFromFile YesterdaysMetData { get; set; }

        /// <summary>Met data for today</summary>
        [JsonIgnore]
        public DailyMetDataFromFile TodaysMetData { get; set; }

        /// <summary>Met data for tomorrow</summary>
        [JsonIgnore]
        public DailyMetDataFromFile TomorrowsMetData { get; set; }

        /// <summary>Returns our input file names</summary>
        public IEnumerable<string> GetReferencedFileNames()
        {
            return new string[] { FileName };
        }

        /// <summary>Remove all paths from referenced file names</summary>
        public void RemovePathsFromReferencedFileNames()
        {
            _fileName = Path.GetFileName(_fileName);
        }

        /// <summary>
        /// Check values in weather and return a collection of warnings
        /// </summary>
        public IEnumerable<string> Validate()
        {
            if (Amp > 20)
            {
                yield return $"The value of Weather.AMP ({Amp}) is > 20 oC. Please check the value.";
            }
        }

        /// <summary>Overrides the base class method to allow for initialization of this model </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The arguments of the event</param>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            minimumTemperatureIndex = 0;
            maximumTemperatureIndex = 0;
            radiationIndex = 0;
            dayLengthIndex = 0;
            diffuseFractionIndex = 0;
            rainIndex = 0;
            evaporationIndex = 0;
            rainfallHoursIndex = 0;
            vapourPressureIndex = 0;
            windIndex = 0;
            co2Index = 0;

            if (reader != null)
            {
                reader.Close();
                reader = null;
            }

            foreach (var message in Validate())
                summary.WriteMessage(this, message, MessageType.Warning);
        }

        /// <summary>Overrides the base class method to perform the necessary initialisation</summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The arguments of the event</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            bool hasYesterday = true;
            try
            {
                YesterdaysMetData = GetMetData(clock.Today.AddDays(-1));
            }
            catch (Exception)
            {
                hasYesterday = false;
            }

            TodaysMetData = GetMetData(clock.Today);

            if (!hasYesterday)
                YesterdaysMetData = TodaysMetData;

            TomorrowsMetData = GetMetData(clock.Today.AddDays(1));
        }

        /// <summary>Overrides the base class method to allow for clean up task</summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The arguments of the event</param>
        [EventSubscribe("Completed")]
        private void OnSimulationCompleted(object sender, EventArgs e)
        {
            if (reader != null)
                reader.Close();
            reader = null;
        }

        /// <summary> Performs the tasks to update the weather data</summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The arguments of the event</param>
        [EventSubscribe("DoWeather")]
        private void OnDoWeather(object sender, EventArgs e)
        {
            // read weather data (for yesterday, today, and tomorrow)
            YesterdaysMetData = TodaysMetData;
            TodaysMetData = TomorrowsMetData;
            try
            {
                TomorrowsMetData = GetMetData(clock.Today.AddDays(1));
            }
            catch (Exception)
            {
                // if this fails, we've run out of met data
                TomorrowsMetData = GetMetData(clock.Today);
            }

            // assign values to output variables
            MinT = TodaysMetData.MinT;
            MaxT = TodaysMetData.MaxT;
            Radn = TodaysMetData.Radn;
            DayLength = TodaysMetData.DayLength;
            DiffuseFraction = TodaysMetData.DiffuseFraction;
            Rain = TodaysMetData.Rain;
            PanEvap = TodaysMetData.PanEvap;
            RainfallHours = TodaysMetData.RainfallHours;
            VP = TodaysMetData.VP;
            Wind = TodaysMetData.Wind;
            CO2 = TodaysMetData.CO2;
            AirPressure = calculateAirPressure(27.08889); // to default 1010;

            // invoke event that allows other models to modify weather data
            if (PreparingNewWeatherData != null)
                PreparingNewWeatherData.Invoke(this, new EventArgs());

            // compute a series of values derived from weather data
            MeanT = (MaxT + MinT) / 2.0;
            Qmax = MetUtilities.QMax(clock.Today.DayOfYear + 1, Latitude, MetUtilities.Taz, MetUtilities.Alpha, VP);

            // do sanity check on weather
            SensibilityCheck(clock as Clock, this);
        }

        /// <summary>Reads the weather data for one day from file</summary>
        /// <param name="date">The date to read met data</param>
        public DailyMetDataFromFile GetMetData(DateTime date)
        {
            if (reader == null)
                if (!OpenDataFile())
                    throw new ApsimXException(this, "Cannot find weather file '" + _fileName + "'");

            // get the weather data for that date
            DailyMetDataFromFile readMetData = new DailyMetDataFromFile();
            try
            {
                reader.SeekToDate(date);
                readMetData.Raw = reader.GetNextLineOfData();
            }
            catch (IndexOutOfRangeException err)
            {
                throw new Exception($"Unable to retrieve weather data on {date.ToString("yyy-MM-dd")} in file {_fileName}", err);
            }

            if (date != reader.GetDateFromValues(readMetData.Raw))
                throw new Exception("Non consecutive dates found in file: " + _fileName + ".");

            return CheckDailyMetData(readMetData);
        }

        /// <summary>Checks the values for weather data, uses either daily values or a constant</summary>
        /// <param name="readMetData">The weather data structure with values for one line</param>
        /// <returns>The weather data structure with values checked</returns>
        private DailyMetDataFromFile CheckDailyMetData(DailyMetDataFromFile readMetData)
        {
            if (minimumTemperatureIndex != -1)
                readMetData.MinT = Convert.ToSingle(readMetData.Raw[minimumTemperatureIndex], CultureInfo.InvariantCulture);
            else
                readMetData.MinT = reader.ConstantAsDouble("mint");

            if (maximumTemperatureIndex != -1)
                readMetData.MaxT = Convert.ToSingle(readMetData.Raw[maximumTemperatureIndex], CultureInfo.InvariantCulture);
            else
                readMetData.MaxT = reader.ConstantAsDouble("maxt");

            if (radiationIndex != -1)
                readMetData.Radn = Convert.ToSingle(readMetData.Raw[radiationIndex], CultureInfo.InvariantCulture);
            else
                readMetData.Radn = reader.ConstantAsDouble("radn");

            if (rainIndex != -1)
                readMetData.Rain = Convert.ToSingle(readMetData.Raw[rainIndex], CultureInfo.InvariantCulture);
            else
                readMetData.Rain = reader.ConstantAsDouble("rain");

            if (evaporationIndex == -1)
                readMetData.PanEvap = double.NaN;
            else
                readMetData.PanEvap = Convert.ToSingle(readMetData.Raw[evaporationIndex], CultureInfo.InvariantCulture);

            if (rainfallHoursIndex == -1)
                readMetData.RainfallHours = double.NaN;
            else
                readMetData.RainfallHours = Convert.ToSingle(readMetData.Raw[rainfallHoursIndex], CultureInfo.InvariantCulture);

            if (vapourPressureIndex == -1)
                readMetData.VP = Math.Max(0, MetUtilities.svp(readMetData.MinT));
            else
                readMetData.VP = Convert.ToSingle(readMetData.Raw[vapourPressureIndex], CultureInfo.InvariantCulture);

            if (windIndex == -1)
                readMetData.Wind = 3.0;
            else
                readMetData.Wind = Convert.ToSingle(readMetData.Raw[windIndex], CultureInfo.InvariantCulture);

            if (co2Index == -1)
            { // CO2 is not a column - check for a constant
                if (reader.Constant("co2") != null)
                    readMetData.DayLength = reader.ConstantAsDouble("co2");
                else
                    readMetData.CO2 = 350;
            }
            else
                readMetData.CO2 = Convert.ToDouble(readMetData.Raw[co2Index], CultureInfo.InvariantCulture);

            if (diffuseFractionIndex == -1)
                readMetData.DiffuseFraction = calculateDiffuseRadiationFraction(readMetData.Radn);
            else
                readMetData.DiffuseFraction = Convert.ToSingle(readMetData.Raw[diffuseFractionIndex], CultureInfo.InvariantCulture);

            if (dayLengthIndex == -1)  // DayLength is not a column - check for a constant
            {
                if (reader.Constant("daylength") != null)
                    readMetData.DayLength = reader.ConstantAsDouble("daylength");
                else
                    readMetData.DayLength = -1;
            }
            else
                readMetData.DayLength = Convert.ToSingle(readMetData.Raw[dayLengthIndex], CultureInfo.InvariantCulture);

            return readMetData;
        }

        /// <summary>Opens the weather data file</summary>
        /// <returns>True if the file was successfully opened</returns>
        public bool OpenDataFile()
        {
            if (System.IO.File.Exists(FileName))
            {
                if (reader == null)
                {
                    if (ExcelUtilities.IsExcelFile(FileName) && string.IsNullOrEmpty(ExcelWorkSheetName))
                        throw new Exception($"Unable to open excel file {FileName}: no sheet name is specified");

                    reader = new ApsimTextFile();
                    reader.Open(FileName, ExcelWorkSheetName);

                    if (reader.Headings == null)
                    {
                        string message = "Cannot find the expected header in ";
                        if (ExcelUtilities.IsExcelFile(FileName))
                            message += $"sheet '{ExcelWorkSheetName}' of ";
                        message += $"weather file: {FileName}";
                        throw new Exception(message);
                    }

                    minimumTemperatureIndex = StringUtilities.IndexOfCaseInsensitive(reader.Headings, "Mint");
                    maximumTemperatureIndex = StringUtilities.IndexOfCaseInsensitive(reader.Headings, "Maxt");
                    radiationIndex = StringUtilities.IndexOfCaseInsensitive(reader.Headings, "Radn");
                    rainIndex = StringUtilities.IndexOfCaseInsensitive(reader.Headings, "Rain");
                    evaporationIndex = StringUtilities.IndexOfCaseInsensitive(reader.Headings, "Evap");
                    rainfallHoursIndex = StringUtilities.IndexOfCaseInsensitive(reader.Headings, "RainHours");
                    vapourPressureIndex = StringUtilities.IndexOfCaseInsensitive(reader.Headings, "VP");
                    windIndex = StringUtilities.IndexOfCaseInsensitive(reader.Headings, "Wind");
                    diffuseFractionIndex = StringUtilities.IndexOfCaseInsensitive(reader.Headings, "DifFr");
                    dayLengthIndex = StringUtilities.IndexOfCaseInsensitive(reader.Headings, "DayLength");
                    co2Index = StringUtilities.IndexOfCaseInsensitive(reader.Headings, "CO2");

                    if (!string.IsNullOrEmpty(ConstantsFile))
                    {
                        ApsimTextFile constantsReader = new ApsimTextFile();
                        constantsReader.Open(ConstantsFile);
                        if (constantsReader.Constants != null)
                            foreach (ApsimConstant constant in constantsReader.Constants)
                                reader.AddConstant(constant.Name, constant.Value, constant.Units, constant.Comment);
                    }

                    if (minimumTemperatureIndex == -1)
                        if (reader == null || reader.Constant("mint") == null)
                            throw new Exception("Cannot find MinT in weather file: " + FileName);

                    if (maximumTemperatureIndex == -1)
                        if (reader == null || reader.Constant("maxt") == null)
                            throw new Exception("Cannot find MaxT in weather file: " + FileName);

                    if (radiationIndex == -1)
                        if (reader == null || reader.Constant("radn") == null)
                            throw new Exception("Cannot find Radn in weather file: " + FileName);

                    if (rainIndex == -1)
                        if (reader == null || reader.Constant("rain") == null)
                            throw new Exception("Cannot find Rain in weather file: " + FileName);
                }
                else
                {
                    if (reader.IsExcelFile != true)
                        reader.SeekToDate(reader.FirstDate);
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>Closes the data file</summary>
        public void CloseDataFile()
        {
            if (reader != null)
                reader.Close();
            reader = null;
        }

        /// <summary>Computes the duration of the day, with light (hours)</summary>
        /// <param name="Twilight">The angle to measure time for twilight (degrees)</param>
        /// <returns>The length of day light</returns>
        public double CalculateDayLength(double Twilight)
        {
            if (dayLengthIndex == -1 && DayLength == -1)  // daylength is not set as column or constant
                return MathUtilities.DayLength(clock.Today.DayOfYear, Twilight, Latitude);
            else
                return DayLength;
        }

        /// <summary>Computes the time of sun rise (h)</summary>
        /// <returns>Sun rise time</returns>
        public double CalculateSunRise()
        {
            return 12 - CalculateDayLength(-6) / 2;
        }

        /// <summary>Computes the time of sun set (h)</summary>
        /// <returns>Sun set time</returns>
        public double CalculateSunSet()
        {
            return 12 + CalculateDayLength(-6) / 2;
        }

        /// <summary>Estimate diffuse radiation fraction (0-1)</summary>
        /// <remarks>Uses the approach of Bristow and Campbell (0000)</remarks>
        /// <returns>The diffuse radiation fraction</returns>
        private double calculateDiffuseRadiationFraction(double todaysRadiation)
        {
            // estimate diffuse fraction using the approach of Bristow and Campbell
            double Qmax = MetUtilities.QMax(clock.Today.DayOfYear + 1, Latitude, MetUtilities.Taz, MetUtilities.Alpha, 0.0); // Radiation for clear and dry sky (ie low humidity)
            double Q0 = MetUtilities.Q0(clock.Today.DayOfYear + 1, Latitude);
            double B = Qmax / Q0;
            double Tt = MathUtilities.Bound(todaysRadiation / Q0, 0, 1);
            if (Tt > B) Tt = B;
            double result = (1 - Math.Exp(0.6 * (1 - B / Tt) / (B - 0.4)));
            if (Tt > 0.5 && result < 0.1)
                result = 0.1;
            return result;
        }

        /// <summary>Computes today's atmospheric vapour pressure deficit (hPa)</summary>
        /// <param name="minTemp">Today's minimum temperature (oC)</param>
        /// <param name="maxTemp">Today's maximum temperature (oC)</param>
        /// <param name="vapourPressure">Today's vapour pressure (hPa)</param>
        /// <returns>The vapour pressure deficit (hPa)</returns>
        private double calculateVapourPressureDefict(double minTemp, double maxTemp, double vapourPressure)
        {
            const double SVPfrac = 0.66;
            double result;
            double VPDmint = MetUtilities.svp(minTemp) - vapourPressure;
            VPDmint = Math.Max(VPDmint, 0.0);

            double VPDmaxt = MetUtilities.svp(MaxT) - vapourPressure;
            VPDmaxt = Math.Max(VPDmaxt, 0.0);

            result = SVPfrac * VPDmaxt + (1 - SVPfrac) * VPDmint;
            return result;
        }

        /// <summary>Computes the air pressure for a given location</summary>
        /// <param name="localAltitude">The altitude (m)</param>
        /// <returns>The air pressure (hPa)</returns>
        private double calculateAirPressure(double localAltitude)
        {
            const double baseTemperature = 288.15;             // (K)
            const double basePressure = 101325.0;              // (Pa)
            const double lapseRate = 0.0065;                   // (K/m)
            const double gravitationalAcceleration = 9.80665;  // (m/s)
            const double molarMassOfAir = 0.0289644;           // (kg/mol)
            const double universalGasConstant = 8.3144598;     // (J/mol/K)
            double result;
            result = basePressure * Math.Pow(1.0 - localAltitude * lapseRate / baseTemperature,
                     gravitationalAcceleration * molarMassOfAir / (universalGasConstant * lapseRate));
            return result / 100.0;  // to hPa
        }

        /// <summary>Read a user-defined variable from today's weather data</summary>
        /// <param name="columnName">Name of the column/variable to retrieve</param>
        public double GetValue(string columnName)
        {
            int columnIndex = StringUtilities.IndexOfCaseInsensitive(reader.Headings, columnName);
            if (columnIndex == -1)
                throw new InvalidOperationException($"Column {columnName} does not exist in {_fileName}");
            return Convert.ToDouble(TodaysMetData.Raw[columnIndex], CultureInfo.InvariantCulture);
        }

        /// <summary>Checks the weather data to ensure values are valid/sensible</summary>
        /// <remarks>
        /// This will send an error message if:
        ///  - MinT is less than MaxT
        ///  - Radn is greater than 0.0 or greater than 40.0
        ///  - Rain is less than 0.0
        ///  - VP is less or equal to 0.0
        /// Also checks that every day has weather
        /// </remarks>
        /// <param name="clock">The clock</param>
        /// <param name="weatherToday">The weather</param>
        private void SensibilityCheck(Clock clock, SimpleWeather weatherToday)
        {
            if (weatherToday.MinT > weatherToday.MaxT)
            {
                throw new Exception("Error: Weather on " + clock.Today.ToString() + " has higher minimum temperature (" + weatherToday.MinT + ") than maximum (" + weatherToday.MaxT + ")");
            }
            if (weatherToday.Radn < 0)
            {
                throw new Exception("Error: Weather on " + clock.Today.ToString() + " has negative solar radiation (" + weatherToday.Radn + ")");
            }
            if (weatherToday.Radn > 40)
            {
                throw new Exception("Error: Weather on " + clock.Today.ToString() + " has solar radiation (" + weatherToday.Radn + ") which is above 40");
            }
            if (weatherToday.Rain < 0)
            {
                throw new Exception("Error: Weather on " + clock.Today.ToString() + " has negative ranfaill (" + weatherToday.Radn + ")");
            }
            if (weatherToday.VP <= 0)
            {
                throw new Exception("Error: Weather on " + clock.Today.ToString() + " has vapour pressure (" + weatherToday.VP + ") which is below 0");
            }
        }
    }
}
