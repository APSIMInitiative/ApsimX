//-----------------------------------------------------------------------
// <copyright file="GridPresenter.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace UserInterface.Presenters
{
    using Interfaces;
    using Models.Interfaces;

    /// <summary>
    /// This presenter displays a table of data, which it gets from the model via
    /// the interface IModelAsTable, allows user to edit it and returns the data
    /// to the model via the same interface.
    /// </summary>
    public class GridPresenter : IPresenter
    {
        /// <summary>The underlying grid control to work with.</summary>
        private IGridView grid;

        /// <summary>The interface we're going to work with</summary>
        private IModelAsTable tableModel;

        /// <summary>The parent ExplorerPresenter.</summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>
        /// Attach the model to the view.
        /// </summary>
        /// <param name="model">The model to connect to</param>
        /// <param name="view">The view to connect to</param>
        /// <param name="explorerPresenter">The parent explorer presenter</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.grid = view as IGridView;
            this.tableModel = model as IModelAsTable;
            this.explorerPresenter = explorerPresenter;

            // populate the grid
            this.grid.DataSource = tableModel.GetTable();
            grid.RowCount = 100;
            //this.grid.ResizeControls();

            this.explorerPresenter.CommandHistory.ModelChanged += this.OnModelChanged;
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            this.grid.EndEdit();
            this.explorerPresenter.CommandHistory.ModelChanged -= this.OnModelChanged;
            tableModel.SetTable(grid.DataSource);
        }

        /// <summary>
        /// The model has changed, update the grid.
        /// </summary>
        /// <param name="changedModel">The model that has changed</param>
        private void OnModelChanged(object changedModel)
        {
            this.grid.DataSource = tableModel.GetTable();
        }

    }
}
