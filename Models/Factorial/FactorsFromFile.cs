using APSIM.Core;
using APSIM.Shared.Utilities;
using DocumentFormat.OpenXml.Wordprocessing;
using Models.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace Models.Factorial
{
    /// <summary>
    /// This class permutates all child models by each other.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Factors))]
    [ValidParent(ParentType = typeof(Factor))]
    [ValidParent(ParentType = typeof(Permutation))]
    [Description("Generate factors as specified in an Excel spreadsheet")]
    public class FactorsFromFile: Model
    {
        private Dictionary<string, string> PropertiesDictionary = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
        private string createFactorsLog = "No details available";

        /// <summary>
        /// The name of the Excel spreadsheet containing all factors details. One sheet (Properties) contains the label and full APSIM Node path for any properties used. Each other spreadsheet if a factor using the sheet name with the first column (levels) containing the factor levels and each column after that representing the values to set for a property identified by the name of the column matching the property label in the Properties sheet. 
        /// </summary>
        [Description("Factor details spreadsheet")]
        [Core.Display(Type = DisplayType.FileName)]
        [Required(AllowEmptyStrings = false, ErrorMessage = "Factors spreadsheet name must be supplied")]
        public string FileName { get; set; }

        /// <summary>
        /// Switch to enable ignoring any column that does not have a property table in the properties sheet.
        /// </summary>
        [Description("Ignore columns with no property label")]
        public bool IgnoreColumnsMissingLabel { get; set; } = true;

        /// <summary>
        /// Gets or sets the full file name (with path). 
        /// </summary>
        [JsonIgnore]
        public string FullFileName
        {
            get
            {
                if (string.IsNullOrEmpty(FileName))
                    throw new FileNotFoundException($"Factors spreadsheet must be supplied");
                else
                {
                    Simulations simulations = Node.FindParent<Simulations>(recurse: true);
                    if (simulations != null)
                        return PathUtilities.GetAbsolutePath(FileName, simulations.FileName);
                    else
                        return FileName;
                }
            }
        }

        /// <summary>An event handler to allow us to initialise ourselves.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        [EventSubscribe("BeginRun")]
        private void OnBeginRun(object sender, EventArgs e)
        {
            if (Enabled)
                CreateFactorsFromFile();
        }

        /// <summary>
        /// Method to read factors from an excel spreadsheet and populate parent model with factors and composite factor components.
        /// </summary>
        public void CreateFactorsFromFile()
        {
            string absoluteFileName = FullFileName;

            using StringWriter log = new StringWriter();

            if (Path.GetExtension(absoluteFileName).Equals(".xls", StringComparison.CurrentCultureIgnoreCase))
                throw new Exception($"EXCEL file '{absoluteFileName}' must be in .xlsx format.");

            log.WriteLine($"Experiment factors imported from {absoluteFileName}");

            // Read sheet names
            List<string> sheetNames = ExcelUtilities.GetWorkSheetNames(absoluteFileName);

            // Ensure we have a Properties sheet
            string propsSheetName = sheetNames.FirstOrDefault(s => string.Equals(s, "Properties", StringComparison.InvariantCultureIgnoreCase));
            if (propsSheetName == null)
            {
                throw new Exception($"Excel file '{absoluteFileName}' must contain a 'Properties' worksheet where property labels and the associated APSIM Node path are provided");
            }

            // Read Properties sheet (header row expected)
            DataTable propsTable = ExcelUtilities.ReadExcelFileData(absoluteFileName, propsSheetName, headerRow: true);
            if (propsTable == null)
                throw new Exception($"Unable to read '{propsSheetName}' from '{absoluteFileName}'");

            if (propsTable.Columns.Count < 2)
                throw new Exception($"Properties worksheet '{propsSheetName}' must contain at least two columns: label and path");

            // Build properties dictionary from properties sheet table. Use first column as label, second as full path.
            PropertiesDictionary = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            DataColumn labelColumn = propsTable.Columns
                .Cast<DataColumn>()
                .FirstOrDefault(c => string.Equals(c.ColumnName?.Trim(), "label", StringComparison.InvariantCultureIgnoreCase));
            int labelColIndex = labelColumn == null ? -1 : propsTable.Columns.IndexOf(labelColumn);

            DataColumn pathColumn = propsTable.Columns
                .Cast<DataColumn>()
                .FirstOrDefault(c => string.Equals(c.ColumnName?.Trim(), "path", StringComparison.InvariantCultureIgnoreCase));
            int pathColIndex = pathColumn == null ? -1 : propsTable.Columns.IndexOf(pathColumn);

            for (int rowIndex = 0; rowIndex < propsTable.Rows.Count; rowIndex++)
            {
                DataRow row = propsTable.Rows[rowIndex];
                string label = row[labelColumn.ColumnName]?.ToString()?.Trim();
                string path = row[pathColumn.ColumnName]?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(label) && !string.IsNullOrEmpty(path))
                {
                    // If a duplicate label already exists, throw an error instead of silently overwriting.
                    if (PropertiesDictionary.ContainsKey(label))
                    {
                        throw new Exception($"Duplicate property label '{label}' found in worksheet '{propsSheetName}' at row {rowIndex + 1}.");
                    }

                    PropertiesDictionary[label] = path;
                }
            }

            log.WriteLine($"Property paths defined from Properties sheet: {PropertiesDictionary.Keys.Join(",")}");
            log.WriteLine($"Factors created:");

            // Process every other sheet as a Factor
            bool factorFound = false;
            foreach (string sheet in sheetNames.Where(s => !string.Equals(s, propsSheetName, StringComparison.InvariantCultureIgnoreCase)))
            {
                DataTable table = ExcelUtilities.ReadExcelFileData(absoluteFileName, sheet, headerRow: true);
                if (table == null || table.Columns.Count == 0)
                {
                    continue; // skip empty sheets
                }

                // Create Factor with Name = sheet name and replace if already exists in parent children
                var factor = new Factor() { Name = sheet };
                if (Node.FindChildren<Factor>(name: sheet).Any())
                {
                    Node.RemoveChild(Node.FindChild<Factor>(name: sheet));
                }
                Node.AddChild(factor);

                log.WriteLine($"\tFactor: {sheet}");

                List<string> levels = new List<string>();
                List<string> propertyColumns = new List<string>();

                // Find "include" column (case-insensitive, trimmed)
                DataColumn levelColumn = table.Columns
                    .Cast<DataColumn>()
                    .FirstOrDefault(c => string.Equals(c.ColumnName?.Trim(), "level", StringComparison.InvariantCultureIgnoreCase));
                int levelColIndex = levelColumn == null ? -1 : table.Columns.IndexOf(levelColumn);
                string levelColName = levelColumn.ColumnName;

                // Find "include" column (case-insensitive, trimmed)
                DataColumn includeColumn = table.Columns
                    .Cast<DataColumn>()
                    .FirstOrDefault(c => string.Equals(c.ColumnName?.Trim(), "include", StringComparison.InvariantCultureIgnoreCase));
                int includeColIndex = includeColumn == null ? -1 : table.Columns.IndexOf(includeColumn);

                // Inside foreach (DataRow row in table.Rows) — skip row if include column exists and says not to include
                foreach (DataRow row in table.Rows)
                {
                    if (row is null || includeColIndex >= 0 && !IsIncluded(row[includeColIndex]))
                    {
                        continue;
                    }

                    // Create composite factor for this level
                    var composite = new CompositeFactor()
                    {
                        Name = row[levelColName]?.ToString()?.Trim(),
                        Specifications = new List<string>()
                    };

                    if (levels.Contains(composite.Name))
                        throw new Exception($"Duplicate level name '{composite.Name}' found in sheet '{sheet}' of '{absoluteFileName}'. Level names must be unique within a factor.");
                    else
                        levels.Add(composite.Name);

                    // For each column (not 'include' or 'level'), map column header to property path via dictionary and create "path = value" entries for the compositeFactor.Specifications list

                    propertyColumns = new List<string>();

                    for (int c = 0; c < table.Columns.Count; c++)
                    {
                        if (c == includeColIndex || c == levelColIndex)
                            continue;

                        string colHeader = table.Columns[c].ColumnName?.Trim();
                        if (!IgnoreColumnsMissingLabel && string.IsNullOrEmpty(colHeader))
                            throw new Exception($"Missing column header name representing a property in worksheet '{absoluteFileName}', sheet '{propsSheetName}'.");

                        if (propertyColumns.Contains(colHeader))
                            throw new Exception($"Duplicate property column '{colHeader}' provided in sheet '{sheet}' of '{absoluteFileName}'. Properties must only be specified once per factor sheet.");
                        else
                            propertyColumns.Add(colHeader);

                        if (!PropertiesDictionary.TryGetValue(colHeader, out string propertyPath))
                        {
                            if (IgnoreColumnsMissingLabel)
                                continue;
                            throw new Exception($"Cannot find node path in 'properties' sheet for column '{colHeader}' in '{absoluteFileName}', sheet '{propsSheetName}'.");
                        }

                        object cellObj = row[c];
                        if (cellObj == null || cellObj == DBNull.Value)
                            continue;

                        string cellValue = cellObj.ToString().Trim();
                        if (string.IsNullOrEmpty(cellValue))
                            continue;

                        // Append specification "full.path = value"
                        composite.Specifications.Add($"{propertyPath} = {cellValue}");
                    }

                    if (composite.Specifications.Count == 0)
                        throw new Exception($"No properties found for composite factor '{sheet}' level '{row[levelColName]?.ToString()?.Trim()}' in '{absoluteFileName}', sheet '{propsSheetName}'.");

                    // Add composite factor as child of factor
                    factor.Node.AddChild(composite);
                }

                if (levels.Count == 0)
                {
                    log.WriteLine($"\t\tNo factor levels defined!");
                }
                else
                {
                    factorFound = true;
                    log.WriteLine($"\t\tFactor levels: {levels.Join(",")}");
                    if (propertyColumns.Count == 0)
                    {
                        log.WriteLine($"\t\tNo CompositeFactor properties defined!");
                    }
                    else
                    {
                        log.WriteLine($"\t\tProperties set: {propertyColumns.Join(",")}");
                    }
                }
            }
            if (factorFound == false)
                throw new Exception($"No factors found in '{absoluteFileName}'");

            createFactorsLog = log.ToString();

            // I'm not sure how to present this information to the user.
            // summary hasn't been created and we don't want to write for each simulation in experiment, but once at start of simulation, but I couldn't find a suitable event.
            // is there a way to ask if this is the first of a group of simulations?

            // The components are actually added to the simulation tree as this is not a copy of base simulation as is used in the simulations.
            // This is good as from this point onward the factors appear as if the user build them and will be overwritten each time the simulation runs based on the spreadsheet.
            // The latest approach is to create the components as children below this component so they can been inspected by user and it is clear they are associated with the loading of factors from file.
            // This required these to be included in factors identified below an experiment or permutation as they would otherwise be hidden  one level deeper so some recurse:true settings are needed.

            // Is there a way to fire the building of the UI tree after this method so that the new components are visible in the UI immediately?
            // This should not be possible as UI is not the concern of the Model, but the UI has no say over the execution of Experiments.
            // If not, they only become visible upon opening the next instance of APISM, but are still present for the correct execution of the simulations.

            // If we can't display this, all StringWriter code can be deleted.
            // Otherwise it would need a special presenter with the property entry area and a text/markdown display section for the last genrated details information string.
            // Summary.WriteMessage(this, log.ToString(), MessageType.Information);
        }

        // Helper local function to interpret include cell values
        static bool IsIncluded(object cell)
        {
            if (cell == null || cell == DBNull.Value) return false;
            var s = cell.ToString().Trim();
            if (string.IsNullOrEmpty(s)) return false;
            if (bool.TryParse(s, out bool b)) return b;
            if (int.TryParse(s, out int i)) return i != 0;
            switch (s.ToLowerInvariant())
            {
                case "yes":
                case "y":
                case "include":
                    return true;
                default:
                    return false;
            }
        }

    }
}
