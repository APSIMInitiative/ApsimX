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
using System.Text;
using System.Threading.Tasks;

namespace Models.CLEM
{
    ///<summary>
    /// Reads in external resource input data and makes it available to other models.
    ///</summary>
    ///    
    ///<remarks>
    ///</remarks>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(ZoneCLEM))]
    [ValidParent(ParentType = typeof(Market))]
    [ValidParent(ParentType = typeof(ActivityFolder))]
    [Description("This component specifies a resource input file for the CLEM simulation")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/DataReaders/ResourceDataReader.htm")]
    public class FileResource : CLEMModel, IValidatableObject
    {
        /// <summary>
        /// Gets or sets the file name. Should be relative filename where possible.
        /// </summary>
        [Summary]
        [Description("Resource file name")]
        [Models.Core.Display(Type = DisplayType.FileName)]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Resource file name must be supplied")]
        public string FileName { get; set; }

        /// <summary>
        /// Used to hold the WorkSheet Name if data retrieved from an Excel file
        /// </summary>
        [Summary]
        [Description("Worksheet name if spreadsheet")]
        public string ExcelWorkSheetName { get; set; }

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
        /// A reference to the text file reader object
        /// </summary>
        [NonSerialized]
        private ApsimTextFile reader = null;

        /// <summary>
        /// The entire Crop File read in as a DataTable with Primary Keys assigned.
        /// </summary>
        private DataTable resourceFileAsTable;

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
                string filename = FullFileName.Replace("\\", "\\&shy;");
                if(filename == "")
                {
                    filename = "Not set";
                }
                string errorMsg = String.Format("@error:Could not locate file [o={0}] for [x={1}]", filename, this.Name);
                throw new ApsimXException(this, errorMsg);
            }
            this.resourceFileAsTable = GetAllData();
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
        public FileResource()
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
                    YearColumnName,
                    MonthColumnName,
                    ResourceNameColumnName,
                    AmountColumnName
                };

                DataTable table = this.reader.ToTable(cropProps);

                DataColumn[] primarykeys = new DataColumn[5];
                primarykeys[1] = table.Columns[ResourceNameColumnName];
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
        /// Searches the DataTable created from the Resource File using the specified parameters.
        /// <returns></returns>
        /// </summary>
        /// <param name="month"></param>
        /// <param name="year"></param>
        /// <returns>A struct called CropDataType containing the crop data for this month.
        /// This struct can be null. 
        /// </returns>
        public DataView GetCurrentResourceData(int month, int year)
        {
            string filter = $"( { YearColumnName} = " + year + $" AND { MonthColumnName} = " + month + ")";
            DataView dataView = new DataView(resourceFileAsTable);
            dataView.RowFilter = filter;
            dataView.Sort = $" {AmountColumnName} ASC";
            return dataView;
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
                        string extra = "\nExpecting Header row followed by units row in brackets.\nHeading1      Heading2      Heading3\n( )         ( )        ( )";
                        if (reader.IsCSVFile)
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

                    if (StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, ResourceNameColumnName) == -1)
                    {
                        if (this.reader == null || this.reader.Constant(ResourceNameColumnName) == null)
                        {
                            throw new Exception($"@error:Cannot find ResourceName column [o={ResourceNameColumnName ?? "Empty"}] in crop file [x=" + this.FullFileName.Replace("\\", "\\&shy;") + "]" + $" for [x={this.Name}]");
                        }
                    }

                    if (StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, YearColumnName) == -1)
                    {
                        if (this.reader == null || this.reader.Constant(YearColumnName) == null)
                        {
                            throw new Exception($"@error:Cannot find Year column [o={YearColumnName ?? "Empty"}] in crop file [x=" + this.FullFileName.Replace("\\", "\\&shy;") + "]" + $" for [x={this.Name}]");
                        }
                    }

                    if (StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, MonthColumnName) == -1)
                    {
                        if (this.reader == null || this.reader.Constant(MonthColumnName) == null)
                        {
                            throw new Exception($"@error:Cannot find Month column [o={MonthColumnName ?? "Empty"}] in crop file [x=" + this.FullFileName.Replace("\\", "\\&shy;") + "]" + $" for [x={this.Name}]");
                        }
                    }

                    if (StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, AmountColumnName) == -1)
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
                html += "Using <span class=\"errorlink\">FILE NOT SET</span>";
            }
            else if (!this.FileExists)
            {
                html += "The file <span class=\"errorlink\">" + FullFileName + "</span> could not be found";
            }
            else
            {
                html += "Using <span class=\"filelink\">" + FileName + "</span>";
            }

            if (FileName != null && FileName.Contains(".xls"))
            {
                if (ExcelWorkSheetName == null || ExcelWorkSheetName == "")
                {
                    html += " with <span class=\"errorlink\">WORKSHEET NOT SET</span>";
                }
                else
                {
                    html += " with worksheet <span class=\"filelink\">" + ExcelWorkSheetName + "</span>";
                }
            }

            html += "\n<div class=\"activityentry\">";
            html += "\n<div class=\"activityentry\" style=\"Margin-left:15px;\">";
            html += "\n<div class=\"activityentry\">Column name for <span class=\"filelink\">Resource name</span> is ";
            if (ResourceNameColumnName is null || ResourceNameColumnName == "")
            {
                html += "<span class=\"errorlink\">NOT SET</span></div>";
            }
            else
            {
                html += "<span class=\"setvalue\">" + ResourceNameColumnName + "</span></div>";
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

            html += "\n<div class=\"activityentry\">Column name for <span class=\"filelink\">Amount</span> is ";
            if (AmountColumnName is null || AmountColumnName == "")
            {
                html += "<span class=\"errorlink\">NOT SET</span></div>";
            }
            else
            {
                html += "<span class=\"setvalue\">" + AmountColumnName + "</span></div>";
            }

            html += "\n</div>";
            return html;
        }

        /// <summary>
        /// Validate this component
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if(FileName.ToLower().EndsWith("xlsx") && (ExcelWorkSheetName == null || ExcelWorkSheetName == ""))
            {
                string[] memberNames = new string[] { "WorksheetName" };
                results.Add(new ValidationResult("You must specify a worksheet name containing the data when reading an Excel spreadsheet", memberNames));
            }
            return results;
        }
    }

}
