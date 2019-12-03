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

// -----------------------------------------------------------------------
// <copyright file="FileSQLiteGRASP.cs" company="CSIRO">
//     Copyright (c) CSIRO
// </copyright>
//-----------------------------------------------------------------------
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
    [HelpUri(@"Content/Features/DataReaders/GRASPDataReaderSQL.htm")]
    public class FileSQLiteGRASP : CLEMModel, IFileGRASP, IValidatableObject
    {
        /// <summary>
        /// A link to the clock model.
        /// </summary>
        [Link]
        private Clock clock = null;

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
        /// All the distinct Stocking Rates that were found in the database
        /// </summary>
        [XmlIgnore]
        private double[] distinctStkRates;

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
            if(!this.FileExists)
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
                    string[] memberNames = new string[] { "SQLite database error" };
                    results.Add(new ValidationResult("There was a problem opening the SQLite database [x=" + FullFileName + "] for [" + this.Name + "]\n" + ex.Message, memberNames));
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
                //"Region","Soil","GrassBA","LandCon","StkRate",
                //"Year", "Month", "Growth", "BP1", "BP2"
                };

                DataTable res = SQLiteReader.ExecuteQuery("PRAGMA table_info("+ TableName + ")");

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
                GrowthColumnName +
                //"BP1," +
                //"BP2" +
                " FROM " + TableName;
                //Region, Soil,GrassBA,LandCon,StkRate,Year,CutNum,Month,Growth,BP1,BP2 FROM Native_Inputs";
            //sqlQuery += " WHERE Year BETWEEN " + startYear + " AND " + endYear;
            sqlQuery += " WHERE "+YearColumnName+" BETWEEN " + startYear + " AND " + endYear;

            try
            {
                DataTable results = SQLiteReader.ExecuteQuery(sqlQuery);
//                results.DefaultView.Sort = "Year, Month";
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
            if (OpenSQLiteDB() == false)
            { 
                throw new Exception(ErrorMessage);
            }

            // get list of distinct stocking rates available in database
            // database has already been opened and checked in Validate()
            this.distinctStkRates = GetCategories(StkRateColumnName);
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
        /// Finds the closest Stocking Rate Category in the GRASP file for a given Stocking Rate.
        /// The GRASP file does not have every stocking rate. 
        /// Each GRASP file has its own set of stocking rate value categories
        /// Need to find the closest the stocking rate category in the GRASP file for this stocking rate.
        /// It will find the category with the next largest value to the actual stocking rate.
        /// So if the stocking rate is 0 the category with the next largest value will normally be 1
        /// </summary>
        /// <param name="stockingRate"></param>
        /// <returns></returns>
        private double FindClosestStkRateCategory(double stockingRate)
        {
            //https://stackoverflow.com/questions/41277957/get-closest-value-in-an-array
            //https://msdn.microsoft.com/en-us/library/2cy9f6wb(v=vs.110).aspx

            // sorting not needed as now done at array creation
            int index = Array.BinarySearch(distinctStkRates, stockingRate); 
            if(index < 0)
            {
                throw new ApsimXException(this, $"Unable to locate a suitable dataset for stocking rate [{stockingRate}] in [x={this.FileName}] using the [x={this.Name}] datareader");
            }
            double category = (index < 0) ? distinctStkRates[~index] : distinctStkRates[index];
            return category;
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
        public List<PastureDataType> GetIntervalsPastureData(int region, string soil, int grassBasalArea, int landCondition, int stockingRate,
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

            string sqlQuery = "SELECT "+
                YearColumnName + ", " +
                //"CutNum," +
                MonthColumnName + "," +
                GrowthColumnName +
                //"BP1," +
                //"BP2" +
                " FROM " + TableName +
                " WHERE "+RegionColumnName+" = " + region +
                " AND "+LandIdColumnName+" = " + soil +
                " AND "+GrassBAColumnName+" = " + grassBasalArea +
                " AND "+LandConColumnName+" = " + landCondition +
                " AND "+StkRateColumnName+" = " + stkRateCategory;

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
            
            DataTable results = SQLiteReader.ExecuteQuery(sqlQuery);
            if(results.Rows.Count == 0)
            {
                return null;
            }
            //results.DefaultView.Sort = "Year, Month";
            results.DefaultView.Sort = YearColumnName + ", " + MonthColumnName;

            List<PastureDataType> pastureDetails = new List<PastureDataType>();
            foreach (DataRowView row in results.DefaultView)
            {
                pastureDetails.Add(DataRow2PastureDataType(row));
            }

            CheckAllMonthsWereRetrieved(pastureDetails, ecolCalculationDate, endDate,
                region, soil, grassBasalArea, landCondition, stockingRate);

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
            int region, string soil, int grassBasalArea, int landCondition, int stockingRate)
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

        private static PastureDataType DataRow2PastureDataType(DataRowView dr)
        {
            PastureDataType pasturedata = new PastureDataType
            {
                Year = int.Parse(dr["Year"].ToString(), CultureInfo.InvariantCulture),
                //CutNum = int.Parse(dr["CutNum"].ToString(), CultureInfo.InvariantCulture),
                Month = int.Parse(dr["Month"].ToString(), CultureInfo.InvariantCulture),
                Growth = double.Parse(dr["Growth"].ToString(), CultureInfo.InvariantCulture),
                //BP1 = double.Parse(dr["BP1"].ToString(), CultureInfo.InvariantCulture),
                //BP2 = double.Parse(dr["BP2"].ToString(), CultureInfo.InvariantCulture)
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

                html += "\n</div>";
                html += "\n</div>";
            }
            return html;
        }

    }

}