using System;
using System.Data;
using Models.Core;
using System.IO;

namespace Models
{
    /// <summary>
    /// Reads in met data and makes it available for other components.
    /// </summary>
    public class WeatherFile : Model
    {
        // Links
        [Link] private Clock Clock = null;
        [Link] private Simulations Simulations = null;
        [Link] private ISummary Summary = null;

        // Privates
        private Utility.ApsimTextFile File = null;
        private DataTable Data = new DataTable();
        private bool HaveReadData = false;
        private int MaxTIndex;
        private int MinTIndex;
        private int RadnIndex;
        private int RainIndex;
        private NewMetType TodaysMetData = new NewMetType();

        // Parameters read in
        public string FileName { get; set; }

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
        public double Latitude
        {
            get
            {
                if (File.Constant("Latitude") == null)
                    return 0;
                else
                    return Convert.ToDouble(File.Constant("Latitude").Value);
            }
        }
        public double Tav
        {
            get
            {
                if (File.Constant("tav") == null)
                {
                    //this constant has not been found so do a calculation
                    CalcTAVAMP();
                }
                return Convert.ToDouble(File.Constant("tav").Value);
            }
        }
        public double Amp
        {
            get
            {
                if (File.Constant("amp") == null)
                {
                    //this constant has not been found so do a calculation
                    CalcTAVAMP();
                }
                return Convert.ToDouble(File.Constant("amp").Value);
            }
        }
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

        /// <summary>
        /// An event handler to allow use to initialise ourselves.
        /// </summary>
        public override void OnInitialised()
        {
            if (File == null)
            {
                HaveReadData = false;
                Clock.Tick += OnTick;
                File = new Utility.ApsimTextFile();
                string FullFileName = Path.Combine(Path.GetDirectoryName(Simulations.FileName), FileName);
                Summary.WriteProperty("Weather file name", FileName);
                File.Open(FullFileName);
                MaxTIndex = Utility.String.IndexOfCaseInsensitive(File.Headings, "Maxt");
                MinTIndex = Utility.String.IndexOfCaseInsensitive(File.Headings, "Mint");
                RadnIndex = Utility.String.IndexOfCaseInsensitive(File.Headings, "Radn");
                RainIndex = Utility.String.IndexOfCaseInsensitive(File.Headings, "Rain");
                if (MaxTIndex == -1)
                    throw new Exception("Cannot find MaxT in weather file: " + FileName);
                if (MinTIndex == -1)
                    throw new Exception("Cannot find MinT in weather file: " + FileName);
                if (RadnIndex == -1)
                    throw new Exception("Cannot find Radn in weather file: " + FileName);
                if (RainIndex == -1)
                    throw new Exception("Cannot find Rain in weather file: " + FileName);
            }
        }

        /// <summary>
        /// An event handler for the tick event.
        /// </summary>
        public void OnTick(DateTime today)
        {
            if (!HaveReadData)
            {
                File.SeekToDate(Clock.Today);
                HaveReadData = true;
            }


            object[] Values = File.GetNextLineOfData();

            int RowIndex = Data.Rows.Count - 1;
            if (Clock.Today != File.GetDateFromValues(Values))
                throw new Exception("Non consecutive dates found in file: " + FileName);

            TodaysMetData.today = (double)Clock.Today.Ticks;
            TodaysMetData.radn = Convert.ToSingle(Values[RadnIndex]);
            TodaysMetData.maxt = Convert.ToSingle(Values[MaxTIndex]);
            TodaysMetData.mint = Convert.ToSingle(Values[MinTIndex]);
            TodaysMetData.rain = Convert.ToSingle(Values[RainIndex]);
            if (NewMet != null)
                NewMet.Invoke(MetData);

            RowIndex++;
        }

        /// <summary>
        /// Simulation has terminated. Perform cleanup.
        /// </summary>
        public override void OnCompleted()
        {
            Clock.Tick -= OnTick;
            File.Close();
            File = null;
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

            if (File.Constant("tav") == null)
            {
                File.AddConstant("tav", tav.ToString(), "", ""); //add a new constant
            }
            else
                File.SetConstant("tav", tav.ToString());

            if (File.Constant("amp") == null)
            {
                File.AddConstant("amp", amp.ToString(), "", ""); //add a new constant
            }
            else
                File.SetConstant("amp", amp.ToString());
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
            DateTime start = File.FirstDate;
            DateTime last = File.LastDate;
            int nyears = last.Year - start.Year + 1;
            //temp storage arrays
            double[,] monthlyMeans = new double[12, nyears];
            double[,] monthlySums = new double[12, nyears];
            int[,] monthlyDays  = new int[12, nyears];

            File.SeekToDate(start); //goto start of data set

            //read the daily data from the met file
            object[] Values;
            DateTime curDate;
            int curMonth = 0;
            Boolean moreData = true;
            while (moreData)
            {
                Values = File.GetNextLineOfData();
                curDate = File.GetDateFromValues(Values);
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

            HaveReadData = false;           //ensure that OnTick will set the file ptr correctly for next read    
        }
    }
}