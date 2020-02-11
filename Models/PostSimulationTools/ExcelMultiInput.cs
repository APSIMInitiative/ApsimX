using Models.Core;
using Models.Core.Run;
using Models.Storage;
using System;
using System.Linq;
using System.Text;
using ExcelDataReader;
using System.IO;
using System.Data;
using APSIM.Shared.Utilities;

namespace Models.PostSimulationTools
{
    /// <summary>
    /// Reads data from one or more excel spreadsheets. Concatenates data from sheets of the same name.
    /// </summary>
    [Serializable]
    [Description("Reads data from one or more excel spreadsheets. Concatenates data from sheets of the same name.")]
    [ViewName("UserInterface.Views.GridView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(DataStore))]
    public class ExcelMultiInput : Model, IPostSimulationTool
    {
        [Link]
        private IDataStore storage = null;

        /// <summary>
        /// Names of the excel files to be read.
        /// </summary>
        [Description("Excel file names (.xlsx)")]
        public string[] FileNames { get; set; }

        /// <summary>
        /// Names of the worksheets to be read.
        /// </summary>
        [Description("Names of the worksheets to be read")]
        public string[] SheetNames { get; set; }

        /// <summary>
        /// Reads data from the excel spreadsheets and stores it in the datastore.
        /// </summary>
        public void Run()
        {
            foreach (string sheet in SheetNames)
                if (storage.Reader.TableNames.Contains(sheet))
                    storage.Writer.DeleteTable(sheet);

            foreach (string fileName in FileNames)
            {
                string absoluteFileName = PathUtilities.GetAbsolutePath(fileName, storage.FileName);
                if (!File.Exists(absoluteFileName))
                    continue;

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
                            if (StringUtilities.IndexOfCaseInsensitive(this.SheetNames, table.TableName) != -1)
                            {
                                ExcelInput.TruncateDates(table);

                                // If the DB already contains a table with this name, we want to append.
                                // Note that this is only possible if another excel file with the same
                                // sheet name has recently been read in.
                                if (storage.Reader.TableNames.Contains(table.TableName))
                                    Merge(table, storage.Reader.GetData(table.TableName));

                                storage.Writer.WriteTable(table);
                                storage.Writer.WaitForIdle();
                            }
                        }
                    }
                }
            }
        }

        private void Merge(DataTable table, DataTable existingTable)
        {
            for (int i = 0; i < table.Columns.Count; i++)
            {
                DataColumn newColumn = table.Columns[i];
                DataColumn oldColumn = existingTable.Columns[newColumn.ColumnName];

                if (oldColumn != null && oldColumn.DataType != newColumn.DataType)
                    ChangeColumnType(table, newColumn.ColumnName, oldColumn.DataType);
            }

            table.Merge(existingTable);

            table.Columns.Remove("SimulationID");
            table.Columns.Remove("CheckpointID");
            table.Columns.Remove("CheckpointName");
        }

        private void ChangeColumnType(DataTable table, string columnName, Type dataType)
        {
            DataColumn newColumn = new DataColumn(columnName + "_new", dataType);

            int ord = table.Columns[columnName].Ordinal;
            table.Columns.Add(newColumn);
            newColumn.SetOrdinal(ord);

            foreach (DataRow row in table.Rows)
            {
                object value = row[columnName];
                row[newColumn.ColumnName] = value == DBNull.Value ? DBNull.Value : Convert.ChangeType(value, dataType);
            }

            table.Columns.Remove(columnName);
            newColumn.ColumnName = columnName;
        }
    }
}
