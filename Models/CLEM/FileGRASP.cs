using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml.Serialization;
using Models.Core;
using APSIM.Shared.Utilities;
using Models.Interfaces;
using System.ComponentModel.DataAnnotations;

// -----------------------------------------------------------------------
// <copyright file="WeatherFile.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.CLEM
{

    ///<summary>
    /// Reads in weather data and makes it available to other models.
    ///</summary>
    ///    
    ///<remarks>
    /// Keywords in the weather file
    /// - Maxt for maximum temperature
    /// - Mint for minimum temperature
    /// - Radn for radiation
    /// - Rain for rainfall
    /// - VP for vapour pressure
    /// - Wind for wind speed
    ///     
    /// VP is calculated using function Utility.Met.svp
    /// Wind assign default value 3 if Wind is not resent.
    ///</remarks>
    [Serializable]
    [ViewName("UserInterface.Views.WFFileView")]
    [PresenterName("UserInterface.Presenters.WFFileGRASPPresenter")]
    [ValidParent(ParentType=typeof(Simulation))]
    [Description("This model holds a GRASP data file for native pasture used in the CLEM simulation.")]
    public class FileGRASP : CLEMModel
    {
        ///// <summary>
        ///// A link to the clock model.
        ///// </summary>
        //[Link]
        //private Clock clock = null;

        /// <summary>
        /// A reference to the text file reader object
        /// </summary>
        [NonSerialized]
        private ApsimTextFile reader = null;

        /// <summary>
        /// The index of the maximum temperature column in the weather file
        /// </summary>
        private int regionIndex;

        /// <summary>
        /// The index of the minimum temperature column in the weather file
        /// </summary>
        private int soilIndex;

        /// <summary>
        /// The index of the solar radiation column in the weather file
        /// </summary>
        private int forageNoIndex;

        /// <summary>
        /// The index of the rainfall column in the weather file
        /// </summary>
        private int grassBAIndex;

        /// <summary>
        /// The index of the rainfall column in the weather file
        /// </summary>
        private int landConIndex;

        /// <summary>
        /// The index of the rainfall column in the weather file
        /// </summary>
        private int stkRateIndex;

        /// <summary>
        /// The index of the evaporation column in the weather file
        /// </summary>
        private int yearNumIndex;

        /// <summary>
        /// The index of the evaporation column in the weather file
        /// </summary>
        private int yearIndex;

        /// <summary>
        /// The index of the vapor pressure column in the weather file
        /// </summary>
        private int cutNumIndex;

        /// <summary>
        /// The index of the wind column in the weather file
        /// </summary>
        private int monthIndex;

        /// <summary>
        /// The index of the wind column in the weather file
        /// </summary>
        private int growthIndex;

        /// <summary>
        /// The index of the wind column in the weather file
        /// </summary>
        private int bp1Index;

        /// <summary>
        /// The index of the wind column in the weather file
        /// </summary>
        private int bp2Index;

        /// <summary>
        /// The index of the wind column in the weather file
        /// </summary>
        private int utilisnIndex;

        /// <summary>
        /// The index of the wind column in the weather file
        /// </summary>
        private int soillossIndex;

        /// <summary>
        /// The index of the wind column in the weather file
        /// </summary>
        private int coverIndex;

        /// <summary>
        /// The index of the wind column in the weather file
        /// </summary>
        private int treeBAIndex;

        /// <summary>
        /// The index of the wind column in the weather file
        /// </summary>
        private int rainfallIndex;

        /// <summary>
        /// The index of the wind column in the weather file
        /// </summary>
        private int runoffIndex;


        /// <summary>
        /// The entire pasture File read in as a DataTable with Primary Keys assigned.
        /// </summary>
        private DataTable PastureFileAsTable;

        ///// <summary>
        ///// An instance of the weather data.
        ///// </summary>
        //private CropDataType monthsCropData = new CropDataType();

        ///// <summary>
        ///// A flag indicating whether this model should do a seek on the weather file
        ///// </summary>
        //private bool doSeek;

        ///// <summary>
        ///// This event will be invoked immediately before models get their weather data.
        ///// models and scripts an opportunity to change the weather data before other models
        ///// reads it.
        ///// </summary>
        //public event EventHandler PreparingNewWeatherData;

        /// <summary>
        /// Gets or sets the file name. Should be relative filename where possible.
        /// </summary>
        [Summary]
        [Description("Pasture file name")]
        [Required(AllowEmptyStrings = false, ErrorMessage ="Pasture file name must be supplied.")]
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
                if (simulation != null & this.FileName != null)
                    return PathUtilities.GetAbsolutePath(this.FileName, simulation.FileName);
                else
                    return this.FileName;
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

        ///// <summary>
        ///// Gets the start date of the weather file
        ///// </summary>
        //public DateTime StartDate
        //{
        //    get
        //    {
        //        if (this.reader != null)
        //        {
        //            return this.reader.FirstDate;
        //        }
        //        else
        //        {
        //            return new DateTime(0);
        //        }
        //    }
        //}

        ///// <summary>
        ///// Gets the end date of the weather file
        ///// </summary>
        //public DateTime EndDate
        //{
        //    get
        //    {
        //        if (this.reader != null)
        //        {
        //            return this.reader.LastDate;
        //        }
        //        else
        //        {
        //            return new DateTime(0);
        //        }
        //    }
        //}

        ///// <summary>
        ///// Gets the weather data as a single structure.
        ///// </summary>
        //public CropDataType CropData
        //{
        //    get
        //    {
        //        return this.monthsCropData;
        //    }
        //}

        ///// <summary>
        ///// Gets or sets the maximum temperature (oC)
        ///// </summary>
        ////[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed.")]
        //[XmlIgnore]
        //public int Region
        //{
        //    get
        //    {
        //        return this.CropData.Region;
        //    }

        //    set
        //    {
        //        this.monthsCropData.Region = value;
        //    }
        //}

        ///// <summary>
        ///// Gets or sets the minimum temperature (oC)
        ///// </summary>
        ////[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed.")]
        //[XmlIgnore]
        //public int Soil
        //{
        //    get
        //    {
        //        return this.CropData.Soil;
        //    }

        //    set
        //    {
        //        this.monthsCropData.Soil = value;
        //    }
        //}


        ///// <summary>
        ///// Gets or sets the rainfall (mm)
        ///// </summary>
        //[XmlIgnore]
        //public int CropNo
        //{
        //    get
        //    {
        //        return this.CropData.CropNo;
        //    }

        //    set
        //    {
        //        this.monthsCropData.CropNo = value;
        //    }
        //}

        ///// <summary>
        ///// Gets or sets the solar radiation. MJ/m2/day
        ///// </summary>
        //[XmlIgnore]
        //public string CropName
        //{
        //    get
        //    {
        //        return this.CropData.CropName;
        //    }

        //    set
        //    {
        //        this.monthsCropData.CropName = value;
        //    }
        //}
		
        ///// <summary>
        ///// Gets or sets the Pan Evaporation (mm) (Class A pan)
        ///// </summary>
        //[XmlIgnore]
        //public int YearNum
        //    {
        //    get
        //        {
        //        return this.CropData.YearNum;
        //        }

        //    set
        //        {
        //        this.monthsCropData.YearNum = value;
        //        }
        //    }

        ///// <summary>
        ///// Gets or sets the number of hours rainfall occured in
        ///// </summary>
        //[XmlIgnore]
        //public int Year
        //{
        //    get
        //    {
        //        return this.CropData.Year;
        //    }

        //    set
        //    {
        //        this.monthsCropData.Year = value;
        //    }
        //}

        ///// <summary>
        ///// Gets or sets the vapor pressure (hPa)
        ///// </summary>
        //[XmlIgnore]
        //public int CutNum
        //{
        //    get
        //    {
        //        return this.CropData.CutNum;
        //    }

        //    set
        //    {
        //        this.monthsCropData.CutNum = value;
        //    }
        //}

        ///// <summary>
        ///// Gets or sets the wind value found in weather file or zero if not specified. (code says 3.0 not zero)
        ///// </summary>
        //[XmlIgnore]
        //public int Month
        //{
        //    get
        //    {
        //        return this.CropData.Month;
        //    }

        //    set
        //    {
        //        this.monthsCropData.Month = value;
        //    }
        //}


        ///// <summary>
        ///// Gets or sets the wind value found in weather file or zero if not specified. (code says 3.0 not zero)
        ///// </summary>
        //[XmlIgnore]
        //public double Growth
        //{
        //    get
        //    {
        //        return this.CropData.Growth;
        //    }

        //    set
        //    {
        //        this.monthsCropData.Growth = value;
        //    }
        //}


        ///// <summary>
        ///// Gets or sets the wind value found in weather file or zero if not specified. (code says 3.0 not zero)
        ///// </summary>
        //[XmlIgnore]
        //public double NPerCent
        //{
        //    get
        //    {
        //        return this.CropData.NPerCent;
        //    }

        //    set
        //    {
        //        this.monthsCropData.NPerCent = value;
        //    }
        //}


        ///// <summary>
        ///// Gets or sets the wind value found in weather file or zero if not specified. (code says 3.0 not zero)
        ///// </summary>
        //[XmlIgnore]
        //public int Priority
        //{
        //    get
        //    {
        //        return this.CropData.Priority;
        //    }

        //    set
        //    {
        //        this.monthsCropData.Priority = value;
        //    }
        //}


        ///// <summary>
        ///// Gets or sets the wind value found in weather file or zero if not specified. (code says 3.0 not zero)
        ///// </summary>
        //[XmlIgnore]
        //public int BP1
        //{
        //    get
        //    {
        //        return this.CropData.BP1;
        //    }

        //    set
        //    {
        //        this.monthsCropData.BP1 = value;
        //    }
        //}


        ///// <summary>
        ///// Gets or sets the wind value found in weather file or zero if not specified. (code says 3.0 not zero)
        ///// </summary>
        //[XmlIgnore]
        //public int BP2
        //{
        //    get
        //    {
        //        return this.CropData.BP2;
        //    }

        //    set
        //    {
        //        this.monthsCropData.BP2 = value;
        //    }
        //}



        /// <summary>
        /// Overrides the base class method to allow for initialization.
        /// </summary>
        [EventSubscribe("Commencing")]
        private void OnSimulationCommencing(object sender, EventArgs e)
        {

            //this.doSeek = true;
            this.regionIndex = 0;
            this.soilIndex = 0;
            this.forageNoIndex = 0;
            this.grassBAIndex = 0;
            this.landConIndex = 0;
            this.stkRateIndex = 0;
            this.yearNumIndex = 0;
            this.yearIndex = 0;
            this.cutNumIndex = 0;
            this.monthIndex = 0;
            this.growthIndex = 0;
            this.bp1Index = 0;
            this.bp2Index = 0;
            this.utilisnIndex = 0;
            this.soillossIndex = 0;
            this.coverIndex = 0;
            this.treeBAIndex = 0;
            this.rainfallIndex = 0;
            this.runoffIndex = 0;

            this.PastureFileAsTable = GetAllData();
        }

        /// <summary>
        /// Overrides the base class method to allow for clean up
        /// </summary>
        [EventSubscribe("Completed")]
        private void OnSimulationCompleted(object sender, EventArgs e)
        {
            if (this.reader != null)
            {
                this.reader.Close();
                this.reader = null;
            }

        }



        /// <summary>
        /// Provides an error message to display if something is wrong.
        /// Used by the UserInterface to give a warning of what is wrong
        /// 
        /// When the user selects a file using the browse button in the UserInterface 
        /// and the file can not be displayed for some reason in the UserInterface.
        /// </summary>
        public string ErrorMessage = string.Empty;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public DataTable GetTable()
        {
            try
            {
                return GetAllData();
            }
            catch (Exception err)
            {
                ErrorMessage = err.Message;
                return null;
            }
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
                List<string> pastureProps = new List<string>();
                pastureProps.Add("Region");
                pastureProps.Add("Soil");
                pastureProps.Add("ForageNo");
                pastureProps.Add("GrassBA");
                pastureProps.Add("LandCon");
                pastureProps.Add("StkRate");
                pastureProps.Add("YearNum");
                pastureProps.Add("Year");
                pastureProps.Add("CutNum");
                pastureProps.Add("Month");
                pastureProps.Add("Growth");
                pastureProps.Add("BP1");
                pastureProps.Add("BP2");
                pastureProps.Add("Utilisn");
                pastureProps.Add("SoilLoss");
                pastureProps.Add("Cover");
                pastureProps.Add("TreeBA");
                pastureProps.Add("Rainfall");
                pastureProps.Add("Runoff");

                DataTable table = this.reader.ToTable(pastureProps);

                DataColumn[] primarykeys = new DataColumn[8];
                primarykeys[0] = table.Columns["Region"];
                primarykeys[1] = table.Columns["Soil"];
                primarykeys[2] = table.Columns["ForageNo"];
                primarykeys[3] = table.Columns["GrassBA"];
                primarykeys[4] = table.Columns["LandCon"];
                primarykeys[5] = table.Columns["StkRate"];
                primarykeys[6] = table.Columns["Year"];
                primarykeys[7] = table.Columns["Month"];

                table.PrimaryKey = primarykeys;




                CloseDataFile();

                return table;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Searches the DataTable created from the GRASP File using the specified parameters.
        /// </summary>
        /// <param name="Region"></param>
        /// <param name="Soil"></param>
        /// <param name="ForageNo"></param>
        /// <param name="GrassBA"></param>
        /// <param name="LandCon"></param>
        /// <param name="StkRate"></param>
        /// <param name="EcolCalculationDate"></param>
        /// <param name="EcolCalculationInterval"></param>
        /// <returns></returns>
        public List<PastureDataType> GetIntervalsPastureData(int Region, int Soil, int ForageNo, int GrassBA, int LandCon, int StkRate,
                                         DateTime EcolCalculationDate, int EcolCalculationInterval)
        {

            int startYear = EcolCalculationDate.Year;
            int startMonth = EcolCalculationDate.Month;
            DateTime EndDate = EcolCalculationDate.AddMonths(EcolCalculationInterval);
            int endYear = EndDate.Year;
            int endMonth = EndDate.Month;

            //http://www.csharp-examples.net/dataview-rowfilter/

            string filter = "( Region = " + Region
                + ") AND (Soil = " + Soil + ") AND (ForageNo = " + "'" + ForageNo + "'"
                + ") AND (GrassBA = " + GrassBA + ") AND (LandCon = " + LandCon + ") AND (StkRate = " + StkRate 
                + ") AND ("
                + "( Year = " + startYear + " AND Month >= " + startMonth + ")"
                + " OR  ( Year > " + startYear + " AND Year < " + endYear + ")"
                + " OR  ( Year = " + endYear + " AND Month <= " + endMonth + ")"
                + ")";


            DataRow[] foundRows = this.PastureFileAsTable.Select(filter);

            List<PastureDataType> filtered = new List<PastureDataType>();

            foreach (DataRow dr in foundRows)
            {
                filtered.Add(DataRow2PastureDataType(dr));
            }

            filtered.Sort((r, s) => DateTime.Compare(r.CutDate, s.CutDate));

            return filtered;

        }

        /// <summary>
        /// Searches the DataTable created from the PastureFile using the specified parameters.
        /// </summary>
        /// <param name="Region"></param>
        /// <param name="Soil"></param>
        /// <param name="ForageNo"></param>
        /// <param name="GrassBA"></param>
        /// <param name="LandCon"></param>
        /// <param name="StkRate"></param>
        /// <param name="Year"></param>
        /// <param name="Month"></param>
        /// <returns>CropDataType containg the crop data for this month</returns>
        public PastureDataType GetMonthsPastureData(int Region, int Soil, int ForageNo, int GrassBA, int LandCon, int StkRate, 
                                         int Year, int Month)
        {
            //string climRegion = ClimRegion.ToString(); //' Climatic region
            //string soil = Native_land.ToString();  //' soil
            //string grassBA = Grass_BA.ToString(); //' Grass Basal area
            //string landCond = Land_Con.ToString(); //' Land condition
            //string stkRate = St_Rate.ToString(); //' Stocking rate
            //string year = Pyr[x].ToString(); //' year

            object[] keyVals = new Object[8];
            keyVals[0] = Region;
            keyVals[1] = Soil;
            keyVals[2] = ForageNo;
            keyVals[3] = GrassBA;
            keyVals[4] = LandCon;
            keyVals[5] = StkRate;
            keyVals[6] = Year;
            keyVals[7] = Month;

            DataRow dr = this.PastureFileAsTable.Rows.Find(keyVals);

            if (dr != null)
            {
                PastureDataType pasturedata = DataRow2PastureDataType(dr);

                return pasturedata;
            }
            else
            {
                throw new ApsimXException(this, "Unable to find pasture data for : "
                    + "[Region = " + Region
                    + ", Soil = " + Soil
                    + ", ForageNo = " + ForageNo
                    + ", GrassBA = " + GrassBA
                    + ", LandCon = " + LandCon
                    + ", StkRate = " + StkRate
                    + ", Year = " + Year
                    + ", Month = " + Month + "]"
                    );
            }

        }

        private static PastureDataType DataRow2PastureDataType(DataRow dr)
        {
            PastureDataType pasturedata = new PastureDataType();

            pasturedata.Region = int.Parse(dr["Region"].ToString());
            pasturedata.Soil = int.Parse(dr["Soil"].ToString());
            pasturedata.ForageNo = int.Parse(dr["ForageNo"].ToString());
            pasturedata.GrassBA = int.Parse(dr["GrassBA"].ToString());
            pasturedata.LandCon = int.Parse(dr["LandCon"].ToString());
            pasturedata.StkRate = int.Parse(dr["StkRate"].ToString());
            pasturedata.YearNum = int.Parse(dr["YearNum"].ToString());
            pasturedata.Year = int.Parse(dr["Year"].ToString());
            pasturedata.CutNum = int.Parse(dr["CutNum"].ToString());
            pasturedata.Month = int.Parse(dr["Month"].ToString());
            pasturedata.Growth = double.Parse(dr["Growth"].ToString());
            pasturedata.BP1 = double.Parse(dr["BP1"].ToString());
            pasturedata.BP2 = double.Parse(dr["BP2"].ToString());
            pasturedata.Utilisn = double.Parse(dr["Utilisn"].ToString());
            pasturedata.SoilLoss = double.Parse(dr["SoilLoss"].ToString());
            pasturedata.Cover = double.Parse(dr["Cover"].ToString());
            pasturedata.TreeBA = double.Parse(dr["TreeBA"].ToString());
            pasturedata.Rainfall = double.Parse(dr["Rainfall"].ToString());
            pasturedata.Runoff = double.Parse(dr["Runoff"].ToString());
            pasturedata.CutDate = new DateTime(pasturedata.Year, pasturedata.Month, 1);
            return pasturedata;
        }





        ///// <summary>
        ///// An event handler for the daily DoWeather event.
        ///// </summary>
        ///// <param name="sender">The sender of the event</param>
        ///// <param name="e">The arguments of the event</param>
        ////[EventSubscribe("DoWeather")]
        ////private void OnDoWeather(object sender, EventArgs e)
        //public CropDataType GetThisMonth()
        //{
        //    if (this.doSeek)
        //    {
        //        if (!this.OpenDataFile())
        //        {
        //            throw new ApsimXException(this, "Cannot find weather file '" + this.FileName + "'");
        //        }

        //        this.doSeek = false;
        //        this.reader.SeekToDate(this.clock.Today);
        //    }

        //    object[] values = this.reader.GetNextLineOfData();

        //    if (this.clock.Today != this.reader.GetDateFromValues(values))
        //    {
        //        throw new Exception("Non consecutive dates found in file: " + this.FileName + ".  Another posibility is that you have two clock objects in your simulation, there should only be one");
        //    }

        //    this.monthsCropData.Today = this.clock.Today;
        //    if (this.forageNoIndex != -1)
        //        this.monthsCropData.Radn = Convert.ToSingle(values[this.forageNoIndex]);
        //    else
        //        this.monthsCropData.Radn = this.reader.ConstantAsDouble("radn");

        //    if (this.regionIndex != -1)
        //        this.monthsCropData.Maxt = Convert.ToSingle(values[this.regionIndex]);
        //    else
        //        this.monthsCropData.Maxt = this.reader.ConstantAsDouble("maxt");

        //    if (this.soilIndex != -1)
        //        this.monthsCropData.Mint = Convert.ToSingle(values[this.soilIndex]);
        //    else
        //        this.monthsCropData.Mint = this.reader.ConstantAsDouble("mint");

        //    if (this.cropNameIndex != -1)
        //        this.monthsCropData.Rain = Convert.ToSingle(values[this.cropNameIndex]);
        //    else
        //        this.monthsCropData.Rain = this.reader.ConstantAsDouble("rain");

        //    if (this.yearNumIndex == -1)
        //    {
        //        // If Evap is not present in the weather file assign a default value
        //        this.monthsCropData.PanEvap = double.NaN;
        //    }
        //    else
        //    {
        //        this.monthsCropData.PanEvap = Convert.ToSingle(values[this.yearNumIndex]);
        //    }

        //    if (this.yearIndex == -1)
        //    {
        //        // If Evap is not present in the weather file assign a default value
        //        this.monthsCropData.RainfallHours = double.NaN;
        //    }
        //    else
        //    {
        //        this.monthsCropData.RainfallHours = Convert.ToSingle(values[this.yearIndex]);
        //    }

        //    if (this.cutNumIndex == -1)
        //    {
        //        // If VP is not present in the weather file assign a defalt value
        //        this.monthsCropData.VP = Math.Max(0, MetUtilities.svp(this.CropData.Mint));
        //    }
        //    else
        //    {
        //        this.monthsCropData.VP = Convert.ToSingle(values[this.cutNumIndex]);
        //    }

        //    if (this.monthIndex == -1)
        //    {
        //        // If Wind is not present in the weather file assign a default value
        //        this.monthsCropData.Wind = 3.0;
        //    }
        //    else
        //    {
        //        this.monthsCropData.Wind = Convert.ToSingle(values[this.monthIndex]);
        //    }

        //    if (this.PreparingNewWeatherData != null)
        //    {
        //        this.PreparingNewWeatherData.Invoke(this, new EventArgs());
        //    }
        //}

        /// <summary>
        /// Open the weather data file.
        /// </summary>
        /// <returns>True if the file was successfully opened</returns>
        public bool OpenDataFile()
        {
            if (this.FullFileName == null || this.FullFileName == "") return false;
            if (System.IO.File.Exists(this.FullFileName))
            {
                if (this.reader == null)
                {
                    this.reader = new ApsimTextFile();
                    this.reader.Open(this.FullFileName, this.ExcelWorkSheetName);

                    this.regionIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, "Region");
                    this.soilIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, "Soil");
                    this.forageNoIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, "ForageNo");
                    this.grassBAIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, "GrassBA");
                    this.landConIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, "LandCon");
                    this.stkRateIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, "StkRate");
                    this.yearNumIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, "YearNum");
                    this.yearIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, "Year");
                    this.cutNumIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, "CutNum");
                    this.monthIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, "Month");
                    this.growthIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, "Growth");
                    this.bp1Index = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, "BP1");
                    this.bp2Index = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, "BP2");
                    this.utilisnIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, "Utilisn");
                    this.soillossIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, "SoilLoss");
                    this.coverIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, "Cover");
                    this.treeBAIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, "TreeBA");
                    this.rainfallIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, "Rainfall");
                    this.runoffIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, "Runoff");

                    if (this.regionIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant("Region") == null)
                            throw new Exception("Cannot find Region in pasture file: " + this.FullFileName);
                    }

                    if (this.soilIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant("Soil") == null)
                            throw new Exception("Cannot find Soil in pasture file: " + this.FullFileName);
                    }

                    if (this.forageNoIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant("ForageNo") == null)
                            throw new Exception("Cannot find ForageNo in pasture file: " + this.FullFileName);
                    }

                    if (this.grassBAIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant("GrassBA") == null)
                            throw new Exception("Cannot find GrassBA in pasture file: " + this.FullFileName);
                    }

                    if (this.landConIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant("LandCon") == null)
                            throw new Exception("Cannot find LandCon in pasture file: " + this.FullFileName);
                    }

                    if (this.stkRateIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant("StkRate") == null)
                            throw new Exception("Cannot find StkRate in pasture file: " + this.FullFileName);
                    }

                    if (this.yearNumIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant("YearNum") == null)
                            throw new Exception("Cannot find YearNum in pasture file: " + this.FullFileName);
                    }

                    if (this.yearIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant("Year") == null)
                            throw new Exception("Cannot find Year in pasture file: " + this.FullFileName);
                    }

                    if (this.cutNumIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant("CutNum") == null)
                            throw new Exception("Cannot find CutNum in pasture file: " + this.FullFileName);
                    }

                    if (this.monthIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant("Month") == null)
                            throw new Exception("Cannot find Month in pasture file: " + this.FullFileName);
                    }

                    if (this.growthIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant("Growth") == null)
                            throw new Exception("Cannot find Growth in pasture file: " + this.FullFileName);
                    }

                    if (this.bp1Index == -1)
                    {
                        if (this.reader == null || this.reader.Constant("BP1") == null)
                            throw new Exception("Cannot find BP1 in pasture file: " + this.FullFileName);
                    }

                    if (this.bp2Index == -1)
                    {
                        if (this.reader == null || this.reader.Constant("BP2") == null)
                            throw new Exception("Cannot find BP2 in pasture file: " + this.FullFileName);
                    }

                    if (this.utilisnIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant("Utilisn") == null)
                            throw new Exception("Cannot find Utilisn in pasture file: " + this.FullFileName);
                    }

                    if (this.soillossIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant("SoilLoss") == null)
                            throw new Exception("Cannot find SoilLoss in pasture file: " + this.FullFileName);
                    }

                    if (this.coverIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant("Cover") == null)
                            throw new Exception("Cannot find Cover in pasture file: " + this.FullFileName);
                    }

                    if (this.treeBAIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant("TreeBA") == null)
                            throw new Exception("Cannot find TreeBA in pasture file: " + this.FullFileName);
                    }

                    if (this.rainfallIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant("Rainfall") == null)
                            throw new Exception("Cannot find RainFall in pasture file: " + this.FullFileName);
                    }

                    if (this.runoffIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant("Runoff") == null)
                            throw new Exception("Cannot find Runoff in pasture file: " + this.FullFileName);
                    }





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
            {
                reader.Close();
                reader = null;
            }
        }



        ///// <summary>
        ///// Calculate the amp and tav 'constant' values for this weather file.
        ///// </summary>
        ///// <param name="tav">The calculated tav value</param>
        ///// <param name="amp">The calculated amp value</param>
        //[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed.")]
        //private void ProcessMonthlyTAVAMP(out double tav, out double amp)
        //{
        //    int savedPosition = reader.GetCurrentPosition();

        //    // init return values
        //    tav = 0;
        //    amp = 0;
        //    double maxt, mint;

        //    // get dataset size
        //    DateTime start = this.reader.FirstDate;
        //    DateTime last = this.reader.LastDate;
        //    int nyears = last.Year - start.Year + 1;

        //    // temp storage arrays
        //    double[,] monthlyMeans = new double[12, nyears];
        //    double[,] monthlySums = new double[12, nyears];
        //    int[,] monthlyDays = new int[12, nyears];

        //    this.reader.SeekToDate(start); // goto start of data set

        //    // read the daily data from the met file
        //    object[] values;
        //    DateTime curDate;
        //    int curMonth = 0;
        //    bool moreData = true;
        //    while (moreData)
        //    {
        //        values = this.reader.GetNextLineOfData();
        //        curDate = this.reader.GetDateFromValues(values);
        //        int yearIndex = curDate.Year - start.Year;
        //        maxt = Convert.ToDouble(values[this.maximumTemperatureIndex]);
        //        mint = Convert.ToDouble(values[this.minimumTemperatureIndex]);

        //        // accumulate the daily mean for each month
        //        if (curMonth != curDate.Month)
        //        {
        //            // if next month then
        //            curMonth = curDate.Month;
        //            monthlySums[curMonth - 1, yearIndex] = 0;    // initialise the total
        //        }

        //        monthlySums[curMonth - 1, yearIndex] = monthlySums[curMonth - 1, yearIndex] + ((maxt + mint) * 0.5);
        //        monthlyDays[curMonth - 1, yearIndex]++;

        //        if (curDate >= last)
        //        {
        //            // if have read last record
        //            moreData = false;
        //        }
        //    }

        //    // do more summary calculations
        //    double sumOfMeans;
        //    double maxMean, minMean;
        //    double yearlySumMeans = 0;
        //    double yearlySumAmp = 0;
        //    for (int y = 0; y < nyears; y++)
        //    {
        //        maxMean = -999;
        //        minMean = 999;
        //        sumOfMeans = 0;
        //        for (int m = 0; m < 12; m++)
        //        {
        //            monthlyMeans[m, y] = monthlySums[m, y] / monthlyDays[m, y];  // calc monthly mean
        //            sumOfMeans += monthlyMeans[m, y];
        //            maxMean = Math.Max(monthlyMeans[m, y], maxMean);
        //            minMean = Math.Min(monthlyMeans[m, y], minMean);
        //        }

        //        yearlySumMeans += sumOfMeans / 12.0;        // accum the ave of monthly means
        //        yearlySumAmp += maxMean - minMean;          // accum the amp of means
        //    }

        //    tav = yearlySumMeans / nyears;  // calc the ave of the yearly ave means
        //    amp = yearlySumAmp / nyears;    // calc the ave of the yearly amps

        //    reader.SeekToPosition(savedPosition);
        //}



        ///// <summary>
        ///// A structure containing the commonly used weather data.
        ///// </summary>
        //[Serializable]
        //public struct NewMetType
        //{


        //    /// <summary>
        //    /// The current date
        //    /// </summary>
        //    public DateTime Today;

        //    /// <summary>
        //    /// Solar radiation. MJ/m2/day
        //    /// </summary>
        //    public double Radn;

        //    /// <summary>
        //    /// Maximum temperature (oC)
        //    /// </summary>
        //    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed.")]
        //    public double Maxt;

        //    /// <summary>
        //    /// Minimum temperature (oC)
        //    /// </summary>
        //    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed.")]
        //    public double Mint;

        //    /// <summary>
        //    /// Rainfall (mm)
        //    /// </summary>
        //    public double Rain;

        //    /// <summary>
        //    /// Pan Evaporation (mm) (Class A pan) (NaN if not present)
        //    /// </summary>
        //    public double PanEvap;

        //    /// <summary>
        //    /// Pan Evaporation (mm) (Class A pan) (NaN if not present)
        //    /// </summary>
        //    public double RainfallHours;

        //    /// <summary>
        //    /// The vapor pressure (hPa)
        //    /// </summary>
        //    public double VP;

        //    /// <summary>
        //    /// The wind value found in weather file or zero if not specified (code says 3.0 not zero).
        //    /// </summary>
        //    public double Wind;
        //}

    }


    /// <summary>
    /// A structure containing the commonly used weather data.
    /// </summary>
    [Serializable]
    public struct PastureDataType
    {
        /// <summary>
        /// Climatic Region Number
        /// </summary>
        public int Region;

        /// <summary>
        /// Soil Number
        /// </summary>
        public int Soil;

        /// <summary>
        /// Forage Number 
        /// </summary>
        public int ForageNo;

        /// <summary>
        /// Grass Basal Area
        /// </summary>
        public int GrassBA;

        /// <summary>
        /// Land Condition
        /// </summary>
        public int LandCon;

        /// <summary>
        /// Stocking Rate
        /// </summary>
        public int StkRate;

        /// <summary>
        /// Year Number (counting from start of simulation ?)
        /// </summary>
        public int YearNum;

        /// <summary>
        /// Year (eg. 2017)
        /// </summary>
        public int Year;

        /// <summary>
        /// Cut Number in this year
        /// </summary>
        public int CutNum;

        /// <summary>
        /// Month (eg. 1 is Jan, 2 is Feb)
        /// </summary>
        public int Month;

        /// <summary>
        /// Amout in Kg of Biomass of the pasture
        /// </summary>
        public double Growth;

        /// <summary>
        /// Amount in Kg of By Product 1 of the production of this pasture
        /// </summary>
        public double BP1;

        /// <summary>
        /// Amount in Kg of By Product 2 of the production of this pasture
        /// </summary>
        public double BP2;

        /// <summary>
        /// Utilisation
        /// </summary>
        public double Utilisn;

        /// <summary>
        /// Soil Loss
        /// </summary>
        public double SoilLoss;

        /// <summary>
        /// Cover
        /// </summary>
        public double Cover;

        /// <summary>
        /// Tree Basal Area
        /// </summary>
        public double TreeBA;

        /// <summary>
        /// Rainfall
        /// </summary>
        public double Rainfall;

        /// <summary>
        /// Runoff
        /// </summary>
        public double Runoff;


        /// <summary>
        /// Combine Year and Month to create a DateTime. 
        /// Day is set to the 1st of the month.
        /// </summary>
        public DateTime CutDate;

    }

}