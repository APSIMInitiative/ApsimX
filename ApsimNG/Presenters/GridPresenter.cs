namespace UserInterface.Presenters
{
    using EventArguments;
    using Interfaces;
    using Models.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Text;
    using System.Linq;

    /// <summary>
    /// This presenter displays a table of data, which it gets from the model via
    /// the interface IModelAsTable, allows user to edit it and returns the data
    /// to the model via the same interface.
    /// </summary>
    public class GridPresenter : IPresenter
    {
        /// <summary>
        /// The underlying grid control to work with.</summary>
        protected IGridView grid;

        /// <summary>
        /// The parent ExplorerPresenter.
        /// </summary>
        protected ExplorerPresenter presenter;

        /// <summary>
        /// Attach the model to the view.
        /// </summary>
        /// <param name="model">The model to connect to</param>
        /// <param name="view">The view to connect to</param>
        /// <param name="parentPresenter">The parent explorer presenter</param>
        public virtual void Attach(object model, object view, ExplorerPresenter parentPresenter)
        {
            grid = view as IGridView;
            grid.CopyCells += CopyCells;
            grid.PasteCells += PasteCells;
            grid.DeleteCells += DeleteCells;
            presenter = parentPresenter;
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public virtual void Detach()
        {
            if (grid != null)
            {
                grid.EndEdit();
                grid.CopyCells -= CopyCells;
                grid.PasteCells -= PasteCells;
                grid.DeleteCells -= DeleteCells;
                grid.Dispose();
            }
        }

        /// <summary>
        /// Copies data from a cell or range of cells to the clipboard.
        /// If the first cell is the same as the last cell, only the data from that cell will be copied.
        /// </summary>
        private void CopyCells(object sender, GridCellActionArgs args)
        {
            StringBuilder textToCopy = new StringBuilder();
            for (int row = args.StartCell.RowIndex; row <= args.EndCell.RowIndex; row++)
            {
                for (int column = args.StartCell.ColumnIndex; column <= args.EndCell.ColumnIndex; column++)
                {
                    textToCopy.Append(grid.GetCell(column, row).Value.ToString());
                    if (column != args.EndCell.ColumnIndex)
                        textToCopy.Append('\t');
                }
                textToCopy.AppendLine();
            }
            presenter.SetClipboardText(textToCopy.ToString(), "CLIPBOARD");
        }

        /// <summary>
        /// Pastes tab-delimited data from the clipboard into a range of cells.
        /// </summary>
        private void PasteCells(object sender, GridCellPasteArgs args)
        {
            List<IGridCell> cellsChanged = new List<IGridCell>();
            List<GridCellChangedArgs> changedArgs = new List<GridCellChangedArgs>();

            string[] lines = args.Text.Split(Environment.NewLine.ToCharArray()).Where(line => !string.IsNullOrEmpty(line)).ToArray();
            for (int i = 0; i < lines.Length; i++)
            {
                // Add new rows, if necessary.
                while (args.Grid.RowCount < args.StartCell.RowIndex + i + 1)
                    args.Grid.RowCount++;
                string[] words = lines[i].Split('\t');
                int numReadOnlyColumns = 0;
                for (int j = 0; j < words.Length; j++)
                {
                    // Skip over any read-only columns.
                    IGridColumn column = args.Grid.GetColumn(args.StartCell.ColumnIndex + j + numReadOnlyColumns);
                    while (column != null && column.ReadOnly)
                    {
                        numReadOnlyColumns++;
                        column = args.Grid.GetColumn(args.StartCell.ColumnIndex + j + numReadOnlyColumns);
                    }
                    if (column == null)
                        throw new Exception("Error pasting into grid - not enough columns.");

                    int row = args.StartCell.RowIndex + i;
                    int col = args.StartCell.ColumnIndex + j + numReadOnlyColumns;

                    IGridCell cell = args.Grid.GetCell(col, row);

                    string oldValue = null;
                    if (args.Grid.DataSource.Rows.Count > row)
                        oldValue = args.Grid.DataSource.Rows[row][col]?.ToString();

                    string newValue = words[j];

                    changedArgs.Add(new GridCellChangedArgs(row, col, oldValue, newValue));

                    // fixme - this should not be set here. What if the item is an array?
                    //cell.Value = Convert.ChangeType(words[j], args.Grid.DataSource.Columns[args.StartCell.ColumnIndex + j + numReadOnlyColumns].DataType);

                    cellsChanged.Add(cell);
                }
            }
            // If some cells were changed then send out an event.
            args.Grid.SelectCells(cellsChanged);
            args.Grid.Refresh(new GridCellsChangedArgs(changedArgs.ToArray()));
        }

        /// <summary>
        /// Deletes data from a cell or range of cells.
        /// If the first cell is the same as the last cell, only the data from that cell will be copied.
        /// </summary>
        private void DeleteCells(object sender, GridCellActionArgs args)
        {
            if (args.Grid.ReadOnly)
                throw new Exception("Unable to delete cells - grid is read-only.");
            List<IGridCell> cellsChanged = new List<IGridCell>();
            List<GridCellChangedArgs> changedArgs = new List<GridCellChangedArgs>();
            for (int row = args.StartCell.RowIndex; row <= args.EndCell.RowIndex; row++)
            {
                for (int column = args.StartCell.ColumnIndex; column <= args.EndCell.ColumnIndex; column++)
                {
                    if (!args.Grid.GetColumn(column).ReadOnly)
                    {
                        string oldValue = args.Grid.DataSource.Rows[row][column]?.ToString();
                        string newValue = null;
                        changedArgs.Add(new GridCellChangedArgs(row, column, oldValue, newValue));
                        args.Grid.DataSource.Rows[row][column] = DBNull.Value;
                        cellsChanged.Add(grid.GetCell(column, row));
                    }
                }
                //if (args.Grid.CanGrow && args.StartCell.ColumnIndex == 0 && args.EndCell.ColumnIndex == args.Grid.DataSource.Columns.Count - 1)
                //{
                //    // User has selected the entire row. In this case, we delete the entire row,
                //    // but only if the grid can change size.
                //    args.Grid.DataSource.Rows.Remove(args.Grid.DataSource.Rows[row]);
                //}
            }
            // If some cells were changed then we will need to update the model.
            if (cellsChanged.Count > 0)
                args.Grid.Refresh(new GridCellsChangedArgs(changedArgs.ToArray()));
        }
    }
}
