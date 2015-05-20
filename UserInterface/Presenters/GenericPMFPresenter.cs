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
using System.Reflection;

namespace UserInterface.Presenters
{
    class GenericPMFPresenter : IPresenter
    {
        /// <summary>
        /// The model used
        /// </summary>
        IModel model;
        /// <summary>
        /// The Dependenies view used
        /// </summary>
        GenericPMFView genericPMFView;
        /// <summary>
        /// The Dependenies view used
        /// </summary>
        IGridView gridViewDependencies;
        /// <summary>
        /// The Dependenies view used
        /// </summary>
        IGridView gridViewParameters;
        /// <summary>
        /// The Dependenies view used
        /// </summary>
        IGridView gridViewProperties;
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
            this.model = Model as IModel;
            this.genericPMFView = view as GenericPMFView;
            this.explorerPresenter = explorerPresenter;


            //Grab the individual grid views
            gridViewDependencies = genericPMFView.DependenciesGrid;
            gridViewParameters = genericPMFView.ParametersGrid;
            gridViewProperties = genericPMFView.PropertiesGrid;

            PopulateGrids();

            this.gridViewProperties.CellsChanged += this.OnCellValueChanged;
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
        public void PopulateGrids()
        {
            PopulateDependenciesGrid(model);

            PopulateParametersGrid(model);

            PopulatePropertiesGrid(model);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        public void PopulateDependenciesGrid(IModel model)
        {
            IGridCell selectedCell = this.gridViewDependencies.GetCurrentCell;

            DataTable table = new DataTable();

            table.Columns.Add("Value", typeof(object));

            //table.Rows.Add(new object[] { model.Value });

            //this.gridView.DataSource = table;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        public void PopulateParametersGrid(IModel model)
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="model"></param>
        public void PopulatePropertiesGrid(IModel model)
        {
            PropertyInfo[] properties = model.GetType().GetProperties();

            DataTable table = new DataTable();

            table.Columns.Add("Property Name", typeof(object));
            table.Columns.Add("Property Value", typeof(object));

            for (int i = 0; i < properties.Length; i++)
            {
                try
                {
                    table.Rows.Add(new object[] { properties[i].Name, properties[i].GetValue(model, null) });
                }
                catch (Exception e)
                {
                    string a = e.Message;
                }
            }

            this.gridViewProperties.DataSource = table;
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
            //Should only be the propertires grid that has been changed
            this.explorerPresenter.CommandHistory.ModelChanged -= this.OnModelChanged;

            IGridCell cell = e.ChangedCells[0];

            double val;

            if (double.TryParse(cell.Value.ToString(), out val))
            {
                //model.Value = val;
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
                this.PopulateGrids();
            }
        }
    }
}
