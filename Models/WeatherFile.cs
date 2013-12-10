using System;
using System.Data;
using Models.Core;
using System.IO;
using System.Xml.Serialization;

namespace Models
{
    /// <summary>
    /// Reads in met data and makes it available for other components.
    /// </summary>
    [ViewName("UserInterface.Views.TabbedMetDataView")]
    [PresenterName("UserInterface.Presenters.MetDataPresenter")]
    public class WeatherFile : Model 
    {
        // Links
        [Link] private Clock Clock = null;
        [Link] private Simulations Simulations = null;

        // Privates
        private Utility.ApsimTextFile WtrFile = null;
        private DataTable WtrData = new DataTable();
        private int MaxTIndex;
        private int MinTIndex;
        private int RadnIndex;
        private int RainIndex;
        private NewMetType TodaysMetData = new NewMetType();
        private bool DoSeek;

        public WeatherFile()
        {
        }

        /// <summary>
        /// FileName parameter read in. Should be relative filename where possible.
        /// </summary>
        public string FileName { get; set;}

        // A property providing a full file name. The user interface uses this.
        [XmlIgnore]
        public string FullFileName
        {
            get
            {
                string FullFileName = FileName;
                if (Simulations.FileName != null && Path.GetFullPath(FileName) != FileName)
                    FullFileName = Path.Combine(Path.GetDirectoryName(Simulations.FileName), FileName);
                return FullFileName;
            }
            set
            {
                FileName = value;
               
                // try and convert to path relative to the Simulations.FileName.
                FileName = FileName.Replace(Path.GetDirectoryName(Simulations.FileName) + @"\", "");
            }
        }

        public DateTime StartDate
        {
            get
            {
                if (WtrFile != null)
                    return WtrFile.FirstDate;
                else
                    return new DateTime(0);
            }
        }
        public DateTime EndDate
        {
            get
            {
                if (WtrFile != null)
                    return WtrFile.LastDate;
                else
                    return new DateTime(0);
            }
        }
        // Events
        public struct NewMetType
        {
            public double today;
            public float radn;
            public float maxt;
            public float mint;
            public float rain;
            public float vp;
        }
        public delegate void NewMetDelegate(NewMetType Data); 
        public event NewMetDelegate NewMet;

        // Outputs
        public NewMetType MetData { get { return TodaysMetData; } }
        public double MaxT { get { return MetData.maxt; } }
        public double MinT { get { return MetData.mint; } }
        public double Rain { get { return MetData.rain; } }
        public double Radn { get { return MetData.radn; } }
        public double vp { get { return MetData.vp; } }
        public double Latitude
        {
            get
            {
                if (WtrFile.Constant("Latitude") == null)
                    return 0;
                else
                    return Convert.ToDouble(WtrFile.Constant("Latitude").Value);
            }
        }
        public double Tav
        {
            get
            {
                if (WtrFile.Constant("tav") == null)
                {
                    //this constant has not been found so do a calculation
                    CalcTAVAMP();
                }
                return Convert.ToDouble(WtrFile.Constant("tav").Value);
            }
        }
        public double Amp
        {
            get
            {
                if (WtrFile.Constant("amp") == null)
                {
                    //this constant has not been found so do a calculation
                    CalcTAVAMP();
                }
                return Convert.ToDouble(WtrFile.Constant("amp").Value);
            }
        }
        //=====================================================================
        /// <summary>
        /// Return the duration of the day in hours.
        /// </summary>
        public double DayLength
        {
            get
            {
                //APSIM uses civil twilight
                return Utility.Math.DayLength(Clock.Today.DayOfYear, -6.0, Latitude); 
            }
        }
        //=====================================================================
        /// <summary>
        /// Return the 3 hourly estimates of air temperature in this daily timestep.
        /// ref: Jones, C. A., and Kiniry, J. R. (1986). "'CERES-Maize: A simulation model of maize growth
        ///      and development'." Texas A&M University Press: College Station, TX.
        /// Example of use:
        /// <code>
        /// double tot = 0;
        /// for (int period = 1; period &lt;= 8; period++)
        /// {
        ///    tot = tot + ThermalTimeFn.ValueIndexed(Temps3Hours[i]);
        /// }
        /// return tot / 8;
        ///</code>    
        /// </summary>
        public double[] Temps3Hours
        {
            get
            {
                return CalcPeriodTemps();
            }
        }
        public void YearlyRainfall(out double[] YearlyTotals, out double[] LTAvMonthly)
        {
            YearlyRain(out YearlyTotals, out LTAvMonthly);
        }
        //=====================================================================
        /// <summary>
        /// Open the specified weather data file.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private Boolean OpenDataFile()
        {
            if (System.IO.File.Exists(FullFileName))
            {
                WtrFile = new Utility.ApsimTextFile();
                WtrFile.Open(FullFileName);
                MaxTIndex = Utility.String.IndexOfCaseInsensitive(WtrFile.Headings, "Maxt");
                MinTIndex = Utility.String.IndexOfCaseInsensitive(WtrFile.Headings, "Mint");
                RadnIndex = Utility.String.IndexOfCaseInsensitive(WtrFile.Headings, "Radn");
                RainIndex = Utility.String.IndexOfCaseInsensitive(WtrFile.Headings, "Rain");
                if (MaxTIndex == -1)
                    throw new Exception("Cannot find MaxT in weather file: " + FullFileName);
                if (MinTIndex == -1)
                    throw new Exception("Cannot find MinT in weather file: " + FullFileName);
                if (RadnIndex == -1)
                    throw new Exception("Cannot find Radn in weather file: " + FullFileName);
                if (RainIndex == -1)
                    throw new Exception("Cannot find Rain in weather file: " + FullFileName);

                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// An event handler to allow use to initialise ourselves.
        /// </summary>
        [EventSubscribe("Initialised")]
        private void OnInitialised(object sender, EventArgs e)
        {
            DoSeek = true; 
        }

        /// <summary>
        /// An event handler for the tick event.
        /// </summary>
        [EventSubscribe("Tick")]
        private void OnTick(object sender, EventArgs e)
        {
            if (DoSeek)
            {
                if (!OpenDataFile())
                    throw new ApsimXException(this.FullPath, "Cannot find weather file '" + FileName + "'");

                DoSeek = false;
                WtrFile.SeekToDate(Clock.Today);
            }

            object[] Values = WtrFile.GetNextLineOfData();

            if (Clock.Today != WtrFile.GetDateFromValues(Values))
                throw new Exception("Non consecutive dates found in file: " + FileName);

            TodaysMetData.today = (double)Clock.Today.Ticks;
            TodaysMetData.radn = Convert.ToSingle(Values[RadnIndex]);
            TodaysMetData.maxt = Convert.ToSingle(Values[MaxTIndex]);
            TodaysMetData.mint = Convert.ToSingle(Values[MinTIndex]);
            TodaysMetData.rain = Convert.ToSingle(Values[RainIndex]);
            if (NewMet != null)
                NewMet.Invoke(MetData);
        }
        //=====================================================================
        /// <summary>
        /// Simulation has terminated. Perform cleanup.
        /// </summary>
        [EventSubscribe("Completed")]
        private void OnCompleted(object sender, EventArgs e)
        {
            Clock.Tick -= OnTick;
           // Simulation.Completed -= OnCompleted;
            WtrFile.Close();
            WtrFile = null;
        }
        //=====================================================================
        /// <summary>
        /// Calculate the amp and tav 'constant' values for this metfile
        /// and store the values into the File constants.
        /// </summary>
        private void CalcTAVAMP()
        {
            double tav = 0;
            double amp = 0;

            //do the calculations
            ProcessMonthlyTAVAMP(out tav, out amp);

            if (WtrFile.Constant("tav") == null)
            {
                WtrFile.AddConstant("tav", tav.ToString(), "", ""); //add a new constant
            }
            else
                WtrFile.SetConstant("tav", tav.ToString());

            if (WtrFile.Constant("amp") == null)
            {
                WtrFile.AddConstant("amp", amp.ToString(), "", ""); //add a new constant
            }
            else
                WtrFile.SetConstant("amp", amp.ToString());
        }
        //=====================================================================
        /// <summary>
        /// Calculate the amp and tav 'constant' values for this metfile.
        /// </summary>
        /// <param name="tav">The calculated tav value</param>
        /// <param name="amp">The calculated amp value</param>
        private void ProcessMonthlyTAVAMP(out double tav, out double amp)
        {
            //init return values
            tav = 0;
            amp = 0;
            double maxt, mint;

            //get dataset size
            DateTime start = WtrFile.FirstDate;
            DateTime last = WtrFile.LastDate;
            int nyears = last.Year - start.Year + 1;
            //temp storage arrays
            double[,] monthlyMeans = new double[12, nyears];
            double[,] monthlySums = new double[12, nyears];
            int[,] monthlyDays  = new int[12, nyears];

            WtrFile.SeekToDate(start); //goto start of data set

            //read the daily data from the met file
            object[] Values;
            DateTime curDate;
            int curMonth = 0;
            Boolean moreData = true;
            while (moreData)
            {
                Values = WtrFile.GetNextLineOfData();
                curDate = WtrFile.GetDateFromValues(Values);
                int yrIdx = curDate.Year - start.Year;
                maxt = Convert.ToDouble(Values[MaxTIndex]);
                mint = Convert.ToDouble(Values[MinTIndex]);
                //accumulate the daily mean for each month
                if (curMonth != curDate.Month) //if next month then
                {
                    curMonth = curDate.Month;
                    monthlySums[curMonth - 1, yrIdx] = 0;    //initialise the total
                }
                monthlySums[curMonth - 1, yrIdx] = monthlySums[curMonth - 1, yrIdx] + ((maxt + mint) * 0.5);
                monthlyDays[curMonth - 1, yrIdx]++;

                if (curDate >= last)    //if have read last record
                    moreData = false;
            }

            //do more summary calculations
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
                    monthlyMeans[m, y] = monthlySums[m, y] / monthlyDays[m, y];  //calc monthly mean
                    sumOfMeans += monthlyMeans[m, y];
                    maxMean = Math.Max(monthlyMeans[m, y], maxMean);
                    minMean = Math.Min(monthlyMeans[m, y], minMean);
                }
                yearlySumMeans += sumOfMeans / 12.0;        //accum the ave of monthly means
                yearlySumAmp += maxMean - minMean;          //accum the amp of means
            }

            tav = yearlySumMeans / nyears;  //calc the ave of the yearly ave means
            amp = yearlySumAmp / nyears;    //calc the ave of the yearly amps
        }
        //=====================================================================
        /// <summary>
        /// Get the DataTable view of this data
        /// </summary>
        /// <returns>The DataTable</returns>
        public DataTable GetAllData()
        {
            if (OpenDataFile())
            {
                WtrData = WtrFile.ToTable();
                return WtrData;
            }
            else
                return null;
        }
        //=====================================================================
        /// <summary>
        /// Interpolations of the air temperature for 3 hourly intervals.
        /// ref: Jones, C. A., and Kiniry, J. R. (1986). "'CERES-Maize: A simulation model of maize growth
        ///      and development'"
        /// Refactored from Phenology.cpp (Sorghum model)
        /// </summary>
        /// <returns>Array of air mean temperatures</returns>
        private double[] CalcPeriodTemps()
        {
            double[] PeriodTemps = new double[8];
            for (int period = 1; period <= 8; period++)
            {
                PeriodTemps[period - 1] = temp3Hr(MetData.maxt, MetData.mint, period);  // get an air temperature for this period
            }
            return PeriodTemps;
        }
        //=====================================================================
        /// <summary>
        /// An estimate of mean air temperature for the period number in this timestep
        /// </summary>
        /// <param name="tMax"></param>
        /// <param name="tMin"></param>
        /// <param name="period"></param>
        /// <returns></returns>
        private double temp3Hr (double tMax, double tMin, double period)
        {
            double tRangeFract = 0.92105 + 0.1140 * period - 
                                           0.0703 * Math.Pow(period,2) +
                                           0.0053 * Math.Pow(period,3);
            double diurnalRange = tMax - tMin;
            double deviation = tRangeFract * diurnalRange;
            return  (tMin + deviation);
        }
        /// <summary>
        /// Summarise the long term yearly rainfall
        /// </summary>
        /// <param name="YearlyTotals"></param>
        /// <param name="LTAvMonthly"></param>
        private void YearlyRain(out double[] YearlyTotals, out double[] LTAvMonthly)
        {
            double rain;

            //get dataset size
            DateTime start = WtrFile.FirstDate;
            DateTime last = WtrFile.LastDate;
            int nyears = last.Year - start.Year + 1;
            //temp storage arrays
            double[,] monthlySums = new double[12, nyears];
            int[,] monthlyDays = new int[12, nyears];
            //return values
            YearlyTotals = new double[nyears];
            LTAvMonthly = new double[12];

            WtrFile.SeekToDate(start); //goto start of data set

            //read the daily data from the met file
            object[] Values;
            DateTime curDate;
            int curYear = 0;
            int curMonth = 0;
            Boolean moreData = true;
            while (moreData)
            {
                Values = WtrFile.GetNextLineOfData();
                curDate = WtrFile.GetDateFromValues(Values);
                curMonth = curDate.Month;
                int yrIdx = curDate.Year - start.Year;
                rain = Convert.ToDouble(Values[RainIndex]);
                //accumulate the yearly rainfal
                if (curYear != curDate.Year) //if next year then
                {
                    curYear = curDate.Year;
                    YearlyTotals[yrIdx] = 0;    //initialise the total for next year
                    for (int m = 0; m < 12; m++)
                    {
                        monthlySums[curMonth - 1, yrIdx] = 0;
                        monthlyDays[curMonth - 1, yrIdx] = 0;
                    }
                }
                monthlySums[curMonth - 1, yrIdx] += rain;
                monthlyDays[curMonth - 1, yrIdx] += 1;
                YearlyTotals[yrIdx] += rain;
                if (curDate >= last)    //if have read last record
                    moreData = false;
            }
            //now calculate the long term av month rainfall
            int yrCount;
            double sum;
            for (int m = 0; m < 12; m++)
            {
                sum = 0;
                yrCount = 0;
                for (int y = 0; y < nyears; y++)
                {
                    if (monthlyDays[m, y] > 0)  //don't use 0 rainfall for non existent months
                    {
                        sum += monthlySums[m, y];
                        yrCount++;
                    }
                }
                if (yrCount > 0)
                    LTAvMonthly[m] = sum / yrCount;
            }
        }
    }
}