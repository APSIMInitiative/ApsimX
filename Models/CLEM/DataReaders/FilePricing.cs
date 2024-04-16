using APSIM.Shared.Utilities;
using Models.CLEM.Activities;
using Models.CLEM.Interfaces;
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
    /// Reads in external pricing input data and adjusts resource pricing as specified through simulation.
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
    [Description("Access to a pricing input file to manage prices")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/DataReaders/PriceDataReader.htm")]
    public class FilePricing : CLEMModel, IValidatableObject
    {
        [Link]
        private IClock clock = null;

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
        /// Name of column holding date data
        /// </summary>
        [Summary]
        [System.ComponentModel.DefaultValueAttribute("Date")]
        [Description("Column name for date")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Date column name must be supplied")]
        public string DateColumnName { get; set; }

        [NonSerialized]
        private ApsimTextFile reader = null;
        [NonSerialized]
        private DataRowCollection priceFileAsRows;
        [NonSerialized]
        private List<IResourcePricing> pricingComonentsFound;

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
                    return "";
                else
                {
                    Simulation simulation = FindAncestor<Simulation>();
                    if (simulation != null)
                        return PathUtilities.GetAbsolutePath(this.FileName, simulation.FileName);
                    else
                        return this.FileName;
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
                if (filename == "")
                    filename = "Not set";

                string errorMsg = String.Format("Could not locate file [o={0}] for [x={1}]", filename, this.Name);
                throw new ApsimXException(this, errorMsg);
            }

            // get all pricing component names
            // put in a list that provides a link to the object so we can use this to set values
            var resources = FindAllAncestors<Zone>().FirstOrDefault();
            if (resources != null)
                pricingComonentsFound = resources.FindAllDescendants<IResourcePricing>().ToList();

            DataView dataView = new DataView(GetAllData())
            {
                Sort = $"{DateColumnName} ASC"
            };
            priceFileAsRows = dataView.ToTable().Rows;

            UpdatePricingToDate(clock.StartDate.Year, clock.StartDate.Month);
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

        /// <summary>Function to determine naturally wean individuals at start of timestep</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("CLEMStartOfTimeStep")]
        private void OnCLEMStartOfTimeStep(object sender, EventArgs e)
        {
            // update all pricing this timestep
            // use last price provided for timestep
            UpdatePricingToDate(clock.Today.Year, clock.Today.Month);
        }

        private void UpdatePricingToDate(int year, int month)
        {
            DateTime checkDate = new DateTime(year, month, DateTime.DaysInMonth(year, month));
            // work through to start date
            while (priceFileAsRows.Count > 0 && (DateTime.Parse(priceFileAsRows[0][DateColumnName].ToString()) <= checkDate))
            {
                int cnt = 0;
                foreach (var column in priceFileAsRows[0].Table.Columns)
                {
                    if (!column.ToString().Equals(DateColumnName, StringComparison.OrdinalIgnoreCase) && double.TryParse(priceFileAsRows[0][cnt].ToString(), out double res))
                    {
                        // update
                        var components = pricingComonentsFound.Where(a => (a as IModel).Parent.Name == column.ToString());
                        if (components.Count() > 1)
                        {
                            string warn = $"Multiple resource [r=PricingComponents] named [{column}] were found when applying pricing by [a={this.Name}]. \r\n Ensure input price applies to all these components or provide unique component names";
                            Warnings.CheckAndWrite(warn, Summary, this, MessageType.Warning);
                        }
                        foreach (IResourcePricing resourcePricing in components)
                            resourcePricing.SetPrice(res, this);
                    }
                    cnt++;
                }
                // remove row
                priceFileAsRows.RemoveAt(0);
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
        public FilePricing()
        {
            base.SetDefaults();
            base.ModelSummaryStyle = HTMLSummaryStyle.FileReader;
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
                List<string> pricingColumns = new List<string>
                {
                    DateColumnName
                };

                pricingColumns.AddRange(pricingComonentsFound.Select(a => (a as IModel).Name).Distinct());

                // Add all other column names

                DataTable table = this.reader.ToTable(pricingColumns);

                DataColumn[] primarykeys = new DataColumn[5];
                primarykeys[1] = table.Columns[DateColumnName];
                table.PrimaryKey = primarykeys;
                CloseDataFile();
                return table;
            }
            else
                return null;
        }

        /// <summary>
        /// Open the data file.
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
                        if (reader.IsCSVFile)
                            fileType = "Comma delimited text file (csv)";
                        if (reader.IsExcelFile)
                        {
                            fileType = "Excel file";
                            extra = "";
                        }
                        throw new Exception($"Invalid {fileType} format of datafile [x={this.FullFileName.Replace("\\", "\\&shy;")}]{extra}");
                    }

                    if (StringUtilities.IndexOfCaseInsensitive(this.reader.Headings, DateColumnName) == -1)
                        if (this.reader == null || this.reader.Constant(DateColumnName) == null)
                            throw new Exception($"Cannot find Date column [o={DateColumnName ?? "Empty"}] in Pricing file [x=" + this.FullFileName.Replace("\\", "\\&shy;") + "]" + $" for [x={this.Name}]");
                }
                else
                {
                    if (this.reader.IsExcelFile != true)
                        this.reader.SeekToDate(this.reader.FirstDate);
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
            using (StringWriter htmlWriter = new StringWriter())
            {
                htmlWriter.Write("\r\n<div class=\"activityentry\">");
                if (FileName == null || FileName == "")
                    htmlWriter.Write("Using <span class=\"errorlink\">FILE NOT SET</span>");
                else if (!this.FileExists)
                    htmlWriter.Write("The file <span class=\"errorlink\">" + FullFileName + "</span> could not be found");
                else
                    htmlWriter.Write("Using <span class=\"filelink\">" + FileName + "</span>");

                if (FileName != null && FileName.Contains(".xls"))
                    if (ExcelWorkSheetName == null || ExcelWorkSheetName == "")
                        htmlWriter.Write(" with <span class=\"errorlink\">WORKSHEET NOT SET</span>");
                    else
                        htmlWriter.Write(" with worksheet <span class=\"filelink\">" + ExcelWorkSheetName + "</span>");

                htmlWriter.Write("</div>");
                return htmlWriter.ToString();
            }
        }

        #endregion

        #region validation

        /// <summary>
        /// Validate this component
        /// </summary>
        /// <param name="validationContext"></param>
        /// <returns></returns>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (FileName.ToLower().EndsWith("xlsx") && (ExcelWorkSheetName == null || ExcelWorkSheetName == ""))
            {
                string[] memberNames = new string[] { "WorksheetName" };
                results.Add(new ValidationResult("You must specify a worksheet name containing the data when reading an Excel spreadsheet", memberNames));
            }
            return results;
        }
        #endregion
    }
}
