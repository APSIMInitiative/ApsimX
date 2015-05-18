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
        private XYPairs function;

        /// <summary>
        /// The function view;
        /// </summary>
        private IFunctionView functionView;

        /// <summary>
        /// The parent explorer presenter.
        /// </summary>
        private ExplorerPresenter explorerPresenter;

        /// <summary>
        /// A reference to the 'graphPresenter' responsible for our graph.
        /// </summary>
        //private GraphPresenter graphPresenter;

        /// <summary>
        /// Our graph.
        /// </summary>
       // private Graph graph;

        /// <summary>
        /// Our grid.
        /// </summary>
        private IGridView grid;

        /// <summary>
        /// Attach the view to the model.
        /// </summary>
        /// <param name="model">The function model</param>
        /// <param name="view">The function view</param>
        /// <param name="explorerPresenter">The parent explorer presenter</param>
        public void Attach(object model, object view, ExplorerPresenter explorerPresenter)
        {
            this.function = model as XYPairs;
            this.functionView = view as IFunctionView;
            this.explorerPresenter = explorerPresenter as ExplorerPresenter;

            this.ConnectViewEvents();

            this.explorerPresenter.CommandHistory.ModelChanged += OnModelChanged;

            grid = functionView.gridView;

           // graphPresenter = new GraphPresenter();
 
            


            PopulateGrid();  
        }

        /// <summary>
        /// Detach the model from the view.
        /// </summary>
        public void Detach()
        {
            this.explorerPresenter.CommandHistory.ModelChanged -= OnModelChanged;
            this.DisconnectViewEvents();
        }
        
        /// <summary>
        /// Populate the grid
        /// </summary>
        /// <param name="model">The model to examine for properties</param>
        public void PopulateGrid()
        {
            IGridCell selectedCell = this.grid.GetCurrentCell;

            DataTable table = new DataTable();
  
            table.Columns.Add("X", typeof(object));
            table.Columns.Add("Y", typeof(object));

            for (int i = 0; i < function.X.Length; i++)
            {
                table.Rows.Add(new object[] { function.X[i], function.Y[i] });
            }
            
            this.grid.DataSource = table;
        }

        /// <summary>
        /// Connect all events from the view.
        /// </summary>
        private void ConnectViewEvents()
        {
           // this.functionView.OnCellValueChanged += this.OnCellValueChanged;
        }

        /// <summary>
        /// Disconnect all view events.
        /// </summary>
        private void DisconnectViewEvents()
        {
           // this.functionView.OnCellValueChanged -= this.OnCellValueChanged;
        }

        /// <summary>
        /// User has changed the value of a cell.
        /// </summary>
        /// <param name="sender">Sender of event</param>
        /// <param name="e">Event arguments</param>
        private void OnCellValueChanged(object sender, GridCellsChangedArgs e)
        {
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
