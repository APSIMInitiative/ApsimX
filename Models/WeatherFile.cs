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
        [Link] private Simulation Simulation = null;
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
                    return 0;
                else
                    return Convert.ToDouble(File.Constant("tav").Value);
            }
        }
        public double Amp
        {
            get
            {
                if (File.Constant("amp") == null)
                    return 0;
                else
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
                Simulation.Completed += OnCompleted;
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
        private void OnCompleted()
        {
            Clock.Tick -= OnTick;
            Simulation.Completed -= OnCompleted;
            File.Close();
            File = null;
        }
    }
}