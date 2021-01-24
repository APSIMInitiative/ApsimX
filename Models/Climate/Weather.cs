namespace Models.Climate
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Interfaces;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;

    ///<summary>
    /// Reads in weather data and makes it available to other models.
    ///</summary>
    [Serializable]
    [ViewName("UserInterface.Views.TabbedMetDataView")]
    [PresenterName("UserInterface.Presenters.MetDataPresenter")]
    [ValidParent(ParentType=typeof(Simulation))]
    [ValidParent(ParentType = typeof(Zone))]
    public class Weather : Model, IWeather, IReferenceExternalFiles
    {
        /// <summary>
        /// A link to the clock model.
        /// </summary>
        [Link]
        private Clock clock = null;

        /// <summary>
        /// A reference to the text file reader object
        /// </summary>
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
        /// The index of the evaporation column in the weather file
        /// </summary>
        private int rainfallHoursIndex;

        /// <summary>
        /// The index of the vapor pressure column in the weather file
        /// </summary>
        private int vapourPressureIndex;

        /// <summary>
        /// The index of the wind column in the weather file
        /// </summary>
        private int windIndex;

        /// <summary>
        /// The index of the DiffuseFraction column in the weather file
        /// </summary>
        private int DiffuseFractionIndex;

        /// <summary>
        /// The index of the DayLength column in the weather file
        /// </summary>
        private int dayLengthIndex;


        /// <summary>
        /// A flag indicating whether this model should do a seek on the weather file
        /// </summary>
        private bool doSeek;

        /// <summary>
        /// This event will be invoked immediately before models get their weather data.
        /// models and scripts an opportunity to change the weather data before other models
        /// reads it.
        /// </summary>
        public event EventHandler PreparingNewWeatherData;

        /// <summary>
        /// Optional constants file name. This should only be accessed via
        /// <see cref="ConstantsFile" />, which handles conversion between
        /// relative/absolute paths.
        /// </summary>
        private string constantsFile;

        /// <summary>
        /// Allows to specify a second file which contains constants such as lat, long,
        /// tav, amp, etc. Really only used when the actual met data is in a .csv file.
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
        /// Gets or sets the file name. Should be relative filename where possible.
        /// </summary>
        [Summary]
        [Description("Weather file name")]
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the full file name (with path). The user interface uses this. 
        /// </summary>
        [JsonIgnore]
        public string FullFileName
        {
            get
            {
                Simulation simulation = FindAncestor<Simulation>();
                if (simulation != null)
                    return PathUtilities.GetAbsolutePath(this.FileName, simulation.FileName);
                else
                {
                    Simulations simulations = FindAncestor<Simulations>();
                    if (simulations != null)
                        return PathUtilities.GetAbsolutePath(this.FileName, simulations.FileName);
                    else
                        return this.FileName;
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
        /// Used to hold the WorkSheet Name if data retrieved from an Excel file
        /// </summary>
        public string ExcelWorkSheetName { get; set; }

        /// <summary>
        /// Gets the start date of the weather file
        /// </summary>
        public DateTime StartDate
        {
            get
            {
                if (this.reader == null && !this.OpenDataFile())
                    return new DateTime(0);

                return this.reader.FirstDate;
            }
        }

        /// <summary>
        /// Gets the end date of the weather file
        /// </summary>
        public DateTime EndDate
        {
            get
            {
                if (this.reader == null && !this.OpenDataFile())
                    return new DateTime(0);

                return this.reader.LastDate;
            }
        }

        /// <summary>
        /// Gets or sets the maximum temperature (oC)
        /// </summary>
        [Units("°C")]
        [JsonIgnore]
        public double MaxT { get; set; }

        /// <summary>
        /// Gets or sets the minimum temperature (oC)
        /// </summary>
        [JsonIgnore]
        [Units("°C")]
        public double MinT { get; set; }

        /// <summary>
        /// Daily Mean temperature (oC)
        /// </summary>
        [Units("°C")]
        [JsonIgnore]
        public double MeanT { get { return (MaxT + MinT) / 2; } }

        /// <summary>
        /// Daily mean VPD (hPa)
        /// </summary>
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

        /// <summary>
        /// days since winter solstice (day)
        /// </summary>
        [Units("day")]
        [JsonIgnore]
        public int WinterSolsticeDOY
        {
            get {
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

        private bool First = true;
        /// <summary>
        /// Number of days lapsed since the winter solstice
        /// </summary>
        [Units("d")]
        [JsonIgnore]
        public int DaysSinceWinterSolstice { get; set; }

        /// <summary>
        /// Maximum clear sky radiation (MJ/m2)
        /// </summary>
        [Units("MJ/M2")]
        [JsonIgnore]
        public double Qmax { get; set; }

        /// <summary>
        /// Gets or sets the rainfall (mm)
        /// </summary>
        [Units("mm")]
        [JsonIgnore]
        public double Rain { get; set; }

        /// <summary>
        /// Gets or sets the solar radiation. MJ/m2/day
        /// </summary>
        [Units("MJ/m^2/d")]
        [JsonIgnore]
        public double Radn { get; set; }

        /// <summary>
        /// Gets or sets the Pan Evaporation (mm) (Class A pan)
        /// </summary>
        [Units("mm")]
        [JsonIgnore]
        public double PanEvap { get; set; }

        /// <summary>
        /// Gets or sets the number of hours rainfall occured in
        /// </summary>
        [JsonIgnore]
        public double RainfallHours { get; set; }

        /// <summary>
        /// Gets or sets the vapor pressure (hPa)
        /// </summary>
        [Units("hPa")]
        [JsonIgnore]
        public double VP { get; set; }

        /// <summary>
        /// Gets or sets the wind value found in weather file or zero if not specified. (code says 3.0 not zero)
        /// </summary>
        [JsonIgnore]
        public double Wind { get; set; }

        /// <summary>
        /// Gets or sets the DF value found in weather file or zero if not specified
        /// </summary>
        [Units("0-1")]
        [JsonIgnore]
        public double DiffuseFraction { get; set; }

        /// <summary>
        /// Gets or sets the Daylength value found in weather file or zero if not specified
        /// </summary>
        [JsonIgnore]
        public double DayLength { get; set; }


        /// <summary>
        /// Gets or sets the CO2 level. If not specified in the weather file the default is 350.
        /// </summary>
        [JsonIgnore]
        public double CO2 { get; set; }

        /// <summary>
        /// Gets or sets the atmospheric air pressure. If not specified in the weather file the default is 1010 hPa.
        /// </summary>
        [Units("hPa")]
        [JsonIgnore]
        public double AirPressure { get; set; }

        /// <summary>
        /// Gets the latitude
        /// </summary>
        public double Latitude
        {
            get
            {
                if (this.reader == null && !this.OpenDataFile())
                    return 0;

                return this.reader.ConstantAsDouble("Latitude");
            }
        }

        /// <summary>
        /// Gets the longitude
        /// </summary>
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

        /// <summary>
        /// Gets the average temperature
        /// </summary>
        [Units("°C")]
        public double Tav
        {
            get
            {
                if (this.reader == null)
                    return 0;
                else if (this.reader.Constant("tav") == null)
                    this.CalcTAVAMP();

                return this.reader.ConstantAsDouble("tav");
            }
        }

        /// <summary>
        /// Gets the temperature amplitude.
        /// </summary>
        public double Amp
        {
            get
            {
                if (this.reader == null)
                    return 0;
                else if (this.reader.Constant("amp") == null)
                    this.CalcTAVAMP();

                return this.reader.ConstantAsDouble("amp");
            }
        }

        /// <summary>Met Data from yesterday</summary>
        [JsonIgnore]
        public DailyMetDataFromFile YesterdaysMetData { get; set; }

        /// <summary>Met Data for Today</summary>
        [JsonIgnore]
        public DailyMetDataFromFile TodaysMetData { get; set; }

        /// <summary>Met Data from yesterday</summary>
        [JsonIgnore]
        public DailyMetDataFromFile TomorrowsMetData { get; set; }

        /// <summary>First date of summer.</summary>
        [JsonIgnore] 
        public string FirstDateOfSummer { get; set; } = "1-dec";

        /// <summary>First date of autumn / fall.</summary>
        [JsonIgnore] 
        public string FirstDateOfAutumn { get; set; } = "1-mar";

        /// <summary>First date of winter.</summary>
        [JsonIgnore] 
        public string FirstDateOfWinter { get; set; } = "1-jun";

        /// <summary>First date of spring.</summary>
        [JsonIgnore] 
        public string FirstDateOfSpring { get; set; } = "1-sep";

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

        /// <summary>Start of spring event.</summary>
        public event EventHandler StartOfSpring;

        /// <summary>Start of summer event.</summary>
        public event EventHandler StartOfSummer;

        /// <summary>Start of autumn/fall event.</summary>
        public event EventHandler StartOfAutumn;

        /// <summary>Start of winter event.</summary>
        public event EventHandler StartOfWinter;

        /// <summary>End of spring event.</summary>
        public event EventHandler EndOfSpring;

        /// <summary>End of summer event.</summary>
        public event EventHandler EndOfSummer;

        /// <summary>End of autumn/fall event.</summary>
        public event EventHandler EndOfAutumn;

        /// <summary>End of winter event.</summary>
        public event EventHandler EndOfWinter;

        /// <summary>Name of current season.</summary>
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

        /// <summary>Return our input filenames</summary>
        public IEnumerable<string> GetReferencedFileNames()
        {
            return new string[] { FullFileName };
        }

        /// <summary>Remove all paths from referenced filenames.</summary>
        public void RemovePathsFromReferencedFileNames()
        {
            FileName = Path.GetFileName(FileName);
        }

        /// <summary>
        /// Gets the duration of the day in hours.
        /// </summary>
        public double CalculateDayLength(double Twilight)
        {
            if (dayLengthIndex == -1 && DayLength == -1)  // daylength is not set as column or constant
                return MathUtilities.DayLength(this.clock.Today.DayOfYear, Twilight, this.Latitude);
            else
                return this.DayLength;
        }

        /// <summary> calculate the time of sun rise</summary>
        /// <returns>Sun rise time</returns>
        public double CalculateSunRise()
        {
            return 12 - CalculateDayLength(-6) / 2;
        }

        /// <summary> calculate the time of sun set</summary>
        /// <returns>Sun set time</returns>
        public double CalculateSunSet()
        {
            return 12 + CalculateDayLength(-6)/2;
        }

        /// <summary>
        /// Overrides the base class method to allow for initialization.
        /// </summary>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {
            this.doSeek = true;
            this.maximumTemperatureIndex = 0;
            this.minimumTemperatureIndex = 0;
            this.radiationIndex = 0;
            this.rainIndex = 0;
            this.evaporationIndex = 0;
            this.rainfallHoursIndex = 0;
            this.vapourPressureIndex = 0;
            this.windIndex = 0;
            this.DiffuseFractionIndex = 0;
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
        }

        /// <summary>
        /// Overrides the base class method to allow for clean up
        /// </summary>
        [EventSubscribe("Completed")]
        private void OnSimulationCompleted(object sender, EventArgs e)
        {
            if (this.reader != null)
                this.reader.Close();
            this.reader = null;            
        }

        /// <summary>
        /// Get the DataTable view of this data
        /// </summary>
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

        /// <summary>
        /// An event handler for the daily DoWeather event.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The arguments of the event</param>
        [EventSubscribe("DoWeather")]
        private void OnDoWeather(object sender, EventArgs e)
        {
            if (First)
            {
                TodaysMetData = GetMetData(this.clock.Today); //Read first date to get todays data
                YesterdaysMetData = TodaysMetData; //Use todays Data to represent yesterday on the first day
                TomorrowsMetData = GetMetData(this.clock.Today.AddDays(1)); // Read another line ahead to get tomorrows data
            }
            else
            { // Move everything forward a day
                YesterdaysMetData = TodaysMetData;
                TodaysMetData = TomorrowsMetData;
                
                if (clock.Today == clock.EndDate && clock.EndDate == reader.LastDate)
                    TomorrowsMetData = TodaysMetData;
                else
                    TomorrowsMetData = GetMetData(this.clock.Today.AddDays(1));
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
            
            if (this.PreparingNewWeatherData != null)
                this.PreparingNewWeatherData.Invoke(this, new EventArgs());

            if (First)
            {
                //StartDAWS = met.DaysSinceWinterSolstice;
                if (clock.Today.DayOfYear < WinterSolsticeDOY)
                {
                    if (DateTime.IsLeapYear(clock.Today.Year-1))
                        DaysSinceWinterSolstice = 366 - WinterSolsticeDOY + clock.Today.DayOfYear;  
                    else
                        DaysSinceWinterSolstice = 365 - WinterSolsticeDOY + clock.Today.DayOfYear; 
                }
                else
                    DaysSinceWinterSolstice = clock.Today.DayOfYear - WinterSolsticeDOY;
                    
                First = false;
            }

            if (clock.Today.DayOfYear == WinterSolsticeDOY & First == false)
                DaysSinceWinterSolstice = 0;
            else DaysSinceWinterSolstice += 1;

            Qmax = MetUtilities.QMax(clock.Today.DayOfYear + 1, Latitude, MetUtilities.Taz, MetUtilities.Alpha,VP);
        }

        /// <summary>Method to read one days met data in from file</summary>
        /// <param name="date">the date to read met data</param>
        private DailyMetDataFromFile GetMetData(DateTime date)
        {
            if (this.doSeek)
            {
                if (!this.OpenDataFile())
                    throw new ApsimXException(this, "Cannot find weather file '" + this.FileName + "'");

                this.doSeek = false;
                this.reader.SeekToDate(date);
            }

            object[] values;

            DailyMetDataFromFile readMetData = new DailyMetDataFromFile();

            try
            {
                values = this.reader.GetNextLineOfData();
            }
            catch (IndexOutOfRangeException err)
            {
                throw new Exception($"Unable to retrieve weather data on {date.ToString("yyy-MM-dd")} in file {FileName}", err);
            }

            if (date != this.reader.GetDateFromValues(values))
                throw new Exception("Non consecutive dates found in file: " + this.FileName + ".  Another posibility is that you have two clock objects in your simulation, there should only be one");

            if (this.radiationIndex != -1)
                readMetData.Radn = Convert.ToSingle(values[this.radiationIndex], CultureInfo.InvariantCulture);
            else
                readMetData.Radn = this.reader.ConstantAsDouble("radn");

            if (this.maximumTemperatureIndex != -1)
                readMetData.MaxT = Convert.ToSingle(values[this.maximumTemperatureIndex], CultureInfo.InvariantCulture);
            else
                readMetData.MaxT = this.reader.ConstantAsDouble("maxt");

            if (this.minimumTemperatureIndex != -1)
                readMetData.MinT = Convert.ToSingle(values[this.minimumTemperatureIndex], CultureInfo.InvariantCulture);
            else
                readMetData.MinT = this.reader.ConstantAsDouble("mint");

            if (this.rainIndex != -1)
                readMetData.Rain = Convert.ToSingle(values[this.rainIndex], CultureInfo.InvariantCulture);
            else
                readMetData.Rain = this.reader.ConstantAsDouble("rain");

            if (this.evaporationIndex == -1)
                readMetData.PanEvap = double.NaN;
            else
                readMetData.PanEvap = Convert.ToSingle(values[this.evaporationIndex], CultureInfo.InvariantCulture);

            if (this.rainfallHoursIndex == -1)
                readMetData.RainfallHours = double.NaN;
            else
                readMetData.RainfallHours = Convert.ToSingle(values[this.rainfallHoursIndex], CultureInfo.InvariantCulture);

            if (this.vapourPressureIndex == -1)
                readMetData.VP = Math.Max(0, MetUtilities.svp(readMetData.MinT));
            else
                readMetData.VP = Convert.ToSingle(values[this.vapourPressureIndex], CultureInfo.InvariantCulture);

            if (this.windIndex == -1)
                readMetData.Wind = 3.0;
            else
                readMetData.Wind = Convert.ToSingle(values[this.windIndex], CultureInfo.InvariantCulture);

            if (this.DiffuseFractionIndex == -1)
                readMetData.DiffuseFraction = -1;
            else
                readMetData.DiffuseFraction = Convert.ToSingle(values[this.DiffuseFractionIndex], CultureInfo.InvariantCulture);

            if (this.dayLengthIndex == -1)  // Daylength is not a column - check for a constant
            {
                if (this.reader.Constant("daylength") != null)
                    readMetData.DayLength = this.reader.ConstantAsDouble("daylength");
                else
                    readMetData.DayLength = -1;
            }
            else
                readMetData.DayLength = Convert.ToSingle(values[this.dayLengthIndex], CultureInfo.InvariantCulture);

            return readMetData;
        }

        /// <summary>
        /// An event handler for the start of day event.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The arguments of the event</param>
        [EventSubscribe("StartOfDay")]
        private void OnStartOfDay(object sender, EventArgs e)
        {
            if (StartOfSummer != null && DateUtilities.DatesEqual(FirstDateOfSummer, clock.Today))
                StartOfSummer.Invoke(this, e);

            if (StartOfAutumn != null && DateUtilities.DatesEqual(FirstDateOfAutumn, clock.Today))
                StartOfAutumn.Invoke(this, e);

            if (StartOfWinter != null && DateUtilities.DatesEqual(FirstDateOfWinter, clock.Today))
                StartOfWinter.Invoke(this, e);

            if (StartOfSpring != null && DateUtilities.DatesEqual(FirstDateOfSpring, clock.Today))
                StartOfSpring.Invoke(this, e);
        }

        /// <summary>
        /// An event handler for the end of day event.
        /// </summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The arguments of the event</param>
        [EventSubscribe("EndOfDay")]
        private void OnEndOfDay(object sender, EventArgs e)
        {
            if (EndOfSummer != null && DateUtilities.DatesEqual(FirstDateOfAutumn, clock.Today.AddDays(1)))
                EndOfSummer.Invoke(this, e);

            if (EndOfAutumn != null && DateUtilities.DatesEqual(FirstDateOfWinter, clock.Today.AddDays(1)))
                EndOfAutumn.Invoke(this, e);

            if (EndOfWinter != null && DateUtilities.DatesEqual(FirstDateOfSpring, clock.Today.AddDays(1)))
                EndOfWinter.Invoke(this, e);

            if (EndOfSpring != null && DateUtilities.DatesEqual(FirstDateOfSummer, clock.Today.AddDays(1)))
                EndOfSpring.Invoke(this, e);
        }

        /// <summary>
        /// Open the weather data file.
        /// </summary>
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
                    this.DiffuseFractionIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, "DifFr");
                    this.dayLengthIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, "DayLength");

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

        /// <summary>Close the datafile.</summary>
        public void CloseDataFile()
        {
            if (reader != null)
                reader.Close();
            reader = null;
        }

        /// <summary>
        /// Calculate the amp and tav 'constant' values for this weather file
        /// and store the values into the File constants.
        /// </summary>
        private void CalcTAVAMP()
        {
            double tav = 0;
            double amp = 0;

            // do the calculations
            this.ProcessMonthlyTAVAMP(out tav, out amp);

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
        /// Calculate the amp and tav 'constant' values for this weather file.
        /// </summary>
        /// <param name="tav">The calculated tav value</param>
        /// <param name="amp">The calculated amp value</param>
        private void ProcessMonthlyTAVAMP(out double tav, out double amp)
        {
            int savedPosition = reader.GetCurrentPosition();

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
                maxMean = -999;
                minMean = 999;
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

                yearlySumMeans += sumOfMeans / 12.0;        // accum the ave of monthly means
                yearlySumAmp += maxMean - minMean;          // accum the amp of means
            }

            tav = yearlySumMeans / nyears;  // calc the ave of the yearly ave means
            amp = yearlySumAmp / nyears;    // calc the ave of the yearly amps

            reader.SeekToPosition(savedPosition);
        }
    }
}