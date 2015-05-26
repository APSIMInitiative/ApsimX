using System;
using UserInterface.Views;
using Models.Graph;
using Models.Core;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Collections;
using System.Linq;
using Models.Factorial;
using UserInterface.Interfaces;
using System.Data;
using Models;
using UserInterface.EventArguments;
using Models.Soils;
using APSIM.Shared.Utilities;
using Models.PMF.Functions;

namespace UserInterface.Presenters
{
    class ConstantPresenter : IPresenter
    {
        /// <summary>
        /// The model used
        /// </summary>
        Constant model;
        /// <summary>
        /// The view used
        /// </summary>
        IGridView gridView;
        /// <summary>
        /// The explorer presenter used
        /// </summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>
        /// Attach the Manager model and ManagerView to this presenter.
        /// </summary>
        /// <param name="model">The model</param>
        /// <param name="view">The view to attach</param>
        /// <param name="explorerPresenter">The explorer presenter being used</param>
        public void Attach(object Model, object view, ExplorerPresenter explorerPresenter)
        {
            this.model = Model as Constant;
            this.gridView = view as IGridView;
            this.explorerPresenter = explorerPresenter;

            PopulateGrid();

            this.gridView.CellsChanged += this.OnCellValueChanged;
            this.explorerPresenter.CommandHistory.ModelChanged += this.OnModelChanged;
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {

        }

        /// <summary>
        /// Populate the grid
        /// </summary>
        /// <param name="model">The model to examine for properties</param>
        public void PopulateGrid()
        {
            IGridCell selectedCell = this.gridView.GetCurrentCell;

            DataTable table = new DataTable();

            table.Columns.Add("Value", typeof(object));

            table.Rows.Add(new object[] { model.Value });

            this.gridView.DataSource = table;
        }

        /// <summary>
        /// Format the grid.
        /// </summary>
        private void FormatGrid()
        {

        }

        /// <summary>
        /// User has changed the value of a cell.
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnCellValueChanged(object sender, GridCellsChangedArgs e)
        {
            this.explorerPresenter.CommandHistory.ModelChanged -= this.OnModelChanged;

            IGridCell cell = e.ChangedCells[0];

            double val;

            if (double.TryParse(cell.Value.ToString(), out val))
            {
                model.Value = val;
            }

            this.explorerPresenter.CommandHistory.ModelChanged += this.OnModelChanged;
        }

        /// <summary>
        /// The model has changed, update the grid.
        /// </summary>
        /// <param name="changedModel">The model that has changed</param>
        private void OnModelChanged(object changedModel)
        {
            if (changedModel == this.model)
            {
                this.PopulateGrid();
            }
        }
    }
}
