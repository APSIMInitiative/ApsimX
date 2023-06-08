using APSIM.Shared.Utilities;
using Models.Core;
using Models.Functions;
using Models.Interfaces;
using Models.PMF;
using Models.PMF.Scrum;
using Newtonsoft.Json;
using SixLabors.Fonts;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Models.Climate
{
    ///<summary>
    /// Reads in temperature data over a specified period and returns thermal time sum.
    ///</summary>
    [Serializable]
    [ValidParent(ParentType=typeof(Plant))]
    public class GetTempSum : Model, IReferenceExternalFiles
    {
        /// <summary>
        /// A reference to the text file reader object
        /// </summary>
        [NonSerialized]
        private ApsimTextFile reader = null;

        [Link]
        Weather weather = null;

        /// <summary>
        /// The index of the maximum temperature column in the weather file
        /// </summary>
        private int maximumTemperatureIndex;

        /// <summary>
        /// The index of the minimum temperature column in the weather file
        /// </summary>
        private int minimumTemperatureIndex;
            
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
                return weather.FullFileName;
            }
        }

        /// <summary>
        /// Used to hold the WorkSheet Name if data retrieved from an Excel file
        /// </summary>
        public string ExcelWorkSheetName { get; set; }

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
        /// Calculates the accumulated thermal time between two dates
        /// </summary>
        /// <param name="start">Start Date</param>
        /// <param name="end">End Date</param>
        /// <param name="BaseT">Base temperature</param>
        /// <param name="OptT">Optimal temperature</param>
        /// <param name="MaxT">Maximum temperature</param>
        public double GetTtSum(DateTime start, DateTime end,double BaseT,double OptT,double MaxT)
        {
            this.maximumTemperatureIndex = 0;
            this.minimumTemperatureIndex = 0;
            if (reader != null)
            {
                reader.Close();
                reader = null;
            }

            double[] xs = new double[] { BaseT, OptT, MaxT };
            double[] ys = new double[] { 0, OptT - BaseT, 0 };
            XYPairs TtResponse = new XYPairs() {X=xs,Y=ys};

            double TtSum = 0;
            for (DateTime d = start; d <= end; d = d.AddDays(1))
            {
                DailyMetDataFromFile TodaysMetData = GetMetData(d); // Read another line ahead to get tomorrows data
                TtSum += TtResponse.ValueIndexed((TodaysMetData.MinT + TodaysMetData.MaxT)/2);
            }

            if (this.reader != null)
                this.reader.Close();
            this.reader = null;

            return TtSum;
        }

        /// <summary>
        /// Calculates the accumulated thermal time between two dates
        /// </summary>
        /// <param name="start">Start Date</param>
        /// <param name="HarvTt">Thermal time from establishment to Harvest</param>
        /// <param name="BaseT">Base Temperature</param>
        /// <param name="OptT">Optimum temperature</param>
        /// <param name="MaxT">Maximum Temperautre</param>
        public DateTime GetHarvestDate(DateTime start, double HarvTt, double BaseT, double OptT, double MaxT)
        {
            this.maximumTemperatureIndex = 0;
            this.minimumTemperatureIndex = 0;
            if (reader != null)
            {
                reader.Close();
                reader = null;
            }

            double[] xs = new double[] { BaseT, OptT, MaxT };
            double[] ys = new double[] { 0, OptT - BaseT, 0 };
            XYPairs TtResponse = new XYPairs { X = xs, Y = ys };

            double TtSum = 0;
            DateTime d = start;
            while (TtSum < HarvTt)
            {
                DailyMetDataFromFile TodaysMetData = GetMetData(d); // Read another line ahead to get tomorrows data
                TtSum += TtResponse.ValueIndexed((TodaysMetData.MinT + TodaysMetData.MaxT) / 2);
                d = d.AddDays(1);
            }


            if (this.reader != null)
                this.reader.Close();
            this.reader = null;

            return d;
        }

        /// <summary>Method to read one days met data in from file</summary>
        /// <param name="date">the date to read met data</param>
        public DailyMetDataFromFile GetMetData(DateTime date)
        {
            if (!this.OpenDataFile())
                throw new ApsimXException(this, "Cannot find weather file '" + this.FileName + "'");

            this.reader.SeekToDate(date);
            
            DailyMetDataFromFile readMetData = new DailyMetDataFromFile();

            try
            {
                readMetData.Raw = reader.GetNextLineOfData();
            }
            catch (IndexOutOfRangeException err)
            {
                throw new Exception($"Unable to retrieve weather data on {date.ToString("yyy-MM-dd")} in file {FileName}", err);
            }

            if (date != this.reader.GetDateFromValues(readMetData.Raw))
                throw new Exception("Non consecutive dates found in file: " + this.FileName + ".  Another posibility is that you have two clock objects in your simulation, there should only be one");

            if (this.maximumTemperatureIndex != -1)
                readMetData.MaxT = Convert.ToSingle(readMetData.Raw[this.maximumTemperatureIndex], CultureInfo.InvariantCulture);
            else
                readMetData.MaxT = this.reader.ConstantAsDouble("maxt");

            if (this.minimumTemperatureIndex != -1)
                readMetData.MinT = Convert.ToSingle(readMetData.Raw[this.minimumTemperatureIndex], CultureInfo.InvariantCulture);
            else
                readMetData.MinT = this.reader.ConstantAsDouble("mint");

            return readMetData;
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
                    
                    if (this.maximumTemperatureIndex == -1)
                        if (this.reader == null || this.reader.Constant("maxt") == null)
                            throw new Exception("Cannot find MaxT in weather file: " + this.FullFileName);

                    if (this.minimumTemperatureIndex == -1)
                        if (this.reader == null || this.reader.Constant("mint") == null)
                            throw new Exception("Cannot find MinT in weather file: " + this.FullFileName);
                    
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
    }
}