namespace Models.PostSimulationTools
{
    using APSIM.Shared.Utilities;
    using ExcelDataReader;
    using Models.Core;
    using Models.Core.Run;
    using Storage;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Globalization;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// # [Name]
    /// Reads the contents of a specific sheet from an EXCEL file and stores into the DataStore. 
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType=typeof(DataStore))]
    public class ExcelInput : Model, IPostSimulationTool, IReferenceExternalFiles
    {
        private string[] filenames;

        /// <summary>
        /// The DataStore.
        /// </summary>
        [Link]
        private IDataStore storage = null;

        /// <summary>
        /// Gets or sets the file name to read from.
        /// </summary>
        [Description("EXCEL file names")]
        [Tooltip("Can contain more than one file name, separated by commas.")]
        [Display(Type=DisplayType.FileNames)]
        public string[] FileNames
        {
            get
            {
                return this.filenames;
            }
            set
            {
                Simulations simulations = Apsim.Parent(this, typeof(Simulations)) as Simulations;
                if (simulations != null && simulations.FileName != null)
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
                    sheetNames = new string[0];
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

        /// <summary>Return our input filenames</summary>
        public IEnumerable<string> GetReferencedFileNames()
        {
            return FileNames;
        }

        /// <summary>
        /// Main run method for performing our calculations and storing data.
        /// </summary>
        public void Run()
        {
            foreach (string sheet in SheetNames)
                if (storage.Reader.TableNames.Contains(sheet))
                    storage.Writer.DeleteTable(sheet);

            foreach (string fileName in FileNames)
            {
                string absoluteFileName = PathUtilities.GetAbsolutePath(fileName.Trim(), storage.FileName);
                if (!File.Exists(absoluteFileName))
                    throw new Exception($"Error in {Name}: file '{absoluteFileName}' does not exist");

                if (Path.GetExtension(absoluteFileName).Equals(".xls", StringComparison.CurrentCultureIgnoreCase))
                    throw new Exception($"EXCEL file '{absoluteFileName}' must be in .xlsx format.");

                // Open the file
                using (FileStream stream = File.Open(absoluteFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
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
                        {
                            if (SheetNames.Any(str => string.Equals(str.Trim(), table.TableName, StringComparison.InvariantCultureIgnoreCase)))
                            {
                                TruncateDates(table);

                                storage.Writer.WriteTable(table);
                                storage.Writer.WaitForIdle();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// If the data table contains DateTime fields, convert them to hold
        /// only the "Date" portion, and not the "Time" within the day.
        /// We do this because in estatablishing PredictedObserved connections,
        /// we commonly use the DateTime fields, but are (currently) only 
        /// interested in the Date.
        /// WARNING: This could potentially cause issues in the future, especially
        /// if we begin to make use of sub-day model steps.
        /// </summary>
        /// <param name="table">Table to be adjusted</param>
        public static void TruncateDates(DataTable table)
        {
            for (int icol = 0; icol < table.Columns.Count; icol++)
                if (table.Columns[icol].DataType == typeof(DateTime))
                {
                    foreach (DataRow row in table.Rows)
                        if (!DBNull.Value.Equals(row[icol]))
                            row[icol] = Convert.ToDateTime(row[icol], CultureInfo.InvariantCulture).Date;
                }
        }
    }
}
