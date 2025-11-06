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
    /// Reads the contents of a specific sheet from an EXCEL file and stores into the DataStore.
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
            //Clear the tables at the start, since we need to read into them again
            storage.Reader.Refresh();
            foreach (string sheet in SheetNames)
                if (storage.Reader.TableNames.Contains(sheet))
                    storage.Writer.DeleteTable(sheet);
            storage.Writer.WaitForIdle();

            Simulations simulations = Structure.FindParent<Simulations>(recurse: true);
            foreach (string fileName in FileNames)
            {
                string fullFileName = fileName;
                if (!string.IsNullOrEmpty(fullFileName))
                {
                    if (simulations != null && simulations.FileName != null)
                        fullFileName = PathUtilities.GetRelativePath(fileName, simulations.FileName);

                    string absoluteFileName = PathUtilities.GetAbsolutePath(fullFileName.Trim(), storage.FileName);
                    if (!File.Exists(absoluteFileName))
                        throw new Exception($"Error in {Name}: file '{absoluteFileName}' does not exist");

                    List<DataTable> tables = LoadFromExcel(absoluteFileName);
                    foreach (DataTable table in tables)
                    {
                        DataColumn col = table.Columns.Add("_Filename", typeof(string));
                        for (int i = 0; i < table.Rows.Count; i++)
                            table.Rows[i][col] = fullFileName;

                        DataTable fixedTable = ColumnInfo.FixColumnTypes(table);

                        // Don't delete previous data existing in this table. Doing so would
                        // cause problems when merging sheets from multiple excel files.
                        storage.Writer.WriteTable(fixedTable, false);
                        storage.Writer.WaitForIdle();
                    }
                }
            }

            storage.Writer.WaitForIdle();
            storage.Reader.Refresh();

            ColumnData = new List<ColumnInfo>();
            DerivedData = new List<DerivedInfo>();
            SimulationData = new List<SimulationInfo>();
            MergeData = new List<MergeInfo>();
            ZeroData = new List<ZeroInfo>();

            foreach (string sheet in SheetNames)
            {
                DataTable dt = storage.Reader.GetData(sheet).Copy();
                if (dt != null)
                {
                    MergeData.AddRange(MergeInfo.CombineRows(dt, out DataTable combinedDatatable));
                    ColumnData.AddRange(GetAPSIMColumnsFromObserved(combinedDatatable));
                    GetSimulationsFromObserved(combinedDatatable);
                    DerivedData.AddRange(DerivedInfo.AddDerivedColumns(combinedDatatable));
                    ZeroData.AddRange(ZeroInfo.DetectZeros(combinedDatatable));

                    storage.Writer.WriteTable(combinedDatatable, true);
                    storage.Writer.WaitForIdle();
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataTable"></param>
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
        /// 
        /// </summary>
        /// <param name="dataTable"></param>
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
    }
}
