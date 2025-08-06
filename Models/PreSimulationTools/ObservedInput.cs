using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using APSIM.Core;
using APSIM.Shared.Utilities;
using ExcelDataReader;
using Models.Core;
using Models.Core.Run;
using Models.Storage;

namespace Models.PreSimulationTools
{

    /// <summary>
    /// Reads the contents of a specific sheet from an EXCEL file and stores into the DataStore.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.ObservedInputView")]
    [PresenterName("UserInterface.Presenters.ObservedInputPresenter")]
    [ValidParent(ParentType = typeof(DataStore))]
    public class ObservedInput : Model, IPreSimulationTool, IReferenceExternalFiles, IStructureDependency
    {
        /// <summary>Structure instance supplied by APSIM.core.</summary>
        [field: NonSerialized]
        public IStructure Structure { private get; set; }


        /// <summary>
        /// Stores information about a column in an observed table
        /// </summary>
        public class ColumnInfo
        {
            /// <summary></summary>
            public string Name;

            /// <summary></summary>
            public string IsApsimVariable;

            /// <summary></summary>
            public string DataType;

            /// <summary></summary>
            public bool HasErrorColumn;

            /// <summary></summary>
            public string Filename;
        }

        /// <summary>
        /// Stores information about derived values from the input
        /// </summary>
        public class DerivedInfo
        {
            /// <summary></summary>
            public string Name;

            /// <summary></summary>
            public string Function;

            /// <summary></summary>
            public string DataType;

            /// <summary></summary>
            public int Added;

            /// <summary></summary>
            public int Existing;
        }

        /// <summary>The DataStore</summary>
        [Link]
        private IDataStore storage = null;

        private string[] filenames;

        /// <summary>
        /// Gets or sets the file name to read from.
        /// </summary>
        [Description("EXCEL file names")]
        [Tooltip("Can contain more than one file name, separated by commas.")]
        [Display(Type = DisplayType.FileNames)]
        public string[] FileNames
        {
            get
            {
                return this.filenames;
            }
            set
            {
                Simulations simulations = Structure.FindParent<Simulations>(recurse: true);
                if (simulations != null && simulations.FileName != null && value != null)
                    this.filenames = value.Select(v => PathUtilities.GetRelativePath(v, simulations.FileName)).ToArray();
                else
                    this.filenames = value;
            }
        }

        /// <summary>
        /// List of Excel sheet names to read from.
        /// </summary>
        private string[] sheetNames;

        /// <summary>
        /// Gets or sets the list of EXCEL sheet names to read from.
        /// </summary>
        [Description("EXCEL sheet names (csv)")]
        public string[] SheetNames
        {
            get
            {
                return sheetNames;
            }
            set
            {
                if (value == null)
                {
                    sheetNames = new string[0];
                }
                else
                {
                    string[] formattedSheetNames = new string[value.Length];
                    for (int i = 0; i < value.Length; i++)
                    {
                        if (Char.IsNumber(value[i][0]))
                            formattedSheetNames[i] = "\"" + value[i] + "\"";
                        else
                            formattedSheetNames[i] = value[i];
                    }

                    sheetNames = formattedSheetNames;
                }
            }
        }

        /// <summary></summary>
        public List<ColumnInfo> ColumnData {get; set;}

        /// <summary>Returns the ColumnData as a DataTable object</summary>
        [Display]
        public DataTable ColumnTable {
            get
            {
                DataTable newTable = new DataTable();
                newTable.Columns.Add("Name");
                newTable.Columns.Add("APSIM");
                newTable.Columns.Add("Type");
                newTable.Columns.Add("Error Bars");
                newTable.Columns.Add("File");

                if (ColumnData == null)
                    return newTable;

                foreach (ColumnInfo columnInfo in ColumnData)
                {

                    DataRow row = newTable.NewRow();
                    row["Name"] = columnInfo.Name;
                    row["APSIM"] = columnInfo.IsApsimVariable;
                    row["Type"] = columnInfo.DataType;
                    row["Error Bars"] = columnInfo.HasErrorColumn;
                    row["File"] = columnInfo.Filename;

                    newTable.Rows.Add(row);
                }

                for(int i = 0; i < newTable.Columns.Count; i++)
                    newTable.Columns[i].ReadOnly = true;

                DataView dv = newTable.DefaultView;
                dv.Sort = "APSIM desc, Name asc";

                return dv.ToTable();
            }
        }

        /// <summary></summary>
        public List<DerivedInfo> DerivedData {get; set;}

        /// <summary>List of variables that can be calculated from existing columns</summary>
        [Display]
        public DataTable DerivedTable
        {
            get
            {
                DataTable newTable = new DataTable();
                newTable.Columns.Add("Name");
                newTable.Columns.Add("Function");
                newTable.Columns.Add("DataType");
                newTable.Columns.Add("Added");
                newTable.Columns.Add("Existing");

                if (DerivedData == null)
                    return newTable;

                foreach (DerivedInfo info in DerivedData)
                {

                    DataRow row = newTable.NewRow();
                    row["Name"] = info.Name;
                    row["Function"] = info.Function;
                    row["DataType"] = info.DataType;
                    row["Added"] = info.Added;
                    row["Existing"] = info.Existing;

                    newTable.Rows.Add(row);
                }

                for(int i = 0; i < newTable.Columns.Count; i++)
                    newTable.Columns[i].ReadOnly = true;

                DataView dv = newTable.DefaultView;
                dv.Sort = "Name asc";

                return dv.ToTable();
            }
        }

        /// <summary>Get list of column names found in this input data</summary>
        public List<string> ColumnNames { get; set; }

        /// <summary>Return our input filenames</summary>
        public IEnumerable<string> GetReferencedFileNames()
        {
            return FileNames.Select(f => f.Trim());
        }

        /// <summary>Remove all paths from referenced filenames.</summary>
        public void RemovePathsFromReferencedFileNames()
        {
            for (int i = 0; i < FileNames.Length; i++)
                FileNames[i] = Path.GetFileName(FileNames[i]);
        }

        /// <summary>
        /// Main run method for performing our calculations and storing data.
        /// </summary>
        public void Run()
        {
            //Clear the tables at the start, since we need to read into them again
            foreach (string sheet in SheetNames)
                if (storage.Reader.TableNames.Contains(sheet))
                    storage.Writer.DeleteTable(sheet);

            foreach (string fileName in FileNames)
            {
                string absoluteFileName = PathUtilities.GetAbsolutePath(fileName.Trim(), storage.FileName);
                if (!File.Exists(absoluteFileName))
                    throw new Exception($"Error in {Name}: file '{absoluteFileName}' does not exist");

                List<DataTable> tables = LoadFromExcel(absoluteFileName);
                foreach (DataTable table in tables)
                {
                    //DataTable validatedTable = ValidateColumns(table);
                    DataTable validatedTable = table;

                    DataColumn col = table.Columns.Add("_Filename", typeof(string));
                    for (int i = 0; i < table.Rows.Count; i++)
                        table.Rows[i][col] = fileName;

                    // Don't delete previous data existing in this table. Doing so would
                    // cause problems when merging sheets from multiple excel files.
                    storage.Writer.WriteTable(table, false);
                    storage.Writer.WaitForIdle();
                }
            }

            GetAPSIMColumnsFromObserved();
            GetDerivedColumnsFromObserved();

        }

        /// <summary>
        /// </summary>
        public List<DataTable> LoadFromExcel(string filepath)
        {
            if (Path.GetExtension(filepath).Equals(".xls", StringComparison.CurrentCultureIgnoreCase))
                throw new Exception($"EXCEL file '{filepath}' must be in .xlsx format.");

            List<DataTable> tables = new List<DataTable>();

            // Open the file
            using (FileStream stream = File.Open(filepath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                // Reading from a OpenXml Excel file (2007 format; *.xlsx)
                using (IExcelDataReader excelReader = ExcelReaderFactory.CreateOpenXmlReader(stream))
                {
                    // Read all sheets from the EXCEL file as a data set.
                    DataSet dataSet = excelReader.AsDataSet(new ExcelDataSetConfiguration()
                    {
                        UseColumnDataType = true,
                        ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                        {
                            UseHeaderRow = true
                        }
                    });

                    // Write all sheets that are specified in 'SheetNames' to the data store
                    foreach (DataTable table in dataSet.Tables)
                        if (SheetNames.Any(str => string.Equals(str.Trim(), table.TableName, StringComparison.InvariantCultureIgnoreCase)))
                            tables.Add(table);
                }
            }
            return tables;
        }

        /// <summary>From the list of columns read in, get a list of columns that match apsim variables.</summary>
        public void GetDerivedColumnsFromObserved()
        {
            Simulations sims = Structure.FindParent<Simulations>(recurse: true);

            List<string> tableNames = SheetNames.ToList();

            DerivedData = new List<DerivedInfo>();

            for (int i = 0; i < tableNames.Count; i++)
            {
                string tableName = tableNames[i];

                storage.Reader.Refresh();
                DataTable dt = storage.Reader.GetData(tableName);
                dt.TableName = tableName;
                dt.Columns.Remove("SimulationName");
                dt.Columns.Remove("CheckpointName");

                bool noMoreFound = false;

                while(!noMoreFound)
                {
                    int count = 0;
                    //Our current list of derived variables
                    count += DeriveColumn(dt, ".NConc",     ".N", "/", ".Wt") ? 1 : 0;
                    count += DeriveColumn(dt, ".N",     ".NConc", "*", ".Wt") ? 1 : 0;
                    count += DeriveColumn(dt, ".Wt",        ".N", "/", ".NConc") ? 1 : 0;

                    count += DeriveColumn(dt, ".",  ".Live.", "+", ".Dead.") ? 1 : 0;
                    count += DeriveColumn(dt, ".Live.",  ".", "-", ".Dead.") ? 1 : 0;
                    count += DeriveColumn(dt, ".Dead.",  ".", "-", ".Live.") ? 1 : 0;

                    count += DeriveColumn(dt, "Leaf.SpecificAreaCanopy",  "Leaf.LAI", "/", "Leaf.Live.Wt") ? 1 : 0;
                    count += DeriveColumn(dt, "Leaf.LAI",  "Leaf.SpecificAreaCanopy", "*", "Leaf.Live.Wt") ? 1 : 0;
                    count += DeriveColumn(dt, "Leaf.Live.Wt",  "Leaf.LAI", "/", "Leaf.SpecificAreaCanopy") ? 1 : 0;

                    if (count == 0)
                        noMoreFound = true;
                }


                storage.Writer.WriteTable(dt, true);
                storage.Writer.WaitForIdle();
                storage.Writer.Stop();
            }
        }

        /// <summary>From the list of columns read in, get a list of columns that match apsim variables.</summary>
        public void GetAPSIMColumnsFromObserved()
        {
            Simulations sims = Structure.FindParent<Simulations>(recurse: true);

            storage?.Writer.Stop();
            storage?.Reader.Refresh();

            List<string> tableNames = SheetNames.ToList();

            ColumnNames = new List<string>();
            ColumnData = new List<ColumnInfo>();

            for (int i = 0; i < tableNames.Count; i++)
            {
                string tableName = tableNames[i];
                DataTable dt = storage.Reader.GetData(tableName);
                List<string> allColumnNames = dt.GetColumnNames().ToList();

                for (int j = 0; j < dt.Columns.Count; j++)
                {
                    string columnName = dt.Columns[j].ColumnName;
                    string columnNameOriginal = columnName;
                    //remove Error from name
                    if (columnName.EndsWith("Error"))
                        columnName = columnName.Remove(columnName.IndexOf("Error"), 5);

                    //check if it has maths
                    bool hasMaths = false;
                    if (columnName.IndexOfAny(new char[] {'+', '-', '*', '/', '='}) > -1 || columnName.StartsWith("sum"))
                        hasMaths = true;

                    //remove ( ) from name
                    if (!hasMaths && columnName.IndexOf('(') > -1 && columnName.EndsWith(')'))
                    {
                        int start = columnName.IndexOf('(');
                        int end = columnName.LastIndexOf(')');
                        columnName = columnName.Remove(start, end-start+1);
                    }

                    //filter out reserved names
                    bool reservedName = false;
                    if (columnName == "CheckpointName" || columnName == "CheckpointID" || columnName == "SimulationName" || columnName == "SimulationID" || columnName == "_Filename")
                        reservedName = true;

                    if(!ColumnNames.Contains(columnName) && !reservedName)
                    {
                        ColumnNames.Add(columnName);

                        bool nameInAPSIMFormat = this.NameIsAPSIMFormat(columnName);
                        VariableComposite variable = null;
                        bool nameIsAPSIMModel = false;
                        if(nameInAPSIMFormat)
                        {
                            variable = this.NameMatchesAPSIMModel(columnName, sims);
                            if (variable != null) {
                                nameIsAPSIMModel = true;
                            }
                        }

                        //Get a filename for this property
                        string filename = "";
                        for (int k = 0; k < dt.Rows.Count && string.IsNullOrEmpty(filename); k++)
                        {
                            DataRow row = dt.Rows[k];
                            if (!string.IsNullOrEmpty(row[columnNameOriginal].ToString()))
                            {
                                filename = row["_Filename"].ToString();
                            }
                        }

                        ColumnInfo colInfo = new ColumnInfo();
                        colInfo.Filename = filename;
                        colInfo.Name = columnName;

                        colInfo.IsApsimVariable = "No";
                        colInfo.DataType = "";
                        if (nameInAPSIMFormat)
                            colInfo.IsApsimVariable = "Not Found";
                        if (hasMaths)
                            colInfo.IsApsimVariable = "Maths";
                        if (nameIsAPSIMModel && variable != null)
                        {
                            colInfo.IsApsimVariable = "Yes";
                            colInfo.DataType = variable.DataType.Name;
                        }

                        colInfo.HasErrorColumn = false;
                        if (allColumnNames.Contains(columnName + "Error"))
                            colInfo.HasErrorColumn = true;

                        ColumnData.Add(colInfo);
                    }
                }
            }
        }

        /// <summary></summary>
        private bool NameIsAPSIMFormat(string columnName)
        {
            if (columnName.Contains('.'))
                return true;
            else
                return false;
        }

        /// <summary></summary>
        private VariableComposite NameMatchesAPSIMModel(string columnName, Simulations sims)
        {
            string nameWithoutBrackets = columnName;
            //remove any characters between ( and ) as these are often layers of a model
            while (nameWithoutBrackets.Contains('(') && nameWithoutBrackets.Contains(')'))
            {
                int start = nameWithoutBrackets.IndexOf('(');
                int end = nameWithoutBrackets.IndexOf(')');
                nameWithoutBrackets = nameWithoutBrackets.Substring(0, start) + nameWithoutBrackets.Substring(end+1);
            }

            //if name ends in Error, remove Error before checking
            if (nameWithoutBrackets.EndsWith("Error"))
                nameWithoutBrackets = nameWithoutBrackets.Substring(0, nameWithoutBrackets.IndexOf("Error"));

            if (nameWithoutBrackets.Length == 0)
                return null;

            string[] nameParts = nameWithoutBrackets.Split('.');
            IModel firstPart = Structure.FindChild<IModel>(nameParts[0], relativeTo:sims);
            if (firstPart == null)
                return null;

            sims.Links.Resolve(firstPart, true, true, false);
            string fullPath = firstPart.FullPath;
            for (int i = 1; i < nameParts.Length; i++)
                fullPath += "." + nameParts[i];

            try
            {
                VariableComposite variable = Structure.GetObject(fullPath, relativeTo: sims);
                return variable;
            }
            catch
            {
                return null;
            }
        }

        private Type GetTypeOfCell(string value)
        {

            if (DateUtilities.ValidateStringHasYear(value)) //try parsing to date
            {
                string dateString = DateUtilities.ValidateDateString(value);
                if (dateString != null)
                {
                    DateTime date = DateUtilities.GetDate(value);
                    if (DateUtilities.CompareDates("1900/01/01", date) >= 0)
                        return typeof(DateTime);
                }
            }

            //try parsing to double
            bool d = double.TryParse(value, out double num);
            if (d == true)
            {
                double wholeNum = num - Math.Floor(num);
                if (wholeNum == 0) //try parsing to int
                    return typeof(int);
                else
                    return typeof(double);
            }

            bool b = bool.TryParse(value.Trim(), out bool boolean);
            if (b == true)
                return typeof(bool);

            return typeof(string);
        }

        private string GetNumberOfValuesOfEachType(List<Type> types)
        {
            int countString = 0;
            int countInt = 0;
            int countDouble = 0;
            int countDate = 0;
            int countBool = 0;

            for (int i = 0; i < types.Count; i++)
            {
                if (types[i] == typeof(string))
                    countString += 1;
                else if (types[i] == typeof(int))
                    countInt += 1;
                else if (types[i] == typeof(double))
                    countDouble += 1;
                else if (types[i] == typeof(DateTime))
                    countDate += 1;
                else if (types[i] == typeof(bool))
                    countBool += 1;
            }

            string message = "";
            if (countString > 0)
                message += $" Type string read {countString} times.";
            if (countInt > 0)
                message += $" Type int read {countInt} times.";
            if (countDouble > 0)
                message += $" Type double read {countDouble} times.";
            if (countDate > 0)
                message += $" Type DateTime read {countDate} times.";
            if (countBool > 0)
                message += $" Type bool read {countBool} times.";

            return message;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="data"></param>
        /// <param name="derived"></param>
        /// <param name="variable1"></param>
        /// <param name="operation"></param>
        /// <param name="variable2"></param>
        /// <returns>True if a value was derived, false if not</returns>
        private bool DeriveColumn(DataTable data, string derived, string variable1, string operation, string variable2)
        {
            return DeriveColumn(data, derived, operation, new List<string>() {variable1, variable2});
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="data"></param>
        /// <param name="derived"></param>
        /// <param name="operation"></param>
        /// <param name="variables"></param>
        /// <returns>True if a value was derived, false if not</returns>
        private bool DeriveColumn(DataTable data, string derived, string operation, List<string> variables)
        {
            bool valuesDerived = false;
            if (variables.Count == 0)
                return valuesDerived;

            List<string> allColumnNames = data.GetColumnNames().ToList();

            for (int j = 0; j < data.Columns.Count; j++)
            {
                string columnName = data.Columns[j].ColumnName;
                string variable1 = variables[0];

                //exclude error columns
                if (!columnName.EndsWith("Error") && columnName.LastIndexOf(variable1) > -1)
                {
                    //work out the prefix and suffix of the variables to be used
                    string prefix = columnName.Substring(0, columnName.LastIndexOf(variable1));
                    string postfix = columnName.Substring(columnName.LastIndexOf(variable1) + variable1.Length);

                    //check all the variables exist
                    bool foundAllVariables = true;
                    for (int k = 1; k < variables.Count && foundAllVariables; k++)
                        if (!allColumnNames.Contains(prefix + variables[k] + postfix))
                            foundAllVariables = false;

                    if (foundAllVariables)
                    {
                        string nameDerived = prefix + derived + postfix;
                        //create the column if it doesn't exist
                        if (!data.Columns.Contains(nameDerived))
                            data.Columns.Add(nameDerived);

                        //for each row in the datastore, see if we can compute the derived value
                        int added = 0;
                        int existing = 0;
                        for (int k = 0; k < data.Rows.Count; k++)
                        {
                            DataRow row = data.Rows[k];

                            //if it already exists, we do nothing
                            if (!string.IsNullOrEmpty(row[nameDerived].ToString()))
                            {
                                existing += 1;
                            }
                            else
                            {
                                double value = 0;

                                //Check that all our variables have values on this row
                                bool allVariablesHaveValues = true;
                                for (int m = 0; m < variables.Count && allVariablesHaveValues; m++)
                                {
                                    string nameVariable = prefix + variables[m] + postfix;
                                    if (string.IsNullOrEmpty(row[nameVariable].ToString()))
                                        allVariablesHaveValues = false;
                                    else if (m == 0)
                                        value = Convert.ToDouble(row[nameVariable]);
                                }

                                if (allVariablesHaveValues)
                                {
                                    string nameVariable = prefix + variables[0] + postfix;
                                    double? result = Convert.ToDouble(row[nameVariable]);

                                    //start at 1 here since our running value has the first value in it
                                    for (int m = 1; m < variables.Count; m++)
                                    {
                                        if (result != null && !double.IsNaN((double)result))
                                        {
                                            nameVariable = prefix + variables[m] + postfix;
                                            double valueVar = Convert.ToDouble(row[nameVariable]);

                                            if (operation == "+" || operation == "sum")
                                                result = value + valueVar;
                                            else if (operation == "-")
                                                result = value - valueVar;
                                            else if (operation == "*" || operation == "product")
                                                result = value * valueVar;
                                            else if (operation == "/" && valueVar != 0)
                                                result = value / valueVar;
                                            else
                                                result = null;
                                        }
                                    }
                                    if (result != null && !double.IsNaN((double)result))
                                    {
                                        row[nameDerived] = result;
                                        added += 1;
                                    }
                                }
                            }
                        }
                        //if we added some derived variables, list the stats for the user
                        if (added > 0)
                        {
                            valuesDerived = true;
                            string functionString = "";
                            for (int k = 0; k < variables.Count; k++)
                            {
                                if (k != 0)
                                    functionString += " " + operation + " ";
                                functionString += prefix + variables[k] + postfix;
                            }

                            DerivedInfo info = new DerivedInfo();
                            info.Name = nameDerived;
                            info.Function = functionString;
                            info.DataType = "Double";
                            info.Added = added;
                            info.Existing = existing;
                            DerivedData.Add(info);
                        }
                    }
                }
            }

            return valuesDerived;
        }
    }
}
