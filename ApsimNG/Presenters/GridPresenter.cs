namespace UserInterface.Presenters
{
    using EventArguments;
    using Interfaces;
    using Models.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Text;
    
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
        public void Attach(object model, object view, ExplorerPresenter parentPresenter)
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
        public void Detach()
        {
            grid.EndEdit();
            grid.CopyCells -= CopyCells;
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
        /// Pastes data from the clipboard into a cell or range of cells.
        /// If the first cell is the same as the last cell, data will only be pasted into that cell.
        /// If the selected number of cells is less than the number of cells the user has copied,
        /// a dialog will be shown to the user, asking for confirmation before proceeding.
        /// </summary>
        private void PasteCells(object sender, GridCellPasteArgs args)
        {
            List<IGridCell> cellsChanged = new List<IGridCell>();
            string[] lines = args.Text.Split(Environment.NewLine.ToCharArray());
            for (int i = 0; i < lines.Length; i++)
            {
                // Add new rows, if necessary.
                if (args.Grid.RowCount < args.StartCell.RowIndex + i + 1)
                    args.Grid.RowCount++;
                string[] words = lines[i].Split('\t');
                int numReadOnlyColumns = 0;
                for (int j = 0; j < words.Length; j++)
                {
                    IGridColumn column = args.Grid.GetColumn(args.StartCell.ColumnIndex + j + numReadOnlyColumns);
                    while (column != null && column.ReadOnly)
                    {
                        numReadOnlyColumns++;
                        column = args.Grid.GetColumn(args.StartCell.ColumnIndex + j + numReadOnlyColumns);
                    }
                    if (column == null)
                        throw new Exception("Error pasting into grid - not enough columns.");
                    /*
                    if (column <= args.EndCell.ColumnIndex && !args.Grid.GetColumn(column).ReadOnly)
                    {
                        string word = words.Length >= column ? words[column] : null;
                        IGridCell cell = args.Grid.GetCell(column, row);
                        cell.Value = string.IsNullOrEmpty(words[column]) ? DBNull.Value.ToString() : words[column];
                        cellsChanged.Add(cell);
                    }
                    */
                }
            }
            /*
            foreach (string line in lines)
            {
                if (rowIndex < RowCount && line.Length > 0)
                {
                    string[] words = line.Split('\t');
                    for (int i = 0; i < words.GetLength(0); ++i)
                    {
                        if (columnIndex + i < DataSource.Columns.Count)
                        {
                            // Make sure there are enough rows in the data source.
                            while (DataSource.Rows.Count <= rowIndex)
                            {
                                DataSource.Rows.Add(DataSource.NewRow());
                            }

                            IGridCell cell = GetCell(columnIndex + i, rowIndex);
                            IGridColumn column = GetColumn(columnIndex + i);
                            if (!column.ReadOnly)
                            {
                                try
                                {
                                    if (cell.Value == null || AsString(cell.Value) != words[i])
                                    {
                                        // We are pasting a new value for this cell. Put the new
                                        // value into the cell.
                                        if (words[i] == string.Empty)
                                        {
                                            cell.Value = DBNull.Value;
                                        }
                                        else
                                        {
                                            cell.Value = Convert.ChangeType(words[i], DataSource.Columns[columnIndex + i].DataType);
                                        }

                                        // Put a cell into the cells changed member.
                                        cellsChanged.Add(GetCell(columnIndex + i, rowIndex));
                                    }
                                }
                                catch (FormatException)
                                {
                                }
                            }
                        }
                        else
                            break;
                    }
                    rowIndex++;
                }
                else
                    break;
                    */
            }
            /*
            // If some cells were changed then send out an event.
            if (cellsChanged.Count > 0 && CellsChanged != null)
            {
                fixedColView.QueueDraw();
                Grid.QueueDraw();
                CellsChanged.Invoke(this, new GridCellsChangedArgs() { ChangedCells = cellsChanged });
            }
            */
        }

        /// <summary>
        /// Deletes data from a cell or range of cells.
        /// If the first cell is the same as the last cell, only the data from that cell will be copied.
        /// </summary>
        private void DeleteCells(object sender, GridCellActionArgs args)
        {
            List<IGridCell> cellsChanged = new List<IGridCell>();
            for (int row = args.StartCell.RowIndex; row <= args.EndCell.RowIndex; row++)
            {
                for (int column = args.StartCell.ColumnIndex; column <= args.EndCell.ColumnIndex; column++)
                {
                    if (!args.Grid.GetColumn(column).ReadOnly)
                    {
                        args.Grid.DataSource.Rows[row][column] = DBNull.Value;
                        cellsChanged.Add(grid.GetCell(column, row));
                    }
                }
            }
            // If some cells were changed then we will need to update the model.
            if (cellsChanged.Count > 0)
                args.Grid.Refresh(new GridCellsChangedArgs() { ChangedCells = cellsChanged });
        }
    }
}
