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
            foreach (GridCellChangedArgs cell in e.ChangedCells)
            {
                try
                {
                    table = view.Grid2.DataSource;
                    Type dataType = table.Columns[cell.ColIndex].DataType;
                    object newValue = ReflectionUtilities.StringToObject(dataType, cell.NewValue);
                    table.Rows[cell.RowIndex][cell.ColIndex] = newValue;

                    ChangeProperty cmd = new ChangeProperty(tableModel, "Tables", new List<DataTable>() { table });
                    explorerPresenter.CommandHistory.Add(cmd);
                }
                catch (Exception ex)
                {
                    explorerPresenter.MainPresenter.ShowError(ex);
                }
            }
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
