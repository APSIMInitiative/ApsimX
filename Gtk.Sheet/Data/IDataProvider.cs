namespace Gtk.Sheet
{
    public enum SheetCellState
    {
        ReadOnly,
        Calculated,
        Normal
    }

    /// <summary>An interface used by the sheet widget to get and set the contents of a sheet cell.</summary>
    public interface IDataProvider
    {
        /// <summary>Delegate for a CellChanged event.</summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="colIndices">The indices of the columns that were changed.</param>
        /// <param name="rowIndices">The indices of the rows that were changed.</param>
        /// <param name="values">The values of the cells changed.</param>

        delegate void CellChangedDelegate(IDataProvider sender, int[] colIndices, int[] rowIndices, string[] values);

        /// <summary>An event invoked when a cell changes.</summary>
        event CellChangedDelegate CellChanged;

        /// <summary>Gets the number of columns of data.</summary>
        int ColumnCount { get;  }

        /// <summary>Gets the number of rows of data.</summary>
        int RowCount { get; }

        /// <summary>Get the name of a column.</summary>
        /// <param name="colIndex">Column index.</param>
        string GetColumnName(int colIndex);

        /// <summary>Get the units of a column.</summary>
        /// <param name="colIndex">Column index.</param>
        string GetColumnUnits(int colIndex);

        /// <summary>Get the allowable units of a column.</summary>
        /// <param name="colIndex">Column index.</param>
        IReadOnlyList<string> GetColumnValidUnits(int colIndex);

        /// <summary>Get the contents of a cell.</summary>
        /// <param name="colIndex">Column index of cell.</param>
        /// <param name="rowIndex">Row index of cell.</param>
        string GetCellContents(int colIndex, int rowIndex);

        /// <summary>Set the contents of a cell.</summary>
        /// <param name="colIndices">Column index of cell.</param>
        /// <param name="rowIndices">Row index of cell.</param>
        /// <param name="values">The value.</param>
        void SetCellContents(int[] colIndices, int[] rowIndices, string[] values);

        /// <summary>Delete the specified rows.</summary>
        /// <param name="rowIndices">Row indexes of cell.</param>
        void DeleteRows(int[] rowIndices);

        /// <summary>Get the cell state.</summary>
        /// <param name="colIndex">Column index of cell.</param>
        /// <param name="rowIndex">Row index of cell.</param>
        SheetCellState GetCellState(int colIndex, int rowIndex);

        /// <summary>Set the cell state.</summary>
        /// <param name="colIndex">Column index of cell.</param>
        /// <param name="rowIndex">Row index of cell.</param>
        /// <param name="state">The cell state</param>
        void SetCellState(int colIndex, int rowIndex);
    }
}