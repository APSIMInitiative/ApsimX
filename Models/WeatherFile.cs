// -----------------------------------------------------------------------
// <copyright file="WeatherFile.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models
{
    using System;
    using System.Data;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Xml.Serialization;
    using Models.Core;

    /// <summary>
    /// Reads in weather data and makes it available to other models.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.TabbedMetDataView")]
    [PresenterName("UserInterface.Presenters.MetDataPresenter")]
    public class WeatherFile : Model 
    {
        /// <summary>
        /// A link to the clock model.
        /// </summary>
        [Link] 
        private Clock clock = null;

        /// <summary>
        /// A reference to the text file reader object
        /// </summary>
        private Utility.ApsimTextFile reader = null;

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
        /// The index of the vapor pressure column in the weather file
        /// </summary>
        private int vapourPressureIndex;

        /// <summary>
        /// The index of the wind column in the weather file
        /// </summary>
        private int windIndex;

        /// <summary>
        /// An instance of the weather data.
        /// </summary>
        private NewMetType todaysMetData = new NewMetType();

        /// <summary>
        /// A flag indicating whether this model should do a seek on the weather file
        /// </summary>
        private bool doSeek;

        /// <summary>
        /// This event will be invoked immediately before 'NewWeatherDataAvailable' giving
        /// models and scripts an opportunity to change the weather data before other models
        /// reads it.
        /// </summary>
        public event EventHandler PreparingNewWeatherData;

        /// <summary>
        /// This event will be invoked when new weather data is available to models.
        /// </summary>
        public event EventHandler NewWeatherDataAvailable;

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
                Simulation simulation = ParentOfType(typeof(Simulation)) as Simulation;

               // return Utility.PathUtils.GetAbsolutePath(this.FileName, simulation.FileName);
                    
                string fullFileName = this.FileName;
                if (this.FileName != null && simulation != null && simulation.FileName != null && Path.GetFullPath(this.FileName) != this.FileName)
                {
                    //fullFileName = Path.Combine(Path.GetDirectoryName(simulation.FileName), this.FileName);
                    if (!File.Exists(fullFileName))
                    {
                        fullFileName = Utility.PathUtils.GetAbsolutePath(fullFileName, simulation.FileName);
                    }
                }

                return fullFileName;
            }

            set
            {
                this.FileName = Utility.PathUtils.OSFilePath(value);
                this.reader = null; // ensure it is reopened
                // try and convert to path relative to the Simulations.FileName or ApsimX install directory.
                if (this.FileName != null)
                {
                    Simulation simulation = ParentOfType(typeof(Simulation)) as Simulation;
                    if (simulation != null && simulation.FileName != null)
                    {
                        this.FileName = Utility.PathUtils.GetRelativePath(value, simulation.FileName);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the start date of the simulation
        /// </summary>
        public DateTime StartDate
        {
            get
            {
                if (this.reader != null)
                {
                    return this.reader.FirstDate;
                }
                else
                {
                    return new DateTime(0);
                }
            }
        }

        /// <summary>
        /// Gets the end date of the simulation
        /// </summary>
        public DateTime EndDate
        {
            get
            {
                if (this.reader != null)
                {
                    return this.reader.LastDate;
                }
                else
                {
                    return new DateTime(0);
                }
            }
        }

        /// <summary>
        /// Gets the weather data as a single structure.
        /// </summary>
        public NewMetType MetData 
        { 
            get 
            { 
                return this.todaysMetData; 
            } 
        }
        
        /// <summary>
        /// Gets or sets the maximum temperature (oc)
        /// </summary>
        [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed.")]
        [XmlIgnore]
        public double MaxT 
        { 
            get 
            { 
                return this.MetData.Maxt; 
            } 
            
            set 
            { 
                this.todaysMetData.Maxt = value; 
            } 
        }
        
        /// <summary>
        /// Gets or sets the minimum temperature (oc)
        /// </summary>
        [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed.")]
        [XmlIgnore]
        public double MinT 
        { 
            get 
            { 
                return this.MetData.Mint; 
            } 

            set
            { 
                this.todaysMetData.Mint = value; 
            }
        }
        
        /// <summary>
        /// Gets or sets the rainfall (mm)
        /// </summary>
        [XmlIgnore]
        public double Rain 
        { 
            get 
            {
                return this.MetData.Rain;
            }

            set
            { 
                this.todaysMetData.Rain = value; 
            }
        }
        
        /// <summary>
        /// Gets or sets the solar radiation. MJ/m2/day
        /// </summary>
        [XmlIgnore]
        public double Radn 
        { 
            get 
            {
                return this.MetData.Radn;
            }

            set 
            { 
                this.todaysMetData.Radn = value; 
            }
        }
        
        /// <summary>
        /// Gets or sets the vapor pressure
        /// </summary>
        [XmlIgnore]
        public double VP 
        { 
            get 
            {
                return this.MetData.VP;
            }

            set 
            { 
                this.todaysMetData.VP = value; 
            }
        }
        
        /// <summary>
        /// Gets or sets the wind value found in weather file or zero if not specified.
        /// </summary>
        [XmlIgnore]
        public double Wind 
        { 
            get
                {
                    return this.MetData.Wind; 
                }

            set 
            {
                this.todaysMetData.Wind = value;
            }
        }
        
        /// <summary>
        /// Gets or sets the CO2 level. If not specified in the weather file the default is 350.
        /// </summary>
        [XmlIgnore]
        public double CO2 { get; set; }
        
        /// <summary>
        /// Gets the latitude
        /// </summary>
        public double Latitude
        {
            get
            {
                if (this.reader == null || this.reader.Constant("Latitude") == null)
                {
                    return 0;
                }
                else
                {
                    return Convert.ToDouble(this.reader.Constant("Latitude").Value);
                }
            }
        }
        
        /// <summary>
        /// Gets the average temperature
        /// </summary>
        public double Tav
        {
            get
            {
                if (this.reader == null)
                {
                    return 0;
                }
                else if (this.reader.Constant("tav") == null)
                {
                    // this constant has not been found so do a calculation
                    this.CalcTAVAMP();
                }

                return Convert.ToDouble(this.reader.Constant("tav").Value);
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
                {
                    return 0;
                }
                else if (this.reader.Constant("amp") == null)
                {
                    // this constant has not been found so do a calculation
                    this.CalcTAVAMP();
                }

                return Convert.ToDouble(this.reader.Constant("amp").Value);
            }
        }
        
        /// <summary>
        /// Gets the duration of the day in hours.
        /// </summary>
        public double DayLength
        {
            get
            {
                // APSIM uses civil twilight
                return Utility.Math.DayLength(this.clock.Today.DayOfYear, -6.0, this.Latitude); 
            }
        }

        /// <summary>
        /// Gets the 3 hourly estimates of air temperature in this daily timestep.
        /// ref: Jones, C. A., and Kiniry, J. R. (1986). "'CERES-Maize: A simulation model of maize growth
        ///      and development'." Texas A and M University Press: College Station, TX.
        /// Example of use:
        /// <code>
        /// double tot = 0;
        /// for (int period = 1; period &lt;= 8; period++)
        /// {
        ///    tot = tot + ThermalTimeFn.ValueIndexed(Temps3Hours[i]);
        /// }
        /// return tot / 8;
        /// </code>    
        /// </summary>
        [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed.")]
        public double[] Temps3Hours
        {
            get
            {
                double[] periodTemps = new double[8];
                for (int period = 1; period <= 8; period++)
                {
                    periodTemps[period - 1] = this.Temp3Hr(this.MetData.Maxt, this.MetData.Mint, period);  // get an air temperature for this period
                }

                return periodTemps;
            }
        }

        /// <summary>
        /// Overrides the base class method to allow for initialization.
        /// </summary>
        public override void OnSimulationCommencing()
        {
            this.doSeek = true;
            this.maximumTemperatureIndex = 0;
            this.minimumTemperatureIndex = 0;
            this.radiationIndex = 0;
            this.rainIndex = 0;
            this.vapourPressureIndex = 0;
            this.windIndex = 0;
            this.CO2 = 350;
        }

        /// <summary>
        /// Overrides the base class method to allow for clean up
        /// </summary>
        public override void OnSimulationCompleted()
        {
            if (this.reader != null)
            {
                this.reader.Close();
                this.reader = null;
            }
        }
        
        /// <summary>
        /// Get the DataTable view of this data
        /// </summary>
        /// <returns>The DataTable</returns>
        public DataTable GetAllData()
        {
            if (this.OpenDataFile())
            {
                return this.reader.ToTable();
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Summarize the long term yearly rainfall
        /// </summary>
        /// <param name="yearlyTotals">The yearly rainfall totals returned</param>
        /// <param name="averageMonthly">The average monthly long term values returned</param>
        public void YearlyRainfall(out double[] yearlyTotals, out double[] averageMonthly)
        {
            if (this.reader != null)
            {
                double rain;

                // get dataset size
                DateTime start = this.reader.FirstDate;
                DateTime last = this.reader.LastDate;
                int nyears = last.Year - start.Year + 1;

                // temp storage arrays
                double[,] monthlySums = new double[12, nyears];
                int[,] monthlyDays = new int[12, nyears];

                // return values
                yearlyTotals = new double[nyears];
                averageMonthly = new double[12];

                this.reader.SeekToDate(start); // goto start of data set

                // read the daily data from the met file
                object[] values;
                DateTime curDate;
                int curYear = 0;
                int curMonth = 0;
                bool moreData = true;
                while (moreData)
                {
                    values = this.reader.GetNextLineOfData();
                    curDate = this.reader.GetDateFromValues(values);
                    curMonth = curDate.Month;
                    int yearIndex = curDate.Year - start.Year;
                    rain = Convert.ToDouble(values[this.rainIndex]);

                    // accumulate the yearly rainfal
                    if (curYear != curDate.Year) 
                    {
                        // if next year then
                        curYear = curDate.Year;
                        yearlyTotals[yearIndex] = 0;    // initialise the total for next year
                        for (int m = 0; m < 12; m++)
                        {
                            monthlySums[curMonth - 1, yearIndex] = 0;
                            monthlyDays[curMonth - 1, yearIndex] = 0;
                        }
                    }

                    monthlySums[curMonth - 1, yearIndex] += rain;
                    monthlyDays[curMonth - 1, yearIndex] += 1;
                    yearlyTotals[yearIndex] += rain;
                    if (curDate >= last)
                    {
                        // if have read last record
                        moreData = false;
                    }
                }

                // now calculate the long term av month rainfall
                int yearrCount;
                double sum;
                for (int m = 0; m < 12; m++)
                {
                    sum = 0;
                    yearrCount = 0;
                    for (int y = 0; y < nyears; y++)
                    {
                        if (monthlyDays[m, y] > 0)  
                        {
                            // don't use 0 rainfall for non existent months
                            sum += monthlySums[m, y];
                            yearrCount++;
                        }
                    }

                    if (yearrCount > 0)
                    {
                        averageMonthly[m] = sum / yearrCount;
                    }
                }
            }
            else
            {
                yearlyTotals = new double[0];
                averageMonthly = new double[0];
            }
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
                {
                    throw new ApsimXException(this.FullPath, "Cannot find weather file '" + this.FileName + "'");
                }

                this.doSeek = false;
                this.reader.SeekToDate(this.clock.Today);
            }

            object[] values = this.reader.GetNextLineOfData();

            if (this.clock.Today != this.reader.GetDateFromValues(values))
            {
                throw new Exception("Non consecutive dates found in file: " + this.FileName);
            }

            this.todaysMetData.Today = this.clock.Today;
            this.todaysMetData.Radn = Convert.ToSingle(values[this.radiationIndex]);
            this.todaysMetData.Maxt = Convert.ToSingle(values[this.maximumTemperatureIndex]);
            this.todaysMetData.Mint = Convert.ToSingle(values[this.minimumTemperatureIndex]);
            this.todaysMetData.Rain = Convert.ToSingle(values[this.rainIndex]);
            if (this.vapourPressureIndex == -1)
            {
                // If VP is not present in the weather file assign a defalt value
                this.todaysMetData.VP = Math.Max(0, Utility.Met.svp(this.MetData.Mint));
            }
            else
            {
                this.todaysMetData.VP = Convert.ToSingle(values[this.vapourPressureIndex]);
            }

            if (this.windIndex == -1)
            {
                // If Wind is not present in the weather file assign a defalt value
                this.todaysMetData.Wind = 3.0;
            }
            else
            {
                this.todaysMetData.Wind = Convert.ToSingle(values[this.windIndex]);
            }

            if (this.PreparingNewWeatherData != null)
            {
                this.PreparingNewWeatherData.Invoke(this, new EventArgs());
            }

            if (this.NewWeatherDataAvailable != null)
            {
                this.NewWeatherDataAvailable.Invoke(this, new EventArgs());
            }
        }

        /// <summary>
        /// Open the weather data file.
        /// </summary>
        /// <returns>True if the file was successfully opened</returns>
        private bool OpenDataFile()
        {
            if (System.IO.File.Exists(this.FullFileName))
            {
                if (this.reader == null)
                {
                    this.reader = new Utility.ApsimTextFile();
                    this.reader.Open(this.FullFileName);
                    this.maximumTemperatureIndex = Utility.String.IndexOfCaseInsensitive(this.reader.Headings, "Maxt");
                    this.minimumTemperatureIndex = Utility.String.IndexOfCaseInsensitive(this.reader.Headings, "Mint");
                    this.radiationIndex = Utility.String.IndexOfCaseInsensitive(this.reader.Headings, "Radn");
                    this.rainIndex = Utility.String.IndexOfCaseInsensitive(this.reader.Headings, "Rain");
                    this.vapourPressureIndex = Utility.String.IndexOfCaseInsensitive(this.reader.Headings, "VP");
                    this.windIndex = Utility.String.IndexOfCaseInsensitive(this.reader.Headings, "Wind");
                    if (this.maximumTemperatureIndex == -1)
                    {
                        throw new Exception("Cannot find MaxT in weather file: " + this.FullFileName);
                    }

                    if (this.minimumTemperatureIndex == -1)
                    {
                        throw new Exception("Cannot find MinT in weather file: " + this.FullFileName);
                    }

                    if (this.radiationIndex == -1)
                    {
                        throw new Exception("Cannot find Radn in weather file: " + this.FullFileName);
                    }

                    if (this.rainIndex == -1)
                    {
                        throw new Exception("Cannot find Rain in weather file: " + this.FullFileName);
                    }
                }
                else
                {
                    this.reader.SeekToDate(this.reader.FirstDate);
                }

                return true;
            }
            else
            {
                return false;
            }
        }
        
        /// <summary>
        /// Calculate the amp and tav 'constant' values for this weather file
        /// and store the values into the File constants.
        /// </summary>
        [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed.")]
        private void CalcTAVAMP()
        {
            double tav = 0;
            double amp = 0;

            // do the calculations
            this.ProcessMonthlyTAVAMP(out tav, out amp);

            if (this.reader.Constant("tav") == null)
            {
                this.reader.AddConstant("tav", tav.ToString(), string.Empty, string.Empty); // add a new constant
            }
            else
            {
                this.reader.SetConstant("tav", tav.ToString());
            }

            if (this.reader.Constant("amp") == null)
            {
                this.reader.AddConstant("amp", amp.ToString(), string.Empty, string.Empty); // add a new constant
            }
            else
            {
                this.reader.SetConstant("amp", amp.ToString());
            }
        }
        
        /// <summary>
        /// Calculate the amp and tav 'constant' values for this weather file.
        /// </summary>
        /// <param name="tav">The calculated tav value</param>
        /// <param name="amp">The calculated amp value</param>
        [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed.")]
        private void ProcessMonthlyTAVAMP(out double tav, out double amp)
        {
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
            int[,] monthlyDays  = new int[12, nyears];

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
                maxt = Convert.ToDouble(values[this.maximumTemperatureIndex]);
                mint = Convert.ToDouble(values[this.minimumTemperatureIndex]);

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
                {
                    // if have read last record
                    moreData = false;
                }
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
        }
        
        /// <summary>
        /// An estimate of mean air temperature for the period number in this time step
        /// </summary>
        /// <param name="tempMax">The maximum temperature</param>
        /// <param name="tempMin">The minimum temperature</param>
        /// <param name="period">The period number 1 to 8</param>
        /// <returns>The mean air temperature</returns>
        private double Temp3Hr(double tempMax, double tempMin, double period)
        {
            double tempRangeFract = 0.92105 + (0.1140 * period) - 
                                           (0.0703 * Math.Pow(period, 2)) +
                                           (0.0053 * Math.Pow(period, 3));
            double diurnalRange = tempMax - tempMin;
            double deviation = tempRangeFract * diurnalRange;
            return tempMin + deviation;
        }

        /// <summary>
        /// A structure containing the commonly used weather data.
        /// </summary>
        [Serializable]
        public struct NewMetType
        {
            /// <summary>
            /// The current date
            /// </summary>
            public DateTime Today;

            /// <summary>
            /// Solar radiation. MJ/m2/day
            /// </summary>
            public double Radn;

            /// <summary>
            /// Maximum temperature (oc)
            /// </summary>
            [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed.")]
            public double Maxt;

            /// <summary>
            /// Minimum temperature (oc)
            /// </summary>
            [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed.")]
            public double Mint;

            /// <summary>
            /// Rainfall (mm)
            /// </summary>
            public double Rain;

            /// <summary>
            /// The vapor pressure
            /// </summary>
            public double VP;

            /// <summary>
            /// The wind value found in weather file or zero if not specified.
            /// </summary>
            public double Wind;
        }
    }
}