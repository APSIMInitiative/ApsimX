namespace UserInterface.Presenters
{
    using Commands;
    using EventArguments;
    using Interfaces;
    using Models;
    using System;

    class MorrisPresenter : GridPresenter, IPresenter
    {
        /// <summary>
        /// The underlying model.
        /// </summary>
        private Morris morrisModel;

        /// <summary>
        /// Attach the model to the view.
        /// </summary>
        /// <param name="model">The model to connect to.</param>
        /// <param name="view">The view to connect to.</param>
        /// <param name="parentPresenter">The parent explorer presenter.</param>
        public override void Attach(object model, object view, ExplorerPresenter parentPresenter)
        {
            base.Attach(model, view, parentPresenter);
            morrisModel = model as Morris;
            grid.DataSource = morrisModel.GetTable();
            grid.CellsChanged += OnCellValueChanged;
            presenter.CommandHistory.ModelChanged += OnModelChanged;
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public override void Detach()
        {
            base.Detach();
        }

        /// <summary>
        /// User has changed the value of a cell.
        /// </summary>
        /// <param name="sender">Sender object.</param>
        /// <param name="e">Event arguments.</param>
        private void OnCellValueChanged(object sender, GridCellsChangedArgs e)
        {
            presenter.CommandHistory.ModelChanged -= OnModelChanged;

            foreach (IGridCell cell in e.ChangedCells)
            {
                try
                {
                    if (e.InvalidValue)
                        throw new Exception("The value you entered was not valid for its datatype.");
                    morrisModel.SetTable(grid.DataSource);
                    presenter.MainPresenter.ShowMessage("Warning: actions in the Morris view not undo-able!", Models.Core.Simulation.MessageType.Warning);
                    /*
                    ChangeProperty cmd = new ChangeProperty(morrisModel, property.Name, value);
                    presenter.CommandHistory.Add(cmd, true);
                    */
                }
                catch (Exception ex)
                {
                    presenter.MainPresenter.ShowError(ex);
                }
            }

            presenter.CommandHistory.ModelChanged += OnModelChanged;
        }

        /// <summary>
        /// The model has changed, update the grid.
        /// </summary>
        /// <param name="changedModel">The model that has changed.</param>
        private void OnModelChanged(object changedModel)
        {
            if (changedModel == morrisModel)
                grid.DataSource = morrisModel.GetTable();
        }
    }
}
