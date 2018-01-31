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
    [ViewName("UserInterface.Views.CLEMFileSQLiteGRASPView")]
    [PresenterName("UserInterface.Presenters.CLEMFileSQLiteGRASPPresenter")]
    [ValidParent(ParentType=typeof(Simulation))]
    [Description("This component reads a SQLite database with GRASP data for native pasture production used in the CLEM simulation.")]
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
        [Description("Pasture file name")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Pasture file name must be supplied.")]
        public string FileName { get; set; }



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
        /// Opens the SQLite database if necessary
        /// </summary>
        /// <returns>true if open suceeded, false if the opening failed </returns>
        private bool OpenSQLiteDB()
        {
            if (SQLiteReader == null)
            {
                if (this.FullFileName == null || this.FullFileName == "")
                {
                    ErrorMessage = "File Name for the SQLite database is missing";
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
                        ErrorMessage = "There was a problem opening the SQLite database (" + FullFileName + ")\n" + ex.Message;
                        return false;
                    }
                }
                else
                {
                    ErrorMessage = "This SQLite database (" + FullFileName + ") cound not be found";
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
        private double[] GetCategories(string ColumnName)
        {
            //if the SQLite Database can't be opened throw an exception.
            if (OpenSQLiteDB() == false)
            {
                throw new Exception(ErrorMessage);
            }

            DataTable res = SQLiteReader.ExecuteQuery("SELECT DISTINCT " + ColumnName + " FROM Native_Inputs ORDER BY " + ColumnName + " ASC");

            double[] results = new double[res.Rows.Count];
            int i = 0;
            foreach (DataRow row in res.Rows)
            {
                results[i] = Convert.ToDouble(row[0]);
                i++;
            }
            return results;
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
            if(!File.Exists(FullFileName))
            {
                string[] memberNames = new string[] { "FileName" };
                results.Add(new ValidationResult("This SQLite database ("+FullFileName+") cound not be found", memberNames));
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
                    results.Add(new ValidationResult("There was a problem opening the SQLite database (" + FullFileName + ")\n"+ex.Message, memberNames));
                }

                // check all columns present
                List<string> expectedColumns = new List<string>()
                {
                "Region","Soil","GrassBA","LandCon","StkRate",
                "Year", "Month", "Growth", "BP1", "BP2"
                };

                DataTable res = SQLiteReader.ExecuteQuery("PRAGMA table_info(Native_Inputs)");

                List<string> DBcolumns = new List<string>();
                foreach (DataRow row in res.Rows)
                {
                    DBcolumns.Add(row[1].ToString());
                }

                foreach (string col in expectedColumns)
                {
                    if (!DBcolumns.Contains(col))
                    {
                        string[] memberNames = new string[] { "Missing SQLite database column" };
                        results.Add(new ValidationResult("Unable to find column " + col + " in GRASP database (" + FullFileName +")", memberNames));
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
        /// Gets the first year in the SQLite File
        /// </summary>
        /// <returns></returns>
        public double[] GetYearsInFile()
        {
            return GetCategories("Year");
        }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public DataTable GetTable(int StartYear, int EndYear)
        {
            if (OpenSQLiteDB() == false)
            {
                return null;
            }

            string SQLquery = "SELECT  Region,Soil,GrassBA,LandCon,StkRate,Year,CutNum,Month,Growth,BP1,BP2 FROM Native_Inputs";
            SQLquery += " WHERE Year BETWEEN " + StartYear + " AND " + EndYear;

            try
            {
                DataTable results = SQLiteReader.ExecuteQuery(SQLquery);
                results.DefaultView.Sort = "Year, Month";
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
            this.distinctStkRates = GetCategories("StkRate");
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
        /// <param name="StkRate"></param>
        /// <returns></returns>
        private double FindClosestStkRateCategory(double StkRate)
        {
            //https://stackoverflow.com/questions/41277957/get-closest-value-in-an-array
            //https://msdn.microsoft.com/en-us/library/2cy9f6wb(v=vs.110).aspx

            // sorting not needed as now done at array creation
            int index = Array.BinarySearch(distinctStkRates, StkRate); 
            double category = (index < 0) ? distinctStkRates[~index] : distinctStkRates[index];
            return category;
        }

        /// <summary>
        /// Queries the the GRASP SQLite database using the specified parameters.
        /// nb. Ignore ForageNo , it is a legacy column in the GRASP file that is not used anymore.
        /// </summary>
        /// <param name="Region"></param>
        /// <param name="Soil"></param>
        /// <param name="GrassBA"></param>
        /// <param name="LandCon"></param>
        /// <param name="StkRate"></param>
        /// <param name="EcolCalculationDate"></param>
        /// <param name="EcolCalculationInterval"></param>
        /// <returns></returns>
        public List<PastureDataType> GetIntervalsPastureData(int Region, int Soil, int GrassBA, int LandCon, int StkRate,
                                         DateTime EcolCalculationDate, int EcolCalculationInterval)
        {
            int startYear = EcolCalculationDate.Year;
            int startMonth = EcolCalculationDate.Month;
            DateTime EndDate = EcolCalculationDate.AddMonths(EcolCalculationInterval+1);
            if(EndDate > clock.EndDate)
            {
                EndDate = clock.EndDate;
            }
            int endYear = EndDate.Year;
            int endMonth = EndDate.Month;

            double stkRateCategory = FindClosestStkRateCategory(StkRate);

            string SQLquery = "SELECT Year,CutNum,Month,Growth,BP1,BP2 FROM Native_Inputs"
                + " WHERE Region = " + Region
                + " AND Soil = " + Soil
                + " AND GrassBA = " + GrassBA
                + " AND LandCon = " + LandCon
                + " AND StkRate = " + stkRateCategory;

            if (startYear == endYear)
            {
                SQLquery += " AND (( Year = " + startYear + " AND Month >= " + startMonth + " AND Month < " + endMonth + ")"
                + ")";
            }
            else
            {
                SQLquery += " AND (( Year = " + startYear + " AND Month >= " + startMonth + ")"
                + " OR  ( Year > " + startYear + " AND Year < " + endYear + ")"
                + " OR  ( Year = " + endYear + " AND Month < " + endMonth + ")"
                + ")"; 
            }
            //SQLquery += " ORDER BY Year,Month";

            //DateTime now = DateTime.Now;
            //DataView results;
            //string ConnectionString = $"Data Source=C:\\Data\\Source\\Repos\\ApsimX\\Prototypes\\CLEM\\NABSA_GRASP_NthQld_WdThck_Base.db;Version=3;";
            //using (var connection = new SQLiteConnection(ConnectionString))
            //{
            //    using (var dataAdapter = new SQLiteDataAdapter(SQLquery, connection))
            //    {
            //        DataSet dataSet = new DataSet();
            //        dataAdapter.Fill(dataSet);
            //        results = dataSet.Tables[0].DefaultView;
            //        dataSet.Dispose();
            //    }
            //}

            DataTable results = SQLiteReader.ExecuteQuery(SQLquery);
            results.DefaultView.Sort = "Year, Month";

            List<PastureDataType> pastureDetails = new List<PastureDataType>();
            foreach (DataRowView row in results.DefaultView)
            {
                pastureDetails.Add(DataRow2PastureDataType(row));
            }

            CheckAllMonthsWereRetrieved(pastureDetails, EcolCalculationDate, EndDate,
                Region, Soil, GrassBA, LandCon, StkRate);

            return pastureDetails;
        }


        /// <summary>
        /// Do simple error checking to make sure the data retrieved is usable
        /// </summary>
        /// <param name="Filtered"></param>
        /// <param name="StartDate"></param>
        /// <param name="EndDate"></param>
        /// <param name="Region"></param>
        /// <param name="Soil"></param>
        /// <param name="GrassBA"></param>
        /// <param name="LandCon"></param>
        /// <param name="StkRate"></param>
        private void CheckAllMonthsWereRetrieved(List<PastureDataType> Filtered, DateTime StartDate, DateTime EndDate,
            int Region, int Soil, int GrassBA, int LandCon, int StkRate)
        {
            string errormessageStart = "Problem with GRASP input file." + System.Environment.NewLine
                        + "For Region: " + Region + ", Soil: " + Soil 
                        + ", GrassBA: " + GrassBA + ", LandCon: " + LandCon + ", StkRate: " + StkRate + System.Environment.NewLine;

            if (clock.EndDate == clock.Today) return;

            //Check if there is any data
            if ((Filtered == null) || (Filtered.Count == 0))
            {
                throw new ApsimXException(this, errormessageStart
                    + "Unable to retrieve any data what so ever");
            }

            //Check no gaps in the months
            DateTime tempdate = StartDate;
            foreach (PastureDataType month in Filtered)
            {
                if ((tempdate.Year != month.Year) || (tempdate.Month != month.Month))
                {
                    throw new ApsimXException(this, errormessageStart 
                        + "Missing entry for Year: " + month.Year + " and Month: " + month.Month);
                }
                tempdate = tempdate.AddMonths(1);
            }

            //Check months go right up until EndDate
            if ((tempdate.Month != EndDate.Month)&&(tempdate.Year != EndDate.Year))
            {
                throw new ApsimXException(this, errormessageStart
                        + "Missing entry for Year: " + tempdate.Year + " and Month: " + tempdate.Month);
            }
            

        }

        private static PastureDataType DataRow2PastureDataType(DataRowView dr)
        {
            PastureDataType pasturedata = new PastureDataType
            {
                Year = int.Parse(dr["Year"].ToString()),
                CutNum = int.Parse(dr["CutNum"].ToString()),
                Month = int.Parse(dr["Month"].ToString()),
                Growth = double.Parse(dr["Growth"].ToString()),
                BP1 = double.Parse(dr["BP1"].ToString()),
                BP2 = double.Parse(dr["BP2"].ToString())
            };
            return pasturedata;
        }




        #endregion

    }

}