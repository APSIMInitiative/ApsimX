using System;

namespace UserInterface.Views
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
        /// <summary>Gets the number of columns of data.</summary>
        int ColumnCount { get;  }

        /// <summary>Gets the number of rows of data.</summary>
        int RowCount { get; }

        /// <summary>Get the contents of a cell.</summary>
        /// <param name="colIndex">Column index of cell.</param>
        /// <param name="rowIndex">Row index of cell.</param>
        string GetCellContents(int colIndex, int rowIndex);

        /// <summary>Set the contents of a cell.</summary>
        /// <param name="colIndex">Column index of cell.</param>
        /// <param name="rowIndex">Row index of cell.</param>
        /// <param name="value">The value.</param>
        void SetCellContents(int colIndex, int rowIndex, string value);

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