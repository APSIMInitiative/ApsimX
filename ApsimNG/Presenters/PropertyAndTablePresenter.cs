namespace UserInterface.Presenters
{
    using APSIM.Shared.Utilities;
    using Commands;
    using EventArguments;
    using Interfaces;
    using Models.Core;
    using Models.Interfaces;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using Views;

    /// <summary>Presenter that has a PropertyPresenter and a GridPresenter.</summary>
    class PropertyAndTablePresenter : IPresenter
    {
        /// <summary>The underlying model</summary>
        private IModelAsTable tableModel;

        private IDualGridView view;
        private ExplorerPresenter explorerPresenter;
        private DataTable table;
        private PropertyPresenter propertyPresenter;
        private GridPresenter gridPresenter;

        /// <summary>
        /// Attach the model to the view.
        /// </summary>
        /// <param name="model">The model to connect to.</param>
        /// <param name="v">The view to connect to.</param>
        /// <param name="parentPresenter">The parent explorer presenter.</param>
        public void Attach(object model, object v, ExplorerPresenter parentPresenter)
        {
            explorerPresenter = parentPresenter;
            view = v as IDualGridView;
            tableModel = model as IModelAsTable;
            if (tableModel.Tables.Count != 1)
                throw new Exception("PropertyAndTablePresenter must have a single data table.");
            table = tableModel.Tables[0];
            view.Grid2.DataSource = table;
            view.Grid2.CellsChanged += OnCellValueChanged2;
            view.Grid2.NumericFormat = null;
            parentPresenter.CommandHistory.ModelChanged += OnModelChanged;

            propertyPresenter = new PropertyPresenter();
            explorerPresenter.ApsimXFile.Links.Resolve(propertyPresenter);
            propertyPresenter.Attach(model, view.Grid1, parentPresenter);
            gridPresenter = new GridPresenter();
            gridPresenter.Attach(model, view.Grid2, parentPresenter);
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            view.Grid2.CellsChanged -= OnCellValueChanged2;
            propertyPresenter.Detach();
            gridPresenter.Detach();
            explorerPresenter.CommandHistory.ModelChanged -= OnModelChanged;
        }

        /// <summary>
        /// User has changed the value of a cell.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnCellValueChanged2(object sender, GridCellsChangedArgs e)
        {
            // If all cells are being set to null, and the number of changed
            // cells is a multiple of the number of properties, we could be
            // deleting an entire row (or rows). In this case, we need
            // different logic, to resize all of the arrays.
            bool deletedRow = false;
            if (e.ChangedCells.All(c => c.NewValue == null) && e.ChangedCells.Count % 4 == 0)
            {
                // Get list of distinct rows which have been changed.
                int[] changedRows = e.ChangedCells.Select(c => c.RowIndex).Distinct().ToArray();
                List<int> rowsToDelete = new List<int>();
                foreach (int row in changedRows)
                {
                    // Get columns which have been changed in this row.
                    var changesInRow = e.ChangedCells.Where(c => c.RowIndex == row);
                    int[] columns = changesInRow.Select(c => c.ColIndex).ToArray();

                    // If all non-readonly properties have been set to null in this row,
                    // delete the row.
                    bool deleteRow = true;
                    for (int i = 0; i < 4; i++)
                        if (!columns.Contains(i))
                            deleteRow = false;

                    if (deleteRow)
                    {
                        // We can't delete the row now - what if the user has deleted
                        // multiple rows at once (this is possible via shift-clicking).
                        // We need one change property command per property. Otherwise,
                        // only the last command will have an effect.
                        deletedRow = true;
                        rowsToDelete.Add(row);
                        e.ChangedCells = e.ChangedCells.Where(c => c.RowIndex != row).ToList();
                    }
                }

                for (int i = 0; i < rowsToDelete.Count; i++)
                {
                    int row = rowsToDelete[i];
                    table.Rows.RemoveAt(row);

                    // Row numbers will change after deleting each row.
                    rowsToDelete = rowsToDelete.Select(r => r > row ? r - 1 : r).ToList();
                }
            }

            foreach (GridCellChangedArgs change in e.ChangedCells)
            {
                if (change.NewValue == change.OldValue)
                    continue; // silently fail

                // If the user has entered data into a new row, we will need to
                // resize all of the array properties.
                CheckRowCount(change);

                object element = ReflectionUtilities.StringToObject(table.Columns[change.ColIndex].DataType, change.NewValue);

                table.Rows[change.RowIndex][change.ColIndex] = element;
            }

            // Apply all changes to the model in a single undoable command.
            explorerPresenter.CommandHistory.Add(new ChangeProperty(tableModel, "Tables", new List<DataTable>() { table }));

            // Update the value shown in the grid. This needs to happen after
            // we have applied changes to the model for obvious reasons.
            foreach (GridCellChangedArgs cell in e.ChangedCells)
            {
                // Add new rows to the view's grid if necessary.
                while (view.Grid2.RowCount <= cell.RowIndex + 1)
                    view.Grid2.RowCount++;
                view.Grid2.DataSource.Rows[cell.RowIndex][cell.ColIndex] = table.Rows[cell.RowIndex][cell.ColIndex];
            }

            // If the user deleted an entire row, do a full refresh of the
            // grid. Otherwise, only refresh read-only columns (PAWC).
            if (deletedRow)
                view.Grid2.DataSource = table;
        }

        private void CheckRowCount(GridCellChangedArgs change)
        {
            while (change.RowIndex >= table.Rows.Count)
                table.Rows.Add(table.NewRow());
        }

        private void DeleteRows(int from, int to)
        {
            if (from > to)
            {
                int tmp = to;
                to = from;
                from = tmp;
            }

            for (int i = from; i <= to; i++)
                table.Rows.RemoveAt(i);
        }

        /// <summary>
        /// The model has changed, update the grid.
        /// </summary>
        /// <param name="changedModel">The model that has changed.</param>
        private void OnModelChanged(object changedModel)
        {
            if (changedModel == tableModel)
            {
                table = tableModel.Tables[0];
                view.Grid2.DataSource = table;
            }
        }
    }
}
