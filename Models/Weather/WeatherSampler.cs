namespace Models
{
    using APSIM.Shared.Utilities;
    using Models.Core;
    using Models.Interfaces;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Allow random resampling of whole years of weather data from a met file.
    /// </summary>
    /// <remarks>
    /// Parameters / inputs
    ///     * read in a met file
    ///     * parameter setting the 'start' of the year as dd-mmm-yyyy (e.g. "01-jun-3000") where the 
    ///     dd-mmm part sets the start of all sampling years and the full dd-mmm-yyyy sets the start 
    ///     date of the simulation (end date is the start plus the number of years listed below)
    ///     * read in either:
    ///         - a list of years in order to sample (e.g. "1997,2014,2000"); or
    ///         - the number of years required and "random"; or
    ///         - the number of years required and "pseudo-random" and a seed value
    ///     * ? an add-on to the existing met file?
    ///     * needs to be able to be used as a factor in an experiment
    /// What it does
    ///     * sets the (fake) start date of the simulation (here 01-jun-3000) along with the weather data 
    ///     to be used with the "sampling_date" (here the first one would be 01-jun-1997 for example) being outputable
    ///     * leap years - ignores 29-feb in the sampling data and pads an extra final day in the year if needed 
    ///     (want the same weather regardless of the simulation year - hope that makes sense!)
    ///     * in the example above, by the time the simulation gets to 31-may-3001, sampling_date is 31-may-1998. 
    ///     Then on simulation date 01-jun-3001 the sampling_date is 01-jun-2014. Equivalent behaviour for
    ///     randomly generated weather years.
    /// </remarks>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Simulation))]
    public class WeatherSampler : Model, IWeather
    {
        /// <summary>A data table of all weather data.</summary>
        private DataTable data;

        /// <summary>The current row into the weather data table.</summary>
        private int currentRowIndex;

        /// <summary>The current index into the Years array.</summary>
        private int currentYearIndex;

        [Link]
        private Clock clock = null;

        /// <summary>Type of randomiser enum for drop down.</summary>
        public enum RandomiserTypeEnum
        {
            /// <summary>Specify years manually.</summary>
            SpecifyYears,

            /// <summary>Random sampler.</summary>
            RandomSampler
        }


        /// <summary>The weather file name.</summary>
        [Summary]
        [Description("Weather file name")]
        public string FileName { get; set; }

        /// <summary>The start date of the simulation.</summary>
        [Summary]
        [Description("Start date")]
        public string StartDateOfSimulation { get; set; }

        /// <summary>Type of year sampling.</summary>
        [Description("Type of sampling")]
        public RandomiserTypeEnum TypeOfSampling { get; set; }

        /// <summary>The sample years.</summary>
        [Summary]
        [Description("Years to sample from the weather file.")]
        [Display(EnabledCallback = "IsSpecifyYearsEnabled")]
        public double[] Years { get; set; }

        /// <summary>The sample years.</summary>
        [Summary]
        [Description("Number of years to sample from the weather file.")]
        [Display(EnabledCallback = "IsRandomSamplerEnabled")]
        public int NumYears { get; set; }


        /// <summary>Is 'specify years' enabled?</summary>
        public bool IsSpecifyYearsEnabled { get { return TypeOfSampling == RandomiserTypeEnum.SpecifyYears; } }

        /// <summary>Is 'Random Sampler' enabled?</summary>
        public bool IsRandomSamplerEnabled { get { return TypeOfSampling == RandomiserTypeEnum.RandomSampler; } }


        /// <summary>The start date of the weather file.</summary>
        public DateTime StartDate { get; set; }

        /// <summary>The end date of the weather file.</summary>
        public DateTime EndDate { get; set; }
        
        /// <summary>The maximum temperature (oc).</summary>
        [Units("°C")]
        [JsonIgnore]
        public double MaxT { get; set; }

        /// <summary>Gets or sets the minimum temperature (oc).</summary>
        [Units("°C")]
        [JsonIgnore]
        public double MinT { get; set; }

        /// <summary>Mean temperature. </summary>
        [Units("°C")]
        [JsonIgnore]
        public double MeanT { get { return (MaxT + MinT) / 2; } }

        /// <summary>Daily mean VPD.</summary>
        [Units("hPa")]
        [JsonIgnore]
        public double VPD { get; set; }

        /// <summary>Rainfall (mm).</summary>
        [Units("mm")]
        [JsonIgnore]
        public double Rain { get; set; }

        /// <summary>Solar radiation (MJ/m2/day).</summary>
        [Units("MJ/m^2/d")]
        [JsonIgnore]
        public double Radn { get; set; }

        /// <summary>Vapor pressure.</summary>
        [Units("hPa")]
        [JsonIgnore]
        public double VP { get; set; }

        /// <summary>Wind.</summary>
        [JsonIgnore]
        public double Wind { get; set; }

        /// <summary>CO2 level. If not specified in the weather file the default is 350.</summary>
        [Units("ppm")]
        [JsonIgnore]
        public double CO2 { get; set; }

        /// <summary>Atmospheric air pressure. If not specified in the weather file the default is 1010 hPa.</summary>
        [Units("hPa")]
        [JsonIgnore]
        public double AirPressure { get; set; }

        /// <summary>Latitude.</summary>
        [JsonIgnore]
        public double Latitude { get; set; }

        /// <summary>Average temperature.</summary>
        [Units("°C")]
        [JsonIgnore]
        public double Tav { get; set; }

        /// <summary>Temperature amplitude.</summary>
        [Units("°C")]
        [JsonIgnore]
        public double Amp { get; set; }

        /// <summary>Duration of the day in hours.</summary>
        /// <param name="Twilight">The twilight angle.</param>
        public double CalculateDayLength(double Twilight)
        {
            return MathUtilities.DayLength(clock.Today.DayOfYear, Twilight, this.Latitude);
        }

        /// <summary>Called at the beginning of a simulation.</summary>
        [EventSubscribe("Commencing")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            // Open the weather file and read its contents.
            DateTime firstDateInFile;
            DateTime lastDateInFile;
            var file = new ApsimTextFile();
            try
            {
                file.Open(FileName);
                data = file.ToTable();
                firstDateInFile = file.FirstDate;
                lastDateInFile = file.LastDate;
                Latitude = Convert.ToDouble(file.Constant("Latitude").Value);
                if (file.Constant("TAV") == null)
                    throw new Exception("TAV not specified in weather file.");
                Tav = Convert.ToDouble(file.Constant("TAV").Value);
                if (file.Constant("AMP") == null)
                    throw new Exception("AMP not specified in weather file.");
                Amp = Convert.ToDouble(file.Constant("Amp").Value);
                if (file.Constant("CO2") != null)
                    CO2 = Convert.ToDouble(file.Constant("CO2").Value);
                else
                    CO2 = 350;
            }
            finally
            {
                file.Close();
            }

            // Make sure some data was read.
            if (data.Rows.Count == 0)
                throw new Exception($"No weather data found in file {FileName}");

            // Parse the user input start date into a StartDate.
            DateTime date;
            if (DateTime.TryParse(StartDateOfSimulation, out date))
                StartDate = date;
            else
                throw new Exception($"Invalid start date specified in WeatherRandomiser {StartDateOfSimulation}");

            // If sampling method is random then create an array of random years.
            if (TypeOfSampling == RandomiserTypeEnum.RandomSampler)
            {
                // Make sure user has entered number of years.
                if (NumYears <= 0)
                    throw new Exception("The number of years to sample from the weather file hasn't been speciifed.");
                
                // Determine the year range to sample from.
                var firstYearToSampleFrom = firstDateInFile.Year;
                var lastYearToSampleFrom = lastDateInFile.Year - 1;
                if (firstDateInFile > StartDate)
                    firstYearToSampleFrom++;

                // Randomly sample from the weather record for the required number of years.
                Years = new double[NumYears];
                var random = new Random();
                for (int i = 0; i < NumYears; i++)
                    Years[i] = random.Next(firstYearToSampleFrom, lastYearToSampleFrom);
            }

            if (Years == null || Years.Length == 0)
                throw new Exception("No years specified in WeatherRandomiser");

            EndDate = StartDate.AddYears(Years.Length).AddDays(-1);

            currentYearIndex = -1;
        }

        /// <summary>An event handler for the daily DoWeather event.</summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The arguments of the event</param>
        [EventSubscribe("DoWeather")]
        private void OnDoWeather(object sender, EventArgs e)
        {
            if (currentRowIndex >= data.Rows.Count)
                throw new Exception("Have run out of weather data");
            var dateInFile = DataTableUtilities.GetDateFromRow(data.Rows[currentRowIndex]);
            if (currentYearIndex == -1 || (dateInFile.Day == StartDate.Day && dateInFile.Month == StartDate.Month))
            {
                // Need to change years to next one in sequence.
                currentYearIndex++;
                if (currentYearIndex >= Years.Length)
                    throw new Exception("Have run out of years to sample in WeatherRandomiser");
                var dateToFind = new DateTime(Convert.ToInt32(Years[currentYearIndex]), StartDate.Month, StartDate.Day);
                currentRowIndex = FindRowForDate(dateToFind);
            }

            MaxT = Convert.ToDouble(data.Rows[currentRowIndex]["MaxT"]);
            MinT = Convert.ToDouble(data.Rows[currentRowIndex]["MinT"]);
            Radn = Convert.ToDouble(data.Rows[currentRowIndex]["Radn"]);
            Rain = Convert.ToDouble(data.Rows[currentRowIndex]["Rain"]);
            if (data.Columns.Contains("VP")) 
                VP = Convert.ToDouble(data.Rows[currentRowIndex]["VP"]);
            if (data.Columns.Contains("Wind"))
                Wind = Convert.ToDouble(data.Rows[currentRowIndex]["Wind"]);
            if (AirPressure == 0)
                this.AirPressure = 1010;

            currentRowIndex++;
        }

        /// <summary>
        /// Find a row in the data table that matches a date. Will throw if not found.
        /// </summary>
        /// <param name="dateToFind">The date to find.</param>
        /// <returns>The index of the found row.</returns>
        private int FindRowForDate(DateTime dateToFind)
        {
            var firstDateInFile = DataTableUtilities.GetDateFromRow(data.Rows[0]);
            var rowIndex = (dateToFind - firstDateInFile).Days;

            // check to make sure dates are ok.
            if (rowIndex < 0)
                throw new Exception($"Cannot find year in weather file. Year = {Years[currentYearIndex]}");
            if (DataTableUtilities.GetDateFromRow(data.Rows[rowIndex]) != dateToFind)
                throw new Exception($"Non consecutive dates found in file {FileName}");
            return rowIndex;
        }

    }
}
