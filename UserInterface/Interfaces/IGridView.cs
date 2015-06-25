// -----------------------------------------------------------------------
// <copyright file="IGridView.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
// -----------------------------------------------------------------------
namespace UserInterface.Interfaces
{
    using System;
    using EventArguments;

    /// <summary>
    /// The interface to a grid view. Clients of this class should set the data source
    /// before calling other methods.
    /// </summary>
    public interface IGridView
    {
        /// <summary>
        /// This event is invoked when the values of 1 or more cells have changed.
        /// </summary>
        event EventHandler<GridCellsChangedArgs> CellsChanged;

        /// <summary>
        /// Invoked when a grid cell header is clicked.
        /// </summary>
        event EventHandler<GridHeaderClickedArgs> ColumnHeaderClicked;

        /// <summary>Occurs when user clicks a button on the cell.</summary>
        event EventHandler<GridCellsChangedArgs> ButtonClick;

        /// <summary>
        /// Gets or sets the data to use to populate the grid.
        /// </summary>
        System.Data.DataTable DataSource { get; set; }

        /// <summary>
        /// Gets or sets the number of rows in grid.
        /// </summary>
        int RowCount { get; set; }
        
        /// <summary>
        /// Gets or sets the numeric grid format e.g. N3
        /// </summary>
        string NumericFormat { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the grid is read only
        /// </summary>
        bool ReadOnly { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the grid has an auto filter
        /// </summary>
        bool AutoFilterOn { get; set; }

        /// <summary>
        /// The name of the associated model.
        /// </summary>
        string ModelName { get; set; }

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
        /// Add an action (on context menu) on the series grid.
        /// </summary>
        /// <param name="menuItemText">The text of the menu item</param>
        /// <param name="onClick">The event handler to call when menu is selected</param>
        void AddContextAction(string menuItemText, System.EventHandler onClick);

        /// <summary>
        /// Clear all presenter defined context items.
        /// </summary>
        void ClearContextActions();

        /// <summary>
        /// Load the image associated with the Model (if it exists).
        /// </summary>
        void LoadImage();

        /// <summary>
        /// Returns true if the grid row is empty.
        /// </summary>
        /// <param name="rowIndex">The row index</param>
        /// <returns>True if the specified row is empty</returns>
        bool RowIsEmpty(int rowIndex);

        /// <summary>
        /// Resizes controls on the GridView.
        /// </summary>
        void ResizeControls();
    }
}
