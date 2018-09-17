namespace UserInterface.Presenters
{
    using Commands;
    using EventArguments;
    using Interfaces;
    using Models.Interfaces;
    using System;

    /// <summary>
    /// Presenter for any <see cref="IModelAsTable"/>.
    /// </summary>
    class TablePresenter : GridPresenter, IPresenter
    {
        /// <summary>
        /// The underlying model.
        /// </summary>
        private IModelAsTable tableModel;

        /// <summary>
        /// Attach the model to the view.
        /// </summary>
        /// <param name="model">The model to connect to.</param>
        /// <param name="view">The view to connect to.</param>
        /// <param name="parentPresenter">The parent explorer presenter.</param>
        public override void Attach(object model, object view, ExplorerPresenter parentPresenter)
        {
            base.Attach(model, view, parentPresenter);
            tableModel = model as IModelAsTable;
            grid.DataSource = tableModel.Table;
            grid.CellsChanged += OnCellValueChanged;
            presenter.CommandHistory.ModelChanged += OnModelChanged;
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public override void Detach()
        {
            grid.CellsChanged -= OnCellValueChanged;
            base.Detach();
            presenter.CommandHistory.ModelChanged -= OnModelChanged;
        }

        /// <summary>
        /// User has changed the value of a cell.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnCellValueChanged(object sender, GridCellsChangedArgs e)
        {
            foreach (IGridCell cell in e.ChangedCells)
            {
                try
                {
                    if (e.InvalidValue)
                        throw new Exception("The value you entered was not valid for its datatype.");
                    ChangeProperty cmd = new ChangeProperty(tableModel, "Table", grid.DataSource);
                    presenter.CommandHistory.Add(cmd);
                }
                catch (Exception ex)
                {
                    presenter.MainPresenter.ShowError(ex);
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
                grid.DataSource = tableModel.Table;
        }
    }
}
