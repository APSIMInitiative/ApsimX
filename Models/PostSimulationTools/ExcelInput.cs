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
        private string _filename;

        /// <summary>
        /// The DataStore.
        /// </summary>
        [Link]
        private IDataStore storage = null;

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

        /// <summary>Gets the absolute file name.</summary>
        private string AbsoluteFileName
        {
            get
            {
                //var storage = Apsim.Find(this, typeof(IDataStore)) as IDataStore;
                return PathUtilities.GetAbsolutePath(this.FileName, storage.FileName);
            }
        }

        /// <summary>
        /// Main run method for performing our calculations and storing data.
        /// </summary>
        public void Run()
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
                        storage.Writer.WriteTable(table);
                    }
                }

                // Close the reader and free resources.
                excelReader.Close();
            }
            else
            {
                throw new ApsimXException(this, string.Format("Unable to read Excel file '{0}': file does not exist.", fullFileName));
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
