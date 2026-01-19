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
using Models.PreSimulationTools.ObservationsInfo;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("UnitTests")]
namespace Models.PreSimulationTools
{
    /// <summary>
    /// The Observations model allows users to import data from excel spreadsheets into tables in the Datastore within APSIM.
    /// 
    /// It can then report back useful information about potential problems within your data that may cause issues:
    /// - Columns: List of columns, if they match apsim variables and if the data type from excel is correct
    /// - Devired Data: List of Data that can be derived from existing data that was read from excel rather than storing pre-calculated values within the sheets.
    /// - Simulations: A list of simulations found in your data compared with the simulations in the apsim file.
    /// - Zeros: A list of all values of 0 found in the data, generally you do not want to compare predicted values against 0
    /// - Merged Data: A list of data that could be merged (same simulation and day) along with how different the two values were
    /// 
    /// In order to see the results, run your simulations or refresh the datastore.
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.ObservationsView")]
    [PresenterName("UserInterface.Presenters.ObservationsPresenter")]
    [ValidParent(ParentType = typeof(DataStore))]
    public class Observations : Model, IPreSimulationTool, IReferenceExternalFiles, IStructureDependency
    {
        /// <summary>Structure instance supplied by APSIM.core.</summary>
        [field: NonSerialized]
        public IStructure Structure { private get; set; }

        /// <summary>The DataStore</summary>
        [Link]
        private IDataStore storage = null;

        /// <summary>
        /// List of Excel sheet names to read from.
        /// </summary>
        internal string[] sheetNames;

        internal string[] filenames;

        internal List<ColumnInfo> ColumnData { get; set; }
        internal List<DerivedInfo> DerivedData { get; set; }
        internal List<SimulationInfo> SimulationData { get; set; }
        internal List<MergeInfo> MergeData { get; set; }
        internal List<ZeroInfo> ZeroData { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public static readonly string[] RESERVED_COLUMNS = { "SimulationID", "SimulationName", "CheckpointID", "CheckpointName", "_Filename" };

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
                this.filenames = value;
            }
        }

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

        /// <summary>Returns the ColumnData as a DataTable object</summary>
        [Display]
        public DataTable ColumnTable
        {
            get { return ColumnInfo.CreateDataTable(ColumnData); }
        }

        /// <summary>List of variables that can be calculated from existing columns</summary>
        [Display]
        public DataTable DerivedTable
        {
            get { return DerivedInfo.CreateDataTable(DerivedData); }
        }

        /// <summary>List of Simulations that are either missing data in the observed, or missing simulations in apsim</summary>
        [Display]
        public DataTable SimulationTable
        {
            get { return SimulationInfo.CreateDataTable(SimulationData); }
        }

        /// <summary>List of merge conflicts encountered</summary>
        [Display]
        public DataTable MergeTable
        {
            get { return MergeInfo.CreateDataTable(MergeData); }
        }

        /// <summary>List of zero values found in the data</summary>
        [Display]
        public DataTable ZeroTable
        {
            get { return ZeroInfo.CreateDataTable(ZeroData); }
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
            if (storage == null)
                storage = Node.FindParent<DataStore>(recurse: true);

            //Clear the tables at the start, since we need to read into them again
            storage.Reader.Refresh();
            foreach (string sheet in SheetNames)
                if (storage.Reader.TableNames.Contains(sheet))
                    storage.Writer.DeleteTable(sheet);
            storage.Writer.WaitForIdle();

            //Get list of file names to open
            List<string> files = new List<string>();
            foreach (string fileName in FileNames)
            {
                if (!string.IsNullOrEmpty(fileName))
                {
                    string relativeFilename = fileName;
                    if (Node != null && Node.FileName != null)
                        relativeFilename = PathUtilities.GetRelativePath(fileName, Node.FileName);
                    string absoluteFileName = PathUtilities.GetAbsolutePath(relativeFilename.Trim(), storage.FileName);
                    if (File.Exists(absoluteFileName))
                        files.Add(absoluteFileName);
                    else
                        throw new Exception($"Error in {Name}: file '{absoluteFileName}' does not exist");
                }
            }

            foreach (string sheet in SheetNames)
            {
                //get a list of all the columns for this sheet
                List<string> columns = new List<string>();
                List<Type> columnsType = new List<Type>();
                foreach (string fileName in files)
                {
                    List<DataTable> tables = LoadFromExcel(fileName);
                    foreach (DataTable table in tables)
                    {
                        if (table.TableName == sheet)
                        {
                            foreach (DataColumn column in table.Columns)
                            {
                                if (!columns.Contains(column.ColumnName))
                                {
                                    columns.Add(column.ColumnName);
                                    if (column.DataType == typeof(DateTime))
                                        columnsType.Add(typeof(DateTime));
                                    else
                                        columnsType.Add(typeof(string));
                                }
                            }
                        }
                    }
                }

                // Create a table with all the required column, each with the type set to string unless it was a datetime
                DataTable data = new DataTable();
                data.TableName = sheet;
                data.Columns.Add("_Filename", typeof(string));
                for (int i = 0; i < columns.Count; i++)
                    data.Columns.Add(columns[i], columnsType[i]);

                //Copy in the data from the files
                foreach (string fileName in files)
                {
                    List<DataTable> tables = LoadFromExcel(fileName);
                    string relativeFilename = fileName;
                    if (Node != null && Node.FileName != null)
                        relativeFilename = PathUtilities.GetRelativePath(fileName, Node.FileName);
                    foreach (DataTable table in tables)
                    {
                        if (table.TableName == sheet)
                        {
                            foreach (DataRow row in table.Rows)
                            {
                                data.ImportRow(row);
                                data.Rows[data.Rows.Count - 1]["_Filename"] = relativeFilename;
                            }
                        }
                    }
                }

                //Set the column types based on the contents
                DataTable fixedTable = ColumnInfo.FixColumnTypes(data);

                //Write to the database
                storage.Writer.WriteTable(fixedTable, true);
            }

            storage.Writer.Stop();
            storage.Reader.Refresh();

            ColumnData = new List<ColumnInfo>();
            DerivedData = new List<DerivedInfo>();
            SimulationData = new List<SimulationInfo>();
            MergeData = new List<MergeInfo>();
            ZeroData = new List<ZeroInfo>();

            foreach (string sheet in SheetNames)
            {
                DataTable dt = storage.Reader.GetData(sheet);
                dt.TableName = sheet;
                if (dt != null)
                {
                    MergeData.AddRange(MergeInfo.CombineRows(dt));
                    ColumnData.AddRange(GetAPSIMColumnsFromObserved(dt));
                    GetSimulationsFromObserved(dt);
                    DerivedData.AddRange(DerivedInfo.AddDerivedColumns(dt));
                    ZeroData.AddRange(ZeroInfo.DetectZeros(dt));

                    storage.Writer.WriteTable(dt, true);
                    storage.Writer.WaitForIdle();

                    DataTable dt2 = storage.Reader.GetData(sheet);
                    dt2.TableName = sheet;
                }
            }
            storage.Writer.Stop();
        }

        /// <summary>
        /// Returns a list of new Column infos from a DataTable that excludes names that are reserved
        /// </summary>
        /// <param name="dataTable">The DataTable to read</param>
        /// <returns>A list of ColumnInfo for the new columns</returns>
        public List<ColumnInfo> GetAPSIMColumnsFromObserved(DataTable dataTable)
        {
            ColumnNames = new List<string>();

            Simulations sims = Structure.FindParent<Simulations>(recurse: true);
            List<string> knownColumnNames = new List<string>();
            knownColumnNames.AddRange(RESERVED_COLUMNS);
            knownColumnNames.AddRange(ColumnNames);

            List<ColumnInfo> infos = ColumnInfo.GetAPSIMColumnsFromObserved(dataTable, sims, knownColumnNames);
            foreach (ColumnInfo info in infos)
            {
                if (!ColumnNames.Contains(info.Name) && !RESERVED_COLUMNS.Contains(info.Name))
                    ColumnNames.Add(info.Name);
            }

            return infos;
        }

        /// <summary>
        /// Fills in the SimulationInfos with data from the given DataTable. Must be here because it needs to handle multiple sheets being read in to prevent duplication.
        /// </summary>
        /// <param name="dataTable">The DataTable to read</param>
        /// <returns>Nothing. But does append the simulation infos to SimulationData</returns>
        public void GetSimulationsFromObserved(DataTable dataTable)
        {
            Simulations sims = Structure.FindParent<Simulations>(recurse: true);
            List<SimulationInfo> infos = SimulationInfo.GetSimulationsFromObserved(dataTable, sims);

            List<string> existingSims = SimulationData.AsEnumerable().Select(s => s.Name).ToList<string>();
            foreach (SimulationInfo info in infos)
            {
                if (existingSims.Contains(info.Name))
                {
                    SimulationInfo existingInfo = SimulationData.FirstOrDefault(s => s.Name == info.Name);
                    if (info.HasData)
                        existingInfo.HasData = true;
                    existingInfo.Rows += info.Rows;
                }
                else
                {
                    SimulationData.Add(info);
                }
            }
        }

        /// <summary>
        /// Function to load a file from Excel, should be refactored later into a utility class. Used by the unit tests.
        /// </summary>
        /// <param name="filepath">File to open</param>
        /// <returns>A list of DataTables from the file where each table is a sheet.</returns>
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
    }
}
