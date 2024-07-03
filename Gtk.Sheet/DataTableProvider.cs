using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Gtk.Sheet;
using static Gtk.Sheet.ISheetDataProvider;

namespace Gtk.Sheet
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

        /// <summary>An event invoked when a cell changes.</summary>
        public event CellChangedDelegate CellChanged;

        /// <summary>Constructor.</summary>
        /// <param name="dataSource">A data table.</param>
        /// <param name="isReadOnly">Is the data readonly?</param>
        /// <param name="columnUnits">Optional units for each column of data.</param>
        /// <param name="cellStates">A column order matrix of cell states</param>
        public DataTableProvider(DataTable dataSource,bool isReadOnly, IList<string> columnUnits = null, List<List<bool>> cellStates = null)
        {
            if (dataSource == null)
                Data = new DataTable();
            else
                Data = dataSource;
            units = columnUnits;
            this.cellStates = cellStates;
            IsReadOnly = isReadOnly;
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

        /// <summary>Is the data readonly?</summary>
        public bool IsReadOnly { get; }        

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
            for(int i = colIndices.Length-1; i >= 0; i-- )
            {
                var cellState = GetCellState(colIndices[i], rowIndices[i]);
                if (cellState == SheetDataProviderCellState.Normal || cellState == SheetDataProviderCellState.Calculated)
                {
                    int j = rowIndices[i] - numHeadingRows;
                    if (j >= 0)
                    {
                        while (j >= Data.Rows.Count)
                            Data.Rows.Add(Data.NewRow());

                        var existingValue = Data.Rows[j][colIndices[i]];
                        if (existingValue != null && values[i] == null ||
                            existingValue == null && values[i] != null ||
                            existingValue.ToString() != values[i].ToString())
                        {
                            Data.Rows[j][colIndices[i]] = values[i];                           
                        }
                    }
                }
            }

            // Check each row. If all cells in a row are null except the read-only cells
            // make them mutable temporarily, change the value to null, and make them read-only again.
            // This is required to allow the DataTable to have rows removed correctly.
            if (values.All(v => v == ""))
            {
                foreach (DataRow row in Data.Rows)
                {
                    bool isRowEmptyExceptForReadOnlyCells = false;
                    List<bool> rowEmptyStates = new();    
                    foreach(DataColumn column in Data.Columns)
                    {
                        SheetDataProviderCellState cellState = GetCellState(column.Ordinal, Data.Rows.IndexOf(row));
                        if(cellState != SheetDataProviderCellState.ReadOnly)
                        {
                            var dataValue = Data.Rows[Data.Rows.IndexOf(row)][column.Ordinal];
                            if(dataValue.ToString().Equals(""))
                            {
                                rowEmptyStates.Add(true);
                            }
                            else rowEmptyStates.Add(false);
                        }
                    }

                    if (!rowEmptyStates.Contains(false) && rowEmptyStates.Any() != false)
                        isRowEmptyExceptForReadOnlyCells = true;
                    
                    // Set the read-only cells to mutable temporarily, change value, set back to read-only.
                    if (isRowEmptyExceptForReadOnlyCells)
                    {
                        foreach(DataColumn column in Data.Columns)
                        {
                            SheetDataProviderCellState cellState = GetCellState(column.Ordinal, Data.Rows.IndexOf(row));
                            if(cellState == SheetDataProviderCellState.ReadOnly)
                            {
                                column.ReadOnly = false;
                                Data.Rows[Data.Rows.IndexOf(row)][column.Ordinal] = "";
                                column.ReadOnly = true;
                            }                      
                        }
                    }
                }
            }

            CellChanged?.Invoke(this, colIndices, rowIndices, values);
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
                return null;
            else
                return units[colIndex];
        }
    }
}