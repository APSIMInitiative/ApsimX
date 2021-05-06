using System;
using System.Collections.Generic;
using System.Data;

namespace UserInterface.Views
{
    public class DataTableProvider : ISheetDataProvider
    {
        private DataTable data;
        private IList<string> units;
        private const int numHeadingRows = 2; // first row is number of heading rows. Second row is units.

        public DataTableProvider(DataTable dataSource, IList<string> columnUnits)
        {
            if (dataSource == null)
                data = new DataTable();
            else
                data = dataSource;
            units = columnUnits;
        }

        public int ColumnCount => data.Columns.Count;

        public int RowCount => data.Rows.Count + numHeadingRows;  

        public string GetCellContents(int colIndex, int rowIndex)
        {
            if (rowIndex == 0)
                return data.Columns[colIndex].ColumnName;
            else if (rowIndex == 1)
                return units[colIndex];
            var value = data.Rows[rowIndex - numHeadingRows][colIndex];
            if (value is double)
                return ((double)value).ToString("F3");  // 3 decimal places.
            else if (value is DateTime)
                return ((DateTime)value).ToString("yyyy-MM-dd");
            return value.ToString();
        }

        public void SetCellContents(int colIndex, int rowIndex, string value)
        {
            data.Rows[rowIndex - numHeadingRows][colIndex] = value;
        }
    }
}