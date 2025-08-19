using APSIM.Shared.Utilities;
using Models.CLEM.Resources;
using Models.Core;
using Models.Core.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.IO;

namespace Models.CLEM
{
    ///<summary>
    /// Reads in ruminant cohorts from an input file and makes it available to other models.
    ///</summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(RuminantInitialCohorts))]
    [Description("Access to a ruminant cohorts input file")]
    [Version(1, 0, 1, "")]
    [HelpUri(@"Content/Features/DataReaders/ResourceRuminantCohorts.htm")]
    [MinimumTimeStepPermitted(TimeStepTypes.Daily)]
    public class FileRuminantCohorts : CLEMModel, IValidatableObject
    {
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
        /// Name of column cohort name
        /// </summary>
        [Summary]
        [Description("Column name for name of cohort")]
        public string NameColumnName { get; set; }

        /// <summary>
        /// Name of column holding sex
        /// </summary>
        [Summary]
        [Description("Column name for sex of individual")]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Sex column name must be supplied")]
        public string SexColumnName { get; set; } = "Sex";

        /// <summary>
        /// Name of column holding age
        /// </summary>
        [Summary]
        [Description("Column name for age")]
        public string AgeColumnName { get; set; } = "Age";

        /// <summary>
        /// Name of column holding age standard deviation
        /// </summary>
        [Summary]
        [Description("Column name for age standard deviation")]
        public string AgeSDColumnName { get; set; }

        /// <summary>
        /// Name of column holding number of individuals
        /// </summary>
        [Summary]
        [Description("Column name for number of individuals")]
        public string NumberColumnName { get; set; } = "Number";

        /// <summary>
        /// Name of column holding weight
        /// </summary>
        [Summary]
        [Description("Column name for live weight (kg)")]
        public string WeightColumnName { get; set; }

        /// <summary>
        /// Name of column holding weight standard deviation
        /// </summary>
        [Summary]
        [Description("Column name for weight standard deviation")]
        public string WeightSDColumnName { get; set; }

        /// <summary>
        /// Name of column holding initial fat protein allocation style
        /// </summary>
        [Summary]
        [Description("Column name for fat & protein allocation style")]
        public string FatProteinAllocationColumnName { get; set; }

        /// <summary>
        /// Name of column holding initial fat protein values
        /// </summary>
        [Summary]
        [Description("Column name for fat & protien values")]
        public string FatProteinColumnName { get; set; }

        /// <summary>
        /// Name of column holding sire status
        /// </summary>
        [Summary]
        [Description("Column name for sire status")]
        public string SireColumnName { get; set; }

        /// <summary>
        /// Name of column holding suckling status
        /// </summary>
        [Summary]
        [Description("Column name for suckling status")]
        public string SucklingColumnName { get; set; }

        /// <summary>
        /// Name of column holding castration status
        /// </summary>
        [Summary]
        [Description("Column name for castration status")]
        public string CastratedColumnName { get; set; }

        /// <summary>
        /// Name of column holding proportion of fleece present
        /// </summary>
        [Summary]
        [Description("Column name for proportion of fleece")]
        public string ProportionFleeceColumnName { get; set; }

        /// <summary>
        /// Name of column holding number of days pregnant for breeders
        /// </summary>
        [Summary]
        [Description("Column name for number days pregnant")]
        public string DaysPregnantColumnName { get; set; }

        /// <summary>
        /// Name of column holding the location where individuals are to be placed when created
        /// </summary>
        [Summary]
        [Description("Column name for location")]
        public string LocationColumnName { get; set; }

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
        public FileRuminantCohorts()
        {
            base.ModelSummaryStyle = HTMLSummaryStyle.FileReader;
        }

        /// <summary>
        /// Reads the specified file and returns a list of RuminantTypeCohort objects.
        /// </summary>
        /// <returns>List of RuminantTypeCohort objects.</returns>
        public List<RuminantTypeCohort> ReadCohortsFromFile()
        {
            string fileName = FullFileName;
            if (!FileExists)
            {
                if (fileName == "")
                    fileName = "Not set";
                string errorMsg = $"Could not locate file [o={fileName}] for [x={Name}]";
                throw new ApsimXException(this, errorMsg);
            }

            var result = new List<RuminantTypeCohort>();
            DataTable table;
            int rowCount = 1;

            // Determine file type and read into DataTable
            if (fileName.EndsWith(".xls", StringComparison.OrdinalIgnoreCase) ||
                fileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                // Excel file
                if (string.IsNullOrWhiteSpace(ExcelWorkSheetName))
                    throw new ApsimXException(this, $"Worksheet name must be specified for Excel file: {fileName}");

                table = ExcelUtilities.ReadExcelFileData(fileName, ExcelWorkSheetName, true);
                // not using APSIM excel reader as it will convert columns to dates and break the age specifiers.
                //var reader = new ApsimTextFile();
                //reader.Open(fileName, ExcelWorkSheetName);
                //table = reader.ToTable();
            }
            else
            {
                // CSV or text file
                table = ApsimTextFile.ToTable(fileName);
            }

            if (table == null || table.Rows.Count == 0)
            {
                throw new ApsimXException(this, $"No data found in file: {fileName}");
            }

            // Check required columns
            // Number will defaul to 1 if not provided.
            // Weight will use normalised weight for age if not provided.

            // Read each row and create a cohort
            foreach (DataRow row in table.Rows)
            {
                rowCount++;
                var cohort = new RuminantTypeCohort();
                Sex sex = Sex.Male;

                // Name (Optional) - will be assigned "FileCohort" if not provided
                if (!string.IsNullOrWhiteSpace(NameColumnName) && table.Columns.Contains(NameColumnName))
                {
                    if (row[NameColumnName] is not null && (row[NameColumnName]?.ToString()??"") != "")
                    {
                        cohort.Name = row[NameColumnName]?.ToString() ?? "";
                    }
                    else
                    {
                        cohort.Name = $"FileCohort";
                    }
                }

                // Sex (Required field)
                if (!table.Columns.Contains(SexColumnName))
                    throw new ApsimXException(this, $"Missing required column '{SexColumnName}' in file: {fileName} {ExcelWorkSheetName}");
                if (row[SexColumnName] is string sexStr && Enum.TryParse<Sex>(sexStr, true, out sex))
                    cohort.Sex = sex;
                else if (row[SexColumnName] is Sex sexEnum)
                    cohort.Sex = sexEnum;
                else
                    throw new ApsimXException(this, $"Invalid value for '{SexColumnName}' in file: {fileName} at line: {rowCount}");

                // Age (Required field)
                if (!table.Columns.Contains(AgeColumnName))
                    throw new ApsimXException(this, $"Missing required column '{AgeColumnName}' in file: {fileName} {ExcelWorkSheetName}");
                if (!string.IsNullOrWhiteSpace(AgeColumnName) && table.Columns.Contains(AgeColumnName))
                {
                    if (TryParseIntArray(row[AgeColumnName]?.ToString(), out int[] age, true))
                        cohort.AgeDetails.Parts = age;
                    else
                        throw new ApsimXException(this, $"Invalid value for '{AgeColumnName}' in file: {fileName} at line: {rowCount}. Expecting {{y,m,d}}");
                }

                // Age SD (optional)
                if (!string.IsNullOrWhiteSpace(AgeSDColumnName) && table.Columns.Contains(AgeSDColumnName))
                {
                    if (double.TryParse(row[AgeSDColumnName]?.ToString(), out double ageSD))
                        cohort.AgeSD = ageSD;
                }

                // Number (optional) will be assigned 1 by default if not provided.
                if (!string.IsNullOrWhiteSpace(NumberColumnName) && table.Columns.Contains(NumberColumnName))
                {
                    if (int.TryParse(row[NumberColumnName]?.ToString(), out int number))
                        cohort.Number = number;
                    else
                        throw new ApsimXException(this, $"Invalid value for '{NumberColumnName}' in file: {fileName} at line: {rowCount}");
                }
                else
                {
                    cohort.Number = 1;
                }

                // Weight (optional) - if missing CLEM moved to using normalised weight for age.
                if (!string.IsNullOrWhiteSpace(WeightSDColumnName) && table.Columns.Contains(WeightSDColumnName))
                {
                    if (double.TryParse(row[WeightColumnName]?.ToString(), out double weight))
                        cohort.Weight = weight;
                    else
                        throw new ApsimXException(this, $"Invalid value for '{WeightColumnName}' in file: {fileName} at line: {rowCount}");
                }

                // Weight SD (optional)
                if (!string.IsNullOrWhiteSpace(WeightSDColumnName) && table.Columns.Contains(WeightSDColumnName))
                {
                    if (double.TryParse(row[WeightSDColumnName]?.ToString(), out double weightsd))
                        cohort.WeightSD = weightsd;
                    else
                        throw new ApsimXException(this, $"Invalid value for '{WeightSDColumnName}' in file: {fileName} at line: {rowCount}. Expecting array of fat and protein (see user guide)");
                }

                // FatProteinAllocation (optional) - assumes estimated from relative condition if missing as this is best case for activities requiring fat and protein and usual style for initial herd
                if (!string.IsNullOrWhiteSpace(FatProteinAllocationColumnName) && table.Columns.Contains(FatProteinAllocationColumnName))
                {
                    if (Enum.TryParse<InitialiseFatProteinAssignmentStyle>(row[FatProteinAllocationColumnName]?.ToString(), true, out var fpassignment))
                    cohort.InitialFatProteinStyle = fpassignment;
                }
                else
                {
                    cohort.InitialFatProteinStyle = InitialiseFatProteinAssignmentStyle.EstimateFromRelativeCondition;
                }

                // FatProtein (optional) - will be needed if other allocation styles set
                if (!string.IsNullOrWhiteSpace(FatProteinColumnName) && table.Columns.Contains(FatProteinColumnName))
                {
                    double[] fatProtein = null;
                    if (TryParseDoubleArray(row[FatProteinColumnName]?.ToString(), out double[] fatprotein))
                        cohort.InitialFatProteinValues = fatProtein;
                    else
                        throw new ApsimXException(this, $"Invalid value for '{FatProteinColumnName}' in file: {fileName} at line: {rowCount}. Expecting array of fat and protein (see user guide)");
                }

                // Sire (optional)
                if (!string.IsNullOrWhiteSpace(SireColumnName) && table.Columns.Contains(SireColumnName))
                {
                    if (Boolean.TryParse(row[SireColumnName]?.ToString(), out var sireStatus))
                        cohort.Sire = sireStatus;
                    else
                        throw new ApsimXException(this, $"Invalid value for '{SireColumnName}' in file: {fileName} at line: {rowCount}");
                }

                // Suckling (optional)
                if (!string.IsNullOrWhiteSpace(SucklingColumnName) && table.Columns.Contains(SucklingColumnName))
                {
                    if (Boolean.TryParse(row[SucklingColumnName]?.ToString(), out var sucklingStatus))
                        cohort.Suckling = sucklingStatus;
                    else
                        throw new ApsimXException(this, $"Invalid value for '{SucklingColumnName}' in file: {fileName} at line: {rowCount}");
                }

                // ProportionFleece (optional) - default is 1 and not considered for breeds with no fleece
                if (!string.IsNullOrWhiteSpace(ProportionFleeceColumnName) && table.Columns.Contains(ProportionFleeceColumnName))
                {
                    if (double.TryParse(row[ProportionFleeceColumnName]?.ToString(), out double fleece))
                        cohort.ProportionFleecePresent = fleece;
                    else
                        throw new ApsimXException(this, $"Invalid value for '{ProportionFleeceColumnName}' in file: {fileName} at line: {rowCount}");
                }
                else
                {
                    cohort.ProportionFleecePresent = 1.0; // Default to 100% if not specified
                }

                // Location (optional)
                if (!string.IsNullOrWhiteSpace(LocationColumnName) && table.Columns.Contains(LocationColumnName))
                {
                    if (row[ProportionFleeceColumnName]?.ToString() != "")
                        cohort.ManagedPastureName = row[LocationColumnName]?.ToString();
                }

                // Castrated (optional)
                if (!string.IsNullOrWhiteSpace(CastratedColumnName) && table.Columns.Contains(CastratedColumnName))
                {
                    if (Boolean.TryParse(row[CastratedColumnName]?.ToString(), out var castratedStatus))
                    {
                        if (castratedStatus)
                        {
                            SetAttributeWithValue castrate = new();
                            castrate.AttributeName = "Castrated";
                            castrate.Category = RuminantAttributeCategoryTypes.Sterilise_Castrate;
                            Core.ApsimFile.Structure.Add(castrate, cohort);
                        }
                    }
                    else
                        throw new ApsimXException(this, $"Invalid value for '{SireColumnName}' in file: {fileName} at line: {rowCount}");
                }

                // Days pregnant (optional) - provides the SetConception component functionality
                if (!string.IsNullOrWhiteSpace(DaysPregnantColumnName) && table.Columns.Contains(DaysPregnantColumnName) && sex == Sex.Female)
                {
                    if (int.TryParse(row[DaysPregnantColumnName]?.ToString(), out int daysPregnant))
                    {
                        if (daysPregnant > 0)
                        {
                            SetPreviousConception prevconcep = new();
                            prevconcep.NumberDaysPregnant = daysPregnant;
                            Core.ApsimFile.Structure.Add(prevconcep, cohort);
                        }
                    }
                    else
                        throw new ApsimXException(this, $"Invalid value for '{DaysPregnantColumnName}' in file: {fileName} at line: {rowCount}");
                }
                result.Add(cohort);
            }
            FileRuminantCohorts.EnsureUniqueCohortNames(result);
            return result;
        }

        // Helper method to parse a string like "1,2,3" into an int[]
        private static bool TryParseIntArray(string input, out int[] result, bool isAgeSpecifier = false)
        {
            result = null;
            if (string.IsNullOrWhiteSpace(input))
                return false;
            var parts = input.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (isAgeSpecifier && parts.Length == 0 || parts.Length > 3)
                return false;
            var temp = new List<int>();
            foreach (var part in parts)
            {
                if (int.TryParse(part.Trim(), out int value))
                    temp.Add(value);
                else
                    return false;
            }
            while (isAgeSpecifier && temp.Count < 3)
                temp.Insert(0, 0);
            result = temp.ToArray();
            return true;
        }

        // Helper method to parse a string like "1.4,2.5,3.6" into an double[]
        private static bool TryParseDoubleArray(string input, out double[] result)
        {
            result = null;
            if (string.IsNullOrWhiteSpace(input))
                return false;
            var parts = input.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var temp = new List<double>();
            foreach (var part in parts)
            {
                if (double.TryParse(part.Trim(), out double value))
                    temp.Add(value);
                else
                    return false;
            }
            result = temp.ToArray();
            return true;
        }

        /// <summary>
        /// Ensures all cohort names are unique by appending a number to duplicates.
        /// </summary>
        /// <param name="cohorts">List of RuminantTypeCohort objects to process.</param>
        public static void EnsureUniqueCohortNames(List<RuminantTypeCohort> cohorts)
        {
            if (cohorts == null)
                return;

            var nameCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var cohort in cohorts)
            {
                if (string.IsNullOrWhiteSpace(cohort.Name))
                    cohort.Name = "FileCohort";

                var baseName = cohort.Name;
                if (!nameCounts.ContainsKey(baseName))
                {
                    nameCounts[baseName] = 1;
                    if (cohort.Name == "FileCohort")
                    {
                        cohort.Name = $"{baseName}_{nameCounts[baseName]}";
                    }
                }
                else
                {
                    nameCounts[baseName]++;
                    cohort.Name = $"{baseName}_{nameCounts[baseName]}";
                }
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
            htmlWriter.Write("\r\n<div class=\"activityentry\">NOT YET IMPLEMENTED</div>");
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
        } 

        #endregion
    }

}
