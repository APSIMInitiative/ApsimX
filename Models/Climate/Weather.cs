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
    [ViewName("UserInterface.Views.TabbedMetDataView")]
    [PresenterName("UserInterface.Presenters.MetDataPresenter")]
    [ValidParent(ParentType = typeof(Simulation))]
    [ValidParent(ParentType = typeof(Zone))]
    public class Weather : Model, IWeather, IReferenceExternalFiles
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

        /// <summary>Event that will be invoked at the start of spring </summary>
        public event EventHandler StartOfSpring;

        /// <summary>Event that will be invoked at the start of summer</summary>
        public event EventHandler StartOfSummer;

        /// <summary>Event that will be invoked at the start of autumn</summary>
        public event EventHandler StartOfAutumn;

        /// <summary>Event that will be invoked at the start of winter</summary>
        public event EventHandler StartOfWinter;

        /// <summary>Event that will be invoked at the end of spring</summary>
        public event EventHandler EndOfSpring;

        /// <summary>Event that will be invoked at the end of summer</summary>
        public event EventHandler EndOfSummer;

        /// <summary>Event that will be invoked at the end of autumn</summary>
        public event EventHandler EndOfAutumn;

        /// <summary>Event that will be invoked at the end of winter</summary>
        public event EventHandler EndOfWinter;

        /// <summary>A reference to the text file reader object</summary>
        [NonSerialized]
        private ApsimTextFile reader = null;

        /// <summary>
        /// A LinkedList of weather that has previously been read. The data is stored in order of
        /// date, from newest to oldest, as most recent weather is most likely to be searched for
        /// </summary>
        private LinkedList<WeatherRecordEntry> weatherCache = new LinkedList<WeatherRecordEntry>();

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
        /// Stores the CO2 value from either the default 350 or from a column in met file. Public property can then also check
        /// if this value was supplied by a constant
        /// </summary>
        [JsonIgnore]
        private double co2Value { get; set; }

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
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the full file name (with path). Needed for the user interface
        /// </summary>
        [JsonIgnore]
        public string FullFileName
        {
            get
            {
                Simulation simulation = FindAncestor<Simulation>();
                if (simulation != null && simulation.FileName != null)
                    return PathUtilities.GetAbsolutePath(this.FileName, simulation.FileName);
                else
                {
                    Simulations simulations = FindAncestor<Simulations>();
                    if (simulations != null)
                        return PathUtilities.GetAbsolutePath(this.FileName, simulations.FileName);
                    else
                        return PathUtilities.GetAbsolutePath(this.FileName, "");
                }
            }
            set
            {
                Simulations simulations = FindAncestor<Simulations>();
                if (simulations != null)
                    this.FileName = PathUtilities.GetRelativePath(value, simulations.FileName);
                else
                    this.FileName = value;
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
        public double CO2
        {
            get
            {
                if (this.reader == null || this.reader.Constant("co2") == null)
                    return co2Value;
                else
                    return this.reader.ConstantAsDouble("co2");
            }
            set
            {
                co2Value = value;
            }
        }

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
                else if (this.reader.Constant("tav") == null)
                    this.calculateTAVAMP();

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
                else if (this.reader.Constant("amp") == null)
                    this.calculateTAVAMP();

                return this.reader.ConstantAsDouble("amp");
            }
        }

        /// <summary>Gets the day for the winter solstice (day)</summary>
        [Units("day")]
        [JsonIgnore]
        public int WinterSolsticeDOY
        {
            get
            {
                if (Latitude <= 0)
                {
                    if (DateTime.IsLeapYear(clock.Today.Year))
                        return 173;
                    else
                        return 172;
                }
                else
                {
                    if (DateTime.IsLeapYear(clock.Today.Year))
                        return 356;
                    else
                        return 355;
                }
            }
        }

        /// <summary>Gets the number of days since the winter solstice</summary>
        [Units("d")]
        [JsonIgnore]
        public int DaysSinceWinterSolstice { get; set; }

        /// <summary>Gets or sets the first date of summer (dd-mmm)</summary>
        [JsonIgnore]
        public string FirstDateOfSummer { get; set; } = "1-Dec";

        /// <summary>Gets or sets the first date of autumn (dd-mmm)</summary>
        [JsonIgnore]
        public string FirstDateOfAutumn { get; set; } = "1-Mar";

        /// <summary>Gets or sets the first date of winter (dd-mmm)</summary>
        [JsonIgnore]
        public string FirstDateOfWinter { get; set; } = "1-Jun";

        /// <summary>Gets or sets the first date of spring (dd-mmm)</summary>
        [JsonIgnore]
        public string FirstDateOfSpring { get; set; } = "1-Sep";

        /// <summary>Met data from yesterday</summary>
        [JsonIgnore]
        public DailyMetDataFromFile YesterdaysMetData { get { return GetAdjacentMetData(-1); } }

        /// <summary>Met data for today</summary>
        [JsonIgnore]
        public DailyMetDataFromFile TodaysMetData { get; set; }

        /// <summary>Met data for tomorrow</summary>
        [JsonIgnore]
        public DailyMetDataFromFile TomorrowsMetData { get { return GetAdjacentMetData(1); } }

        /// <summary>
        /// Temporarily stores which tab is currently displayed.
        /// Meaningful only within the GUI
        /// </summary>
        [JsonIgnore] public int ActiveTabIndex = 0;

        /// <summary>
        /// Temporarily stores the starting date for charts.
        /// Meaningful only within the GUI
        /// </summary>
        [JsonIgnore] public int StartYear = -1;

        /// <summary>
        /// Temporarily stores the years to show in charts.
        /// Meaningful only within the GUI
        /// </summary>
        [JsonIgnore] public int ShowYears = 1;

        /// <summary>Name of current season</summary>
        public string Season
        {
            get
            {
                if (DateUtilities.WithinDates(FirstDateOfSummer, clock.Today, FirstDateOfAutumn))
                    return "Summer";
                else if (DateUtilities.WithinDates(FirstDateOfAutumn, clock.Today, FirstDateOfWinter))
                    return "Autumn";
                else if (DateUtilities.WithinDates(FirstDateOfWinter, clock.Today, FirstDateOfSpring))
                    return "Winter";
                else
                    return "Spring";
            }
        }

        /// <summary>Returns our input file names</summary>
        public IEnumerable<string> GetReferencedFileNames()
        {
            return new string[] { FullFileName };
        }

        /// <summary>Remove all paths from referenced file names</summary>
        public void RemovePathsFromReferencedFileNames()
        {
            FileName = Path.GetFileName(FileName);
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
            if (AirPressure == 0)
                this.AirPressure = 1010;
            if (DiffuseFraction == 0)
                this.DiffuseFraction = -1;
            if (reader != null)
            {
                reader.Close();
                reader = null;
            }

            if (Latitude > 0)
            {
                // Swap summer and winter dates.
                var temp = FirstDateOfSummer;
                FirstDateOfSummer = FirstDateOfWinter;
                FirstDateOfWinter = temp;

                // Swap spring and autumn dates.
                temp = FirstDateOfSpring;
                FirstDateOfSpring = FirstDateOfAutumn;
                FirstDateOfAutumn = temp;
            }

            foreach (var message in Validate())
                summary.WriteMessage(this, message, MessageType.Warning);
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
            TodaysMetData = GetMetData(this.clock.Today); //Read first date to get todays data

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
            this.CO2 = TodaysMetData.CO2;

            if (this.PreparingNewWeatherData != null)
                this.PreparingNewWeatherData.Invoke(this, new EventArgs());

            if (clock.Today.Date == clock.StartDate.Date)
            {
                if (clock.Today.DayOfYear < WinterSolsticeDOY)
                {
                    if (DateTime.IsLeapYear(clock.Today.Year - 1))
                        DaysSinceWinterSolstice = 366 - WinterSolsticeDOY + clock.Today.DayOfYear;
                    else
                        DaysSinceWinterSolstice = 365 - WinterSolsticeDOY + clock.Today.DayOfYear;
                }
                else
                    DaysSinceWinterSolstice = clock.Today.DayOfYear - WinterSolsticeDOY;
            }
            else
            {
                if (clock.Today.DayOfYear == WinterSolsticeDOY)
                    DaysSinceWinterSolstice = 0;
                else
                    DaysSinceWinterSolstice += 1;
            }

            Qmax = MetUtilities.QMax(clock.Today.DayOfYear + 1, Latitude, MetUtilities.Taz, MetUtilities.Alpha, VP);

            // do sanity check on weather
            SensibilityCheck(clock as Clock, this);
        }

        /// <summary>Get the DataTable view of the weather data</summary>
        /// <returns>The DataTable</returns>
        public DataTable GetAllData()
        {
            this.reader = null;

            if (this.OpenDataFile())
            {
                List<string> metProps = new List<string>();
                metProps.Add("mint");
                metProps.Add("maxt");
                metProps.Add("radn");
                metProps.Add("rain");
                metProps.Add("wind");
                metProps.Add("diffr");

                return this.reader.ToTable(metProps);
            }
            else
                return null;
        }

        /// <summary>Reads the weather data for one day from file</summary>
        /// <param name="date">The date to read met data</param>
        public DailyMetDataFromFile GetMetData(DateTime date)
        {
            // check if we've looked at this date before
            WeatherRecordEntry previousEntry = null;
            foreach (WeatherRecordEntry entry in this.weatherCache)
            {
                if (entry.Date.Equals(date))
                {
                    return entry.MetData;
                }
                else if (entry.Date < date)
                {
                    previousEntry = entry;
                    break;
                }
            }

            if (this.reader == null)
                if (!this.OpenDataFile())
                    throw new ApsimXException(this, "Cannot find weather file '" + this.FileName + "'");

            // get the weather data for that date
            DailyMetDataFromFile readMetData = new DailyMetDataFromFile();
            try
            {
                this.reader.SeekToDate(date);
                readMetData.Raw = reader.GetNextLineOfData();
            }
            catch (IndexOutOfRangeException err)
            {
                throw new Exception($"Unable to retrieve weather data on {date.ToString("yyy-MM-dd")} in file {FileName}", err);
            }

            if (date != this.reader.GetDateFromValues(readMetData.Raw))
                throw new Exception("Non consecutive dates found in file: " + this.FileName + ".");

            // since this data was valid, store in our cache for next time
            WeatherRecordEntry record = new WeatherRecordEntry();
            record.Date = date;
            record.MetData = readMetData;
            if (previousEntry != null)
                this.weatherCache.AddBefore(this.weatherCache.Find(previousEntry), record);
            else
                this.weatherCache.AddFirst(record);

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

            if (co2Index == -1)
                readMetData.CO2 = 350;
            else
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

        /// <summary>Performs tasks at the start of the day</summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The arguments of the event</param>
        [EventSubscribe("StartOfDay")]
        private void OnStartOfDay(object sender, EventArgs e)
        {
            if (StartOfSummer != null && DateUtilities.DayMonthIsEqual(FirstDateOfSummer, clock.Today))
                StartOfSummer.Invoke(this, e);

            if (StartOfAutumn != null && DateUtilities.DayMonthIsEqual(FirstDateOfAutumn, clock.Today))
                StartOfAutumn.Invoke(this, e);

            if (StartOfWinter != null && DateUtilities.DayMonthIsEqual(FirstDateOfWinter, clock.Today))
                StartOfWinter.Invoke(this, e);

            if (StartOfSpring != null && DateUtilities.DayMonthIsEqual(FirstDateOfSpring, clock.Today))
                StartOfSpring.Invoke(this, e);
        }

        /// <summary>Performs the tasks for the end of the day</summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The arguments of the event</param>
        [EventSubscribe("EndOfDay")]
        private void OnEndOfDay(object sender, EventArgs e)
        {
            if (EndOfSummer != null && DateUtilities.DayMonthIsEqual(FirstDateOfAutumn, clock.Today.AddDays(1)))
                EndOfSummer.Invoke(this, e);

            if (EndOfAutumn != null && DateUtilities.DayMonthIsEqual(FirstDateOfWinter, clock.Today.AddDays(1)))
                EndOfAutumn.Invoke(this, e);

            if (EndOfWinter != null && DateUtilities.DayMonthIsEqual(FirstDateOfSpring, clock.Today.AddDays(1)))
                EndOfWinter.Invoke(this, e);

            if (EndOfSpring != null && DateUtilities.DayMonthIsEqual(FirstDateOfSummer, clock.Today.AddDays(1)))
                EndOfSpring.Invoke(this, e);
        }

        /// <summary>Opens the weather data file</summary>
        /// <returns>True if the file was successfully opened</returns>
        public bool OpenDataFile()
        {
            if (!System.IO.File.Exists(this.FullFileName) &&
                System.IO.Path.GetExtension(FullFileName) == string.Empty)
                FileName += ".met";

            if (System.IO.File.Exists(this.FullFileName))
            {
                if (this.reader == null)
                {
                    if (ExcelUtilities.IsExcelFile(FullFileName) && string.IsNullOrEmpty(ExcelWorkSheetName))
                        throw new Exception($"Unable to open excel file {FullFileName}: no sheet name is specified");

                    this.reader = new ApsimTextFile();
                    this.reader.Open(this.FullFileName, this.ExcelWorkSheetName);

                    if (this.reader.Headings == null)
                    {
                        string message = "Cannot find the expected header in ";
                        if (ExcelUtilities.IsExcelFile(FullFileName))
                            message += $"sheet '{ExcelWorkSheetName}' of ";
                        message += $"weather file: {FullFileName}";
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
                    this.co2Index = StringUtilities.IndexOfCaseInsensitive(reader.Headings, "CO2");

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
                            throw new Exception("Cannot find MaxT in weather file: " + this.FullFileName);

                    if (this.minimumTemperatureIndex == -1)
                        if (this.reader == null || this.reader.Constant("mint") == null)
                            throw new Exception("Cannot find MinT in weather file: " + this.FullFileName);

                    if (this.radiationIndex == -1)
                        if (this.reader == null || this.reader.Constant("radn") == null)
                            throw new Exception("Cannot find Radn in weather file: " + this.FullFileName);

                    if (this.rainIndex == -1)
                        if (this.reader == null || this.reader.Constant("rain") == null)
                            throw new Exception("Cannot find Rain in weather file: " + this.FullFileName);
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

        /// <summary>Closes the data file</summary>
        public void CloseDataFile()
        {
            if (reader != null)
                reader.Close();
            reader = null;
        }

        /// <summary>Read a user-defined variable from today's weather data</summary>
        /// <param name="columnName">Name of the column/variable to retrieve</param>
        public double GetValue(string columnName)
        {
            int columnIndex = StringUtilities.IndexOfCaseInsensitive(reader.Headings, columnName);
            if (columnIndex == -1)
                throw new InvalidOperationException($"Column {columnName} does not exist in {FileName}");
            return Convert.ToDouble(TodaysMetData.Raw[columnIndex], CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Calculates the values for the constants Tav and Amp for this weather file
        /// and store the values into the reader constants list.
        /// </summary>
        private void calculateTAVAMP()
        {
            double tav = 0;
            double amp = 0;

            // do the calculations
            this.processMonthlyTAVAMP(out tav, out amp);

            if (this.reader.Constant("tav") == null)
                this.reader.AddConstant("tav", tav.ToString(CultureInfo.InvariantCulture), string.Empty, string.Empty); // add a new constant
            else
                this.reader.SetConstant("tav", tav.ToString(CultureInfo.InvariantCulture));

            if (this.reader.Constant("amp") == null)
                this.reader.AddConstant("amp", amp.ToString(CultureInfo.InvariantCulture), string.Empty, string.Empty); // add a new constant
            else
                this.reader.SetConstant("amp", amp.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Calculates the values for the constants Tav and Amp for this weather file
        /// </summary>
        /// <param name="tav">The calculated tav value</param>
        /// <param name="amp">The calculated amp value</param>
        private void processMonthlyTAVAMP(out double tav, out double amp)
        {
            long savedPosition = reader.GetCurrentPosition();

            // init return values
            tav = 0;
            amp = 0;
            double maxt, mint;

            // get dataset size
            DateTime start = this.reader.FirstDate;
            DateTime last = this.reader.LastDate;
            int nyears = last.Year - start.Year + 1;

            // temp storage arrays
            double[,] monthlyMeans = new double[12, nyears];
            double[,] monthlySums = new double[12, nyears];
            int[,] monthlyDays = new int[12, nyears];

            this.reader.SeekToDate(start); // goto start of data set

            // read the daily data from the met file
            object[] values;
            DateTime curDate;
            int curMonth = 0;
            bool moreData = true;
            while (moreData)
            {
                values = this.reader.GetNextLineOfData();
                curDate = this.reader.GetDateFromValues(values);
                int yearIndex = curDate.Year - start.Year;
                maxt = Convert.ToDouble(values[this.maximumTemperatureIndex], System.Globalization.CultureInfo.InvariantCulture);
                mint = Convert.ToDouble(values[this.minimumTemperatureIndex], System.Globalization.CultureInfo.InvariantCulture);

                // accumulate the daily mean for each month
                if (curMonth != curDate.Month)
                {
                    // if next month then
                    curMonth = curDate.Month;
                    monthlySums[curMonth - 1, yearIndex] = 0;    // initialise the total
                }

                monthlySums[curMonth - 1, yearIndex] = monthlySums[curMonth - 1, yearIndex] + ((maxt + mint) * 0.5);
                monthlyDays[curMonth - 1, yearIndex]++;

                if (curDate >= last)
                    moreData = false;
            }

            // do more summary calculations
            double sumOfMeans;
            double maxMean, minMean;
            double yearlySumMeans = 0;
            double yearlySumAmp = 0;
            for (int y = 0; y < nyears; y++)
            {
                maxMean = double.MinValue;
                minMean = double.MaxValue;
                sumOfMeans = 0;
                for (int m = 0; m < 12; m++)
                {
                    monthlyMeans[m, y] = monthlySums[m, y] / monthlyDays[m, y];  // calc monthly mean
                    if (monthlyDays[m, y] != 0)
                    {
                        sumOfMeans += monthlyMeans[m, y];
                        maxMean = Math.Max(monthlyMeans[m, y], maxMean);
                        minMean = Math.Min(monthlyMeans[m, y], minMean);
                    }
                }

                if (maxMean != double.MinValue && minMean != double.MaxValue)
                {
                    yearlySumMeans += sumOfMeans / 12.0;        // accum the ave of monthly means
                    yearlySumAmp += maxMean - minMean;          // accum the amp of means
                }
            }

            tav = yearlySumMeans / nyears;  // calc the ave of the yearly ave means
            amp = yearlySumAmp / nyears;    // calc the ave of the yearly amps

            reader.SeekToPosition(savedPosition);
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
        private void SensibilityCheck(Clock clock, Weather weatherToday)
        {
            if (weatherToday.MinT > weatherToday.MaxT)
            {
                summary.WriteMessage(weatherToday, "Error: Weather on " + clock.Today.ToString() + " has higher minimum temperature (" + weatherToday.MinT + ") than maximum (" + weatherToday.MaxT + ")", MessageType.Warning);
            }
            if (weatherToday.VP <= 0)
            {
                summary.WriteMessage(weatherToday, "Error: Weather on " + clock.Today.ToString() + " has vapour pressure (" + weatherToday.VP + ") which is below 0", MessageType.Warning);
            }
            if (weatherToday.Radn < 0)
            {
                summary.WriteMessage(weatherToday, "Error: Weather on " + clock.Today.ToString() + " has negative solar radiation (" + weatherToday.Radn + ")", MessageType.Warning);
            }
            if (weatherToday.Radn > 40)
            {
                summary.WriteMessage(weatherToday, "Error: Weather on " + clock.Today.ToString() + " has solar radiation (" + weatherToday.Radn + ") which is above 40", MessageType.Warning);
            }
            if (weatherToday.Rain < 0)
            {
                summary.WriteMessage(weatherToday, "Error: Weather on " + clock.Today.ToString() + " has negative rainfall (" + weatherToday.Radn + ")", MessageType.Warning);
            }
            return;
        }

        /// <summary>Returns the weather data for a date defined by an offset from today</summary>
        /// <remarks>
        /// Will return today's data if the offset given would make the date sit outside those
        /// available in the weather file
        /// </remarks>
        /// <param name="offset">The number of days away from today</param>
        private DailyMetDataFromFile GetAdjacentMetData(int offset)
        {
            if ((this.clock.Today.Equals(this.reader.FirstDate) && offset < 0) ||
                this.clock.Today.Equals(this.reader.LastDate) && offset > 0)
            {
                summary.WriteMessage(this, "Warning: Weather on " + this.clock.Today.AddDays(offset).ToString("d") + " does not exist. Today's weather on " + this.clock.Today.ToString("d") + " was used instead.", MessageType.Warning);
                return GetMetData(this.clock.Today);
            }
            else
                return GetMetData(this.clock.Today.AddDays(offset));
        }
    }
}
