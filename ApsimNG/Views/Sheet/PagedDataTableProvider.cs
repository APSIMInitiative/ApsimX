using Models.Storage;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using APSIM.Shared.Utilities;

namespace UserInterface.Views
{
    /// <summary>
    /// Provides paged access to a table in the DataStore.
    /// </summary>
    public class PagedDataTableProvider : ISheetDataProvider
    {
        /// <summary>The data store.</summary>
        private IStorageReader dataStore;

        /// <summary>The data table.</summary>
        private List<DataPage> dataPages = new List<DataPage>();

        /// <summary>The optional units for each column in the data table. Can be null.</summary>
        private IList<string> units;

        /// <summary>The number of rows to load at a time from the datastore table.</summary>
        private int pageSize;

        /// <summary>The name of the checkpoint.</summary>
        private string checkpointName;
        
        /// <summary>The name of the table.</summary>
        private string tableName;

        /// <summary>The names of the simulations in scope.</summary>
        private IEnumerable<string> simulationNames;

        /// <summary>The names of the columns to read from table.</summary>
        private IEnumerable<string> columnNames;

        /// <summary>The row filter.</summary>
        private string filter;

        /// <summary>Constructor.</summary>
        /// <param name="store">The data store.</param>
        /// <param name="dataTableName">Name of table to read.</param>
        /// <param name="checkpointNameInScope">Name of checkpoint to load.</param>
        /// <param name="simulationNamesInScope">The names of simulations in scope.</param>
        /// <param name="columnNameFilter">Column name filter. Can be null.</param>
        /// <param name="dataFilter">The data filter to apply.</param>
        /// <param name="dataPageSize">The number of rows to load at a time from the datastore table.</param>
        public PagedDataTableProvider(IStorageReader store, 
                                     string checkpointNameInScope,
                                     string dataTableName,
                                     IEnumerable<string> simulationNamesInScope,
                                     string columnNameFilter,
                                     string dataFilter,
                                     int dataPageSize = 100)
        {
            dataStore = store;
            checkpointName = checkpointNameInScope;
            tableName = dataTableName;
            simulationNames = simulationNamesInScope;
            filter = dataFilter;
            pageSize = dataPageSize;
            GetColumnNames(columnNameFilter);
            GetData(0);
            GetUnits();
            GetRowCount();
        }

        /// <summary>Number of heading rows.</summary>
        public int NumHeadingRows { get; set; }

        public int NumPriorityColumns { get; set; }

        /// <summary>Gets the number of columns of data.</summary>
        public int ColumnCount => dataPages[0].ColumnCount;

        /// <summary>Gets the number of rows of data.</summary>
        public int RowCount { get; private set; }

        /// <summary>Get the contents of a cell.</summary>
        /// <param name="colIndex">Column index of cell.</param>
        /// <param name="rowIndex">Row index of cell.</param>
        public string GetCellContents(int colIndex, int rowIndex)
        {
            // Return heading or units if rowIndex = 0 or 1.
            if (rowIndex == 0)
                return dataPages[0].GetColumnName(colIndex);
            else if (units != null && rowIndex == 1)
                return units[colIndex];

            rowIndex -= NumHeadingRows;

            // Load more data if necessary.
            var dataPage = dataPages.Find(d => d.Contains(rowIndex));
            if (dataPage == null)
                dataPage = GetData(rowIndex);

            object value = dataPage.GetCellContents(colIndex, rowIndex);

            if (value is double)
                return ((double)value).ToString("F3");  // 3 decimal places.
            else if (value is DateTime)
                return ((DateTime)value).ToString("yyyy-MM-dd");
            return value.ToString();
        }

        /// <summary>Set the contents of a cell.</summary>
        /// <param name="colIndex">Column index of cell.</param>
        /// <param name="rowIndex">Row index of cell.</param>
        /// <param name="value">The value.</param>
        public void SetCellContents(int colIndex, int rowIndex, string value)
        {
            throw new NotImplementedException();
        }

        /// <summary>Get data to show in grid.</summary>
        private DataPage GetData(int from)
        {
            int startRowIndex = from / pageSize;
            int count = pageSize;

            var newData = dataStore.GetData(tableName,
                                            checkpointName,
                                            simulationNames,
                                            columnNames,
                                            filter,
                                            from,
                                            pageSize);

            // Remove unwanted columns from data table.
            foreach (string columnName in DataTableUtilities.GetColumnNames(newData))
                if (!columnNames.Contains(columnName))
                    newData.Columns.Remove(columnName);

            var newPage = new DataPage(newData, from, pageSize);

            dataPages.Add(newPage);
            return newPage;
        }

        private void GetUnits()
        {
            // Get units for all columns
            units = new List<string>();
            foreach (var columnName in columnNames)
                units.Add(dataStore.Units(tableName, columnName));

            // If all units are empty then null the units list.
            if (!units.Where(unit => !string.IsNullOrEmpty(unit)).Any())
                units = null;
        }

        private void GetRowCount()
        {
            var table = dataStore.GetDataUsingSql($"SELECT COUNT(*) FROM {tableName}");
            RowCount = Convert.ToInt32(table.Rows[0][0]) + 1; // add a row for headings.
            NumHeadingRows = 1;
            if (units != null)
            {
                RowCount = RowCount + 1; // add a row for units.
                NumHeadingRows++;
            }
        }

        private void GetColumnNames(string columnNameFilter)
        {
            // Get a list of column names to read from the datastore.
            var rawColumnNames = dataStore.ColumnNames(tableName).ToList();

            // Strip out unwanted columns.
            var unwantedColumns = new string[] { "CheckpointName", "CheckpointID", "SimulationID" };
            rawColumnNames.RemoveAll(name => unwantedColumns.Contains(name));

            // Determine columns that always appear at start of grid: Date, SimulationName, Zone
            var priorityColumns = rawColumnNames.Where(name => name.Contains("Date") || name.Contains("Today"))
                                                .Concat(new string[] { "SimulationName" })
                                                .Concat(rawColumnNames.Where(name => name == "Zone"));

            NumPriorityColumns = priorityColumns.Count();

            if (!string.IsNullOrEmpty(columnNameFilter))
                columnNames = priorityColumns.Concat(columnNameFilter.Split(',')
                                                                           .Where(x => !string.IsNullOrEmpty(x)));
            else
                columnNames = priorityColumns.Concat(rawColumnNames.Except(priorityColumns));
        }


        class DataPage
        {
            private int start;
            private int count;
            private DataTable data;

            public DataPage(DataTable table, int startRowIndex, int pageSize)
            {
                data = table;
                start = startRowIndex;
                count = pageSize;
            }

            public bool Contains(int rowIndex)
            {
                return rowIndex >= start && rowIndex < start + count;
            }

            public int ColumnCount => data.Columns.Count;

            public string GetColumnName(int columnIndex)
            {
                return data.Columns[columnIndex].ColumnName;
            }

            public object GetCellContents(int columnIndex, int rowIndex)
            {
                return data.Rows[rowIndex - start][columnIndex];
            }

        }
    }
}