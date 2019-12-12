namespace Models
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Xml.Serialization;

    ///<summary>
    /// Reads in weather data and makes it available to other models.
    ///</summary>
    [Serializable]
    [ViewName("UserInterface.Views.TabbedMetDataView")]
    [PresenterName("UserInterface.Presenters.MetDataPresenter")]
    [ValidParent(ParentType=typeof(Simulation))]
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
        /// Gets or sets the file name. Should be relative filename where possible.
        /// </summary>
        [Summary]
        [Description("Weather file name")]
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets the full file name (with path). The user interface uses this. 
        /// </summary>
        [XmlIgnore]
        public string FullFileName
        {
            get
            {
                Simulation simulation = Apsim.Parent(this, typeof(Simulation)) as Simulation;
                if (simulation != null)
                    return PathUtilities.GetAbsolutePath(this.FileName, simulation.FileName);
                else
                {
                    Simulations simulations = Apsim.Parent(this, typeof(Simulations)) as Simulations;
                    if (simulations != null)
                        return PathUtilities.GetAbsolutePath(this.FileName, simulations.FileName);
                    else
                        return this.FileName;
                }
            }
            set
            {
                Simulations simulations = Apsim.Parent(this, typeof(Simulations)) as Simulations;
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
        [XmlIgnore]
        public double MaxT { get; set; }

        /// <summary>
        /// Gets or sets the minimum temperature (oC)
        /// </summary>
        [XmlIgnore]
        [Units("°C")]
        public double MinT { get; set; }

        /// <summary>
        /// Daily Mean temperature (oC)
        /// </summary>
        [Units("°C")]
        [XmlIgnore]
        public double MeanT { get { return (MaxT + MinT) / 2; } }

        /// <summary>
        /// Daily mean VPD (hPa)
        /// </summary>
        [Units("hPa")]
        [XmlIgnore]
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

        private int WinterSolsticeDOY
        {
            get {
                if (Latitude <= 0)
                    return 173;
                else
                    return 356;
                }
        }
        
        /// <summary>
        /// Number of days lapsed since the winter solstice
        /// </summary>
        [Units("d")]
        [XmlIgnore]
        public int DaysSinceWinterSolstice { get; set; }

        /// <summary>
        /// Maximum clear sky radiation (MJ/m2)
        /// </summary>
        [Units("MJ/M2")]
        [XmlIgnore]
        public double Qmax { get; set; }

        /// <summary>
        /// Gets or sets the rainfall (mm)
        /// </summary>
        [Units("mm")]
        [XmlIgnore]
        public double Rain { get; set; }

        /// <summary>
        /// Gets or sets the solar radiation. MJ/m2/day
        /// </summary>
        [Units("MJ/m^2/d")]
        [XmlIgnore]
        public double Radn { get; set; }

        /// <summary>
        /// Gets or sets the Pan Evaporation (mm) (Class A pan)
        /// </summary>
        [Units("mm")]
        [XmlIgnore]
        public double PanEvap { get; set; }

        /// <summary>
        /// Gets or sets the number of hours rainfall occured in
        /// </summary>
        [XmlIgnore]
        public double RainfallHours { get; set; }

        /// <summary>
        /// Gets or sets the vapor pressure (hPa)
        /// </summary>
        [Units("hPa")]
        [XmlIgnore]
        public double VP { get; set; }

        /// <summary>
        /// Gets or sets the wind value found in weather file or zero if not specified. (code says 3.0 not zero)
        /// </summary>
        [XmlIgnore]
        public double Wind { get; set; }

        /// <summary>
        /// Gets or sets the DF value found in weather file or zero if not specified
        /// </summary>
        [XmlIgnore]
        public double DiffuseFraction { get; set; }

        /// <summary>
        /// Gets or sets the Daylength value found in weather file or zero if not specified
        /// </summary>
        [XmlIgnore]
        public double DayLength { get; set; }


        /// <summary>
        /// Gets or sets the CO2 level. If not specified in the weather file the default is 350.
        /// </summary>
        [XmlIgnore]
        public double CO2 { get; set; }

        /// <summary>
        /// Gets or sets the atmospheric air pressure. If not specified in the weather file the default is 1010 hPa.
        /// </summary>
        [Units("hPa")]
        [XmlIgnore]
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

        /// <summary>
        /// Temporarily stores which tab is currently displayed.
        /// Meaningful only within the GUI
        /// </summary>
        [XmlIgnore] public int ActiveTabIndex = 0;
        /// <summary>
        /// Temporarily stores the starting date for charts.
        /// Meaningful only within the GUI
        /// </summary>
        [XmlIgnore] public int StartYear = -1;
        /// <summary>
        /// Temporarily stores the years to show in charts.
        /// Meaningful only within the GUI
        /// </summary>
        [XmlIgnore] public int ShowYears = 1;


        /// <summary>Return our input filenames</summary>
        public IEnumerable<string> GetReferencedFileNames()
        {
            return new string[] { FileName };
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
            if (reader != null)
            {
                reader.Close();
                reader = null;
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
            if (this.doSeek)
            {
                if (!this.OpenDataFile())
                    throw new ApsimXException(this, "Cannot find weather file '" + this.FileName + "'");

                this.doSeek = false;
                this.reader.SeekToDate(this.clock.Today);
            }

            object[] values = this.reader.GetNextLineOfData();

            if (this.clock.Today != this.reader.GetDateFromValues(values))
                throw new Exception("Non consecutive dates found in file: " + this.FileName + ".  Another posibility is that you have two clock objects in your simulation, there should only be one");

            if (this.radiationIndex != -1)
                this.Radn = Convert.ToSingle(values[this.radiationIndex], CultureInfo.InvariantCulture);
            else
                this.Radn = this.reader.ConstantAsDouble("radn");

            if (this.maximumTemperatureIndex != -1)
                this.MaxT = Convert.ToSingle(values[this.maximumTemperatureIndex], CultureInfo.InvariantCulture);
            else
                this.MaxT = this.reader.ConstantAsDouble("maxt");

            if (this.minimumTemperatureIndex != -1)
                this.MinT = Convert.ToSingle(values[this.minimumTemperatureIndex], CultureInfo.InvariantCulture);
            else
                this.MinT = this.reader.ConstantAsDouble("mint");

            if (this.rainIndex != -1)
                this.Rain = Convert.ToSingle(values[this.rainIndex], CultureInfo.InvariantCulture);
            else
                this.Rain = this.reader.ConstantAsDouble("rain");
				
            if (this.evaporationIndex == -1)
                this.PanEvap = double.NaN;
            else
                this.PanEvap = Convert.ToSingle(values[this.evaporationIndex], CultureInfo.InvariantCulture);

            if (this.rainfallHoursIndex == -1)
                this.RainfallHours = double.NaN;
            else
                this.RainfallHours = Convert.ToSingle(values[this.rainfallHoursIndex], CultureInfo.InvariantCulture);

            if (this.vapourPressureIndex == -1)
                this.VP = Math.Max(0, MetUtilities.svp(this.MinT));
            else
                this.VP = Convert.ToSingle(values[this.vapourPressureIndex], CultureInfo.InvariantCulture);

            if (this.windIndex == -1)
                this.Wind = 3.0;
            else
                this.Wind = Convert.ToSingle(values[this.windIndex], CultureInfo.InvariantCulture);

            if (this.DiffuseFractionIndex == -1)
                this.DiffuseFraction = -1;
            else
                this.DiffuseFraction = Convert.ToSingle(values[this.DiffuseFractionIndex], CultureInfo.InvariantCulture);

            if (this.dayLengthIndex == -1)  // Daylength is not a column - check for a constant
            {
                if (this.reader.Constant("daylength") != null)
                    this.DayLength = this.reader.ConstantAsDouble("daylength");
                else
                   this.DayLength = -1;
            }
            else
                this.DayLength = Convert.ToSingle(values[this.dayLengthIndex], CultureInfo.InvariantCulture);


            if (this.PreparingNewWeatherData != null)
                this.PreparingNewWeatherData.Invoke(this, new EventArgs());

            if (clock.Today.DayOfYear == WinterSolsticeDOY)
                DaysSinceWinterSolstice = 0;
            else DaysSinceWinterSolstice += 1;

            Qmax = MetUtilities.QMax(clock.Today.DayOfYear + 1, Latitude, MetUtilities.Taz, MetUtilities.Alpha,VP);
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
                    this.reader = new ApsimTextFile();
                    this.reader.Open(this.FullFileName, this.ExcelWorkSheetName);

                    if (this.reader.Headings == null)
                        throw new Exception("Cannot find the expected header in weather file: " + this.FullFileName);

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
                this.reader.AddConstant("tav", tav.ToString(), string.Empty, string.Empty); // add a new constant
            else
                this.reader.SetConstant("tav", tav.ToString());
 
            if (this.reader.Constant("amp") == null)
                this.reader.AddConstant("amp", amp.ToString(), string.Empty, string.Empty); // add a new constant
            else
                this.reader.SetConstant("amp", amp.ToString());
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
                    sumOfMeans += monthlyMeans[m, y];
                    maxMean = Math.Max(monthlyMeans[m, y], maxMean);
                    minMean = Math.Min(monthlyMeans[m, y], minMean);
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