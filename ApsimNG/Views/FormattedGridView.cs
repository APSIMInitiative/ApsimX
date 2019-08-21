namespace UserInterface.Views
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using Gtk;
    using EventArguments;
    using Interfaces;
    using System.Data;
    using APSIM.Shared.Utilities;
    using System.Collections;

    /// <summary>Metadata about a column on the grid.</summary>
    public class GridColumnMetaData
    {
        public string ColumnName { get; set; }
        public Type ColumnDataType { get; set; }
        public string Format { get; set; }
        public IEnumerable Values { get; set; }
        public bool ValuesHaveChanged { get; set; }
        public Color BackgroundColour { get; set; } = Color.Empty;
        public Color ForegroundColour { get; set; } = Color.Empty;
        public bool IsReadOnly { get; set; }
        public string[] CellToolTips { get; set; }
        public string[] HeaderContextMenuItems { get; set; }
        public bool AddTotalToColumnName { get; set; }
        public int Width { get; set; } = 70;         // -1 is auto sized.
        public int MinimumWidth { get; set; } = 70;  // pixels
        public bool LeftJustification { get; set; } = false;
    }

    /// <summary>
    /// Event arguments used to perform an action on a range of grid cells.
    /// </summary>
    public class GridCellColumnMenuClickedArgs : EventArgs
    {
        /// <summary>The column that had its context menu clicked.</summary>
        public GridColumnMetaData ColumnClicked { get; set; }

        /// <summary>The menu name that was clicked.</summary>
        public string MenuNameClicked { get; set; }

    }

    interface IFormattedGridView
    {
        /// <summary>This event is invoked when the values of 1 or more cells have changed.</summary>
        event EventHandler CellsHaveChanged;

        /// <summary>This event is invoked when a column has one of its context menu items clicked.</summary>
        event EventHandler<GridCellColumnMenuClickedArgs> ColumnMenuClicked;

        /// <summary>Set the columns for the grid.</summary>
        void SetColumns(List<GridColumnMetaData> columns);

        /// <summary>End the user editing the cell.</summary>
        void EndEdit();
    }

    public class FormattedGridView : GridView, IFormattedGridView
    {
        /// <summary>The columns in the grid.</summary>
        private List<GridColumnMetaData> columnMetadata;
        
        /// <summary>When the user right clicks a column header, this field contains the index of that column.</summary>
        private int indexOfClickedVariable;

        /// <summary>This event is invoked when the values of 1 or more cells have changed.</summary>
        public event EventHandler CellsHaveChanged;

        // <summary>This event is invoked when a column has one of its context menu items clicked.</summary>
        public event EventHandler<GridCellColumnMenuClickedArgs> ColumnMenuClicked;
        
        
        // <summary>Constructor.</summary>
        public FormattedGridView(ViewBase owner) : base(owner)
        {
            CellsChanged += OnCellValueChanged;
            mainWidget.Destroyed += MainWidgetDestroyed;
            GridColumnClicked += OnGridColumnClicked;
        }

        /// <summary>
        /// Does cleanup when the main widget is destroyed.
        /// </summary>
        /// <param name="sender">The sending object.</param>
        /// <param name="e">The event arguments.</param>
        private void MainWidgetDestroyed(object sender, EventArgs e)
        {
            GridColumnClicked -= OnGridColumnClicked;
            CellsChanged -= OnCellValueChanged;
            mainWidget.Destroyed -= MainWidgetDestroyed;
        }

        /// <summary>Set the columns for the grid.</summary>
        /// <param name="columns">The columns to show on grid.</param>
        public void SetColumns(List<GridColumnMetaData> columns)
        {
            columnMetadata = columns;
            DataSource = CreateTable();
            FormatGrid();
        }

        /// <summary>Create a datatable based on the properties.</summary>
        /// <returns>The filled data table. Never returns null.</returns>
        private DataTable CreateTable()
        {
            DataTable table = new DataTable();

            foreach (var column in this.columnMetadata)
            {
                string columnName = column.ColumnName;

                // add a total to the column name if necessary.
                if (column.AddTotalToColumnName)
                    columnName = GetColumnNameWithTotal(column.Values, columnName);

                // Get values
                DataTableUtilities.AddColumnOfObjects(table, columnName, column.Values as IEnumerable);
            }
            // Add in a dummy column so that the far right column with data isn't really wide.
            table.Columns.Add(" ");

            return table;
        }

        /// <summary>Get a column name with total.</summary>
        /// <param name="values"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        private static string GetColumnNameWithTotal(IEnumerable values, string columnName)
        {
            try
            {
                if (values != null)
                {
                    double total = MathUtilities.Sum(values as double[]);
                    int posBracket = columnName.IndexOf('(');
                    if (posBracket == -1)
                        columnName = columnName + "\r\n" + total.ToString("N1");
                    else
                        columnName = columnName.Insert(posBracket, total.ToString("N1") + " ");
                }
            }
            catch (Exception)
            { }
            return columnName;
        }

        /// <summary>Format the grid based on the data in the column metadata.</summary>
        private void FormatGrid()
        {
            for (int col = 0; col < columnMetadata.Count; col++)
            {
                var gridColumn = GetColumn(col);

                // Set grid format.
                gridColumn.Format = columnMetadata[col].Format;
                if (string.IsNullOrEmpty(gridColumn.Format))
                    gridColumn.Format = "N3";

                // Set colours
                gridColumn.BackgroundColour = columnMetadata[col].BackgroundColour;
                gridColumn.ForegroundColour = columnMetadata[col].ForegroundColour;

                // Set other properties.
                gridColumn.ReadOnly = columnMetadata[col].IsReadOnly;
                gridColumn.Width = columnMetadata[col].Width;
                gridColumn.MinimumWidth = columnMetadata[col].MinimumWidth;
                gridColumn.LeftJustification = columnMetadata[col].LeftJustification;
                gridColumn.HeaderLeftJustification = columnMetadata[col].LeftJustification;
            }

            // Make dummy column at right readonly.
            var dummyColumn = GetColumn(columnMetadata.Count);
            dummyColumn.ReadOnly = true;

            RowCount = 100;
        }

        /// <summary>User has changed the value of one or more cells in the profile grid.</summary>
        /// <param name="sender">The sender of the event</param>
        /// <param name="e">The event arguments</param>
        private void OnCellValueChanged(object sender, GridCellsChangedArgs e)
        {
            bool refreshGrid = false;
            foreach (var changedCell in e.ChangedCells)
            {
                if (!Convert.IsDBNull(changedCell.Value))
                {
                    GridColumnMetaData column = columnMetadata[changedCell.ColumnIndex];
                    Array array = column.Values as Array;
                    int numValues = e.ChangedCells.Max(cell => cell.RowIndex);
                    if (array == null)
                    {
                        array = Array.CreateInstance(column.ColumnDataType, numValues + 1);
                    }

                    // If we've added a new row, the column metadata will not contain an entry
                    // for this row (the array will be too short).
                    if (array.Length <= changedCell.RowIndex)
                    {
                        Array newArray = Array.CreateInstance(column.ColumnDataType, numValues + 1);
                        array.CopyTo(newArray, 0);
                        array = newArray;
                    }
                    array.SetValue(Convert.ChangeType(changedCell.Value, column.ColumnDataType), changedCell.RowIndex);

                    column.Values = array;
                    column.ValuesHaveChanged = true;
                    if (column.AddTotalToColumnName)
                        refreshGrid = true;
                }
                else
                {
                    GridColumnMetaData column = columnMetadata[changedCell.ColumnIndex];
                    Array array = column.Values as Array;
                    if (column.ColumnDataType == typeof(double) || column.ColumnDataType == typeof(float))
                        array.SetValue(double.NaN, changedCell.RowIndex);
                    column.Values = array;
                    column.ValuesHaveChanged = true;
                }
            }

            if (refreshGrid)
            {
                // refresh
                DataSource = CreateTable();
                FormatGrid();
            }

            CellsHaveChanged?.Invoke(this, new EventArgs());
        }

        /// <summary>Invoked when user clicks on a grid column.</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnGridColumnClicked(object sender, GridColumnClickedArgs e)
        {
            ClearContextActions(!e.OnHeader);
            if (e.RightClick && e.OnHeader)
            {
                indexOfClickedVariable = e.Column.ColumnIndex;
                var property = columnMetadata[indexOfClickedVariable];
                if (property.HeaderContextMenuItems != null)
                {
                    if (!e.OnHeader)
                        AddContextSeparator();
                    foreach (var menuItem in property.HeaderContextMenuItems)
                        AddContextOption(menuItem, menuItem, OnMenuItemClick, property.ColumnName.Contains("(" + menuItem + ")"));
                }
            }
        }

        /// <summary>Invoked when user selects a header menu item.</summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMenuItemClick(object sender, EventArgs e)
        {
            ColumnMenuClicked?.Invoke(this, new GridCellColumnMenuClickedArgs()
            {
                MenuNameClicked = (sender as MenuItem).Name,
                ColumnClicked = columnMetadata[indexOfClickedVariable]
            });

            // refresh
            DataSource = CreateTable();
            FormatGrid();
        }
    }
}
