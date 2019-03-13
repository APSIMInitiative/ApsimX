namespace Models.PostSimulationTools
{
    using System;
    using System.Data;
    using System.IO;
    using ExcelDataReader;
    using Models.Core;
    using APSIM.Shared.Utilities;
    using Storage;
    using System.Collections.Generic;
    using Models.Core.Run;
    using System.Threading;

    /// <summary>
    /// # [Name]
    /// Reads the contents of a specific sheet from an EXCEL file and stores into the DataStore. 
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType=typeof(DataStore))]
    public class ExcelInput : Model, IRunnable, IReferenceExternalFiles
    {
        private string _filename;

        /// <summary>
        /// Gets or sets the file name to read from.
        /// </summary>
        [Description("EXCEL file name (must be .xlsx)")]
        [Display(Type=DisplayType.FileName)]
        public string FileName
        {
            get
            {
                return this._filename;
            }

            set
            {
                Simulations simulations = Apsim.Parent(this, typeof(Simulations)) as Simulations;
                if (simulations != null && simulations.FileName != null)
                    this._filename = PathUtilities.GetRelativePath(value, simulations.FileName);
                else
                    this._filename = value;
            }
        }

        /// <summary>Gets or sets the texture metadata.</summary>
        /// <value>The texture metadata.</value>
        public string[] FileNameMetadata { get; set; }

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
            return new string[] { FileName };
        }

        /// <summary>
        /// Gets the parent simulation or null if not found
        /// </summary>
        private IStorageWriter StorageWriter
        {
            get
            {
                var dataStore = Apsim.Parent(this, typeof(IDataStore)) as IDataStore;
                if (dataStore == null)
                    throw new Exception("Cannot find a datastore");
                return dataStore.Writer;
            }
        }

        /// <summary>Gets the absolute file name.</summary>
        private string AbsoluteFileName
        {
            get
            {
                Simulations simulations = Apsim.Parent(this, typeof(Simulations)) as Simulations;
                return PathUtilities.GetAbsolutePath(this.FileName, simulations.FileName);
            }
        }

        /// <summary>
        /// Main run method for performing our calculations and storing data.
        /// </summary>
        /// <param name="cancelToken">The cancel token.</param>
        public void Run(CancellationTokenSource cancelToken)
        {
            string fullFileName = AbsoluteFileName;
            if (fullFileName != null && File.Exists(fullFileName))
            {
                // Open the file
                FileStream stream = File.Open(fullFileName, FileMode.Open, FileAccess.Read, FileShare.Read);

                // Create a reader.
                IExcelDataReader excelReader;
                if (Path.GetExtension(fullFileName).Equals(".xls", StringComparison.CurrentCultureIgnoreCase))
                    throw new Exception("EXCEL file must be in .xlsx format. Filename: " + fullFileName);
                else
                {
                    // Reading from a OpenXml Excel file (2007 format; *.xlsx)
                    excelReader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                }

                // Read all sheets from the EXCEL file as a data set
                // excelReader.IsFirstRowAsColumnNames = true;
                DataSet dataSet = excelReader.AsDataSet(new ExcelDataSetConfiguration()
                {
                    ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                    {
                        UseHeaderRow = true
                    }
                });

                // Write all sheets that are specified in 'SheetNames' to the data store
                foreach (DataTable table in dataSet.Tables)
                {
                    bool keep = StringUtilities.IndexOfCaseInsensitive(this.SheetNames, table.TableName) != -1;
                    if (keep)
                    {
                        TruncateDates(table);
                        StorageWriter.WriteTable(table);
                    }
                }

                // Close the reader and free resources.
                excelReader.Close();
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
        private void TruncateDates(DataTable table)
        {
            for (int icol = 0; icol < table.Columns.Count; icol++)
                if (table.Columns[icol].DataType == typeof(DateTime))
                {
                    foreach (DataRow row in table.Rows)
                        if (!DBNull.Value.Equals(row[icol]))
                            row[icol] = Convert.ToDateTime(row[icol]).Date;
                }
        }

    }
}
