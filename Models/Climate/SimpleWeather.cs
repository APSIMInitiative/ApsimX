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
        /// The index of the diffuse radiation fraction column in the weather file
        /// </summary>
        private int diffuseFractionIndex;

        /// <summary>
        /// The index of the day length column in the weather file
        /// </summary>
        private int dayLengthIndex;

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
                    return PathUtilities.GetAbsolutePath(this.constantsFile, simulation.FileName);
                else
                {
                    Simulations simulations = FindAncestor<Simulations>();
                    if (simulations != null)
                        return PathUtilities.GetAbsolutePath(this.constantsFile, simulations.FileName);
                    else
                        return this.constantsFile;
                }
            }
            set
            {
                Simulations simulations = FindAncestor<Simulations>();
                if (simulations != null)
                    this.constantsFile = PathUtilities.GetRelativePath(value, simulations.FileName);
                else
                    this.constantsFile = value;
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
                    return PathUtilities.GetAbsolutePath(this._fileName, simulation.FileName);
                else
                {
                    Simulations simulations = FindAncestor<Simulations>();
                    if (simulations != null)
                        return PathUtilities.GetAbsolutePath(this._fileName, simulations.FileName);
                    else
                        return this._fileName;
                }
            }
            set
            {
                Simulations simulations = FindAncestor<Simulations>();
                if (simulations != null)
                    this._fileName = PathUtilities.GetRelativePath(value, simulations.FileName);
                else
                    this._fileName = value;
                if (this.reader != null)
                    this.reader.Close();
                this.reader = null;
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
                if (this.reader == null && !this.OpenDataFile())
                    return new DateTime(0);

                return this.reader.FirstDate;
            }
        }

        /// <summary>Gets the end date of the weather file</summary>
        public DateTime EndDate
        {
            get
            {
                if (this.reader == null && !this.OpenDataFile())
                    return new DateTime(0);

                return this.reader.LastDate;
            }
        }

        /// <summary>Gets or sets the maximum air temperature (oC)</summary>
        [Units("°C")]
        [JsonIgnore]
        public double MaxT { get; set; }

        /// <summary>Gets or sets the minimum air temperature (oC)</summary>
        [JsonIgnore]
        [Units("°C")]
        public double MinT { get; set; }

        /// <summary>Daily mean air temperature (oC)</summary>
        [Units("°C")]
        [JsonIgnore]
        public double MeanT { get { return (MaxT + MinT) / 2; } }

        /// <summary>Gets or sets the solar radiation (MJ/m2)</summary>
        [Units("MJ/m2")]
        [JsonIgnore]
        public double Radn { get; set; }

        /// <summary>Gets or sets the maximum clear sky radiation (MJ/m2)</summary>
        [Units("MJ/m2")]
        [JsonIgnore]
        public double Qmax { get; set; }

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

        /// <summary>Gets the daily mean vapour pressure deficit (hPa)</summary>
        [Units("hPa")]
        [JsonIgnore]
        public double VPD
        {
            get
            {
                const double SVPfrac = 0.66;
                double VPDmint = MetUtilities.svp((float)MinT) - VP;
                VPDmint = Math.Max(VPDmint, 0.0);

                double VPDmaxt = MetUtilities.svp((float)MaxT) - VP;
                VPDmaxt = Math.Max(VPDmaxt, 0.0);

                return SVPfrac * VPDmaxt + (1 - SVPfrac) * VPDmint;
            }
        }

        /// <summary>Gets or sets the average wind speed (m/s)</summary>
        [Units("m/s")]
        [JsonIgnore]
        public double Wind { get; set; }

        /// <summary>Gets or sets the day length, period with light (h)</summary>
        [Units("h")]
        [JsonIgnore]
        public double DayLength { get; set; }

        /// <summary>Gets or sets the CO2 level in the atmosphere (ppm)</summary>
        [Units("ppm")]
        [JsonIgnore]
        public double CO2 { get; set; }

        /// <summary>Gets or sets the mean atmospheric air pressure</summary>
        [Units("hPa")]
        [JsonIgnore]
        public double AirPressure { get; set; }

        /// <summary>Gets the latitude (decimal degrees)</summary>
        [Units("degrees")]
        public double Latitude
        {
            get
            {
                if (this.reader == null && !this.OpenDataFile())
                    return 0;

                return this.reader.ConstantAsDouble("Latitude");
            }
        }

        /// <summary>Gets the longitude (decimal degrees)</summary>
        [Units("degrees")]
        public double Longitude
        {
            get
            {
                if (this.reader == null || this.reader.Constant("Longitude") == null)
                    return 0;
                else
                    return this.reader.ConstantAsDouble("Longitude");
            }
        }

        /// <summary>Gets the long-term average air temperature (oC)</summary>
        [Units("°C")]
        public double Tav
        {
            get
            {
                if (this.reader == null)
                    return 0;
                return this.reader.ConstantAsDouble("tav");
            }
        }

        /// <summary>Gets the long-term average temperature amplitude (oC)</summary>
        [Units("°C")]
        public double Amp
        {
            get
            {
                if (this.reader == null)
                    return 0;
                return this.reader.ConstantAsDouble("amp");
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

        /// <summary>Computes the duration of the day, with light (hours)</summary>
        /// <param name="Twilight">The angle to measure time for twilight (degrees)</param>
        /// <returns>The length of day light</returns>
        public double CalculateDayLength(double Twilight)
        {
            if (dayLengthIndex == -1 && DayLength == -1)  // daylength is not set as column or constant
                return MathUtilities.DayLength(this.clock.Today.DayOfYear, Twilight, this.Latitude);
            else
                return this.DayLength;
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
            this.maximumTemperatureIndex = 0;
            this.minimumTemperatureIndex = 0;
            this.radiationIndex = 0;
            this.rainIndex = 0;
            this.evaporationIndex = 0;
            this.rainfallHoursIndex = 0;
            this.vapourPressureIndex = 0;
            this.windIndex = 0;
            this.co2Index = -1;
            this.diffuseFractionIndex = 0;
            this.dayLengthIndex = 0;
            if (CO2 == 0)
                this.CO2 = 350;
            if (AirPressure == 0)
                this.AirPressure = 1010;
            if (DiffuseFraction == 0)
                this.DiffuseFraction = -1;
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
            if (this.reader != null)
                this.reader.Close();
            this.reader = null;
        }

        /// <summary> Performs the tasks to update the weather data</summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The arguments of the event</param>
        [EventSubscribe("DoWeather")]
        private void OnDoWeather(object sender, EventArgs e)
        {
            YesterdaysMetData = TodaysMetData;
            TodaysMetData = TomorrowsMetData;
            try
            {
                TomorrowsMetData = GetMetData(this.clock.Today.AddDays(1));
            }
            catch (Exception)
            {
                // If this fails, we've run out of met data
                TomorrowsMetData = GetMetData(this.clock.Today);
            }

            this.Radn = TodaysMetData.Radn;
            this.MaxT = TodaysMetData.MaxT;
            this.MinT = TodaysMetData.MinT;
            this.Rain = TodaysMetData.Rain;
            this.PanEvap = TodaysMetData.PanEvap;
            this.RainfallHours = TodaysMetData.RainfallHours;
            this.VP = TodaysMetData.VP;
            this.Wind = TodaysMetData.Wind;
            this.DiffuseFraction = TodaysMetData.DiffuseFraction;
            this.DayLength = TodaysMetData.DayLength;
            if (co2Index != -1)
                CO2 = TodaysMetData.CO2;

            if (this.PreparingNewWeatherData != null)
                this.PreparingNewWeatherData.Invoke(this, new EventArgs());

            Qmax = MetUtilities.QMax(clock.Today.DayOfYear + 1, Latitude, MetUtilities.Taz, MetUtilities.Alpha, VP);

            // do sanity check on weather
            SensibilityCheck(clock as Clock, this);
        }

        /// <summary>Reads the weather data for one day from file</summary>
        /// <param name="date">The date to read met data</param>
        public DailyMetDataFromFile GetMetData(DateTime date)
        {
            if (this.reader == null)
                if (!this.OpenDataFile())
                    throw new ApsimXException(this, "Cannot find weather file '" + this._fileName + "'");

            // get the weather data for that date
            DailyMetDataFromFile readMetData = new DailyMetDataFromFile();
            try
            {
                this.reader.SeekToDate(date);
                readMetData.Raw = reader.GetNextLineOfData();
            }
            catch (IndexOutOfRangeException err)
            {
                throw new Exception($"Unable to retrieve weather data on {date.ToString("yyy-MM-dd")} in file {_fileName}", err);
            }

            if (date != this.reader.GetDateFromValues(readMetData.Raw))
                throw new Exception("Non consecutive dates found in file: " + this._fileName + ".");

            return CheckDailyMetData(readMetData);
        }

        /// <summary>Checks the values for weather data, uses either daily values or a constant</summary>
        /// <param name="readMetData">The weather data structure with values for one line</param>
        /// <returns>The weather data structure with values checked</returns>
        private DailyMetDataFromFile CheckDailyMetData(DailyMetDataFromFile readMetData)
        {
            if (this.radiationIndex != -1)
                readMetData.Radn = Convert.ToSingle(readMetData.Raw[this.radiationIndex], CultureInfo.InvariantCulture);
            else
                readMetData.Radn = this.reader.ConstantAsDouble("radn");

            if (this.maximumTemperatureIndex != -1)
                readMetData.MaxT = Convert.ToSingle(readMetData.Raw[this.maximumTemperatureIndex], CultureInfo.InvariantCulture);
            else
                readMetData.MaxT = this.reader.ConstantAsDouble("maxt");

            if (this.minimumTemperatureIndex != -1)
                readMetData.MinT = Convert.ToSingle(readMetData.Raw[this.minimumTemperatureIndex], CultureInfo.InvariantCulture);
            else
                readMetData.MinT = this.reader.ConstantAsDouble("mint");

            if (this.rainIndex != -1)
                readMetData.Rain = Convert.ToSingle(readMetData.Raw[this.rainIndex], CultureInfo.InvariantCulture);
            else
                readMetData.Rain = this.reader.ConstantAsDouble("rain");

            if (this.evaporationIndex == -1)
                readMetData.PanEvap = double.NaN;
            else
                readMetData.PanEvap = Convert.ToSingle(readMetData.Raw[this.evaporationIndex], CultureInfo.InvariantCulture);

            if (this.rainfallHoursIndex == -1)
                readMetData.RainfallHours = double.NaN;
            else
                readMetData.RainfallHours = Convert.ToSingle(readMetData.Raw[this.rainfallHoursIndex], CultureInfo.InvariantCulture);

            if (this.vapourPressureIndex == -1)
                readMetData.VP = Math.Max(0, MetUtilities.svp(readMetData.MinT));
            else
                readMetData.VP = Convert.ToSingle(readMetData.Raw[this.vapourPressureIndex], CultureInfo.InvariantCulture);

            if (this.windIndex == -1)
                readMetData.Wind = 3.0;
            else
                readMetData.Wind = Convert.ToSingle(readMetData.Raw[this.windIndex], CultureInfo.InvariantCulture);

            if (co2Index != -1)
                readMetData.CO2 = Convert.ToDouble(readMetData.Raw[co2Index], CultureInfo.InvariantCulture);

            if (this.diffuseFractionIndex == -1)
            {
                // estimate diffuse fraction using the approach of Bristow and Campbell
                double Qmax = MetUtilities.QMax(clock.Today.DayOfYear + 1, Latitude, MetUtilities.Taz, MetUtilities.Alpha, 0.0); // Radiation for clear and dry sky (ie low humidity)
                double Q0 = MetUtilities.Q0(clock.Today.DayOfYear + 1, Latitude);
                double B = Qmax / Q0;
                double Tt = MathUtilities.Bound(readMetData.Radn / Q0, 0, 1);
                if (Tt > B) Tt = B;
                readMetData.DiffuseFraction = (1 - Math.Exp(0.6 * (1 - B / Tt) / (B - 0.4)));
                if (Tt > 0.5 && readMetData.DiffuseFraction < 0.1) readMetData.DiffuseFraction = 0.1;
            }
            else
                readMetData.DiffuseFraction = Convert.ToSingle(readMetData.Raw[this.diffuseFractionIndex], CultureInfo.InvariantCulture);

            if (this.dayLengthIndex == -1)  // DayLength is not a column - check for a constant
            {
                if (this.reader.Constant("daylength") != null)
                    readMetData.DayLength = this.reader.ConstantAsDouble("daylength");
                else
                    readMetData.DayLength = -1;
            }
            else
                readMetData.DayLength = Convert.ToSingle(readMetData.Raw[this.dayLengthIndex], CultureInfo.InvariantCulture);

            return readMetData;
        }

        /// <summary>Opens the weather data file</summary>
        /// <returns>True if the file was successfully opened</returns>
        public bool OpenDataFile()
        {
            if (System.IO.File.Exists(this.FileName))
            {
                if (this.reader == null)
                {
                    if (ExcelUtilities.IsExcelFile(FileName) && string.IsNullOrEmpty(ExcelWorkSheetName))
                        throw new Exception($"Unable to open excel file {FileName}: no sheet name is specified");

                    this.reader = new ApsimTextFile();
                    this.reader.Open(this.FileName, this.ExcelWorkSheetName);

                    if (this.reader.Headings == null)
                    {
                        string message = "Cannot find the expected header in ";
                        if (ExcelUtilities.IsExcelFile(FileName))
                            message += $"sheet '{ExcelWorkSheetName}' of ";
                        message += $"weather file: {FileName}";
                        throw new Exception(message);
                    }

                    this.maximumTemperatureIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, "Maxt");
                    this.minimumTemperatureIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, "Mint");
                    this.radiationIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, "Radn");
                    this.rainIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, "Rain");
                    this.evaporationIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, "Evap");
                    this.rainfallHoursIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, "RainHours");
                    this.vapourPressureIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, "VP");
                    this.windIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, "Wind");
                    this.diffuseFractionIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, "DifFr");
                    this.dayLengthIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, "DayLength");
                    co2Index = StringUtilities.IndexOfCaseInsensitive(reader.Headings, "CO2");

                    if (!string.IsNullOrEmpty(ConstantsFile))
                    {
                        ApsimTextFile constantsReader = new ApsimTextFile();
                        constantsReader.Open(ConstantsFile);
                        if (constantsReader.Constants != null)
                            foreach (ApsimConstant constant in constantsReader.Constants)
                                this.reader.AddConstant(constant.Name, constant.Value, constant.Units, constant.Comment);
                    }

                    if (this.maximumTemperatureIndex == -1)
                        if (this.reader == null || this.reader.Constant("maxt") == null)
                            throw new Exception("Cannot find MaxT in weather file: " + this.FileName);

                    if (this.minimumTemperatureIndex == -1)
                        if (this.reader == null || this.reader.Constant("mint") == null)
                            throw new Exception("Cannot find MinT in weather file: " + this.FileName);

                    if (this.radiationIndex == -1)
                        if (this.reader == null || this.reader.Constant("radn") == null)
                            throw new Exception("Cannot find Radn in weather file: " + this.FileName);

                    if (this.rainIndex == -1)
                        if (this.reader == null || this.reader.Constant("rain") == null)
                            throw new Exception("Cannot find Rain in weather file: " + this.FileName);
                }
                else
                {
                    if (this.reader.IsExcelFile != true)
                        this.reader.SeekToDate(this.reader.FirstDate);
                }

                return true;
            }
            else
            {
                return false;
            }
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
            if (weatherToday.VP <= 0)
            {
                throw new Exception("Error: Weather on " + clock.Today.ToString() + " has vapour pressure (" + weatherToday.VP + ") which is below 0");
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
        }
    }
}
