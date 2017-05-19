using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Models.Core;
using APSIM.Shared.Utilities;
using Models.Interfaces;

// -----------------------------------------------------------------------
// <copyright file="WeatherFile.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.WholeFarm
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
    [PresenterName("UserInterface.Presenters.WFFileForagePresenter")]
    [ValidParent(ParentType=typeof(Simulation))]
    public class FileAPSIMForage : Model
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
        private int forageNameIndex;

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
        private int nPerCentIndex;

        /// <summary>
        /// The index of the wind column in the weather file
        /// </summary>
        private int priorityIndex;

        /// <summary>
        /// The index of the wind column in the weather file
        /// </summary>
        private int bp1Index;

        /// <summary>
        /// The index of the wind column in the weather file
        /// </summary>
        private int bp2Index;

        /// <summary>
        /// The entire Crop File read in as a DataTable with Primary Keys assigned.
        /// </summary>
        private DataTable ForageFileAsTable;



        /// <summary>
        /// Gets or sets the file name. Should be relative filename where possible.
        /// </summary>
        [Summary]
        [Description("APSIM Forage file name")]
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
            this.forageNameIndex = 0;
            this.yearNumIndex = 0;
            this.yearIndex = 0;
            this.cutNumIndex = 0;
            this.monthIndex = 0;
            this.growthIndex = 0;
            this.nPerCentIndex = 0;
            this.priorityIndex = 0;
            this.bp1Index = 0;
            this.bp2Index = 0;

            this.ForageFileAsTable = GetAllData();
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
                List<string> forageProps = new List<string>();
                forageProps.Add("Region");
                forageProps.Add("Soil");
                forageProps.Add("ForageNo");
                forageProps.Add("ForageName");
                forageProps.Add("YearNum");
                forageProps.Add("Year");
                forageProps.Add("CutNum");
                forageProps.Add("Month");
                forageProps.Add("Growth");
                forageProps.Add("NPerCent");
                forageProps.Add("Priority");
                forageProps.Add("BP1");
                forageProps.Add("BP2");

                DataTable table = this.reader.ToTable(forageProps);

                DataColumn[] primarykeys = new DataColumn[5];
                primarykeys[0] = table.Columns["Region"];
                primarykeys[1] = table.Columns["Soil"];
                primarykeys[2] = table.Columns["ForageName"];
                primarykeys[3] = table.Columns["Year"];
                primarykeys[4] = table.Columns["CutNum"]; //not Month because you can have multiple cuts in a month

                table.PrimaryKey = primarykeys;

                CloseDataFile();

                return table;
            }
            else
            {
                return null;
            }
        }


        ///// <summary>
        ///// Searches the DataTable created from the Forage File using the specified parameters.
        ///// <returns></returns>
        ///// </summary>
        ///// <param name="Region"></param>
        ///// <param name="Soil"></param>
        ///// <param name="ForageName"></param>
        ///// <param name="Year"></param>
        ///// <param name="Month"></param>
        ///// <returns>A struct called CropDataType containing the crop data for this month.
        ///// This struct can be null. 
        ///// </returns>
        //public ForageDataType GetForageDataForThisMonth(int Region, int Soil, int ForageName, 
        //                                int Year, int Month)
        //{
        //    object[] keyVals = new Object[5];
        //    keyVals[0] = Region;
        //    keyVals[1] = Soil;
        //    keyVals[2] = ForageName;
        //    keyVals[3] = Year;
        //    keyVals[4] = Month;

        //    DataRow dr = this.ForageFileAsTable.Rows.Find(keyVals);

        //    if (dr != null)
        //    {
        //        return DataRow2ForageData(dr);
        //    }
        //    else
        //    {
        //        return null;
        //    }

        //}

        /// <summary>
        /// Searches the DataTable created from the Forage File using the specified parameters.
        /// <returns></returns>
        /// </summary>
        /// <param name="Region"></param>
        /// <param name="Soil"></param>
        /// <param name="ForageName"></param>
        /// <param name="StartDate"></param>
        /// <param name="EndDate"></param>
        /// <returns>A struct called CropDataType containing the crop data for this month.
        /// This struct can be null. 
        /// </returns>
        public List<ForageDataType> GetForageDataForEntireRun(int Region, int Soil, string ForageName,
                                        DateTime StartDate, DateTime EndDate)
        {
            int startYear = StartDate.Year;
            int startMonth = StartDate.Month;
            int endYear = EndDate.Year;
            int endMonth = EndDate.Month;


            //        string filterbyYear = "Region = " + Region
            //+ " AND Soil = " + Soil + "AND ForageName = " + ForageName
            //+ " AND ( Year > " + startYear + " AND Year < " + endYear + ")"

            //        DataRow[] foundRows = this.ForageFileAsTable.Select(filterbyYear);

            //        List<ForageDataType> filteredByYear = new List<ForageDataType>();

            //        foreach (DataRow dr in foundRows)
            //        {
            //            filteredByYear.Add(DataRow2ForageData(dr));
            //        }

            //        List<ForageDataType> filteredByMonth;
            //        filteredByMonth = filteredByYear.Where(r => r.CutDate >= StartDate && r.CutDate <= EndDate).ToList();

            //        List<ForageDataType> sortedByDate;
            //        sortedByDate = filteredByMonth.OrderByDescending(r => r.CutDate).ToList();

            //http://www.csharp-examples.net/dataview-rowfilter/

            string filter = "( Region = " + Region
                + ") AND (Soil = " + Soil + ") AND (ForageName = " +  "'" + ForageName + "'"
                + ") AND (" 
                +      "( Year = " + startYear + " AND Month >= " + startMonth + ")" 
                + " OR  ( Year > " + startYear + " AND Year < " + endYear +")"
                + " OR  ( Year = " + endYear + " AND Month <= " + endMonth + ")"
                +      ")";


            DataRow[] foundRows = this.ForageFileAsTable.Select(filter);

            List<ForageDataType> filtered = new List<ForageDataType>(); 

            foreach (DataRow dr in foundRows)
            {
                filtered.Add(DataRow2ForageData(dr));
            }

            filtered.Sort( (r,s) => DateTime.Compare(r.CutDate, s.CutDate) );

            return filtered;
        }



        private ForageDataType DataRow2ForageData(DataRow dr)
        {
            ForageDataType foragedata = new ForageDataType();

            foragedata.Region = int.Parse(dr["Region"].ToString());
            foragedata.Soil = int.Parse(dr["Soil"].ToString());
            foragedata.ForageNo = int.Parse(dr["ForageNo"].ToString());
            foragedata.ForageName = dr["ForageName"].ToString();
            foragedata.YearNum = int.Parse(dr["YearNum"].ToString());
            foragedata.Year = int.Parse(dr["Year"].ToString());
            foragedata.CutNum = int.Parse(dr["CutNum"].ToString());
            foragedata.Month = int.Parse(dr["Month"].ToString());

            foragedata.Growth = double.Parse(dr["Growth"].ToString());
            foragedata.NPerCent = double.Parse(dr["NPerCent"].ToString());
            foragedata.Priority = int.Parse(dr["Priority"].ToString());
            foragedata.BP1 = double.Parse(dr["BP1"].ToString());
            foragedata.BP2 = double.Parse(dr["BP2"].ToString());

            foragedata.CutDate = new DateTime(foragedata.Year, foragedata.Month, 1);

            return foragedata;
        }




        /// <summary>
        /// Open the forage data file.
        /// </summary>
        /// <returns>True if the file was successfully opened</returns>
        public bool OpenDataFile()
        {
            if (System.IO.File.Exists(this.FullFileName))
            {
                if (this.reader == null)
                {
                    this.reader = new ApsimTextFile();
                    this.reader.Open(this.FullFileName, this.ExcelWorkSheetName);

                    this.regionIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, "Region");
                    this.soilIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, "Soil");
                    this.forageNoIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, "ForageNo");
                    this.forageNameIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, "ForageName");
                    this.yearNumIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, "YearNum");
                    this.yearIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, "Year");
                    this.cutNumIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, "CutNum");
                    this.monthIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, "Month");
                    this.growthIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, "Growth");
                    this.nPerCentIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, "NPerCent");
                    this.priorityIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, "Priority");
                    this.bp1Index = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, "BP1");
                    this.bp2Index = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, "BP2");

                    if (this.regionIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant("Region") == null)
                            throw new Exception("Cannot find Region in APSIM Forage file: " + this.FullFileName);
                    }

                    if (this.soilIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant("Soil") == null)
                            throw new Exception("Cannot find Soil in APSIM Forage file: " + this.FullFileName);
                    }

                    if (this.forageNoIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant("ForageNo") == null)
                            throw new Exception("Cannot find CropNo in APSIM Forage file: " + this.FullFileName);
                    }

                    if (this.forageNameIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant("ForageName") == null)
                            throw new Exception("Cannot find CropName in APSIM Forage file: " + this.FullFileName);
                    }

                    if (this.yearNumIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant("YearNum") == null)
                            throw new Exception("Cannot find YearNum in APSIM Forage file: " + this.FullFileName);
                    }

                    if (this.yearIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant("Year") == null)
                            throw new Exception("Cannot find Year in APSIM Forage file: " + this.FullFileName);
                    }

                    if (this.cutNumIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant("CutNum") == null)
                            throw new Exception("Cannot find CutNum in APSIM Forage file: " + this.FullFileName);
                    }

                    if (this.monthIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant("Month") == null)
                            throw new Exception("Cannot find Month in APSIM Forage file: " + this.FullFileName);
                    }

                    if (this.growthIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant("Growth") == null)
                            throw new Exception("Cannot find Growth in APSIM Forage file: " + this.FullFileName);
                    }

                    if (this.nPerCentIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant("NPerCent") == null)
                            throw new Exception("Cannot find NPerCent in APSIM Forage file: " + this.FullFileName);
                    }

                    if (this.priorityIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant("Priority") == null)
                            throw new Exception("Cannot find Priority in APSIM Forage file: " + this.FullFileName);
                    }

                    if (this.bp1Index == -1)
                    {
                        if (this.reader == null || this.reader.Constant("BP1") == null)
                            throw new Exception("Cannot find BP1 in APSIM Forage file: " + this.FullFileName);
                    }

                    if (this.bp2Index == -1)
                    {
                        if (this.reader == null || this.reader.Constant("BP2") == null)
                            throw new Exception("Cannot find BP2 in APSIM Forage file: " + this.FullFileName);
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


    }



    /// <summary>
    /// A structure containing the commonly used weather data.
    /// </summary>
    [Serializable]
    public class ForageDataType
    {
        /// <summary>
        /// Region Number
        /// </summary>
        public int Region;

        /// <summary>
        /// Soil Number
        /// </summary>
        public int Soil;

        /// <summary>
        /// Crop Number 
        /// </summary>
        public int ForageNo;

        /// <summary>
        /// Name of Crop
        /// </summary>
        public string ForageName;

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
        /// Amout in Kg of Biomass of the Crop
        /// </summary>
        public double Growth;

        /// <summary>
        /// Nitrogen Percentage of the Biomass of the Crop
        /// </summary>
        public double NPerCent;

        /// <summary>
        /// Unsure ?
        /// </summary>
        public int Priority;

        /// <summary>
        /// Amount in Kg of By Product 1 of the production of this crop
        /// </summary>
        public double BP1;

        /// <summary>
        /// Amount in Kg of By Product 2 of the production of this crop
        /// </summary>
        public double BP2;

        /// <summary>
        /// Combine Year and Month to create a DateTime. 
        /// Day is set to the 1st of the month.
        /// </summary>
        public DateTime CutDate;
    }



}