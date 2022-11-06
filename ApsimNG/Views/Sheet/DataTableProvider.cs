using System;
using System.Collections.Generic;
using System.Data;

namespace UserInterface.Views
{
    /// <summary>
    /// Wraps a .NET DataTable as a data provider for a sheet widget.
    /// </summary>
    public class DataTableProvider : ISheetDataProvider
    {
        /// <summary>The optional units for each column in the data table. Can be null.</summary>
        private readonly IList<string> units;

        /// <summary>Number of heading rows.</summary>
        private int numHeadingRows;


        /// <summary>Delegate for a CellChanged event.</summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="colIndex">The index of the column that was changed.</param>
        /// <param name="rowIndex">The index of the row that was changed.</param>
        public delegate void CellChangedDelegate(ISheetDataProvider sender, int colIndex, int rowIndex);

        /// <summary>An event invoked when a cell changes.</summary>
        public event CellChangedDelegate CellChanged;

        /// <summary>Constructor.</summary>
        /// <param name="dataSource">A data table.</param>
        /// <param name="columnUnits">Optional units for each column of data.</param>
        public DataTableProvider(DataTable dataSource, IList<string> columnUnits = null)
        {
            if (dataSource == null)
                Data = new DataTable();
            else
                Data = dataSource;
            units = columnUnits;
            if (units == null)
                numHeadingRows = 1;
            else
                numHeadingRows = 2;
        }

        /// <summary>The wrapped data table.</summary>
        public DataTable Data { get; }

        /// <summary>Gets the number of columns of data.</summary>
        public int ColumnCount => Data.Columns.Count;

        /// <summary>Gets the number of rows of data.</summary>
        public int RowCount => Data.Rows.Count + numHeadingRows;

        /// <summary>Get the contents of a cell.</summary>
        /// <param name="colIndex">Column index of cell.</param>
        /// <param name="rowIndex">Row index of cell.</param>
        public string GetCellContents(int colIndex, int rowIndex)
        {
            if (colIndex >= Data.Columns.Count || rowIndex - numHeadingRows >= Data.Rows.Count)
                return null;

            if (rowIndex == 0)
                return Data.Columns[colIndex].ColumnName;
            else if (numHeadingRows == 2 && rowIndex == 1)
                return units[colIndex];
            var value = Data.Rows[rowIndex - numHeadingRows][colIndex];
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
            if (!IsColumnReadonly(colIndex))
            {
                int i = rowIndex - numHeadingRows;
                while (i >= Data.Rows.Count)
                    Data.Rows.Add(Data.NewRow());

                var existingValue = Data.Rows[i][colIndex];
                if (existingValue != null && value == null ||
                    existingValue == null && value != null ||
                    existingValue.ToString() != value.ToString())
                {
                    Data.Rows[i][colIndex] = value;
                    CellChanged?.Invoke(this, colIndex, rowIndex);
                }
            }
        }

        /// <summary>Is the column readonly?</summary>
        /// <param name="colIndex">Column index of cell.</param>
        public bool IsColumnReadonly(int colIndex)
        {
            return Data.Columns[colIndex].ReadOnly;
        }
    }
}