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

        /// <summary>A matrix of cell booleans to indicate if a cell is calculated (rather than measured).</summary>
        private List<List<bool>> cellStates;

        /// <summary>Delegate for a CellChanged event.</summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="colIndices">The indices of the columns that were changed.</param>
        /// <param name="rowIndices">The indices of the rows that were changed.</param>
        /// <param name="values">The values of the cells changed.</param>

        public delegate void CellChangedDelegate(ISheetDataProvider sender, int[] colIndices, int[] rowIndices, string[] values);

        /// <summary>An event invoked when a cell changes.</summary>
        public event CellChangedDelegate CellChanged;

        /// <summary>Constructor.</summary>
        /// <param name="dataSource">A data table.</param>
        /// <param name="columnUnits">Optional units for each column of data.</param>
        /// <param name="cellStates">A column order matrix of cell states</param>
        public DataTableProvider(DataTable dataSource, IList<string> columnUnits = null, List<List<bool>> cellStates = null)
        {
            if (dataSource == null)
                Data = new DataTable();
            else
                Data = dataSource;
            units = columnUnits;
            this.cellStates = cellStates;
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
        /// <param name="colIndices">Column indices.</param>
        /// <param name="rowIndices">Row indices.</param>
        /// <param name="values">The values.</param>
        public void SetCellContents(int[] colIndices, int[] rowIndices, string[] values)
        {
            int cellCount = colIndices.Length * rowIndices.Length;
            for(int i = 0 ; i < cellCount; i++ )
            {
                var cellState = GetCellState(colIndices[i], rowIndices[i]);
                if (cellState == SheetDataProviderCellState.Normal || cellState == SheetDataProviderCellState.Calculated)
                {
                    int j = rowIndices[i] - numHeadingRows;
                    if (j >= 0)
                    {
                        while (i >= Data.Rows.Count)
                            Data.Rows.Add(Data.NewRow());

                        var existingValue = Data.Rows[i][colIndices[i]];
                        if (existingValue != null && values[i] == null ||
                            existingValue == null && values[i] != null ||
                            existingValue.ToString() != values[i].ToString())
                        {
                            Data.Rows[i][colIndices[i]] = values[i];
                            CellChanged?.Invoke(this, colIndices, rowIndices, values);
                        }
                    }
                }
            }
        }

        /// <summary>Is the column readonly?</summary>
        /// <param name="colIndex">Column index of cell.</param>
        /// <param name="rowIndex">Row index of cell.</param>
        public SheetDataProviderCellState GetCellState(int colIndex, int rowIndex)
        {
            if (Data.Columns[colIndex].ReadOnly)
                return SheetDataProviderCellState.ReadOnly;
            else if (cellStates != null && colIndex < cellStates.Count && cellStates[colIndex] != null && rowIndex < cellStates[colIndex].Count)
                return cellStates[colIndex][rowIndex] ? SheetDataProviderCellState.Calculated : SheetDataProviderCellState.Normal;
            else
                return SheetDataProviderCellState.Normal;
        }

        /// <summary>Set the cell state.</summary>
        /// <param name="colIndex">Column index of cell.</param>
        /// <param name="rowIndex">Row index of cell.</param>
        public void SetCellState(int colIndex, int rowIndex)
        {
            throw new NotImplementedException("Cannot set the state of a DataTable");
        }

        /// <summary>Get the Units assigned to this column</summary>
        /// <param name="colIndex">Column index of cell.</param>
        public string GetColumnUnits(int colIndex)
        {
            if (units == null)
                return "";
            else
                return units[colIndex];
        }
    }
}