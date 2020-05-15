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
using Models.Core.Attributes;
using Models.CLEM.Activities;
using System.Globalization;
using System.Linq;

namespace Models.CLEM
{
    ///<summary>
    /// SQLite database reader for access to GRASP data for other models.
    ///</summary>
    ///<remarks>
    ///</remarks>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")] //CLEMFileSQLiteGRASPView
    [PresenterName("UserInterface.Presenters.PropertyPresenter")] //CLEMFileSQLiteGRASPPresenter
    [ValidParent(ParentType = typeof(ZoneCLEM))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [ValidParent(ParentType = typeof(PastureActivityManage))]
    [Description("This component reads a SQLite database with GRASP data for native pasture production used in the CLEM simulation.")]
    [Version(1, 0, 1, "")]
    [Version(1, 0, 2, "Added ability to define table and columns to use")]
    [Version(1, 0, 3, "Includes access to ecological indicators from database")]
    [Version(1, 0, 4, "Allow more categories of land condition and grass basal area in datacube lookup")]
    [HelpUri(@"Content/Features/DataReaders/GRASPDataReaderSQL.htm")]
    public class FileSQLiteGRASP : CLEMModel, IFileGRASP, IValidatableObject
    {
        /// <summary>
        /// A link to the clock model.
        /// </summary>
        [Link]
        private readonly Clock clock = null;

        private List<ValidationResult> validationResults;
        private RainfallShuffler shuffler = null;

        /// <summary>
        /// All the distinct Stocking Rates that were found in the database
        /// </summary>
        [XmlIgnore]
        private double[] distinctStkRates;
        [XmlIgnore]
        private double[] distinctGBAs;
        [XmlIgnore]
        private double[] distinctLandConditions;

        /// <summary>
        /// Gets or sets the file name. Should be relative filename where possible.
        /// </summary>
        [Summary]
        [Description("Pasture database file name")]
        [Models.Core.Display(Type = DisplayType.FileName)]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Pasture database file name must be supplied")]
        public string FileName { get; set; }

        /// <summary>
        /// Defines the name of the table in the database holding the pasture data.
        /// </summary>
        [Summary]
        [Description("Database table name")]
        [System.ComponentModel.DefaultValueAttribute("Native_Inputs")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Database table name must be supplied")]
        public string TableName { get; set; }

        /// <summary>
        /// Name of column holding region id data
        /// </summary>
        [Summary]
        [System.ComponentModel.DefaultValueAttribute("Region")]
        [Description("Column name for region id")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Region id column name must be supplied")]
        public string RegionColumnName { get; set; }

        /// <summary>
        /// Name of column holding land id data
        /// </summary>
        [Summary]
        [System.ComponentModel.DefaultValueAttribute("Soil")]
        [Description("Column name for land id")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Land id column name must be supplied")]
        public string LandIdColumnName { get; set; }

        /// <summary>
        /// Name of column holding grass basal area data
        /// </summary>
        [Summary]
        [System.ComponentModel.DefaultValueAttribute("GrassBA")]
        [Description("Column name for grass basal area")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Grass basal area column name must be supplied")]
        public string GrassBAColumnName { get; set; }

        /// <summary>
        /// Name of column holding land condition data
        /// </summary>
        [Summary]
        [System.ComponentModel.DefaultValueAttribute("LandCon")]
        [Description("Column name for land condition")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Land condition column name must be supplied")]
        public string LandConColumnName { get; set; }

        /// <summary>
        /// Name of column holding stocking rate data
        /// </summary>
        [Summary]
        [System.ComponentModel.DefaultValueAttribute("StkRate")]
        [Description("Column name for stocking rate")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Stocking rate column name must be supplied")]
        public string StkRateColumnName { get; set; }

        /// <summary>
        /// Name of column holding year data
        /// </summary>
        [Summary]
        [System.ComponentModel.DefaultValueAttribute("Year")]
        [Description("Column name for year")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Year column name must be supplied")]
        public string YearColumnName { get; set; }

        /// <summary>
        /// Name of column holding month data
        /// </summary>
        [Summary]
        [System.ComponentModel.DefaultValueAttribute("Month")]
        [Description("Column name for month")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Month column name must be supplied")]
        public string MonthColumnName { get; set; }

        /// <summary>
        /// Name of column holding growth data
        /// </summary>
        [Summary]
        [System.ComponentModel.DefaultValueAttribute("Growth")]
        [Description("Column name for growth")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Growth column name must be supplied")]
        public string GrowthColumnName { get; set; }

        /// <summary>
        /// Name of column holding erosion soilloss data
        /// </summary>
        [Summary]
        [System.ComponentModel.DefaultValueAttribute("soilloss")]
        [Description("Column name for erosion")]
        public string ErosionColumnName { get; set; }

        /// <summary>
        /// Name of column holding runoff data
        /// </summary>
        [Summary]
        [System.ComponentModel.DefaultValueAttribute("runoff")]
        [Description("Column name for runoff")]
        public string RunoffColumnName { get; set; }

        /// <summary>
        /// Name of column holding rainfall data
        /// </summary>
        [Summary]
        [System.ComponentModel.DefaultValueAttribute("rainfall")]
        [Description("Column name for rainfall")]
        public string RainfallColumnName { get; set; }

        /// <summary>
        /// Name of column holding cover data
        /// </summary>
        [Summary]
        [System.ComponentModel.DefaultValueAttribute("cover")]
        [Description("Column name for cover")]
        public string CoverColumnName { get; set; }

        /// <summary>
        /// Name of column holding tree basal area data
        /// </summary>
        [Summary]
        [System.ComponentModel.DefaultValueAttribute("treeba")]
        [Description("Column name for tree basal area")]
        public string TBAColumnName { get; set; }

        /// <summary>
        /// APSIMx SQLite class
        /// </summary>
        [NonSerialized]
        SQLite SQLiteReader = null;

        /// <summary>
        /// Provides an error message to display if something is wrong.
        /// The message is displayed in the warning label of the View.
        /// </summary>
        [XmlIgnore]
        public string ErrorMessage = string.Empty;

        /// <summary>
        /// Constructor
        /// </summary>
        public FileSQLiteGRASP()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.FileReader;
            this.SetDefaults();
        }

        /// <summary>
        /// Does file exist
        /// </summary>
        public bool FileExists
        {
            get { return File.Exists(this.FullFileName); }
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMInitialiseResource")]
        private void OnCLEMInitialiseResource(object sender, EventArgs e)
        {
            // look for a shuffler
            shuffler = Apsim.Children(this, typeof(RainfallShuffler)).FirstOrDefault() as RainfallShuffler;
        }

        /// <summary>
        /// Opens the SQLite database if necessary
        /// </summary>
        /// <returns>true if open suceeded, false if the opening failed </returns>
        private bool OpenSQLiteDB()
        {
            if (SQLiteReader == null)
            {
                if (this.FullFileName == null || this.FullFileName == "")
                {
                    ErrorMessage = "File name for the SQLite database is missing";
                    return false;
                }
                    
                if (System.IO.File.Exists(this.FullFileName))
                {
                    // check SQL file
                    SQLiteReader = new SQLite();
                    try
                    {
                        SQLiteReader.OpenDatabase(FullFileName, true);
                        return true;
                    }
                    catch (Exception ex)
                    {
                        ErrorMessage = "@error:There was a problem opening the SQLite database [o=" + FullFileName + "for [x=" + this.Name +"]\n" + ex.Message;
                        return false;
                    }
                }
                else
                {
                    ErrorMessage = "@error:The SQLite database [o=" + FullFileName + "] could not be found for [x="+this.Name+"]";
                    return false;
                }
            }
            else
            {
                //it's already open
                return true;  
            } 
        }

        /// <summary>
        /// Searches the DataTable created from the GRASP File for all the distinct values for the specified ColumnName.
        /// </summary>
        /// <returns>Sorted array of unique values for the column</returns>
        private double[] GetCategories(string columnName)
        {
            //if the SQLite Database can't be opened throw an exception.
            if (OpenSQLiteDB() == false)
            {
                throw new Exception(ErrorMessage);
            }
            else
            {
                DataTable res = SQLiteReader.ExecuteQuery("SELECT DISTINCT " + columnName + " FROM "+ TableName + " ORDER BY " + columnName + " ASC");

                double[] results = new double[res.Rows.Count];
                int i = 0;
                foreach (DataRow row in res.Rows)
                {
                    results[i] = Convert.ToDouble(row[0], CultureInfo.InvariantCulture);
                    i++;
                }
                return results;
            }
        }

        /// <summary>
        /// Validate model
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            // check for file
            if (!this.FileExists)
            {
                string[] memberNames = new string[] { "FileName" };
                results.Add(new ValidationResult("The SQLite database [x="+FullFileName+"] could not be found for ["+this.Name+"]", memberNames));
            }
            else
            {
                // check SQL file
                SQLiteReader = new SQLite();
                try
                {
                    SQLiteReader.OpenDatabase(FullFileName, true);
                }
                catch(Exception ex)
                {
                    throw new ApsimXException(this, "There was a problem opening the SQLite database [x=" + FullFileName + "] for [" + this.Name + "]\n" + ((ex.Message == "file is not a database")?"The file is not a supported SQLite database":ex.Message));
                }

                // check all columns present
                List<string> expectedColumns = new List<string>()
                {
                    RegionColumnName,
                    LandIdColumnName,
                    GrassBAColumnName,
                    LandConColumnName,
                    StkRateColumnName,
                    YearColumnName,
                    MonthColumnName,
                    GrowthColumnName
                };

                // add extra data columns if specified
                if (ErosionColumnName != null || ErosionColumnName != "")
                {
                    expectedColumns.Add(ErosionColumnName);
                }
                if (RunoffColumnName != null || RunoffColumnName != "")
                {
                    expectedColumns.Add(RunoffColumnName);
                }
                if (RainfallColumnName != null || RainfallColumnName != "")
                {
                    expectedColumns.Add(RainfallColumnName);
                }
                if (CoverColumnName != null || CoverColumnName != "")
                {
                    expectedColumns.Add(CoverColumnName);
                }
                if (TBAColumnName != null || TBAColumnName != "")
                {
                    expectedColumns.Add(TBAColumnName);
                }

                try
                {
                    DataTable res = SQLiteReader.ExecuteQuery("PRAGMA table_info(" + TableName + ")");
                    List<string> dBcolumns = new List<string>();
                    foreach (DataRow row in res.Rows)
                    {
                        dBcolumns.Add(row[1].ToString());
                    }

                    foreach (string col in expectedColumns)
                    {
                        if (!dBcolumns.Contains(col))
                        {
                            string[] memberNames = new string[] { "Missing SQLite database column" };
                            results.Add(new ValidationResult("Unable to find column [o=" + col + "] in GRASP database [x=" + FullFileName + "] for [" + this.Name + "]", memberNames));
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new ApsimXException(this, "There was a problem opening the SQLite database [x=" + FullFileName + "] for [" + this.Name + "]\n" + ((ex.Message == "file is not a database") ? "The file is not a supported SQLite database" : ex.Message));
                }
            }
            return results;
        }

        #region Properties and Methods for populating the User Interface with data

        /// <summary>
        /// Gets or sets the full file name (with path). The user interface uses this. 
        /// Must be a property so that the Prsenter can use a  Commands.ChangeProperty() on it.
        /// ChangeProperty does not work on fields.
        /// </summary>
        [XmlIgnore]
        public string FullFileName
        {
            get
            {
                if (Apsim.Parent(this, typeof(Simulation)) is Simulation simulation && this.FileName != null)
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
        /// Gets the first year in the SQLite File
        /// </summary>
        /// <returns></returns>
        public double[] GetYearsInFile()
        {
            return GetCategories(YearColumnName);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public DataTable GetTable(int startYear, int endYear)
        {
            if (OpenSQLiteDB() == false)
            {
                return null;
            }

            string sqlQuery = "SELECT  " +
            RegionColumnName + "," +
            LandIdColumnName + "," +
            GrassBAColumnName + "," +
            LandConColumnName + "," +
            StkRateColumnName + "," +
            YearColumnName + "," +
            //"CutNum," +
            MonthColumnName + "," +
            GrowthColumnName;
            //"BP1," +
            //"BP2" +

            if (ErosionColumnName != null && ErosionColumnName != "")
            {
                sqlQuery += "," + ErosionColumnName;
            }
            if (RunoffColumnName != null || RunoffColumnName != "")
            {
                sqlQuery += "," + RunoffColumnName;
            }
            if (RainfallColumnName != null || RainfallColumnName != "")
            {
                sqlQuery += "," + RainfallColumnName;
            }
            if (CoverColumnName != null || CoverColumnName != "")
            {
                sqlQuery += "," + CoverColumnName;
            }
            if (TBAColumnName != null || TBAColumnName != "")
            {
                sqlQuery += "," + TBAColumnName;
            }

            sqlQuery += " FROM " + TableName;
            sqlQuery += " WHERE "+YearColumnName+" BETWEEN " + startYear + " AND " + endYear;

            try
            {
                DataTable results = SQLiteReader.ExecuteQuery(sqlQuery);
                results.DefaultView.Sort = YearColumnName+", "+MonthColumnName;
                return results;
            }
            catch (Exception err)
            {
                SQLiteReader.CloseDatabase();
                ErrorMessage = err.Message;
                return null;
            }
        }

        #endregion

        #region Event Handlers for Running Simulation

        /// <summary>An event handler to allow us to initialise</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("StartOfSimulation")]
        private void OnStartOfSimulation(object sender, EventArgs e)
        {
            //if the SQLite Database can't be opened throw an exception.
            var validationContext = new ValidationContext(this, null, null);
            validationResults = new List<ValidationResult>();
            Validator.TryValidateObject(this, validationContext, validationResults, true);

            if (OpenSQLiteDB() == false)
            { 
                throw new Exception(ErrorMessage);
            }

            // get list of distinct stocking rates available in database
            // database has already been opened and checked in Validate()
            this.distinctStkRates = GetCategories(StkRateColumnName);
            this.distinctGBAs = GetCategories(GrassBAColumnName);
            this.distinctLandConditions = GetCategories(LandConColumnName);
        }

        /// <summary>
        /// Overrides the base class method to allow for clean up
        /// </summary>
        [EventSubscribe("Completed")]
        private void OnSimulationCompleted(object sender, EventArgs e)
        {
            if(SQLiteReader != null)
            {
                SQLiteReader.CloseDatabase();
                SQLiteReader = null;
            }
        }

        /// <summary>
        /// Finds the closest Value of categorised lookup values form the database
        /// This applies to Stocking rates, Grass Basal Area (or use GBA) and Land Condition
        /// The GRASP file does not have every stocking rate, grass basal area or land condition. 
        /// It will find the category with the next largest value to the actual value supplied.
        /// So if the value is 0 the category with the next largest value will normally be the first entry
        /// </summary>
        /// <param name="category">The name of the distict categories to use</param>
        /// <param name="value">The value to search for</param>
        /// <returns></returns>
        private double FindClosestCategory(string category, double value)
        {
            double[] valuesToUse;
            switch (category)
            {
                case "StockingRate":
                    valuesToUse = distinctStkRates;
                    break;
                case "GrassBasalArea":
                case "GBA":
                    valuesToUse = distinctGBAs;
                    break;
                case "LandCondition":
                    valuesToUse = distinctLandConditions;
                    break;
                default:
                    throw new ApsimXException(this, $"Unknown GRASP data cetegory [{category}] used in code behind [x={this.Name}]");
            }

            // sorting not needed as now done at array creation
            int index = Array.BinarySearch(valuesToUse, value);
            return (index < 0) ? valuesToUse[~index] : valuesToUse[index];
        }

        /// <summary>
        /// Queries the the GRASP SQLite database using the specified parameters.
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
            List<PastureDataType> pastureDetails = new List<PastureDataType>();

            if (validationResults.Count > 0 | ecolCalculationDate > clock.EndDate)
            {
                return pastureDetails;
            }

            int startYear = ecolCalculationDate.Year;
            int startMonth = ecolCalculationDate.Month;
            DateTime endDate = ecolCalculationDate.AddMonths(ecolCalculationInterval+1);
            if(endDate > clock.EndDate)
            {
                endDate = clock.EndDate;
            }
            int endYear = endDate.Year;
            int endMonth = endDate.Month;

            double stkRateCategory = FindClosestCategory("StockingRate", stockingRate);
            double grassBasalAreaCategory = FindClosestCategory("GBA", grassBasalArea);
            double landConditionCategory = FindClosestCategory("LandCondition", landCondition);

            string sqlQuery = "SELECT " +
                YearColumnName + ", " +
                MonthColumnName + "," +
                GrowthColumnName;

            if (ErosionColumnName != null && ErosionColumnName != "")
            {
                sqlQuery += "," + ErosionColumnName;
            }
            if (RunoffColumnName != null || RunoffColumnName != "")
            {
                sqlQuery += "," + RunoffColumnName;
            }
            if (RainfallColumnName != null || RainfallColumnName != "")
            {
                sqlQuery += "," + RainfallColumnName;
            }
            if (CoverColumnName != null || CoverColumnName != "")
            {
                sqlQuery += "," + CoverColumnName;
            }
            if (TBAColumnName != null || TBAColumnName != "")
            {
                sqlQuery += "," + TBAColumnName;
            }

            sqlQuery += " FROM " + TableName +
                " WHERE "+RegionColumnName+" = " + region +
                " AND "+LandIdColumnName+" = " + soil +
                " AND "+GrassBAColumnName+" = " + grassBasalAreaCategory +
                " AND "+LandConColumnName+" = " + landConditionCategory +
                " AND "+StkRateColumnName+" = " + stkRateCategory;

            if (shuffler != null)
            {
                int shuffleStartYear = shuffler.ShuffledYears.Where(a => a.Year == startYear).FirstOrDefault().RandomYear;
                int shuffleEndYear = shuffler.ShuffledYears.Where(a => a.Year == endYear).FirstOrDefault().RandomYear;

                // first year
                sqlQuery += " AND (( " + YearColumnName + " = " + shuffleStartYear + " AND " + MonthColumnName + " >= " + startMonth + ")";

                // any middle years
                for (int i = startYear+1; i < endYear; i++)
                {
                    sqlQuery += " OR ( " + YearColumnName + " = " + shuffler.ShuffledYears[i] + ")";
                }

                //last year
                sqlQuery += " OR ( " + YearColumnName + " = " + shuffleEndYear + " AND " + MonthColumnName + " <= " + endMonth + "))";

            }
            else
            {
                if (startYear == endYear)
                {
                    sqlQuery += " AND (( " + YearColumnName + " = " + startYear + " AND " + MonthColumnName + " >= " + startMonth + " AND " + MonthColumnName + " < " + endMonth + ")"
                    + ")";
                }
                else
                {
                    sqlQuery += " AND (( " + YearColumnName + " = " + startYear + " AND " + MonthColumnName + " >= " + startMonth + ")"
                    + " OR  ( " + YearColumnName + " > " + startYear + " AND " + YearColumnName + " < " + endYear + ")"
                    + " OR  ( " + YearColumnName + " = " + endYear + " AND " + MonthColumnName + " < " + endMonth + ")"
                    + ")";
                }
            }
            
            DataTable results = SQLiteReader.ExecuteQuery(sqlQuery);
            if(results.Rows.Count == 0)
            {
                return null;
            }

            // re-label shuffled years
            if (shuffler != null)
            {
                foreach (DataRow row in results.Rows)
                {
                    row["Year"] = shuffler.ShuffledYears.Where(a => a.RandomYear == Convert.ToInt32(row["Year"])).FirstOrDefault().Year;
                }
            }

            results.DefaultView.Sort = YearColumnName + ", " + MonthColumnName;

            foreach (DataRowView row in results.DefaultView)
            {
                pastureDetails.Add(DataRow2PastureDataType(row));
            }

            CheckAllMonthsWereRetrieved(pastureDetails, ecolCalculationDate, endDate,
                region, soil, grassBasalAreaCategory, landConditionCategory, stkRateCategory);

            return pastureDetails;
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
        }

        private PastureDataType DataRow2PastureDataType(DataRowView dr)
        {
            PastureDataType pasturedata = new PastureDataType
            {
                Year = int.Parse(dr["Year"].ToString(), CultureInfo.InvariantCulture),
                Month = int.Parse(dr["Month"].ToString(), CultureInfo.InvariantCulture),
                Growth = double.Parse(dr["Growth"].ToString(), CultureInfo.InvariantCulture),
                SoilLoss = ((ErosionColumnName!="" & dr.DataView.Table.Columns.Contains(ErosionColumnName))?double.Parse(dr[ErosionColumnName].ToString(), CultureInfo.InvariantCulture):0),
                Runoff = ((RunoffColumnName != "" & dr.DataView.Table.Columns.Contains(RunoffColumnName)) ? double.Parse(dr[RunoffColumnName].ToString(), CultureInfo.InvariantCulture) : 0),
                Rainfall = ((RainfallColumnName != "" & dr.DataView.Table.Columns.Contains(RainfallColumnName)) ? double.Parse(dr[RainfallColumnName].ToString(), CultureInfo.InvariantCulture) : 0),
                Cover = ((CoverColumnName != "" & dr.DataView.Table.Columns.Contains(CoverColumnName)) ? double.Parse(dr[CoverColumnName].ToString(), CultureInfo.InvariantCulture) : 0),
                TreeBA = ((TBAColumnName != "" & dr.DataView.Table.Columns.Contains(TBAColumnName)) ? double.Parse(dr[TBAColumnName].ToString(), CultureInfo.InvariantCulture) : 0),
            };
            return pasturedata;
        }

        #endregion

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
                html += "\n</div>";
            }
            else if(!this.FileExists)
            {
                html += "The file <span class=\"errorlink\">" + FullFileName + "</span> could not be found";
                html += "\n</div>";
            }
            else
            {
                html += "Using <span class=\"filelink\">" + FileName + "</span>";
                html += "\n</div>";

                // Add table name
                html += "\n<div class=\"activityentry\" style=\"Margin-left:15px;\">";
                html += "Using table <span class=\"filelink\">" + TableName + "</span>";
                // add column links
                html += "\n<div class=\"activityentry\" style=\"Margin-left:15px;\">";

                html += "\n<div class=\"activityentry\">Column name for <span class=\"filelink\">Region id</span> is ";
                if (RegionColumnName is null || RegionColumnName == "")
                {
                    html += "<span class=\"errorlink\">NOT SET</span></div>";
                }
                else
                {
                    html += "<span class=\"setvalue\">" + RegionColumnName + "</span></div>";
                }
                html += "\n<div class=\"activityentry\">Column name for <span class=\"filelink\">Land id</span> is ";
                if (LandIdColumnName is null || LandIdColumnName == "")
                {
                    html += "<span class=\"errorlink\">NOT SET</span></div>";
                }
                else
                {
                    html += "<span class=\"setvalue\">" + LandIdColumnName + "</span></div>";
                }
                html += "\n<div class=\"activityentry\">Column name for <span class=\"filelink\">Grass basal area</span> is ";
                if (GrassBAColumnName is null || GrassBAColumnName == "")
                {
                    html += "<span class=\"errorlink\">NOT SET</span></div>";
                }
                else
                {
                    html += "<span class=\"setvalue\">" + GrassBAColumnName + "</span></div>";
                }
                html += "\n<div class=\"activityentry\">Column name for <span class=\"filelink\">Land condition</span> is ";
                if (LandConColumnName is null || LandConColumnName == "")
                {
                    html += "<span class=\"errorlink\">NOT SET</span></div>";
                }
                else
                {
                    html += "<span class=\"setvalue\">" + LandConColumnName + "</span></div>";
                }
                html += "\n<div class=\"activityentry\">Column name for <span class=\"filelink\">Stocking rate</span> is ";
                if (StkRateColumnName is null || StkRateColumnName == "")
                {
                    html += "<span class=\"errorlink\">NOT SET</span></div>";
                }
                else
                {
                    html += "<span class=\"setvalue\">" + StkRateColumnName + "</span></div>";
                }
                html += "\n<div class=\"activityentry\">Column name for <span class=\"filelink\">Year</span> is ";
                if (YearColumnName is null || YearColumnName == "")
                {
                    html += "<span class=\"errorlink\">NOT SET</span></div>";
                }
                else
                {
                    html += "<span class=\"setvalue\">" + YearColumnName + "</span></div>";
                }
                html += "\n<div class=\"activityentry\">Column name for <span class=\"filelink\">Month</span> is ";
                if (MonthColumnName is null || MonthColumnName == "")
                {
                    html += "<span class=\"errorlink\">NOT SET</span></div>";
                }
                else
                {
                    html += "<span class=\"setvalue\">" + MonthColumnName + "</span></div>";
                }

                html += "\n<div class=\"activityentry\">Column name for <span class=\"filelink\">Growth</span> is ";
                if (GrowthColumnName is null || GrowthColumnName == "")
                {
                    html += "<span class=\"errorlink\">NOT SET</span></div>";
                }
                else
                {
                    html += "<span class=\"setvalue\">" + GrowthColumnName + "</span></div>";
                }

                // other data columns
                if (ErosionColumnName is null || ErosionColumnName == "")
                {
                    html += "\n<div class=\"activityentry\">No erosion data will be obtained from database</div>";
                }
                else
                {
                    html += "\n<div class=\"activityentry\">Erosion data will be obtained from column named ";
                    html += "<span class=\"setvalue\">" + ErosionColumnName + "</span></div>";
                }
                if (RunoffColumnName is null || RunoffColumnName == "")
                {
                    html += "\n<div class=\"activityentry\">No runoff data will be obtained from database</div>";
                }
                else
                {
                    html += "\n<div class=\"activityentry\">Runoff data will be obtained from column named ";
                    html += "<span class=\"setvalue\">" + RunoffColumnName + "</span></div>";
                }
                if (RainfallColumnName is null || RainfallColumnName == "")
                {
                    html += "\n<div class=\"activityentry\">No rainfall data will be obtained from database</div>";
                }
                else
                {
                    html += "\n<div class=\"activityentry\">Rainfall data will be obtained from column named ";
                    html += "<span class=\"setvalue\">" + RainfallColumnName + "</span></div>";
                }
                if (CoverColumnName is null || CoverColumnName == "")
                {
                    html += "\n<div class=\"activityentry\">No cover data will be obtained from database</div>";
                }
                else
                {
                    html += "\n<div class=\"activityentry\">Cover data will be obtained from column named ";
                    html += "<span class=\"setvalue\">" + CoverColumnName + "</span></div>";
                }
                if (TBAColumnName is null || TBAColumnName == "")
                {
                    html += "\n<div class=\"activityentry\">No tree basal area data will be obtained from database</div>";
                }
                else
                {
                    html += "\n<div class=\"activityentry\">Tree basal area data will be obtained from column named ";
                    html += "<span class=\"setvalue\">" + TBAColumnName + "</span></div>";
                }
                html += "\n</div>";
                html += "\n</div>";
            }
            return html;
        }
    }
}