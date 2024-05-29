using APSIM.Shared.Utilities;
using Models.CLEM.Activities;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.IO;
using System.Linq;

namespace Models.CLEM
{
    ///<summary>
    /// Reads in external resource input data and makes it available to other models.
    ///</summary>
    ///    
    ///<remarks>
    ///</remarks>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ZoneCLEM))]
    [ValidParent(ParentType = typeof(Market))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("Access to a resource input file")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/DataReaders/ResourceDataReader.htm")]
    public class FileResource : CLEMModel, IValidatableObject
    {
        /// <summary>
        /// The entire Crop File read in as a DataTable with Primary Keys assigned.
        /// </summary>
        private DataTable resourceFileAsTable;

        /// <summary>
        /// A reference to the text file reader object
        /// </summary>
        [NonSerialized]
        private ApsimTextFile reader = null;

        /// <summary>
        /// Gets or sets the file name. Should be relative filename where possible.
        /// </summary>
        [Summary]
        [Description("File name")]
        [Models.Core.Display(Type = DisplayType.FileName)]
        [Required(AllowEmptyStrings = false, ErrorMessage = "File name must be supplied")]
        public string FileName { get; set; }

        /// <summary>
        /// Used to hold the WorkSheet Name if data retrieved from an Excel file
        /// </summary>
        [Summary]
        [Description("Worksheet name if spreadsheet")]
        public string ExcelWorkSheetName { get; set; }

        /// <summary>
        /// Name of column holding year or date data
        /// </summary>
        [Summary]
        [System.ComponentModel.DefaultValueAttribute("Year")]
        [Description("Column name for year")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Year/Date column name must be supplied")]
        public string YearColumnName { get; set; }

        /// <summary>
        /// Name of column holding month data
        /// </summary>
        [Summary]
        [System.ComponentModel.DefaultValueAttribute("Month")]
        [Description("Column name for month")]
        public string MonthColumnName { get; set; }

        /// <summary>
        /// Name of column holding resource name data
        /// </summary>
        [Summary]
        [System.ComponentModel.DefaultValueAttribute("ResourceName")]
        [Description("Column name for resource name")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Resource name column name must be supplied")]
        public string ResourceNameColumnName { get; set; }

        /// <summary>
        /// Name of column holding amount data
        /// </summary>
        [Summary]
        [System.ComponentModel.DefaultValueAttribute("Amount")]
        [Description("Column name for amount")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Amount column name must be supplied")]
        public string AmountColumnName { get; set; }

        /// <summary>
        /// Style of date input to use
        /// </summary>
        [JsonIgnore]
        public DateStyle StyleOfDateEntry { get; set; }

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
                if ((FileName == null) || (FileName == ""))
                    return "";
                else
                {
                    Simulation simulation = FindAncestor<Simulation>();
                    if (simulation != null)
                        return PathUtilities.GetAbsolutePath(FileName, simulation.FileName);
                    else
                        return FileName;
                }
            }
        }

        /// <summary>
        /// Does file exist
        /// </summary>
        public bool FileExists
        {
            get { return File.Exists(FullFileName); }
        }

        /// <summary>
        /// Overrides the base class method to allow for initialization.
        /// </summary>
        [EventSubscribe("CLEMInitialiseResource")]
        private void OnCLEMInitialiseResource(object sender, EventArgs e)
        {
            if (!FileExists)
            {
                string filename = FullFileName.Replace("\\", "\\&shy;");
                if(filename == "")
                    filename = "Not set";
                string errorMsg = String.Format("Could not locate file [o={0}] for [x={1}]", filename, Name);
                throw new ApsimXException(this, errorMsg);
            }
            resourceFileAsTable = GetAllData();
        }

        /// <summary>
        /// Overrides the base class method to allow for clean up
        /// </summary>
        [EventSubscribe("Completed")]
        private void OnSimulationCompleted(object sender, EventArgs e)
        {
            if (reader != null)
            {
                reader.Close();
                reader = null;
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
        public FileResource()
        {
            base.SetDefaults();
            base.ModelSummaryStyle = HTMLSummaryStyle.FileReader;
            StyleOfDateEntry = DateStyle.YearAndMonth;
        }

        /// <summary>
        /// Read data from data file to DataTable
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
            reader = null;

            if (OpenDataFile())
            {
                List<string> cropProps = new List<string>
                {
                    YearColumnName,
                    MonthColumnName,
                    ResourceNameColumnName,
                    AmountColumnName
                };

                DataTable table = reader.ToTable(cropProps);

                DataColumn[] primarykeys = new DataColumn[5];
                primarykeys[1] = table.Columns[ResourceNameColumnName];
                primarykeys[2] = table.Columns[YearColumnName];
                if (StyleOfDateEntry == DateStyle.YearAndMonth)
                    primarykeys[3] = table.Columns[MonthColumnName];

                table.PrimaryKey = primarykeys;
                CloseDataFile();
                return table;
            }
            else
                return null;
        }

        /// <summary>
        /// Collect the 
        /// </summary>
        /// <returns>A list of column names</returns>
        public IEnumerable<string> GetUniqueResourceTypes()
        {
            DataView dataView = new DataView(resourceFileAsTable);
            return dataView.Table.AsEnumerable().Select(a => a.Field<string>(ResourceNameColumnName)).Distinct();
        }

        /// <summary>
        /// Searches the DataTable created from the Resource File using the specified parameters.
        /// <returns></returns>
        /// </summary>
        /// <param name="month">Month to consider</param>
        /// <param name="year">Year to consider</param>
        /// <param name="resourceNames">Ienumerable of strings representing resource columns to include</param>
        /// <param name="includeZeroAmounts">Include entries where amount equals zero</param>
        /// <returns>
        /// A dataview with all data for the month.
        /// </returns>
        public DataView GetCurrentResourceData(int month, int year, IEnumerable<string> resourceNames = null, bool includeZeroAmounts = false)
        {
            string filter = "(";
            switch (StyleOfDateEntry)
            {
                case DateStyle.DateStamp:
                    throw new NotImplementedException();
                case DateStyle.YearAndMonth:
                    filter = $"{filter}({YearColumnName} = {year} AND {MonthColumnName} = {month})";
                    break;
            }
            DataView dataView = new DataView(resourceFileAsTable);
            if(resourceNames.Any())
            {
                filter = $"{filter} AND (";
                foreach (var resourceName in resourceNames)
                {
                    if(!filter.EndsWith("("))
                        filter = $"{filter} OR ";
                    filter = $"{filter} {ResourceNameColumnName} = '{resourceName.Trim()}'";
                }
                filter = $"{filter} )";
            }
            if(!includeZeroAmounts)
            {
                filter = $"{filter} AND ({AmountColumnName} <> 0)";
            }
            filter = $"{filter})";

            dataView.RowFilter = filter;
            dataView.Sort = $" {AmountColumnName} ASC";
            return dataView;
        }

        /// <summary>
        /// Open the data file.
        /// </summary>
        /// <returns>True if the file was successfully opened</returns>
        public bool OpenDataFile()
        {
            if (System.IO.File.Exists(FullFileName))
            {
                if (reader == null)
                {
                    reader = new ApsimTextFile();
                    reader.Open(FullFileName, ExcelWorkSheetName);

                    if (reader.Headings == null)
                    {
                        string fileType = "Text file";
                        string extra = "\r\nExpecting Header row followed by units row in brackets.\r\nHeading1      Heading2      Heading3\r\n( )         ( )        ( )";
                        if (reader.IsCSVFile)
                            fileType = "Comma delimited text file (csv)";

                        if (reader.IsExcelFile)
                        {
                            fileType = "Excel file";
                            extra = "";
                        }
                        throw new Exception($"Invalid {fileType} format of datafile [x={FullFileName.Replace("\\", "\\&shy;")}]{extra}");
                    }

                    if (StringUtilities.IndexOfCaseInsensitive(reader.Headings, ResourceNameColumnName) == -1)
                        if (reader == null || reader.Constant(ResourceNameColumnName) == null)
                            throw new Exception($"Cannot find ResourceName column [o={ResourceNameColumnName ?? "Empty"}] in resource file [x=" + FullFileName.Replace("\\", "\\&shy;") + "]" + $" for [x={Name}]");

                    if (StringUtilities.IndexOfCaseInsensitive(reader.Headings, YearColumnName) == -1)
                        if (reader == null || reader.Constant(YearColumnName) == null)
                            throw new Exception($"Cannot find Year column [o={YearColumnName ?? "Empty"}] in resource file [x=" + FullFileName.Replace("\\", "\\&shy;") + "]" + $" for [x={Name}]");

                    if (StyleOfDateEntry == DateStyle.YearAndMonth)
                        if (StringUtilities.IndexOfCaseInsensitive(reader.Headings, MonthColumnName) == -1)
                            if (reader == null || reader.Constant(MonthColumnName) == null)
                                throw new Exception($"Cannot find Month column [o={MonthColumnName ?? "Empty"}] in resource file [x=" + FullFileName.Replace("\\", "\\&shy;") + "]" + $" for [x={Name}]");

                    if (StringUtilities.IndexOfCaseInsensitive(reader.Headings, AmountColumnName) == -1)
                        if (reader == null || reader.Constant(AmountColumnName) == null)
                            throw new Exception($"Cannot find Amount column [o={AmountColumnName}] in resource file [x=" + FullFileName.Replace("\\", "\\&shy;") + "]" + $" for [x={Name}]");
                }
                else
                {
                    if (reader.IsExcelFile != true)
                        reader.SeekToDate(reader.FirstDate);
                }

                return true;
            }
            else
                return false;
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

        /// <inheritdoc/>
        public override string ModelSummary()
        {
            using StringWriter htmlWriter = new();
            htmlWriter.Write("\r\n<div class=\"activityentry\">");
            if (FileName == null || FileName == "")
            {
                htmlWriter.Write("Using <span class=\"errorlink\">FILE NOT SET</span>");
            }
            else
            {
                if (!FileExists)
                {
                    htmlWriter.Write($"The file <span class=\"errorlink\">{FullFileName}</span> could not be found");
                }
                else
                {
                    htmlWriter.Write($"Using <span class=\"filelink\">{FileName}</span>");
                }
            }
            if (FileName != null && FileName.Contains(".xls"))
            {
                if (ExcelWorkSheetName == null || ExcelWorkSheetName == "")
                {
                    htmlWriter.Write(" with <span class=\"errorlink\">WORKSHEET NOT SET</span>");
                }
                else
                {
                    htmlWriter.Write($" with worksheet <span class=\"filelink\">{ExcelWorkSheetName}</span>");
                }
            }
            htmlWriter.Write("</div>");
            htmlWriter.Write("\r\n<div class=\"activityentry\" style=\"Margin-left:15px;\">");
            htmlWriter.Write("\r\n<div class=\"activityentry\">Column name for <span class=\"filelink\">Resource name</span> is ");
            if (ResourceNameColumnName is null || ResourceNameColumnName == "")
            {
                htmlWriter.Write("<span class=\"errorlink\">NOT SET</span></div>");
            }
            else
            {
                htmlWriter.Write($"<span class=\"setvalue\">{ResourceNameColumnName}</span></div>");
            }
            string yearLabel = (StyleOfDateEntry == DateStyle.DateStamp) ? "Date" : "Year";
            htmlWriter.Write($"\r\n<div class=\"activityentry\">Column name for <span class=\"filelink\">{yearLabel}</span> is ");
            if (YearColumnName is null || YearColumnName == "")
            {
                htmlWriter.Write("<span class=\"errorlink\">NOT SET</span></div>");
            }
            else
            {
                htmlWriter.Write($"<span class=\"setvalue\">{YearColumnName}</span></div>");
            }

            if (StyleOfDateEntry == DateStyle.YearAndMonth)
            {
                htmlWriter.Write("\r\n<div class=\"activityentry\">Column name for <span class=\"filelink\">Month</span> is ");
                if (MonthColumnName is null || MonthColumnName == "")
                {
                    htmlWriter.Write("<span class=\"errorlink\">NOT SET</span></div>");
                }
                else
                {
                    htmlWriter.Write($"<span class=\"setvalue\">{MonthColumnName}</span></div>");
                }
            }

            htmlWriter.Write("\r\n<div class=\"activityentry\">Column name for <span class=\"filelink\">Amount</span> is ");
            if (AmountColumnName is null || AmountColumnName == "")
            {
                htmlWriter.Write("<span class=\"errorlink\">NOT SET</span></div>");
            }
            else
            {
                htmlWriter.Write($"<span class=\"setvalue\">{AmountColumnName}</span></div>");
            }

            htmlWriter.Write("\r\n</div>");
            return htmlWriter.ToString();
        }

        #endregion

        #region validation

        /// <inheritdoc/>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (FileName.ToLower().EndsWith("xlsx") && (ExcelWorkSheetName == null || ExcelWorkSheetName == ""))
            {
                yield return new ValidationResult("You must specify a worksheet name containing the data when reading an Excel spreadsheet", new string[] { "WorksheetName" });
            }
            if(StyleOfDateEntry == DateStyle.YearAndMonth && (MonthColumnName is null || MonthColumnName == ""))
            {
                yield return new ValidationResult("You must specify a column for month data when using YearAndMonth style date entry", new string[] { "MonthColumnName" });
            }
        } 
        #endregion
    }

}
