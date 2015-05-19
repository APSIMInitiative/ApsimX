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
    class XYFunctionPresenter : IPresenter
    {

        /// <summary>
        /// The function model.
        /// </summary>
        private LinearInterpolationFunction function;

        /// <summary>
        /// The function view;
        /// </summary>
        private IFunctionView functionView;

        /// <summary>
        /// The parent explorer presenter.
        /// </summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>
        /// Our grid.
        /// </summary>
        private IGridView gridView;

        /// <summary>
        /// Our grid.
        /// </summary>
        private IGridView gridViewXProp;

        /// <summary>
        /// 
        /// </summary>
        //private Graph graph;

        /// <summary>
        /// 
        /// </summary>
        //private IGraphView graphView;

        /// <summary>
        /// 
        /// </summary>
        //private GraphPresenter graphPresenter;

        /// <summary>
        /// Attach the view to the model.
        /// </summary>
        /// <param name="model">The function model</param>
        /// <param name="view">The function view</param>
        /// <param name="explorerPresenter">The parent explorer presenter</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.function = model as LinearInterpolationFunction;
            this.functionView = view as IFunctionView;
            this.explorerPresenter = explorerPresenter as ExplorerPresenter;

            //this.ConnectViewEvents();
            gridViewXProp = functionView.gridViewXProp;
            gridView = functionView.gridView;


            //graphView = functionView.graphView;

            //graphPresenter = new GraphPresenter();

            //graph = new Graph();

            //graphPresenter.Attach(graph, graphView, explorerPresenter);
            PopulateGridXProp();
            PopulateGrid();


            this.gridViewXProp.CellsChanged += this.OnCellValueChangedXProp;
            this.gridView.CellsChanged += this.OnCellValueChanged;
            this.explorerPresenter.CommandHistory.ModelChanged += this.OnModelChanged;

            //graphPresenter.DrawGraph();
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            this.explorerPresenter.CommandHistory.ModelChanged -= OnModelChanged;
            // this.DisconnectViewEvents();
        }
        /// <summary>
        /// Populate the grid
        /// </summary>
        /// <param name="model">The model to examine for properties</param>
        public void PopulateGridXProp()
        {
            IGridCell selectedCell = this.gridViewXProp.GetCurrentCell;

            DataTable table = new DataTable();

            table.Columns.Add("X Property", typeof(object));

            table.Rows.Add(new object[] { function.XProperty });

            this.gridViewXProp.DataSource = table;
        }
        /// <summary>
        /// Populate the grid
        /// </summary>
        /// <param name="model">The model to examine for properties</param>
        public void PopulateGrid()
        {
            IGridCell selectedCell = this.gridView.GetCurrentCell;

            DataTable table = new DataTable();

            table.Columns.Add("X", typeof(object));
            table.Columns.Add("Y", typeof(object));

            for (int i = 0; i < function.XYPairs.X.Length; i++)
            {
                table.Rows.Add(new object[] { function.XYPairs.X[i], function.XYPairs.Y[i] });
            }

            this.gridView.DataSource = table;
        }

        /// <summary>
        /// 
        /// </summary>
        private void setupChart()
        {
            //this.graph.Series.Clear();
            //this.graph.Series.Add(new Series());        

        }
        ///// <summary>
        ///// Connect all events from the view.
        ///// </summary>
        //private void ConnectViewEvents()
        //{
        //   // this.functionView.OnCellValueChanged += this.OnCellValueChanged;
        //}

        ///// <summary>
        ///// Disconnect all view events.
        ///// </summary>
        //private void DisconnectViewEvents()
        //{
        //   // this.functionView.OnCellValueChanged -= this.OnCellValueChanged;
        //}

        /// <summary>
        /// User has changed the value of a cell.
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnCellValueChangedXProp(object sender, GridCellsChangedArgs e)
        {
            this.explorerPresenter.CommandHistory.ModelChanged -= this.OnModelChanged;

            IGridCell cell = e.ChangedCells[0];

            function.XProperty = cell.Value.ToString();

            this.explorerPresenter.CommandHistory.ModelChanged += this.OnModelChanged;
        }

        /// <summary>
        /// User has changed the value of a cell.
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnCellValueChanged(object sender, GridCellsChangedArgs e)
        {
            this.explorerPresenter.CommandHistory.ModelChanged -= this.OnModelChanged;

            foreach (IGridCell cell in e.ChangedCells)
            {
                double val;
                if (double.TryParse(cell.Value.ToString(), out val))
                {
                    if (cell.ColumnIndex == 0)
                    {
                        function.XYPairs.X[cell.RowIndex] = val;
                    }
                    else
                    {
                        function.XYPairs.Y[cell.RowIndex] = val;
                    }
                }
            }

            this.explorerPresenter.CommandHistory.ModelChanged += this.OnModelChanged;
        }

        /// <summary>
        /// The model has changed. Update the view.
        /// </summary>
        /// <param name="changedModel">The model that has changed.</param>
        void OnModelChanged(object changedModel)
        {
            if (changedModel == this.function)
                this.PopulateGrid();
        }
    }
}
