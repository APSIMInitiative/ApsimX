using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Models.Core;
using APSIM.Shared.Utilities;
using Models.Interfaces;
using System.ComponentModel.DataAnnotations;
using Models.Core.Attributes;
using Models.CLEM.Activities;
using System.Globalization;

namespace Models.CLEM
{
    ///<summary>
    /// Reads in crop growth data and makes it available to other models.
    ///</summary>
    ///    
    ///<remarks>
    ///</remarks>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")] 
    [PresenterName("UserInterface.Presenters.PropertyPresenter")] 
    [ValidParent(ParentType = typeof(ZoneCLEM))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This component specifies a crop data file for the CLEM simulation")]
    [Version(1, 0, 5, "Fixed problem with passing soil type filter")]
    [Version(1, 0, 4, "Problem with pasture nitrogen allocation resulting in very poor pasture quality now fixed")]
    [Version(1, 0, 3, "Added ability to use Excel spreadsheets with given worksheet name")]
    [Version(1, 0, 2, "Added customisable column names.\r\nDelete and recreate old FileCrop components to set default values as previously used.")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/DataReaders/CropDataReader.htm")]
    public class FileCrop : CLEMModel, IFileCrop
    {
        /// <summary>
        /// A reference to the text file reader object
        /// </summary>
        [NonSerialized]
        private ApsimTextFile reader = null;

        /// <summary>
        /// The character spacing index for the SoilNum column
        /// </summary>
        private int soilNumIndex;

        /// <summary>
        /// The character spacing index for the CropName column
        /// </summary>
        private int cropNameIndex;

        /// <summary>
        /// The character spacing index for the Year column
        /// </summary>
        private int yearIndex;

        /// <summary>
        /// The character spacing index for the Month column
        /// </summary>
        private int monthIndex;

        /// <summary>
        /// The character spacing index for the AmtKg column
        /// </summary>
        private int amountKgIndex;

        /// <summary>
        /// The character spacing index for the Npct column
        /// </summary>
        private int nitrogenPercentIndex;

        /// <summary>
        /// The character spacing index for the harvest type column
        /// </summary>
        private int harvestTypeIndex;

        /// <summary>
        /// The entire Crop File read in as a DataTable with Primary Keys assigned.
        /// </summary>
        private DataTable forageFileAsTable;

        /// <summary>
        /// Gets or sets the file name. Should be relative filename where possible.
        /// </summary>
        [Summary]
        [Description("Crop file name")]
        [Models.Core.Display(Type=DisplayType.FileName)]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Crop file name must be supplied")]
        public string FileName { get; set; }

        /// <summary>
        /// Used to hold the WorkSheet Name if data retrieved from an Excel file
        /// </summary>
        [Summary]
        [Description("Worksheet name if spreadsheet")]
        public string ExcelWorkSheetName { get; set; }

        /// <summary>
        /// Name of column holding crop name data
        /// </summary>
        [Summary]
        [System.ComponentModel.DefaultValueAttribute("CropName")]
        [Description("Column name for crop name")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Crop name column name must be supplied")]
        public string CropNameColumnName { get; set; }

        /// <summary>
        /// Name of column holding soil type data
        /// </summary>
        [Summary]
        [System.ComponentModel.DefaultValueAttribute("SoilNum")]
        [Description("Column name for land id")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Land id column name must be supplied")]
        public string SoilTypeColumnName { get; set; }

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
        /// Name of column holding amount data
        /// </summary>
        [Summary]
        [System.ComponentModel.DefaultValueAttribute("AmtKg")]
        [Description("Column name for amount")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Amount column name must be supplied")]
        public string AmountColumnName { get; set; }

        /// <summary>
        /// Name of column holding nitrogen data
        /// </summary>
        [Summary]
        [Description("Column name for percent nitrogen")]
        public string PercentNitrogenColumnName { get; set; }

        /// <summary>
        /// Name of column holding harvest details
        /// </summary>
        [Summary]
        [Description("Column name for harvest type")]
        public string HarvestTypeColumnName { get; set; }

        /// <summary>
        /// Gets or sets the full file name (with path). 
        /// The Commands.ChangeProperty() uses this property to change the model.
        /// This is done after the user changes the file using the browse button in the View.
        /// </summary>
        [JsonIgnore]
        public string FullFileName
        {
            get
            {
                if ((this.FileName == null) || (this.FileName == ""))
                {
                    return "";
                }
                else
                {
                    Simulation simulation = FindAncestor<Simulation>();
                    if (simulation != null)
                    {
                        return PathUtilities.GetAbsolutePath(this.FileName, simulation.FileName);
                    }
                    else
                    {
                        return this.FileName;
                    }
                }
            }
        }

        /// <summary>
        /// Does file exist
        /// </summary>
        public bool FileExists
        {
            get { return File.Exists(this.FullFileName); }
        }

        /// <summary>
        /// Overrides the base class method to allow for initialization.
        /// </summary>
        [EventSubscribe("CLEMInitialiseResource")]
        private void OnCLEMInitialiseResource(object sender, EventArgs e)
        {
            if (!this.FileExists)
            {
                string errorMsg = String.Format("@error:Could not locate file [o={0}] for [x={1}]", FullFileName.Replace("\\", "\\&shy;"), this.Name);
                throw new ApsimXException(this, errorMsg);
            }
            this.soilNumIndex = 0;
            this.cropNameIndex = 0;
            this.yearIndex = 0;
            this.monthIndex = 0;
            this.amountKgIndex = 0;
            this.nitrogenPercentIndex = 0;
            this.harvestTypeIndex = 0;
            this.forageFileAsTable = GetAllData();
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
        [JsonIgnore]
        public string ErrorMessage = string.Empty;

        /// <summary>
        /// Constructor
        /// </summary>
        public FileCrop()
        {
            base.SetDefaults();
            base.ModelSummaryStyle = HTMLSummaryStyle.FileReader;
        }

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
                List<string> cropProps = new List<string>
                {
                    SoilTypeColumnName,
                    CropNameColumnName,
                    YearColumnName,
                    MonthColumnName,
                    AmountColumnName
                };
                //Npct column is optional 
                //Only try to read it in if it exists in the file.
                if (nitrogenPercentIndex != -1)
                {
                    cropProps.Add(PercentNitrogenColumnName);
                }
                if (harvestTypeIndex != -1)
                {
                    cropProps.Add(HarvestTypeColumnName);
                }
                DataTable table = this.reader.ToTable(cropProps);

                DataColumn[] primarykeys = new DataColumn[5];
                primarykeys[0] = table.Columns[SoilTypeColumnName];
                primarykeys[1] = table.Columns[CropNameColumnName];
                primarykeys[2] = table.Columns[YearColumnName];
                primarykeys[3] = table.Columns[MonthColumnName];

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
        /// Searches the DataTable created from the Forage File using the specified parameters.
        /// <returns></returns>
        /// </summary>
        /// <param name="landId"></param>
        /// <param name="cropName"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns>A struct called CropDataType containing the crop data for this month.
        /// This struct can be null. 
        /// </returns>
        public List<CropDataType> GetCropDataForEntireRun(string landId, string cropName,
                                        DateTime startDate, DateTime endDate)
        {
            int startYear = startDate.Year;
            int startMonth = startDate.Month;
            int endYear = endDate.Year;
            int endMonth = endDate.Month;

            //http://www.csharp-examples.net/dataview-rowfilter/

            string soiltypeparenth = (forageFileAsTable.Columns[SoilTypeColumnName].DataType == typeof(System.String)) ? "'" : "";
            string cropnameparenth = (forageFileAsTable.Columns[CropNameColumnName].DataType == typeof(System.String)) ? "'" : "";

            // check that entry is in correct type
            if(forageFileAsTable.Columns[SoilTypeColumnName].DataType == typeof(System.Single))
            {
                if(!System.Single.TryParse(landId, out Single val))
                {
                    throw new ApsimXException(this, $"[o={this.Parent.Name}.{this.Name}] encountered a problem reading data\r\nCause: The value [{landId}] specified for column [{SoilTypeColumnName}] is not a [Single] type as expected by the data provided.\r\nFix: Ensure the Land Id [{landId}] assigned to the Land type used is present in column [{SoilTypeColumnName}] of the production data provided.");
                }
            }
            if (forageFileAsTable.Columns[CropNameColumnName].DataType == typeof(System.Single))
            {
                if (!System.Single.TryParse(cropName, out Single val))
                {
                    throw new ApsimXException(this, $"[o={this.Parent.Name}.{this.Name}] encountered a problem reading data\r\nCause: The value [{cropName}] specified for column [{CropNameColumnName}] is not a [Single] type as expected by the data provided.\r\nFix: Ensure the Crop name [{cropName}] required is present in column [{CropNameColumnName}] of the production data provided.");
                }
            }

            string filter = $"({SoilTypeColumnName} = {soiltypeparenth}{landId}{soiltypeparenth}) AND ({CropNameColumnName} = {cropnameparenth}{cropName}{cropnameparenth})"
                + " AND ("
                + $"( {YearColumnName} = " + startYear + $" AND {MonthColumnName} >= " + startMonth + ")"
                + $" OR  ( {YearColumnName} > " + startYear + $" AND {YearColumnName} < " + endYear + ")"
                + $" OR  ( {YearColumnName} = " + endYear + $" AND {MonthColumnName} <= " + endMonth + ")"
                + ")";

            DataRow[] foundRows = this.forageFileAsTable.Select(filter);

            List<CropDataType> filtered = new List<CropDataType>();

            foreach (DataRow dr in foundRows)
            {
                filtered.Add(DataRow2CropData(dr));
            }

            filtered.Sort((r, s) => DateTime.Compare(r.HarvestDate, s.HarvestDate));

            return filtered;
        }

        private CropDataType DataRow2CropData(DataRow dr)
        {
            CropDataType cropdata = new CropDataType
            {
                SoilNum = dr[SoilTypeColumnName].ToString(),
                CropName = dr[CropNameColumnName].ToString(),
                Year = int.Parse(dr[YearColumnName].ToString()),
                Month = int.Parse(dr[MonthColumnName].ToString()),
                AmtKg = double.Parse(dr[AmountColumnName].ToString(), CultureInfo.InvariantCulture)
            };

            //Npct column is optional 
            //Only try to read it in if it exists in the file.
            if (nitrogenPercentIndex != -1)
            {
                cropdata.Npct = double.Parse(dr[PercentNitrogenColumnName].ToString(), CultureInfo.InvariantCulture);
            }
            else
            {
                cropdata.Npct = double.NaN;
            }

            //HarvestType column is optional 
            //Only try to read it in if it exists in the file.
            if (harvestTypeIndex != -1)
            {
                if ("first:last".Contains(dr[HarvestTypeColumnName].ToString().ToLower()))
                {
                    cropdata.HarvestType = dr[HarvestTypeColumnName].ToString().ToLower();
                }
                else
                {
                    cropdata.HarvestType = "";
                }
            }
            else
            {
                cropdata.HarvestType = "";
            }

            cropdata.HarvestDate = new DateTime(cropdata.Year, cropdata.Month, 1);

            return cropdata;
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

                    if (this.reader.Headings == null)
                    {
                        string fileType = "Text file";
                        string extra = "\r\nExpecting Header row followed by units row in brackets.\r\nHeading1      Heading2      Heading3\r\n( )         ( )        ( )";
                        if(reader.IsCSVFile)
                        {
                            fileType = "Comma delimited text file (csv)";
                        }
                        if (reader.IsExcelFile)
                        {
                            fileType = "Excel file";
                            extra = "";
                        }
                        throw new Exception($"@error:Invalid {fileType} format of datafile [x={this.FullFileName.Replace("\\", "\\&shy;")}]{extra}");
                    }

                    this.soilNumIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, SoilTypeColumnName);
                    this.cropNameIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, CropNameColumnName);
                    this.yearIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, YearColumnName);
                    this.monthIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, MonthColumnName);
                    this.amountKgIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, AmountColumnName);
                    this.nitrogenPercentIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, PercentNitrogenColumnName);
                    this.harvestTypeIndex = StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, HarvestTypeColumnName);

                    if (this.soilNumIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant(SoilTypeColumnName) == null)
                        {
                            throw new Exception($"@error:Cannot find Land Id column [o={SoilTypeColumnName??"Empty"}] in crop file [x={this.FullFileName.Replace("\\", "\\&shy;")}] for [x={this.Name}]");
                        }
                    }

                    if (this.cropNameIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant(CropNameColumnName) == null)
                        {
                            throw new Exception($"@error:Cannot find CropName column [o={CropNameColumnName ?? "Empty"}] in crop file [x=" + this.FullFileName.Replace("\\", "\\&shy;") + "]" + $" for [x={this.Name}]");
                        }
                    }

                    if (this.yearIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant(YearColumnName) == null)
                        {
                            throw new Exception($"@error:Cannot find Year column [o={YearColumnName ?? "Empty"}] in crop file [x=" + this.FullFileName.Replace("\\", "\\&shy;") + "]" + $" for [x={this.Name}]");
                        }
                    }

                    if (this.monthIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant(MonthColumnName) == null)
                        {
                            throw new Exception($"@error:Cannot find Month column [o={MonthColumnName ?? "Empty"}] in crop file [x=" + this.FullFileName.Replace("\\", "\\&shy;") + "]" + $" for [x={this.Name}]");
                        }
                    }

                    if (this.amountKgIndex == -1)
                    {
                        if (this.reader == null || this.reader.Constant(AmountColumnName) == null)
                        {
                            throw new Exception($"@error:Cannot find Amount column [o={AmountColumnName}] in crop file [x=" + this.FullFileName.Replace("\\", "\\&shy;") + "]" + $" for [x={this.Name}]");
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

        #region descriptive summary
        /// <summary>
        /// Provides the description of the model settings for summary (GetFullSummary)
        /// </summary>
        /// <param name="formatForParentControl">Use full verbose description</param>
        /// <returns></returns>
        public override string ModelSummary(bool formatForParentControl)
        {
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"activityentry\">");
                if (FileName == null || FileName == "")
                {
                    htmlWriter.Write("Using <span class=\"errorlink\">FILE NOT SET</span>");
                }
                else if (!this.FileExists)
                {
                    htmlWriter.Write("The file <span class=\"errorlink\">" + FullFileName + "</span> could not be found");
                }
                else
                {
                    htmlWriter.Write("Using <span class=\"filelink\">" + FileName + "</span>");
                }

                if (FileName != null && FileName.Contains(".xls"))
                {
                    if (ExcelWorkSheetName == null || ExcelWorkSheetName == "")
                    {
                        htmlWriter.Write(" with <span class=\"errorlink\">WORKSHEET NOT SET</span>");
                    }
                    else
                    {
                        htmlWriter.Write(" with worksheet <span class=\"filelink\">" + ExcelWorkSheetName + "</span>");
                    }
                }
                htmlWriter.Write("\r\n</div>");

                htmlWriter.Write("\r\n<div class=\"activityentry\">");
                htmlWriter.Write("\r\n<div class=\"activityentry\" style=\"Margin-left:15px;\">");
                htmlWriter.Write("\r\n<div class=\"activityentry\">Column name for <span class=\"filelink\">Land id</span> is ");
                if (SoilTypeColumnName is null || SoilTypeColumnName == "")
                {
                    htmlWriter.Write("<span class=\"errorlink\">NOT SET</span></div>");
                }
                else
                {
                    htmlWriter.Write("<span class=\"setvalue\">" + SoilTypeColumnName + "</span></div>");
                }
                htmlWriter.Write("\r\n<div class=\"activityentry\">Column name for <span class=\"filelink\">Crop name</span> is ");
                if (CropNameColumnName is null || CropNameColumnName == "")
                {
                    htmlWriter.Write("<span class=\"errorlink\">NOT SET</span></div>");
                }
                else
                {
                    htmlWriter.Write("<span class=\"setvalue\">" + CropNameColumnName + "</span></div>");
                }
                htmlWriter.Write("\r\n<div class=\"activityentry\">Column name for <span class=\"filelink\">Year</span> is ");
                if (YearColumnName is null || YearColumnName == "")
                {
                    htmlWriter.Write("<span class=\"errorlink\">NOT SET</span></div>");
                }
                else
                {
                    htmlWriter.Write("<span class=\"setvalue\">" + YearColumnName + "</span></div>");
                }
                htmlWriter.Write("\r\n<div class=\"activityentry\">Column name for <span class=\"filelink\">Month</span> is ");
                if (MonthColumnName is null || MonthColumnName == "")
                {
                    htmlWriter.Write("<span class=\"errorlink\">NOT SET</span></div>");
                }
                else
                {
                    htmlWriter.Write("<span class=\"setvalue\">" + MonthColumnName + "</span></div>");
                }

                htmlWriter.Write("\r\n<div class=\"activityentry\">Column name for <span class=\"filelink\">Amount</span> grown/harvested is ");
                if (AmountColumnName is null || AmountColumnName == "")
                {
                    htmlWriter.Write("<span class=\"errorlink\">NOT SET</span></div>");
                }
                else
                {
                    htmlWriter.Write("<span class=\"setvalue\">" + AmountColumnName + "</span></div>");
                }
                if (PercentNitrogenColumnName is null || PercentNitrogenColumnName == "")
                {
                    htmlWriter.Write("\r\n<div class=\"activityentry\">Column name for <span class=\"filelink\">Nitrogen</span> is <span class=\"setvalue\">NOT NEEDED</span></div>");
                }
                else
                {
                    htmlWriter.Write("\r\n<div class=\"activityentry\">Column name for <span class=\"filelink\">Nitrogen</span> is <span class=\"setvalue\">" + PercentNitrogenColumnName + "</span></div>");
                }
                if (HarvestTypeColumnName is null || HarvestTypeColumnName == "")
                {
                    htmlWriter.Write("\r\n<div class=\"activityentry\">Column name for <span class=\"filelink\">Harvest</span> is <span class=\"setvalue\">NOT NEEDED</span></div>");
                }
                else
                {
                    htmlWriter.Write("\r\n<div class=\"activityentry\">Column name for <span class=\"filelink\">Harvest</span> is <span class=\"setvalue\">" + HarvestTypeColumnName + "</span></div>");
                }

                htmlWriter.Write("\r\n</div>");

                htmlWriter.Write("\r\n</div>");
                return htmlWriter.ToString(); 
            }
        } 
        #endregion

    }

    /// <summary>
    /// A structure containing the commonly used crop input data.
    /// </summary>
    [Serializable]
    public class CropDataType
    {
        /// <summary>
        /// Soil Number
        /// </summary>
        public string SoilNum;

        /// <summary>
        /// Name of Crop
        /// </summary>
        public string CropName;

        /// <summary>
        /// Year (eg. 2017)
        /// </summary>
        public int Year;

        /// <summary>
        /// Month (eg. 1 is Jan, 2 is Feb)
        /// </summary>
        public int Month;

        /// <summary>
        /// Amount in Kg (perHa or perTree) 
        /// </summary>
        public double AmtKg;

        /// <summary>
        /// Nitrogen Percentage of the Amount
        /// </summary>
        public double Npct;

        /// <summary>
        /// Combine Year and Month to create a DateTime. 
        /// Day is set to the 1st of the month.
        /// </summary>
        public DateTime HarvestDate;

        /// <summary>
        /// Harvest type identifier
        /// </summary>
        public string HarvestType;
    }
}
