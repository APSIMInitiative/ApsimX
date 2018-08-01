namespace UserInterface.Presenters
{
    using EventArguments;
    using Interfaces;
    using Models.Interfaces;
    using System;
    using System.Collections.Generic;
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
        /// If the first cell is the same as the last cell, only the data from that cell will be copied.
        /// </summary>
        private void PasteCells(object sender, GridCellActionArgs args)
        {

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
