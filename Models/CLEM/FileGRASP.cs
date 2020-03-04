using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Xml.Serialization;
using Models.Core;
using APSIM.Shared.Utilities;
using System.ComponentModel.DataAnnotations;
using Models.Core.Attributes;
using System.Globalization;

namespace Models.CLEM
{
    ///<summary>
    /// Reads in GRASP file data and makes it available to other models.
    ///</summary>
    ///    
    ///<remarks>
    ///</remarks>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")] //CLEMFileGRASPView
    [PresenterName("UserInterface.Presenters.PropertyPresenter")] //CLEMFileGRASPPresenter
    // No longer supported. Ignore from tree or ability to add until deleted.
    //[ValidParent(ParentType = typeof(ZoneCLEM))]
    //[ValidParent(ParentType = typeof(ActivityFolder))]
    //[ValidParent(ParentType = typeof(PastureActivityManage))]
    [Description("This model holds a GRASP data file for native pasture used in the CLEM simulation.")]
    [Version(1, 0, 2, "This component is no longer supported.\nUse the FileSQLiteGRASP reader for best performance.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/DataReaders/GRASPDataReader.htm")]
    public class FileGRASP : CLEMModel, IFileGRASP
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
        /// The index of the climate region number column in the GRASP file
        /// </summary>
        private int regionIndex;

        /// <summary>
        /// The index of the soil number column in the GRASP file
        /// </summary>
        private int soilIndex;

        /// <summary>
        /// The index of the forage number column in the GRASP file
        /// nb. This column is to be ignored.
        /// It is a legacy column in the GRASP file and is not used any more.
        /// </summary>
        private int forageNoIndex;

        /// <summary>
        /// The index of the grass basal area column in the GRASP file
        /// </summary>
        private int grassBAIndex;

        /// <summary>
        /// The index of the land condition column in the GRASP file
        /// </summary>
        private int landConIndex;

        /// <summary>
        /// The index of the stocking rate column in the GRASP file
        /// nb. a row does NOT exist for every stocking rate.
        /// instead only certain stocking rate categories have rows.
        /// We need to find the closest categegory in the GRASP file
        /// to the actual stocking rate in any given month.
        /// These stocking rate categories vary from GRASP file to GRASP file
        /// and are not standarised categories.
        /// </summary>
        private int stkRateIndex;

        /// <summary>
        /// The index of the year number column in the GRASP file
        /// This is NOT the actually date year, this is the number of
        /// years since the start of the GRASP run that generated the
        /// GRASP data. 
        /// eg. it starts at 1 and goes up sequentially 
        /// for however many years the GRASP run went for.
        /// </summary>
        private int yearNumIndex;

        /// <summary>
        /// The index of the year column in the GRASP file.
        /// This is the actual date year
        /// eg.1975
        /// </summary>
        private int yearIndex;

        /// <summary>
        /// The index of the cut number column in the GRASP file
        /// Some crops such as lucerne are ratooning crops.
        /// So we need to provide a cut number to keep track of
        /// how many harvests from the original planting of the
        /// crop. Cut Number = 1 is the first harvest after planting
        /// and it goes up from there until it is pulled out and
        /// replanted.
        /// nb. you may have multiple cuts in the one month so 
        /// year and month does not uniquely identify the monthly
        /// yield data for that month. 
        /// We need to add up all the cuts within that month and use
        /// this as the monthly yield data for these crops.
        /// </summary>
        private int cutNumIndex;

        /// <summary>
        /// The index of the month number column in the GRASP file
        /// eg. 1 to 12 (for Jan to Dec)
        /// </summary>
        private int monthIndex;

        /// <summary>
        /// The index of the growth amount column in the GRASP file
        /// </summary>
        private int growthIndex;

        /// <summary>
        /// The index of the by product 1 column in the GRASP file
        /// Crops can have by products that are produced as a consequence
        /// of growing the crop. 
        /// This is the amount of the first by product of this crop
        /// Eg. straw from growing wheat grain.
        /// nb. THIS IS NOT REALLY USED BY PASTURES
        /// </summary>
        private int bp1Index;

        /// <summary>
        /// The index of the by product 2 (second) column in the GRASP file
        /// Crops can have by products that are produced as a consequence
        /// of growing the crop. 
        /// This is the amount of the second by product of this crop
        /// Eg. grain husks from growing wheat grain.
        /// nb. THIS IS NOT REALLY USED BY PASTURES. 
        /// </summary>
        private int bp2Index;

        /// <summary>
        /// The index of the utilisation column in the GRASP file
        /// The fractional proportional green growth pasture growth that the animals ate.
        /// </summary>
        private int utilisnIndex;

        /// <summary>
        /// The index of the soil loss column in the GRASP file
        /// erosion caused to soil by your stocking number on this pasture growth.
        /// </summary>
        private int soillossIndex;

        /// <summary>
        /// The index of the cover column in the GRASP file
        /// fraction of the soil surface that has cover (both dead and green) over it.
        /// </summary>
        private int coverIndex;

        /// <summary>
        /// The index of the tree basal area column in the GRASP file
        /// </summary>
        private int treeBAIndex;

        /// <summary>
        /// The index of the rainfall column in the GRASP file
        /// </summary>
        private int rainfallIndex;

        /// <summary>
        /// The index of the runoff column in the GRASP file
        /// </summary>
        private int runoffIndex;

        /// <summary>
        /// The entire pasture File read in as a DataTable with Primary Keys assigned.
        /// </summary>
        private DataTable pastureFileAsTable;

        /// <summary>
        /// All the distinct Stocking Rates that were found in the PastureFileAsDataTable
        /// </summary>
        private double[] distinctStkRates;

        /// <summary>
        /// Constructor
        /// </summary>
        public FileGRASP()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.FileReader;
        }

        /// <summary>
        /// Does file exist
        /// </summary>
        public bool FileExists
        {
            get { return File.Exists(this.FullFileName); }
        }

        /// <summary>
        /// Gets or sets the file name. Should be relative filename where possible.
        /// </summary>
        [Summary]
        [Description("Pasture file name")]
        [Models.Core.Display(Type = DisplayType.FileName)]
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
                if (simulation != null && this.FileName != null)
                {
                    return PathUtilities.GetAbsolutePath(this.FileName, simulation.FileName);
                }
                else
                {
                    return this.FileName;
                }
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
            // check filename exists
            if(!this.FileExists)
            {
                throw new ApsimXException(this, "@error:The database[o="+FullFileName+"] could not be found for [x="+this.Name+"]");
            }

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

            this.pastureFileAsTable = GetAllData();
            this.distinctStkRates = GetStkRateCategories();
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

            if (this.pastureFileAsTable != null)
            {
                this.pastureFileAsTable.Dispose();
                this.pastureFileAsTable = null;
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
                List<string> pastureProps = new List<string>
                {
                    "Region",
                    "Soil",
                    "ForageNo",
                    "GrassBA",
                    "LandCon",
                    "StkRate",
                    "YearNum",
                    "Year",
                    "CutNum",
                    "Month",
                    "Growth",
                    "BP1",
                    "BP2",
                    "Utilisn",
                    "SoilLoss",
                    "Cover",
                    "TreeBA",
                    "Rainfall",
                    "Runoff"
                };

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
        /// Searches the DataTable created from the GRASP File for all the distinct StkRate values.
        /// </summary>
        /// <returns></returns>
        private double[] GetStkRateCategories()
        {

            DataView dataview = new DataView(this.pastureFileAsTable);
            DataTable distinctStkRates = dataview.ToTable(true, "StkRate");

            double[] results = new double[distinctStkRates.Rows.Count];
            int i = 0;
            foreach (DataRow row in distinctStkRates.Rows)
            {
                results[i] = Convert.ToDouble(row["StkRate"], CultureInfo.InvariantCulture);
                i++;
            }

            return results;
        }

        /// <summary>
        /// Finds the closest Stocking Rate Category in the GRASP file for a given Stocking Rate.
        ///The GRASP file does not have every stocking rate. 
        ///Each GRASP file has its own set of stocking rate value categories
        ///Need to find the closest the stocking rate category in the GRASP file for this stocking rate.
        ///It will find the category with the next largest value to the actual stocking rate.
        ///So if the stocking rate is 0 the category with the next largest value will normally be 1
        /// </summary>
        /// <param name="stockingRate"></param>
        /// <returns></returns>
        private double FindClosestStkRateCategory(double stockingRate)
        {
            //https://stackoverflow.com/questions/41277957/get-closest-value-in-an-array
            //https://msdn.microsoft.com/en-us/library/2cy9f6wb(v=vs.110).aspx
            Array.Sort(distinctStkRates);
            int index = Array.BinarySearch(distinctStkRates, stockingRate); 
            double category = (index < 0) ? distinctStkRates[~index] : distinctStkRates[index];
            return category;
        }

        /// <summary>
        /// Searches the DataTable created from the GRASP File using the specified parameters.
        /// nb. Ignore ForageNo , it is a legacy column in the GRASP file that is not used anymore.
        /// </summary>
        /// <param name="region"></param>
        /// <param name="soil"></param>
        /// <param name="grassBasalArea"></param>
        /// <param name="landCondition"></param>
        /// <param name="stockingRate"></param>
        /// <param name="ecolCalculationDate"></param>
        /// <param name="ecolCalculationInterval"></param>
        /// <returns></returns>
        public List<PastureDataType> GetIntervalsPastureData(int region, string soil, double grassBasalArea, double landCondition, double stockingRate,
                                         DateTime ecolCalculationDate, int ecolCalculationInterval)
        {
            int startYear = ecolCalculationDate.Year;
            int startMonth = ecolCalculationDate.Month;
            DateTime endDate = ecolCalculationDate.AddMonths(ecolCalculationInterval+1);
            if(endDate > clock.EndDate)
            {
                endDate = clock.EndDate;
            }
            int endYear = endDate.Year;
            int endMonth = endDate.Month;

            double stkRateCategory = FindClosestStkRateCategory(stockingRate);

            //http://www.csharp-examples.net/dataview-rowfilter/

            string filter;
            if (startYear == endYear)
            {
                filter = "( Region = " + region + ") AND (Soil = " + soil + ")"
                + " AND (GrassBA = " + grassBasalArea + ") AND (LandCon = " + landCondition + ") AND (StkRate = " + stkRateCategory + ")"
                + " AND ("
                + "( Year = " + startYear + " AND Month >= " + startMonth + " AND Month < " + endMonth + ")"
                + ")";
            }
            else
            {
                filter = "( Region = " + region + ") AND (Soil = " + soil + ")"
                + " AND (GrassBA = " + grassBasalArea + ") AND (LandCon = " + landCondition + ") AND (StkRate = " + stkRateCategory + ")"
                + " AND ("
                + "( Year = " + startYear + " AND Month >= " + startMonth + ")"
                + " OR  ( Year > " + startYear + " AND Year < " + endYear + ")"
                + " OR  ( Year = " + endYear + " AND Month < " + endMonth + ")"
                + ")";
            }

            DataRow[] foundRows = this.pastureFileAsTable.Select(filter);

            List<PastureDataType> filtered = new List<PastureDataType>();

            foreach (DataRow dr in foundRows)
            {
                filtered.Add(DataRow2PastureDataType(dr));
            }

            filtered.Sort((r, s) => DateTime.Compare(r.CutDate, s.CutDate));

            CheckAllMonthsWereRetrieved(filtered, ecolCalculationDate, endDate,
                region, soil, grassBasalArea, landCondition, stockingRate);

            return filtered;
        }

        /// <summary>
        /// Do simple error checking to make sure the data retrieved is usable
        /// </summary>
        /// <param name="filtered"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="region"></param>
        /// <param name="soil"></param>
        /// <param name="grassBasalArea"></param>
        /// <param name="landCondition"></param>
        /// <param name="stockingRate"></param>
        private void CheckAllMonthsWereRetrieved(List<PastureDataType> filtered, DateTime startDate, DateTime endDate,
            int region, string soil, double grassBasalArea, double landCondition, double stockingRate)
        {
            string errormessageStart = "Problem with GRASP input file." + System.Environment.NewLine
                        + "For Region: " + region + ", Soil: " + soil 
                        + ", GrassBA: " + grassBasalArea + ", LandCon: " + landCondition + ", StkRate: " + stockingRate + System.Environment.NewLine;

            if (clock.EndDate == clock.Today)
            {
                return;
            }

            //Check if there is any data
            if ((filtered == null) || (filtered.Count == 0))
            {
                throw new ApsimXException(this, errormessageStart
                    + "Unable to retrieve any data what so ever");
            }

            //Check no gaps in the months
            DateTime tempdate = startDate;
            foreach (PastureDataType month in filtered)
            {
                if ((tempdate.Year != month.Year) || (tempdate.Month != month.Month))
                {
                    throw new ApsimXException(this, errormessageStart 
                        + "Missing entry for Year: " + month.Year + " and Month: " + month.Month);
                }
                tempdate = tempdate.AddMonths(1);
            }

            //Check months go right up until EndDate
            if ((tempdate.Month != endDate.Month)&&(tempdate.Year != endDate.Year))
            {
                throw new ApsimXException(this, errormessageStart
                        + "Missing entry for Year: " + tempdate.Year + " and Month: " + tempdate.Month);
            }
        }

        /// <summary>
        /// Searches the DataTable created from the PastureFile using the specified parameters.
        /// </summary>
        /// <param name="region"></param>
        /// <param name="soil"></param>
        /// <param name="forageNo"></param>
        /// <param name="grassBasalArea"></param>
        /// <param name="landCondition"></param>
        /// <param name="stockingRate"></param>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <returns>CropDataType containg the crop data for this month</returns>
        public PastureDataType GetMonthsPastureData(int region, int soil, int forageNo, int grassBasalArea, int landCondition, int stockingRate, 
                                         int year, int month)
        {
            object[] keyVals = new Object[8];
            keyVals[0] = region;
            keyVals[1] = soil;
            keyVals[2] = forageNo;
            keyVals[3] = grassBasalArea;
            keyVals[4] = landCondition;
            keyVals[5] = stockingRate;
            keyVals[6] = year;
            keyVals[7] = month;

            DataRow dr = this.pastureFileAsTable.Rows.Find(keyVals);

            if (dr != null)
            {
                PastureDataType pasturedata = DataRow2PastureDataType(dr);

                return pasturedata;
            }
            else
            {
                throw new ApsimXException(this, "Unable to find pasture data for : "
                    + "[Region = " + region
                    + ", Soil = " + soil
                    + ", ForageNo = " + forageNo
                    + ", GrassBA = " + grassBasalArea
                    + ", LandCon = " + landCondition
                    + ", StkRate = " + stockingRate
                    + ", Year = " + year
                    + ", Month = " + month + "]"
                    );
            }
        }

        private static PastureDataType DataRow2PastureDataType(DataRow dr)
        {
            PastureDataType pasturedata = new PastureDataType
            {
                Region = int.Parse(dr["Region"].ToString(), CultureInfo.InvariantCulture),
                Soil = int.Parse(dr["Soil"].ToString(), CultureInfo.InvariantCulture),
                ForageNo = int.Parse(dr["ForageNo"].ToString(), CultureInfo.InvariantCulture),
                GrassBA = int.Parse(dr["GrassBA"].ToString(), CultureInfo.InvariantCulture),
                LandCon = int.Parse(dr["LandCon"].ToString(), CultureInfo.InvariantCulture),
                StkRate = int.Parse(dr["StkRate"].ToString(), CultureInfo.InvariantCulture),
                YearNum = int.Parse(dr["YearNum"].ToString(), CultureInfo.InvariantCulture),
                Year = int.Parse(dr["Year"].ToString(), CultureInfo.InvariantCulture),
                CutNum = int.Parse(dr["CutNum"].ToString(), CultureInfo.InvariantCulture),
                Month = int.Parse(dr["Month"].ToString(), CultureInfo.InvariantCulture),
                Growth = double.Parse(dr["Growth"].ToString(), CultureInfo.InvariantCulture),
                BP1 = double.Parse(dr["BP1"].ToString(), CultureInfo.InvariantCulture),
                BP2 = double.Parse(dr["BP2"].ToString(), CultureInfo.InvariantCulture),
                Utilisn = double.Parse(dr["Utilisn"].ToString(), CultureInfo.InvariantCulture),
                SoilLoss = double.Parse(dr["SoilLoss"].ToString(), CultureInfo.InvariantCulture),
                Cover = double.Parse(dr["Cover"].ToString(), CultureInfo.InvariantCulture),
                TreeBA = double.Parse(dr["TreeBA"].ToString(), CultureInfo.InvariantCulture),
                Rainfall = double.Parse(dr["Rainfall"].ToString(), CultureInfo.InvariantCulture),
                Runoff = double.Parse(dr["Runoff"].ToString(), CultureInfo.InvariantCulture)
            };
            pasturedata.CutDate = new DateTime(pasturedata.Year, pasturedata.Month, 1);
            return pasturedata;
        }

        /// <summary>
        /// Open the GRASP data file.
        /// </summary>
        /// <returns>True if the file was successfully opened</returns>
        public bool OpenDataFile()
        {
            if (this.FullFileName == null || this.FullFileName == "")
            {
                return false;
            }

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
                        {
                            throw new Exception("Cannot find Region in pasture file: " + this.FullFileName);
                        }
                    }

                    if (this.soilIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant("Soil") == null)
                        {
                            throw new Exception("Cannot find Soil in pasture file: " + this.FullFileName);
                        }
                    }

                    if (this.forageNoIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant("ForageNo") == null)
                        {
                            throw new Exception("Cannot find ForageNo in pasture file: " + this.FullFileName);
                        }
                    }

                    if (this.grassBAIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant("GrassBA") == null)
                        {
                            throw new Exception("Cannot find GrassBA in pasture file: " + this.FullFileName);
                        }
                    }

                    if (this.landConIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant("LandCon") == null)
                        {
                            throw new Exception("Cannot find LandCon in pasture file: " + this.FullFileName);
                        }
                    }

                    if (this.stkRateIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant("StkRate") == null)
                        {
                            throw new Exception("Cannot find StkRate in pasture file: " + this.FullFileName);
                        }
                    }

                    if (this.yearNumIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant("YearNum") == null)
                        {
                            throw new Exception("Cannot find YearNum in pasture file: " + this.FullFileName);
                        }
                    }

                    if (this.yearIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant("Year") == null)
                        {
                            throw new Exception("Cannot find Year in pasture file: " + this.FullFileName);
                        }
                    }

                    if (this.cutNumIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant("CutNum") == null)
                        {
                            throw new Exception("Cannot find CutNum in pasture file: " + this.FullFileName);
                        }
                    }

                    if (this.monthIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant("Month") == null)
                        {
                            throw new Exception("Cannot find Month in pasture file: " + this.FullFileName);
                        }
                    }

                    if (this.growthIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant("Growth") == null)
                        {
                            throw new Exception("Cannot find Growth in pasture file: " + this.FullFileName);
                        }
                    }

                    if (this.bp1Index == -1)
                    {
                        if (this.reader == null || this.reader.Constant("BP1") == null)
                        {
                            throw new Exception("Cannot find BP1 in pasture file: " + this.FullFileName);
                        }
                    }

                    if (this.bp2Index == -1)
                    {
                        if (this.reader == null || this.reader.Constant("BP2") == null)
                        {
                            throw new Exception("Cannot find BP2 in pasture file: " + this.FullFileName);
                        }
                    }

                    if (this.utilisnIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant("Utilisn") == null)
                        {
                            throw new Exception("Cannot find Utilisn in pasture file: " + this.FullFileName);
                        }
                    }

                    if (this.soillossIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant("SoilLoss") == null)
                        {
                            throw new Exception("Cannot find SoilLoss in pasture file: " + this.FullFileName);
                        }
                    }

                    if (this.coverIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant("Cover") == null)
                        {
                            throw new Exception("Cannot find Cover in pasture file: " + this.FullFileName);
                        }
                    }

                    if (this.treeBAIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant("TreeBA") == null)
                        {
                            throw new Exception("Cannot find TreeBA in pasture file: " + this.FullFileName);
                        }
                    }

                    if (this.rainfallIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant("Rainfall") == null)
                        {
                            throw new Exception("Cannot find RainFall in pasture file: " + this.FullFileName);
                        }
                    }

                    if (this.runoffIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant("Runoff") == null)
                        {
                            throw new Exception("Cannot find Runoff in pasture file: " + this.FullFileName);
                        }
                    }
                }
                else
                {
                    if (this.reader.IsExcelFile != true)
                    {
                        this.reader.SeekToDate(this.reader.FirstDate);
                    }
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

        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            string html = "";
            html += "\n<div class=\"activityentry\">";
            if (FileName == null || FileName == "")
            {
                html += "Using <span class=\"errorlink\">[FILE NOT SET]</span>";
            }
            else if (!this.FileExists)
            {
                html += "The file <span class=\"errorlink\">" + FullFileName + "</span> could not be found";
            }
            else
            {
                html += "Using <span class=\"filelink\">" + FileName + "</span>";
            }
            html += "\n</div>";
            return html;
        }
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
        /// nb. This column is to be ignored.
        /// It is a legacy column in the GRASP file and is not used any more.
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