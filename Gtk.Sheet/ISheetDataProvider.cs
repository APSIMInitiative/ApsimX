using System;

namespace Gtk.Sheet
{

    public enum SheetDataProviderCellState
    {
        ReadOnly,
        Calculated,
        Normal
    }

    /// <summary>An interface used by the sheet widget to get and set the contents of a sheet cell.</summary>
    public interface ISheetDataProvider
    {
        /// <summary>Delegate for a CellChanged event.</summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="colIndices">The indices of the columns that were changed.</param>
        /// <param name="rowIndices">The indices of the rows that were changed.</param>
        /// <param name="values">The values of the cells changed.</param>

        delegate void CellChangedDelegate(ISheetDataProvider sender, int[] colIndices, int[] rowIndices, string[] values);

        /// <summary>An event invoked when a cell changes.</summary>
        event CellChangedDelegate CellChanged;

        /// <summary>Is the data readonly?</summary>
        bool IsReadOnly { get; }

        /// <summary>Gets the number of columns of data.</summary>
        int ColumnCount { get;  }

        /// <summary>Gets the number of rows of data.</summary>
        int RowCount { get; }

        /// <summary>Get the contents of a cell.</summary>
        /// <param name="colIndex">Column index of cell.</param>
        /// <param name="rowIndex">Row index of cell.</param>
        string GetCellContents(int colIndex, int rowIndex);

        /// <summary>Set the contents of a cell.</summary>
        /// <param name="colIndices">Column index of cell.</param>
        /// <param name="rowIndices">Row index of cell.</param>
        /// <param name="values">The value.</param>
        void SetCellContents(int[] colIndices, int[] rowIndices, string[] values);

        /// <summary>Get the cell state.</summary>
        /// <param name="colIndex">Column index of cell.</param>
        /// <param name="rowIndex">Row index of cell.</param>
        SheetDataProviderCellState GetCellState(int colIndex, int rowIndex);

        /// <summary>Set the cell state.</summary>
        /// <param name="colIndex">Column index of cell.</param>
        /// <param name="rowIndex">Row index of cell.</param>
        /// <param name="state">The cell state</param>
        void SetCellState(int colIndex, int rowIndex);

        /// <summary>Get the Units assigned to this column</summary>
        /// <param name="colIndex">Column index of cell.</param>
        public string GetColumnUnits(int colIndex);
    }
}