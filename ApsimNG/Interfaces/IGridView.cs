namespace UserInterface.Interfaces
{
    using EventArguments;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// The interface to a grid view. Clients of this class should set the data source
    /// before calling other methods.
    /// </summary>
    public interface IGridView
    {
        /// <summary>
        /// Invoked when the user wants to copy a range of cells to the clipboard.
        /// </summary>
        event EventHandler<GridCellActionArgs> CopyCells;

        /// <summary>
        /// Invoked when the user wants to paste data into a range of cells.
        /// </summary>
        event EventHandler<GridCellPasteArgs> PasteCells;

        /// <summary>
        /// Invoked when the user wants to delete data from a range of cells.
        /// </summary>
        event EventHandler<GridCellActionArgs> DeleteCells;

        /// <summary>
        /// This event is invoked when the values of 1 or more cells have changed.
        /// </summary>
        event EventHandler<GridCellsChangedArgs> CellsChanged;

        /// <summary>
        /// Invoked when a grid cell header is clicked.
        /// </summary>
        event EventHandler<GridColumnClickedArgs> GridColumnClicked;

        /// <summary>
        /// Occurs when user clicks a button on the cell.
        /// </summary>
        event EventHandler<GridCellChangedArgs> ButtonClick;

        /// <summary>
        /// Invoked when the user needs context items for the intellisense.
        /// </summary>
        event EventHandler<NeedContextItemsArgs> ContextItemsNeeded;

        /// <summary>
        /// Gets or sets the data to use to populate the grid.
        /// </summary>
        System.Data.DataTable DataSource { get; set; }

        /// <summary>
        /// Gets or sets the number of rows in grid.
        /// Setting this when <see cref="CanGrow"/> is false will generate an exception.
        /// </summary>
        int RowCount { get; set; }

        /// <summary>
        /// Gets the number of columns in the grid.
        /// </summary>
        int ColumnCount { get; }
        
        /// <summary>
        /// Gets or sets the numeric grid format e.g. N3
        /// </summary>
        string NumericFormat { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the grid is read only
        /// </summary>
        bool ReadOnly { get; set; }

        /// <summary>
        /// If true, the grid can grow larger.
        /// </summary>
        bool CanGrow { get; set; }

        /// <summary>
        /// Gets or sets the currently selected cell.
        /// </summary>
        IGridCell GetCurrentCell { get; set; }

        /// <summary>
        /// Return a particular cell of the grid.
        /// </summary>
        /// <param name="columnIndex">The column index</param>
        /// <param name="rowIndex">The row index</param>
        /// <returns>The cell</returns>
        IGridCell GetCell(int columnIndex, int rowIndex);

        /// <summary>
        /// Return a particular column of the grid.
        /// </summary>
        /// <param name="columnIndex">The column index</param>
        /// <returns>The column</returns>
        IGridColumn GetColumn(int columnIndex);

        /// <summary>
        /// Add a separator line to the context menu
        /// </summary>
        void AddContextSeparator();
        
        /// <summary>
        /// Add an option (on context menu) on the series grid.
        /// </summary>
        /// <param name="itemName">The name of the item</param>
        /// <param name="menuItemText">The text of the menu item - may include spaces or other "special" characters (if empty, the itemName is used)</param>
        /// <param name="onClick">The event handler to call when menu is selected</param>
        /// <param name="active">Indicates whether the option is current selected</param>
        void AddContextOption(string itemName, string menuItemText, System.EventHandler onClick, bool active);

        /// <summary>
        /// Clear all presenter defined context items.
        /// </summary>
        void ClearContextActions(bool hideDefault);

        /// <summary>
        /// Returns true if the grid row is empty.
        /// </summary>
        /// <param name="rowIndex">The row index</param>
        /// <returns>True if the specified row is empty</returns>
        bool RowIsEmpty(int rowIndex);

        /// <summary>
        /// End the user editing the cell.
        /// </summary>
        void EndEdit();

        /// <summary>Lock the left most number of columns.</summary>
        /// <param name="number"></param>
        void LockLeftMostColumns(int number);

        /// <summary>
        /// Indicates that a row should be treated as a separator line
        /// </summary>
        /// <param name="row">the row number</param>
        /// <param name="isSep">added as a separator if true; removed as a separator if false</param>
        void SetRowAsSeparator(int row, bool isSep = true);

        /// <summary>
        /// Checks if a row is a separator row.
        /// </summary>
        /// <param name="row">Index of the row.</param>
        /// <returns>True iff the row is a separator row.</returns>
        bool IsSeparator(int row);

        /// <summary>
        /// Inserts text into the current cell at the cursor position.
        /// </summary>
        /// <param name="text">Text to be inserted.</param>
        void InsertText(string text);

        /// <summary>
        /// Refreshes the grid.
        /// </summary>
        void Refresh();

        /// <summary>
        /// Refreshes the grid and updates the model.
        /// </summary>
        /// <param name="args"></param>
        void Refresh(GridCellsChangedArgs args);

        /// <summary>
        /// Unselects any currently selected cells and selects a new range of cells.
        /// Passing in a null or empty list of cells will deselect all cells.
        /// </summary>
        /// <param name="cells">Cells to be selected.</param>
        void SelectCells(List<IGridCell> cells);

        /// <summary>
        /// Does some cleanup work on the Grid.
        /// </summary>
        void Dispose();
    }
}
