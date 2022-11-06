using System;

namespace UserInterface.Views
{
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

        /// <summary>Is the column readonly?</summary>
        /// <param name="colIndex">Column index of cell.</param>
        bool IsColumnReadonly(int colIndex);
    }
}